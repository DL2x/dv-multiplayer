using MPAPI.Interfaces;
using Multiplayer.Networking.Managers.Server;
using Multiplayer.Utils;
using System;
using System.Collections.Generic;
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

        //public IReadOnlyCollection<IPlayer> Players => server.ServerPlayers;

        #endregion

        #region Server Util
        public float AnyPlayerSqrMag(GameObject item) => DvExtensions.AnyPlayerSqrMag(item);

        public float AnyPlayerSqrMag(Vector3 anchor) => DvExtensions.AnyPlayerSqrMag(anchor);
        #endregion


        #region Class Helpers
        internal ServerAPIProvider(NetworkServer serverInstance)
        {
            this.server = serverInstance;

            server.PlayerConnected += OnPlayerConnected;
            server.PlayerDisconnected += OnPlayerDisconnected;
        }

        internal void Dispose()
        {
            server.PlayerConnected -= OnPlayerConnected;
            server.PlayerDisconnected -= OnPlayerDisconnected;
        }
        #endregion
    }
}
