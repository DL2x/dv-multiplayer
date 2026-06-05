using DV;
using DV.Common;
using DV.UI;
using DV.UIFramework;
using Multiplayer.API;
using Multiplayer.Components.Networking;
using Multiplayer.Networking.Data;
using Multiplayer.Patches.MainMenu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Multiplayer.Components.DedicatedServer;

/// <summary>
/// Minimal dedicated-server bootstrap: when the game is launched headless/dedicated,
/// load the last played save and host it via direct IP transport.
/// </summary>
public class DedicatedServerBootstrap : MonoBehaviour
{
    private const int DEFAULT_PORT = 7777;
    private const int MIN_PORT = 1024;
    private const int MAX_PORT = 49151;
    private const int MIN_PLAYERS = 2;
    private const int MAX_PLAYERS = 20;

    private bool started;

    public static void CreateIfNeeded()
    {
        if (RuntimeConfiguration.RuntimeType != MultiplayerRuntimeType.Dedicated)
            return;

        Multiplayer.Log("Dedicated server mode requested; creating bootstrap.");

        if (FindObjectOfType<DedicatedServerBootstrap>() != null)
            return;

        NetworkLifecycle.Instance.gameObject.AddComponent<DedicatedServerBootstrap>();
    }

    private void Start()
    {
        StartCoroutine(StartLastWorldWhenMenuIsReady());
    }

    private IEnumerator StartLastWorldWhenMenuIsReady()
    {
        Multiplayer.Log("Dedicated server bootstrap waiting for the main menu/save providers...");
        // In batch mode the main menu still needs to initialise the profile/save providers first.
        while (!started && (MainMenuControllerPatch.MainMenuControllerInstance == null || MainMenuControllerPatch.MenuProvider == null))
            yield return null;

        if (started)
            yield break;

        Multiplayer.Log("Dedicated server bootstrap menu/save providers are ready.");
        started = true;

        try
        {
            StartLastWorldAsDirectIpHost();
        }
        catch (Exception ex)
        {
            Multiplayer.LogException("Dedicated server bootstrap failed:", ex);
        }
    }

    private void StartLastWorldAsDirectIpHost()
    {
        Multiplayer.Settings.HostTransportMode = NetworkTransportMode.Direct;
        Multiplayer.Settings.Port = SanitizePort(Multiplayer.Settings.Port);
        Multiplayer.Settings.MaxPlayers = Mathf.Clamp(Multiplayer.Settings.MaxPlayers, MIN_PLAYERS, MAX_PLAYERS);

        object menuProvider = MainMenuControllerPatch.MenuProvider;
        AUserProfileProvider userProvider = ResolveUserProvider(menuProvider);
        AScenarioProvider scenarioProvider = ResolveScenarioProvider(menuProvider);
        ISaveGame saveGame = ResolveLastSave(menuProvider, userProvider);

        if (saveGame == null)
        {
            Multiplayer.LogError("Dedicated server bootstrap could not find a last played save game.");
            return;
        }

        ConfigureServerData(saveGame, userProvider);

        NetworkLifecycle.Instance.IsSinglePlayer = false;

        LauncherController launcher = FindObjectOfType<LauncherController>();
        if (launcher == null)
        {
            Multiplayer.LogError("Dedicated server bootstrap could not find LauncherController.");
            return;
        }

        MethodInfo setData = typeof(LauncherController).GetMethod(
            "SetData",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(ISaveGame), typeof(AUserProfileProvider), typeof(AScenarioProvider), typeof(LauncherController.UpdateRequest) },
            null);

        if (setData == null)
        {
            Multiplayer.LogError("Dedicated server bootstrap could not find LauncherController.SetData(ISaveGame, ...).");
            return;
        }

        setData.Invoke(launcher, new object[] { saveGame, userProvider, scenarioProvider, null });

        MethodInfo run = typeof(LauncherController).GetMethod("OnRunClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        if (run == null)
        {
            Multiplayer.LogError("Dedicated server bootstrap could not find LauncherController.OnRunClicked().");
            return;
        }

        Multiplayer.Log($"Dedicated server starting last world '{GetSafeName(saveGame)}' on 0.0.0.0:{Multiplayer.Settings.Port} using direct IP hosting.");
        run.Invoke(launcher, null);
    }

    private static void ConfigureServerData(ISaveGame saveGame, AUserProfileProvider userProvider)
    {
        LobbyServerData serverData = new()
        {
            port = SanitizePort(Multiplayer.Settings.Port),
            Name = string.IsNullOrWhiteSpace(Multiplayer.Settings.ServerName) ? "Dedicated Server" : Multiplayer.Settings.ServerName.Trim(),
            HasPassword = !string.IsNullOrEmpty(Multiplayer.Settings.Password),
            Visibility = Multiplayer.Settings.Visibility,
            CurrentPlayers = 0,
            MaxPlayers = Mathf.Clamp(Multiplayer.Settings.MaxPlayers, MIN_PLAYERS, MAX_PLAYERS),
            GameMode = 0,
            Difficulty = 0,
            TimePassed = "N/A",
            RequiredMods = ModCompatibilityManager.Instance.GetLocalMods() ?? Array.Empty<ModInfo>(),
            GameVersion = MainMenuControllerPatch.MenuProvider?.BuildVersionString ?? string.Empty,
            MultiplayerVersion = Multiplayer.Ver,
            ServerDetails = Multiplayer.Settings.Details ?? string.Empty,
            TransportMode = NetworkTransportMode.Direct,
            // The public lobby server predates the Dedicated runtime value.
            // Advertise the store runtime while keeping RuntimeConfiguration.RuntimeType
            // as Dedicated internally, otherwise registration can be rejected as
            // "Invalid server information".
            RuntimeType = GetAdvertisedRuntimeType(),
        };

        if (userProvider != null)
        {
            try
            {
                ISaveGameplayInfo saveGameplayInfo = userProvider.GetSaveGameplayInfo(saveGame);
                if (saveGameplayInfo != null && !saveGameplayInfo.IsCorrupt)
                {
                    serverData.TimePassed = saveGameplayInfo.InGameDate != DateTime.MinValue
                        ? saveGameplayInfo.InGameTimePassed.ToString("d\\d\\ hh\\h\\ mm\\m\\ ss\\s")
                        : "N/A";
                    serverData.Difficulty = LobbyServerData.GetDifficultyFromString(userProvider.GetSessionDifficulty(saveGame.ParentSession).Name);
                    serverData.GameMode = LobbyServerData.GetGameModeFromString(saveGame.GameMode);
                }
            }
            catch (Exception ex)
            {
                Multiplayer.LogWarning($"Dedicated server bootstrap could not read save metadata: {ex}");
            }
        }

        Multiplayer.Settings.ServerName = serverData.Name;
        Multiplayer.Settings.HostTransportMode = NetworkTransportMode.Direct;
        Multiplayer.Settings.Port = serverData.port;
        Multiplayer.Settings.MaxPlayers = serverData.MaxPlayers;

        NetworkLifecycle.Instance.serverData = serverData;
    }


    private static MultiplayerRuntimeType GetAdvertisedRuntimeType()
    {
        string buildDestination = RuntimeConfiguration.BuildDestination;

        if (buildDestination.Contains("oculus"))
            return MultiplayerRuntimeType.Oculus;

        return MultiplayerRuntimeType.Steam;
    }

    private static ISaveGame ResolveLastSave(object menuProvider, AUserProfileProvider userProvider)
    {
        List<ISaveGame> candidates = new();

        AddSaveCandidate(candidates, InvokeAny(menuProvider, "GetLastPlayedSave"));
        AddSaveCandidate(candidates, InvokeAny(menuProvider, "GetLastPlayedCareerSave"));
        AddSaveCandidate(candidates, InvokeAny(menuProvider, "GetLastPlayedFreeRoamSave"));

        AddSaveCandidate(candidates, InvokeAny(userProvider, "GetLastPlayedSave"));
        AddSaveCandidate(candidates, InvokeAny(userProvider, "GetLastPlayedCareerSave"));
        AddSaveCandidate(candidates, InvokeAny(userProvider, "GetLastPlayedFreeRoamSave"));

        foreach (string gameMode in new[] { "Career", "FreeRoam" })
        {
            AddSaveCandidate(candidates, InvokeAny(menuProvider, "GetLastPlayedSaveForGameMode", gameMode));
            AddSaveCandidate(candidates, InvokeAny(userProvider, "GetLastPlayedSaveForGameMode", gameMode));
        }

        foreach (object session in ResolveSessions(menuProvider, userProvider))
        {
            AddSaveCandidate(candidates, InvokeAny(session, "GetLastPlayedSave"));
            AddSaveCandidate(candidates, GetMember(session, "LastPlayedSave"));
        }

        ISaveGame first = candidates.FirstOrDefault();
        if (first == null)
            return null;

        return candidates
            .Distinct()
            .OrderByDescending(GetLastWriteTime)
            .FirstOrDefault() ?? first;
    }

    private static IEnumerable<object> ResolveSessions(params object[] roots)
    {
        foreach (object root in roots.Where(r => r != null))
        {
            foreach (string method in new[] { "GetLastPlayedSession", "GetLastPlayedCareerSession", "GetLastPlayedFreeRoamSession" })
            {
                object session = InvokeAny(root, method);
                if (session != null)
                    yield return session;
            }

            foreach (string gameMode in new[] { "Career", "FreeRoam" })
            {
                object session = InvokeAny(root, "GetLastPlayedSessionForGameMode", gameMode);
                if (session != null)
                    yield return session;
            }

            object currentSession = GetMember(root, "CurrentSession");
            if (currentSession != null)
                yield return currentSession;
        }
    }

    private static AUserProfileProvider ResolveUserProvider(object menuProvider)
    {
        return GetMember(menuProvider, "UserProfileProvider") as AUserProfileProvider
            ?? GetMember(menuProvider, "userProfileProvider") as AUserProfileProvider
            ?? FindObjectOfType<SaveLoadController>()?.GetType().GetField("userProfileProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(FindObjectOfType<SaveLoadController>()) as AUserProfileProvider;
    }

    private static AScenarioProvider ResolveScenarioProvider(object menuProvider)
    {
        return GetMember(menuProvider, "ScenarioProvider") as AScenarioProvider
            ?? GetMember(menuProvider, "scenarioProvider") as AScenarioProvider
            ?? FindObjectOfType<SaveLoadController>()?.GetType().GetField("scenarioProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(FindObjectOfType<SaveLoadController>()) as AScenarioProvider;
    }

    private static void AddSaveCandidate(ICollection<ISaveGame> candidates, object value)
    {
        if (value is ISaveGame saveGame)
            candidates.Add(saveGame);
    }

    private static object InvokeAny(object target, string methodName, params object[] args)
    {
        if (target == null)
            return null;

        Type[] argTypes = args.Select(a => a?.GetType() ?? typeof(object)).ToArray();
        MethodInfo method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == methodName && ParametersMatch(m.GetParameters(), argTypes));

        if (method == null)
            return null;

        try
        {
            return method.Invoke(target, args);
        }
        catch (Exception ex)
        {
            Multiplayer.LogDebug(() => $"Dedicated server bootstrap: {target.GetType().Name}.{methodName} failed: {ex.Message}");
            return null;
        }
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, Type[] argTypes)
    {
        if (parameters.Length != argTypes.Length)
            return false;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (argTypes[i] == typeof(object))
                continue;
            if (!parameters[i].ParameterType.IsAssignableFrom(argTypes[i]))
                return false;
        }

        return true;
    }

    private static object GetMember(object target, string name)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null)
            return property.GetValue(target, null);

        FieldInfo field = type.GetField(name, flags);
        return field?.GetValue(target);
    }

    private static DateTime GetLastWriteTime(ISaveGame saveGame)
    {
        object value = GetMember(saveGame, "LastWriteTime")
            ?? GetMember(saveGame, "lastWriteTime")
            ?? GetMember(saveGame, "ModifiedAt")
            ?? GetMember(saveGame, "CreatedAt");

        return value is DateTime dateTime ? dateTime : DateTime.MinValue;
    }

    private static int SanitizePort(int port)
    {
        return port >= MIN_PORT && port <= MAX_PORT ? port : DEFAULT_PORT;
    }

    private static string GetSafeName(ISaveGame saveGame)
    {
        return GetMember(saveGame, "Name") as string
            ?? GetMember(saveGame, "name") as string
            ?? "<unnamed>";
    }
}
