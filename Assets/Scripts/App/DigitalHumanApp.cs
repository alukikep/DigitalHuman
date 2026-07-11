using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Live2D;
using DigitalHuman.Net;
using DigitalHuman.Realtime;
using DigitalHuman.Services;
using UnityEngine;
using UnityEngine.Networking;

namespace DigitalHuman.App
{
    /// <summary>
    /// 顶层编排器。串起所有子系统，订阅事件并驱动 UI。
    ///
    /// 挂载：场景中空 GameObject（建议命名 BackendRoot/DigitalHumanApp）。
    /// Inspector 需要：
    ///   - config: BackendConfig 资产
    ///   - voiceChannel / reminderChannel: WebSocketChannel (AddComponent)
    ///   - realtimeSession: RealtimeVoiceSession
    ///   - microphoneCapture: MicrophoneCapture
    ///   - mp3Player + AudioSource: Mp3SegmentPlayer
    ///   - hiyoriDriver: HiyoriDriver
    ///   - chatService / reminderService / reminderChannel
    /// </summary>
    [AddComponentMenu("DigitalHuman/App")]
    public class DigitalHumanApp : MonoBehaviour
    {
        public static DigitalHumanApp Instance { get; private set; }

        [Header("Config")]
        public BackendConfig config;

        [Header("Net")]
        public ApiClient apiClient;
        public WebSocketChannel voiceChannel;
        public WebSocketChannel reminderSocket;

        [Header("Realtime")]
        public RealtimeVoiceSession realtimeSession;
        public MicrophoneCapture microphoneCapture;
        public Mp3SegmentPlayer mp3Player;

        [Header("Live2D")]
        public HiyoriDriver hiyoriDriver;

        [Header("Services")]
        public ChatService chatService;
        public ProfileService profileService;
        public MemoryService memoryService;
        public ScheduleService scheduleService;
        public ReminderService reminderService;
        public ReminderChannel reminderChannel;
        public CareService careService;
        public EmotionService emotionService;
        public TtsService ttsService;
        public FileAsrUploader fileAsrUploader;

        [Header("UI")]
        public StatusView statusView;
        public ChatHistoryView chatHistoryView;

        [Header("Reminder Toast")]
        public GameObject reminderToast;
        public TMPro.TextMeshProUGUI reminderToastText;

        // 当前会话 LLM 增量缓存
        private string _currentLlmReply = "";
        private string _currentEmotion = "";

        // ---------- Lifecycle ----------

        private void Awake()
        {
            Instance = this;
            Inject();
        }

        private void Start()
        {
            Subscribe();
            StartCoroutine(PollStatusLoop());
            StartCoroutine(Bootstrap());
        }

        private void OnDestroy()
        {
            try
            {
                if (realtimeSession != null) realtimeSession.CloseAsync();
                if (reminderChannel != null) reminderChannel.CloseAsync();
                if (microphoneCapture != null) microphoneCapture.StopRecording();
                if (mp3Player != null) mp3Player.Cancel();
            }
            catch { }
            Instance = null;
        }

        private void Inject()
        {
            if (config == null)
            {
                Debug.LogError("[DH] DigitalHumanApp: config is null. Create a BackendConfig asset and assign it.");
                enabled = false;
                return;
            }
            apiClient?.Init(config);

            if (voiceChannel == null) voiceChannel = gameObject.AddComponent<WebSocketChannel>();
            if (reminderSocket == null) reminderSocket = gameObject.AddComponent<WebSocketChannel>();
            if (microphoneCapture == null) microphoneCapture = gameObject.AddComponent<MicrophoneCapture>();
            microphoneCapture.Inject(config);

            if (realtimeSession == null) realtimeSession = gameObject.AddComponent<RealtimeVoiceSession>();
            realtimeSession.Inject(config, voiceChannel, microphoneCapture, mp3Player);

            chatService?.Inject(apiClient, config);
            profileService?.Inject(apiClient, config);
            memoryService?.Inject(apiClient, config);
            scheduleService?.Inject(apiClient, config);
            reminderService?.Inject(apiClient, config);
            careService?.Inject(apiClient, config);
            emotionService?.Inject(apiClient, config);
            ttsService?.Inject(apiClient, config);
            fileAsrUploader?.Inject(apiClient);
            reminderChannel?.Inject(config, reminderSocket);
        }

        private void Subscribe()
        {
            if (realtimeSession != null)
            {
                realtimeSession.OnSessionReady += (ready, state, device) => statusView?.SetSession($"Voice: {state} ({device})");
                realtimeSession.OnPartial += txt => { if (chatHistoryView != null) chatHistoryView.SetPartial(txt); };
                realtimeSession.OnFinal += txt => { if (chatHistoryView != null) chatHistoryView.SetFinalUser(txt); };
                realtimeSession.OnEmotion += ev => { _currentEmotion = ev?.emotion ?? "neutral"; hiyoriDriver?.ApplyEmotion(ev); statusView?.SetEmotion(_currentEmotion); };
                realtimeSession.OnLlmDelta += txt => { _currentLlmReply += txt; chatHistoryView?.AppendAssistantDelta(txt); };
                realtimeSession.OnLlmReply += (reply, runtime) => { _currentLlmReply = reply ?? _currentLlmReply; };
                realtimeSession.OnTurnDone += td => { chatHistoryView?.FinalizeAssistant(_currentLlmReply, td); _currentLlmReply = ""; };
                realtimeSession.OnError += (code, msg, stage, retryable) => statusView?.SetError($"{code}@{stage}: {msg}");
                if (mp3Player != null) mp3Player.OnQueueEmpty += () => hiyoriDriver?.ResetToIdle();
            }

            if (reminderChannel != null)
            {
                reminderChannel.OnReminder += push =>
                {
                    ShowReminderToast(push.title);
                };
                reminderChannel.OnError += err => statusView?.SetError($"reminder: {err}");
            }
        }

        private IEnumerator Bootstrap()
        {
            // 启动 reminder WS
            if (reminderChannel != null) yield return reminderChannel.ConnectAsync();

            // 拉 pending 提醒（离线补领）
            if (reminderService != null)
                yield return reminderService.GetPendingAsync(
                    r =>
                    {
                        if (r?.data?.items != null)
                        {
                            foreach (var it in r.data.items) ShowReminderToast(it.title);
                        }
                    },
                    err => statusView?.SetError($"pending: {err.Code}"));
        }

        private IEnumerator PollStatusLoop()
        {
            while (true)
            {
                yield return HealthPoll();
                yield return ReadyPoll();
                yield return new WaitForSeconds(Mathf.Max(1f, config.statusPollSeconds));
            }
        }

        private IEnumerator HealthPoll()
        {
            using (var req = UnityWebRequest.Get(config.httpBase + "/health"))
            {
                req.timeout = 3;
                yield return req.SendWebRequest();
                string s = req.result == UnityWebRequest.Result.Success ? "Health 200" : $"Health {req.responseCode}";
                statusView?.SetHealth(s);
            }
        }

        private IEnumerator ReadyPoll()
        {
            // /api/v1/ready 这里由于 httpBase 已含 /api/v1，使用 config.httpBase + "/ready"
            using (var req = UnityWebRequest.Get(config.httpBase + "/ready"))
            {
                req.timeout = 3;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
                {
                    var dataJson = JsonExtensions.ExtractDataJson(req.downloadHandler.text);
                    var ready = JsonUtility.FromJson<ReadyResponse>(dataJson);
                    string label = (ready?.asr != null)
                        ? $"ASR {(ready.asr.ready ? "ready" : "loading")} ({ready.asr.device})"
                        : "ASR ?";
                    statusView?.SetReady(label);
                }
                else
                {
                    statusView?.SetReady($"Ready {req.responseCode}");
                }
            }
        }

        // ---------------- Public API for UI Binders ----------------

        public void StartOrStopRecording()
        {
            if (realtimeSession == null) return;
            switch (realtimeSession.CurrentState)
            {
                case RealtimeVoiceSession.State.Idle:
                case RealtimeVoiceSession.State.Error:
                    _ = realtimeSession.OpenAsync();
                    break;
                case RealtimeVoiceSession.State.Ready:
                    _ = realtimeSession.StartRecordingAsync();
                    break;
                case RealtimeVoiceSession.State.Recording:
                    realtimeSession.StopRecording();
                    break;
                default:
                    statusView?.SetError($"busy: {realtimeSession.CurrentState}");
                    break;
            }
        }

        public void CancelTurn() => realtimeSession?.CancelTurn();

        public void SendTextChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || chatService == null) return;
            chatHistoryView?.SetFinalUser(text);
            _currentLlmReply = "";
            StartCoroutine(chatService.SendChatAsync(
                text, false,
                r =>
                {
                    var d = r?.data;
                    chatHistoryView?.FinalizeAssistant(d?.reply ?? "", new TurnDoneData
                    {
                        reply = d?.reply, emotion = d?.emotion, sub_emotion = d?.sub_emotion,
                        expression = d?.expression, action = d?.action, audio_url = d?.audio_url,
                        runtime = d?.runtime
                    });
                    hiyoriDriver?.ApplyEmotion(new EmotionEventData
                    {
                        emotion = d?.emotion, sub_emotion = d?.sub_emotion,
                        expression = d?.expression, action = d?.action
                    });
                    statusView?.SetEmotion(d?.emotion);
                },
                err => statusView?.SetError($"chat: {err.Code} {err.Message}")));
        }

        public void AckReminder(long id)
        {
            if (reminderService == null) return;
            StartCoroutine(reminderService.AckAsync(id,
                _ => HideReminderToast(),
                err => statusView?.SetError($"ack: {err.Code}")));
        }

        public void ShowReminderToast(string title)
        {
            if (reminderToast == null) return;
            if (reminderToastText != null) reminderToastText.text = title;
            reminderToast.SetActive(true);
        }

        public void HideReminderToast()
        {
            if (reminderToast != null) reminderToast.SetActive(false);
        }
    }
}
