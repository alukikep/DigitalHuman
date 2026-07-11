using System;
using UnityEngine;

namespace DigitalHuman.Core
{
    /// <summary>
    /// 后端连接参数。在 Project 中通过 Assets/Create/DigitalHuman/BackendConfig 创建唯一实例，
    /// 然后拖到 DigitalHumanApp.BackendConfig 字段。可在运行时被 SetActive 让生效。
    ///
    /// 典型配置：
    ///   HttpBase   : http://127.0.0.1:8000/api/v1
    ///   WsBase     : ws://127.0.0.1:8000/api/v1
    ///   UserId     : 1001
    ///   VoiceStyle : warm_female
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "DigitalHuman/BackendConfig", order = 0)]
    public class BackendConfig : ScriptableObject
    {
        [Header("HTTP")]
        [Tooltip("REST 基址，例如 http://127.0.0.1:8000/api/v1")]
        public string httpBase = "http://127.0.0.1:8000/api/v1";

        [Tooltip("WebSocket 基址，例如 ws://127.0.0.1:8000/api/v1")]
        public string wsBase = "ws://127.0.0.1:8000/api/v1";

        [Header("Identity")]
        [Tooltip("当前用户 ID。所有 REST/WS 路径都会附加此 ID。")]
        public string userId = "1001";

        [Header("Realtime Voice")]
        [Tooltip("Edge TTS 声音风格名，传递给 session.start.voice_style。")]
        public string voiceStyle = "warm_female";

        [Tooltip("实时语音会话是否默认合成 TTS。true = 流式播放 MP3，false = 只走文本。")]
        public bool enableTts = true;

        [Tooltip("麦克风采样率（设备采样率），用于 AudioClip 的读取。可保持 48000 适配多数设备。")]
        public int microphoneSampleRate = 48000;

        [Tooltip("发送到后端的采样率（必须 16000）。")]
        public int targetSampleRate = 16000;

        [Tooltip("每帧时长，毫秒。建议 100。")]
        public int chunkMs = 100;

        [Tooltip("单句最长录音秒数。超过则强制结束以避免超时。")]
        public int maxUtteranceSeconds = 60;

        [Tooltip("实时语音 WS 重连尝试次数（指数退避）。")]
        public int realtimeReconnectMax = 5;

        [Header("Polling")]
        [Tooltip("状态轮询间隔（秒）。")]
        public float statusPollSeconds = 3f;

        public int FrameBytes => (targetSampleRate * chunkMs / 1000) * 2; // mono int16

        /// <summary>把后端返回的相对 audio_url 转成绝对可访问的 URL。</summary>
        public string ResolveAudioUrl(string relativeOrAbsolute)
        {
            if (string.IsNullOrEmpty(relativeOrAbsolute)) return null;
            if (relativeOrAbsolute.StartsWith("http://") || relativeOrAbsolute.StartsWith("https://"))
                return relativeOrAbsolute;
            // httpBase 形如 http://host:port/api/v1；audio_url 形如 /static/audio/xxx.mp3
            // 取 httpBase 的 host 部分，即去掉 /api/v1 再拼接。
            string baseRoot = httpBase;
            int idx = baseRoot.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) baseRoot = baseRoot.Substring(0, idx);
            if (relativeOrAbsolute.StartsWith("/")) return baseRoot + relativeOrAbsolute;
            return baseRoot + "/" + relativeOrAbsolute;
        }

        public string RealtimeVoiceWsUrl() => $"{wsBase}/ws/voice/{userId}";
        public string ReminderWsUrl() => $"{wsBase}/ws/reminders/{userId}";
    }
}
