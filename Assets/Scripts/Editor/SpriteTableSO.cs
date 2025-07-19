using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteTableSO", menuName = "SpriteTableSO")]
public class SpriteTableSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string spriteName; // TSV에서 쓰는 값과 동일해야 함!
        public Sprite sprite;
    }
    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, Sprite> dict = null;

    public Sprite GetSprite(string spriteName)
    {
        if (dict == null)
        {
            dict = new Dictionary<string, Sprite>();
            foreach (var entry in entries)
                dict[entry.spriteName] = entry.sprite;
        }
        dict.TryGetValue(spriteName, out var sprite);
        return sprite;
    }
}