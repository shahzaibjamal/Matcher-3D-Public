using UnityEngine;
using UnityEngine.UI;

public class LoadingMenuView : MenuView
{
    [Header("Bar Elements")]
    public Slider ProgressSlider;
    public RectTransform BarFillArea;

    [Header("Slot Machine Elements")]
    public float IconSpinSpeed = 0.1f;

    public float MinTime;
    public float MaxTime;


    [Header("Orbital Settings")]
    public RectTransform IconCenter; // The pivot point
    public float Radius = 200f;
    public float IconSize = 100f;
    public float RotationSpeed = 3f;
    public RectTransform[] OrbitalIcons;
}