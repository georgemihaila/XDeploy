using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace XDeploy.Core.IO.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IO"/> namespace.
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
        /// Computes and returns the differences between two <see cref="FileInfo"/> lists.
        /// </summary>
        public static IEnumerable<IODifference> Diff(IEnumerable<FileInfo> current, IEnumerable<FileInfo> previous)
        {
            var result = new List<IODifference>();
            result.AddRange(current.Where(x => !previous.Select(x => x.SHA256CheckSum).Contains(x.SHA256CheckSum)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Addition,
                Path = x.Name,
                Type = IODifference.ObjectType.File,
                Checksum = x.SHA256CheckSum
            }));
            result.AddRange(previous.Where(x => !current.Select(x => x.SHA256CheckSum).Contains(x.SHA256CheckSum)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Removal,
                Path = x.Name,
                Type = IODifference.ObjectType.File,
                Checksum = x.SHA256CheckSum
            }));
            return result;
        }
    }
}
