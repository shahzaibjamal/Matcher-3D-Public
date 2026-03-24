#if UNITY_EDITOR
using System.Linq;
#endif
using UnityEngine;

public partial class Spawner : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) Debug_ClickRandom();
        if (Input.GetKeyDown(KeyCode.M)) Debug_ClickTarget();
    }

    private void Debug_ClickRandom()
    {
        if (_itemClickables.Count == 0)
        {
            Debug.LogWarning("<color=yellow>[Debug]</color> No items left on board to click.");
            return;
        }

        int index = Random.Range(0, _itemClickables.Count);
        InvokeClick(_itemClickables[index]);
    }

    private void Debug_ClickTarget()
    {
        if (_collectableLeft == null || _collectableLeft.Count == 0) return;

        string targetID = _collectableLeft.Keys.First();

        var targetItem = _itemClickables.FirstOrDefault(i => i.ItemData.Id == targetID);
        if (targetItem != null)
        {
            InvokeClick(targetItem);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Debug]</color> Could not find any board items with ID: {targetID}");
        }
    }

    private void InvokeClick(ClickableItem item)
    {
        Debug.Log($"<color=cyan>[Debug]</color> Simulating click on: {item.ItemData.Id}");
        item.OnHandleClick(default);
    }
#endif

    private int _debugSpawnIndex = 0;
    private bool _showDebugMenu = false; // Toggle state for the menu
    private void DebugOnGUIItemVisualizer()
    {
        var items = DataManager.Instance.Metadata.Items;
        if (items == null || items.Count == 0) return;

        // 1. SETUP BIG SCALABLE STYLES
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 28 };
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.linearGrayTexture } };
        GUIStyle stepButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };

        // 2. MAIN TOGGLE BUTTON (Large and centered at the top)
        float screenWidth = Screen.width;
        float toggleWidth = 300;
        float toggleHeight = 100;

        if (GUI.Button(new Rect(0, 0, toggleWidth, toggleHeight),
            _showDebugMenu ? "X CLOSE MENU" : "☰ OPEN DEBUG MENU", buttonStyle))
        {
            _showDebugMenu = !_showDebugMenu;
        }

        if (!_showDebugMenu) return;

        // 3. THE BIG MENU PANEL
        float menuWidth = 500;
        float menuHeight = 650;
        // Positioned in the middle of the screen
        Rect menuRect = new Rect((screenWidth / 2) - (menuWidth / 2), 100, menuWidth, menuHeight);

        GUI.Box(menuRect, "", boxStyle); // Dark background for contrast
        GUILayout.BeginArea(menuRect);
        GUILayout.BeginVertical();
        GUILayout.Space(20);

        var targetItemData = items[_debugSpawnIndex];

        GUILayout.BeginVertical();
        // --- Index Row with -10 / +10 ---
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", stepButtonStyle, GUILayout.Width(100), GUILayout.Height(80)))
            ChangeIndex(-10, items.Count);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+10", stepButtonStyle, GUILayout.Width(100), GUILayout.Height(80)))
            ChangeIndex(10, items.Count);
        GUILayout.EndHorizontal();
        GUILayout.Label($"  Index: <b>{_debugSpawnIndex}</b> / {items.Count - 1}", labelStyle);
        GUILayout.Label($"  Name: <color=cyan>{targetItemData.Name}</color>", labelStyle);
        GUILayout.Label($"  ID: {targetItemData.Id}", labelStyle);
        GUILayout.Label($"  Size: <color=yellow>{targetItemData.Size}</color>", labelStyle);

        string statusColor = targetItemData.Enabled ? "#00FF00" : "#FF0000";
        GUILayout.Label($"  Enabled: <color={statusColor}>{targetItemData.Enabled}</color>", labelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(30);

        // ACTION BUTTONS (BIG)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SPAWN 3x INSTANCES", GUILayout.Height(80), GUILayout.Width(menuWidth - 20)))
        {
            for (int i = 0; i < 3; i++)
            {
                CreateItemInstance(targetItemData);
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        // NAVIGATION BUTTONS (BIG)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◄ PREVIOUS", GUILayout.Height(70), GUILayout.Width((menuWidth / 2) - 15)))
            ChangeIndex(-1, items.Count);

        if (GUILayout.Button("NEXT ►", GUILayout.Height(70), GUILayout.Width((menuWidth / 2) - 15)))
            ChangeIndex(1, items.Count);

        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        // RESET
        GUI.backgroundColor = Color.gray;
        if (GUILayout.Button("RESET TO INDEX 0", GUILayout.Height(40)))
        {
            _debugSpawnIndex = 0;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void ChangeIndex(int amount, int totalCount)
    {
        _debugSpawnIndex += amount;

        // Wrap logic for high-speed scrolling
        if (_debugSpawnIndex >= totalCount) _debugSpawnIndex = 0;
        if (_debugSpawnIndex < 0) _debugSpawnIndex = totalCount - 1;

        Debug.Log($"<color=white>[Debug]</color> Index shifted to: {_debugSpawnIndex}");
    }

}