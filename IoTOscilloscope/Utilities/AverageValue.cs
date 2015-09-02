using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTOscilloscope.Utilities
{
    public class AverageValue
    {
        Queue queue;
        double _Sum;
        int _SampleSize;

        public AverageValue(int samplesize = 5)
        {
            SampleSize = samplesize;
        }

        public int SampleSize
        {
            get { return _SampleSize; }
            set
            {
                if (value > 0)
                {
                    _SampleSize = value;
                    queue = new Queue(_SampleSize);
                }
            }
        }

        public double Value
        {
            get { return _Sum / queue.Count; }
            set
            {
                if (queue.Count == _SampleSize)
                {
                    _Sum -= (double)queue.Dequeue();
                }
                queue.Enqueue(value);
                _Sum += value;
            }
        }
    }
}

