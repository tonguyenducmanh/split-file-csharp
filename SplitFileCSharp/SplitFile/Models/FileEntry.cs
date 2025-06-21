namespace SplitFile.Models
{
    public class FileEntry
    {
        /// <summary>
        /// Đường dẫn đầy đủ của file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Tên file không bao gồm đường dẫn
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        /// Đánh dấu file chính
        /// </summary>
        public bool IsMainFile { get; set; }

        /// <summary>
        /// Trạng thái đã phân tích
        /// </summary>
        public bool IsAnalyzed { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public FileEntry(string filePath, bool isMainFile = false)
        {
            FilePath = filePath;
            IsMainFile = isMainFile;
            IsAnalyzed = false;
        }

        public override bool Equals(object obj)
        {
            if (obj is FileEntry other)
            {
                return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FilePath.ToLower().GetHashCode();
        }
    }
}