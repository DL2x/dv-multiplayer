using DV.Optimizers;
using HarmonyLib;
using Multiplayer.Components.Networking;
using UnityEngine;

namespace Multiplayer.Patches.Train;

[HarmonyPatch(typeof(ScriptStripperRuntime))]
public static class ScriptStripperRuntimePatch
{
    [HarmonyPatch(nameof(ScriptStripperRuntime.Strip))]
    [HarmonyPrefix]
    public static bool Strip(GameObject goToStrip)
    {
        if(!NetworkLifecycle.Instance.IsHost())
            return true;

        var trainCar = TrainCar.Resolve(goToStrip);

        if (trainCar == null)
            return true;

        MonoBehaviour[] scripts = goToStrip.GetComponentsInChildren<MonoBehaviour>();
        Joint[] joints = goToStrip.GetComponentsInChildren<Joint>();
        Rigidbody[] rigidBodies = goToStrip.GetComponentsInChildren<Rigidbody>();
        Collider[] colliders = goToStrip.GetComponentsInChildren<Collider>();

        // Dedicated server has no display or audio: these are built-in Unity components (NOT
        // MonoBehaviours), so the script loop below never touches them and they keep doing
        // per-frame work (animation evaluation, particle simulation, audio) for every vehicle.
        Animator[] animators = goToStrip.GetComponentsInChildren<Animator>(true);
        ParticleSystem[] particleSystems = goToStrip.GetComponentsInChildren<ParticleSystem>(true);
        AudioSource[] audioSources = goToStrip.GetComponentsInChildren<AudioSource>(true);

        for (int i = 0; i < animators.Length; i++)
            Object.Destroy(animators[i]);

        for (int i = 0; i < particleSystems.Length; i++)
            Object.Destroy(particleSystems[i]);

        for (int i = 0; i < audioSources.Length; i++)
            Object.Destroy(audioSources[i]);

        for (int i = 0; i < joints.Length; i++)
        {
            Object.Destroy(joints[i]);
        }

        for (int i = 0; i < rigidBodies.Length; i++)
        {
            Object.Destroy(rigidBodies[i]);
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if(!colliders[i].TryGetComponent<LocoResourceReceiver>(out _))
                Object.Destroy(colliders[i]);
            //else
            //{
            //    Multiplayer.LogDebug(() => $"ScriptStripperRuntimePatch.Strip() Keeping collider {colliders[i].gameObject.GetPath()} for {trainCar.ID}, has LocoResourceReceiver component.");
            //}
        }

        for (int i = 0; i < scripts.Length; i++)
        {
            if (!scripts[i].GetType().Equals(typeof(LocoResourceReceiver)))
                Object.Destroy(scripts[i]);
            //else
            //{
            //    Multiplayer.LogDebug(() => $"ScriptStripperRuntimePatch.Strip() Keeping script {scripts[i].gameObject.GetPath()} for {trainCar.ID}, is LocoResourceReceiver component.");
            //}
        }

        return false;
    }
}
