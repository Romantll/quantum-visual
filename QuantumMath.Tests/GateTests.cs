using System;
using NUnit.Framework;
using QuantumMath;

namespace QuantumMath.Tests
{
    [TestFixture]
    public class GateTests
    {
        private const double Tol = 1e-10;

        private static void AssertState(QubitState actual, double aReal, double aImag, double bReal, double bImag)
        {
            Assert.That(actual.Alpha.Real, Is.EqualTo(aReal).Within(Tol));
            Assert.That(actual.Alpha.Imag, Is.EqualTo(aImag).Within(Tol));
            Assert.That(actual.Beta.Real,  Is.EqualTo(bReal).Within(Tol));
            Assert.That(actual.Beta.Imag,  Is.EqualTo(bImag).Within(Tol));
        }

        // --- Hadamard ---
        [Test] public void H_on_Zero_gives_Plus() =>
            AssertState(Gates.Apply(Gates.H, QubitState.Zero),
                1/Math.Sqrt(2), 0, 1/Math.Sqrt(2), 0);

        [Test] public void H_on_One_gives_Minus() =>
            AssertState(Gates.Apply(Gates.H, QubitState.One),
                1/Math.Sqrt(2), 0, -1/Math.Sqrt(2), 0);

        [Test] public void H_is_its_own_inverse()
        {
            var recovered = Gates.Apply(Gates.H, Gates.Apply(Gates.H, QubitState.Zero));
            AssertState(recovered, 1, 0, 0, 0);
        }

        // --- Pauli-X ---
        [Test] public void X_on_Zero_gives_One() =>
            AssertState(Gates.Apply(Gates.X, QubitState.Zero), 0, 0, 1, 0);

        [Test] public void X_on_One_gives_Zero() =>
            AssertState(Gates.Apply(Gates.X, QubitState.One), 1, 0, 0, 0);

        // --- Pauli-Z ---
        [Test] public void Z_on_Zero_is_unchanged() =>
            AssertState(Gates.Apply(Gates.Z, QubitState.Zero), 1, 0, 0, 0);

        [Test] public void Z_on_One_flips_phase() =>
            AssertState(Gates.Apply(Gates.Z, QubitState.One), 0, 0, -1, 0);

        // --- Pauli-Y ---
        [Test] public void Y_on_Zero_gives_iOne()
        {
            var result = Gates.Apply(Gates.Y, QubitState.Zero);
            Assert.That(result.Alpha.Real, Is.EqualTo(0).Within(Tol));
            Assert.That(result.Beta.Imag,  Is.EqualTo(1).Within(Tol));
        }

        // --- S gate ---
        [Test] public void S_on_Zero_is_unchanged() =>
            AssertState(Gates.Apply(Gates.S, QubitState.Zero), 1, 0, 0, 0);

        [Test] public void S_on_One_gives_iOne()
        {
            var result = Gates.Apply(Gates.S, QubitState.One);
            Assert.That(result.Beta.Real, Is.EqualTo(0).Within(Tol));
            Assert.That(result.Beta.Imag, Is.EqualTo(1).Within(Tol));
        }

        // --- Bloch sphere mapping ---
        [Test] public void BlochZ_of_Zero_is_plus1() =>
            Assert.That(QubitState.Zero.BlochZ, Is.EqualTo(1.0).Within(Tol));

        [Test] public void BlochZ_of_One_is_minus1() =>
            Assert.That(QubitState.One.BlochZ, Is.EqualTo(-1.0).Within(Tol));

        [Test] public void BlochX_of_Plus_is_plus1() =>
            Assert.That(QubitState.Plus.BlochX, Is.EqualTo(1.0).Within(Tol));

        [Test] public void Probabilities_sum_to_one()
        {
            foreach (var state in new[] { QubitState.Zero, QubitState.One,
                                          QubitState.Plus, QubitState.Minus,
                                          QubitState.PlusI, QubitState.MinusI })
                Assert.That(state.P0 + state.P1, Is.EqualTo(1.0).Within(Tol));
        }

        // --- Gate sequence: H then Z then H = X ---
        [Test] public void HZH_equals_X()
        {
            var via_HZH = Gates.Apply(Gates.H, Gates.Apply(Gates.Z, Gates.Apply(Gates.H, QubitState.Zero)));
            var via_X   = Gates.Apply(Gates.X, QubitState.Zero);
            Assert.That(via_HZH.Alpha.Real, Is.EqualTo(via_X.Alpha.Real).Within(Tol));
            Assert.That(via_HZH.Beta.Real,  Is.EqualTo(via_X.Beta.Real).Within(Tol));
        }
    }
}
