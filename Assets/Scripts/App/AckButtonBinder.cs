using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>提醒 Ack 按钮：调用 app.AckReminder(id)。在 Inspector 里设置 reminderId。</summary>
    [AddComponentMenu("DigitalHuman/UI/Ack Button Binder")]
    [RequireComponent(typeof(Button))]
    public class AckButtonBinder : MonoBehaviour
    {
        public Button button;
        public long reminderId;

        private DigitalHumanApp _app;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void Start() { _app = FindObjectOfType<DigitalHumanApp>(); }

        public void SetReminderId(long id) { reminderId = id; }

        private void OnClick() { _app?.AckReminder(reminderId); }
    }
}
