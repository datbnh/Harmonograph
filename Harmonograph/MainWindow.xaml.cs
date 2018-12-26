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
        Oscillator oH1, oH2, oH3, oV1, oV2, oV3, oC;
        DispatcherTimer plotTimer;
        DispatcherTimer randomTimer;
        Stopwatch stopwatch;

        bool isInitialised = false;
        bool isPathUpdating = false;
        private bool isPlotting = false;

        private int currentSegmentStartIdx = 0;

        private double simTime = 2000; // ms
        private double timeRes = 0.05; // ms

        double minFps = double.MaxValue;
        double maxFps = 0;
        double sumFps = 0;
        long frameCount = 0;

        public double OffsetH;
        public double OffsetV;

        public List<Point> path;

        /// <summary>
        /// alpha from 0 to 255, hue form 0 to 360, saturation from 0 to 1, value from 0 to 1.
        /// </summary>
        /// <param name="alpha">0 to 255</param>
        /// <param name="hue">0 to 360</param>
        /// <param name="saturation">0 to 1</param>
        /// <param name="value">0 to 1</param>
        /// <returns></returns>
        public static Color ColorFromAHSV(byte alpha, double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)Convert.ToInt32(value);
            byte p = (byte)Convert.ToInt32(value * (1 - saturation));
            byte q = (byte)Convert.ToInt32(value * (1 - f * saturation));
            byte t = (byte)Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(alpha, t, p, v);
            else
                return Color.FromArgb(alpha, v, p, q);
        }

        public MainWindow()
        {
            InitializeComponent();

            plotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000/60) };
            plotTimer.Tick += Timer_Tick;

            randomTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            randomTimer.Tick += RandomTimer_Tick;

            stopwatch = new Stopwatch();

            oH1 = new Oscillator();
            oH2 = new Oscillator();
            oV1 = new Oscillator();
            oV2 = new Oscillator();
            oH3 = new Oscillator();
            oV3 = new Oscillator();

            oC = new Oscillator(360, 0.001);
        }

        private void RandomTimer_Tick(object sender, EventArgs e)
        {
            Randomise();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void UpdatePlot()
        {
            if (isPlotting) { return; }
            if (isPathUpdating) { Console.WriteLine("Frame Dropped."); return; }

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
                Stroke = new SolidColorBrush(ColorFromAHSV(255, oC.U(currentSegmentStartIdx), 0.5, 1))
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
            double.TryParse(tbF1.Text, out oH1.F);
            double.TryParse(tbAx1.Text, out oH1.A);
            double.TryParse(tbAy1.Text, out oV1.A);
            double.TryParse(tbPx1.Text, out oH1.P);
            double.TryParse(tbPy1.Text, out oV1.P);
            double.TryParse(tbD1.Text, out oH1.D);
            oV1.F = oH1.F;
            oV1.D = oH1.D;

            double.TryParse(tbF2.Text, out oH2.F);
            double.TryParse(tbAx2.Text, out oH2.A);
            double.TryParse(tbAy2.Text, out oV2.A);
            double.TryParse(tbPx2.Text, out oH2.P);
            double.TryParse(tbPy2.Text, out oV2.P);
            double.TryParse(tbD2.Text, out oH2.D);
            oV2.F = oH2.F;
            oV2.D = oH2.D;

            double.TryParse(tbF3.Text, out oH3.F);
            double.TryParse(tbAx3.Text, out oH3.A);
            double.TryParse(tbAy3.Text, out oV3.A);
            double.TryParse(tbPx3.Text, out oH3.P);
            double.TryParse(tbPy3.Text, out oV3.P);
            double.TryParse(tbD3.Text, out oH3.D);
            oV3.F = oH3.F;
            oV3.D = oH3.D;

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
            return ((bool)cb1.IsChecked ? oH1.U(t) : 0) 
                + ((bool)cb2.IsChecked ? oH2.U(t) : 0) 
                + ((bool)cb3.IsChecked ? oH3.U(t) : 0) + OffsetH;
        }

        public double Y(double t)
        {
            return ((bool)cb1.IsChecked ? oV1.U(t) : 0)
               + ((bool)cb2.IsChecked ? oV2.U(t) : 0)
               + ((bool)cb3.IsChecked ? oV3.U(t) : 0) + OffsetV;
        }

        private void SliderColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialised)
                return;
            UpdateFc();
        }

        private void UpdateFc() {
            if (!isInitialised)
                return;
            oC.F = sliderFc.Value / 100000;
        }

        private void TextboxOscillatorParameter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialised)
                return;
            UpdateOscillatorParameters();
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Background = new SolidColorBrush(Color.FromArgb(128,128,128,128));
            ((DockPanel)sender).Opacity = 1;
        }

        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Background = new SolidColorBrush(Colors.Transparent);
            ((DockPanel)sender).Opacity = 0.2;
        }


        public double Scale(double value, double l1, double u1, double l2, double u2, bool isInverted = false)
        {
            if (isInverted)
                return (value - l2) / (u2 - l2) * (u1 - l1) + l1;
            else
                return (value - l1) / (u1 - l1) * (u2 - l2) + l2;
        }

        private void Cb_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
                return;
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

        private void Randomise()
        {
            Random x = new Random();
            isInitialised = false;

            var f1 = x.Next(1, 21) + 0f;
            var f2 = x.Next(1, 21) + 0f;
            var nearMiss = 1f + (x.Next(0, 5) < 1 ? 0 : 1) * x.Next(-5, 5) / 100f;
            Console.WriteLine(nearMiss + " " + x.Next(0, 1));
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
            return Scale(slider.Value, slider.Minimum, slider.Maximum, l, u);
        }

        public void SetSliderValue(Slider slider, double value, double l, double u)
        {
            slider.Value = Scale(value, slider.Minimum, slider.Maximum, l, u, true);
        }

        private void ResetButton_Clicked(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        public double TimeScale { get { return Math.Pow(10, GetSliderValue(sliderT, -2, 2)); } }

        internal class Oscillator
        {
            public double A, F, P, D;

            public Oscillator()
            {

            }

            public Oscillator(double a, double f, double p = 0, double d = 0)
            {
                A = a;
                F = f;
                P = p;
                D = d;
            }

            public double U(double t)
            {
                return A * Math.Sin(F * t + P * Math.PI / 180) * Math.Exp(-t * D);
            }

            public double C(double t)
            {
                return A * Math.Cos(F * t + P * Math.PI / 180) * Math.Exp(-t * D);
            }

            public double S(double t)
            {
                return A * Math.Sin(F * t) * Math.Exp(-t * D);
            }
        }
    }
}
