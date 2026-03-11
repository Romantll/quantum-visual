using System;
using UnityEngine;

namespace QuantumVisualizer
{
    /// <summary>
    /// A minimal complex number struct tailored for single-qubit arithmetic.
    ///
    /// Unity's Mathf library operates on floats, and the built-in System.Numerics.Complex
    /// uses doubles. We use floats throughout because the precision is more than sufficient
    /// for visualization, and it keeps the code consistent with the rest of the Unity math
    /// stack without any conversion overhead.
    ///
    /// Making this a struct rather than a class means gate calculations allocate nothing
    /// on the heap. Gate application runs every frame in some visualizer modes, so keeping
    /// allocations low avoids garbage collection hitches.
    /// </summary>
    [Serializable]
    public struct ComplexNumber
    {
        public float real;
        public float imag;

        public ComplexNumber(float real, float imag)
        {
            this.real = real;
            this.imag = imag;
        }

        /// <summary>
        /// The distance from the origin in the complex plane.
        /// In quantum mechanics this is used to compute measurement probability via |amplitude|^2.
        /// </summary>
        public float Magnitude => Mathf.Sqrt(real * real + imag * imag);

        /// <summary>
        /// Squared magnitude, computed without a square root.
        /// Preferred over Magnitude when we only need the probability (Born rule: P = |amplitude|^2),
        /// since the extra sqrt in Magnitude would immediately be squared away.
        /// </summary>
        public float MagnitudeSq => real * real + imag * imag;

        /// <summary>
        /// The angle of the complex number in the complex plane, measured in radians from the positive real axis.
        /// This is the "phase" of a quantum amplitude. While phase has no effect on measurement probabilities
        /// by itself, relative phase between alpha and beta determines where the state sits on the Bloch sphere
        /// and is what interference effects depend on.
        /// </summary>
        public float Phase => Mathf.Atan2(imag, real);

        /// <summary>
        /// Flips the sign of the imaginary part.
        /// Used when computing the Hermitian conjugate (dagger) of a matrix, which is needed to
        /// verify that a gate is unitary. Not currently called internally but useful for debugging
        /// and for future gate validation.
        /// </summary>
        public readonly ComplexNumber Conjugate => new(real, -imag);

        public static ComplexNumber operator +(ComplexNumber a, ComplexNumber b)
            => new ComplexNumber(a.real + b.real, a.imag + b.imag);

        public static ComplexNumber operator -(ComplexNumber a, ComplexNumber b)
            => new ComplexNumber(a.real - b.real, a.imag - b.imag);

        public static ComplexNumber operator -(ComplexNumber a)
            => new ComplexNumber(-a.real, -a.imag);

        /// <summary>
        /// Complex multiplication follows (a + bi)(c + di) = (ac - bd) + (ad + bc)i.
        /// This is the core operation behind gate application: multiplying a gate matrix
        /// element by a state amplitude gives the contribution of that amplitude to the output.
        /// </summary>
        public static ComplexNumber operator *(ComplexNumber a, ComplexNumber b)
            => new ComplexNumber(
                a.real * b.real - a.imag * b.imag,
                a.real * b.imag + a.imag * b.real);

        public static ComplexNumber operator *(float s, ComplexNumber c)
            => new ComplexNumber(s * c.real, s * c.imag);

        public static ComplexNumber operator *(ComplexNumber c, float s)
            => new ComplexNumber(s * c.real, s * c.imag);

        public static ComplexNumber operator /(ComplexNumber c, float s)
            => new ComplexNumber(c.real / s, c.imag / s);

        /// <summary>
        /// Pre-defined values for the most common amplitudes so gate matrices
        /// can be written clearly without magic numbers scattered through QuantumGates.cs.
        /// </summary>
        public static readonly ComplexNumber Zero = new(0f, 0f);
        public static readonly ComplexNumber One  = new(1f, 0f);
        public static readonly ComplexNumber I    = new(0f, 1f);
        public static readonly ComplexNumber NegI = new(0f, -1f);

        /// <summary>
        /// Euler's formula: e^(i*theta) = cos(theta) + i*sin(theta).
        /// Used by rotation and phase gates to produce a unit-magnitude complex number
        /// at a given angle. Because the result always has magnitude 1, applying it as
        /// a phase factor cannot change the probability of a measurement outcome; it only
        /// rotates the state on the Bloch sphere.
        /// </summary>
        public static ComplexNumber Exp(float theta)
            => new ComplexNumber(Mathf.Cos(theta), Mathf.Sin(theta));

        /// <summary>
        /// Omits near-zero components so debug output stays readable.
        /// For example, the state |0> prints as "1.0000" rather than "(1.0000 + 0.0000i)".
        /// </summary>
        public override string ToString()
        {
            if (Mathf.Abs(imag) < 1e-4f) return $"{real:F4}";
            if (Mathf.Abs(real) < 1e-4f) return $"{imag:F4}i";
            string sign = imag >= 0 ? "+" : "-";
            return $"({real:F4} {sign} {Mathf.Abs(imag):F4}i)";
        }
    }
}
