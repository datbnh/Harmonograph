using System;

namespace Harmonograph
{
    internal class Oscillator
    {
        public double Amplitude, AngularFrequency, InitialPhase, DecayConstant;

        public Oscillator()
        {

        }

        public Oscillator(double amplitude, double angularFrequency, double initialPhase = 0, double decayConstant = 0)
        {
            Amplitude = amplitude;
            AngularFrequency = angularFrequency;
            InitialPhase = initialPhase;
            DecayConstant = decayConstant;
        }

        /// <summary>
        /// Returns the instantanious amplitude U at time t following the equation:
        /// U(t) = A*Sin(omega*t + phi) * exp(-t*d)
        /// where A is amplitude, 
        /// omega is angular frequency, 
        /// phi is initial phase 
        /// and d is decay constant of the oscillation.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double GetInstantaniousAmplitutdeAtTime(double t)
        {
            return Amplitude * Math.Sin(AngularFrequency * t + InitialPhase * Math.PI / 180)
                * Math.Exp(-t * DecayConstant);
        }

        /// <summary>
        /// Frequency in Hertz.
        /// </summary>
        public double Fequency { get => AngularFrequency / 2d / Math.PI; set => AngularFrequency = 2d * Math.PI * value; }
        /// <summary>
        /// Period in second.
        /// </summary>
        public double Period { get => 1d / Fequency; set => Fequency = 1d / value; }
    }
}
