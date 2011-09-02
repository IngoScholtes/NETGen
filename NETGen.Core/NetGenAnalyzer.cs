﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NETGen.Core
{
    public class ResultSet
    {
        private Dictionary<double, double[]> _values;
        public string XLabel = "X";
        public string Path;

        public static CultureInfo ci = CultureInfo.GetCultureInfo("en-US");

        public ResultSet(string path)
        {
            _values = new Dictionary<double, double[]>();
            Path = path;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SubmitResults(double x, params double[] values)
        {
            _values[x] = values;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void WriteToFile(bool include_avg_sd)
        {
            System.IO.File.WriteAllText(Path, "# Result file generated by NetGen on " + DateTime.Now.ToLongTimeString()+"\n");
            foreach (double x in _values.Keys)
            {
                string line = x.ToString(ci.NumberFormat);
                foreach (double v in _values[x])
                    line += "\t" + v.ToString(ci.NumberFormat);


                if (include_avg_sd)
                {
                    line += "\t" + ComputeMean(_values[x]).ToString(ci.NumberFormat);
                    line += "\t" + ComputeStandardVariation(_values[x]).ToString(ci.NumberFormat);
                }
                System.IO.File.AppendAllText(Path, line+"\n");
            }
        }

        public static double ComputeMean(double[] values)
        {
            double avg = 0d;
            foreach (double d in values)
                avg += d;
            return avg / (double)values.Count();
        }

        public static double ComputeStandardVariation(double[] values)
        {
            double avg = ComputeMean(values);
            double sd = 0d;

            foreach (double d in values)
                sd += Math.Pow(d - avg, 2d);
            sd /= (values.Count() - 1);
            return Math.Sqrt(sd);
        }
    }

    public class NetGenAnalyzer
    {
        private static Dictionary<string,ResultSet> _resultSets = new Dictionary<string,ResultSet>();

        public static Dictionary<string, ResultSet> ResultSets
        {
            get { return _resultSets; }
            private set { _resultSets = value; }
        }        

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ClearResultSet()
        {
            _resultSets = new Dictionary<string, ResultSet>();
        }       
    }
}