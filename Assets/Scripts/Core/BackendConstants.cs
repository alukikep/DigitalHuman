namespace DigitalHuman.Core
{
    /// <summary>统一事件类型、错误码与默认值常量。</summary>
    public static class BackendConstants
    {
        // ---------------- WebSocket event types ----------------
        public const string EvtSessionReady = "session.ready";
        public const string EvtAsrPartial = "asr.partial";
        public const string EvtAsrFinal = "asr.final";
        public const string EvtEmotionResult = "emotion.result";
        public const string EvtLlmDelta = "llm.delta";
        public const string EvtLlmDone = "llm.done";
        public const string EvtTtsSegmentStart = "tts.segment.start";
        public const string EvtTtsSegmentEnd = "tts.segment.end";
        public const string EvtTurnDone = "turn.done";
        public const string EvtError = "error";

        // ---------------- Client command types ----------------
        public const string CmdSessionStart = "session.start";
        public const string CmdUtteranceEnd = "utterance.end";
        public const string CmdResponseCancel = "response.cancel";
        public const string CmdSessionClose = "session.close";

        // ---------------- Error codes ----------------
        public const string ErrAsrBusy = "asr_busy";
        public const string ErrAsrNotReady = "asr_not_ready";
        public const string ErrSessionNotStarted = "session_not_started";
        public const string ErrInvalidAudioConfig = "invalid_audio_config";
        public const string ErrAudioFrameTooLarge = "audio_frame_too_large";
        public const string ErrEmptyTranscript = "empty_transcript";
        public const string ErrTtsFailed = "tts_failed";
        public const string ErrTurnFailed = "turn_failed";
        public const string ErrResponseCancelled = "response_cancelled";
    }
}
