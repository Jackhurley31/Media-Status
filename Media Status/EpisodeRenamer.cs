using Media_Status;
using MediaControlApp.Models;
using MediaControlApp.Forms;
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
    private static readonly Dictionary<string, string> _lastRenameHistory = new(StringComparer.OrdinalIgnoreCase);

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
        string targetName = string.Empty;
        bool isFilmSelection = false;

        HashSet<string> categoryNodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Watched", "Continue", "Recently Added", "Films", "TV Shows", "All Shows", "Favorites", "Other"
        };

        // 1. Determine if selection is under Films
        if (treeShows.SelectedNode != null)
        {
            TreeNode check = treeShows.SelectedNode;
            while (check != null)
            {
                string text = check.Text.Replace("★ ", "").Trim();
                if (text.Equals("Films", StringComparison.OrdinalIgnoreCase))
                {
                    isFilmSelection = true;
                    break;
                }
                check = check.Parent;
            }
        }

        if (listEpisodes.SelectedItems.Count > 0)
        {
            foreach (ListViewItem li in listEpisodes.SelectedItems)
            {
                if (li.Tag is MediaItem ep) itemsToRename.Add(ep);
            }
            targetName = itemsToRename.FirstOrDefault()?.CachedSeriesName;
        }
        else if (treeShows.SelectedNode != null)
        {
            TreeNode selectedNode = treeShows.SelectedNode;
            string topNodeText = selectedNode.Text.Replace("★ ", "").Trim();

            if (categoryNodes.Contains(topNodeText) && topNodeText != "Films")
            {
                DarkMessageBox.Show(parentForm, "Please select an actual show, season, or film folder to rename.", "Auto-Rename", MessageBoxButtons.OK, darkMode);
                return;
            }

            itemsToRename = getItemsFromNode(selectedNode);

            if (isFilmSelection && itemsToRename.Count == 0 && selectedNode.Nodes.Count > 0)
            {
                foreach (TreeNode subNode in selectedNode.Nodes)
                {
                    itemsToRename.AddRange(getItemsFromNode(subNode));
                }
            }

            TreeNode curr = selectedNode;
            while (curr != null)
            {
                string cleanedText = curr.Text.Replace("★ ", "").Trim();

                if (!categoryNodes.Contains(cleanedText))
                {
                    if (curr.Parent == null || categoryNodes.Contains(curr.Parent.Text.Trim()))
                    {
                        targetName = cleanedText;
                        break;
                    }
                }
                curr = curr.Parent;
            }

            if (string.IsNullOrWhiteSpace(targetName))
            {
                targetName = selectedNode.Text.Replace("★ ", "").Trim();
            }
        }

        if (itemsToRename.Count == 0 && listEpisodes.Items.Count > 0)
        {
            foreach (ListViewItem li in listEpisodes.Items)
            {
                if (li.Tag is MediaItem ep) itemsToRename.Add(ep);
            }
        }

        if (string.IsNullOrWhiteSpace(targetName) && itemsToRename.Count > 0)
        {
            string firstPath = buildFullPath(itemsToRename.First().FilePath);
            if (!string.IsNullOrWhiteSpace(firstPath))
            {
                DirectoryInfo dir = Directory.GetParent(firstPath);
                if (dir != null && !categoryNodes.Contains(dir.Name))
                {
                    targetName = dir.Name;
                }
            }
        }

        if (itemsToRename.Count == 0 || string.IsNullOrWhiteSpace(targetName))
        {
            DarkMessageBox.Show(parentForm, "No items found to rename.", "Rename Items", MessageBoxButtons.OK, darkMode);
            return;
        }

        parentForm.Cursor = Cursors.WaitCursor;

        try
        {

            var localSettings = AppConfig.Load();

            var junkTags = JunkTagManager.Load();

            string commonJunkRegex =
                $@"(?i)\b({string.Join("|", junkTags)})\b";

            var previewItems = new List<AutoRenamePreviewItem>();

            // =========================================================================
            // FILMS RENAME PREVIEW
            // =========================================================================
            if (isFilmSelection)
            {
                foreach (var item in itemsToRename)
                {
                    string oldFullPath = buildFullPath(item.FilePath);
                    if (string.IsNullOrWhiteSpace(oldFullPath) || !File.Exists(oldFullPath)) continue;

                    string fileNameOnly = Path.GetFileNameWithoutExtension(oldFullPath);

                    string cleaned = fileNameOnly.Replace(".", " ").Replace("_", " ");
                    cleaned = Regex.Replace(cleaned, @"\[.*?\]", string.Empty);
                    cleaned = Regex.Replace(cleaned, @"\(.*?\)", string.Empty);
                    cleaned = Regex.Replace(cleaned, commonJunkRegex, string.Empty, RegexOptions.IgnoreCase);
                    cleaned = Regex.Replace(cleaned, @"[\.\-_]+", " ");
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

                    if (string.IsNullOrWhiteSpace(cleaned)) cleaned = fileNameOnly;

                    string directory = Path.GetDirectoryName(oldFullPath);
                    string ext = Path.GetExtension(oldFullPath);

                    string newFileName = cleaned + ext;
                    string newFullPath = Path.Combine(directory, newFileName);

                    if (!oldFullPath.Equals(newFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        previewItems.Add(new AutoRenamePreviewItem
                        {
                            MediaItem = item,
                            OriginalName = Path.GetFileName(oldFullPath),
                            NewName = newFileName,
                            OriginalFullPath = oldFullPath,
                            NewFullPath = newFullPath
                        });
                    }
                }
            }
            else
            {
                // =========================================================================
                // TV SHOWS RENAME PREVIEW (Loads from Local AppConfig JSON & TVMaze API)
                // =========================================================================

                string rawPattern = localSettings.RenameFormatPattern;
                string formatPattern = string.IsNullOrWhiteSpace(rawPattern) ? "{Show} - S{Season}E{Episode} - {Title}" : rawPattern;

                bool includeShow = localSettings.RenameIncludeShow;
                bool padNumbers = localSettings.RenamePadNumbers;
                bool replaceWithSpaces = localSettings.RenameReplaceWithSpaces;
                bool replaceDots = localSettings.RenameReplaceDots;
                bool removeSpecial = localSettings.RenameRemoveSpecial;

                string processedShowName = targetName.Replace(".", " ");
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
                        client.Timeout = TimeSpan.FromSeconds(5);
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
                catch { }

                foreach (var item in itemsToRename)
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
                        string cleaned = fileNameOnly.Replace(".", " ");
                        cleaned = Regex.Replace(cleaned, @"\[.*?\]", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"\(.*?\)", string.Empty);
                        cleaned = Regex.Replace(cleaned, commonJunkRegex, string.Empty, RegexOptions.IgnoreCase);
                        cleaned = Regex.Replace(cleaned, @$"(?i)\b{Regex.Escape(targetName.Replace(".", " "))}\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"\b(19|20)\d{2}\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"(?i)\bS\d+E\d+\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"(?i)\b\d+x\d+\b", string.Empty);
                        cleaned = Regex.Replace(cleaned, @"[\.\-_]+", " ");
                        epTitle = Regex.Replace(cleaned, @"\s+", " ").Trim();

                        if (string.IsNullOrWhiteSpace(epTitle) || epTitle.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            epTitle = fileNameOnly;
                        }
                    }

                    string directory = Path.GetDirectoryName(oldFullPath);
                    string ext = Path.GetExtension(oldFullPath);

                    int safeSeason = season > 0 ? season : 1;
                    int safeEpisode = episode > 0 ? episode : 1;

                    string seasonStr = padNumbers ? safeSeason.ToString("D2") : safeSeason.ToString();
                    string episodeStr = padNumbers ? safeEpisode.ToString("D2") : safeEpisode.ToString();

                    string formattedName = formatPattern;

                    string displayShowName = Regex.Replace(targetName, @"\b(19|20)\d{2}\b", "").Trim();
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

                    if (!oldFullPath.Equals(newFullPath, StringComparison.Ordinal))
                    {
                        previewItems.Add(new AutoRenamePreviewItem
                        {
                            MediaItem = item,
                            OriginalName = Path.GetFileName(oldFullPath),
                            NewName = newFileName,
                            OriginalFullPath = oldFullPath,
                            NewFullPath = newFullPath
                        });
                    }
                }
            }

            parentForm.Cursor = Cursors.Default;

            if (previewItems.Count == 0)
            {
                DarkMessageBox.Show(parentForm, "All items are already correctly formatted or no changes needed.", "Auto-Rename", MessageBoxButtons.OK, darkMode);
                return;
            }

            // =========================================================================
            // SHOW BEFORE/AFTER PREVIEW DIALOG (YES / NO)
            // =========================================================================
            using (var previewForm = new AutoRenamePreviewForm(previewItems, darkMode))
            {
                if (previewForm.ShowDialog(parentForm) != DialogResult.OK || previewForm.ApprovedItems.Count == 0)
                {
                    return; // User clicked Cancel (No)
                }

                parentForm.Cursor = Cursors.WaitCursor;
                int renamedCount = 0;
                bool replacedUndoHistory = false;

                try
                {
                    await Task.Run(() =>
                    {
                        foreach (var preview in previewForm.ApprovedItems)
                        {
                            if (File.Exists(preview.OriginalFullPath))
                            {
                                if (File.Exists(preview.NewFullPath))
                                {
                                    string tempPath = preview.OriginalFullPath + ".tmp_" + Guid.NewGuid().ToString("N");
                                    File.Move(preview.OriginalFullPath, tempPath);
                                    File.Move(tempPath, preview.NewFullPath);
                                }
                                else
                                {
                                    File.Move(preview.OriginalFullPath, preview.NewFullPath);
                                }

                                lock (_lastRenameHistory)
                                {
                                    if (!replacedUndoHistory)
                                    {
                                        _lastRenameHistory.Clear();
                                        replacedUndoHistory = true;
                                    }
                                    _lastRenameHistory[preview.NewFullPath] = preview.OriginalFullPath;
                                }

                                preview.MediaItem.FilePath = preview.NewFullPath;
                                preview.MediaItem.CacheParsed();

                                // Update visible ListView row immediately
                                parentForm.Invoke((MethodInvoker)delegate
                                {
                                    foreach (ListViewItem lvi in listEpisodes.Items)
                                    {
                                        if (ReferenceEquals(lvi.Tag, preview.MediaItem))
                                        {
                                            lvi.Text = Path.GetFileName(preview.NewFullPath);
                                            break;
                                        }
                                    }
                                });

                                renamedCount++;
                            }
                        }
                        saveAndMergeState().Wait();
                    });

                    refreshAll();
                    refreshNodeSelection();

                    DarkMessageBox.Show(parentForm, $"Successfully renamed {renamedCount} file(s)!", "Rename Complete", MessageBoxButtons.OK, darkMode);
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show(parentForm, $"Error during execution: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
                }
                finally
                {
                    parentForm.Cursor = Cursors.Default;
                }
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

    private static async Task RevertLastRenameAsync(
        Form parentForm,
        ListView listEpisodes,
        Func<string, string> buildFullPath,
        Action<MediaItem, string, string> transferState,
        Func<Task> saveAndMergeState,
        Action refreshAll,
        Action refreshNodeSelection,
        bool darkMode)
    {
        if (_lastRenameHistory.Count == 0) return;

        parentForm.Cursor = Cursors.WaitCursor;
        int revertedCount = 0;

        try
        {
            foreach (var kvp in _lastRenameHistory.ToList())
            {
                string currentPath = kvp.Key;
                string originalPath = kvp.Value;

                if (File.Exists(currentPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalPath) ?? string.Empty);
                    File.Move(currentPath, originalPath);
                    revertedCount++;

                    foreach (ListViewItem lvi in listEpisodes.Items)
                    {
                        if (lvi.Tag is MediaItem m)
                        {
                            string fullCurrent = buildFullPath(m.FilePath);
                            if (currentPath.Equals(fullCurrent, StringComparison.OrdinalIgnoreCase))
                            {
                                transferState(m, currentPath, originalPath);
                                m.FilePath = originalPath;
                                m.CacheParsed();
                            }
                        }
                    }

                    _lastRenameHistory.Remove(currentPath);
                }
            }

            await saveAndMergeState();
            refreshAll();
            refreshNodeSelection();

            string resultMessage = revertedCount == 0
                ? $"No renamed files were found. Undo history for {_lastRenameHistory.Count} file(s) was kept."
                : $"Successfully reverted {revertedCount} file(s) to their original names.";
            if (revertedCount > 0 && _lastRenameHistory.Count > 0)
            {
                resultMessage += $"\n\nUndo history for {_lastRenameHistory.Count} file(s) was kept because the renamed files could not be found.";
            }

            DarkMessageBox.Show(parentForm, resultMessage, "Revert Complete", MessageBoxButtons.OK, darkMode);
        }
        catch (Exception ex)
        {
            DarkMessageBox.Show(parentForm, $"Error during revert: {ex.Message}", "Error", MessageBoxButtons.OK, darkMode);
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