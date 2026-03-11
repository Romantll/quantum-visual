using UnityEngine;
using TMPro;

namespace QuantumVisualizer
{
    /// <summary>
    /// Renders the Bloch sphere and smoothly animates the state vector arrow
    /// to reflect the current qubit state.
    ///
    /// The Bloch sphere maps every pure qubit state to a unique point on the
    /// surface of a unit sphere. The north pole represents |0>, the south pole
    /// represents |1>, and all points on the equator are equal superpositions
    /// that differ only in their relative phase. This makes gate effects
    /// immediately visible: Pauli-X flips the arrow from pole to pole, Hadamard
    /// moves it from a pole to the equator, and Rz spins it around the vertical axis.
    ///
    /// Scene setup:
    ///   1. Create a sphere GameObject (scale roughly 2, 2, 2) and give it a
    ///      transparent or wireframe material. Assign it to BlochSphereRoot.
    ///      The transparency lets you see the arrow pass through the interior,
    ///      which makes rotations through the sphere much easier to follow.
    ///   2. Create an arrow GameObject (a thin cylinder with a cone tip works well,
    ///      parented under an empty called "StateVector"). Assign it to StateVectorRoot.
    ///      The arrow must point in its own local +Y direction so the rotation math
    ///      in OnStateChanged aligns correctly.
    ///   3. Optionally create 6 TextMeshPro (world space) label objects and assign
    ///      them. Suggested positions at radius 1.3 from the sphere center:
    ///        |0>  at (0,  +1.3, 0)
    ///        |1>  at (0,  -1.3, 0)
    ///        |+>  at (+1.3, 0, 0)
    ///        |->  at (-1.3, 0, 0)
    ///        |+i> at (0, 0, +1.3)
    ///        |-i> at (0, 0, -1.3)
    ///   4. Add this component to any GameObject and assign SimulatorRef in the
    ///      Inspector, or leave it blank and it will find QubitSimulator automatically.
    /// </summary>
    public class BlochSphereVisualizer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The QubitSimulator this visualizer listens to. Leave blank to auto-find.")]
        [SerializeField] private QubitSimulator _simulator;

        [Tooltip("Root transform of the Bloch sphere mesh.")]
        [SerializeField] private Transform _blochSphereRoot;

        [Tooltip("Root transform of the state vector arrow. Its local +Y axis must point toward the tip.")]
        [SerializeField] private Transform _stateVectorRoot;

        [Header("Axis Labels (optional)")]
        [SerializeField] private TextMeshPro _label0;
        [SerializeField] private TextMeshPro _label1;
        [SerializeField] private TextMeshPro _labelPlus;
        [SerializeField] private TextMeshPro _labelMinus;
        [SerializeField] private TextMeshPro _labelPlusI;
        [SerializeField] private TextMeshPro _labelMinusI;

        [Header("Animation")]
        [Tooltip("Maximum rotation speed of the state vector arrow in degrees per second. A finite speed makes gate effects easier to follow than instant snapping.")]
        [SerializeField] private float _rotationSpeed = 180f;

        [Header("Colors")]
        [Tooltip("Arrow color when the qubit is in superposition. Blue-ish by default to suggest quantum uncertainty.")]
        [SerializeField] private Color _superpositionColor = new Color(0.2f, 0.8f, 1f);

        [Tooltip("Arrow color when the qubit is in a definite basis state. Yellow by default to suggest a classical, resolved value.")]
        [SerializeField] private Color _basisStateColor = new Color(1f, 0.9f, 0.2f);


        private Quaternion _targetRotation;
        private Renderer   _arrowRenderer;


        private void Awake()
        {
            if (_simulator == null)
                _simulator = FindObjectOfType<QubitSimulator>();

            if (_stateVectorRoot != null)
                _arrowRenderer = _stateVectorRoot.GetComponentInChildren<Renderer>();

            SetupAxisLabels();
        }

        private void OnEnable()
        {
            if (_simulator != null)
            {
                _simulator.OnStateChanged.AddListener(OnStateChanged);

                // Sync immediately so the arrow starts in the correct position
                // rather than animating in from the default rotation on the first frame.
                OnStateChanged(_simulator.State);
            }
        }

        private void OnDisable()
        {
            if (_simulator != null)
                _simulator.OnStateChanged.RemoveListener(OnStateChanged);
        }

        private void Update()
        {
            if (_stateVectorRoot == null) return;

            // RotateTowards moves at a constant angular speed rather than
            // using an exponential Slerp. This means the arrow always takes the
            // same amount of time per degree, making the animation speed predictable
            // and easy to tune via the _rotationSpeed field.
            _stateVectorRoot.rotation = Quaternion.RotateTowards(
                _stateVectorRoot.rotation,
                _targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }

        private void OnStateChanged(QuantumState state)
        {
            if (state == null) return;

            Vector3 blochVec = state.BlochVector;

            // FromToRotation computes the shortest rotation that takes the arrow's
            // resting direction (local +Y, i.e. world Vector3.up before any rotation)
            // to the target Bloch vector direction. We set this as a target rather
            // than applying it directly so the Update loop can animate toward it smoothly.
            if (blochVec.sqrMagnitude > 1e-6f)
                _targetRotation = Quaternion.FromToRotation(Vector3.up, blochVec.normalized);

            // Color the arrow so a viewer can tell at a glance whether the qubit is
            // in a definite state or a superposition, without needing to read numbers.
            if (_arrowRenderer != null)
            {
                _arrowRenderer.material.color = state.IsInSuperposition
                    ? _superpositionColor
                    : _basisStateColor;
            }
        }

        private void SetupAxisLabels()
        {
            // Only set the text if the label object exists and has not already been
            // given custom text in the editor. This lets a designer override the default
            // strings (e.g. with localized text) without the code overwriting them at runtime.
            SetLabelText(_label0,      "|0\u27e9");
            SetLabelText(_label1,      "|1\u27e9");
            SetLabelText(_labelPlus,   "|+\u27e9");
            SetLabelText(_labelMinus,  "|-\u27e9");
            SetLabelText(_labelPlusI,  "|+i\u27e9");
            SetLabelText(_labelMinusI, "|-i\u27e9");
        }

        private static void SetLabelText(TextMeshPro label, string text)
        {
            if (label != null && string.IsNullOrEmpty(label.text))
                label.text = text;
        }
    }
}
