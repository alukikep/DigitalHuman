using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>
    /// Wires a Button.onClick to DigitalHumanApp.StartOrStopRecording().
    /// The label text toggles with the realtime session state.
    /// </summary>
    [AddComponentMenu("DigitalHuman/UI/Voice Button Binder")]
    [RequireComponent(typeof(Button))]
    public class VoiceButtonBinder : MonoBehaviour
    {
        public Button button;
        public Text labelText;
        public TMPro.TextMeshProUGUI labelTMP;

        private DigitalHumanApp _app;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            _app = FindObjectOfType<DigitalHumanApp>();
            if (_app == null) Debug.LogError("[DH] VoiceButtonBinder: DigitalHumanApp not found in scene.");
            UpdateLabel();
        }

        private void OnClick()
        {
            if (_app == null) return;
            _app.StartOrStopRecording();
        }

        private void Update() { UpdateLabel(); }

        private void UpdateLabel()
        {
            if (_app == null || _app.realtimeSession == null) return;
            string s = _app.realtimeSession.CurrentState switch
            {
                Realtime.RealtimeVoiceSession.State.Idle       => "Connecting...",
                Realtime.RealtimeVoiceSession.State.Connecting => "Connecting...",
                Realtime.RealtimeVoiceSession.State.Ready      => "Start Talking",
                Realtime.RealtimeVoiceSession.State.Recording => "Stop",
                Realtime.RealtimeVoiceSession.State.Finalizing => "Processing...",
                Realtime.RealtimeVoiceSession.State.Responding => "Generating reply...",
                Realtime.RealtimeVoiceSession.State.Playing    => "Playing...",
                Realtime.RealtimeVoiceSession.State.Cancelling => "Cancelled",
                Realtime.RealtimeVoiceSession.State.Error      => "Error (retry)",
                _ => "..."
            };
            if (labelText != null) labelText.text = s;
            if (labelTMP != null) labelTMP.text = s;
        }
    }
}
