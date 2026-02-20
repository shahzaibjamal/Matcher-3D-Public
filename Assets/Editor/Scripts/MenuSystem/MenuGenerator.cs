using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MenuGenerator : EditorWindow
{
    private string menuName = "Settings";
    private string templateFolder;

    // Change this if your path is slightly different, but this matches your description
    private const string MenusFilePath = "Assets/Scripts/UI/Menu/Menus.cs";

    [MenuItem("Tools/UI/Generate New Menu")]
    public static void ShowWindow() => GetWindow<MenuGenerator>("Menu Generator");

    private void OnGUI()
    {
        GUILayout.Label("Generate Menu (Direct Path Version)", EditorStyles.boldLabel);
        menuName = EditorGUILayout.TextField("Menu Name", menuName);

        if (GUILayout.Button("Generate"))
        {
            if (string.IsNullOrEmpty(menuName)) return;
            Generate();
        }
    }

    private void Generate()
    {
        var script = MonoScript.FromScriptableObject(this);
        templateFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));

        string menuRootPath = Path.Combine("Assets/Scripts/UI/Menu", $"{menuName}Menu");
        string statesPath = Path.Combine(menuRootPath, "States");

        if (!Directory.Exists(menuRootPath)) Directory.CreateDirectory(menuRootPath);
        if (!Directory.Exists(statesPath)) Directory.CreateDirectory(statesPath);

        var fileDefinitions = new Dictionary<string, string>
        {
            { "TempMenuView", menuRootPath },
            { "TempMenuData", menuRootPath },
            { "TempMenuController", menuRootPath },
            { "TempMenuBaseState", menuRootPath }, // Moved back to root as requested
            { "TempMenuState_Main", statesPath }
        };

        foreach (var file in fileDefinitions)
        {
            string sourcePath = Path.Combine(templateFolder, file.Key + ".txt");
            if (File.Exists(sourcePath))
            {
                string content = File.ReadAllText(sourcePath);
                content = content.Replace("Temp", menuName);
                string newFileName = file.Key.Replace("Temp", menuName) + ".cs";
                File.WriteAllText(Path.Combine(file.Value, newFileName), content);
            }
        }

        UpdateMenusScript();
        AssetDatabase.Refresh();
    }

    private void UpdateMenusScript()
    {
        // Check if file exists at the specific hardcoded path
        if (!File.Exists(MenusFilePath))
        {
            Debug.LogError($"[MenuGenerator] Menus.cs not found at: {MenusFilePath}. Please check the path in the script.");
            return;
        }

        string content = File.ReadAllText(MenusFilePath);

        // Check if already contains the menu to avoid double entry
        if (content.Contains($" {menuName},") || content.Contains($" {menuName}\n"))
        {
            Debug.Log($"[MenuGenerator] {menuName} already exists in Menus.cs");
            return;
        }

        // Logic: Find the MenuType enum block
        int enumIndex = content.IndexOf("enum MenuType");
        if (enumIndex == -1)
        {
            Debug.LogError("[MenuGenerator] Could not find 'enum MenuType' in Menus.cs");
            return;
        }

        // Find the first closing brace AFTER the enum keyword
        int closingBrace = content.IndexOf("}", enumIndex);
        if (closingBrace == -1) return;

        // Find the last comma or the last enum value to handle formatting
        int lastContentChar = content.LastIndexOfAny(new char[] { ',', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' }, closingBrace - 1);

        // Insert a comma if the last item doesn't have one, then our new item
        string insertion = $",\n        {menuName}";

        // We insert right before the closing brace
        content = content.Insert(closingBrace, insertion);

        // Clean up formatting: if there was no comma before, it might look like 'Game, Settings'
        // If there was a comma, it might look like 'Game,, Settings'. We fix that.
        content = content.Replace(",,", ",");

        File.WriteAllText(MenusFilePath, content);
        Debug.Log($"[MenuGenerator] Successfully updated {MenusFilePath}");
    }
}