using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Represents a file tree.
    /// </summary>
    public class Tree
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tree"/> class.
        /// </summary>
        public Tree()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tree"/> class.
        /// </summary>
        /// <param name="path">The path of the base directory.</param>
        /// <param name="cacheDirectory">The location of where the cache dictionary will pe placed and read from, in case it exists.</param>
        public Tree(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            BaseDirectory = new DirectoryInfo(path);

            AllFiles = new List<FileInfo>();
            AddToAllFiles(BaseDirectory);
        }

        private void AddToAllFiles(DirectoryInfo directory)
        {
            AllFiles.AddRange(directory.Files);
            foreach(var dir in directory.Subdirectories)
            {
                AddToAllFiles(dir);
            }
        }

        /// <summary>
        /// Gets or sets the base directory.
        /// </summary>
        public DirectoryInfo BaseDirectory { get; set; }


        /// <summary>
        /// Gets all the files in the tree.
        /// </summary>
        public List<FileInfo> AllFiles { get; private set; }

        /// <summary>
        /// Calculates the differences between this tree and a different one.
        /// </summary>
        public IEnumerable<IODifference> Diff(Tree source, FileUpdateCheckType updateCheckType)
        {
            return Diffs(this.BaseDirectory, source.BaseDirectory, updateCheckType);
        }

        public enum FileUpdateCheckType { DateTime, Checksum }

        /// <summary>
        /// Computes and returns the differences between two file trees.
        /// </summary>
        private static IEnumerable<IODifference> Diffs(DirectoryInfo oldInfo, DirectoryInfo newInfo, FileUpdateCheckType updateCheckType)
        {
            var result = new List<IODifference>();
            //Added directories
            result.AddRange(newInfo.Subdirectories.Where(newDirectory => !oldInfo.Subdirectories.Any(x => x.Name == newDirectory.Name)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Addition,
                Path = Path.Join(x.Name),
                Type = IODifference.ObjectType.Directory
            }));
            //Removed directories
            result.AddRange(oldInfo.Subdirectories.Where(oldDirectory => !newInfo.Subdirectories.Any(x => x.Name == oldDirectory.Name)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Removal,
                Path = Path.Join(x.Name),
                Type = IODifference.ObjectType.Directory
            }));
            //Removed files
            result.AddRange(oldInfo.Files.Where(oldFile => !newInfo.Files.Any(x => x.Name == oldFile.Name)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Removal,
                Path = Path.Join(x.Name),
                Type = IODifference.ObjectType.File,
                Checksum = x.SHA256CheckSum
            }));
            //Added files
            result.AddRange(newInfo.Files.Where(newFile => !oldInfo.Files.Any(x => x.Name == newFile.Name)).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Addition,
                Path = Path.Join(x.Name),
                Type = IODifference.ObjectType.File,
                Checksum = x.SHA256CheckSum
            }));
            //Updated files
            result.AddRange(oldInfo.Files.Where(oldFile => newInfo.Files.Any(x => x.Name == oldFile.Name && ((updateCheckType == FileUpdateCheckType.Checksum) ? x.SHA256CheckSum != oldFile.SHA256CheckSum : x.LastModified != oldFile.LastModified))).Select(x => new IODifference()
            {
                DifferenceType = IODifference.IODifferenceType.Update,
                Path = Path.Join(x.Name),
                Type = IODifference.ObjectType.File,
                Checksum = x.SHA256CheckSum
            }));
            foreach (var x in oldInfo.Subdirectories)
            {
                if (newInfo.Subdirectories.Count(y => y.Name == x.Name) == 1)
                    result.AddRange(Diffs(x, newInfo.Subdirectories.First(y => y.Name == x.Name), updateCheckType));
            }
            foreach (var x in newInfo.Subdirectories)
            {
                if (oldInfo.Subdirectories.Count(y => y.Name == x.Name) == 1)
                    result.AddRange(Diffs(x, oldInfo.Subdirectories.First(y => y.Name == x.Name), updateCheckType));
            }
            return result;
        }

        /// <summary>
        /// Relativizes the current tree.
        /// </summary>
        public void Relativize()
        {
            BaseDirectory.Relativize(BaseDirectory.FullPath);
        }
    }
}
