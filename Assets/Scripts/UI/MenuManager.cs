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
        // 1. Get prefab from Registry instead of string path
        MenuRegistry.MenuEntry menuEntry = registry.GetMenuEntry(menuType);

        if (menuEntry.prefab != null)
        {
            MenuSession existingSession = _menuStack.FirstOrDefault(s => s.View is TView);

            if (existingSession.View != null)
            {
                // Pop until the existing menu is at the top
                while (_menuStack.Peek().View != existingSession.View)
                {
                    GoBack();
                }
                return; // We are now back at the original instance
            }
            Menus.MenuDisplayMode displayMode = menuEntry.defaultMode;
            // 2. Handle previous menu only if the NEW one is a ScreenReplace
            if (_menuStack.Count > 0 && displayMode == Menus.MenuDisplayMode.ScreenReplace)
            {
                var topMenu = _menuStack.Peek();
                topMenu.View.SetVisible(false);
                topMenu.Controller.OnPause();
            }

            // 3. Determine correct layer parent
            RectTransform parentLayer = GetLayer(displayMode);

            GameObject menuObject = Instantiate(menuEntry.prefab, parentLayer);
            if (menuObject != null && menuObject.TryGetComponent<TView>(out TView menuView))
            {
                // Assign the display mode to the view so GoBack knows how to handle it
                menuView.DisplayMode = displayMode;

                TController controller = new TController();
                controller.Bind(menuView, tData);
                controller.OnEnter();

                _menuStack.Push(new MenuSession { View = menuView, Controller = controller });
            }

            UpdateBlockingLayer();
        }
    }

    public void GoBack()
    {
        if (_menuStack.Count == 0) return;

        MenuSession top = _menuStack.Pop();
        Menus.MenuDisplayMode closedMode = top.View.DisplayMode;

        top.Controller.OnExit();
        top.View.Destroy();

        // 4. Restore previous menu ONLY if the one we just closed was a ScreenReplace
        if (_menuStack.Count > 0 && closedMode == Menus.MenuDisplayMode.ScreenReplace)
        {
            var previousMenu = _menuStack.Peek();
            previousMenu.View.SetVisible(true);
            previousMenu.Controller.OnResume();
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

        //        blockingLayer.SetActive(shouldDim);

        Image dimImage = blockingLayer.GetComponent<Image>();

        if (shouldDim)
        {
            // If it's already on, don't restart the animation
            if (!blockingLayer.activeSelf)
            {
                blockingLayer.SetActive(true);
                dimImage.DOFade(0.7f, 0.3f).From(0); // Fade from transparent to 70% black
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
}