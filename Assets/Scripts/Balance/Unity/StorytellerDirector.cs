using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Lightweight storyteller that schedules colony events based on the active balance profile.
    /// </summary>
    public class StorytellerDirector : MonoBehaviour
    {
        [SerializeField] private string storytellerName = "Эхо Земли";
        [SerializeField, TextArea] private List<string> narrativeTemplates = new List<string>
        {
            "{0} наблюдает за колонистами и готовит новое испытание.",
            "{0} считает, что сейчас подходящее время встряхнуть рутину.",
            "{0} предвкушает драму: на горизонте новая история.",
            "{0} шепчет о переменах в ветрах судьбы."
        };

        private float timer;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            ScheduleNext();
            if (GameBalanceManager.Instance != null)
                GameBalanceManager.Instance.ProfileChanged += HandleProfileChanged;
        }

        void OnDestroy()
        {
            if (GameBalanceManager.Instance != null)
                GameBalanceManager.Instance.ProfileChanged -= HandleProfileChanged;
        }

        void Update()
        {
            if (GameBalanceManager.Instance == null)
                return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                TriggerStoryEvent();
                ScheduleNext();
            }
        }

        void HandleProfileChanged(GameBalanceProfile profile)
        {
            EventConsole.Log("Storyteller", $"Профиль {profile.Difficulty} активирован. Корректируем частоту событий.");
            ScheduleNext();
        }

        void ScheduleNext()
        {
            if (GameBalanceManager.Instance == null)
            {
                timer = 60f;
                return;
            }
            float hours = GameBalanceManager.Instance.SampleEventIntervalHours();
            float secondsPerHour = DayNightCycle.Instance != null ? (DayNightCycle.Instance.minutesPerDay * 60f / 24f) : 60f;
            secondsPerHour = Mathf.Max(1f, secondsPerHour / GameBalanceManager.Instance.DayLengthMultiplier);
            timer = Mathf.Max(10f, hours * secondsPerHour);
        }

        void TriggerStoryEvent()
        {
            if (narrativeTemplates.Count == 0)
                narrativeTemplates.Add("{0} задумался о будущем колонии.");

            string template = narrativeTemplates[Random.Range(0, narrativeTemplates.Count)];
            string message = string.Format(template, storytellerName);
            EventConsole.Log("Storyteller", message);
        }
    }
}
