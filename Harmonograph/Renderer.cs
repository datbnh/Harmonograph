using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Harmonograph
{
    public class PathBuider
    {
        private readonly PendulumSet PendulumSet;
        public double TimeResolution { get; private set; } = 0.2;
        private long LastRenderedIndex;

        public enum ColoringMode
        {
            SEPARATE_COLOR_FOR_EACH_POINT,
            SAME_COLOR_AS_START_POINT_FOR_WHOLE_SEGMENT,
            SAME_COLOR_AS_END_POINT_FOR_WHOLE_SEGMENT,
        }

        private ColoringMode coloringMode;

        public PathBuider(PendulumSet pendulumSet)
        {
            PendulumSet = pendulumSet;
        }

        public Path GeneratePath(double startTime, double endTime)
        {
            List<Point> points = CalculateCoordinates(startTime, endTime);
            return ConstructPathFromCoordinates(points);
        }

        private List<Point> CalculateCoordinates(double startTime, double endTime)
        {
            var startIndex = Math.Min(GetPointIndexFromTime(startTime), LastRenderedIndex);
            var endIndex = GetPointIndexFromTime(endTime);

            //Console.WriteLine("{0} {1}", startIndex, endIndex);

            var points = new List<Point>();
            for (long i = startIndex; i <= endIndex; i++)
            {
                points.Add(PendulumSet.GetInstantaniousCoordinate(GetTimeFromPointIndex(i)));
            }

            LastRenderedIndex = endIndex;
            return points;
        }

        private Path ConstructPathFromCoordinates(List<Point> points)
        {
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = points[0],
            };

            PathSegmentCollection pathSegments = new PathSegmentCollection();

            for (int i = 1; i < points.Count; i++)
            {
                pathSegments.Add(new LineSegment(points[i], true));
            }
            pathFigure.Segments = pathSegments;

            PathFigureCollection pathFigures = new PathFigureCollection { pathFigure };

            PathGeometry pathGeometry = new PathGeometry { Figures = pathFigures, };

            Path path = new Path
            {
                StrokeThickness = 1,
                //Stroke = new SolidColorBrush(Utilities.ColorFromAHSV(255, oC.GetInstantaniousAmplitutdeAtTime(currentSegmentStartIdx), 0.5, 1))
                Stroke = new SolidColorBrush(Utilities.ColorFromAHSV(255, 180, 1, 1)),
            };
            path.Data = pathGeometry;

            return path;
        }


        private long GetPointIndexFromTime(double time)
        {
            return (long)Math.Round(time / TimeResolution);
        }

        private double GetTimeFromPointIndex(long index)
        {
            return index * TimeResolution;
        }

        //public struct ColorDefinedPoint
        //{
        //    public readonly Color Color;
        //    public readonly Vector Coordinate;

        //    public ColorDefinedPoint(Color color, Point coordinate)
        //    {
        //        Color = color;
        //        Coordinate = new Vector(coordinate.X, coordinate.Y);
        //    }
        //}
    }
}
