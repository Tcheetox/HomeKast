namespace Cast.App.TagHelpers.Svg
{
    public class SvgColor
    {
        public double Red { get; private set; }
        public double Green { get; private set; }
        public double Blue { get; private set; }

        public SvgColor(double red, double green, double blue)
        {
            SetColor(red, green, blue);
        }

        public override string ToString()
            => $"rgb(${Math.Round(Red)}, ${Math.Round(Green)}, ${Math.Round(Blue)})";

        public void SetColor(double red, double green, double blue)
        {
            Red = Clamp(red);
            Green = Clamp(green);
            Blue = Clamp(blue);
        }

        public void HueRotate(double angle = 0)
        {
            angle = angle / 180 * Math.PI;
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            Multiply(new double[] {
                0.213 + cos * 0.787 - sin * 0.213,
                0.715 - cos * 0.715 - sin * 0.715,
                0.072 - cos * 0.072 + sin * 0.928,
                0.213 - cos * 0.213 + sin * 0.143,
                0.715 + cos * 0.285 + sin * 0.140,
                0.072 - cos * 0.072 - sin * 0.283,
                0.213 - cos * 0.213 - sin * 0.787,
                0.715 - cos * 0.715 + sin * 0.715,
                0.072 + cos * 0.928 + sin * 0.072
            });
        }

        public void Sepia(double value = 1)
            => Multiply(new double[] {
                0.393 + 0.607 * (1 - value),
                0.769 - 0.769 * (1 - value),
                0.189 - 0.189 * (1 - value),
                0.349 - 0.349 * (1 - value),
                0.686 + 0.314 * (1 - value),
                0.168 - 0.168 * (1 - value),
                0.272 - 0.272 * (1 - value),
                0.534 - 0.534 * (1 - value),
                0.131 + 0.869 * (1 - value),
            });

        public void Grayscale(double value = 1)
            => Multiply(new double[] {
                0.2126 + 0.7874 * (1 - value),
                0.7152 - 0.7152 * (1 - value),
                0.0722 - 0.0722 * (1 - value),
                0.2126 - 0.2126 * (1 - value),
                0.7152 + 0.2848 * (1 - value),
                0.0722 - 0.0722 * (1 - value),
                0.2126 - 0.2126 * (1 - value),
                0.7152 - 0.7152 * (1 - value),
                0.0722 + 0.9278 * (1 - value),
            });

        public void Saturate(double value = 1)
            => Multiply(new double[] {
                0.213 + 0.787 * value,
                0.715 - 0.715 * value,
                0.072 - 0.072 * value,
                0.213 - 0.213 * value,
                0.715 + 0.285 * value,
                0.072 - 0.072 * value,
                0.213 - 0.213 * value,
                0.715 - 0.715 * value,
                0.072 + 0.928 * value,
            });

        public void Brightness(double value = 1)
            => Linear(value);

        public void Contrast(double value = 1)
            => Linear(value, -(0.5 * value) + 0.5);

        public void Invert(double value = 1)
        {
            Red = Clamp((value + (Red / 255) * (1 - 2 * value)) * 255);
            Green = Clamp((value + (Green / 255) * (1 - 2 * value)) * 255);
            Blue = Clamp((value + (Blue / 255) * (1 - 2 * value)) * 255);
        }

        public (double H, double S, double L) Hsl()
        {
            var red = Red / 255;
            var green = Green / 255;
            var blue = Blue / 255;
            var colors = new double[] { red, green, blue };
            var max = colors.Max();
            var min = colors.Min();

            double h = default, s;
            double l = (max + min) / 2;

            if (max == min)
            {
                h = 0;
                s = 0;
            }
            else
            {
                var diff = max - min;
                s = l > 0.5 ? diff / (2 - max - min) : diff / (max + min);
                if (max == red)
                    h = (green - blue) / diff + (green < blue ? 6 : 0);
                else if (max == green)
                    h = (blue - red) / diff + 2;
                else if (max == blue)
                    h = (red - green) / diff + 4;
                h /= 6;
            }

            return (h * 100, s * 100, l * 100);
        }

        #region Private members
        private void Linear(double slope = 1, double intercept = 0)
        {
            Red = Clamp(Red * slope + intercept * 255);
            Green = Clamp(Green * slope + intercept * 255);
            Blue = Clamp(Blue * slope + intercept * 255);
        }

        private void Multiply(double[] matrix)
        {
            double newRed = Clamp(Red * matrix[0] + Green * matrix[1] + Blue * matrix[2]);
            double newGreen = Clamp(Red * matrix[3] + Green * matrix[4] + Blue * matrix[5]);
            double newBlue = Clamp(Red * matrix[6] + Green * matrix[7] + Blue * matrix[8]);
            Red = newRed;
            Green = newGreen;
            Blue = newBlue;
        }

        private static double Clamp(double value)
        {
            if (value > 255)
                return 255;
            else if (value < 0)
                return 0;
            return value;
        }
        #endregion
    }
}
