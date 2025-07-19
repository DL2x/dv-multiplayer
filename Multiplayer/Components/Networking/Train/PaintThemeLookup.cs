using DV.Customization.Paint;
using DV.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JetBrains.Annotations;


namespace Multiplayer.Components.Networking.Train;

public class PaintThemeLookup : SingletonBehaviour<PaintThemeLookup>
{
    private readonly Dictionary<uint, string> hashToThemeName = [];

    protected override void Awake()
    {
        base.Awake();
        var themeNames = Resources.LoadAll<Object>("").Where(x => x is PaintTheme)
            .Select(x => ((PaintTheme)x).AssetName)
            .ToArray();

        foreach (var themeName in themeNames)
            RegisterTheme(themeName);
    }

    public PaintTheme GetPaintTheme(uint themeId)
    {
        PaintTheme theme = null;

        var themeName = GetThemeNameFromId(themeId);

        if (themeName != null)
            PaintTheme.TryLoad(themeName, out theme);

        return theme;
    }

    public string GetThemeNameFromId(uint themeId)
    {
        hashToThemeName.TryGetValue(themeId, out string themeName);

        return themeName;
    }

    public uint GetThemeId(PaintTheme theme)
    {
        if(theme == null)
            return 0;

        return GetThemeId(theme.AssetName);
    }

    public uint GetThemeId(string themeName)
    {
        return Fnv1aHash(themeName);
    }

    public uint RegisterTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            return 0;

        var hash = GetThemeId(themeName);

        if (hashToThemeName.ContainsKey(hash))
        {
            Multiplayer.LogWarning($"Theme '{themeName}' is already registered with id: {hash}.");
            return hash;
        }

        hashToThemeName[hash] = themeName;

        Multiplayer.Log($"Theme '{themeName}' registered with id: {hash}.");

        return hash;
    }

    public void UnregisterTheme(string themeName)
    {
        var hash = GetThemeId(themeName);

        if (hashToThemeName.TryGetValue(hash, out _))
        {
            hashToThemeName.Remove(hash);
        }
        else
        {
            Multiplayer.LogWarning($"Tried to unregister theme '{themeName}', but theme is not registered.");
        }
    }
    

    private uint Fnv1aHash(string text)
    {
        unchecked
        {
            const uint fnvPrime = 0x01000193;
            uint hash = 0x811C9DC5;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= fnvPrime;
            }
            return hash;
        }
    }

    [UsedImplicitly]
    public new static string AllowAutoCreate()
    {
        return $"[{nameof(PaintThemeLookup)}]";
    }
}
