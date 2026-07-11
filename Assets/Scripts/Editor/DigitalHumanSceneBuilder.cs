#if UNITY_EDITOR
using System;
using System.IO;
using DigitalHuman.App;
using DigitalHuman.Core;
using DigitalHuman.Live2D;
using DigitalHuman.Net;
using DigitalHuman.Realtime;
using DigitalHuman.Services;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DigitalHuman.EditorTools
{
    /// <summary>
    /// One-click scene setup menu. Open SampleScene, then click
    /// "DigitalHuman / Setup Scene" to wire everything up automatically.
    /// All UI labels are in English by default (TMP's default LiberationSans SDF has no CJK).
    /// </summary>
    public static class DigitalHumanSceneBuilder
    {
        private const string BackendRootName = "BackendRoot";
        private const string Mp3PlayerName = "Mp3Player";
        private const string HiyoriRootName = "HiyoriRoot";
        private const string CanvasName = "DigitalHumanCanvas";
        private const string EventSystemName = "EventSystem";
        private const string ConfigFolder = "Assets/DigitalHumanSettings";
        private const string ConfigAssetPath = ConfigFolder + "/DefaultBackendConfig.asset";
        private const string HiyoriPrefabPath = "Assets/hiyori_pro/runtime/hiyori_pro_t11.prefab";

        [MenuItem("DigitalHuman/Setup Scene")]
        public static void SetupScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                {
                    EditorUtility.DisplayDialog("DigitalHuman", "Please open a scene first (e.g. SampleScene).", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Creating config asset...", 0.05f);
                var cfg = EnsureBackendConfig();

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Building BackendRoot...", 0.15f);
                var backendRoot = GetOrCreateGameObject(BackendRootName);
                DestroyChildrenExcept(backendRoot);
                EnsureComponent<Transform>(backendRoot);

                var app = EnsureComponent<DigitalHumanApp>(backendRoot);
                var apiClient = EnsureComponent<ApiClient>(backendRoot);
                var voiceChannel = EnsureComponent<WebSocketChannel>(backendRoot);
                voiceChannel.gameObject.name = "VoiceWsChannel";
                var reminderSocket = EnsureComponent<WebSocketChannel>(backendRoot);
                reminderSocket.gameObject.name = "ReminderWsChannel";
                var realtime = EnsureComponent<RealtimeVoiceSession>(backendRoot);
                var mic = EnsureComponent<MicrophoneCapture>(backendRoot);
                mic.gameObject.name = "MicrophoneCapture";

                var chat = EnsureComponent<ChatService>(backendRoot);
                var profile = EnsureComponent<ProfileService>(backendRoot);
                var memory = EnsureComponent<MemoryService>(backendRoot);
                var schedule = EnsureComponent<ScheduleService>(backendRoot);
                var reminder = EnsureComponent<ReminderService>(backendRoot);
                var reminderCh = EnsureComponent<ReminderChannel>(backendRoot);
                var care = EnsureComponent<CareService>(backendRoot);
                var emotionSvc = EnsureComponent<EmotionService>(backendRoot);
                var tts = EnsureComponent<TtsService>(backendRoot);
                var fileAsr = EnsureComponent<FileAsrUploader>(backendRoot);

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Building Mp3Player...", 0.3f);
                var mp3Go = GetOrCreateGameObject(Mp3PlayerName);
                DestroyChildrenExcept(mp3Go);
                var audioSrc = EnsureComponent<AudioSource>(mp3Go);
                audioSrc.playOnAwake = false;
                audioSrc.loop = false;
                audioSrc.spatialBlend = 0f;
                var mp3Player = EnsureComponent<Mp3SegmentPlayer>(mp3Go);

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Building HiyoriRoot...", 0.45f);
                var hiyoriGo = InstantiateHiyoriPrefab(HiyoriPrefabPath, HiyoriRootName);
                var hiyoriDriver = EnsureComponent<HiyoriDriver>(hiyoriGo);

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Building UI Canvas...", 0.7f);
                var canvasGo = BuildUICanvas(CanvasName);

                EditorUtility.DisplayProgressBar("DigitalHuman Setup", "Wiring fields...", 0.9f);

                app.config = cfg;
                app.apiClient = apiClient;
                app.voiceChannel = voiceChannel;
                app.reminderSocket = reminderSocket;
                app.realtimeSession = realtime;
                app.microphoneCapture = mic;
                app.mp3Player = mp3Player;
                app.hiyoriDriver = hiyoriDriver;
                app.chatService = chat;
                app.profileService = profile;
                app.memoryService = memory;
                app.scheduleService = schedule;
                app.reminderService = reminder;
                app.reminderChannel = reminderCh;
                app.careService = care;
                app.emotionService = emotionSvc;
                app.ttsService = tts;
                app.fileAsrUploader = fileAsr;

                var statusView = canvasGo.transform.Find("StatusBar")?.GetComponent<StatusView>();
                var chatView = canvasGo.transform.Find("ChatPanel/ChatHistory")?.GetComponent<ChatHistoryView>();
                app.statusView = statusView;
                app.chatHistoryView = chatView;
                app.reminderToast = canvasGo.transform.Find("ReminderToast")?.gameObject;
                app.reminderToastText = canvasGo.transform.Find("ReminderToast/Title")?.GetComponent<TextMeshProUGUI>();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                EditorUtility.ClearProgressBar();
                Debug.Log("[DH] Scene setup complete. Open SampleScene, check the GameObject tree, then Play.");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[DH] Setup failed: " + e);
                EditorUtility.DisplayDialog("DigitalHuman Setup Failed", e.Message, "OK");
            }
        }

        private static GameObject GetOrCreateGameObject(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        private static void DestroyChildrenExcept(GameObject go)
        {
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                var child = go.transform.GetChild(i).gameObject;
                Undo.DestroyObjectImmediate(child);
            }
        }

        private static BackendConfig EnsureBackendConfig()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
                AssetDatabase.Refresh();
            }
            var existing = AssetDatabase.LoadAssetAtPath<BackendConfig>(ConfigAssetPath);
            if (existing != null) return existing;
            var cfg = ScriptableObject.CreateInstance<BackendConfig>();
            AssetDatabase.CreateAsset(cfg, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        private static GameObject InstantiateHiyoriPrefab(string assetPath, string newName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[DH] {assetPath} not found. HiyoriRoot created as empty GameObject (drag the prefab in later).");
                return GetOrCreateGameObject(newName);
            }
            var existing = GameObject.Find(newName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = newName;
            Undo.RegisterCreatedObjectUndo(inst, "Instantiate Hiyori");
            return inst;
        }

        private static GameObject BuildUICanvas(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            if (GameObject.Find(EventSystemName) == null)
            {
                var es = new GameObject(EventSystemName, typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var statusBar = CreateChildPanel(canvasGo.transform, "StatusBar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(520, 220));
            var statusViewComp = statusBar.gameObject.AddComponent<StatusView>();
            BindStatusText(statusViewComp, statusBar.transform);

            var chatPanel = CreateChildPanel(canvasGo.transform, "ChatPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 0), new Vector2(480, 700));
            var chatScroll = CreateChild(chatPanel.transform, "ChatScroll");
            SetRect(chatScroll, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            AddImage(chatScroll, new Color(0, 0, 0, 0.5f));
            var chatScrollRect = chatScroll.AddComponent<ScrollRect>();
            chatScrollRect.horizontal = false;
            chatScrollRect.vertical = true;
            var viewport = CreateChild(chatScroll.transform, "Viewport");
            SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(viewport, Color.white);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.AddComponent<RectMask2D>();
            var content = CreateChild(viewport.transform, "Content");
            SetRect(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, -1500));
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content.AddComponent<VerticalLayoutGroup>();
            chatScrollRect.viewport = viewport.GetComponent<RectTransform>();
            chatScrollRect.content = content.GetComponent<RectTransform>();

            var chatHistoryGo = CreateChild(content.transform, "ChatHistory");
            SetRect(chatHistoryGo, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, -1500));
            var chatViewComp = chatHistoryGo.gameObject.AddComponent<ChatHistoryView>();
            var chatTmp = chatHistoryGo.gameObject.AddComponent<TextMeshProUGUI>();
            chatTmp.text = "";
            chatTmp.fontSize = 22;
            chatTmp.color = Color.white;
            chatViewComp.labelTMP = chatTmp;
            chatHistoryGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1500);

            var inputBar = CreateChildPanel(canvasGo.transform, "InputBar", new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(1400, 60));
            var inputField = inputBar.gameObject.AddComponent<TMP_InputField>();
            var inputBg = CreateChild(inputBar.transform, "InputBg");
            SetRect(inputBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(inputBg, new Color(1, 1, 1, 0.85f));
            var textArea = CreateChild(inputBg.transform, "TextArea");
            SetRect(textArea, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var placeholder = CreateChild(textArea.transform, "Placeholder");
            SetRect(placeholder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var phTmp = placeholder.gameObject.AddComponent<TextMeshProUGUI>();
            phTmp.text = "Type a message...";
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            phTmp.fontSize = 22;
            var textGo = CreateChild(textArea.transform, "Text");
            SetRect(textGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var textTmp = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            textTmp.text = "";
            textTmp.fontSize = 22;
            textTmp.color = Color.black;
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.placeholder = phTmp;
            inputField.textComponent = textTmp;

            var sendGo = CreateChild(inputBar.transform, "SendButton");
            SetRect(sendGo, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-130, 5), new Vector2(-10, -5));
            AddImage(sendGo, new Color(0.2f, 0.5f, 0.9f, 1));
            var sendLb = CreateChild(sendGo.transform, "Label");
            SetRect(sendLb, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sendTmp = sendLb.gameObject.AddComponent<TextMeshProUGUI>();
            sendTmp.text = "Send";
            sendTmp.alignment = TextAlignmentOptions.Center;
            sendTmp.fontSize = 22;
            sendTmp.color = Color.white;
            var sendBtn = sendGo.AddComponent<Button>();
            var sendBinder = sendGo.AddComponent<SendButtonBinder>();
            sendBinder.button = sendBtn;
            sendBinder.inputField = inputField;

            var voiceGo = CreateChildPanel(canvasGo.transform, "VoiceButton", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 30), new Vector2(160, 70));
            AddImage(voiceGo, new Color(0.9f, 0.3f, 0.3f, 1));
            var voiceLb = CreateChild(voiceGo.transform, "Label");
            SetRect(voiceLb, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var voiceTmp = voiceLb.gameObject.AddComponent<TextMeshProUGUI>();
            voiceTmp.text = "Start Talking";
            voiceTmp.alignment = TextAlignmentOptions.Center;
            voiceTmp.fontSize = 24;
            voiceTmp.color = Color.white;
            var voiceBtn = voiceGo.AddComponent<Button>();
            var voiceBinder = voiceGo.AddComponent<VoiceButtonBinder>();
            voiceBinder.button = voiceBtn;
            voiceBinder.labelTMP = voiceTmp;

            var cancelGo = CreateChildPanel(canvasGo.transform, "CancelButton", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-360, 30), new Vector2(-200, 70));
            AddImage(cancelGo, new Color(0.6f, 0.6f, 0.6f, 1));
            var cancelLb = CreateChild(cancelGo.transform, "Label");
            SetRect(cancelLb, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cancelTmp = cancelLb.gameObject.AddComponent<TextMeshProUGUI>();
            cancelTmp.text = "Cancel";
            cancelTmp.alignment = TextAlignmentOptions.Center;
            cancelTmp.fontSize = 22;
            cancelTmp.color = Color.white;
            var cancelBtn = cancelGo.AddComponent<Button>();
            var cancelBinder = cancelGo.AddComponent<CancelButtonBinder>();
            cancelBinder.button = cancelBtn;

            var toastGo = CreateChildPanel(canvasGo.transform, "ReminderToast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-200, -100), new Vector2(200, -20));
            toastGo.gameObject.SetActive(false);
            AddImage(toastGo, new Color(0.2f, 0.8f, 0.4f, 1));
            var titleGo = CreateChild(toastGo.transform, "Title");
            SetRect(titleGo, new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(10, 0), new Vector2(-10, 0));
            var titleTmp = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Reminder";
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.fontSize = 24;
            titleTmp.color = Color.white;
            var ackGo = CreateChild(toastGo.transform, "AckButton");
            SetRect(ackGo, new Vector2(0.7f, 0), new Vector2(1, 1), new Vector2(0, 5), new Vector2(-10, -5));
            AddImage(ackGo, new Color(1, 1, 1, 0.4f));
            var ackLb = CreateChild(ackGo.transform, "Label");
            SetRect(ackLb, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var ackTmp = ackLb.gameObject.AddComponent<TextMeshProUGUI>();
            ackTmp.text = "Got It";
            ackTmp.alignment = TextAlignmentOptions.Center;
            ackTmp.fontSize = 22;
            ackTmp.color = Color.white;
            var ackBtn = ackGo.AddComponent<Button>();
            var ackBinder = ackGo.AddComponent<AckButtonBinder>();
            ackBinder.button = ackBtn;
            ackBinder.reminderId = 0;

            return canvasGo;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateChildPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = CreateChild(parent, name);
            SetRect(go, anchorMin, anchorMax, offsetMin, offsetMax);
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.localScale = Vector3.one;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void BindStatusText(StatusView sv, Transform parent)
        {
            string[] fields = { "healthText", "readyText", "voiceText", "emotionText", "errorText" };
            string[] tmps   = { "healthTMP",  "readyTMP",  "voiceTMP",  "emotionTMP",  "errorTMP" };
            string[] labels = { "Health: ?",  "Ready: ?",  "Voice: ?",  "Emotion: ?",  "Error: " };
            for (int i = 0; i < fields.Length; i++)
            {
                var row = CreateChild(parent, fields[i]);
                SetRect(row, new Vector2(0, 1 - (i + 1) * 0.18f), new Vector2(1, 1 - i * 0.18f), new Vector2(0, 0), Vector2.zero);
                var tmp = row.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = labels[i];
                tmp.fontSize = 22;
                tmp.color = Color.white;
                var f = typeof(StatusView).GetField(tmps[i]);
                if (f != null) f.SetValue(sv, tmp);
            }
        }
    }
}
#endif
