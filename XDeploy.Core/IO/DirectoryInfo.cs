using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Describes basic information for a directory.
    /// </summary>
    public class DirectoryInfo : IRelativizeable
    {
        public DirectoryInfo()
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryInfo"/> class.
        /// </summary>
        public DirectoryInfo(string fullPath)
        {
            if (fullPath is null)
                throw new ArgumentNullException(nameof(fullPath));

            FullPath = fullPath;
            Name = fullPath.Split(Path.DirectorySeparatorChar)[^1];
            Subdirectories = new List<DirectoryInfo>();
            Files = new List<FileInfo>();
            foreach (var subDirectory in Directory.EnumerateDirectories(fullPath))
            {
                Subdirectories.Add(new DirectoryInfo(subDirectory));
            }
            foreach (var file in Directory.EnumerateFiles(fullPath, "*.*"))
            {
                Files.Add(new FileInfo(file));
            }
        }

        /// <summary>
        /// Gets or sets the name of the directory.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the base path of the directory.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the subdirectories.
        /// </summary>
        public IList<DirectoryInfo> Subdirectories { get; set; }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        public IList<FileInfo> Files { get; set; }

        public void Relativize(string fullPath)
        {
            FullPath = FullPath.Replace(fullPath, string.Empty);
            foreach(var dir in Subdirectories)
            {
                dir.Relativize(fullPath);
            }
            foreach(var file in Files)
            {
                file.Relativize(fullPath);
            }
        }
    }
}
