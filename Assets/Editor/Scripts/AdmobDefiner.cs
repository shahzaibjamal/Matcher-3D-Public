using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[InitializeOnLoad]
public class AdMobDefiner
{
    private const string SYMBOL = "USE_ADMOB";

    static AdMobDefiner()
    {
        // We check if the MobileAds class exists in the current project assemblies
        bool hasAdMob = System.AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetType("GoogleMobileAds.Api.MobileAds") != null);

        UpdateDefines(hasAdMob);
    }

    private static void UpdateDefines(bool enabled)
    {
        // 1. Get the modern NamedBuildTarget
        NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        // 2. Use the non-obsolete Get method
        string defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
        List<string> allDefines = defines.Split(';').ToList();

        bool changed = false;

        if (enabled && !allDefines.Contains(SYMBOL))
        {
            allDefines.Add(SYMBOL);
            changed = true;
        }
        else if (!enabled && allDefines.Contains(SYMBOL))
        {
            allDefines.Remove(SYMBOL);
            changed = true;
        }

        // 3. Only save if something actually changed to avoid infinite recompilation loops
        if (changed)
        {
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", allDefines));
            Debug.Log($"[CoreLib] {(enabled ? "Added" : "Removed")} {SYMBOL} define for {buildTarget}.");
        }
    }
}