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
    void OnEnable() { Refresh(); }
    void Update() { Refresh(); }
    public void Refresh()
    {
        if (!label) return;
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
