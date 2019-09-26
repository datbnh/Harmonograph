using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Harmonograph
{
    public static class Utilities
    {
        /// <summary>
        /// Returns color from a specified value in A-HSV space, of which alpha ranges from 0 to 255, 
        /// hue form 0 to 360, saturation from 0 to 1, value from 0 to 1.
        /// </summary>
        /// <returns></returns>
        public static Color ColorFromAHSV(byte alpha, double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)Convert.ToInt32(value);
            byte p = (byte)Convert.ToInt32(value * (1 - saturation));
            byte q = (byte)Convert.ToInt32(value * (1 - f * saturation));
            byte t = (byte)Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(alpha, t, p, v);
            else
                return Color.FromArgb(alpha, v, p, q);
        }

        public static double ConvertValueFromRange1ToRange2(double valueInRange1,
            double range1LowerBound, double range1UpperBound,
            double range2LowerBound, double range2UpperBound)
        {
            var range1 = range1UpperBound - range1LowerBound;
            var range2 = range2UpperBound - range2LowerBound;
            return (valueInRange1 - range1LowerBound) / range1 * range2 + range2LowerBound;
        }
    }
}
