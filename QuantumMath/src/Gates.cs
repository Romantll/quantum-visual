using System;

namespace QuantumMath
{
    // Each gate is a 2×2 complex matrix stored as [row, col].
    // Apply() computes G|ψ⟩.
    public static class Gates
    {
        public static QubitState Apply(Complex[,] gate, QubitState state)
        {
            var alpha = gate[0, 0] * state.Alpha + gate[0, 1] * state.Beta;
            var beta  = gate[1, 0] * state.Alpha + gate[1, 1] * state.Beta;
            return new QubitState(alpha, beta);
        }

        private static readonly double S2 = Math.Sqrt(2.0);

        // Pauli-X (NOT)  [[0,1],[1,0]]
        public static readonly Complex[,] X = {
            { Complex.Zero, Complex.One  },
            { Complex.One,  Complex.Zero }
        };

        // Pauli-Y  [[0,-i],[i,0]]
        public static readonly Complex[,] Y = {
            { Complex.Zero,        new Complex(0, -1) },
            { new Complex(0, 1),   Complex.Zero       }
        };

        // Pauli-Z  [[1,0],[0,-1]]
        public static readonly Complex[,] Z = {
            { Complex.One,          Complex.Zero       },
            { Complex.Zero,         new Complex(-1, 0) }
        };

        // Hadamard  1/√2 [[1,1],[1,-1]]
        public static readonly Complex[,] H = {
            { new Complex(1/S2,  0), new Complex(1/S2,  0) },
            { new Complex(1/S2,  0), new Complex(-1/S2, 0) }
        };

        // Phase gate S  [[1,0],[0,i]]
        public static readonly Complex[,] S = {
            { Complex.One,  Complex.Zero },
            { Complex.Zero, Complex.I    }
        };

        // T gate (π/8)  [[1,0],[0,e^{iπ/4}]]
        public static readonly Complex[,] T = {
            { Complex.One,  Complex.Zero },
            { Complex.Zero, new Complex(1/S2, 1/S2) }
        };
    }
}
