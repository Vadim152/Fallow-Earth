using System.Text;
using FallowEarth.Infrastructure;
using FallowEarth.ResourcesSystem;
using UnityEngine;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Designer facing overlay with live metrics for tuning balance, needs and weather.
    /// </summary>
    public class DesignDebugOverlay : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool visibleInReleaseBuild = false;
        [SerializeField] private float panelWidth = 360f;

        private bool visible = true;
        private Vector2 scroll;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
#if !UNITY_EDITOR
            if (!visibleInReleaseBuild)
                visible = false;
#endif
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible)
                return;

            GUILayout.BeginArea(new Rect(10f, 10f, panelWidth, Screen.height - 20f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            DrawHeader("Баланс колонии");
            if (GameBalanceManager.Instance != null && GameBalanceManager.Instance.CurrentProfile != null)
            {
                var profile = GameBalanceManager.Instance.CurrentProfile;
                GUILayout.Label($"Сложность: {profile.Difficulty}");
                GUILayout.Label($"Экономика: +{(profile.ResourceYieldMultiplier - 1f) * 100f:0.#}% добычи / {(profile.ResourceConsumptionMultiplier - 1f) * 100f:0.#}% потребления");
                GUILayout.Label($"Частота событий: x{profile.EventFrequencyMultiplier:0.00}");
                GUILayout.Label($"Погода: x{profile.WeatherSeverityMultiplier:0.00}");
                GUILayout.Label($"Длина дня: x{profile.DayLengthMultiplier:0.00}");
                GUILayout.Space(6f);
            }
            else
            {
                GUILayout.Label("Профиль баланса недоступен");
            }

            DrawHeader("Ресурсы");
            if (GameServices.TryResolve(out IResourceManager resourceManager))
            {
                var snapshot = resourceManager.GetSnapshot();
                foreach (var entry in snapshot.Entries)
                {
                    GUILayout.Label($"- {entry.Definition.DisplayName} ({entry.Quality}): {entry.Amount}");
                }
                GUILayout.Space(6f);
            }
            else
            {
                GUILayout.Label("Менеджер ресурсов отсутствует");
            }

            DrawHeader("Погода и время");
            if (WeatherSystem.Instance != null)
            {
                GUILayout.Label(WeatherSystem.Instance.IsRaining ? "Сейчас дождь" : "Ясно");
            }
            if (DayNightCycle.Instance != null)
            {
                GUILayout.Label($"Час дня: {DayNightCycle.Instance.CurrentHour:0.0}");
            }

            GUILayout.Space(8f);
            DrawHeader("Консоль событий");
            var builder = new StringBuilder();
            foreach (var entry in EventConsole.Entries)
            {
                builder.AppendLine($"[{entry.LocalTime:HH:mm:ss}] {entry.Category}: {entry.Message}");
            }
            GUILayout.TextArea(builder.ToString(), GUILayout.Height(220f));

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Label($"<b>{title}</b>", new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                richText = true
            });
        }
    }
}
