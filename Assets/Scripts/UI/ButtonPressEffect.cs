using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Adds a quick scale animation when pressing UI buttons for better feedback.
/// </summary>
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.9f;
    public float animTime = 0.1f;

    RectTransform rect;
    Coroutine routine;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(1f);
    }

    void AnimateTo(float target)
    {
        if (rect == null)
            return;
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ScaleRoutine(target));
    }

    IEnumerator ScaleRoutine(float target)
    {
        Vector3 start = rect.localScale;
        Vector3 end = Vector3.one * target;
        float t = 0f;
        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(start, end, t / animTime);
            yield return null;
        }
        rect.localScale = end;
    }
}
