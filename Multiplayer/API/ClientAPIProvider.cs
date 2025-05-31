using MPAPI.Interfaces;
using Multiplayer.Networking.Managers.Client;
using Multiplayer.Networking.Managers.Server;
using Multiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Multiplayer.API
{
    public class ClientAPIProvider : IClient
    {
        private readonly NetworkClient client;

        public event Action<IPlayer> OnPlayerConnected;
        public event Action<IPlayer> OnPlayerDisconnected;

        #region Client Properties
        public IReadOnlyCollection<IPlayer> Players =>
            client.ClientPlayerManager.Players
                .Select(p => new ClientPlayerWrapper(p))
                .Cast<IPlayer>()
                .ToList();

            
        public bool IsConnected => throw new NotImplementedException();

        public int Ping => client.Ping;

        #endregion

        #region Class Helpers
        internal ClientAPIProvider(NetworkClient clientInstance)
        {
            this.client = clientInstance;

            client.ClientPlayerManager.OnPlayerConnected += OnPlayerConnectedInternal;
            client.ClientPlayerManager.OnPlayerDisconnected += OnPlayerDisconnectedInternal;
        }

        internal void Dispose()
        {
            client.ClientPlayerManager.OnPlayerConnected -= OnPlayerConnectedInternal;
            client.ClientPlayerManager.OnPlayerDisconnected -= OnPlayerDisconnectedInternal;
        }

        public IPlayer GetPlayer(byte id)
        {
            if (client.ClientPlayerManager.TryGetPlayer(id, out var player))
                return new ClientPlayerWrapper(player);
            return null;
        }

        private void OnPlayerConnectedInternal(Components.Networking.Player.NetworkedPlayer networkedPlayer)
        {
            OnPlayerConnected?.Invoke(new ClientPlayerWrapper(networkedPlayer));
        }

        private void OnPlayerDisconnectedInternal(Components.Networking.Player.NetworkedPlayer networkedPlayer)
        {
            OnPlayerDisconnected?.Invoke(new ClientPlayerWrapper(networkedPlayer));
        }
        #endregion
    }
}
