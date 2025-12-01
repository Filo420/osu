// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestScenePercentageCounter : OsuTestScene
    {

        private TestPercentageCounter counter;


        [SetUpSteps]
        public void SetUpSteps()
        {
            createCounter();
        }


        [Test]
        [TestCase(50, 100, 0.5f)]       // normal fraction
        [TestCase(123, 0, 1.0f)]        // divide by zero
        [TestCase(10, 1e-12f, 1.0f)]    // denominator below epsilon
        public void TestSetFraction(float numerator, float denumerator, double expected)
        {
            AddStep("set fraction", () => counter.SetFraction(numerator, denumerator));
            AddAssert("fraction value is correct", () => counter.Current.Value == expected);
        }

        [Test]
        [TestCase(0.5, 1.0, 18750)]    // positive difference
        [TestCase(0.75, 0.75, 0)]      // zero difference
        public void TestGetProportionalDuration(double a, double b, double expected)
        {
            double duration = -1;
            AddStep("calculate duration", () => duration = counter.InvokeGetDuration(a, b));
            AddAssert("duration is correct", () => expected == duration);
        }

        [Test]
        public void TestGetProportionalDurationSymmetric()
        {
            double a = -1;
            double b = -2;

            AddStep("calculate using values a and b", () => a = counter.InvokeGetDuration(0.2, 0.8));
            AddStep("calculate using values b and a", () => b = counter.InvokeGetDuration(0.8, 0.2));

            AddAssert("duration are equal", () => a == b);
        }

        private void createCounter()
        {
            AddStep("create counter", () =>
            {
                counter = new TestPercentageCounter();
            });
        }

        private partial class TestPercentageCounter : PercentageCounter
        {
            public double InvokeGetDuration(double currentValue, double newValue)
                => GetProportionalDuration(currentValue, newValue);
        }


    }
}
