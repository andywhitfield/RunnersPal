using NUnit.Framework;
using RunnersPal.Calculators;

namespace RunnersPal.Tests
{
    [TestFixture]
    public class PaceCalculatorTest
    {
        [Test]
        public void Given_5_min_per_mile_should_convert_to_8_min_km()
        {
            var calc = new PaceCalculator();
            var pace = new PaceData { Pace = "5:00", Calc = "pacemilesunits" };
            calc.Calculate(pace);
            Assert.AreEqual("8:02", pace.Pace); // 5 min/mile is actually a little over 8 min/km

            pace.Calc = "pacekmunits";
            calc.Calculate(pace);
            Assert.AreEqual("4:59", pace.Pace); // and then due to rounding, converting back gives just under 5min/mile
        }
    }
}
