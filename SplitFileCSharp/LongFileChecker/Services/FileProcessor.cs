using LongFileChecker.Models;

namespace LongFileChecker.Services
{
    public class FileProcessor
    {
        private readonly CodeAnalyzerLongFile _codeAnalyzer;

        public FileProcessor()
        {
            _codeAnalyzer = new CodeAnalyzerLongFile();
        }

        public async Task ProcessFile(string filePath, decimal maxLength, bool detailAnalysis, bool findUnused, Action<FileData> onFileProcessed)
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            if (fileContent.Length > maxLength)
            {
                var fileData = new FileData
                {
                    Path = filePath,
                    Length = fileContent.Length
                };

                if (detailAnalysis && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    fileData.CodeItems = _codeAnalyzer.AnalyzeFile(fileContent, findUnused);
                }

                onFileProcessed?.Invoke(fileData);
            }
        }

        public async Task ProcessFolder(string folderPath, string filePattern, decimal maxLength, bool detailAnalysis, bool findUnused, 
            Action<FileData> onFileProcessed, CancellationToken cancellationToken)
        {
            var filePatterns = ProcessFilePatterns(filePattern);
            var includePatterns = filePatterns.includePatterns;
            var excludePatterns = filePatterns.excludePatterns;

            var allFiles = GetMatchingFiles(folderPath, includePatterns, excludePatterns, cancellationToken);

            foreach (var file in allFiles)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await ProcessFile(file, maxLength, detailAnalysis, findUnused, onFileProcessed);
                }
                catch
                {
                    // File processing errors are handled by the caller
                    throw;
                }
            }
        }

        private (List<string> includePatterns, List<string> excludePatterns) ProcessFilePatterns(string filePattern)
        {
            var patterns = filePattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var includePatterns = patterns.Where(p => !p.StartsWith("!")).ToList();
            var excludePatterns = patterns.Where(p => p.StartsWith("!"))
                                        .Select(p => p.Substring(1))
                                        .ToList();

            if (!includePatterns.Any())
            {
                includePatterns.Add("*.*");
            }

            return (includePatterns, excludePatterns);
        }

        private HashSet<string> GetMatchingFiles(string folderPath, List<string> includePatterns, List<string> excludePatterns, CancellationToken cancellationToken)
        {
            var allFiles = new HashSet<string>();
            foreach (var pattern in includePatterns)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var files = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
                allFiles.UnionWith(files);
            }

            return allFiles.Where(file => !ShouldExcludeFile(file, excludePatterns)).ToHashSet();
        }

        private bool ShouldExcludeFile(string file, List<string> excludePatterns)
        {
            return excludePatterns.Any(ep =>
            {
                var wildcard = WildcardToRegex(ep);
                return System.Text.RegularExpressions.Regex.IsMatch(file, wildcard);
            });
        }

        private string WildcardToRegex(string pattern)
        {
            return "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".")
                + "$";
        }
    }
}