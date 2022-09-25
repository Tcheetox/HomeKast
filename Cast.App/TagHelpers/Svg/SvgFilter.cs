namespace Cast.App.TagHelpers.Svg
{
    public class SvgFilter
    {
        public readonly SvgColor TargetColor;
        public readonly (double H, double S, double L) TargetHslColor;
        public readonly SvgColor ReusedColor = new(0, 0, 0);

        public SvgFilter(SvgColor color)
        {
            TargetColor = color;
            TargetHslColor = TargetColor.Hsl();
        }

        public (double[] Values, double Loss, string Filter) Solve()
        {
            var wideResults = SolveWide();
            var (Best, BestLoss) = SolveNarrow(wideResults.Best, wideResults.Loss);
            return (Best, BestLoss, Css(Best));
        }

        #region Private Members
        private static string Css(double[] filters)
        {
            double Fmt(int idx, double multiplier = 1)
                => Math.Round(filters[idx] * multiplier);

            return $"filter: invert({Fmt(0)}%) sepia({Fmt(1)}%) saturate({Fmt(2)}%) hue-rotate({Fmt(3, 3.6)}deg) brightness({Fmt(4)}%) contrast({Fmt(5)}%);";
        }

        private (double[] Best, double Loss) SolveWide()
        {
            var A = 5;
            var c = 15;
            var a = new double[] { 60, 180, 18000, 600, 1.2, 1.2 };

            double[] best = new double[6];
            double loss = double.MaxValue;
            for (int i = 0; loss > 25 && i < 3; i++)
            {
                var initial = new double[] { 50, 20, 3750, 50, 100, 100 };
                var result = Spsa(A, a, c, initial, 1000);
                if (result.Loss < loss)
                {
                    best = result.Best;
                    loss = result.Loss;
                }
            }

            return (best, loss);
        }

        private (double[] Best, double Loss) SolveNarrow(double[] values, double loss)
        {
            var A = loss;
            var c = 2;
            var A1 = A + 1;
            var a = new double[] { 0.25 * A1, 0.25 * A1, A1, 0.25 * A1, 0.2 * A1, 0.2 * A1 };
            return Spsa(A, a, c, values, 500);
        }

        private double Loss(double[] filters)
        {
            var color = ReusedColor;
            color.SetColor(0, 0, 0);

            color.Invert(filters[0] / 100);
            color.Sepia(filters[1] / 100);
            color.Saturate(filters[2] / 100);
            color.HueRotate(filters[3] * 3.6);
            color.Brightness(filters[4] / 100);
            color.Contrast(filters[5] / 100);

            var (h, s, l) = color.Hsl();
            return
                Math.Abs(color.Red - TargetColor.Red)
                + Math.Abs(color.Green - TargetColor.Green)
                + Math.Abs(color.Blue - TargetColor.Blue)
                + Math.Abs(h - TargetHslColor.H)
                + Math.Abs(s - TargetHslColor.S)
                + Math.Abs(l - TargetHslColor.L);
        }

        private (double[] Best, double Loss) Spsa(double A, double[] a, double c, double[] values, int iterations)
        {
            var alpha = 1d;
            var gamma = 1/6;

            double[] best = new double[6];
            double bestLoss = double.MaxValue;

            var deltas = new double[6];
            var highArgs = new double[6];
            var lowArgs = new double[6];

            var rnd = new Random();
            for (int k = 0; k < iterations; k++)
            {
                var ck = c / Math.Pow(k + 1, gamma);
                for (int i = 0; i < 6; i++)
                {
                    deltas[i] = rnd.NextDouble() > 0.5 ? 1 : -1;
                    highArgs[i] = values[i] + ck * deltas[i];
                    lowArgs[i] = values[i] - ck * deltas[i];
                }

                var lossDiff = Loss(highArgs) - Loss(lowArgs);
                for (int i = 0; i < 6; i++)
                {
                    var g = (lossDiff / (2 * ck)) * deltas[i];
                    var ak = a[i] / Math.Pow(A + k + 1, alpha);
                    values[i] = Fix(values[i] - ak * g, i);
                }

                var loss = Loss(values);
                if (loss < bestLoss)
                {
                    values.CopyTo(best, 0);
                    bestLoss = loss;
                }
            }

            return (best, bestLoss);
        }

        private static double Fix(double value, int idx)
        {
            var max = 100;
            if (idx == 2)
                max = 7500;
            else if (idx == 4 || idx == 5)
                max = 200;

            if (idx == 3)
            {
                if (value > max)
                    value %= max;
                else if (value < 0)
                    value = max + (value % max);
            }
            else if (value < 0)
                value = 0;
            else if (value > max)
                value = max;

            return value;
        }
        #endregion
    }
}
