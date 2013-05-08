using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace VisualStudioMissingProjectFilesDetector {
	internal class Program {
		private static int Main(string[] args) {
			var projectFile = args.FirstOrDefault();
			var filePattern = args.Skip(1).FirstOrDefault();
			if (string.IsNullOrEmpty(projectFile) || string.IsNullOrEmpty(filePattern)) {
				Console.WriteLine("Usage: [projectFile] [filePattern]");
				return -1;
			}

			var includedFiles = GetIncludedFiles(projectFile);
			var existingFiles = GetExistingFiles(projectFile);
			
			var notIncludedFiles = GetMissingFiles(FilterFiles(existingFiles, filePattern), includedFiles).ToList();
			if (notIncludedFiles.Any()) {
				Console.WriteLine("Not included files found!");
				foreach (var missingFile in notIncludedFiles) {
					Console.WriteLine("{0} not included!", missingFile);
				}
			}

			var missingFiles = GetMissingFiles(FilterFiles(includedFiles, filePattern), existingFiles).ToList();
			if (missingFiles.Any()) {
				Console.WriteLine("Missing files found!");
				foreach (var missingFile in missingFiles) {
					Console.WriteLine("{0} are missing!", missingFile);
				}
			}

			if (notIncludedFiles.Any() || missingFiles.Any()) {
				return -1;
			}

			return 0;
		}

		private static IEnumerable<string> GetMissingFiles(IEnumerable<string> filesMustBeIncluded, ISet<string> includedFiles) {
			foreach (var fileName in filesMustBeIncluded) {
				if (!includedFiles.Contains(fileName)) {
					yield return fileName;
				}
			}
		}

		private static IEnumerable<string> FilterFiles(IEnumerable<string> files, string filePattern) {
			var regex = new Regex(filePattern, RegexOptions.Compiled);
			return files.Where(file => 
				regex.IsMatch(file)
			).ToList();
		}

		private static ISet<string> GetExistingFiles(string projectFile) {
			var projectDirectory = Path.GetDirectoryName(projectFile);
			var filePaths = Directory.EnumerateFiles(projectDirectory, "*.*", SearchOption.AllDirectories);

			return new HashSet<string>(filePaths, StringComparer.OrdinalIgnoreCase);
		}

		private static ISet<string> GetIncludedFiles(string projectFile) {
			var fileContent = ReadFile(projectFile);
			var projectDirectory = Path.GetDirectoryName(projectFile);
			var includePattern = new Regex("Include=\"(.*?)\"", RegexOptions.Compiled);

			var filePaths = includePattern.Matches(fileContent)
				.Cast<Match>()
				.Where(match => match.Success)
				.Select(match => match.Groups[1].Value)
				.Select(HttpUtility.UrlDecode)
				.Select(HttpUtility.HtmlDecode)
				.Select(fileName => 
					Path.Combine(projectDirectory, fileName)
					)
				.Where(file => File.Exists(file) || !Directory.Exists(file));
			
			return new HashSet<string>(filePaths, StringComparer.OrdinalIgnoreCase);
		}

		private static string ReadFile(string filePath) {
			using (var reader = new StreamReader(filePath)) {
				return reader.ReadToEnd();
			}
		}
	}
}