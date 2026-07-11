using UnityEngine;
using UnityEngine.UI;

namespace DigitalHuman.App
{
    /// <summary>取消按钮：调用 DigitalHumanApp.CancelTurn()。</summary>
    [AddComponentMenu("DigitalHuman/UI/Cancel Button Binder")]
    [RequireComponent(typeof(Button))]
    public class CancelButtonBinder : MonoBehaviour
    {
        public Button button;
        private DigitalHumanApp _app;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void Start() { _app = FindObjectOfType<DigitalHumanApp>(); }

        private void OnClick() { _app?.CancelTurn(); }
    }
}
