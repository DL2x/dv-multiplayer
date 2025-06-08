using MPAPI.Interfaces;
using Multiplayer.Components.Networking;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.TransportLayers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Multiplayer.Networking.Managers.Server;

public class ChatManager
{
    public const string COMMAND_SERVER = "server";
    public const string COMMAND_SERVER_SHORT = "s";
    public const string COMMAND_WHISPER = "whisper";
    public const string COMMAND_WHISPER_SHORT = "w";
    public const string COMMAND_HELP_SHORT = "?";
    public const string COMMAND_HELP = "help";
    public const string COMMAND_LOG = "log";
    public const string COMMAND_LOG_SHORT = "l";
    public const string COMMAND_KICK = "kick";
   
    public const string MESSAGE_COLOUR_SERVER = "9CDCFE";
    public const string MESSAGE_COLOUR_HELP = "00FF00";

    private readonly Dictionary<string, Action<string, IPlayer>> _registeredCommands = [];
    private readonly Dictionary<string, Func<string>> _registeredHelpMessages = [];
    private readonly List<Func<string, IPlayer, bool>> _chatFilters = [];

    public ChatManager()
    {
        RegisterBuiltInCommands();
    }

    private void RegisterBuiltInCommands()
    {
        RegisterChatCommand
        (
            COMMAND_SERVER,
            COMMAND_SERVER_SHORT,
            () => $"\r\n\r\n\t{Locale.CHAT_HELP_SERVER_MSG}" +
                    $"\r\n\t\t/{COMMAND_SERVER} <{Locale.CHAT_HELP_MSG}>" +
                    $"\r\n\t\t/{COMMAND_SERVER_SHORT} <{Locale.CHAT_HELP_MSG}>",
            (message, sender) => ServerMessage(message, sender, null)
        );

        RegisterChatCommand
        (
            COMMAND_WHISPER,
            COMMAND_WHISPER_SHORT,
            ()=> $"\r\n\r\n\t{Locale.CHAT_HELP_WHISPER_MSG}" +
                    $"\r\n\t\t/{COMMAND_WHISPER} <{Locale.CHAT_HELP_PLAYER_NAME}> <{Locale.CHAT_HELP_MSG}>" +
                    $"\r\n\t\t/{COMMAND_WHISPER_SHORT} <{Locale.CHAT_HELP_PLAYER_NAME}> <{Locale.CHAT_HELP_MSG}>",
            WhisperMessage
        );

        RegisterChatCommand
        (
            COMMAND_HELP,
            COMMAND_HELP_SHORT,
            ()=> $"\r\n\r\n\t{Locale.CHAT_HELP_HELP}" +
                    $"\r\n\t\t/{COMMAND_HELP}" +
                    $"\r\n\t\t/{COMMAND_HELP_SHORT}",
            HelpMessage
        );

        RegisterChatCommand
        (
            COMMAND_KICK,
            null,
            () => $"\r\n\r\n\tKick a player from the server (must be host)" +
                    $"\r\n\t\t/{COMMAND_KICK}",
            KickMessage
        );

#if DEBUG
        RegisterChatCommand
        (
            COMMAND_LOG,
            COMMAND_LOG_SHORT,
            null,
            (args, sender) =>
                Multiplayer.specLog = !Multiplayer.specLog);
#endif
    }

    public bool RegisterChatCommand(string commandLong, string commandShort, Func<string> helpMessage, Action<string, IPlayer> callback)
    {
        if (string.IsNullOrEmpty(commandLong) || callback == null)
            return false;

        if (_registeredCommands.ContainsKey(commandLong.ToLower()) ||
            (!string.IsNullOrEmpty(commandShort) && _registeredCommands.ContainsKey(commandShort.ToLower())))
            return false;

        _registeredCommands[commandLong.ToLower()] = callback;

        if (!string.IsNullOrEmpty(commandShort) && !_registeredCommands.ContainsKey(commandShort.ToLower()))
        {
            _registeredCommands[commandShort.ToLower()] = callback;
        }

        if (helpMessage != null)
        {
            _registeredHelpMessages[commandLong.ToLower()] = helpMessage;
        }

        return true;
    }

    public void RegisterChatFilter(Func<string, IPlayer, bool> callback)
    {
        if (callback != null)
        {
            _chatFilters.Add(callback);
        }
    }

    public void ProcessMessage(string message, IPlayer sender)
    {

        if (string.IsNullOrEmpty(message))
            return;

        //Check if we have a command
        if (message.StartsWith("/"))
        {
            string[] messageParams = message.Substring(1).Split(' ');
            string command = messageParams[0].ToLower();

            //check registered commands
            if (!string.IsNullOrEmpty(command) && _registeredCommands.TryGetValue(command, out var commandCallback))
            {
                //remove the command, leading slash and trailing space
                message = message.Substring(command.Length + 2);
                commandCallback(message, sender);

                return;
            }
        }

        //not a server command, process as normal message
        ProcessChatMessage(message, sender);
    }

    private void ProcessChatMessage(string message, IPlayer sender)
    {
        if (sender is not ServerPlayer player)
            return;

        //clean up the message to stop format injection
        message = Regex.Replace(message, "</noparse>", string.Empty, RegexOptions.IgnoreCase);

        //call each filter until either a filter returns false or all filters have been called
        foreach (var filter in _chatFilters)
        {
            if (!filter(message, sender))
                return;
        }

        message = $"<alpha=#50>{sender.Username}:</color> <noparse>{message}</noparse>";
        NetworkLifecycle.Instance.Server.SendChat(message, player.Peer);
    }

    public void ServerMessage(string message, IPlayer sender, IPlayer exclude = null)
    {
        ServerPlayer senderPlayer = null;
        ITransportPeer excludePeer = null;

        if (sender != null)
        {
            if(sender is not ServerPlayer sp)
                return;

            senderPlayer = sp;
        }

        if (exclude != null)
        {
            if (exclude is not ServerPlayer ep)
                return;
            excludePeer = ep.Peer;
        }
       
        //If user is not the host, we should ignore - will require changes for dedicated server
        if (senderPlayer != null && !NetworkLifecycle.Instance.IsHost(senderPlayer.Peer))
            return;

        message = $"<color=#{MESSAGE_COLOUR_SERVER}>{message}</color>";
        NetworkLifecycle.Instance.Server.SendChat(message, excludePeer);
    }

    private void WhisperMessage(string message, IPlayer sender)
    {
        Multiplayer.LogDebug(() => $"Whispering: \"{message}\", sender: {sender?.Username}, senderID: {sender?.Id}");

        if (sender == null || sender is not ServerPlayer senderPlayer)
            return;

        if (string.IsNullOrEmpty(message))
            return;

        string[] parts = message.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            return;

        string recipientName = parts[0];
        string whisperMessage = parts[1];


        Multiplayer.LogDebug(()=>$"Whispering parse 1: \"{message}\", sender: {sender?.Username}, senderID: {sender?.Id}, peerName: {recipientName}");

        //look up the peer ID
        ITransportPeer recipient = NetPeerFromName(recipientName);
        if(recipient == null)
        {
            Multiplayer.LogDebug(() => $"Whispering failed: \"{message}\", sender: {sender?.Username}, senderID: {sender?.Id}, peerName: {recipientName}");

            whisperMessage = $"<color=#{MESSAGE_COLOUR_SERVER}>{recipientName} not found - you're whispering into the void!</color>"; //todo: add translation
            NetworkLifecycle.Instance.Server.SendWhisper(whisperMessage, senderPlayer.Peer);
            return;
        }

        Multiplayer.LogDebug(() => $"Whispering parse 2: \"{message}\", sender: {sender?.Username}, senderID: {sender?.Id}, peerName: {recipientName}, peerID: {recipient?.Id}");

        //clean up the message to stop format injection
        whisperMessage = Regex.Replace(whisperMessage, "</noparse>", string.Empty, RegexOptions.IgnoreCase);

        whisperMessage = "<i><alpha=#50>" + sender.Username + ":</color> <noparse>" + whisperMessage + "</noparse></i>";

        NetworkLifecycle.Instance.Server.SendWhisper(whisperMessage, recipient);
    }

    public void KickMessage(string message, IPlayer sender)
    {
        ITransportPeer player;
        string playerName;

        //If user is not the host, we should ignore - will require changes for dedicated server
        if (sender == null || sender is not ServerPlayer senderPlayer || !NetworkLifecycle.Instance.IsHost(senderPlayer.Peer))
            return;

        playerName = message.Split(' ')[0];
        if (string.IsNullOrEmpty(playerName))
            return;

        player = NetPeerFromName(playerName);

        if (player == null || NetworkLifecycle.Instance.IsHost(player))
        {
            message = $"<color=#{MESSAGE_COLOUR_SERVER}>Unable to kick {playerName}</color>"; //todo: translate
        }
        else
        {
            message = $"<color=#{MESSAGE_COLOUR_SERVER}>{playerName} was kicked</color>"; //todo: translate

            NetworkLifecycle.Instance.Server.KickPlayer(player);
        }

        NetworkLifecycle.Instance.Server.SendWhisper(message, senderPlayer.Peer);
    }

    private void HelpMessage(string _, IPlayer peer)
    {
        if (peer == null || peer is not ServerPlayer player)
            return;

        StringBuilder sb = new($"<color=#{MESSAGE_COLOUR_HELP}>{Locale.CHAT_HELP_AVAILABLE}");

        foreach (var helpMessage in _registeredHelpMessages)
            sb.AppendLine(helpMessage.Value?.Invoke());

        sb.AppendLine("</color>");

        /*
         * $"<color=#{MESSAGE_COLOUR_HELP}>Available commands:" +

                        "\r\n\r\n\tSend a message as the server (host only)" +
                        "\r\n\t\t/server <message>" +
                        "\r\n\t\t/s <message>" +

                        "\r\n\r\n\tWhisper to a player" +
                        "\r\n\t\t/whisper <PlayerName> <message>" +
                        "\r\n\t\t/w <PlayerName> <message>" +

                        "\r\n\r\n\tDisplay this help message" +
                        "\r\n\t\t/help" +
                        "\r\n\t\t/?" +

                        "</color>";
        */
        NetworkLifecycle.Instance.Server.SendWhisper(sb.ToString(), player.Peer);
    }


    private ITransportPeer NetPeerFromName(string peerName)
    {
     
        if(peerName == null || peerName == string.Empty)
            return null;

        ServerPlayer player = NetworkLifecycle.Instance.Server.ServerPlayers.Where(p => p.Username == peerName).FirstOrDefault();
        if (player == null)
            return null;

        if(NetworkLifecycle.Instance.Server.TryGetPeer(player.Id, out ITransportPeer peer))
        {
            return peer;
        }

        return null;

    }
}
