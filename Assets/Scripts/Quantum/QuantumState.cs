using System;
using UnityEngine;

namespace QuantumVisualizer
{
    /// <summary>
    /// Represents a single-qubit pure state as |psi> = alpha|0> + beta|1>.
    ///
    /// The two complex numbers alpha and beta are called probability amplitudes.
    /// Their squared magnitudes give the probability of each measurement outcome
    /// (the Born rule): P(0) = |alpha|^2, P(1) = |beta|^2.
    ///
    /// The normalization constraint |alpha|^2 + |beta|^2 = 1 is enforced after
    /// every operation. Without this, floating-point rounding across many gate
    /// applications would cause the state to drift off the unit sphere and
    /// produce probabilities that no longer sum to 1.
    ///
    /// The computational basis vectors:
    ///   |0> = [1, 0] transpose  (ground state, analogous to classical bit 0)
    ///   |1> = [0, 1] transpose  (excited state, analogous to classical bit 1)
    /// </summary>
    [Serializable]
    public class QuantumState
    {
        // Alpha and beta are serializable so their values are visible in the
        // Unity Inspector during play mode, which makes it much easier to verify
        // that gates are being applied correctly while debugging.
        [SerializeField] private ComplexNumber _alpha = ComplexNumber.One;
        [SerializeField] private ComplexNumber _beta  = ComplexNumber.Zero;

        public ComplexNumber Alpha => _alpha;
        public ComplexNumber Beta  => _beta;

        /// <summary>
        /// A plain C# event rather than a UnityEvent because QuantumState is a
        /// plain data class with no Unity dependency. Keeping Unity concerns out of
        /// the data layer makes the state easier to unit test and reuse. QubitSimulator
        /// bridges this to a UnityEvent so the Inspector wiring still works.
        /// </summary>
        public event Action OnStateChanged;

        /// <summary>Initializes to the ground state |0>.</summary>
        public QuantumState() { }

        /// <summary>
        /// Initializes to an arbitrary state. Normalization is applied immediately
        /// so callers do not need to pre-normalize their inputs.
        /// </summary>
        public QuantumState(ComplexNumber alpha, ComplexNumber beta)
        {
            _alpha = alpha;
            _beta  = beta;
            Normalize();
        }

        /// <summary>
        /// Probability of measuring |0>, computed as |alpha|^2.
        /// Using MagnitudeSq avoids a square root that would immediately be
        /// squared back out if we used Magnitude here.
        /// </summary>
        public float Prob0 => _alpha.MagnitudeSq;

        /// <summary>Probability of measuring |1>, computed as |beta|^2.</summary>
        public float Prob1 => _beta.MagnitudeSq;

        /// <summary>
        /// Returns true when both basis states have non-negligible probability.
        /// The threshold of 1e-4 (0.01%) filters out floating-point noise after
        /// gates like X, which should produce a pure basis state but may leave
        /// a tiny residual imaginary component.
        /// </summary>
        public bool IsInSuperposition => Prob0 > 1e-4f && Prob1 > 1e-4f;

        /// <summary>
        /// Maps the qubit state to a point on the Bloch sphere, returned as a
        /// Unity Vector3 (unit length).
        ///
        /// The Bloch sphere is a geometrical representation where every pure qubit
        /// state corresponds to exactly one point on the surface of a unit sphere.
        /// This makes it possible to visualize what a gate does: applying a gate
        /// is equivalent to rotating the sphere.
        ///
        /// Derivation:
        ///   We strip the global phase by treating alpha as real and non-negative,
        ///   which is valid because global phase is unobservable (it cancels out
        ///   in all measurement probabilities).
        ///
        ///   With alpha = |alpha| and beta = |beta| * e^(i*phi):
        ///     theta = 2 * arccos(|alpha|)   (polar angle from the |0> pole)
        ///     phi   = arg(beta) - arg(alpha) (azimuthal angle, i.e. relative phase)
        ///
        ///   Bloch vector components:
        ///     x = sin(theta) * cos(phi)
        ///     y = sin(theta) * sin(phi)
        ///     z = cos(theta)    where z = +1 is |0> and z = -1 is |1>
        ///
        /// Unity axis mapping: Bloch(x, y, z) maps to Unity(x, z, y) so that
        /// |0> sits at Unity world up (+Y) and |1> at Unity world down (-Y).
        /// This feels natural when viewing the sphere with the camera level.
        /// </summary>
        public Vector3 BlochVector
        {
            get
            {
                float alphaMag = _alpha.Magnitude;

                float theta = 2f * Mathf.Acos(Mathf.Clamp(alphaMag, 0f, 1f));

                // Relative phase: how far beta is rotated around the Z axis relative to alpha.
                // This is the azimuthal position on the Bloch sphere equator.
                float phi = _beta.Phase - _alpha.Phase;

                float sinTheta = Mathf.Sin(theta);
                float blochX   = sinTheta * Mathf.Cos(phi);
                float blochY   = sinTheta * Mathf.Sin(phi);
                float blochZ   = Mathf.Cos(theta);

                return new Vector3(blochX, blochZ, blochY);
            }
        }

        /// <summary>
        /// Applies a unitary 2x2 gate matrix to the current state.
        ///
        /// Gate application is just matrix-vector multiplication:
        ///   [alpha']   [g00  g01] [alpha]
        ///   [beta' ] = [g10  g11] [beta ]
        ///
        /// We use two intermediate variables (newAlpha, newBeta) rather than
        /// writing directly to _alpha and _beta, because the second row of the
        /// multiplication reads the original _alpha. Writing _alpha first would
        /// corrupt the second calculation.
        /// </summary>
        public void ApplyGate(ComplexNumber[,] gate)
        {
            ComplexNumber newAlpha = gate[0, 0] * _alpha + gate[0, 1] * _beta;
            ComplexNumber newBeta  = gate[1, 0] * _alpha + gate[1, 1] * _beta;
            _alpha = newAlpha;
            _beta  = newBeta;
            Normalize();
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Simulates a projective measurement in the computational basis.
        ///
        /// In quantum mechanics, measurement is irreversible: the act of observing
        /// which basis state the qubit is in forces it to "choose" one, destroying
        /// the superposition. The probability of each outcome is given by the Born
        /// rule (P = |amplitude|^2). We use Unity's random number generator rather
        /// than System.Random to stay consistent with any seed the project sets via
        /// Random.InitState for reproducible demos.
        ///
        /// Returns 0 or 1 so the caller can display or log the outcome.
        /// </summary>
        public int Measure()
        {
            int outcome = UnityEngine.Random.value < Prob0 ? 0 : 1;
            if (outcome == 0)
            {
                _alpha = ComplexNumber.One;
                _beta  = ComplexNumber.Zero;
            }
            else
            {
                _alpha = ComplexNumber.Zero;
                _beta  = ComplexNumber.One;
            }
            OnStateChanged?.Invoke();
            return outcome;
        }

        /// <summary>
        /// Resets to the ground state |0>.
        /// This mirrors physically reinitializing a qubit between experiments,
        /// for example by cooling it back to its lowest energy state.
        /// </summary>
        public void Reset()
        {
            _alpha = ComplexNumber.One;
            _beta  = ComplexNumber.Zero;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Re-scales alpha and beta so that |alpha|^2 + |beta|^2 = 1.
        ///
        /// Each floating-point gate operation introduces small rounding errors.
        /// Over many sequential gates these compound, so we renormalize after
        /// every operation. The fallback to |0> handles the degenerate case where
        /// both amplitudes are so close to zero that dividing would produce NaN.
        /// </summary>
        private void Normalize()
        {
            float norm = Mathf.Sqrt(_alpha.MagnitudeSq + _beta.MagnitudeSq);
            if (norm > 1e-8f)
            {
                _alpha = _alpha / norm;
                _beta  = _beta  / norm;
            }
            else
            {
                _alpha = ComplexNumber.One;
                _beta  = ComplexNumber.Zero;
            }
        }

        public override string ToString()
            => $"|psi> = {_alpha}|0> + {_beta}|1>  (P0={Prob0:P1}, P1={Prob1:P1})";
    }
}
