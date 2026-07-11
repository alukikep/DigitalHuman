using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Realtime
{
    [AddComponentMenu("DigitalHuman/Realtime Voice Session")]
    public class RealtimeVoiceSession : MonoBehaviour
    {
        public enum State { Idle, Connecting, Ready, Recording, Finalizing, Responding, Playing, Cancelling, Error }

        public BackendConfig backendConfig;
        public WebSocketChannel channel;
        public MicrophoneCapture mic;
        public Mp3SegmentPlayer mp3Player;

        public event System.Action<bool, string, string> OnSessionReady;
        public event System.Action<string> OnPartial;
        public event System.Action<string> OnFinal;
        public event System.Action<EmotionEventData> OnEmotion;
        public event System.Action<string> OnLlmDelta;
        public event System.Action<string, RuntimeInfo> OnLlmReply;
        public event System.Action<string> OnTtsSegmentStart;
        public event System.Action<byte[]> OnMp3Bytes;
        public event System.Action<string> OnTtsSegmentEnd;
        public event System.Action<TurnDoneData> OnTurnDone;
        public event System.Action<string, string, string, bool> OnError;

        public State CurrentState { get; private set; } = State.Idle;
        private long _lastSeq;
        private float _recordingStartRealtime;
        private bool _awaitingCancelAck;

        public void Inject(BackendConfig cfg, WebSocketChannel ch, MicrophoneCapture m, Mp3SegmentPlayer p)
        { backendConfig = cfg; channel = ch; mic = m; mp3Player = p; }

        public async Task OpenAsync()
        {
            if (CurrentState != State.Idle && CurrentState != State.Error) return;
            if (backendConfig == null || channel == null) { SetError("not initialized", "init"); return; }
            try
            {
                SetState(State.Connecting);
                channel.OnText -= HandleText;
                channel.OnBinary -= HandleBinary;
                channel.OnError -= HandleWsError;
                channel.OnText += HandleText;
                channel.OnBinary += HandleBinary;
                channel.OnError += HandleWsError;
                await channel.ConnectAsync(backendConfig.RealtimeVoiceWsUrl());
                SetState(State.Ready);
            }
            catch (Exception e) { SetError(e.Message, "connect"); }
        }

        public async Task CloseAsync()
        {
            try
            {
                if (channel != null)
                {
                    channel.SendText($"{{\"type\":\"{BackendConstants.CmdSessionClose}\"}}");
                    await channel.CloseAsync();
                }
            }
            catch { }
            finally { SetState(State.Idle); }
        }

        public async Task StartRecordingAsync()
        {
            if (CurrentState != State.Ready)
            {
                if (CurrentState == State.Idle || CurrentState == State.Error) await OpenAsync();
                else { SetError("session not ready: " + CurrentState, "start"); return; }
            }
            if (mp3Player != null) mp3Player.Cancel();

            string startJson = "{" +
                "\"type\":\"" + BackendConstants.CmdSessionStart + "\"," +
                "\"audio\":{\"encoding\":\"pcm_s16le\",\"sample_rate\":" + backendConfig.targetSampleRate + ",\"channels\":1}," +
                "\"voice_style\":\"" + backendConfig.voiceStyle + "\"," +
                "\"enable_tts\":" + (backendConfig.enableTts ? "true" : "false") +
                "}";
            channel.SendText(startJson);
            if (mic != null) mic.OnFrame -= OnMicFrame;
            if (mic != null) mic.OnFrame += OnMicFrame;
            if (mic != null) mic.StartRecording();
            _recordingStartRealtime = Time.realtimeSinceStartup;
            SetState(State.Recording);
        }

        public void StopRecording()
        {
            if (CurrentState != State.Recording) return;
            if (mic != null) mic.OnFrame -= OnMicFrame;
            if (mic != null) mic.StopRecording();
            channel?.SendText($"{{\"type\":\"{BackendConstants.CmdUtteranceEnd}\"}}");
            SetState(State.Finalizing);
        }

        public void CancelTurn()
        {
            if (CurrentState == State.Idle) return;
            _awaitingCancelAck = true;
            SetState(State.Cancelling);
            mp3Player?.Cancel();
            if (mic != null && mic.IsRunning) mic.StopRecording();
            channel?.SendText($"{{\"type\":\"{BackendConstants.CmdResponseCancel}\"}}");
        }

        private void OnMicFrame(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            if (bytes.Length > 64 * 1024)
            {
                SetError(BackendConstants.ErrAudioFrameTooLarge, "send", BackendConstants.ErrAudioFrameTooLarge, false);
                return;
            }
            channel?.SendBinary(bytes);
            if (Time.realtimeSinceStartup - _recordingStartRealtime > backendConfig.maxUtteranceSeconds)
            {
                StopRecording();
            }
        }

        private void HandleWsError(string err)
        {
            if (string.IsNullOrEmpty(err)) err = "ws error";
            if (_awaitingCancelAck) return;
            SetError(err, "transport");
        }

        private void HandleText(string text)
        {
            try
            {
                var env = JsonUtility.FromJson<WsEnvelope>(text);
                if (env == null || string.IsNullOrEmpty(env.type)) return;
                if (env.seq > 0)
                {
                    if (env.seq <= _lastSeq) Logger.Warn("voice", $"out-of-order seq={env.seq} last={_lastSeq}");
                    else _lastSeq = env.seq;
                }
                string dataJson = JsonExtensions.ExtractDataJson(text);
                switch (env.type)
                {
                    case BackendConstants.EvtSessionReady:
                        var sr = SafeFromJson<SessionReadyData>(dataJson);
                        OnSessionReady?.Invoke(sr?.asr_ready ?? true, sr?.asr_state ?? "ready", sr?.device ?? "");
                        break;
                    case BackendConstants.EvtAsrPartial:
                        var ap = SafeFromJson<AsrTextData>(dataJson);
                        OnPartial?.Invoke(ap?.text ?? "");
                        break;
                    case BackendConstants.EvtAsrFinal:
                        var af = SafeFromJson<AsrTextData>(dataJson);
                        OnFinal?.Invoke(af?.text ?? "");
                        SetState(State.Responding);
                        break;
                    case BackendConstants.EvtEmotionResult:
                        var em = SafeFromJson<EmotionEventData>(dataJson);
                        if (em != null) OnEmotion?.Invoke(em);
                        break;
                    case BackendConstants.EvtLlmDelta:
                        var ld = SafeFromJson<LlmDeltaData>(dataJson);
                        if (ld != null && !string.IsNullOrEmpty(ld.text)) OnLlmDelta?.Invoke(ld.text);
                        break;
                    case BackendConstants.EvtLlmDone:
                        var ll = SafeFromJson<LlmDoneData>(dataJson);
                        if (ll != null) OnLlmReply?.Invoke(ll.reply ?? "", ll.runtime);
                        break;
                    case BackendConstants.EvtTtsSegmentStart:
                        var ts = SafeFromJson<TtsSegmentStartData>(dataJson);
                        var segId = ts?.segment_id ?? Guid.NewGuid().ToString("N");
                        mp3Player?.BeginSegment(segId);
                        OnTtsSegmentStart?.Invoke(segId);
                        SetState(State.Playing);
                        break;
                    case BackendConstants.EvtTtsSegmentEnd:
                        var te = SafeFromJson<TtsSegmentEndData>(dataJson);
                        var sid = te?.segment_id ?? "";
                        mp3Player?.EndSegment(sid);
                        OnTtsSegmentEnd?.Invoke(sid);
                        break;
                    case BackendConstants.EvtTurnDone:
                        var td = SafeFromJson<TurnDoneData>(dataJson);
                        OnTurnDone?.Invoke(td);
                        break;
                    case BackendConstants.EvtError:
                        var ee = SafeFromJson<ErrorEventData>(dataJson);
                        if (ee != null && ee.code == BackendConstants.ErrResponseCancelled && _awaitingCancelAck)
                        {
                            _awaitingCancelAck = false;
                            SetState(State.Ready);
                            return;
                        }
                        OnError?.Invoke(
                            ee?.code ?? "unknown",
                            ee?.message ?? "",
                            ee?.stage ?? "",
                            ee?.retryable == true);
                        if (ee != null && (ee.code == BackendConstants.ErrAsrBusy || ee.code == BackendConstants.ErrAsrNotReady))
                            SetState(State.Error);
                        break;
                    default:
                        Logger.Warn("voice", $"unknown event {env.type}");
                        break;
                }
            }
            catch (System.Exception e) { Logger.Error("voice", $"handle text: {e.Message}\n{text}"); }
        }

        private void HandleBinary(byte[] data)
        {
            try { mp3Player?.TakeMp3Bytes(data); OnMp3Bytes?.Invoke(data); }
            catch (System.Exception e) { Logger.Error("voice", $"handle binary: {e.Message}"); }
        }

        private static T SafeFromJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (System.Exception e) { Logger.Error("voice", $"parse {typeof(T).Name}: {e.Message}\n{json}"); return null; }
        }

        private void SetState(State s) { CurrentState = s; Logger.Info("voice", $"state={s}"); }

        private void SetError(string code, string stage, string message = "", bool retryable = false)
        {
            try { OnError?.Invoke(code, message, stage, retryable); }
            catch (System.Exception e) { Logger.Error("voice", e.Message); }
            SetState(State.Error);
        }

        private void OnDestroy()
        {
            try
            {
                if (mic != null) mic.StopRecording();
                if (channel != null)
                {
                    channel.OnText -= HandleText;
                    channel.OnBinary -= HandleBinary;
                    channel.OnError -= HandleWsError;
                }
            }
            catch { }
        }
    }
}
