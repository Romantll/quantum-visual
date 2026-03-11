using UnityEngine;

namespace QuantumVisualizer
{
    /// <summary>
    /// A library of standard single-qubit quantum gates, each represented as a
    /// 2x2 complex unitary matrix.
    ///
    /// Unitarity (U * U_dagger = Identity) is the key property that makes these
    /// valid quantum gates. It guarantees that applying a gate preserves the
    /// normalization of the state (probabilities still sum to 1) and that every
    /// gate operation is reversible, which is a fundamental requirement of quantum
    /// mechanics.
    ///
    /// The matrix convention used throughout is gate[row, col], matching standard
    /// linear algebra notation where the first index is the row. This is consistent
    /// with how ApplyGate reads the matrix in QuantumState.
    ///
    /// All gate effects described below refer to their action on the Bloch sphere,
    /// which is the most intuitive way to understand single-qubit gates visually.
    /// </summary>
    public static class QuantumGates
    {
        // Computed once and reused. The Hadamard gate divides by sqrt(2), and
        // computing Mathf.Sqrt each time the property is accessed would be wasteful.
        private static readonly float INV_SQRT2 = 1f / Mathf.Sqrt(2f);


        /// <summary>
        /// Pauli-X gate, also called the quantum NOT gate.
        /// Flips |0> to |1> and |1> to |0>, exactly like a classical bit flip.
        /// On the Bloch sphere this is a 180-degree rotation around the X axis.
        /// </summary>
        public static ComplexNumber[,] X => new ComplexNumber[,]
        {
            { ComplexNumber.Zero, ComplexNumber.One  },
            { ComplexNumber.One,  ComplexNumber.Zero }
        };

        /// <summary>
        /// Pauli-Y gate.
        /// Like X, it flips the basis states, but it also applies a phase of i
        /// to |0> and -i to |1>. The imaginary off-diagonal entries are why Y
        /// produces a different trajectory on the Bloch sphere than X even though
        /// both are 180-degree rotations (Y rotates around the Y axis).
        /// </summary>
        public static ComplexNumber[,] Y => new ComplexNumber[,]
        {
            { ComplexNumber.Zero, ComplexNumber.NegI },
            { ComplexNumber.I,    ComplexNumber.Zero }
        };

        /// <summary>
        /// Pauli-Z gate, also called the phase-flip gate.
        /// Leaves |0> unchanged and flips the sign of |1>. This has no effect on
        /// measurement probabilities for a basis state, but changes the relative
        /// phase between alpha and beta, rotating the state 180 degrees around the
        /// Z axis on the Bloch sphere. Its effect is visible in the phasor display.
        /// </summary>
        public static ComplexNumber[,] Z => new ComplexNumber[,]
        {
            { ComplexNumber.One,              ComplexNumber.Zero        },
            { ComplexNumber.Zero, new ComplexNumber(-1f, 0f) }
        };

        /// <summary>
        /// Hadamard gate.
        /// The workhorse of quantum computing for creating superposition. Applying H
        /// to a basis state produces an equal-probability superposition of |0> and |1>.
        /// Applying it twice returns to the original state (H is its own inverse).
        ///
        /// On the Bloch sphere, H swaps the Z and X axes, moving |0> to |+> and |1> to |->.
        ///   H|0> = |+> = (|0> + |1>) / sqrt(2)
        ///   H|1> = |-> = (|0> - |1>) / sqrt(2)
        ///
        /// The 1/sqrt(2) factor ensures the output state is still normalized.
        /// </summary>
        public static ComplexNumber[,] H => new ComplexNumber[,]
        {
            { new ComplexNumber( INV_SQRT2, 0f), new ComplexNumber( INV_SQRT2, 0f) },
            { new ComplexNumber( INV_SQRT2, 0f), new ComplexNumber(-INV_SQRT2, 0f) }
        };

        /// <summary>
        /// S gate (also called the phase gate).
        /// Adds a 90-degree (pi/2) phase shift to |1> without affecting |0>.
        /// Applying S twice is equivalent to applying Z. S is important in
        /// quantum algorithms because, combined with H, it can generate any
        /// state on the upper hemisphere of the Bloch sphere.
        /// </summary>
        public static ComplexNumber[,] S => new ComplexNumber[,]
        {
            { ComplexNumber.One,  ComplexNumber.Zero },
            { ComplexNumber.Zero, ComplexNumber.I    }
        };

        /// <summary>
        /// T gate (also called the pi/8 gate).
        /// Adds a 45-degree (pi/4) phase shift to |1>. Applying T twice gives S,
        /// and applying T four times gives Z. T is significant in fault-tolerant
        /// quantum computing because it is more expensive to implement than Clifford
        /// gates (X, Y, Z, H, S) and limits circuit speed in error-corrected systems.
        /// </summary>
        public static ComplexNumber[,] T => new ComplexNumber[,]
        {
            { ComplexNumber.One,  ComplexNumber.Zero               },
            { ComplexNumber.Zero, ComplexNumber.Exp(Mathf.PI / 4f) }
        };

        /// <summary>
        /// Rx(theta): continuous rotation by angle theta around the Bloch X axis.
        ///
        /// Unlike the fixed Pauli-X gate which always rotates by pi, Rx lets the
        /// user rotate by any amount. Setting theta = pi reproduces the X gate.
        /// Setting theta = pi/2 produces a "half-flip" that creates a specific
        /// superposition useful as a building block in many quantum algorithms.
        ///
        /// Derived from the matrix exponential: Rx(theta) = exp(-i * theta/2 * X)
        ///   = cos(theta/2) * I - i * sin(theta/2) * X
        /// </summary>
        public static ComplexNumber[,] Rx(float theta)
        {
            float c = Mathf.Cos(theta / 2f);
            float s = Mathf.Sin(theta / 2f);
            return new ComplexNumber[,]
            {
                { new ComplexNumber(c, 0f),  new ComplexNumber(0f, -s) },
                { new ComplexNumber(0f, -s), new ComplexNumber(c, 0f)  }
            };
        }

        /// <summary>
        /// Ry(theta): continuous rotation by angle theta around the Bloch Y axis.
        ///
        /// Unlike Rx, the Ry matrix has only real entries. This means Ry maps real
        /// states to real states, which can be useful when building circuits where
        /// you want to avoid introducing imaginary components. Setting theta = pi
        /// reproduces the Y gate up to global phase.
        ///
        /// Derived from: Ry(theta) = exp(-i * theta/2 * Y)
        ///   = cos(theta/2) * I - i * sin(theta/2) * Y
        /// </summary>
        public static ComplexNumber[,] Ry(float theta)
        {
            float c = Mathf.Cos(theta / 2f);
            float s = Mathf.Sin(theta / 2f);
            return new ComplexNumber[,]
            {
                { new ComplexNumber( c, 0f), new ComplexNumber(-s, 0f) },
                { new ComplexNumber( s, 0f), new ComplexNumber( c, 0f) }
            };
        }

        /// <summary>
        /// Rz(theta): continuous rotation by angle theta around the Bloch Z axis.
        ///
        /// Rz only modifies the phase of each amplitude; it never changes the
        /// measurement probabilities of a state. This makes it useful for
        /// fine-tuning the azimuthal position on the Bloch sphere without
        /// disturbing the polar angle (and therefore without changing Prob0 or Prob1).
        /// The phasor display in WaveFunctionVisualizer makes Rz's effect clearly visible.
        ///
        /// Derived from: Rz(theta) = exp(-i * theta/2 * Z)
        ///   = e^(-i*theta/2)|0><0| + e^(i*theta/2)|1><1|
        /// </summary>
        public static ComplexNumber[,] Rz(float theta)
        {
            return new ComplexNumber[,]
            {
                { ComplexNumber.Exp(-theta / 2f), ComplexNumber.Zero            },
                { ComplexNumber.Zero,             ComplexNumber.Exp(theta / 2f) }
            };
        }

        /// <summary>
        /// Identity gate: leaves the state completely unchanged.
        /// Included so circuit-building code can insert a no-op gate in a time slot
        /// without needing a special null check. Applying Identity is equivalent
        /// to doing nothing, which is useful when visualizing multi-gate sequences
        /// where one qubit is idle while another is being operated on.
        /// </summary>
        public static ComplexNumber[,] Identity => new ComplexNumber[,]
        {
            { ComplexNumber.One,  ComplexNumber.Zero },
            { ComplexNumber.Zero, ComplexNumber.One  }
        };
    }
}
