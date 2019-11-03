using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server
{
    public class StaticWebSocketsWorkaround
    {
        private static Dictionary<string, List<Action<string>>> _updateActions = new Dictionary<string, List<Action<string>>>();

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

        public static void RegisterOnAppUpdate(string id, Action<string> onUpdate)
        {
            if (!_updateActions.ContainsKey(id))
            {
                _updateActions[id] = new List<Action<string>>();
            }
            _updateActions[id].Add(onUpdate);
        }
    }
}
