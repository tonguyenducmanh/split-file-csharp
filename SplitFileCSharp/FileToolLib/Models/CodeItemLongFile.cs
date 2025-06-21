namespace LongFileChecker.Models
{
    public class CodeItemLongFile
    {
        public string Type { get; set; }  // "Class" hoặc "Method"
        public string Name { get; set; }
        public long Length { get; set; }
        public bool IsMethod { get; set; }
        public string AccessModifier { get; set; }
        public string ReturnType { get; set; }
        public string Parameters { get; set; }
        public bool IsUsed { get; set; }

        // Các trường cần thiết cho việc xóa chính xác và hiển thị
        public string FilePath { get; set; } // Đường dẫn file chứa item này
        public string ClassName { get; set; } // Tên của class chứa member này (nếu là member)
        public int OriginalStartOffset { get; set; } // Vị trí bắt đầu của FullSpan gốc
        public int OriginalFullSpanLength { get; set; } // Độ dài của FullSpan gốc
    }
}