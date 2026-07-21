using System.Globalization;
using System.Threading;
using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    /// <summary>
    /// Number-to-string conversion is script-visible, so it must not follow the
    /// host's regional settings: a script has to compute the same thing on every
    /// machine, and TickLUA's own parser only accepts invariant numerals.
    /// </summary>
    internal class NumberFormattingTests
    {
        /// <summary>
        /// Runs <paramref name="body"/> under a culture whose decimal separator
        /// is a comma — the setting that used to leak into script output.
        /// </summary>
        private static void InCommaDecimalCulture(TestDelegate body)
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                body();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void Concat_UsesInvariantDecimalSeparator()
        {
            InCommaDecimalCulture(() =>
            {
                var vm = Utils.Run("return '' .. (0.1 + 0.2)", 1000);
                Utils.AssertStringResult(vm, "0.3");
            });
        }

        [Test]
        public void ToStringObject_UsesInvariantDecimalSeparator()
        {
            InCommaDecimalCulture(() =>
            {
                Assert.That(new NumberObject(1.5f).ToStringObject().Value, Is.EqualTo("1.5"));
                Assert.That(new NumberObject(1.5f).ToString(), Is.EqualTo("1.5"));
            });
        }

        [Test]
        public void NumberSurvivesAStringRoundTrip()
        {
            // The bug that made this more than cosmetic: formatting produced a
            // comma that the invariant parser then refused, so a number could
            // not survive its own tostring/tonumber cycle.
            InCommaDecimalCulture(() =>
            {
                var vm = Utils.Run("return ('' .. 2.5) + 1", 1000);
                Utils.AssertFloatResult(vm, 3.5f);
            });
        }

        [Test]
        public void IntegralValuesHaveNoDecimalPart()
        {
            var vm = Utils.Run("return '' .. 3.0, '' .. (10 // 3)", 1000);

            Utils.AssertStringResult(vm, "3");
            Utils.AssertStringResult(vm, "3", 1);
        }
    }
}
