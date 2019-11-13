using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server
{
    /// <summary>
    /// Used for registering clients and triggering WebSockets events.
    /// </summary>
    public static class WebsocketsIOC
    {
        private static Dictionary<string, List<Action<string>>> _updateActions = new Dictionary<string, List<Action<string>>>();

        /// <summary>
        /// Triggers an update signal for an application.
        /// </summary>
        /// <param name="id">The application ID.</param>
        public static void TriggerUpdate(string id)
        {
            if (_updateActions.ContainsKey(id))
            {
                foreach(var action in _updateActions[id].ToList())
                {
                    action.Invoke(id);
                }
            }
        }

        /// <summary>
        /// Registers an action to be called by the <see cref="TriggerUpdate(string)"/> method.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        /// <param name="onUpdate">The action that will be called whenever an update is triggered. The action takes in the application ID.</param>
        public static void RegisterOnAppUpdate(string applicationID, Action<string> onUpdate)
        {
            if (!_updateActions.ContainsKey(applicationID))
            {
                _updateActions[applicationID] = new List<Action<string>>();
            }
            _updateActions[applicationID].Add(onUpdate);
        }
    }
}
