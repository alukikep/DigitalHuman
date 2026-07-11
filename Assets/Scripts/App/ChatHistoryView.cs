using System.Text;
using DigitalHuman.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>
    /// Minimal chat history view. Each turn appends a line; supports both
    /// UGUI Text and TextMeshProUGUI for the displayed label.
    /// </summary>
    [AddComponentMenu("DigitalHuman/UI/Chat History View")]
    public class ChatHistoryView : MonoBehaviour
    {
        public Text labelUGUI;
        public TMPro.TextMeshProUGUI labelTMP;

        private readonly StringBuilder _sb = new StringBuilder();
        private string _currentAssistantLine = "";

        public void AppendLine(string line)
        {
            _sb.AppendLine(line);
            Render();
        }

        public void SetPartial(string text)
        {
            string snapshot = _sb + $"\n[Me-draft] {text}";
            if (labelUGUI != null) labelUGUI.text = snapshot;
            if (labelTMP != null) labelTMP.text = snapshot;
        }

        public void SetFinalUser(string text)
        {
            int idx = _sb.ToString().LastIndexOf("\n[Me-draft] ");
            if (idx >= 0)
            {
                _sb.Length = idx + 1;
            }
            _sb.AppendLine($"[Me] {text}");
            _currentAssistantLine = "";
            Render();
        }

        public void AppendAssistantDelta(string delta)
        {
            _currentAssistantLine += delta;
            Render();
        }

        public void FinalizeAssistant(string finalText, TurnDoneData td)
        {
            if (!string.IsNullOrEmpty(finalText)) _currentAssistantLine = finalText;
            if (!string.IsNullOrEmpty(_currentAssistantLine))
            {
                string emo = td?.emotion ?? "neutral";
                string exp = td?.expression ?? "neutral";
                string act = td?.action ?? "idle";
                _sb.AppendLine($"[Bot-{emo}] {_currentAssistantLine}  (expression={exp}, action={act})");
            }
            _currentAssistantLine = "";
            Render();
        }

        public void Clear() { _sb.Clear(); _currentAssistantLine = ""; Render(); }

        private void Render()
        {
            if (labelUGUI != null) labelUGUI.text = _sb.ToString();
            if (labelTMP != null) labelTMP.text = _sb.ToString();
        }
    }
}
