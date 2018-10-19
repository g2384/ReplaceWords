using System.Collections.Generic;

namespace ReplaceWords
{
    public class Settings
    {
        public string SearchTerm { get; set; }

        public string RepalcementTerm { get; set; }

        public string SourceFilePath { get; set; }

        public List<string> ExcludeFolders { get; set; }

        public List<string> FileExtensions { get; set; }

        public bool IsRegex { get; set; }

        public bool ShowFileDetails { get; set; }

        public string Delimiter { get; set; }

        public void Init()
        {
            FileExtensions = new List<string>
            {
                ".*"
            };

            ExcludeFolders = new List<string>
            {
                @"\obj\", @"\bin\"
            };
        }
    }
}
