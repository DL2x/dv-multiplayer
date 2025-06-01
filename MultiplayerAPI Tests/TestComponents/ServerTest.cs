using MPAPI;
using MPAPI.Interfaces;
using MultiplayerAPITest.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerAPITest.TestComponents;


internal class ServerTest : MonoBehaviour
{
    const string LogPrefix = "ServerTest";

    IServer server;

    protected void Awake()
    {
        server = MultiplayerAPI.Server;

        //subscribe to packets
        Subscribe();
    }

    protected void Start() { }

    protected void Update() { }

    protected void OnDestroy() { }


    //Setup subscriptions for the packets we want to/expect to receive
    private void Subscribe()
    {
        server.RegisterPacket<SimpleModPacket>(OnTestSimpleModPacket);
        server.RegisterPacket<SimplePacketWithNetId>(OnSimplePacketWithNetId);
        server.RegisterSerializablePacket<ComplexModPacket>(OnTestComplexModPacket);
    }


    #region Packet Callbacks

    //method called when a `TestSimplePacket` packet is received
    private void OnTestSimpleModPacket(SimpleModPacket packet, IPlayer player)
    {

        Log($"Received {packet.GetType()} from player: {player.Username}");

        Log($"CarId: {packet.CarId}, Position: {packet.Position}");
        
    }

    //method called when a `TestSimplePacket` packet is received
    private void OnSimplePacketWithNetId(SimplePacketWithNetId packet, IPlayer player)
    {

        Log($"Received {packet.GetType()} from player: {player.Username}");

        Log($"CarId: {packet.CarId}, Position: {packet.Position}");

    }

    //method called when a `TestComplexModPacket` packet is received
    private void OnTestComplexModPacket(ComplexModPacket packet, IPlayer player)
    {

        Log($"Received {packet.GetType()} from player: {player.Username}");

        StringBuilder sb = new("\r\nPacket Data");

        foreach (var kvp in packet.CarToPositionMap)
            sb.AppendLine($"CarId: {kvp.Key}, Position: {kvp.Value}");

        Log(sb.ToString());
    }
    #endregion

    #region Logging

    public void LogDebug(Func<object> resolver)
    {
        MultiplayerAPITest.LogDebug(() => $"{LogPrefix} {resolver.Invoke()}");
    }

    public void Log(object msg)
    {
        MultiplayerAPITest.Log($"{LogPrefix} {msg}");
    }

    public void LogWarning(object msg)
    {
        MultiplayerAPITest.LogWarning($"{LogPrefix} {msg}");
    }

    public void LogError(object msg)
    {
        MultiplayerAPITest.LogError($"{LogPrefix} {msg}");
    }

    #endregion
}
