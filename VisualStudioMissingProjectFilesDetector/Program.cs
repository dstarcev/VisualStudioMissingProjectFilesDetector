using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
			var filesMustBeIncluded = FilterFiles(existingFiles, filePattern);

			var missingFiles = GetMissingFiles(filesMustBeIncluded, includedFiles).ToList();
			if (missingFiles.Any()) {
				Console.WriteLine("Not included files found!");
				foreach (var missingFile in missingFiles) {
					Console.WriteLine("{0} not included!", missingFile);
				}

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
			return files.Where(file => regex.IsMatch(file)).ToList();
		}

		private static IEnumerable<string> GetExistingFiles(string projectFile) {
			var projectDirectory = Path.GetDirectoryName(projectFile);
			return Directory.EnumerateFiles(projectDirectory, "*.*", SearchOption.AllDirectories)
				.ToList();
		}

		private static ISet<string> GetIncludedFiles(string projectFile) {
			var fileContent = ReadFile(projectFile);
			var projectDirectory = Path.GetDirectoryName(projectFile);
			var includePattern = new Regex("Include=\"(.*?)\"", RegexOptions.Compiled);

			var filePaths = includePattern.Matches(fileContent)
				.Cast<Match>()
				.Where(match => match.Success)
				.Select(match => match.Groups[1].Value)
				.Select(fileName => Path.Combine(projectDirectory, fileName));
			
			return new HashSet<string>(filePaths, StringComparer.OrdinalIgnoreCase);
		}

		private static string ReadFile(string filePath) {
			using (var reader = new StreamReader(filePath)) {
				return reader.ReadToEnd();
			}
		}
	}
}