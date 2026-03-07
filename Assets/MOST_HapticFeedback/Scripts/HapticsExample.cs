// By SOLO :)
// Check MOST IN ONE package https://assetstore.unity.com/packages/slug/295013

using UnityEngine;
using UnityEngine.UI;


// This is an optional example for all haptic feedback calls.
// from any .cs file inside your project
// call >>> MOST_HapticFeedback.Generate(HapticTypes type)
// or MOST_HapticFeedback.GenerateWithCooldown(HapticTypes type, float cooldown)
// or MOST_HapticFeedback.GeneratePattern(CustomHapticPattern CustomPattern);

public class HapticsExample : MonoBehaviour
{
    public Toggle HapticToggle;
    public Haptics.CustomHapticPattern CustomHapticPatternA;
    public Haptics.CustomHapticPattern CustomHapticPatternB;

    void Start()
    {
        HapticToggle.isOn = Haptics.HapticsEnabled;
    }

    public void GenerateBasicHaptic(Haptics.HapticTypes type)
    {
        Haptics.Generate(type);
    }

    public void GenerateBasicHapticWithCoolDown(Haptics.HapticTypes type, float cooldown)
    {
        Haptics.GenerateWithCooldown(type, cooldown);
    }

    public void GenerateCustomHapticA()
    {
        Haptics.GeneratePattern(CustomHapticPatternA);
    }

    public void GenerateCustomHapticB()
    {
        Haptics.GeneratePattern(CustomHapticPatternB);
    }

    public void HapticEnable(bool enable)
    {
        Haptics.HapticsEnabled = enable;
    }

    // __________________________________ Basic Haptics __________________________________
    public void SelectionHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.Selection);
    }

    public void SuccessHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.Success);
    }

    public void WarningHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.Warning);
    }

    public void FailureHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.Failure);
    }

    public void LightImpactHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.LightImpact);
    }

    public void MediumImpactHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.MediumImpact);
    }

    public void HeavyImpactHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.HeavyImpact);
    }

    public void RigidImpactHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.RigidImpact);
    }

    public void SoftImpactHaptic()
    {
        Haptics.Generate(Haptics.HapticTypes.SoftImpact);
    }

    // __________________________________ Basic Haptics with Cooldown __________________________________ 
    public void SelectionHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.Selection, cooldown);
    }

    public void SuccessHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.Success, cooldown);
    }

    public void WarningHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.Warning, cooldown);
    }

    public void FailureHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.Failure, cooldown);
    }

    public void LightImpactHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.LightImpact, cooldown);
    }

    public void MediumImpactHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.MediumImpact, cooldown);
    }

    public void HeavyImpactHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.HeavyImpact, cooldown);
    }

    public void RigidImpactHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.RigidImpact, cooldown);
    }

    public void SoftImpactHapticWithCooldown(float cooldown)
    {
        Haptics.GenerateWithCooldown(Haptics.HapticTypes.SoftImpact, cooldown);
    }

    // ___________________ Enable / Disable Haptic Feedback ___________________  
    public void ToggleHaptics(bool enabled)
    {
        Haptics.HapticsEnabled = enabled;
    }

    // Opem URL
    public void OpenURL()
    {
        Application.OpenURL("https://assetstore.unity.com/packages/slug/295013");
    }
}
