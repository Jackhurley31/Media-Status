using MediaControlApp.Models;

namespace MediaControlApp.Forms
{
    public class AutoRenamePreviewItem
    {
        public MediaItem MediaItem { get; set; }
        public string OriginalName { get; set; }
        public string NewName { get; set; }
        public string OriginalFullPath { get; set; }
        public string NewFullPath { get; set; }
    }
}