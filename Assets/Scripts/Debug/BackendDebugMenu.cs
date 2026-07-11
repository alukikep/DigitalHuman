#if UNITY_EDITOR
using DigitalHuman.App;
using UnityEditor;
using UnityEngine;

namespace DigitalHuman.DebugTools
{
    /// <summary>Editor menu: connection test, send test message, open the README.</summary>
    public static class BackendDebugMenu
    {
        [MenuItem("DigitalHuman/Debug/Open Doc")]
        public static void OpenDoc()
        {
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "Scripts/README_Unity对接.md"));
            if (System.IO.File.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("DigitalHuman", "README.md not found: " + path, "OK");
            }
        }

        [MenuItem("DigitalHuman/Debug/Log Current Status")]
        public static void LogStatus()
        {
            var app = DigitalHumanApp.Instance;
            if (app == null) { Debug.Log("[DH] DigitalHumanApp.Instance == null. Enter Play Mode first."); return; }
            Debug.Log($"[DH] State={app.realtimeSession?.CurrentState}, cfg=http={app.config.httpBase} ws={app.config.wsBase} uid={app.config.userId}");
        }

        [MenuItem("DigitalHuman/Debug/Send Test Chat")]
        public static void SendTestChat()
        {
            var app = DigitalHumanApp.Instance;
            if (app == null) { Debug.LogWarning("[DH] No app"); return; }
            app.SendTextChat("Hello, please give me a brief self-introduction.");
        }
    }
}
#endif
