using UnityEngine;

/// <summary>
/// Simple transparent overlay used to show designated zones.
/// </summary>
public class ZoneOverlay : MonoBehaviour
{
    static Sprite overlaySprite;

    /// <summary>
    /// Creates a zone overlay object at the given world position.
    /// </summary>
    public static ZoneOverlay Create(Vector2 position, Color color)
    {
        if (overlaySprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            overlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        GameObject go = new GameObject("ZoneOverlay");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = overlaySprite;
        sr.color = color;
        sr.sortingOrder = 20;
        go.transform.position = position;
        return go.AddComponent<ZoneOverlay>();
    }
}
