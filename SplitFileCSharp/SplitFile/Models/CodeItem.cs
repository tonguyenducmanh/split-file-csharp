namespace SplitFile.Models
{
    #region "Public class"
    public class CodeItem
    {
        #region "Property"
        /// <summary>
        /// Tên của thành phần
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Độ dài thành phần
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Loại (Field, Property, Method, etc.)
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        /// Kiểu trả về (với method) hoặc kiểu dữ liệu (với field/property)
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Tham số (chỉ với method)
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Access modifier (public, private...)
        /// </summary>
        public string AccessModifier { get; set; }

        /// <summary>
        /// Nội dung đầy đủ của thành phần
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Đã được tách vào file khác chưa
        /// </summary>
        public bool IsExtracted { get; set; }
        #endregion
    }
    #endregion
}