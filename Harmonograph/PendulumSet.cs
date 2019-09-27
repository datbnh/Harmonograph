using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Harmonograph
{
    public class PendulumSet
    {
        //private Oscillator oC;

        private readonly Pendulum[] Pendulums;

        public readonly int NUM_PENDULUM = 3;

        public PendulumSet()
        {
            Pendulums = new Pendulum[NUM_PENDULUM];
            for (int i = 0; i < NUM_PENDULUM; i++)
            {
                Pendulums[i] = new Pendulum(0, 0, 0, 0, 0, 0);
            }
            //oC = new Oscillator(360, 0.001);
        }

        public void SetPendulumParameters(int pendulumIndex, double amplitudeX, double amplitudeY,
            double initialPhaseX, double initialPhaseY,
            double angularFrequency, double decayConstant)
        {
            if (IsPendulumIndexOutOfBound(pendulumIndex))
                throw new PendulumIndexOutOfBoundException();
            Pendulums[pendulumIndex].AmplitudeX = amplitudeX;
            Pendulums[pendulumIndex].AmplitudeY = amplitudeY;
            Pendulums[pendulumIndex].InitialPhaseX = initialPhaseX;
            Pendulums[pendulumIndex].InitialPhaseY = initialPhaseY;
            Pendulums[pendulumIndex].AngularFrequency = angularFrequency;
            Pendulums[pendulumIndex].DecayConstant = decayConstant;
        }

        /// <param name="index">Zero-based pendulum index</param>
        public void ActivatePendulum(int index)
        {
            SetPendulumState(index, true);
        }

        /// <param name="index">Zero-based pendulum index</param>
        public void DeactivatePendulum(int index)
        {
            SetPendulumState(index, false);
        }

        public bool IsPendulumActivated(int index)
        {
            if (IsPendulumIndexOutOfBound(index))
                throw new PendulumIndexOutOfBoundException();
            return
                Pendulums[index].IsActivated;
        }

        private void SetPendulumState(int index, bool isActivatedState)
        {
            if (IsPendulumIndexOutOfBound(index))
                throw new PendulumIndexOutOfBoundException();
            if (isActivatedState)
                Pendulums[index].Activate();
            else
                Pendulums[index].Deactivate();
        }

        private bool IsPendulumIndexOutOfBound(int index)
        {
            return index < 0 || index >= NUM_PENDULUM;
        }

        public Point GetInstantaniousCoordinate(double time)
        {
            var coordinate = new Point(0, 0);
            for (int i = 0; i < NUM_PENDULUM; i++)
            {
                coordinate.X += Pendulums[i].GetInstantaniousXValue(time);
                coordinate.Y += Pendulums[i].GetInstantaniousYValue(time);
            }
            return coordinate;
        }

        //public Color GetColorAtTime(double time)
        //{
        //    return Utilities.ColorFromAHSV(255, oC.GetInstantaniousAmplitutdeAtTime(time), 0.5, 1);
        //}

        public class PendulumIndexOutOfBoundException : Exception
        {
            public PendulumIndexOutOfBoundException() : base("Pendulum index out of bound.") {; }
        }

        //internal void SetColorAngularFrequency(double v)
        //{
        //    oC.AngularFrequency = v;
        //}
    }
}

