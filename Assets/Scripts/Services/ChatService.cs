using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>POST /chat, GET /chat/history。挂任意 GameObject；Inject 后使用。</summary>
    [AddComponentMenu("DigitalHuman/Services/Chat")]
    public class ChatService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;

        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator SendChatAsync(string message, bool enableTts, Action<ApiResponse<ChatResponse>> onOk, Action<BackendException> onErr)
        {
            var req = new ChatRequest { user_id = config.userId, message = message, enable_tts = enableTts };
            return api.PostJsonAsync<ChatRequest, ChatResponse>("/chat", req, onOk, onErr);
        }

        public IEnumerator GetHistoryAsync(int limit, Action<ApiResponse<ChatHistoryResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/chat/history?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}&limit={limit}";
            return api.GetJsonAsync<ChatHistoryResponse>(path, onOk, onErr);
        }
    }
}
