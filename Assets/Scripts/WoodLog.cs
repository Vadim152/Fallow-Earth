using FallowEarth.ResourcesSystem;
using UnityEngine;

/// <summary>
/// Legacy helper that spawns a wood resource stack in the world.
/// Redirects to the generic resource item system.
/// </summary>
public class WoodLog : MonoBehaviour
{
    public int Amount => resourceItem != null ? resourceItem.Stack.Amount : 0;
    public bool Reserved
    {
        get => resourceItem != null && resourceItem.Reserved;
        set
        {
            if (resourceItem != null)
                resourceItem.Reserved = value;
        }
    }

    ResourceItem resourceItem;

    public static WoodLog Create(Vector2 position, int amount, ResourceQuality quality = ResourceQuality.Common)
    {
        ResourceRegistry.EnsureInitialized();
        var def = ResourceRegistry.GetOrThrow(DefaultResourceIds.Wood);
        var stack = new ResourceStack(def, quality, amount);
        ResourceItem item = ResourceItem.Create(position, stack);
        var wrapper = item.gameObject.AddComponent<WoodLog>();
        wrapper.resourceItem = item;
        return wrapper;
    }
}
