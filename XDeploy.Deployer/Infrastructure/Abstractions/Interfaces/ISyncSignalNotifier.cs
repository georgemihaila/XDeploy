using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a synchronization signal notifier.
    /// </summary>
    public interface ISyncSignalNotifier : IListener, INotifySyncSignalReceived
    {

    }
}
