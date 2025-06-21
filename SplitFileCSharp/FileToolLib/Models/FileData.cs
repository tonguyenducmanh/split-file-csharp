using System.Collections.Generic;

namespace LongFileChecker.Models
{
    public class FileData
    {
        public string Path { get; set; }
        public long Length { get; set; }
        public List<CodeItemLongFile> CodeItems { get; set; }
    }
}