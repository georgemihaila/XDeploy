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
        private static Dictionary<string, List<Action<string>>> _updateAvailableActions = new Dictionary<string, List<Action<string>>>();
        private static List<Action<(string ApplicationID, bool Locked)>> _applicationStateChanged = new List<Action<(string ApplicationID, bool Locked)>>();

        /// <summary>
        /// Triggers an update signal for an application.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        public static void TriggerUpdateAvailable(string applicationID)
        {
            if (_updateAvailableActions.ContainsKey(applicationID))
            {
                foreach(var action in _updateAvailableActions[applicationID].ToList())
                {
                    action.Invoke(applicationID);
                }
            }
        }

        /// <summary>
        /// Registers an action to be called by the <see cref="TriggerUpdateAvailable(string)"/> method.
        /// </summary>
        /// <param name="applicationID">The application ID.</param>
        /// <param name="onUpdate">The action that will be called whenever an update is triggered. The action takes in the application ID.</param>
        public static void RegisterOnAppUpdateAvailable(string applicationID, Action<string> onUpdate)
        {
            if (!_updateAvailableActions.ContainsKey(applicationID))
            {
                _updateAvailableActions[applicationID] = new List<Action<string>>();
            }
            _updateAvailableActions[applicationID].Add(onUpdate);
        }

        /// <summary>
        /// Triggers a lock changed signal for an application.
        /// </summary>
        public static void TriggerApplicationLockedChanged(string ApplicationID, bool Locked)
        {
            foreach (var action in _applicationStateChanged.ToList())
            {
                action.Invoke((ApplicationID, Locked));
            }
        }

        /// <summary>
        /// Registers an action to be called by the <see cref="TriggerApplicationLockedChanged(string, bool)"/> method.
        /// </summary>
        /// <param name="onUpdate">The action that will be called whenever a lock change is triggered. The action takes in the application ID.</param>
        public static void RegisterApplicationLockedChanged(Action<(string ApplicationID, bool Locked)> onUpdate)
        {
            _applicationStateChanged.Add(onUpdate);
        }
    }
}
