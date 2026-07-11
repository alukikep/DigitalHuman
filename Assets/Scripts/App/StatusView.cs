using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>把 BackendStatus / 错误码 / 情绪显示到一个或两个 Text/TMP 上。</summary>
    [AddComponentMenu("DigitalHuman/UI/Status View")]
    public class StatusView : MonoBehaviour
    {
        public Text healthText;
        public Text readyText;
        public Text voiceText;
        public Text emotionText;
        public Text errorText;
        public TMPro.TextMeshProUGUI healthTMP;
        public TMPro.TextMeshProUGUI readyTMP;
        public TMPro.TextMeshProUGUI voiceTMP;
        public TMPro.TextMeshProUGUI emotionTMP;
        public TMPro.TextMeshProUGUI errorTMP;

        private string _lastError;

        public void SetHealth(string s)
        {
            if (healthText != null) healthText.text = s;
            if (healthTMP != null) healthTMP.text = s;
        }
        public void SetReady(string s)
        {
            if (readyText != null) readyText.text = s;
            if (readyTMP != null) readyTMP.text = s;
        }
        public void SetSession(string s)
        {
            if (voiceText != null) voiceText.text = s;
            if (voiceTMP != null) voiceTMP.text = s;
        }
        public void SetEmotion(string s)
        {
            if (emotionText != null) emotionText.text = "Emotion: " + s;
            if (emotionTMP != null) emotionTMP.text = "Emotion: " + s;
        }
        public void SetError(string s)
        {
            if (_lastError == s) return;
            _lastError = s;
            if (errorText != null) errorText.text = s;
            if (errorTMP != null) errorTMP.text = s;
            if (!string.IsNullOrEmpty(s)) Debug.LogWarning("[DH] " + s);
        }
    }
}
