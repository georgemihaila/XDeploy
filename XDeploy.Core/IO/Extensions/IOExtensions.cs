using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace XDeploy.Core.IO.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="XDeploy.Core.IO"/> namespace.
    /// </summary>
    public static class IOExtensions
    {
        /// <summary>
        /// Converts the specified differences to a human-readable format.
        /// </summary>
        public static string Format(this IEnumerable<IODifference> diffs) => $"Files:{Environment.NewLine + '\t'}" +
                            $"{diffs.Where(x => x.Type == IODifference.ObjectType.File && x.DifferenceType == IODifference.IODifferenceType.Addition).Count()} new {Environment.NewLine + '\t'}" +
                            $"{diffs.Where(x => x.Type == IODifference.ObjectType.File && x.DifferenceType == IODifference.IODifferenceType.Update).Count()} changed {Environment.NewLine + '\t'}" +
                            $"{diffs.Where(x => x.Type == IODifference.ObjectType.File && x.DifferenceType == IODifference.IODifferenceType.Removal).Count()} removed {Environment.NewLine}" +
                            $"Directories:{Environment.NewLine + '\t'}" +
                            $"{diffs.Where(x => x.Type == IODifference.ObjectType.Directory && x.DifferenceType == IODifference.IODifferenceType.Addition).Count()} new {Environment.NewLine + '\t'}" +
                            $"{diffs.Where(x => x.Type == IODifference.ObjectType.Directory && x.DifferenceType == IODifference.IODifferenceType.Removal).Count()} removed {Environment.NewLine}";

        /// <summary>
        /// Gets all subdirectories of this directory.
        /// </summary>
        public static IEnumerable<DirectoryInfo> GetAllSubdirectories(this DirectoryInfo directoryInfo)
        {
            var result = new List<DirectoryInfo>();
            result.AddRange(directoryInfo.Subdirectories);
            foreach(var dir in directoryInfo.Subdirectories)
            {
                result.AddRange(dir.GetAllSubdirectories());
            }
            return result;
        }

        public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo directoryInfo)
        {
            var result = new List<FileInfo>();
            result.AddRange(directoryInfo.Files);
            foreach (var dir in directoryInfo.GetAllSubdirectories())
            {
                result.AddRange(dir.GetAllFiles());
            }
            return result;
        }
    }
}
