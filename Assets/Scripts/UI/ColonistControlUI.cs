using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColonistControlUI : MonoBehaviour
{
    public RectTransform buttonContainer;
    public Button buttonPrefab;

    private List<Colonist> colonists = new List<Colonist>();
    private Colonist selected;

    void Start()
    {
        colonists.AddRange(FindObjectsOfType<Colonist>());
        RefreshButtons();
    }

    void RefreshButtons()
    {
        foreach (Transform t in buttonContainer)
            Destroy(t.gameObject);

        foreach (var c in colonists)
        {
            var btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<Text>().text = c.name;
            btn.onClick.AddListener(() => { selected = c; });
        }
    }

    void Update()
    {
        if (selected != null && Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            selected.SetTask(new Task(world));
            selected = null;
        }
    }
}
