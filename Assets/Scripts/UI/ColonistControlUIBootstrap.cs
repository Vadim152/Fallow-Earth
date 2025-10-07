using UnityEngine;

public class ColonistControlUIBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (Object.FindObjectOfType<ColonistControlUI>() == null)
        {
            new GameObject("ColonistControlUI", typeof(RectTransform), typeof(ColonistControlUI));
        }
    }
}
