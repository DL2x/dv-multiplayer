using HarmonyLib;
using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityModManagerNet;
using MPAPI;

namespace MultiplayerAPITest;

public static class MultiplayerAPITest
{
    public static UnityModManager.ModEntry ModEntry;

    [UsedImplicitly]
    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        //Settings = Settings.Load(modEntry);
        //ModEntry.OnGUI = Settings.Draw;
        //ModEntry.OnSaveGUI = Settings.Save;
        //ModEntry.OnLateUpdate = LateUpdate;

        Harmony harmony = null;

        try
        {
            Log($"Multiplayer Mod is loaded: {MultiplayerAPI.IsMultiplayerLoaded}");


            Log("Patching...");
            harmony = new Harmony(ModEntry.Info.Id);
            harmony.PatchAll();

            Log("Loaded!");
        }
        catch (Exception ex)
        {
            LogException("Failed to load:", ex);
            harmony?.UnpatchAll();
            return false;
        }

        return true;
    }

    #region Logging

    public static void LogDebug(Func<object> resolver)
    {
        //if (!Settings.DebugLogging)
        //    return;
        WriteLog($"[Debug] {resolver.Invoke()}");
    }

    public static void Log(object msg)
    {
        WriteLog($"[Info] {msg}");
    }

    public static void LogWarning(object msg)
    {
        WriteLog($"[Warning] {msg}");
    }

    public static void LogError(object msg)
    {
        WriteLog($"[Error] {msg}");
    }

    public static void LogException(object msg, Exception e)
    {
        ModEntry.Logger.LogException($"{msg}", e);
    }

    private static void WriteLog(string msg)
    {
        string str = $"[{DateTime.Now.ToUniversalTime():HH:mm:ss.fff}] {msg}";
        //if (Settings.EnableLogFile)
        //    File.AppendAllLines(LOG_FILE, new[] { str });
        ModEntry.Logger.Log(str);
    }

    #endregion
}
