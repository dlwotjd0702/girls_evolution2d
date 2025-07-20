using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GirlSpriteAddressableLoader : MonoBehaviour
{
    public string spritesLabel = "GirlSprites";
    private Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();
    public bool IsLoaded { get; private set; } = false;

    // 외부에서 스프라이트 딕셔너리 읽을 수 있게 public property
    public Dictionary<string, Sprite> SpriteDict => spriteDict;

    private async void Awake()
    {
        await LoadAllGirlSpritesAsync();
        IsLoaded = true;
        Debug.Log("[GirlSpriteAddressableLoader] 스프라이트 로드 완료");
    }

    public async Task LoadAllGirlSpritesAsync()
    {
        spriteDict.Clear();
        var handle = Addressables.LoadAssetsAsync<Sprite>(spritesLabel, null, false);
        await handle.Task;
        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            foreach (var s in handle.Result)
            {
                var match = Regex.Match(s.name, @"\d+");
                var key = match.Success ? match.Value : s.name;
                if (!spriteDict.ContainsKey(key))
                    spriteDict[key] = s;
            }
        }
        else
        {
            Debug.LogError("[Addressables] Sprite Load Failed");
        }
    }
}