using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTOscilloscope.Utilities
{
    public class OscopeHelper
    {
        public static void NormalizeToAverage(SampleValue[] samples, int average)
        {
            int sampleSize = samples.Length;
            for (int i = 1; i < sampleSize; i++)
            {
                samples[i].Value = samples[i].Value - average;
            }
        }

        public static void ProcessStats(SampleValue[] samples, ref int average, ref int min, ref int max)
        {
            average = 0;
            int sampleSize = samples.Length;
            for (int i = 0; i < sampleSize - 1; i++)
            {
                if (samples[i].Value < min)
                {
                    min = samples[i].Value;
                }
                if (samples[i].Value > max)
                {
                    max = samples[i].Value;
                }
                average += samples[i].Value;
            }
            average = average / sampleSize;
        }

        public static Dictionary<int, long> ProcessZeroCrossings(
            SampleValue[] samples,
            bool positiveTrigger = false,
            int offset = 0,
            int? crossingsCount = null,
            int averageCount = 4)
        {
            AverageValue averageValue = new AverageValue(averageCount);
            var crossings = new Dictionary<int, long>();
            bool crossSet = false;
            int sampleSize = samples.Length;
            for (int i = 0; i < sampleSize; i++)
            {
                averageValue.Value = samples[i].Value;
                if (i > averageValue.SampleSize)
                {
                    var fvalue = averageValue.Value;
                    var averageIndex = i - averageValue.SampleSize / 2;
                    samples[averageIndex].Value = (int)fvalue;
                    if (crossingsCount.HasValue && crossings.Count < crossingsCount.Value)
                    {
                        if (!crossSet && ((!positiveTrigger && (fvalue > offset)) || (positiveTrigger && (fvalue < offset))))
                        {
                            crossSet = true;
                        }
                        if (crossSet && ((!positiveTrigger && (fvalue < offset)) || (positiveTrigger && (fvalue > offset))))
                        {
                            crossings.Add(averageIndex, samples[i].Tick);
                            crossSet = false;
                        }
                    }
                }
            }
            return crossings;
        }
    }
}
