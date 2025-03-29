using System;

namespace Multiplayer.Networking.Data;

public enum PitStopStationInteractionType : byte
{
    Reject,
    ResourceGrab,
    ResourceUngrab,
    ResourceUpdate,

    CarSelectorGrab,
    CarSelectorUngrab,
    CarSelection,

    FaucetGrab,
    FaucetUngrab,
    FaucetPosition,

    PayOrder,
    CancelOrder,
    ProcessOrder
}
