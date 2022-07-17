using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace LGSTrayHID
{
    public interface IPowerModel
    {
        double GetCapacity(double voltage);
    }

    public class PowerModel_3deg : IPowerModel
    {
        private static double a1 =      0.9743;
        private static double b1 =       4.138;
        private static double c1 =      0.3055;
        private static double a2 =       -37.5;
        private static double b2 =       4.074;
        private static double c2 =   0.0001542;
        private static double a3 =      0.2826;
        private static double b3 =       3.819;
        private static double c3 =      0.1342;


        public double GetCapacity(double voltage)
        {
            return
            a1 * Exp(-Pow(((voltage - b1) / c1) , 2)) +
            a2 * Exp(-Pow(((voltage - b2) / c2) , 2)) +
            a3 * Exp(-Pow(((voltage - b3) / c3) , 2));
        }
    }
    public class PowerModel_8deg : IPowerModel
    {
        private static double a1 =      -1.787;
        private static double b1 =       4.342;
        private static double c1 =      0.1531;
        private static double a2 =      0.1734;
        private static double b2 =       4.012;
        private static double c2 =     0.08709;
        private static double a3 =      0.1028;
        private static double b3 =       3.941;
        private static double c3 =     0.06379;
        private static double a4 =   0.0004879;
        private static double b4 =       4.073;
        private static double c4 =   0.0002583;
        private static double a5 =    -0.05001;
        private static double b5 =        3.82;
        private static double c5 =     0.04568;
        private static double a6 =   1.091e+05;
        private static double b6 =       8.412;
        private static double c6 =       1.268;
        private static double a7 =           0;
        private static double b7 =       3.897;
        private static double c7 =    4.58e-05;
        private static double a8 =      0.4275;
        private static double b8 =       3.825;
        private static double c8 =      0.1162;
        public double GetCapacity(double voltage)
        {
            return
            a1 * Exp(-Pow(((voltage - b1) / c1), 2)) + a2 * Exp(-Pow(((voltage - b2) / c2), 2)) +
            a3 * Exp(-Pow(((voltage - b3) / c3), 2)) + a4 * Exp(-Pow(((voltage - b4) / c4), 2)) +
            a5 * Exp(-Pow(((voltage - b5) / c5), 2)) + a6 * Exp(-Pow(((voltage - b6) / c6), 2)) +
            a7 * Exp(-Pow(((voltage - b7) / c7), 2)) + a8 * Exp(-Pow(((voltage - b8) / c8), 2));
        }
    }
}
