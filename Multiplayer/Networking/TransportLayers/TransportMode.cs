using System;

namespace Multiplayer.Networking.TransportLayers;

/// <summary>
/// Selects which transport layer the multiplayer stack should use.
/// Steamworks requires Steam to be running/initialized.
/// LiteNetLib is plain UDP and works without Steam (e.g. Oculus/Meta builds).
/// </summary>
public enum TransportMode
{
    Auto = 0,
    Steamworks = 1,
    LiteNetLib = 2,
}

