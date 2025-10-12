using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a simple placeholder research interface with simulated progress so
/// the research tab feels alive even without a full research backend.
/// </summary>
public class ResearchMenuController : MonoBehaviour
{
    private ManagementTabController tabs;
    private Text activeProjectText;
    private Slider progressSlider;
    private float simulatedProgress;
    private float progressSpeed = 0.02f;
    private int projectIndex;

    private readonly string[] projects = new[]
    {
        "\u0411\u0430\u0437\u043e\u0432\u0430\u044f \u044d\u043d\u0435\u0440\u0433\u0435\u0442\u0438\u043a\u0430",
        "\u041f\u043e\u043b\u0435\u0432\u0430\u044f \u043a\u0443\u0445\u043d\u044f",
        "\u0410\u0440\u043c\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u044b\u0435 \u0441\u0442\u0435\u043d\u044b",
        "\u041c\u0435\u0434\u0442\u0435\u0445\u043d\u0438\u043a\u0430"
    };

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        GameObject section = tabs.CreateSection(ManagementTabController.ResearchTabId, "\u0418\u0441\u0441\u043b\u0435\u0434\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0441\u043a\u0438\u0439 \u0446\u0435\u043d\u0442\u0440");
        activeProjectText = tabs.CreateLabel(section.transform, FormatProjectLabel(), TextAnchor.MiddleLeft, 18, FontStyle.Bold);
        tabs.CreateLabel(section.transform, "\u0417\u0430\u043f\u043b\u0430\u043d\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u044b\u0435 \u043e\u043f\u044b\u0442\u044b \u0441\u0442\u0438\u043c\u0443\u043b\u0438\u0440\u0443\u044e\u0442 \u043c\u043e\u0440\u0430\u043b\u044c \u043a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u043e\u0432.", TextAnchor.MiddleLeft, 14);
        progressSlider = tabs.CreateProgressBar(section.transform.gameObject);
        progressSlider.value = 0.1f;
    }

    void Update()
    {
        if (progressSlider == null)
            return;

        simulatedProgress += Time.unscaledDeltaTime * progressSpeed;
        if (simulatedProgress >= 1f)
        {
            EventLogUI.AddEntry($"\u0418\u0441\u0441\u043b\u0435\u0434\u043e\u0432\u0430\u043d\u0438\u0435 \"{projects[projectIndex]}\" \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u043e.");
            simulatedProgress = 0f;
            progressSpeed = Random.Range(0.01f, 0.03f);
            projectIndex = (projectIndex + 1) % projects.Length;
            if (activeProjectText != null)
                activeProjectText.text = FormatProjectLabel();
        }
        progressSlider.value = Mathf.Clamp01(simulatedProgress);
    }

    string FormatProjectLabel()
    {
        string project = projects[Mathf.Clamp(projectIndex, 0, projects.Length - 1)];
        return $"\u0422\u0435\u043a\u0443\u0449\u0435\u0435 \u0438\u0441\u0441\u043b\u0435\u0434\u043e\u0432\u0430\u043d\u0438\u0435: {project}";
    }
}
