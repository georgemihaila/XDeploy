using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Describes basic information for a file.
    /// </summary>
    public class FileInfo : IRelativizeable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileInfo"/> class.
        /// </summary>
        public FileInfo()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileInfo"/> class.
        /// </summary>
        public FileInfo(string filename)
        {
            Name = filename;
            LastModified = System.IO.File.GetLastWriteTime(filename);
            SHA256CheckSum = Cryptography.SHA256CheckSum(filename);
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 checksum of the file.
        /// </summary>
        public string SHA256CheckSum { get; set; }

        /// <summary>
        /// Gets or sets the time the file was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Relativizes the current object
        /// </summary>
        public void Relativize(string absolute)
        {
            Name = Name.Replace(absolute, string.Empty);
        }
    }
}
