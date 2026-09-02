using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Text stays sharp and translatable instead of being baked into textures.</summary>
public class LocalizedTextureLabel : MonoBehaviour
{
    public TMP_Text label;
    public string korean, english;
    public Image stateImage;
    public Sprite purchaseSprite, upgradeSprite, maxSprite;
    [Tooltip("Use for artwork that already contains Korean lettering. Korean keeps the original art; only non-Korean languages show this label.")]
    public bool englishOnly;
    public Graphic backdrop;

    bool hasSnapshot;
    bool lastKorean;
    Sprite lastStateSprite;

    void OnEnable()
    {
        hasSnapshot = false;
        Refresh();
    }

    void LateUpdate()
    {
        bool koreanNow = LocalizationManager.IsKorean;
        Sprite stateNow = stateImage ? stateImage.sprite : null;
        if (!hasSnapshot || koreanNow != lastKorean || stateNow != lastStateSprite)
            Refresh();
    }

    public void Refresh()
    {
        if (!label) return;
        bool koreanNow = LocalizationManager.IsKorean;
        bool visible = !englishOnly || !koreanNow;
        if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        if (backdrop && backdrop.gameObject.activeSelf != visible) backdrop.gameObject.SetActive(visible);

        lastKorean = koreanNow;
        lastStateSprite = stateImage ? stateImage.sprite : null;
        hasSnapshot = true;
        if (!visible) return;

        string ko=korean,en=english;
        if (stateImage && maxSprite)
        {
            if (stateImage.sprite==maxSprite) ko=en="MAX";
            else if (stateImage.sprite==purchaseSprite) { ko="구매"; en="BUY"; }
            else { ko="강화"; en="UPGRADE"; }
        }
        string value=LocalizationManager.GetText(ko,en);
        if (label.text!=value) label.text=value;
    }
}
