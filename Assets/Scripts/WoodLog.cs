using UnityEngine;

/// <summary>
/// Simple item dropped after chopping a tree.
/// Displays how much wood was collected.
/// </summary>
public class WoodLog : MonoBehaviour
{
    static Sprite woodSprite;

    public int Amount { get; private set; }
    public bool Reserved { get; set; }

    /// <summary>
    /// Spawns a new wood log at the given world position.
    /// </summary>
    public static WoodLog Create(Vector2 position, int amount)
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

        WoodLog log = go.AddComponent<WoodLog>();
        log.Amount = amount;
        log.Reserved = false;
        log.CreateText();
        return log;
    }

    void CreateText()
    {
        GameObject tObj = new GameObject("AmountText");
        tObj.transform.SetParent(transform, false);
        var tm = tObj.AddComponent<TextMesh>();
        tm.text = Amount.ToString();
        tm.characterSize = 0.1f;
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }
}
