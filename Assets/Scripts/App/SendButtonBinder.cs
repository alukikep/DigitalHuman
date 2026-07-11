using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>发送按钮：读取 inputField.text 后调用 app.SendTextChat(text)。</summary>
    [AddComponentMenu("DigitalHuman/UI/Send Button Binder")]
    [RequireComponent(typeof(Button))]
    public class SendButtonBinder : MonoBehaviour
    {
        public Button button;
        public TMPro.TMP_InputField inputField;

        private DigitalHumanApp _app;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            _app = FindObjectOfType<DigitalHumanApp>();
            if (inputField == null) inputField = FindObjectOfType<TMPro.TMP_InputField>();
        }

        private void OnClick()
        {
            if (inputField == null || _app == null) return;
            string text = inputField.text;
            inputField.text = "";
            _app.SendTextChat(text);
        }
    }
}
