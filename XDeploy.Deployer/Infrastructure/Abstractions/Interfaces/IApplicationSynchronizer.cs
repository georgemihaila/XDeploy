namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application synchronizer.
    /// </summary>
    public interface IApplicationSynchronizer : ISynchronizer
    {
        /// <summary>
        /// Gets the application ID.
        /// </summary>
        string ApplicationID { get; }
    }
}
