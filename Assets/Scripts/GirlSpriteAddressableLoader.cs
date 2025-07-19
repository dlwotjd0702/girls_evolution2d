using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GirlSpriteAddressableLoader : MonoBehaviour
{
    public string spritesLabel = "GirlSprites";
    public List<Sprite> girlSprites = new List<Sprite>();

    public async Task LoadAllGirlSpritesAsync()
    {
        girlSprites.Clear();
        var handle = Addressables.LoadAssetsAsync<Sprite>(
            spritesLabel, null, false);

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 이름 기준 정렬 (0, 1, 2, ...)
            girlSprites = handle.Result.OrderBy(s => int.Parse(s.name)).ToList();
            Debug.Log($"[Addressables] {girlSprites.Count} sprites loaded (sorted by name).");
        }
        else
        {
            Debug.LogError("[Addressables] Sprite Load Failed");
        }
    }

    async void Start()
    {
        await LoadAllGirlSpritesAsync();
    }
}