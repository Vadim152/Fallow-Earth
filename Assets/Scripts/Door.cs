using System.Collections;
using UnityEngine;

/// <summary>
/// Simple door that opens when a colonist enters and closes after a delay.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Door : MonoBehaviour
{
    static Sprite doorSprite;
    public float closeDelay = 1f;

    bool open;
    SpriteRenderer sr;
    Coroutine closeRoutine;

    void Awake()
    {
        if (doorSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.6f, 0.4f, 0.2f));
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            doorSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = doorSprite;
        sr.sortingOrder = 6;

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    public static Door Create(Vector2 position)
    {
        GameObject go = new GameObject("Door");
        go.transform.position = position;
        return go.AddComponent<Door>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Colonist>() == null)
            return;
        Open();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Colonist>() == null)
            return;
        if (closeRoutine != null)
            StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CloseAfterDelay());
    }

    void Open()
    {
        open = true;
        sr.color = new Color(1f, 1f, 1f, 0.3f);
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
    }

    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        open = false;
        sr.color = Color.white;
    }
}
