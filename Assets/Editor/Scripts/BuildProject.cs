using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;

public class BuildProject : EditorWindow
{
    private static string ProjectRoot => Path.Combine(Application.dataPath, "..");
    private static string BuildPath => Path.Combine(ProjectRoot, "Builds");
    private static string SettingsPath => Path.Combine(ProjectRoot, "BuildSettings.txt");

    [MenuItem("Tools/Build/Android Release")]
    public static void BuildRelease() => PerformBuild(true);

    [MenuItem("Tools/Build/Android Debug")]
    public static void BuildDebug() => PerformBuild(false);

    private static void PerformBuild(bool isRelease)
    {
        if (!Directory.Exists(BuildPath)) Directory.CreateDirectory(BuildPath);

        Dictionary<string, string> settings = LoadSettings();
        if (settings.Count == 0) return;

        // --- 1. Identity Settings ---
        PlayerSettings.companyName = settings["CompanyName"];
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, settings["PackageName"]);
        PlayerSettings.bundleVersion = settings["Version"];
        PlayerSettings.Android.bundleVersionCode = int.Parse(settings["BundleVersionCode"]);

        // --- 2. Keystore Configuration ---
        string keystoreName = settings["KeystorePath"];
        string absoluteKeystorePath = Path.Combine(ProjectRoot, keystoreName);

        if (!File.Exists(absoluteKeystorePath))
        {
            EditorUtility.DisplayDialog("Build Error", $"Keystore file not found at: {absoluteKeystorePath}", "OK");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = absoluteKeystorePath;
        PlayerSettings.Android.keystorePass = settings["KeystorePass"];
        PlayerSettings.Android.keyaliasName = settings["KeyAlias"];
        PlayerSettings.Android.keyaliasPass = settings["KeyAliasPass"];

        // --- 3. Build Formatting ---
        EditorUserBuildSettings.buildAppBundle = isRelease;
        string extension = isRelease ? ".aab" : ".apk";
        string fileName = isRelease ? $"App_v{settings["Version"]}{extension}" : "App_Debug.apk";
        string fullPath = Path.Combine(BuildPath, fileName);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = fullPath,
            target = BuildTarget.Android,
            options = isRelease ? BuildOptions.None : BuildOptions.Development
        };

        // --- 4. Execution ---
        Debug.Log($"[Build] Starting {(isRelease ? "AAB Release" : "APK Debug")}...");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        // --- 5. Post-Build Notification ---
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorApplication.Beep(); // System sound cue

            if (isRelease)
            {
                IncrementVersion(settings);
                EditorUtility.RevealInFinder(fullPath);
            }

            EditorUtility.DisplayDialog("Build Successful",
                $"Target: {(isRelease ? "Release AAB" : "Debug APK")}\nLocation: {fullPath}", "Great!");
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed", "Check the Console for errors.", "Back to work");
        }
    }

    private static void IncrementVersion(Dictionary<string, string> settings)
    {
        int code = int.Parse(settings["BundleVersionCode"]) + 1;
        settings["BundleVersionCode"] = code.ToString();

        string[] vParts = settings["Version"].Split('.');
        if (vParts.Length == 3 && int.TryParse(vParts[2], out int patch))
        {
            patch++;
            settings["Version"] = $"{vParts[0]}.{vParts[1]}.{patch}";
        }

        File.WriteAllLines(SettingsPath, settings.Select(x => $"{x.Key}={x.Value}"));
    }

    private static Dictionary<string, string> LoadSettings()
    {
        if (!File.Exists(SettingsPath))
        {
            Debug.LogError($"[Build] BuildSettings.txt not found at {SettingsPath}!");
            return new Dictionary<string, string>();
        }

        return File.ReadAllLines(SettingsPath)
            .Select(line => line.Split('='))
            .Where(split => split.Length == 2)
            .ToDictionary(split => split[0].Trim(), split => split[1].Trim());
    }
}