using System;

namespace FallowEarth.Balance
{
    /// <summary>
    /// Inclusive range helper for tuning values without depending on Unity specific math helpers.
    /// </summary>
    public readonly struct FloatRange
    {
        public float Min { get; }
        public float Max { get; }

        public FloatRange(float min, float max)
        {
            if (float.IsNaN(min) || float.IsNaN(max))
                throw new ArgumentException("Range values cannot be NaN");
            if (max < min)
                throw new ArgumentException("Range maximum must be greater than or equal to minimum", nameof(max));
            Min = min;
            Max = max;
        }

        public float Clamp(float value)
        {
            if (value < Min)
                return Min;
            if (value > Max)
                return Max;
            return value;
        }

        public float Lerp(float t)
        {
            return Min + (Max - Min) * Clamp01(t);
        }

        public float Width => Max - Min;

        public float Sample(Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (Math.Abs(Max - Min) < float.Epsilon)
                return Min;
            return (float)(Min + random.NextDouble() * (Max - Min));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
