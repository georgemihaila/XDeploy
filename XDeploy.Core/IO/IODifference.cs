using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Represents an IO difference.
    /// </summary>
    public class IODifference
    {
        public enum ObjectType { File, Directory }

        public enum IODifferenceType { Removal, Addition, Update }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        public ObjectType Type { get; set; }

        /// <summary>
        /// Gets or sets the difference type.
        /// </summary>
        public IODifferenceType DifferenceType { get; set; }

        /// <summary>
        /// Gets or sets the checksum, in case the current object represents a file.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => $"{Type} {DifferenceType}";
    }
}
