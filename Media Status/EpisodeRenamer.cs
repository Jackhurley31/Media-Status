using Media_Status;
using MediaControlApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class EpisodeRenamer
{
    public static async Task ExecuteAutoRenameAsync(
        Form parentForm,
        ListView listEpisodes,
        TreeView treeShows,
        bool darkMode,
        Func<TreeNode, List<MediaItem>> getItemsFromNode,
        Func<string, string> buildFullPath,
        Action<MediaItem, string, string> transferState,
        Func<Task> saveAndMergeState,
        Action refreshAll,
        Action refreshNodeSelection)
    {
        List<MediaItem> itemsToRename = new();
        string showName = string.Empty;

        HashSet<string> categoryNodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Watched", "Continue", "Films", "TV Shows", "All Shows", "Favorites"
        };

        // 1. Check selected items in list view
        if (listEpisodes.SelectedItems.Count > 0)
        {
            foreach (ListViewItem li in listEpisodes.SelectedItems)
            {
                if (li.Tag is MediaItem ep) itemsToRename.Add(ep);
            }
            showName = itemsToRename.FirstOrDefault()?.CachedSeriesName;
        }
        // 2. Check selected tree node if nothing selected in list
        else if (treeShows.SelectedNode != null)
        {
            TreeNode selectedNode = treeShows.SelectedNode;
            itemsToRename = getItemsFromNode(selectedNode);

            TreeNode curr = selectedNode;
            while (curr != null)
            {
                string cleanedText = curr.Text.Replace("★ ", "").Trim();
                if (curr.Parent != null && categoryNodes.Contains(curr.Parent.Text.Trim()))
                {
                    showName = cleanedText;
                    break;
                }
                if (curr.Parent == null && !categoryNodes.Contains(cleanedText))
                {
                    showName = cleanedText;
                    break;
                }
                curr = curr.Parent;
            }

            if (string.IsNullOrWhiteSpace(showName))
            {
                showName = selectedNode.Text.Replace("★ ", "").Trim();
            }
        }

        // 3. Fallback: If still empty, automatically grab all items currently visible in the list view
        if (itemsToRename.Count == 0 && listEpisodes.Items.Count > 0)
        {
            foreach (ListViewItem li in listEpisodes.Items)
            {
                if (li.Tag is MediaItem ep) itemsToRename.Add(ep);
            }
        }

        // Fallback show name from first item if empty
        if (string.IsNullOrWhiteSpace(showName) && itemsToRename.Count > 0)
        {
            showName = itemsToRename.First().CachedSeriesName;
            if (string.IsNullOrWhiteSpace(showName))
            {
                string firstPath = buildFullPath(itemsToRename.First().FilePath);
                showName = Path.GetFileNameWithoutExtension(firstPath);
                var matchCheck = Regex.Match(showName, @"^(.*?)(?:\W+S\d+E\d+|\W+\d+x\d+)", RegexOptions.IgnoreCase);
                if (matchCheck.Success) showName = matchCheck.Groups[1].Value.Replace(".", " ").Trim();
            }
        }

        if (itemsToRename.Count == 0 || string.IsNullOrWhiteSpace(showName))
        {
            DarkMessageBox.Show(parentForm, "No items found to rename.", "Rename Episodes", MessageBoxButtons.OK, darkMode);
            return;
        }

        var confirmResult = DarkMessageBox.Show(parentForm, $"Are you sure you want to auto-rename {itemsToRename.Count} item(s) for '{showName}'?", "Confirm Auto-Rename", MessageBoxButtons.YesNo, darkMode);
        if (confirmResult != DialogResult.Yes) return;

        // Load settings
        string rawPattern = Media_Status.Properties.Settings.Default.RenameFormatPattern;
        string formatPattern = string.IsNullOrWhiteSpace(rawPattern) ? "{Show} - S{Season}E{Episode} - {Title}" : rawPattern;

        bool includeShow = GetSettingSafe(() => Media_Status.Properties.Settings.Default.RenameIncludeShow, true);
        bool padNumbers = GetSettingSafe(() => Media_Status.Properties.Settings.Default.RenamePadNumbers, true);
        bool replaceWithSpaces = GetSettingSafe(() => Media_Status.Properties.Settings.Default.RenameReplaceWithSpaces, false);
        bool replaceDots = GetSettingSafe(() => Media_Status.Properties.Settings.Default.RenameReplaceDots, false);
        bool removeSpecial = GetSettingSafe(() => Media_Status.Properties.Settings.Default.RenameRemoveSpecial, true);

        parentForm.Cursor = Cursors.WaitCursor;

        try
        {
            string commonJunkRegex = @"(?i)\b(720p|1080p|2160p|4K|HDTV|WEB-DL|WEBRip|HDRip|DVDRip|BDRip|BRRip|BluRay|HD|x264|x265|HEVC|AVC|Xvid|DivX|10bit|8bit|HDR|HDR10|Atmos|TrueHD|DDP?|DD\+|AC3|AAC|MP3|DTS|CH|\d+CH|PROPER|REPACK|INTERNAL|EXTENDED|UNRATED|REMASTERED|PSA|FENIX|EZTV|AFG|DSNP|NF|AMZN|HULU|ATVP|MAX|PCOK|CRITERION)\b";

            // Normalize show name for API query
            string processedShowName = showName.Replace(".", " ");
            string cleanShowNameForApi = Regex.Replace(processedShowName, commonJunkRegex, "", RegexOptions.IgnoreCase);
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"\(\d{4}\)", "").Trim();
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"\b(19|20)\d{2}\b", "").Trim();
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"(?i)\bS\d+E\d+\b", "").Trim();
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"(?i)\b\d+x\d+\b", "").Trim();
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"[\.\-_]+", " ").Trim();
            cleanShowNameForApi = Regex.Replace(cleanShowNameForApi, @"\s+", " ");

            bool apiSuccess = false;
            var episodeDict = new Dictionary<(int Season, int Episode), string>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(4);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("MediaControlApp/1.0");

                    string searchUrl = $"https://api.tvmaze.com/singlesearch/shows?q={Uri.EscapeDataString(cleanShowNameForApi)}&embed=episodes";
                    HttpResponseMessage response = await client.GetAsync(searchUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            if (doc.RootElement.TryGetProperty("_embedded", out var embedded) &&
                                embedded.TryGetProperty("episodes", out var episodesArray))
                            {
                                foreach (var epElem in episodesArray.EnumerateArray())
                                {
                                    if (epElem.TryGetProperty("season", out var sProp) && epElem.TryGetProperty("number", out var nProp))
                                    {
                                        if (nProp.ValueKind == JsonValueKind.Number)
                                        {
                                            int s = sProp.GetInt32();
                                            int eNum = nProp.GetInt32();
                                            string epName = epElem.GetProperty("name").GetString() ?? "";
                                            episodeDict[(s, eNum)] = epName;
                                        }
                                    }
                                }
                                apiSuccess = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // API failed, will rely smoothly on local cleaning rules
            }

            int renamedCount = 0;

            foreach (var item in itemsToRename)
            {
                try
                {
                    string oldFullPath = buildFullPath(item.FilePath);
                    if (string.IsNullOrWhiteSpace(oldFullPath) || !File.Exists(oldFullPath)) continue;

                    string fileNameOnly = Path.GetFileNameWithoutExtension(oldFullPath);

                    item.CacheParsed();
                    int season = item.CachedSeason;
                    int episode = item.CachedEpisode;

                    if (season <= 0 || episode <= 0)
                    {
                        var seMatch = Regex.Match(fileNameOnly, @"(?:S(\d+)\s*E(\d+))|(?:(\d+)x(\d+))", RegexOptions.IgnoreCase);
                        if (seMatch.Success)
                        {
                            if (seMatch.Groups[1].Success && seMatch.Groups[2].Success)
                            {
                                int.TryParse(seMatch.Groups[1].Value, out season);
                                int.TryParse(seMatch.Groups[2].Value, out episode);
                            }
                            else if (seMatch.Groups[3].Success && seMatch.Groups[4].Success)
                            {
                                int.TryParse(seMatch.Groups[3].Value, out season);
                                int.TryParse(seMatch.Groups[4].Value, out episode);
                            }
                        }
                    }

                    string epTitle = string.Empty;
                    if (apiSuccess && episodeDict.TryGetValue((season, episode), out string matchedTitle) && !string.IsNullOrWhiteSpace(matchedTitle))
                    {
                        epTitle = matchedTitle;
                    }
                    else
                    {
                        // Clean local fallback: strips out brackets, resolutions, codecs, release groups, and years
                        string cleaned = fileNameOnly.Replace(".", " ");
                        cleaned = Regex.Replace(cleaned, @"\[.*?\]", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"\(.*?\)", string.Empty);
                        cleaned = Regex.Replace(cleaned, commonJunkRegex, string.Empty, RegexOptions.IgnoreCase);
                        cleaned = Regex.Replace(cleaned, @$"(?i)\b{Regex.Escape(showName.Replace(".", " "))}\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"\b(19|20)\d{2}\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"(?i)\bS\d+E\d+\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"(?i)\b\d+x\d+\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"[\.\-_]+", " ");
                        epTitle = Regex.Replace(cleaned, @"\s+", " ").Trim();

                        // If title is completely blank or matches the show name, format it safely as "Episode X" instead of leaving group tags
                        if (string.IsNullOrWhiteSpace(epTitle) || epTitle.Equals(showName, StringComparison.OrdinalIgnoreCase))
                        {
                            epTitle = episode > 0 ? $"Episode {episode}" : "Episode";
                        }
                    }

                    string directory = Path.GetDirectoryName(oldFullPath);
                    string ext = Path.GetExtension(oldFullPath);

                    string seasonStr = padNumbers && season > 0 ? season.ToString("D2") : (season > 0 ? season.ToString() : "01");
                    string episodeStr = padNumbers && episode > 0 ? episode.ToString("D2") : (episode > 0 ? episode.ToString() : "01");

                    string formattedName = formatPattern;

                    // Clean show name for final file output (removes standalone years like 2026 from the filename display)
                    string displayShowName = Regex.Replace(showName, @"\b(19|20)\d{2}\b", "").Trim();
                    displayShowName = Regex.Replace(displayShowName, @"\s+", " ");

                    if (includeShow && !string.IsNullOrWhiteSpace(displayShowName))
                    {
                        formattedName = formattedName.Replace("{Show}", displayShowName);
                    }
                    else
                    {
                        formattedName = formattedName.Replace("{Show}", "").Trim();
                        formattedName = Regex.Replace(formattedName, @"^[\s\-\._]+", "");
                    }

                    formattedName = formattedName
                        .Replace("{Season}", seasonStr)
                        .Replace("{Episode}", episodeStr)
                        .Replace("{Title}", epTitle);

                    if (replaceWithSpaces) formattedName = formattedName.Replace('_', ' ');
                    if (replaceDots) formattedName = formattedName.Replace('.', ' ');

                    if (removeSpecial)
                    {
                        foreach (char c in Path.GetInvalidFileNameChars())
                        {
                            formattedName = formattedName.Replace(c.ToString(), "");
                        }
                    }

                    string newFileName = Regex.Replace(formattedName, @"\s+", " ").Trim() + ext;
                    string newFullPath = Path.Combine(directory, newFileName);

                    if (oldFullPath.Equals(newFullPath, StringComparison.Ordinal)) continue;

                    if (File.Exists(newFullPath))
                    {
                        if (oldFullPath.Equals(newFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string tempPath = oldFullPath + ".tmp_" + Guid.NewGuid().ToString("N");
                            File.Move(oldFullPath, tempPath);
                            File.Move(tempPath, newFullPath);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        File.Move(oldFullPath, newFullPath);
                    }

                    transferState(item, oldFullPath, newFullPath);
                    item.FilePath = newFullPath;
                    item.CacheParsed();
                    renamedCount++;
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenameError] {innerEx.Message}");
                }
            }

            if (renamedCount > 0)
            {
                foreach (ListViewItem lvi in listEpisodes.Items)
                {
                    if (lvi.Tag is MediaItem m) m.CacheParsed();
                }

                await saveAndMergeState();
                refreshAll();
                refreshNodeSelection();

                string msg = apiSuccess ? "Successfully renamed files using API data!" : "Successfully cleaned up files using local rules!";
                DarkMessageBox.Show(parentForm, $"{msg} ({renamedCount} file(s) updated).", "Auto-Rename Complete", MessageBoxButtons.OK, darkMode);
            }
            else
            {
                DarkMessageBox.Show(parentForm, "No files needed renaming or they were already formatted correctly.", "Auto-Rename", MessageBoxButtons.OK, darkMode);
            }
        }
        catch (Exception ex)
        {
            DarkMessageBox.Show(parentForm, $"Error during auto-rename: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
        }
        finally
        {
            parentForm.Cursor = Cursors.Default;
        }
    }

    private static bool GetSettingSafe(Func<bool> settingGetter, bool fallback)
    {
        try { return settingGetter(); }
        catch { return fallback; }
    }
}