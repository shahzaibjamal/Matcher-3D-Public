using CoppraGames;
using UnityEngine;
using UnityEngine.UI;

public class SpinWheelMenuView : MenuView
{
    public SpinWheelController SpinWheelController;

    public Button SpinButton;
}


public class SpinRewardData
{

    public SpinRewardType SpinRewardType;
    public int Amount;
}

public enum SpinRewardType
{
    Gold = 1,
    Magnet = 2,
    Hint = 3,

    Shake = 4,

    Undo = 5
}