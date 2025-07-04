using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple bed that colonists can use to rest and recover fatigue.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Bed : MonoBehaviour
{
    static Sprite bedSprite;
    public static List<Bed> AllBeds { get; } = new List<Bed>();

    public bool Reserved { get; set; }

    void Awake()
    {
        if (bedSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.6f, 0.8f, 1f));
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            bedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = bedSprite;
        sr.sortingOrder = 5;

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        AllBeds.Add(this);
    }

    void OnDestroy()
    {
        AllBeds.Remove(this);
    }

    /// <summary>
    /// Spawns a bed at the given world position.
    /// </summary>
    public static Bed Create(Vector2 position)
    {
        GameObject go = new GameObject("Bed");
        go.transform.position = position;
        return go.AddComponent<Bed>();
    }

    public static Bed FindAvailable(Vector2 pos)
    {
        Bed best = null;
        float bestDist = float.MaxValue;
        foreach (var b in AllBeds)
        {
            if (b == null || b.Reserved)
                continue;
            float d = Vector2.Distance(pos, b.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        return best;
    }
}
