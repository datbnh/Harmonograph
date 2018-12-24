using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Harmonograph
{
    // https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/how-to-create-a-linesegment-in-a-pathgeometry


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Oscillator oH1, oH2, oH3, oV1, oV2, oV3, oC;

        DispatcherTimer plotTimer;
        
        Stopwatch stopwatch;

        private bool isUpdating;

        private double lastT = 0;

        double maxFps = 0;
        double minFps = double.MaxValue;
        double sumFps = 0;
        long frameCount = 0;

        bool isInitialised = false;

        public double OffsetH;
        public double OffsetV;

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

            plotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1)
            };
            plotTimer.Tick += Timer_Tick;

            stopwatch = new Stopwatch();

            oH1 = new Oscillator();
            oH2 = new Oscillator();
            oV1 = new Oscillator();
            oV2 = new Oscillator();
            oH3 = new Oscillator();
            oV3 = new Oscillator();

            oC = new Oscillator(360, 0.001);

            isInitialised = true;

            UpdateFc();
            UpdateOscillatorParameters();


            plotTimer.Start();
            stopwatch.Start();

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdatePlot();
        }

        private void UpdatePlot()
        {
            if (isUpdating)
            {
                Console.WriteLine("Drop");
                return;
            }

            isUpdating = true;

            var fps = 1 / (stopwatch.ElapsedMilliseconds / 1000f);

            var currentT = lastT + stopwatch.ElapsedMilliseconds/sliderT.Value;
            stopwatch.Restart();

            var numPoints = 100;

            PathFigure pathFigure = new PathFigure { StartPoint = new Point(X(lastT), Y(lastT)) };
            PathSegmentCollection pathSegments = new PathSegmentCollection();

            for (int i = 1; i <= numPoints; i++)
            {
                double t = lastT + (currentT - lastT) * i / numPoints;
                pathSegments.Add(new LineSegment(new Point(X(t), Y(t)), true));
            }
            pathFigure.Segments = pathSegments;

            PathFigureCollection pathFigures = new PathFigureCollection { pathFigure };

            PathGeometry pathGeometry = new PathGeometry { Figures = pathFigures };

            Path path = new Path
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(ColorFromAHSV(255, oC.U(currentT), 0.5, 1))
            };
            path.Data = pathGeometry;

            canvas1.Children.Add(path);

            if (canvas1.Children.Count > sliderTrailLength.Value)
            {
                canvas1.Children.RemoveAt(0);
            }

            // Path > Path.Data = PathGemometry > PathGemometry.Figures = PathFigureCollection > 
            // PathFigure > PathFigure.StartPoint, PathFigure.Segments = PathSegmentCollection >
            // LineSegment > Point
            
            maxFps = fps > maxFps ? fps : maxFps;
            minFps = fps < minFps ? fps : minFps;
            sumFps += fps;
            frameCount++;
            label1.Content = (fps.ToString("00.00") 
                + " | " + minFps.ToString("00.00") 
                + " | " + maxFps.ToString("00.00")
                + " | " + (sumFps/frameCount).ToString("00.00")
                + " | " + currentT.ToString("00.00"));
            lastT = currentT;
            
            isUpdating = false;
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

            //canvas1.Children.Clear();
        }

        public double X(double t)
        {
            return oH1.U(t) + oH2.U(t) + oH3.U(t) + OffsetH;
        }

        public double Y(double t)
        {
            return oV1.U(t) + oV2.U(t) + oV3.U(t) + OffsetV;
        }

        private void SliderColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialised)
                return;
            UpdateFc();
        }

        private void UpdateFc()
        {
            oC.F = sliderFc.Value / 1000;
        }

        private void TbAx1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialised)
                return;
            UpdateOscillatorParameters();
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Background = new SolidColorBrush(Color.FromArgb(128,128,128,128));
        }

        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            ((DockPanel)sender).Background = new SolidColorBrush(Colors.Transparent);
        }


        public double Scale(double value, double l1, double u1, double l2, double u2, bool isInverted = false)
        {
            if (isInverted)
                return (value - l2) / (u2 - l2) * (u1 - l1) + l1;
            else
                return (value - l1) / (u1 - l1) * (u2 - l2) + l2;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Random x = new Random();
            isInitialised = false;
            //var a1 = x.Next(000, 500);
            //var a2 = x.Next(0, 700-a1);
            //tbAx1.Text = (a1).ToString();
            //tbAx2.Text = (a2).ToString();
            //tbAy1.Text = (a1).ToString();
            //tbAy2.Text = (a2).ToString();

            //var f1 = 1;//x.Next(0, 1000) / 100f;
            //var f3 = x.Next(150, 2000) / 100f;
            //var f2 = f1 * (x.Next(-100, 100) / 10000f + 1);
            //var f4 = f3 * (x.Next(-100, 100) / 100000f + 1);
            //tbFx1.Text = (f1).ToString();
            //tbFx2.Text = (f3).ToString();
            //tbFy1.Text = (f2).ToString();
            //tbFy2.Text = (f4).ToString();

            //var p1 = x.Next(-180, 180);
            //var p2 = x.Next(-180, 180);
            //tbPx1.Text = p1.ToString(); // (x.Next(0, 180)).ToString();
            //tbPx2.Text = p2.ToString();
            //tbPy1.Text = (p1 + 85 + x.Next(0, 10)).ToString();
            //tbPy2.Text = (p2 + 85 + x.Next(0, 10)).ToString();

            //tbDx1.Text = (x.Next(50, 500)/1000000f).ToString();
            //tbDx2.Text = (x.Next(200, 1500)/1000000f).ToString();
            //tbDy1.Text = (x.Next(50, 500)/1000000f).ToString();
            //tbDy2.Text = (x.Next(200, 1500)/1000000f).ToString();

            isInitialised = true;
            UpdateOscillatorParameters();
            Reset();
        }

        public double GetSliderValue(Slider slider, double l, double u)
        {
            return Scale(slider.Value, slider.Minimum, slider.Maximum, l, u);
        }

        public void SetSliderValue(Slider slider, double value, double l, double u)
        {
            slider.Value = Scale(value, slider.Minimum, slider.Maximum, l, u, true);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void Reset()
        {
            lastT = 0;
            stopwatch.Restart();
            canvas1.Children.Clear();
        }

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
