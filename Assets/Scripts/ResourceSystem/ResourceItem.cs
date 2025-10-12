using FallowEarth.ResourcesSystem;
using UnityEngine;

/// <summary>
/// World entity that represents a stack of resources that can be hauled by colonists.
/// </summary>
public class ResourceItem : MonoBehaviour
{
    static Sprite fallbackSprite;

    public ResourceStack Stack { get; private set; }
    public bool Reserved { get; set; }

    public static ResourceItem Create(Vector2 position, ResourceStack stack)
    {
        if (stack.Definition.Sprite == null)
        {
            EnsureFallbackSprite();
        }

        GameObject go = new GameObject($"Resource:{stack.Definition.DisplayName}");
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = stack.Definition.Sprite != null ? stack.Definition.Sprite : fallbackSprite;

        var item = go.AddComponent<ResourceItem>();
        item.Stack = stack;
        item.Reserved = false;
        item.CreateFloatingText();
        return item;
    }

    void OnEnable()
    {
        ResourceLogisticsManager.RegisterItem(this);
    }

    void OnDisable()
    {
        ResourceLogisticsManager.UnregisterItem(this);
    }

    void CreateFloatingText()
    {
        GameObject tObj = new GameObject("AmountText");
        tObj.transform.SetParent(transform, false);
        var tm = tObj.AddComponent<TextMesh>();
        tm.text = $"{Stack.Amount}\n{Stack.Quality.GetDisplayName()}";
        tm.characterSize = 0.08f;
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }

    static void EnsureFallbackSprite()
    {
        if (fallbackSprite != null)
            return;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.55f, 0.27f, 0.07f));
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    public void UpdateAmount(int newAmount)
    {
        Stack = Stack.WithAmount(newAmount);
        var text = GetComponentInChildren<TextMesh>();
        if (text != null)
            text.text = $"{Stack.Amount}\n{Stack.Quality.GetDisplayName()}";
    }
}
