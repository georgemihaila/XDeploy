using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XDeploy.Core.IO.FileManagement
{
    /// <summary>
    /// Represents a file manager that caches files to the disk.
    /// </summary>
    public class DiskFileManager
    {
        /// <summary>
        /// Gets the base location.
        /// </summary>
        public string BaseLocation { get; private set; }

        /// <summary>
        /// Gets all files the base directory as a collection of <see cref="FileInfo"/>s.
        /// </summary>
        public IEnumerable<FileInfo> AsFileInfoCollection() => System.IO.Directory.EnumerateFiles(BaseLocation, "*.*", System.IO.SearchOption.AllDirectories).Select(x => new FileInfo()
        {
            Name = x.Replace(BaseLocation, string.Empty),
            LastModified = (new System.IO.FileInfo(x)).LastWriteTime,
            SHA256CheckSum = Cryptography.SHA256CheckSum(x)
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskFileManager"/> class.
        /// </summary>
        /// <param name="baseLocation">The base location.</param>
        public DiskFileManager(string baseLocation)
        {
            if (baseLocation is null)
                throw new ArgumentException(baseLocation);

            BaseLocation = baseLocation;
            Directory.CreateDirectory(BaseLocation);
        }

        /// <summary>
        /// Determines whether a file exists at the specified location.
        /// </summary>
        public bool HasFile(string relativePath) => HasFile(relativePath, null);

        /// <summary>
        /// Determines whether a file with the specified SHA-256 checksum exists at the specified location.
        /// </summary>
        public bool HasFile(string relativePath, string sha256checksum)
        {
            var path = Path.Join(BaseLocation, relativePath);
            return File.Exists(path) && ((sha256checksum != null) ? Cryptography.SHA256CheckSum(path) == sha256checksum : true);
        }

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// </summary>
        public byte[] GetFileBytes(string relativePath) => File.ReadAllBytes(Path.Join(BaseLocation, relativePath));

        /// <summary>
        /// Gets the a file's checksum and bytes.
        /// </summary>
        public (string Checksum, byte[] Bytes) GetFileChecksumAndBytes(string relativePath)
        {
            var path = Path.Join(BaseLocation, relativePath);
            return (Cryptography.SHA256CheckSum(path), File.ReadAllBytes(path));
        }

        /// <summary>
        /// Writes a file to disk.
        /// </summary>
        public void WriteFileBytes(string relativePath, byte[] bytes)
        {
            var dir = Path.Combine(BaseLocation, Path.Combine(relativePath.Split(Path.DirectorySeparatorChar)[..^1]));
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Join(BaseLocation, relativePath), bytes);
        }

        /// <summary>
        /// Uses a stream to write a file.
        /// </summary>
        public void WriteFile(string relativePath, Stream stream, int length)
        {
            var dir = Path.Combine(BaseLocation, Path.Combine(relativePath.Split(Path.DirectorySeparatorChar)[..^1]));
            Directory.CreateDirectory(dir);
            using (var fs = System.IO.File.Create(Path.Combine(BaseLocation, relativePath)))
            {
                byte[] buffer = null;
                var requestStream = stream;
                using (var reader = new BinaryReader(requestStream))
                {
                    buffer = reader.ReadBytes(length);
                }
                fs.Write(buffer, 0, buffer.Length);
                fs.Close();
            }
        }

        /// <summary>
        /// Cleans up a directory based on a list of differences.
        /// </summary>
        /// <exception cref="ArgumentNullException">removals</exception>
        /// <exception cref="ArgumentOutOfRangeException">All objects must be of type {IODifference.IODifferenceType.Removal}</exception>
        public void Cleanup(IEnumerable<IODifference> removals)
        {
            if (removals == null)
                throw new ArgumentNullException(nameof(removals));
            if (!removals.All(x => x.DifferenceType == IODifference.IODifferenceType.Removal))
                throw new ArgumentOutOfRangeException($"All objects must be of type {IODifference.IODifferenceType.Removal}");

            foreach (var removal in removals)
            {
                removal.Path = removal.Path.Replace('/', '\\').Replace("%5C", "\\").TrimStart('\\');
                var path = Path.Join(BaseLocation, removal.Path);
                switch (removal.Type) //TODO: Here, we should also remove corresponding files and directories from the removal list so we don't end up with too many unnecessary operations
                {
                    case IODifference.ObjectType.Directory:
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                        break;
                    case IODifference.ObjectType.File:
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        break;
                }
            }
        }
    }
}
