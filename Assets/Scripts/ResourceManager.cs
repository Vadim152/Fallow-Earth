using UnityEngine;

/// <summary>
/// Simple global resource tracker.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public int Wood { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void AddWood(int amount)
    {
        if (Instance == null) return;
        Instance.Wood += amount;
    }

    public static bool UseWood(int amount)
    {
        if (Instance == null) return false;
        if (Instance.Wood < amount)
            return false;
        Instance.Wood -= amount;
        return true;
    }
}
