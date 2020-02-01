using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Harmonograph
{
    // https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/how-to-create-a-linesegment-in-a-pathgeometry
    // http://www.worldtreesoftware.com/harmonograph-intro.html#pre
    // http://andygiger.com/science/harmonograph/index.html


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Oscillator oH1, oH2, oH3, oV1, oV2, oV3, oC;
        private readonly PendulumSet Simulator;
        private readonly PathBuider Plotter;
        private readonly DispatcherTimer PlotTimer;
        private readonly DispatcherTimer RandomTimer;
        private readonly Stopwatch Stopwatch;

        private bool isInitialised = false;
        private bool isPathUpdating = false;
        private bool isPlotting = false;

        private double LastUpdateTime;

        private readonly double simTime = 5000; // ms
        private readonly double timeRes = 0.01; // ms

        double minFps = double.MaxValue;
        double maxFps = 0;
        double sumFps = 0;
        long frameCount = 0;

        private double AmplitudeX1;
        private double AmplitudeX2;
        private double AmplitudeX3;
        private double AmplitudeY1;
        private double AmplitudeY2;
        private double AmplitudeY3;
        private double InitialPhaseX1;
        private double InitialPhaseX2;
        private double InitialPhaseX3;
        private double InitialPhaseY1;
        private double InitialPhaseY2;
        private double InitialPhaseY3;
        private double AngularFrequency1;
        private double AngularFrequency2;
        private double AngularFrequency3;
        private double DecayConstant1;
        private double DecayConstant2;
        private double DecayConstant3;

        public MainWindow()
        {
            InitializeComponent();

            PlotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 60) };
            PlotTimer.Tick += Timer_Tick;

            RandomTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            RandomTimer.Tick += RandomTimer_Tick;

            Stopwatch = new Stopwatch();

            Simulator = new PendulumSet();

            Plotter = new PathBuider(Simulator);
        }

        private void RandomTimer_Tick(object sender, EventArgs e)
        {
            if ((bool)cbAutoRandomise.IsChecked)
            {
                Randomise();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void UpdatePlot()
        {
            if (isPlotting) { return; }
            if (isPathUpdating) { return; }

            var stopwatchTime = Stopwatch.ElapsedMilliseconds;

            isPlotting = true;

            var path = Plotter.GeneratePath(LastUpdateTime, LastUpdateTime + stopwatchTime);
            path.Stroke = new SolidColorBrush(Utilities.ColorFromAHSV(255, LastUpdateTime % 36000, 0.5, 1));
            LastUpdateTime += stopwatchTime;

            Canvas.SetLeft(path, canvas1.ActualWidth / 2);
            Canvas.SetTop(path, canvas1.ActualHeight / 2);

            using (var d = Dispatcher.DisableProcessing())
            {
                canvas1.Children.Add(path);
                while (GetNumberOfSegments(canvas1) * timeRes > sliderTrailLength.Value)
                {
                    canvas1.Children.RemoveAt(0);
                }
            }

            if (LastUpdateTime > simTime)
            {
                //LastUpdateTime = 0;
                PlotTimer.Stop();
                Stopwatch.Stop();
                RandomTimer.Start();
            }
            else
            {
                Stopwatch.Restart();
            }

            var fps = 1 / (stopwatchTime / 1000f);
            maxFps = fps > maxFps ? fps : maxFps;
            minFps = fps < minFps ? fps : minFps;
            sumFps += fps;
            frameCount++;
            label1.Content = ("FPS CUR " + fps.ToString("000.00")
                + " | MIN " + minFps.ToString("00.00")
                + " | MAX " + maxFps.ToString("0000.00")
                + " | AVG " + (sumFps / frameCount).ToString("0000.00")
                + " | CLOCK TIME " + (LastUpdateTime / 1000f).ToString("00.000")
                + " | SIMULATION TIME " + (LastUpdateTime * Plotter.TimeResolution / 1000f).ToString("00.000")
                + (PlotTimer.IsEnabled ? "" : " [DONE]"));

            isPlotting = false;
        }

        public long GetNumberOfSegments(Canvas canvas)
        {
            var count = 0;
            foreach (var item in canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    var data = (item as Path).Data;
                    if (data.GetType() == typeof(PathGeometry))
                    {
                        var fgs = (data as PathGeometry).Figures;
                        count += fgs.First().Segments.Count;
                    }
                }
            }
            return count;
        }

        public void ClearSegmentsInCanvas(Canvas canvas)
        {
            foreach (var item in canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    var data = (item as Path).Data;
                    if (data.GetType() == typeof(PathGeometry))
                    {
                        (data as PathGeometry).Figures.Clear();
                    }
                    (item as Path).Data = null;
                }
            }
        }

        private void Canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //canvas1.Children.Clear();
        }

        private void UpdateOscillatorParameters()
        {
            double.TryParse(tbF1.Text, out AngularFrequency1);
            double.TryParse(tbAx1.Text, out AmplitudeY1);
            double.TryParse(tbAy1.Text, out AmplitudeX1);
            double.TryParse(tbPx1.Text, out InitialPhaseX1);
            double.TryParse(tbPy1.Text, out InitialPhaseY1);
            double.TryParse(tbD1.Text, out DecayConstant1);

            double.TryParse(tbF2.Text, out AngularFrequency2);
            double.TryParse(tbAx2.Text, out AmplitudeY2);
            double.TryParse(tbAy2.Text, out AmplitudeX2);
            double.TryParse(tbPx2.Text, out InitialPhaseX2);
            double.TryParse(tbPy2.Text, out InitialPhaseY2);
            double.TryParse(tbD2.Text, out DecayConstant2);

            double.TryParse(tbF3.Text, out AngularFrequency3);
            double.TryParse(tbAx3.Text, out AmplitudeY3);
            double.TryParse(tbAy3.Text, out AmplitudeX3);
            double.TryParse(tbPx3.Text, out InitialPhaseX3);
            double.TryParse(tbPy3.Text, out InitialPhaseY3);
            double.TryParse(tbD3.Text, out DecayConstant3);

            Simulator.SetPendulumParameters(0, AmplitudeX1, AmplitudeY1, InitialPhaseX1, InitialPhaseY1,
                AngularFrequency1, DecayConstant1);
            Simulator.SetPendulumParameters(1, AmplitudeX2, AmplitudeY2, InitialPhaseX2, InitialPhaseY2,
                AngularFrequency2, DecayConstant2);
            Simulator.SetPendulumParameters(2, AmplitudeX3, AmplitudeY3, InitialPhaseX3, InitialPhaseY3,
                AngularFrequency3, DecayConstant3);

            //TODO UpdatePath();
        }

        private void Reset()
        {
            LastUpdateTime = 0;
            PlotTimer.Stop();
            Stopwatch.Restart();
            PlotTimer.Start();
            //ClearSegmentsInCanvas(canvas1);
            canvas1.Children.Clear();
            //grid1.Children.RemoveAt(0);
            //grid1.Children.Insert(0, canvas1);
            this.UpdateLayout();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void SliderColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialised)
                return;
            UpdateFc();
        }

        private void UpdateFc()
        {
            if (!isInitialised)
                return;
            //TODO
            //Simulator.SetColorAngularFrequency(sliderFc.Value / 100000);
        }

        private void TextboxOscillatorParameter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialised)
                return;
            UpdateOscillatorParameters();
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Opacity = 1;
        }

        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Opacity = 0.1;
        }




        private void Cb_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
                return;
            var checkBox = (CheckBox)sender;
            var isActivating = (bool)checkBox.IsChecked;
            int index;
            if (checkBox.Name.Contains("1"))
                index = 0;
            else if (checkBox.Name.Contains("2"))
                index = 1;
            else if (checkBox.Name.Contains("3"))
                index = 2;
            else
                return;

            if (isActivating)
                Simulator.ActivatePendulum(index);
            else
                Simulator.DeactivatePendulum(index);

            //TODO UpdatePath();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFc();
            //UpdateOscillatorParameters();
            isInitialised = true;
            Randomise();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Randomise();
        }

        private void Canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {


        }

        private void Canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowStyle != WindowStyle.None)
                {
                    WindowStyle = WindowStyle.None;
                    if (WindowState == WindowState.Maximized)
                        WindowState = WindowState.Normal;
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                }
            }
        }

        private void Canvas1_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            Randomise();
        }

        private void Randomise()
        {
            Random x = new Random();
            isInitialised = false;

            var f1 = x.Next(1, 21) + 0f;
            var f2 = x.Next(1, 21) + 0f;
            var nearMiss = 1f + (x.Next(0, 5) < 1 ? 0 : 1) * x.Next(-5, 5) / 100f;
            var f1Normed = (f1 > f2) ? 1 : f1 / f2;
            var f2Normed = (f1 > f2) ? f2 / f1 : 1;
            f2Normed = f2Normed * nearMiss;

            tbF1.Text = f1Normed.ToString();
            tbF2.Text = f2Normed.ToString();
            //------------------------------
            //var px1 = x.Next(0, 180);
            var bound = 10;
            var px1 = 0;
            var p1xy = x.Next(-bound, bound + 1) + 90;
            var p2xy = x.Next(-bound, bound + 1) + 90 * (x.Next(0, 2) == 1 ? 1 : -1);
            var p12 = x.Next(0, 181);
            tbPx1.Text = px1.ToString();
            tbPy1.Text = (px1 + p1xy).ToString();
            tbPx2.Text = (px1 + p12).ToString();
            tbPy2.Text = (px1 + p12 + p2xy).ToString();
            //------------------------------
            var d1 = x.Next(0, 501) / 1000000f;
            var d2 = x.Next(100, 2001) / 100000f;
            tbD1.Text = d1.ToString();
            tbD2.Text = d2.ToString();
            //------------------------------
            var a1 = x.Next(0, (int)(canvas1.ActualWidth / 2));
            var a2 = x.Next(0, (int)(canvas1.ActualHeight / 2));
            tbAx1.Text = a1.ToString();
            tbAy1.Text = a1.ToString();
            tbAx2.Text = a2.ToString();
            tbAy2.Text = a2.ToString();

            isInitialised = true;
            UpdateOscillatorParameters();
            Reset();

            RandomTimer.Stop();
        }

        public double GetSliderValue(Slider slider, double l, double u)
        {
            return Utilities.ConvertValueFromRange1ToRange2(slider.Value,
                slider.Minimum, slider.Maximum, l, u);
        }

        public void SetSliderValue(Slider slider, double value, double l, double u)
        {
            slider.Value = (int)Utilities.ConvertValueFromRange1ToRange2(value, l, u, slider.Minimum,
                slider.Maximum);
        }

        private void ResetButton_Clicked(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        public double TimeScale { get { return Math.Pow(10, GetSliderValue(sliderT, -2, 2)); } }
    }
}
