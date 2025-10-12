using FallowEarth.Balance;
using NUnit.Framework;

namespace Balance.Tests
{
    public class NeedScheduleTests
    {
        [Test]
        public void Evaluate_ReturnsInterpolatedValue()
        {
            var schedule = new NeedSchedule(new[]
            {
                new NeedSchedulePoint(0f, 0.5f),
                new NeedSchedulePoint(12f, 1f),
                new NeedSchedulePoint(18f, 0.3f)
            });

            Assert.That(schedule.Evaluate(0f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(schedule.Evaluate(12f), Is.EqualTo(1f).Within(0.0001f));

            float midday = schedule.Evaluate(6f);
            Assert.That(midday, Is.EqualTo(0.75f).Within(0.0001f));

            float evening = schedule.Evaluate(15f);
            Assert.That(evening, Is.EqualTo(0.65f).Within(0.0001f));
        }

        [Test]
        public void Evaluate_WrapsAroundMidnight()
        {
            var schedule = new NeedSchedule(new[]
            {
                new NeedSchedulePoint(6f, 0.2f),
                new NeedSchedulePoint(18f, 0.8f)
            });

            Assert.That(schedule.Evaluate(30f), Is.EqualTo(schedule.Evaluate(6f)).Within(0.0001f));
            Assert.That(schedule.Evaluate(-6f), Is.EqualTo(schedule.Evaluate(18f)).Within(0.0001f));
        }
    }
}
