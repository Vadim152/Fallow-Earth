using UnityEngine;

/// <summary>
/// Simple item dropped after chopping a tree.
/// Currently only visual and does not affect gameplay.
/// </summary>
public class WoodLog : MonoBehaviour
{
    static Sprite woodSprite;

    /// <summary>
    /// Spawns a new wood log at the given world position.
    /// </summary>
    public static WoodLog Create(Vector2 position)
    {
        if (woodSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.55f, 0.27f, 0.07f));
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            woodSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        GameObject go = new GameObject("WoodLog");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = woodSprite;
        go.transform.position = position;
        return go.AddComponent<WoodLog>();
    }
}
