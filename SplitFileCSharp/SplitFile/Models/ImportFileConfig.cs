using System.ComponentModel.DataAnnotations;

namespace SplitFile.Models
{
    public class ImportFileConfig
    {
        /// <summary>
        /// Đường dẫn đầy đủ của file gốc cần tách
        /// </summary>
        [Required(ErrorMessage = "Phải có đường dẫn file gốc")]
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
        [Required(ErrorMessage = "Phải có tên file mới")]
        public string NewFileName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả về nội dung file
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Danh sách phương thức cần tách
        /// </summary>
        [Required(ErrorMessage = "Phải có ít nhất một phương thức")]
        public string Methods { get; set; } = string.Empty;

        /// <summary>
        /// Kiểm tra tính hợp lệ của cấu hình
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(OriginalFile) || !File.Exists(OriginalFile))
                return false;

            if (!IsMainFile && (string.IsNullOrWhiteSpace(MainFile) || !File.Exists(MainFile)))
                return false;

            if (string.IsNullOrWhiteSpace(NewFileName))
                return false;

            if (string.IsNullOrWhiteSpace(Methods))
                return false;

            return true;
        }

        /// <summary>
        /// Chuyển đổi thành đối tượng SplitConfig
        /// </summary>
        public SplitConfig ToSplitConfig()
        {
            return new SplitConfig
            {
                OriginalFile = OriginalFile,
                IsMainFile = IsMainFile,
                MainFile = MainFile,
                NewFileName = NewFileName,
                Description = Description ?? string.Empty,
                MethodNames = Methods.Split(new[] { Environment.NewLine, " ", "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m =>
                    {
                        var parenIndex = m.IndexOf('(');
                        var name = parenIndex > 0 ? m.Substring(0, parenIndex) : m;
                        return name.Trim();
                    })
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct()
                    .ToList()
            };
        }
    }
}