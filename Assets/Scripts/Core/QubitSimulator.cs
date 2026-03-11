using System;
using UnityEngine;
using UnityEngine.Events;

namespace QuantumVisualizer
{
    // We subclass UnityEvent rather than using plain C# events so that
    // visualizers and UI components can be wired up in the Unity Inspector
    // without writing any additional code. This keeps the project accessible
    // to designers and non-programmers working in the editor.
    [Serializable] public class StateChangedEvent : UnityEvent<QuantumState> { }
    [Serializable] public class MeasurementEvent  : UnityEvent<int>          { }

    /// <summary>
    /// Central controller MonoBehaviour for the single-qubit simulation.
    ///
    /// This class acts as the single source of truth for the qubit state.
    /// Keeping all state mutations here (rather than spread across visualizers)
    /// makes it easy to reason about what the qubit is doing at any point, and
    /// ensures every listener always receives a consistent snapshot.
    ///
    /// Visualizers and UI components subscribe to OnStateChanged so they update
    /// automatically whenever a gate is applied, a measurement occurs, or the
    /// state is reset. They never need to poll or manually request the state.
    ///
    /// Scene setup:
    ///   1. Add this component to a "QuantumSimulator" GameObject.
    ///   2. Wire OnStateChanged and OnMeasurementResult in the Inspector to any
    ///      visualizer or UI component that needs to react to state changes.
    /// </summary>
    public class QubitSimulator : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Fired after every gate application or reset. Passes the updated QuantumState to all listeners.")]
        public StateChangedEvent OnStateChanged = new();

        [Tooltip("Fired after a measurement collapses the state. Passes the outcome as 0 or 1.")]
        public MeasurementEvent OnMeasurementResult = new();

        [Header("Debug")]
        [Tooltip("Logs every gate application and measurement to the Console. Useful during development to trace what sequence of operations produced a given state.")]
        [SerializeField] private bool _debugLog = true;


        private QuantumState _state;

        /// <summary>
        /// Read-only access to the current qubit state.
        /// Exposed so visualizers can read the initial state on startup before
        /// any event has fired.
        /// </summary>
        public QuantumState State => _state;

        /// <summary>
        /// Outcome of the most recent measurement (0 or 1).
        /// Returns -1 if no measurement has been performed yet in this session.
        /// </summary>
        public int LastMeasurement { get; private set; } = -1;


        private void Awake()
        {
            // Initialize to the ground state |0⟩. This is the conventional
            // starting point in quantum computing, analogous to initializing a
            // classical bit to 0 before operating on it.
            _state = new QuantumState();

            // Subscribe to the internal C# event on QuantumState so we can
            // forward it as a UnityEvent. QuantumState uses C# events for
            // performance; we bridge to UnityEvent here at the controller layer
            // so the Unity Inspector wiring works correctly.
            _state.OnStateChanged += HandleInternalStateChange;
        }

        private void OnDestroy()
        {
            // Always unsubscribe from events when a MonoBehaviour is destroyed.
            // Failing to do so causes the delegate to hold a reference to this
            // object after it has been destroyed, which produces MissingReferenceExceptions.
            if (_state != null)
                _state.OnStateChanged -= HandleInternalStateChange;
        }


        // The fixed-gate methods are thin wrappers so that GatePanel (and any
        // other UI) can reference them by name in the Inspector's onClick list
        // without needing to know about the gate matrix representation.

        public void ApplyX() => Apply(QuantumGates.X,  "X (Pauli-X / NOT)");
        public void ApplyY() => Apply(QuantumGates.Y,  "Y (Pauli-Y)");
        public void ApplyZ() => Apply(QuantumGates.Z,  "Z (Pauli-Z / Phase-flip)");
        public void ApplyH() => Apply(QuantumGates.H,  "H (Hadamard)");
        public void ApplyS() => Apply(QuantumGates.S,  "S (S-gate, Z^0.5)");
        public void ApplyT() => Apply(QuantumGates.T,  "T (T-gate, Z^0.25)");

        // Rotation gates take an angle in radians. The caller (typically GatePanel)
        // reads the angle from a slider and passes it here, keeping angle state
        // in the UI layer rather than the simulation layer.
        public void ApplyRx(float theta) => Apply(QuantumGates.Rx(theta), $"Rx({theta * Mathf.Rad2Deg:F1} deg)");
        public void ApplyRy(float theta) => Apply(QuantumGates.Ry(theta), $"Ry({theta * Mathf.Rad2Deg:F1} deg)");
        public void ApplyRz(float theta) => Apply(QuantumGates.Rz(theta), $"Rz({theta * Mathf.Rad2Deg:F1} deg)");

        /// <summary>
        /// Performs a projective (Born rule) measurement.
        ///
        /// In quantum mechanics, measuring a qubit irreversibly collapses its
        /// superposition into a definite classical outcome. The probability of
        /// each outcome is the squared magnitude of its amplitude. We fire a
        /// separate OnMeasurementResult event (in addition to OnStateChanged) so
        /// that UI components can display the outcome independently of the general
        /// state update, for example showing "You measured |1⟩" in a result panel.
        /// </summary>
        public void Measure()
        {
            // Measurement collapses the state and fires OnStateChanged internally
            LastMeasurement = _state.Measure();

            if (_debugLog)
                Debug.Log($"[QubitSimulator] Measured |{LastMeasurement}>");

            OnMeasurementResult.Invoke(LastMeasurement);
        }

        /// <summary>
        /// Resets the qubit to the ground state |0⟩ and clears the last measurement.
        /// This mirrors the physical operation of reinitializing a qubit, for example
        /// by cooling it back to its ground state between experiments.
        /// </summary>
        public void Reset()
        {
            LastMeasurement = -1;

            // Reset fires OnStateChanged internally, which propagates to all
            // visualizers via HandleInternalStateChange
            _state.Reset();

            if (_debugLog)
                Debug.Log("[QubitSimulator] Reset to |0>");
        }


        private void Apply(ComplexNumber[,] gate, string gateName)
        {
            // ApplyGate fires OnStateChanged on QuantumState, which we forward
            // to the UnityEvent in HandleInternalStateChange
            _state.ApplyGate(gate);

            if (_debugLog)
                Debug.Log($"[QubitSimulator] Applied {gateName}  =>  {_state}");
        }

        // This bridge method exists because QuantumState uses a parameterless
        // C# event, but our public UnityEvent needs to pass the state object.
        // Rather than change QuantumState's event signature (which would couple
        // it to Unity), we translate here at the boundary.
        private void HandleInternalStateChange()
        {
            OnStateChanged.Invoke(_state);
        }
    }
}
