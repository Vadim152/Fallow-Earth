using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Runtime facade that exposes the currently selected balance profile to the rest of the simulation.
    /// </summary>
    public class GameBalanceManager : MonoBehaviour
    {
        public static GameBalanceManager Instance { get; private set; }

        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Survivalist;
        [SerializeField, Tooltip("Optional multiplier applied after preset selection to support fine tuning during playtests.")]
        private float economyOffset = 1f;
        [SerializeField] private float storytellerOffset = 1f;
        [SerializeField] private float weatherOffset = 1f;
        [SerializeField] private float dayLengthOffset = 1f;

        private readonly Dictionary<DifficultyLevel, GameBalanceProfile> customProfiles = new Dictionary<DifficultyLevel, GameBalanceProfile>();
        private System.Random rng;

        public event Action<GameBalanceProfile> ProfileChanged;

        public GameBalanceProfile CurrentProfile { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            rng = new System.Random(Environment.TickCount);
            RebuildProfile();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnValidate()
        {
            economyOffset = Mathf.Clamp(economyOffset, 0.5f, 1.5f);
            storytellerOffset = Mathf.Clamp(storytellerOffset, 0.5f, 1.5f);
            weatherOffset = Mathf.Clamp(weatherOffset, 0.5f, 1.8f);
            dayLengthOffset = Mathf.Clamp(dayLengthOffset, 0.5f, 1.5f);
            if (Application.isPlaying)
                RebuildProfile();
        }

        public void SetDifficulty(DifficultyLevel newDifficulty)
        {
            if (difficulty == newDifficulty)
                return;
            difficulty = newDifficulty;
            RebuildProfile();
        }

        public void OverrideProfile(DifficultyLevel level, GameBalanceProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            customProfiles[level] = profile;
            if (difficulty == level)
                RebuildProfile();
        }

        public int ApplyProductionMultiplier(int baseAmount)
        {
            if (CurrentProfile == null)
                return baseAmount;
            return CurrentProfile.AdjustProduction(baseAmount);
        }

        public int ApplyConsumptionMultiplier(int baseAmount)
        {
            if (CurrentProfile == null)
                return baseAmount;
            return CurrentProfile.AdjustConsumption(baseAmount);
        }

        public float EvaluateNeedPressure(NeedType need, float hourOfDay)
        {
            return CurrentProfile?.EvaluateNeedPressure(need, hourOfDay) ?? 1f;
        }

        public float EvaluateNeedRecovery(NeedType need, float hourOfDay)
        {
            return CurrentProfile?.EvaluateNeedRecovery(need, hourOfDay) ?? 1f;
        }

        public float SampleEventIntervalHours()
        {
            if (CurrentProfile == null)
                return 1f;
            return CurrentProfile.RollEventIntervalHours(rng) * storytellerOffset;
        }

        public float WeatherSeverityMultiplier => (CurrentProfile?.WeatherSeverityMultiplier ?? 1f) * weatherOffset;

        public FloatRange ClearWeatherDuration
        {
            get
            {
                if (CurrentProfile == null)
                    return new FloatRange(0.8f, 1.2f);
                var range = CurrentProfile.ClearWeatherDuration;
                return new FloatRange(range.Min / weatherOffset, range.Max / weatherOffset);
            }
        }

        public FloatRange RainWeatherDuration
        {
            get
            {
                if (CurrentProfile == null)
                    return new FloatRange(0.8f, 1.2f);
                var range = CurrentProfile.RainWeatherDuration;
                return new FloatRange(range.Min * weatherOffset, range.Max * weatherOffset);
            }
        }

        public float DayLengthMultiplier => (CurrentProfile?.DayLengthMultiplier ?? 1f) * dayLengthOffset;

        public bool ShouldEnterMentalBreak(float mood, float stress, float deltaTime)
        {
            if (CurrentProfile == null)
                return mood < 0.2f || stress > 0.95f;

            if (mood >= CurrentProfile.MentalBreakMoodThreshold)
                return false;

            float severity = Mathf.InverseLerp(0f, CurrentProfile.MentalBreakMoodThreshold, mood);
            float moodFactor = Mathf.Clamp01(1f - severity);
            float stressFactor = Mathf.Clamp01((stress - 0.65f) / 0.35f);
            float combined = Mathf.Max(moodFactor, stressFactor);
            float mtbSeconds = CurrentProfile.MentalBreakMeanTimeBetweenHours * 3600f;
            float chance = deltaTime / Mathf.Max(1f, mtbSeconds);
            chance *= Mathf.Lerp(0.2f, 1.5f, combined);
            return UnityEngine.Random.value < chance;
        }

        private void RebuildProfile()
        {
            GameBalanceProfile baseProfile;
            if (customProfiles.TryGetValue(difficulty, out var custom))
                baseProfile = custom;
            else
                baseProfile = GameBalanceProfileLibrary.Presets.TryGetValue(difficulty, out var preset) ? preset : null;

            if (baseProfile == null)
            {
                Debug.LogWarning($"No balance profile found for {difficulty}, falling back to Survivalist.");
                baseProfile = GameBalanceProfileLibrary.Presets[DifficultyLevel.Survivalist];
            }

            if (!Mathf.Approximately(economyOffset, 1f) || !Mathf.Approximately(storytellerOffset, 1f) ||
                !Mathf.Approximately(weatherOffset, 1f) || !Mathf.Approximately(dayLengthOffset, 1f))
            {
                baseProfile = new GameBalanceProfile(
                    baseProfile.Difficulty,
                    baseProfile.ResourceYieldMultiplier * economyOffset,
                    baseProfile.ResourceConsumptionMultiplier / economyOffset,
                    baseProfile.EventFrequencyMultiplier * storytellerOffset,
                    baseProfile.StorytellerVariance,
                    baseProfile.WeatherSeverityMultiplier * weatherOffset,
                    baseProfile.DayLengthMultiplier * dayLengthOffset,
                    baseProfile.MentalBreakMoodThreshold,
                    baseProfile.MentalBreakMeanTimeBetweenHours / storytellerOffset,
                    baseProfile.ClearWeatherDuration,
                    baseProfile.RainWeatherDuration,
                    baseProfile.PressureSchedules,
                    baseProfile.RecoverySchedules);
            }

            CurrentProfile = baseProfile;
            EventConsole.Log("Balance", $"Активирован профиль сложности: {difficulty} ({CurrentProfile}).");
            ProfileChanged?.Invoke(CurrentProfile);
        }
    }
}
