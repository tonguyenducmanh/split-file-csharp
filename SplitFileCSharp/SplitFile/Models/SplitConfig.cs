namespace SplitFile.Models
{
    public class SplitConfig
    {
        /// <summary>
        /// Đường dẫn đầy đủ của file gốc cần tách
        /// </summary>
        public string OriginalFile { get; set; } = string.Empty;

        /// <summary>
        /// Có phải là file chính không
        /// </summary>
        public bool IsMainFile { get; set; }

        /// <summary>
        /// Đường dẫn đầy đủ của file chính (nếu đây là file phụ)
        /// </summary>
        public string? MainFile { get; set; }

        /// <summary>
        /// Tên file mới sau khi tách
        /// </summary>
        public string NewFileName { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách tên các phương thức cần tách
        /// </summary>
        public List<string> MethodNames { get; set; } = new();

        /// <summary>
        /// Mô tả về nội dung file
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Tên class
        /// </summary>
        public string? ClassName { get; set; }
    }
}