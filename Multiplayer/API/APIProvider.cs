using MPAPI.Interfaces;
using Multiplayer.Components.Networking;


namespace Multiplayer.API
{
    public class APIProvider : IMultiplayerAPI
    {
        public bool IsMultiplayerLoaded => true;
        
        public bool IsConnected => NetworkLifecycle.Instance.IsClientRunning || NetworkLifecycle.Instance.IsServerRunning;

        public bool IsHost => NetworkLifecycle.Instance.IsHost();

        public bool IsDedicatedServer => false; //feature not implemented

        public bool IsSinglePlayer => NetworkLifecycle.Instance.IsServerRunning && (NetworkLifecycle.Instance?.Server.IsSinglePlayer ?? false);
    }
}
