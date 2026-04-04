using DV.LocoRestoration;
using DV.ThingTypes;
using DV.Utils;
using JetBrains.Annotations;
using Multiplayer.Utils;

namespace Multiplayer.Components.Networking.Train;

public class NetworkRestorationWatcher : SingletonBehaviour<NetworkRestorationWatcher>
{

    public void AddController(LocoRestorationController controller)
    {
        if (controller == null)
            return;

        controller.StateChanged += HandleRestorationStateChange;

    }

    private static void HandleRestorationStateChange(LocoRestorationController controller, TrainCarLivery livery, LocoRestorationController.RestorationState newState)
    {
        ushort[] transportCars = null;

        switch (newState)
        {
            // Handle events that need comms to clients
            case LocoRestorationController.RestorationState.S5_PartOrdered:
                break;
            default:
                return; // No need to send updates for other states
        }

        var netId = controller.loco.GetNetId();
        NetworkLifecycle.Instance.Server.SendRestorationStateChange(netId, newState, transportCars);
    }

    [UsedImplicitly]
    public new static string AllowAutoCreate()
    {
        // Only create this component on the host
        if (NetworkLifecycle.Instance.IsHost())
            return $"[{nameof(NetworkRestorationWatcher)}]";

        return null;
    }
}
