using UnityEngine;
using UnityEngine.UI;

public class LevelNode : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI levelNumberText;
    [SerializeField] private Image statusIcon; // Locked/Unlocked/Completed
    [SerializeField] private GameObject[] stars; // Visual star rating
    [SerializeField] private CanvasGroup _canvasGroup; // Visual star rating

    private Button _button;
    private LevelDisplayData _levelDisplayData;
    void Awake()
    {
        _button = GetComponent<Button>();

        _button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);

    }
    public void Setup(LevelDisplayData data, int index)
    {
        levelNumberText.text = (index + 1).ToString();
        _levelDisplayData = data;

        // Visuals based on IsUnlocked
        var currentLevelId = GameManager.Instance.SaveData.CurrentLevelID;

        statusIcon.gameObject.SetActive(_levelDisplayData.StaticData.Id == currentLevelId);
        statusIcon.color = data.IsUnlocked ? _levelDisplayData.StaticData.Id == currentLevelId ? Color.green : Color.white : Color.gray;
        // _button.interactable = _levelDisplayData.IsUnlocked;
        _canvasGroup.alpha = _levelDisplayData.IsUnlocked ? 1.0f : 0.4f;

        // Show stars if level is completed
        for (int i = 0; i < stars.Length; i++)
        {
            // stars[i].SetActive(data.ProgressData != null && data.ProgressData.StarRating > i);
            stars[i].SetActive(_levelDisplayData.ProgressData != null);
        }
    }

    public void OnClick()
    {
        if (_levelDisplayData.IsUnlocked)
        {
            // Tell the Game Manager to load this level
            MenuManager.Instance.OpenMenu<LevelDetailMenuView, LevelDetailMenuController, LevelDetailMenuData>(Menus.Type.LevelDetail, new LevelDetailMenuData
            {
                LevelData = _levelDisplayData.StaticData
            });
        }
        else
        {
            SoundController.Instance.PlaySoundEffect("deny");
        }
    }
}