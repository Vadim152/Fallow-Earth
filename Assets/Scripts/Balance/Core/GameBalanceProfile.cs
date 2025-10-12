using System;
using System.Collections.Generic;
using System.Linq;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Collection of tuning values that shape the overall experience for a difficulty level.
    /// </summary>
    public sealed class GameBalanceProfile
    {
        private readonly Dictionary<NeedType, NeedSchedule> pressureSchedules;
        private readonly Dictionary<NeedType, NeedSchedule> recoverySchedules;

        public GameBalanceProfile(
            DifficultyLevel difficulty,
            float resourceYieldMultiplier,
            float resourceConsumptionMultiplier,
            float eventFrequencyMultiplier,
            float storytellerVariance,
            float weatherSeverityMultiplier,
            float dayLengthMultiplier,
            float mentalBreakMoodThreshold,
            float mentalBreakMeanTimeBetweenHours,
            FloatRange clearWeatherDuration,
            FloatRange rainWeatherDuration,
            IEnumerable<NeedScheduleMapping> pressure,
            IEnumerable<NeedScheduleMapping> recovery)
        {
            if (resourceYieldMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(resourceYieldMultiplier));
            if (resourceConsumptionMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(resourceConsumptionMultiplier));
            if (eventFrequencyMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(eventFrequencyMultiplier));
            if (storytellerVariance < 0f)
                throw new ArgumentOutOfRangeException(nameof(storytellerVariance));
            if (weatherSeverityMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(weatherSeverityMultiplier));
            if (dayLengthMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(dayLengthMultiplier));
            if (mentalBreakMoodThreshold <= 0f || mentalBreakMoodThreshold > 1f)
                throw new ArgumentOutOfRangeException(nameof(mentalBreakMoodThreshold));
            if (mentalBreakMeanTimeBetweenHours <= 0f)
                throw new ArgumentOutOfRangeException(nameof(mentalBreakMeanTimeBetweenHours));

            Difficulty = difficulty;
            ResourceYieldMultiplier = resourceYieldMultiplier;
            ResourceConsumptionMultiplier = resourceConsumptionMultiplier;
            EventFrequencyMultiplier = eventFrequencyMultiplier;
            StorytellerVariance = storytellerVariance;
            WeatherSeverityMultiplier = weatherSeverityMultiplier;
            DayLengthMultiplier = dayLengthMultiplier;
            MentalBreakMoodThreshold = mentalBreakMoodThreshold;
            MentalBreakMeanTimeBetweenHours = mentalBreakMeanTimeBetweenHours;
            ClearWeatherDuration = clearWeatherDuration;
            RainWeatherDuration = rainWeatherDuration;

            pressureSchedules = BuildDictionary(pressure);
            recoverySchedules = BuildDictionary(recovery);
        }

        public DifficultyLevel Difficulty { get; }
        public float ResourceYieldMultiplier { get; }
        public float ResourceConsumptionMultiplier { get; }
        public float EventFrequencyMultiplier { get; }
        public float StorytellerVariance { get; }
        public float WeatherSeverityMultiplier { get; }
        public float DayLengthMultiplier { get; }
        public float MentalBreakMoodThreshold { get; }
        public float MentalBreakMeanTimeBetweenHours { get; }
        public FloatRange ClearWeatherDuration { get; }
        public FloatRange RainWeatherDuration { get; }

        public IEnumerable<NeedScheduleMapping> PressureSchedules => pressureSchedules.Select(kvp => new NeedScheduleMapping(kvp.Key, kvp.Value));
        public IEnumerable<NeedScheduleMapping> RecoverySchedules => recoverySchedules.Select(kvp => new NeedScheduleMapping(kvp.Key, kvp.Value));

        public int AdjustProduction(int baseAmount)
        {
            return Math.Max(0, (int)Math.Round(baseAmount * ResourceYieldMultiplier, MidpointRounding.AwayFromZero));
        }

        public int AdjustConsumption(int baseAmount)
        {
            return Math.Max(1, (int)Math.Round(baseAmount * ResourceConsumptionMultiplier, MidpointRounding.AwayFromZero));
        }

        public float EvaluateNeedPressure(NeedType need, float hourOfDay)
        {
            return EvaluateSchedule(pressureSchedules, need, hourOfDay);
        }

        public float EvaluateNeedRecovery(NeedType need, float hourOfDay)
        {
            return EvaluateSchedule(recoverySchedules, need, hourOfDay);
        }

        public float RollEventIntervalHours(Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            // Use an exponential distribution to give rare but high drama spikes.
            double lambda = 1.0 / Math.Max(0.01, EventFrequencyMultiplier * 0.75);
            double value = -Math.Log(1 - random.NextDouble()) / lambda;
            double variance = StorytellerVariance * (random.NextDouble() - 0.5);
            return (float)Math.Max(0.1, value + variance);
        }

        public override string ToString()
        {
            return $"GameBalanceProfile({Difficulty}, yield x{ResourceYieldMultiplier:0.##}, consume x{ResourceConsumptionMultiplier:0.##})";
        }

        private static Dictionary<NeedType, NeedSchedule> BuildDictionary(IEnumerable<NeedScheduleMapping> source)
        {
            var dict = new Dictionary<NeedType, NeedSchedule>();
            if (source == null)
                return dict;
            foreach (var mapping in source)
            {
                if (mapping.Schedule != null)
                    dict[mapping.Type] = mapping.Schedule;
            }
            return dict;
        }

        private static float EvaluateSchedule(Dictionary<NeedType, NeedSchedule> dict, NeedType need, float hourOfDay)
        {
            if (dict.TryGetValue(need, out var schedule))
                return schedule.Evaluate(hourOfDay);
            return 1f;
        }
    }

    /// <summary>
    /// Associates a need type with a schedule curve.
    /// </summary>
    public readonly struct NeedScheduleMapping
    {
        public NeedType Type { get; }
        public NeedSchedule Schedule { get; }

        public NeedScheduleMapping(NeedType type, NeedSchedule schedule)
        {
            Type = type;
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }
    }
}
