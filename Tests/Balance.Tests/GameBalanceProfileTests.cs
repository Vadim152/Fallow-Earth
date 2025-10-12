using System.Linq;
using FallowEarth.Balance;
using NUnit.Framework;

namespace Balance.Tests
{
    public class GameBalanceProfileTests
    {
        [Test]
        public void AdjustProductionAndConsumptionRespectMultipliers()
        {
            var profile = GameBalanceProfileLibrary.Presets[DifficultyLevel.Hardcore];
            int production = profile.AdjustProduction(10);
            int consumption = profile.AdjustConsumption(10);

            Assert.That(production, Is.LessThan(10));
            Assert.That(consumption, Is.GreaterThan(10));
        }

        [Test]
        public void EvaluateNeedPressureUsesSchedule()
        {
            var profile = GameBalanceProfileLibrary.Presets[DifficultyLevel.Survivalist];
            float morningPressure = profile.EvaluateNeedPressure(NeedType.Rest, 6f);
            float nightPressure = profile.EvaluateNeedPressure(NeedType.Rest, 23f);

            Assert.That(morningPressure, Is.GreaterThan(nightPressure));
        }

        [Test]
        public void RollEventIntervalProducesPositiveNumbers()
        {
            var profile = GameBalanceProfileLibrary.Presets[DifficultyLevel.Settler];
            var rng = new System.Random(1234);
            var values = Enumerable.Range(0, 100).Select(_ => profile.RollEventIntervalHours(rng)).ToArray();

            Assert.That(values.All(v => v > 0f), Is.True);
            Assert.That(values.Average(), Is.GreaterThan(0.1));
        }
    }
}
