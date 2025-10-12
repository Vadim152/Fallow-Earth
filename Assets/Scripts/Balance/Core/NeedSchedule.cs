using System;
using System.Collections.Generic;
using System.Linq;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Timetable style multiplier that modulates how quickly a need builds up or recovers across the day.
    /// </summary>
    public sealed class NeedSchedule
    {
        private readonly List<NeedSchedulePoint> points;

        public NeedSchedule(IEnumerable<NeedSchedulePoint> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            this.points = points
                .Select(NormalisePoint)
                .GroupBy(p => p.Hour)
                .Select(g => g.Last())
                .OrderBy(p => p.Hour)
                .ToList();

            if (this.points.Count == 0)
                throw new ArgumentException("Schedule requires at least one point", nameof(points));
        }

        public float Evaluate(float hourOfDay)
        {
            if (points.Count == 1)
                return points[0].Multiplier;

            float wrappedHour = WrapHour(hourOfDay);

            for (int i = 0; i < points.Count; i++)
            {
                var current = points[i];
                var next = points[(i + 1) % points.Count];

                float start = current.Hour;
                float end = next.Hour;
                float target = wrappedHour;

                if (i == points.Count - 1)
                {
                    end += 24f;
                    if (target < start)
                        target += 24f;
                }
                else if (target < start)
                {
                    continue;
                }

                if (target <= end)
                {
                    float t = (Math.Abs(end - start) < float.Epsilon) ? 0f : (target - start) / (end - start);
                    return Lerp(current.Multiplier, next.Multiplier, t);
                }
            }

            return points[0].Multiplier;
        }

        private static float WrapHour(float hour)
        {
            float wrapped = hour % 24f;
            if (wrapped < 0f)
                wrapped += 24f;
            return wrapped;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static NeedSchedulePoint NormalisePoint(NeedSchedulePoint point)
        {
            float hour = point.Hour % 24f;
            if (hour < 0f)
                hour += 24f;
            return new NeedSchedulePoint(hour, Clamp01(point.Multiplier));
        }
    }

    /// <summary>
    /// Serializable container describing a key hour / multiplier pair for a schedule.
    /// </summary>
    public readonly struct NeedSchedulePoint
    {
        public float Hour { get; }
        public float Multiplier { get; }

        public NeedSchedulePoint(float hour, float multiplier)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour))
                throw new ArgumentException("Hour must be a finite number", nameof(hour));
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                throw new ArgumentException("Multiplier must be a finite number", nameof(multiplier));
            Hour = hour;
            Multiplier = multiplier;
        }
    }
}
