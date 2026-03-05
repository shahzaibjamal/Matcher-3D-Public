using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private struct MenuSession
    {
        public IMenuView View;
        public IMenuController Controller;
    }

    private Stack<MenuSession> _menuStack = new Stack<MenuSession>();
    public static MenuManager Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private MenuRegistry registry;

    [Header("Layers")]
    [SerializeField] private RectTransform screenLayer;
    [SerializeField] private RectTransform popupLayer;
    [SerializeField] private RectTransform overlayLayer;

    [Header("Blocking Logic")]
    [SerializeField] private GameObject blockingLayer; // The "Dimmer" for popups

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (blockingLayer) blockingLayer.SetActive(false);
    }

    void Start()
    {
        // // Ensure the blocking layer has a Button component
        // if (blockingLayer.TryGetComponent<Button>(out Button btn))
        // {
        //     // When the dark background is clicked, it acts as a "Back" button
        //     btn.onClick.AddListener(() => GoBack());
        // }
        SubscribeBlockingButton();
    }
    public void OpenMenu<TView, TController, TData>(
        Menus.Type menuType,
        TData tData = null)
        where TView : MenuView
        where TController : MenuController<TView, TData>, new()
        where TData : MenuData
    {
        MenuRegistry.MenuEntry menuEntry = registry.GetMenuEntry(menuType);
        if (menuEntry.prefab == null) return;

        Menus.MenuDisplayMode newMode = menuEntry.defaultMode;

        // 1. ONE SCREEN ONLY RULE
        // 1. ONE SCREEN ONLY / CLEAN SLATE RULE
        if (newMode == Menus.MenuDisplayMode.ScreenReplace)
        {
            // Unwind the stack completely until we've removed the previous screen
            // and all popups currently sitting on top of it.
            while (_menuStack.Count > 0)
            {
                var top = _menuStack.Peek();

                // Peek at the mode. If we found the previous screen, 
                // we'll destroy it and stop. If we find popups, we destroy them and keep going.
                bool isPreviousScreen = top.View.DisplayMode == Menus.MenuDisplayMode.ScreenReplace;

                var session = _menuStack.Pop();
                session.Controller.OnExit();
                session.View.Destroy();

                if (isPreviousScreen) break;
            }
        }
        // 2. SEARCH & RESUME (Only for Popups/Overlays)
        else
        {
            MenuSession existingSession = _menuStack.FirstOrDefault(s => s.View is TView);
            if (existingSession.View != null)
            {
                while (_menuStack.Peek().View != existingSession.View)
                {
                    var session = _menuStack.Pop();
                    session.Controller.OnExit();
                    session.View.Destroy();
                }

                existingSession.View.SetVisible(true);
                existingSession.Controller.OnResume();
                UpdateBlockingLayer();
                return;
            }
        }

        // 3. SMART HIDE PREVIOUS
        if (_menuStack.Count > 0)
        {
            var topSession = _menuStack.Peek();

            // ONLY hide the previous menu if the NEW menu is a full screen
            // Popups and Overlays should let the previous menu stay visible
            if (newMode == Menus.MenuDisplayMode.ScreenReplace)
            {
                topSession.View.SetVisible(false);
                topSession.Controller.OnPause();
            }
            else
            {
                // If it's a popup, we might still want to "Pause" logic 
                // but keep the View visible
                topSession.Controller.OnPause();
            }
        }
        // 4. INSTANTIATE & PUSH
        RectTransform parentLayer = GetLayer(newMode);
        GameObject menuObject = Instantiate(menuEntry.prefab, parentLayer);

        if (menuObject != null && menuObject.TryGetComponent<TView>(out TView menuView))
        {
            // IMPORTANT: Assign this IMMEDIATELY so the NEXT OpenMenu call can read it
            menuView.DisplayMode = newMode;

            TController controller = new TController();
            controller.Bind(menuView, tData);

            // Push to stack
            _menuStack.Push(new MenuSession { View = menuView, Controller = controller });

            // Call Enter last
            controller.OnEnter();
        }

        UpdateBlockingLayer();
    }

    public void GoBack()
    {
        if (_menuStack.Count == 0) return;

        // 1. POP THE CURRENT TOP
        MenuSession closingSession = _menuStack.Pop();
        Menus.MenuDisplayMode closingMode = closingSession.View.DisplayMode;

        // Trigger the animated exit
        closingSession.View.OnExit(() =>
        {
            closingSession.Controller.OnExit();
            closingSession.View.Destroy();
        });

        // 2. SMART RESTORE PREVIOUS
        if (_menuStack.Count > 0)
        {
            var previousSession = _menuStack.Peek();
            Menus.MenuDisplayMode previousMode = previousSession.View.DisplayMode;

            // Restore if:
            // - We closed a Screen (which always hid the one below)
            // - We closed a Popup and the one below is a Popup (which was hidden by the top popup)
            bool shouldRestorePrevious =
                closingMode == Menus.MenuDisplayMode.ScreenReplace ||
                (closingMode == Menus.MenuDisplayMode.Popup && previousMode == Menus.MenuDisplayMode.Popup) ||
            (closingMode == Menus.MenuDisplayMode.Overlay && previousMode == Menus.MenuDisplayMode.Overlay);

            if (shouldRestorePrevious)
            {
                previousSession.View.SetVisible(true);
                previousSession.Controller.OnResume();
            }
        }

        UpdateBlockingLayer();
    }
    private RectTransform GetLayer(Menus.MenuDisplayMode mode)
    {
        return mode switch
        {
            Menus.MenuDisplayMode.Popup => popupLayer,
            Menus.MenuDisplayMode.Overlay => overlayLayer,
            _ => screenLayer
        };
    }

    private void UpdateBlockingLayer()
    {
        if (blockingLayer == null) return;

        if (_menuStack.Count == 0)
        {
            blockingLayer.SetActive(false);
            return;
        }

        var topMenu = _menuStack.Peek();
        Menus.MenuDisplayMode mode = topMenu.View.DisplayMode;

        // 1. Should we dim? 
        // Usually, we dim for Popups and specific Overlays (like FTUE)
        bool shouldDim = (mode == Menus.MenuDisplayMode.Popup || mode == Menus.MenuDisplayMode.Overlay);

        Image dimImage = blockingLayer.GetComponent<Image>();

        if (shouldDim)
        {
            // If it's already on, don't restart the animation
            if (!blockingLayer.activeSelf)
            {
                blockingLayer.SetActive(true);
                dimImage.DOFade(0.8f, 0.3f).From(0); // Fade from transparent to 70% black
            }
        }
        else
        {
            // Only fade out if it is actually active
            if (blockingLayer.activeSelf)
            {
                dimImage.DOKill();
                dimImage.DOFade(0, 0.2f).OnComplete(() => blockingLayer.SetActive(false));
            }
        }

        if (shouldDim)
        {
            // 2. Move the dimmer to the SAME layer as the top menu
            RectTransform targetLayer = GetLayer(mode);
            blockingLayer.transform.SetParent(targetLayer);

            // 3. Put it at the very back of THAT layer
            // This ensures it stays behind the menu but in front of everything else below
            blockingLayer.transform.SetAsFirstSibling();

            // 4. Reset scale/position in case parenting messed it up
            RectTransform rect = blockingLayer.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero; // Assuming it's set to stretch
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
        }
    }

    private void SubscribeBlockingButton()
    {
        if (blockingLayer != null)
        {
            if (blockingLayer.TryGetComponent<Button>(out Button btn))
            {
                // Remove any existing listeners first to prevent double-calls
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => GoBack());
            }
            else
            {
                // Add it if it's missing so you don't have to do it manually in the Inspector
                btn = blockingLayer.AddComponent<Button>();
                btn.transition = Selectable.Transition.None; // No visual change on click
                btn.onClick.AddListener(() => GoBack());
            }
        }
    }
    public void ClearAll()
    {
        // We use a while loop to ensure we empty the stack completely
        while (_menuStack.Count > 0)
        {
            MenuSession session = _menuStack.Pop();

            // 1. Unsubscribe events and stop logic
            if (session.Controller != null)
            {
                session.Controller.OnExit();
            }

            // 2. Remove the actual GameObjects from the Hierarchy
            if (session.View != null)
            {
                session.View.Destroy();
            }
        }

        // 3. Reset the Dimmer/Blocking layer
        if (blockingLayer)
        {
            blockingLayer.SetActive(false);
        }

        Debug.Log("<color=red>MenuManager:</color> All menus cleared and controllers released.");
    }

    private void OnDestroy()
    {
        // Safety net: If the manager is destroyed, 
        // force all active controllers to unhook their events.
        ClearAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_menuStack.Count > 0)
            {
                // Ask the top-most menu to handle it
                _menuStack.Peek().Controller.HandleBackInput();
            }
        }
    }
}