using UnityEngine;

public static class AspectRatioEnforcer
{
    public static void Enforce(Camera cam, float targetAspect)
    {
        if (cam == null)
            return;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;
        Rect rect = cam.rect;

        if (scale < 1f)
        {
            rect.width = 1f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1f - scale) / 2f;
        }
        else
        {
            float scalewidth = 1f / scale;
            rect.width = scalewidth;
            rect.height = 1f;
            rect.x = (1f - scalewidth) / 2f;
            rect.y = 0f;
        }

        cam.rect = rect;
        cam.aspect = targetAspect;
    }
}
