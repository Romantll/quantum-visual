using System;

namespace QuantumMath
{
    public readonly struct Complex
    {
        public readonly double Real;
        public readonly double Imag;

        public Complex(double real, double imag) { Real = real; Imag = imag; }

        public static readonly Complex Zero = new(0, 0);
        public static readonly Complex One  = new(1, 0);
        public static readonly Complex I    = new(0, 1);

        public double Magnitude        => Math.Sqrt(Real * Real + Imag * Imag);
        public double MagnitudeSquared => Real * Real + Imag * Imag;
        public Complex Conjugate       => new(Real, -Imag);

        public static Complex operator +(Complex a, Complex b) =>
            new(a.Real + b.Real, a.Imag + b.Imag);

        public static Complex operator -(Complex a, Complex b) =>
            new(a.Real - b.Real, a.Imag - b.Imag);

        public static Complex operator *(Complex a, Complex b) =>
            new(a.Real * b.Real - a.Imag * b.Imag,
                a.Real * b.Imag + a.Imag * b.Real);

        public static Complex operator *(double s, Complex c) =>
            new(s * c.Real, s * c.Imag);

        public override string ToString() =>
            Imag >= 0 ? $"{Real:F6}+{Imag:F6}i" : $"{Real:F6}{Imag:F6}i";
    }
}
