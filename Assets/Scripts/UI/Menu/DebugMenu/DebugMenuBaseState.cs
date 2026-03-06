using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuBaseState : MenuBaseState<DebugMenuController, DebugMenuView, DebugMenuData>
{
    private LevelData _currentLevel;
    public DebugMenuBaseState(DebugMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.LoadButton.onClick.AddListener(OnLoadButtonClicked);
        View.PowerUpButton.onClick.AddListener(OnAddPowerUpButton);
        View.SaveButton.onClick.AddListener(OnSaveButton);
        View.BackButton.onClick.AddListener(Controller.HandleBackInput);
        // LoadGameData();
    }


    public override void Exit()
    {
        View.LoadButton.onClick.RemoveListener(OnLoadButtonClicked);
        View.PowerUpButton.onClick.RemoveListener(OnAddPowerUpButton);
        View.SaveButton.onClick.RemoveListener(OnSaveButton);
        View.BackButton.onClick.RemoveListener(Controller.HandleBackInput);
    }


    private void OnLoadButtonClicked()
    {
        LoadLevel();
    }
    private void OnAddPowerUpButton()
    {
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Shake, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Magnet, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Hint, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Undo, 5);
    }
    private void OnSaveButton()
    {
        MenuManager.Instance.OpenMenu<MatchResultMenuView, MatchResultMenuController, MatchResultMenuData>(Menus.Type.MatchResult, new MatchResultMenuData
        {
            IsWin = true,
            LevelData = LevelManager.Instance.GetLevelByID("level_01"),
            MatchRate = 1
        });
    }

    public void LoadLevel()
    {
    }


}