using UnityEngine;

public static class WeatherBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<WeatherSystem>() == null)
            new GameObject("WeatherSystem").AddComponent<WeatherSystem>();
    }
}
