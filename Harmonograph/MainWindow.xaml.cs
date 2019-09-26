using System;
using System.Collections.Generic;
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
        Simulator simulator;
        DispatcherTimer plotTimer;
        DispatcherTimer randomTimer;
        Stopwatch stopwatch;

        bool isInitialised = false;
        bool isPathUpdating = false;
        private bool isPlotting = false;

        private int currentSegmentStartIdx = 0;

        private double simTime = 5000; // ms
        private double timeRes = 0.05; // ms

        double minFps = double.MaxValue;
        double maxFps = 0;
        double sumFps = 0;
        long frameCount = 0;

        public double OffsetH;
        public double OffsetV;

        public List<Point> path;
        private double AngularFrequency1;
        private double AmplitudeY1;
        private double AmplitudeX1;
        private double InitialPhaseX1;
        private double InitialPhaseY1;
        private double DecayConstant1;
        private double AngularFrequency2;
        private double AmplitudeY2;
        private double AmplitudeX2;
        private double InitialPhaseY2;
        private double InitialPhaseX2;
        private double DecayConstant2;
        private double AngularFrequency3;
        private double AmplitudeY3;
        private double AmplitudeX3;
        private double InitialPhaseX3;
        private double InitialPhaseY3;
        private double DecayConstant3;

        public MainWindow()
        {
            InitializeComponent();

            plotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 60) };
            plotTimer.Tick += Timer_Tick;

            randomTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            randomTimer.Tick += RandomTimer_Tick;

            stopwatch = new Stopwatch();

            simulator = new Simulator();

            //oH1 = new Oscillator();
            //oH2 = new Oscillator();
            //oV1 = new Oscillator();
            //oV2 = new Oscillator();
            //oH3 = new Oscillator();
            //oV3 = new Oscillator();

            //oC = new Oscillator(360, 0.001);
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

            //var overlapping = 0;
            var stopwatchTime = stopwatch.ElapsedMilliseconds;

            isPlotting = true;

            var endIdx = (int)(currentSegmentStartIdx + stopwatchTime / timeRes / TimeScale);
            if (endIdx >= this.path.Count - 0) // 0 : overlapping
            {
                endIdx = this.path.Count - 0 - 1;
                plotTimer.Stop();
                stopwatch.Stop();
                randomTimer.Start();
            }
            else
            {
                stopwatch.Restart();
            }

            PathFigure pathFigure = new PathFigure { StartPoint = this.path.ElementAt(currentSegmentStartIdx) };

            PathSegmentCollection pathSegments = new PathSegmentCollection();

            for (int i = currentSegmentStartIdx + 1; i < endIdx + 0 + 1; i++)
            {
                pathSegments.Add(new LineSegment(this.path.ElementAt(i), true));
            }
            pathFigure.Segments = pathSegments;

            PathFigureCollection pathFigures = new PathFigureCollection { pathFigure };

            PathGeometry pathGeometry = new PathGeometry { Figures = pathFigures, };

            Path path = new Path
            {
                StrokeThickness = 1,
                //Stroke = new SolidColorBrush(Utilities.ColorFromAHSV(255, oC.GetInstantaniousAmplitutdeAtTime(currentSegmentStartIdx), 0.5, 1))
                Stroke = new SolidColorBrush(simulator.GetColorAtTime(currentSegmentStartIdx)),
            };
            path.Data = pathGeometry;

            using (var d = Dispatcher.DisableProcessing())
            {
                canvas1.Children.Add(path);

                //while (canvas1.Children.Count > sliderTrailLength.Value)
                //{
                //    canvas1.Children.RemoveAt(0);
                //}
                while (GetNumberOfSegments(canvas1) * timeRes > sliderTrailLength.Value)
                {
                    canvas1.Children.RemoveAt(0);
                }

            }

            // Path > Path.Data = PathGemometry > PathGemometry.Figures = PathFigureCollection > 
            // PathFigure > PathFigure.StartPoint, PathFigure.Segments = PathSegmentCollection >
            // LineSegment > Point

            var fps = 1 / (stopwatchTime / 1000f);
            maxFps = fps > maxFps ? fps : maxFps;
            minFps = fps < minFps ? fps : minFps;
            sumFps += fps;
            frameCount++;
            label1.Content = ("FPS CUR " + fps.ToString("000.00")
                + " | MIN " + minFps.ToString("00.00")
                + " | MAX " + maxFps.ToString("0000.00")
                + " | AVG " + (sumFps / frameCount).ToString("0000.00")
                + " | CLOCK TIME " + (endIdx * timeRes * TimeScale / 1000f).ToString("00.000")
                + " | SIMULATION TIME " + (endIdx * timeRes / 1000f).ToString("00.000")
                + (plotTimer.IsEnabled ? "" : " [DONE]"));
            currentSegmentStartIdx = endIdx;

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

        private void Canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OffsetH = canvas1.ActualWidth / 2;
            OffsetV = canvas1.ActualHeight / 2;
            canvas1.Children.Clear();
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

            simulator.SetPendulumParameters(0, AmplitudeX1, AmplitudeY1, InitialPhaseX1, InitialPhaseY1,
                AngularFrequency1, DecayConstant1);
            simulator.SetPendulumParameters(1, AmplitudeX2, AmplitudeY2, InitialPhaseX2, InitialPhaseY2,
                AngularFrequency2, DecayConstant2);
            simulator.SetPendulumParameters(2, AmplitudeX3, AmplitudeY3, InitialPhaseX3, InitialPhaseY3,
                AngularFrequency3, DecayConstant3);
            //double.TryParse(tbF1.Text, out oH1.AngularFrequency);
            //double.TryParse(tbAx1.Text, out oH1.Amplitude);
            //double.TryParse(tbAy1.Text, out oV1.Amplitude);
            //double.TryParse(tbPx1.Text, out oH1.InitialPhase);
            //double.TryParse(tbPy1.Text, out oV1.InitialPhase);
            //double.TryParse(tbD1.Text, out oH1.DecayConstant);
            //oV1.AngularFrequency = oH1.AngularFrequency;
            //oV1.DecayConstant = oH1.DecayConstant;

            //double.TryParse(tbF2.Text, out oH2.AngularFrequency);
            //double.TryParse(tbAx2.Text, out oH2.Amplitude);
            //double.TryParse(tbAy2.Text, out oV2.Amplitude);
            //double.TryParse(tbPx2.Text, out oH2.InitialPhase);
            //double.TryParse(tbPy2.Text, out oV2.InitialPhase);
            //double.TryParse(tbD2.Text, out oH2.DecayConstant);
            //oV2.AngularFrequency = oH2.AngularFrequency;
            //oV2.DecayConstant = oH2.DecayConstant;

            //double.TryParse(tbF3.Text, out oH3.AngularFrequency);
            //double.TryParse(tbAx3.Text, out oH3.Amplitude);
            //double.TryParse(tbAy3.Text, out oV3.Amplitude);
            //double.TryParse(tbPx3.Text, out oH3.InitialPhase);
            //double.TryParse(tbPy3.Text, out oV3.InitialPhase);
            //double.TryParse(tbD3.Text, out oH3.DecayConstant);
            //oV3.AngularFrequency = oH3.AngularFrequency;
            //oV3.DecayConstant = oH3.DecayConstant;

            UpdatePath();

            //canvas1.Children.Clear();
        }

        private List<Point> GeneratePath(double startT, double endT, double timeResolution)
        {
            var msg = "Calculating path from " + startT + " to " + endT + "...";
            label1.Content = msg;
            var numPoints = (long)((endT - startT) / timeResolution);
            List<Point> pathSegments = new List<Point>();
            for (int i = 0; i <= numPoints; i++)
            {
                double t = startT + (endT - startT) * i / numPoints;
                pathSegments.Add(new Point(X(t), Y(t)));
            }
            label1.Content = msg + " Done";
            return pathSegments;
        }

        private void UpdatePath()
        {
            if (!isInitialised)
                return;

            stopwatch.Stop();
            while (isPlotting) { }
            isPathUpdating = true;

            var temp = GeneratePath(currentSegmentStartIdx * timeRes, simTime, timeRes);
            if (path == null)
                path = temp;
            for (int i = currentSegmentStartIdx; i <= simTime; i++)
            {
                path[i] = temp[i - currentSegmentStartIdx];
            }

            isPathUpdating = false;
            stopwatch.Start();
        }

        private void Reset()
        {
            plotTimer.Stop();
            currentSegmentStartIdx = 0;
            path = GeneratePath(currentSegmentStartIdx, simTime, timeRes);
            stopwatch.Restart();
            plotTimer.Start();
            canvas1.Children.Clear();
        }

        public double X(double t)
        {
           return simulator.GetInstantaniousCoordinate(t).X + OffsetH;
        }

        public double Y(double t)
        {
            return simulator.GetInstantaniousCoordinate(t).Y + OffsetV;
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
            simulator.SetColorAngularFrequency(sliderFc.Value / 100000);
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
                simulator.ActivatePendulum(index);
            else
                simulator.DeactivatePendulum(index);

            UpdatePath();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OffsetH = canvas1.ActualWidth / 2;
            OffsetV = canvas1.ActualHeight / 2;
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
            var a1 = x.Next(0, (int)OffsetH);
            var a2 = x.Next(0, (int)OffsetH);
            tbAx1.Text = a1.ToString();
            tbAy1.Text = a1.ToString();
            tbAx2.Text = a2.ToString();
            tbAy2.Text = a2.ToString();

            isInitialised = true;
            UpdateOscillatorParameters();
            Reset();

            randomTimer.Stop();
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
