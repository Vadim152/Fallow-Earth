using System;
using System.Collections;
using FallowEarth.Saving;
using UnityEngine;

/// <summary>
/// Simple door that opens when a colonist enters and closes after a delay.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Door : SaveableMonoBehaviour
{
    static Sprite doorSprite;
    public float closeDelay = 1f;
    public bool holdOpen = false; // if true the door remains open

    SpriteRenderer sr;
    Coroutine closeRoutine;

    protected override void Awake()
    {
        base.Awake();
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
        if (holdOpen)
            return;
        if (closeRoutine != null)
            StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CloseAfterDelay());
    }

    void Open()
    {
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
        sr.color = Color.white;
    }

    void OnMouseDown()
    {
        holdOpen = !holdOpen;
        if (holdOpen)
            Open();
        else if (closeRoutine == null)
            sr.color = Color.white;
    }

    public override SaveCategory Category => SaveCategory.Structure;

    [Serializable]
    private struct DoorSaveState
    {
        public Vector3 position;
        public float closeDelay;
        public bool holdOpen;
    }

    public override void PopulateSaveData(SaveData saveData)
    {
        saveData.Set("door", new DoorSaveState
        {
            position = transform.position,
            closeDelay = closeDelay,
            holdOpen = holdOpen
        });
    }

    public override void LoadFromSaveData(SaveData saveData)
    {
        if (saveData.TryGet("door", out DoorSaveState state))
        {
            transform.position = state.position;
            closeDelay = state.closeDelay;
            holdOpen = state.holdOpen;
            if (holdOpen)
                Open();
            else
                sr.color = Color.white;
        }
    }
}
