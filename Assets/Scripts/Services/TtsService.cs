using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>POST /tts/synthesize</summary>
    [AddComponentMenu("DigitalHuman/Services/Tts")]
    public class TtsService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator SynthesizeAsync(string text, string voiceStyle, Action<ApiResponse<TtsResponse>> onOk, Action<BackendException> onErr)
        {
            var req = new TtsRequest { text = text, voice_style = voiceStyle ?? config.voiceStyle, format = "mp3" };
            return api.PostJsonAsync<TtsRequest, TtsResponse>("/tts/synthesize", req, onOk, onErr);
        }
    }
}
