using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalHuman.Core
{
    // ---------------- 统一响应 ----------------
    [Serializable]
    public class ApiResponse<T>
    {
        public int code;
        public string message;
        public T data;
    }

    // ---------------- Health / Ready ----------------
    [Serializable]
    public class ReadyComponents
    {
        public bool ready;
        public string state;
        public string device;
        public int ffmpeg_available_int; // 由 JsonUtility 兜底手写映射
        public string detail;
    }

    [Serializable]
    public class ReadyResponse
    {
        public bool ready;
        public ReadyComponents asr;
    }

    // ---------------- Profile ----------------
    [Serializable]
    public class UserProfile
    {
        public string user_id;
        public string nickname;
        public string digital_human;   // e.g. "hiyori_pro"
        public string voice_style;
        public string companion_goal;  // 自由文本
        public string updated_at;
    }

    // ---------------- Chat ----------------
    [Serializable]
    public class ChatRequest
    {
        public string user_id;
        public string message;
        public bool enable_tts;
    }

    [Serializable]
    public class RuntimeInfo
    {
        public long llm_total_ms;
        public long tts_total_ms;
        public long total_ms;
        public string cache_provider;
    }

    [Serializable]
    public class ChatMessage
    {
        public string role;          // "user" / "assistant"
        public string content;       // 文本
        public string emotion;
        public string sub_emotion;
        public string expression;
        public string action;
        public string audio_url;
        public string created_at;
    }

    [Serializable]
    public class ChatResponse
    {
        public string reply;
        public string emotion;
        public string sub_emotion;
        public string expression;
        public string action;
        public string audio_url;
        public RuntimeInfo runtime;
    }

    [Serializable]
    public class ChatHistoryResponse
    {
        public List<ChatMessage> items;
    }

    // ---------------- Memories ----------------
    [Serializable]
    public class MemoryItem
    {
        public long id;
        public string user_id;
        public string content;
        public string importance;   // low / normal / high
        public string created_at;
    }

    [Serializable]
    public class MemoryListResponse { public List<MemoryItem> items; }

    [Serializable]
    public class MemoryCreateRequest
    {
        public string user_id;
        public string content;
        public string importance;
    }

    [Serializable]
    public class MemoryCreateResponse { public MemoryItem item; }

    // ---------------- Schedules ----------------
    [Serializable]
    public class ScheduleItem
    {
        public long id;
        public string user_id;
        public string title;
        public string start_at;     // ISO8601
        public string end_at;
        public string location;
        public string note;
    }

    [Serializable]
    public class ScheduleListResponse { public List<ScheduleItem> items; }

    [Serializable]
    public class ScheduleUpsertRequest
    {
        public string user_id;
        public string title;
        public string start_at;
        public string end_at;
        public string location;
        public string note;
    }

    [Serializable]
    public class ScheduleUpsertResponse { public ScheduleItem item; }

    // ---------------- Reminders ----------------
    [Serializable]
    public class ReminderItem
    {
        public long id;
        public string user_id;
        public string title;
        public string fire_at;
        public string created_at;
    }

    [Serializable]
    public class ReminderListResponse { public List<ReminderItem> items; }

    [Serializable]
    public class ReminderAckRequest { public string user_id; }
    [Serializable]
    public class ReminderAckResponse { public bool ok; public long id; }

    // ---------------- Reminder WebSocket push ----------------
    [Serializable]
    public class ReminderPushData
    {
        public long id;
        public string user_id;
        public string title;
        public string fire_at;
    }

    // ---------------- Care ----------------
    [Serializable]
    public class CareItem
    {
        public long id;
        public string content;
        public string trigger;
    }

    [Serializable]
    public class CareNextResponse
    {
        public List<CareItem> items;
    }

    // ---------------- Emotion ----------------
    [Serializable]
    public class EmotionAnalyzeRequest
    {
        public string text;
        public string context;
    }

    [Serializable]
    public class EmotionAnalyzeResponse
    {
        public string emotion;
        public string sub_emotion;
        public double score;
        public string expression;
        public string action;
    }

    // ---------------- TTS ----------------
    [Serializable]
    public class TtsRequest
    {
        public string text;
        public string voice_style;
        public string format;        // 默认 mp3
    }

    [Serializable]
    public class TtsResponse
    {
        public string audio_url;
        public string provider;
    }

    // ---------------- Realtime WS event payloads ----------------
    [Serializable]
    public class WsEnvelope
    {
        public string type;
        public long seq;
        public string session_id;
        public string utterance_id;
        public long elapsed_ms;
        public string data_json;     // 后端未拆分 data 子对象前由手工提取
    }

    [Serializable]
    public class SessionReadyData
    {
        public bool asr_ready;
        public string asr_state;
        public string device;
    }

    [Serializable]
    public class AsrTextData
    {
        public string text;
        public bool is_final;
    }

    [Serializable]
    public class EmotionEventData
    {
        public string emotion;
        public string sub_emotion;
        public string expression;
        public string action;
        public double score;
    }

    [Serializable]
    public class LlmDeltaData
    {
        public string text;
    }

    [Serializable]
    public class LlmDoneData
    {
        public string reply;
        public RuntimeInfo runtime;
    }

    [Serializable]
    public class TtsSegmentStartData
    {
        public string segment_id;
        public string text;
        public string content_type;
    }

    [Serializable]
    public class TtsSegmentEndData
    {
        public string segment_id;
    }

    [Serializable]
    public class TurnDoneData
    {
        public string reply;
        public string emotion;
        public string sub_emotion;
        public string expression;
        public string action;
        public string audio_url;
        public RuntimeInfo runtime;
    }

    [Serializable]
    public class ErrorEventData
    {
        public string code;
        public string message;
        public string stage;
        public bool retryable;
    }
}
