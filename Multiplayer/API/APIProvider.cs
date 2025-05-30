using MPAPI.Interfaces;
using Multiplayer.Components.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.API
{
    public class APIProvider : IMultiplayerAPI
    {
        public bool IsMultiplayerLoaded => true;
        
        public bool IsConnected => NetworkLifecycle.Instance.IsClientRunning || NetworkLifecycle.Instance.IsServerRunning;

        public bool IsHost => NetworkLifecycle.Instance.IsHost();

        public bool IsDedicatedServer => throw new NotImplementedException();

        public bool IsSinglePlayer => NetworkLifecycle.Instance.IsServerRunning && (NetworkLifecycle.Instance?.Server.IsSinglePlayer ?? false);
    }
}
