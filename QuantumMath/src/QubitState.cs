using System;

namespace QuantumMath
{
    // Single-qubit pure state: α|0⟩ + β|1⟩, normalized so |α|²+|β|²=1
    public struct QubitState
    {
        public Complex Alpha; // amplitude for |0⟩
        public Complex Beta;  // amplitude for |1⟩

        public QubitState(Complex alpha, Complex beta)
        {
            double norm = Math.Sqrt(alpha.MagnitudeSquared + beta.MagnitudeSquared);
            Alpha = new Complex(alpha.Real / norm, alpha.Imag / norm);
            Beta  = new Complex(beta.Real  / norm, beta.Imag  / norm);
        }

        // Measurement probabilities
        public double P0 => Alpha.MagnitudeSquared;
        public double P1 => Beta.MagnitudeSquared;

        // Bloch sphere angles (0 ≤ θ ≤ π, 0 ≤ φ < 2π)
        public double Theta => 2.0 * Math.Acos(Math.Clamp(Alpha.Magnitude, 0.0, 1.0));
        public double Phi   => Math.Atan2(Beta.Imag, Beta.Real) - Math.Atan2(Alpha.Imag, Alpha.Real);

        // Bloch vector components — what Unity renders as the arrow
        public double BlochX => Math.Sin(Theta) * Math.Cos(Phi);
        public double BlochY => Math.Sin(Theta) * Math.Sin(Phi);
        public double BlochZ => Math.Cos(Theta);

        // The six canonical states
        public static QubitState Zero   => new(Complex.One,  Complex.Zero);
        public static QubitState One    => new(Complex.Zero, Complex.One);
        public static QubitState Plus   => new(new Complex(1/S2, 0),  new Complex(1/S2,  0));
        public static QubitState Minus  => new(new Complex(1/S2, 0),  new Complex(-1/S2, 0));
        public static QubitState PlusI  => new(new Complex(1/S2, 0),  new Complex(0,  1/S2));
        public static QubitState MinusI => new(new Complex(1/S2, 0),  new Complex(0, -1/S2));

        private static readonly double S2 = Math.Sqrt(2.0);

        public override string ToString() => $"({Alpha})|0⟩ + ({Beta})|1⟩";
    }
}
