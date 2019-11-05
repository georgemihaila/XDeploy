namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a listener.
    /// </summary>
    public interface IListener
    {
        /// <summary>
        /// Starts listening.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening.
        /// </summary>
        void StopListening();
    }
}
