using IoTOscilloscope.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Charting;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTOscilloscope
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        enum AdcDevice { NONE, MCP3002, MCP3208 };
        private AdcDevice ADC_DEVICE = AdcDevice.MCP3002;
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* Friendly name for Raspberry Pi 2 SPI controller          */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */
        private SpiDevice SpiADC;
        private const byte MCP3002_CONFIG = 0x68; /* 01101000 channel configuration data for the MCP3002 */
        private const byte MCP3208_CONFIG = 0x06; /* 00000110 channel configuration data for the MCP3208 */
        private Timer periodicTimer;
        private int adcValue;
        private const int interval = 250;
        private bool running = false;
        bool normalized = false;
        bool positiveTrigger = false;
        int sampleSize = 750;
        DateTime lastRun = DateTime.Now;

        public MainViewModel viewModel {
            get
            {
                return this.DataContext as MainViewModel;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            Unloaded += MainPage_Unloaded;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            viewModel.Status = "Waiting for initialization";
            /* Initialize GPIO and SPI */
            InitAll();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            normalized = viewModel.Normalize;
            sampleSize = viewModel.SampleSize;
            positiveTrigger = viewModel.PositiveTrigger;
        }

        /* Initialize SPI */
        private async void InitAll()
        {
            if (ADC_DEVICE == AdcDevice.NONE)
            {
                viewModel.Status = "Please change the ADC_DEVICE variable to either MCP3002 or MCP3208";
                return;
            }

            try
            {
                await InitSPI();    /* Initialize the SPI bus for communicating with the ADC      */
            }
            catch (Exception ex)
            {
                viewModel.Status = ex.Message;
                return;
            }

            running = true;
            periodicTimer = new Timer(this.Running_Timer_Tick, null, 0, System.Threading.Timeout.Infinite);
            viewModel.Status = "Status: Running";
        }

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 500000;   /* 0.5MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                SpiADC = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
            }

            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        public int convertToIntMCP3002(byte[] data)
        {
            int result = data[0] & 0x03;
            result <<= 8;
            result += data[1];
            return result;
        }

        public int convertToIntMCP3208(byte[] data)
        {
            int result = data[1] & 0x0F;
            result <<= 8;
            result += data[2];
            return result;
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* It's good practice to clean up after we're done */
            if (SpiADC != null)
            {
                SpiADC.Dispose();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            running = !running;
            if (running)
            {
                if (periodicTimer != null)
                {
                    periodicTimer.Dispose();
                }
                periodicTimer = new Timer(this.Running_Timer_Tick, null, 0, System.Threading.Timeout.Infinite);
                viewModel.Status = "Status: Running";
            }
            else
            {
                periodicTimer.Dispose();
                viewModel.Status = "Status: Stopped";
            }
        }

        private void Running_Timer_Tick(object state)
        {
            periodicTimer.Dispose();
            TakeReadings();
            if (running)
            {
                periodicTimer = new Timer(this.Running_Timer_Tick, null, interval, System.Threading.Timeout.Infinite);
            }
        }

        private void TakeReadings()
        {
            SampleValue[] samples = new SampleValue[sampleSize];
            ReadFast(samples);
            int average = 0;
            int min = int.MaxValue;
            int max = int.MinValue;
            OscopeHelper.ProcessStats(samples, ref average, ref min, ref max);
            if (normalized)
            {
                OscopeHelper.NormalizeToAverage(samples, (min + max) / 2);
            }
            var crossings = OscopeHelper.ProcessZeroCrossings(samples, positiveTrigger, normalized ? 0 : 512, 2, 1);
            double cross = crossings.Any() ? crossings.First().Value : 0;
            double oneCycle = crossings.Count > 1 ? (crossings.ElementAt(1).Value - crossings.ElementAt(0).Value) : 0;

            var task2 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var freq = 1000.0 / Stopwatch.Frequency;
                viewModel.Points = samples.Select(s => new ScatterDataPoint
                {
                    XValue = (s.Tick - cross) * freq,
                    YValue = s.Value,
                });
                viewModel.Average = average;
                viewModel.Min = min;
                viewModel.Max = max;
                viewModel.OneCycleMS = oneCycle * freq;
                var now = DateTime.Now;
                viewModel.RunMS = (now - lastRun).TotalMilliseconds;
                lastRun = now;
            });
        }

        private void ReadFast(SampleValue[] samples)
        {
            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };
            Func<byte[], int> convertToIntFunc = null;
            /* Setup the appropriate ADC configuration byte */
            switch (ADC_DEVICE)
            {
                case AdcDevice.MCP3002:
                    writeBuffer[0] = MCP3002_CONFIG;
                    convertToIntFunc = convertToIntMCP3002;
                    break;
                case AdcDevice.MCP3208:
                    writeBuffer[0] = MCP3208_CONFIG;
                    convertToIntFunc = convertToIntMCP3208;
                    break;
            }
            Stopwatch.StartNew();
            int sampleSize = samples.Length;
            for (int sampleIndex = 0; sampleIndex < sampleSize; sampleIndex++)
            {
                SpiADC.TransferFullDuplex(writeBuffer, readBuffer); /* Read data from the ADC                           */
                adcValue = convertToIntFunc(readBuffer);            /* Convert the returned bytes into an integer value */
                samples[sampleIndex] = new SampleValue() { Tick = Stopwatch.GetTimestamp(), Value = adcValue };
            }
        }
    }
}
