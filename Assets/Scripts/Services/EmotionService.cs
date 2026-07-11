using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>POST /emotion/analyze（增强情绪分析工具）</summary>
    [AddComponentMenu("DigitalHuman/Services/Emotion")]
    public class EmotionService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator AnalyzeAsync(string text, string context, Action<ApiResponse<EmotionAnalyzeResponse>> onOk, Action<BackendException> onErr)
        {
            var req = new EmotionAnalyzeRequest { text = text, context = context };
            return api.PostJsonAsync<EmotionAnalyzeRequest, EmotionAnalyzeResponse>("/emotion/analyze", req, onOk, onErr);
        }
    }
}
