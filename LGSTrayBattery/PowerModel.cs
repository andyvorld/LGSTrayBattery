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
            _dischargeCurve.Sort((a, b) => (a[2] < b[2]) ? 1 : -1);

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
                if (voltage < _dischargeCurve[ii][1])
                {
                    break;
                }
            }

            double interpValue = 0;

            if (ii == _dischargeCurve.Count)
            {
                // Linear extrapolation
                double[] lower = _dischargeCurve[ii - 2];
                double[] upper = _dischargeCurve[ii - 1];

                interpValue = (voltage - upper[1]) * (upper[2] - lower[2]) / (upper[1] - lower[1]);
            }
            else
            {
                double[] lower = _dischargeCurve[ii - 1];
                double[] upper = _dischargeCurve[ii];

                interpValue = (voltage - lower[1]) / (upper[1] - lower[1]) * (upper[2] - lower[2]) + lower[2];
            }


            return 100 * (1 - interpValue / _dischargeCurve[0][2]);
        }
    }
}
