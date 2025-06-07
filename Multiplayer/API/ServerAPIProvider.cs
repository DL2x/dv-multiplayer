using MPAPI.Interfaces;
using MPAPI.Interfaces.Packets;
using Multiplayer.Networking.Data;
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

        #region Packet API
        public void RegisterPacket<T>(ServerPacketHandler<T> handler) where T : class, IPacket, new()
        {
            server.RegisterExternalPacket<T>(handler);
        }
        public void RegisterSerializablePacket<T>(ServerPacketHandler<T> handler) where T : class, ISerializablePacket, new()
        {
            server.RegisterExternalSerializablePacket<T>(handler);
        }


        public void SendPacketToAll<T>(T packet, bool reliable = true, IPlayer excludePlayer = null) where T : class, IPacket, new()
        {
            server.SendExternalPacketToAll(packet, reliable, excludePlayer.Id);
        }

        public void SendSerializablePacketToAll<T>(T packet, bool reliable = true, IPlayer excludePlayer = null) where T : class, ISerializablePacket, new()
        {
            server.SendExternalSerializablePacketToAll(packet, reliable, excludePlayer.Id);
        }

        public void SendPacketToPlayer<T>(T packet, IPlayer player, bool reliable = true) where T : class, IPacket, new()
        {
            server.SendExternalPacketToPlayer(packet, player.Id, reliable);
        }

        public void SendSerializablePacketToPlayer<T>(T packet, IPlayer player, bool reliable = true) where T : class, ISerializablePacket, new()
        {
            server.SendExternalSerializablePacketToPlayer(packet, player.Id, reliable);
        }
        #endregion

        #region Server Util
        public float AnyPlayerSqrMag(GameObject item) => DvExtensions.AnyPlayerSqrMag(item);

        public float AnyPlayerSqrMag(Vector3 anchor) => DvExtensions.AnyPlayerSqrMag(anchor);
        #endregion

        #region Chat
        public void SendServerChatMessage(string message, IPlayer player = null)
        {
            ServerPlayer serverPlayer = player as ServerPlayer;
            ChatManager.ServerMessage(message, null, serverPlayer?.Peer);
        }

        public bool RegisterChatCommand(string commandLong, string commandShort, Func<string> helpMessage, Action<string[], IPlayer> callback)
        {
            //todo: create chat command registration system
            throw new NotImplementedException();
        }

        public void RegisterChatFilter(Func<string, IPlayer, bool> callback)
        {
            //todo: create chat filter system
            throw new NotImplementedException();
        }
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
