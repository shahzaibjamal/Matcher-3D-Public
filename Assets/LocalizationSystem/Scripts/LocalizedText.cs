using System;
using TMPro;
using TS.LocalizationSystem;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private String _key;
    private TMP_Text _tmpText;

    void Awake()
    {
        if (!string.IsNullOrEmpty(_key) && TryGetComponent(out _tmpText))
        {
            _tmpText.text = LocaleManager.Localize(_key);
            LocaleManager.LocaleChanged += OnLocaleChanged;
        }
    }

    private void OnLocaleChanged(LocaleConfig current, int updateNumber)
    {
        if (_tmpText != null)
        {
            _tmpText.text = LocaleManager.Localize(_key);
        }
    }

    void OnDestroy()
    {
        LocaleManager.LocaleChanged -= OnLocaleChanged;
    }
}