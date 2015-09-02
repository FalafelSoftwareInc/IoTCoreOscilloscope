using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Charting;

namespace IoTOscilloscope
{
    public class MainViewModel : INotifyPropertyChanged
    {
        const double DEFAULTGRAPHMIN = 0;
        const double DEFAULTGRAPHMAX = 1050;

        public MainViewModel()
        {
            Normalize = false;
            SampleSize = 700;
        }

        IEnumerable<ScatterDataPoint> _Points;
        public IEnumerable<ScatterDataPoint> Points
        {
            get
            {
                return _Points;
            }
            set
            {
                _Points = value;
                OnPropertyChanged("Points");
            }
        }

        string _Status;
        public string Status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }

        bool _Normalize = false;
        public bool Normalize
        {
            get
            {
                return _Normalize;
            }
            set
            {
                _Normalize = value;
                if (_Normalize)
                {
                    this.GraphMin = -600;
                    this.GraphMax = 600;
                }
                else
                {
                    this.GraphMin = DEFAULTGRAPHMIN;
                    this.GraphMax = DEFAULTGRAPHMAX;
                }
                OnPropertyChanged("Normalize");
            }
        }

        bool _LineSeries = true;
        public bool LineSeries
        {
            get
            {
                return _LineSeries;
            }
            set
            {
                _LineSeries = value;
                OnPropertyChanged("LineSeries");
            }
        }

        bool _PositiveTrigger = true;
        public bool PositiveTrigger
        {
            get
            {
                return _PositiveTrigger;
            }
            set
            {
                _PositiveTrigger = value;
                OnPropertyChanged("PositiveTrigger");
            }
        }

        int _Average;
        public int Average
        {
            get
            {
                return _Average;
            }
            set
            {
                _Average = value;
                OnPropertyChanged("Average");
            }
        }

        int _SampleSize;
        public int SampleSize
        {
            get
            {
                return _SampleSize;
            }
            set
            {
                _SampleSize = value;
                GraphXMax = _SampleSize / 4;
                OnPropertyChanged("SampleSize");
            }
        }

        double _OneCycleMS;
        public double OneCycleMS
        {
            get
            {
                return _OneCycleMS;
            }
            set
            {
                _OneCycleMS = value;
                OnPropertyChanged("OneCycleMS");
            }
        }

        double _Min;
        public double Min
        {
            get
            {
                return _Min;
            }
            set
            {
                _Min = value;
                OnPropertyChanged("Min");
            }
        }

        double _Max;
        public double Max
        {
            get
            {
                return _Max;
            }
            set
            {
                _Max = value;
                OnPropertyChanged("Max");
            }
        }

        double _GraphMax = DEFAULTGRAPHMAX;
        public double GraphMax
        {
            get
            {
                return _GraphMax;
            }
            set
            {
                _GraphMax = value;
                OnPropertyChanged("GraphMax");
            }
        }

        double _GraphMin = DEFAULTGRAPHMIN;
        public double GraphMin
        {
            get
            {
                return _GraphMin;
            }
            set
            {
                _GraphMin = value;
                OnPropertyChanged("GraphMin");
            }
        }
        double _GraphXMax = 500;
        public double GraphXMax
        {
            get
            {
                return _GraphXMax;
            }
            set
            {
                _GraphXMax = value;
                OnPropertyChanged("GraphXMax");
            }
        }
        double _runMS;
        public double RunMS
        {
            get
            {
                return _runMS;
            }
            set
            {
                _runMS = value;
                OnPropertyChanged("RunMS");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
