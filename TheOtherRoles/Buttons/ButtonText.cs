using TMPro;
using UnityEngine;

namespace TheOtherRoles.Buttons;

/// <summary>
/// The label text a TOR button wears: either a TOR translation id or one of the game's own
/// <see cref="StringNames"/>, optionally with a font material (the vanilla buttons' outline).
/// <para/>
/// This used to live nested inside <c>Objects.CustomButton</c>; it is the only piece of that class
/// the migrated buttons still need, so it moved out on its own.
/// </summary>
public class ButtonText
{
    private readonly int translationId;
    private readonly bool hasTranslationId;
    private readonly StringNames stringName;

    public Material Material { get; }

    public ButtonText(int translationId, Material material = null)
    {
        this.translationId = translationId;
        hasTranslationId = true;
        Material = material;
    }

    public ButtonText(StringNames stringName, Material material = null)
    {
        this.stringName = stringName;
        hasTranslationId = false;
        Material = material;
    }

    public string GetText()
    {
        if (!hasTranslationId)
            return TranslationController.Instance.GetString(stringName);

        return ModTranslation.GetString("Button", translationId);
    }

    public void ApplyMaterial(TMP_Text label)
    {
        if (Material != null && label != null)
            label.SetSharedMaterial(Material);
    }
}
