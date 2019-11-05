using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an update manager.
    /// </summary>
    /// <seealso cref="XDeploy.Client.Infrastructure.ISynchronizer" />
    /// <seealso cref="XDeploy.Client.Infrastructure.IListener" />
    public interface IUpdateManager : ISynchronizer, IListener
    {

    }
}
