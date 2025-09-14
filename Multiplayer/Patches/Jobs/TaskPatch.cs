using DV.Logic.Job;
using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.Jobs;

namespace Multiplayer.Patches.Jobs;

[HarmonyPatch(typeof(Task))]
public static class TaskPatch
{
    [HarmonyPatch(nameof(Task.SetState))]
    [HarmonyPrefix]
    public static void SetStatePrefix(Task __instance, TaskState newState)
    {
        if (!NetworkLifecycle.Instance.IsHost())
            return;

        if (newState == TaskState.InProgress)
            return;

        if (NetworkedJob.TryGetFromJob(__instance.Job, out var netJob) && netJob != null)
        {
            var taskNetId = netJob.GetTaskNetId(__instance);

            if (taskNetId == 0)
            {
                Multiplayer.LogError($"Task.SetState() could not find task index for jobId: {__instance.Job.ID}, taskType: {__instance.InstanceTaskType}");
                return;
            }
            NetworkLifecycle.Instance.Server.SendTaskUpdate(netJob.NetId, taskNetId, newState, __instance.taskStartTime, __instance.taskFinishTime);
        }
      
    }

    [HarmonyPatch(nameof(Task.SetJobBelonging))]
    [HarmonyPostfix]
    public static void SetJobBelongingPostfix(Task __instance)
    {
        if (!NetworkLifecycle.Instance.IsHost())
            return;

        NetworkedJob.EnqueTask(__instance);
    }
}
