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

        public bool TryGetNetId<T>(T obj, out ushort netId) where T : class
        {
            return NetIdProvider.Instance.TryGetNetId<T>(obj, out netId);
        }

        public bool TryGetObjectFromNetId<T>(ushort netId, out T obj) where T : class
        {
            return NetIdProvider.Instance.TryGetObject<T>(netId, out obj);
        }
    }
}
