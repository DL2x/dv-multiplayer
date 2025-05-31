using MPAPI.Interfaces;
using Multiplayer.Networking.Managers.Server;
using Multiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Multiplayer.API
{
    public class ServerAPIProvider : IServer
    {
        private readonly NetworkServer server;

        public event Action<IPlayer> OnPlayerConnected;
        public event Action<IPlayer> OnPlayerDisconnected;

        #region Server Properties

        public int PlayerCount => server.PlayerCount;

        public IReadOnlyCollection<IPlayer> Players =>
            server.ServerPlayers
                .Select(p => new ServerPlayerWrapper(p))
                .Cast<IPlayer>()
                .ToList();

        #endregion

        #region Server Util
        public float AnyPlayerSqrMag(GameObject item) => DvExtensions.AnyPlayerSqrMag(item);

        public float AnyPlayerSqrMag(Vector3 anchor) => DvExtensions.AnyPlayerSqrMag(anchor);
        #endregion


        #region Class Helpers
        internal ServerAPIProvider(NetworkServer serverInstance)
        {
            this.server = serverInstance;

            server.PlayerConnected += OnPlayerConnectedInternal;
            server.PlayerDisconnected += OnPlayerDisconnectedInternal;
        }

        internal void Dispose()
        {
            server.PlayerConnected -= OnPlayerConnectedInternal;
            server.PlayerDisconnected -= OnPlayerDisconnectedInternal;
        }

        private void OnPlayerConnectedInternal(Networking.Data.ServerPlayer serverPlayer)
        {
            OnPlayerConnected?.Invoke(new ServerPlayerWrapper(serverPlayer));
        }

        private void OnPlayerDisconnectedInternal(Networking.Data.ServerPlayer serverPlayer)
        {
            OnPlayerDisconnected?.Invoke(new ServerPlayerWrapper(serverPlayer));
        }
        #endregion
    }
}
