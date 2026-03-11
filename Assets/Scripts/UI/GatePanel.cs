using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuantumVisualizer
{
    /// <summary>
    /// Connects the Unity UI layer to the QubitSimulator.
    ///
    /// This class exists as a thin translation layer so that QubitSimulator does
    /// not need to know anything about Unity's button or slider system. Keeping
    /// UI concerns separate from simulation logic means the simulation can be tested
    /// or driven by other input methods (keyboard shortcuts, scripted sequences,
    /// network messages) without touching this class.
    ///
    /// The Press* methods are public so that buttons can be wired in two ways:
    ///   Option A: Assign the button references in the Inspector here. This script
    ///             will bind them automatically in Awake, keeping the Inspector tidy.
    ///   Option B: Leave the button references blank and wire each button's onClick
    ///             event directly to the Press* methods via the Inspector. This gives
    ///             more flexibility for UI layouts where buttons are scattered across
    ///             multiple canvases or prefabs.
    ///
    /// Scene setup:
    ///   1. Create a Canvas set to Screen Space Overlay.
    ///   2. Add a Panel under the Canvas, then add one Button per gate:
    ///        X, Y, Z, H, S, T, Rx, Ry, Rz, Measure, Reset.
    ///   3. Add a Slider for the rotation angle and a TextMeshProUGUI label
    ///      to display the current angle in human-readable form.
    ///   4. Assign SimulatorRef and any button or slider references in the Inspector.
    /// </summary>
    public class GatePanel : MonoBehaviour
    {
        [Header("Simulator")]
        [SerializeField] private QubitSimulator _simulator;

        [Header("Gate Buttons")]
        [SerializeField] private Button _btnX;
        [SerializeField] private Button _btnY;
        [SerializeField] private Button _btnZ;
        [SerializeField] private Button _btnH;
        [SerializeField] private Button _btnS;
        [SerializeField] private Button _btnT;
        [SerializeField] private Button _btnRx;
        [SerializeField] private Button _btnRy;
        [SerializeField] private Button _btnRz;

        [Header("Measurement and Reset")]
        [SerializeField] private Button _btnMeasure;
        [SerializeField] private Button _btnReset;

        [Header("Rotation Angle Control")]
        [Tooltip("Slider that controls the angle passed to Rx, Ry, and Rz. The normalized value (0 to 1) is mapped to the range AngleMin to AngleMax in radians.")]
        [SerializeField] private Slider _angleSlider;

        [Tooltip("Displays the current angle in both degrees and radians so the user can understand the relationship between the slider position and the Bloch sphere rotation.")]
        [SerializeField] private TextMeshProUGUI _angleLabel;

        [Tooltip("Minimum rotation angle in radians. Zero means no rotation.")]
        [SerializeField] private float _angleMin = 0f;

        [Tooltip("Maximum rotation angle in radians. Defaults to a full turn (2*pi).")]
        [SerializeField] private float _angleMax = Mathf.PI * 2f;

        // We read the slider's normalizedValue (always 0 to 1) and remap it to
        // the configured angle range. This keeps the slider's min and max at 0
        // and 1 so we do not have to worry about the slider's own min/max values
        // conflicting with the angle range we set here.
        private float CurrentAngle => _angleSlider != null
            ? Mathf.Lerp(_angleMin, _angleMax, _angleSlider.normalizedValue)
            : Mathf.PI;


        private void Awake()
        {
            if (_simulator == null)
                _simulator = FindObjectOfType<QubitSimulator>();

            BindButtons();

            if (_angleSlider != null)
            {
                _angleSlider.onValueChanged.AddListener(_ => RefreshAngleLabel());
                _angleSlider.minValue = 0f;
                _angleSlider.maxValue = 1f;

                // Start at the midpoint so rotation gates default to a half-turn (pi),
                // which is the most visually dramatic and instructive starting angle.
                _angleSlider.value = 0.5f;
                RefreshAngleLabel();
            }
        }

        private void BindButtons()
        {
            // A helper avoids repeating the null check at every binding site.
            // Buttons wired here in code and buttons wired in the Inspector both work;
            // Unity will simply call all registered listeners in order.
            Bind(_btnX,       PressX);
            Bind(_btnY,       PressY);
            Bind(_btnZ,       PressZ);
            Bind(_btnH,       PressH);
            Bind(_btnS,       PressS);
            Bind(_btnT,       PressT);
            Bind(_btnRx,      PressRx);
            Bind(_btnRy,      PressRy);
            Bind(_btnRz,      PressRz);
            Bind(_btnMeasure, PressMeasure);
            Bind(_btnReset,   PressReset);
        }

        private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null) btn.onClick.AddListener(action);
        }

        public void PressX()       => _simulator?.ApplyX();
        public void PressY()       => _simulator?.ApplyY();
        public void PressZ()       => _simulator?.ApplyZ();
        public void PressH()       => _simulator?.ApplyH();
        public void PressS()       => _simulator?.ApplyS();
        public void PressT()       => _simulator?.ApplyT();
        public void PressRx()      => _simulator?.ApplyRx(CurrentAngle);
        public void PressRy()      => _simulator?.ApplyRy(CurrentAngle);
        public void PressRz()      => _simulator?.ApplyRz(CurrentAngle);
        public void PressMeasure() => _simulator?.Measure();
        public void PressReset()   => _simulator?.Reset();

        private void RefreshAngleLabel()
        {
            if (_angleLabel != null)
            {
                float deg = CurrentAngle * Mathf.Rad2Deg;
                _angleLabel.text = $"theta = {deg:F1} deg  ({CurrentAngle:F3} rad)";
            }
        }
    }
}
