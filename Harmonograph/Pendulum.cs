using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Harmonograph
{
    public class Pendulum
    {
        private Oscillator OscillatorX;
        private Oscillator OscillatorY;

        public double AmplitudeX { get => OscillatorX.Amplitude; set => OscillatorX.Amplitude = value; }
        public double AmplitudeY { get => OscillatorY.Amplitude; set => OscillatorY.Amplitude = value; }
        public double InitialPhaseX { get => OscillatorX.InitialPhase; set => OscillatorX.InitialPhase = value; }
        public double InitialPhaseY { get => OscillatorY.InitialPhase; set => OscillatorY.InitialPhase = value; }
        public double AngularFrequency
        {
            get => OscillatorX.AngularFrequency;
            set
            {
                OscillatorX.AngularFrequency = value;
                OscillatorY.AngularFrequency = value;
            }
        }

        public double DecayConstant
        {
            get => OscillatorX.DecayConstant;
            set
            {
                OscillatorX.DecayConstant = value;
                OscillatorY.DecayConstant = value;
            }
        }

        private bool isActivated { get; set; }

        public Pendulum(double amplitudeX, double amplitudeY, double initialPhaseX, double initialPhaseY,
            double angularFrequency, double decayConstant, bool isActivated = true)
        {
            OscillatorX = new Oscillator(amplitudeX, angularFrequency, initialPhaseX, decayConstant);
            OscillatorY = new Oscillator(amplitudeY, angularFrequency, initialPhaseY, decayConstant);
            this.isActivated = isActivated;
        }

        public void Activate()
        {
            isActivated = true;
        }

        public void Deactivate()
        {
            isActivated = false;
        }

        public double GetInstantaniousXValue(double time)
        {
            if (!isActivated)
                return 0;
            return OscillatorX.GetInstantaniousAmplitutdeAtTime(time);
        }

        public double GetInstantaniousYValue(double time)
        {
            if (!isActivated)
                return 0;
            return OscillatorY.GetInstantaniousAmplitutdeAtTime(time);
        }

        public Point GetInstantaniousCoordinate(double time)
        {
            return new Point(GetInstantaniousXValue(time), GetInstantaniousYValue(time));
        }
    }
}
