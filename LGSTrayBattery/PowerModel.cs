using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LGSTrayBattery
{
    class PowerModel
    {
        private readonly bool _valid = false;
        private readonly List<double[]> _dischargeCurve;

        private static class LUTCol
        {
            public static int Time = 0;
            public static int Volt = 1;
            public static int MWh = 2;
        };

        public PowerModel(UInt16 WPID)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load($"./PowerModel/46D_{WPID:X4}.xml");
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine($"PowerModel file not found for {WPID:X4}");
                return;
            }

            XmlNode dischargeCurveNode = doc.SelectSingleNode("/powermodel/battery/discharge")?.FirstChild;

            if (dischargeCurveNode == null)
            {
                Debug.WriteLine($"PowerModel file not valid for {WPID:X4}");
                return;
            }

            var temp = dischargeCurveNode.Value.Trim('\n', ' ').Split(new char[] { '\n', }, StringSplitOptions.RemoveEmptyEntries);

            _dischargeCurve = temp.ToList().ConvertAll(x => Array.ConvertAll(x.Split(','), Double.Parse));

            if (_dischargeCurve[0][LUTCol.Volt] > _dischargeCurve[1][LUTCol.Volt])
            {
                _dischargeCurve.Reverse(0, _dischargeCurve.Count);
            }

            _valid = true;
        }

        // Voltage in volts
        public double GetBatteryPercent(double voltage)
        {
            if (!_valid || Double.IsNaN(voltage))
            {
                return double.NaN;
            }

            int ii;

            // Time (minutes), CCV (volts), Discharge (mWh)
            for (ii = 0; ii < _dischargeCurve.Count; ii++)
            {
                if (voltage < _dischargeCurve[ii][LUTCol.Volt])
                {
                    break;
                }
            }

            double interpValue = 0;

            if (_dischargeCurve.Count <= ii)
            {
                // Linear extrapolation
                double[] lower = _dischargeCurve[ii - 2];
                double[] upper = _dischargeCurve[ii - 1];

                interpValue = (voltage - upper[LUTCol.Volt]) * (upper[LUTCol.MWh] - lower[LUTCol.MWh]) / (upper[LUTCol.Volt] - lower[LUTCol.Volt]);
            }
            else if (ii < 1)
            {
                // Not within LUT
                return Double.NaN;
            }
            else
            {
                double[] lower = _dischargeCurve[ii - 1];
                double[] upper = _dischargeCurve[ii];

                interpValue = (voltage - lower[LUTCol.Volt]) / (upper[LUTCol.Volt] - lower[LUTCol.Volt]) * (upper[LUTCol.MWh] - lower[LUTCol.MWh]) + lower[LUTCol.MWh];
            }


            return 100 * (1 - interpValue / _dischargeCurve[0][LUTCol.MWh]);
        }
    }
}
