using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuantumVisualizer
{
    /// <summary>
    /// Visualizes the qubit wave function through three complementary views.
    ///
    /// The three views each highlight a different aspect of the quantum state:
    ///
    ///   Probability bars show P(|0>) and P(|1>) as filled UI images. This is the
    ///   most directly interpretable view because it answers "what would I see if I
    ///   measured right now?" It uses the Born rule: P = |amplitude|^2.
    ///
    ///   Phasor needles show the complex phase of each amplitude as a rotating needle
    ///   on a clock face. Phase is invisible in the probability bars alone, but it
    ///   is physically meaningful: it determines how states interfere when gates are
    ///   applied. For example, Rz changes only the phase, so the probability bars stay
    ///   the same while the phasors rotate. This view makes that difference visible.
    ///
    ///   State labels show the raw alpha and beta values and a superposition indicator.
    ///   These are useful for checking calculations and for learning what the numbers
    ///   behind the visual representations actually mean.
    ///
    /// Scene setup:
    ///   Create a Canvas in Screen Space Overlay (or World Space if you prefer a 3D UI).
    ///
    ///   Probability bars:
    ///     Two UI Image objects. Set Image Type to Filled and Fill Method to Vertical
    ///     so the fill amount directly represents probability as a height fraction.
    ///     Assign to Prob0Bar and Prob1Bar.
    ///
    ///   Phasor needles (optional but recommended):
    ///     Two RectTransform objects (thin UI Images work well). Set the pivot point
    ///     to the bottom centre of each needle so it rotates around its base like a
    ///     clock hand. Assign to AlphaPhasor and BetaPhasor.
    ///
    ///   Labels (optional):
    ///     TextMeshProUGUI objects for AlphaLabel, BetaLabel, Prob0Label, Prob1Label,
    ///     SuperpositionLabel, and MeasurementResultLabel.
    ///
    ///   Assign SimulatorRef in the Inspector or leave blank to auto-find.
    /// </summary>
    public class WaveFunctionVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QubitSimulator _simulator;

        [Header("Probability Bars (UI Image, Fill Vertical)")]
        [SerializeField] private Image _prob0Bar;
        [SerializeField] private Image _prob1Bar;

        [Header("Phasor Needles (RectTransform, pivot at bottom centre)")]
        [SerializeField] private RectTransform _alphaPhasor;
        [SerializeField] private RectTransform _betaPhasor;

        [Header("Labels (TextMeshProUGUI)")]
        [SerializeField] private TextMeshProUGUI _alphaLabel;
        [SerializeField] private TextMeshProUGUI _betaLabel;
        [SerializeField] private TextMeshProUGUI _prob0Label;
        [SerializeField] private TextMeshProUGUI _prob1Label;
        [SerializeField] private TextMeshProUGUI _superpositionLabel;
        [SerializeField] private TextMeshProUGUI _measurementResultLabel;

        [Header("Colors")]
        [Tooltip("Color for |0> elements. Blue suggests the ground state by convention.")]
        [SerializeField] private Color _color0     = new Color(0.3f, 0.7f, 1.0f);

        [Tooltip("Color for |1> elements. Red contrasts with blue to make the two states easy to distinguish.")]
        [SerializeField] private Color _color1     = new Color(1.0f, 0.4f, 0.3f);

        [Tooltip("Color for the superposition indicator. Green is used to avoid confusion with either basis state color.")]
        [SerializeField] private Color _superColor = new Color(0.2f, 1.0f, 0.5f);

        [SerializeField] private Color _basisColor = Color.white;


        private void Awake()
        {
            if (_simulator == null)
                _simulator = FindObjectOfType<QubitSimulator>();

            // Apply colors at Awake rather than at each update to avoid setting
            // material properties every frame for values that never change.
            ApplyBarColors();
        }

        private void OnEnable()
        {
            if (_simulator != null)
            {
                _simulator.OnStateChanged.AddListener(OnStateChanged);
                _simulator.OnMeasurementResult.AddListener(OnMeasured);

                // Populate the display immediately so it reflects the current state
                // rather than showing empty or zero values until the first event fires.
                OnStateChanged(_simulator.State);
            }
        }

        private void OnDisable()
        {
            if (_simulator != null)
            {
                _simulator.OnStateChanged.RemoveListener(OnStateChanged);
                _simulator.OnMeasurementResult.RemoveListener(OnMeasured);
            }
        }

        private void OnStateChanged(QuantumState state)
        {
            if (state == null) return;

            UpdateProbabilityBars(state);
            UpdatePhasors(state);
            UpdateLabels(state);
        }

        private void OnMeasured(int outcome)
        {
            // We keep the measurement result visible after the state collapses so the
            // player has time to read it. It is hidden again in UpdateLabels, which runs
            // on the next state change (e.g. when a new gate is applied or Reset is called).
            if (_measurementResultLabel != null)
            {
                _measurementResultLabel.text  = $"Measured: |{outcome}\u27e9";
                _measurementResultLabel.color = outcome == 0 ? _color0 : _color1;
                _measurementResultLabel.gameObject.SetActive(true);
            }
        }

        private void UpdateProbabilityBars(QuantumState state)
        {
            // fillAmount expects a value in [0, 1], and Prob0 and Prob1 are already
            // in that range by the normalization invariant, so no clamping is needed.
            if (_prob0Bar != null) _prob0Bar.fillAmount = state.Prob0;
            if (_prob1Bar != null) _prob1Bar.fillAmount = state.Prob1;
        }

        private void UpdatePhasors(QuantumState state)
        {
            // The phase of a complex amplitude is the angle it makes with the positive
            // real axis, measured in radians. We convert to degrees here because Unity's
            // transform system uses degrees. The needle rotates to that angle so a viewer
            // can see the relative phase between alpha and beta directly.
            if (_alphaPhasor != null)
            {
                float deg = state.Alpha.Phase * Mathf.Rad2Deg;
                _alphaPhasor.localEulerAngles = new Vector3(0f, 0f, deg);
            }

            if (_betaPhasor != null)
            {
                float deg = state.Beta.Phase * Mathf.Rad2Deg;
                _betaPhasor.localEulerAngles = new Vector3(0f, 0f, deg);
            }
        }

        private void UpdateLabels(QuantumState state)
        {
            if (_alphaLabel != null)
                _alphaLabel.text = $"alpha = {state.Alpha}";

            if (_betaLabel != null)
                _betaLabel.text = $"beta = {state.Beta}";

            if (_prob0Label != null)
                _prob0Label.text = $"P(|0>) = {state.Prob0:P1}";

            if (_prob1Label != null)
                _prob1Label.text = $"P(|1>) = {state.Prob1:P1}";

            if (_superpositionLabel != null)
            {
                bool inSuper = state.IsInSuperposition;
                _superpositionLabel.text  = inSuper ? "SUPERPOSITION" : "BASIS STATE";
                _superpositionLabel.color = inSuper ? _superColor : _basisColor;
            }

            // Any stale measurement result from before this gate was applied is now
            // out of date because the state has changed. Hide it so we do not imply
            // that the displayed outcome reflects the current state.
            if (_measurementResultLabel != null)
                _measurementResultLabel.gameObject.SetActive(false);
        }

        private void ApplyBarColors()
        {
            if (_prob0Bar != null) _prob0Bar.color = _color0;
            if (_prob1Bar != null) _prob1Bar.color = _color1;
        }
    }
}
