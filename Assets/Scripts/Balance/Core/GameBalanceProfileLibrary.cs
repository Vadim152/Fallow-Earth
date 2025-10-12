using System.Collections.Generic;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Provides prebuilt balance profiles inspired by RimWorld style difficulty tiers.
    /// </summary>
    public static class GameBalanceProfileLibrary
    {
        public static IReadOnlyDictionary<DifficultyLevel, GameBalanceProfile> Presets => presets;

        private static readonly Dictionary<DifficultyLevel, GameBalanceProfile> presets = new Dictionary<DifficultyLevel, GameBalanceProfile>
        {
            [DifficultyLevel.Settler] = BuildSettler(),
            [DifficultyLevel.Survivalist] = BuildSurvivalist(),
            [DifficultyLevel.Hardcore] = BuildHardcore()
        };

        private static GameBalanceProfile BuildSettler()
        {
            return new GameBalanceProfile(
                DifficultyLevel.Settler,
                resourceYieldMultiplier: 1.35f,
                resourceConsumptionMultiplier: 0.85f,
                eventFrequencyMultiplier: 0.7f,
                storytellerVariance: 0.35f,
                weatherSeverityMultiplier: 0.85f,
                dayLengthMultiplier: 0.9f,
                mentalBreakMoodThreshold: 0.18f,
                mentalBreakMeanTimeBetweenHours: 36f,
                clearWeatherDuration: new FloatRange(0.8f, 1.4f),
                rainWeatherDuration: new FloatRange(0.6f, 1.1f),
                pressure: new[]
                {
                    Mapping(NeedType.Hunger, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.6f),
                        new NeedSchedulePoint(6f, 0.8f),
                        new NeedSchedulePoint(12f, 1f),
                        new NeedSchedulePoint(18f, 0.7f)
                    })),
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.2f),
                        new NeedSchedulePoint(7f, 0.5f),
                        new NeedSchedulePoint(22f, 0.15f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.6f),
                        new NeedSchedulePoint(8f, 1f),
                        new NeedSchedulePoint(20f, 0.4f)
                    }))
                },
                recovery: new[]
                {
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1.4f),
                        new NeedSchedulePoint(8f, 0.6f),
                        new NeedSchedulePoint(22f, 1.3f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1.2f),
                        new NeedSchedulePoint(12f, 0.8f),
                        new NeedSchedulePoint(18f, 1.1f)
                    }))
                });
        }

        private static GameBalanceProfile BuildSurvivalist()
        {
            return new GameBalanceProfile(
                DifficultyLevel.Survivalist,
                resourceYieldMultiplier: 1f,
                resourceConsumptionMultiplier: 1f,
                eventFrequencyMultiplier: 1f,
                storytellerVariance: 0.55f,
                weatherSeverityMultiplier: 1f,
                dayLengthMultiplier: 1f,
                mentalBreakMoodThreshold: 0.22f,
                mentalBreakMeanTimeBetweenHours: 24f,
                clearWeatherDuration: new FloatRange(0.7f, 1.2f),
                rainWeatherDuration: new FloatRange(0.7f, 1.2f),
                pressure: new[]
                {
                    Mapping(NeedType.Hunger, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.8f),
                        new NeedSchedulePoint(6f, 0.9f),
                        new NeedSchedulePoint(12f, 1.1f),
                        new NeedSchedulePoint(18f, 0.9f)
                    })),
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.3f),
                        new NeedSchedulePoint(7f, 0.6f),
                        new NeedSchedulePoint(23f, 0.25f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.8f),
                        new NeedSchedulePoint(8f, 1.2f),
                        new NeedSchedulePoint(20f, 0.6f)
                    }))
                },
                recovery: new[]
                {
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1.25f),
                        new NeedSchedulePoint(7f, 0.75f),
                        new NeedSchedulePoint(22f, 1.15f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1f),
                        new NeedSchedulePoint(12f, 0.7f),
                        new NeedSchedulePoint(18f, 1.05f)
                    }))
                });
        }

        private static GameBalanceProfile BuildHardcore()
        {
            return new GameBalanceProfile(
                DifficultyLevel.Hardcore,
                resourceYieldMultiplier: 0.8f,
                resourceConsumptionMultiplier: 1.2f,
                eventFrequencyMultiplier: 1.35f,
                storytellerVariance: 0.75f,
                weatherSeverityMultiplier: 1.35f,
                dayLengthMultiplier: 1.2f,
                mentalBreakMoodThreshold: 0.28f,
                mentalBreakMeanTimeBetweenHours: 14f,
                clearWeatherDuration: new FloatRange(0.5f, 1f),
                rainWeatherDuration: new FloatRange(0.9f, 1.6f),
                pressure: new[]
                {
                    Mapping(NeedType.Hunger, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1f),
                        new NeedSchedulePoint(5f, 1.2f),
                        new NeedSchedulePoint(12f, 1.35f),
                        new NeedSchedulePoint(18f, 1.1f)
                    })),
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.45f),
                        new NeedSchedulePoint(6f, 0.8f),
                        new NeedSchedulePoint(23f, 0.35f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1f),
                        new NeedSchedulePoint(8f, 1.4f),
                        new NeedSchedulePoint(20f, 0.7f)
                    })),
                    Mapping(NeedType.Medical, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1.1f),
                        new NeedSchedulePoint(12f, 1.3f),
                        new NeedSchedulePoint(23f, 1.2f)
                    }))
                },
                recovery: new[]
                {
                    Mapping(NeedType.Rest, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 1.1f),
                        new NeedSchedulePoint(7f, 0.65f),
                        new NeedSchedulePoint(22f, 1.05f)
                    })),
                    Mapping(NeedType.Stress, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.85f),
                        new NeedSchedulePoint(12f, 0.6f),
                        new NeedSchedulePoint(18f, 0.95f)
                    })),
                    Mapping(NeedType.Medical, new NeedSchedule(new []
                    {
                        new NeedSchedulePoint(0f, 0.9f),
                        new NeedSchedulePoint(12f, 0.75f),
                        new NeedSchedulePoint(23f, 0.85f)
                    }))
                });
        }

        private static NeedScheduleMapping Mapping(NeedType type, NeedSchedule schedule)
        {
            return new NeedScheduleMapping(type, schedule);
        }
    }
}
