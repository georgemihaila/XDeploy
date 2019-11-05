namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application synchronization result.
    /// </summary>
    public class SynchronizationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationResult"/> class.
        /// </summary>
        public SynchronizationResult()
        {
            NewFiles = 0;
        }

        /// <summary>
        /// Gets or sets the number of deployed files.
        /// </summary>
        public int NewFiles { get; set; }

        public static SynchronizationResult operator +(SynchronizationResult a, SynchronizationResult b) => new SynchronizationResult()
        {
            NewFiles = a.NewFiles + b.NewFiles
        };

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => $"New: {NewFiles}";
    }
}
