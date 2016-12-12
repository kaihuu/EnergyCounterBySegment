using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyCounterBySegment
{
    class MathUtil
    {
        public static double ConvertDegreeToRadian(double degree)
        {
            return degree * Math.PI / 180;
        }
        public static double ConvertRadianToDegree(double radian)
        {
            return radian * 180 / Math.PI;
        }

    }
}
