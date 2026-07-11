using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>GET/POST /memories, DELETE /memories/{id}</summary>
    [AddComponentMenu("DigitalHuman/Services/Memory")]
    public class MemoryService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator ListAsync(Action<ApiResponse<MemoryListResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/memories?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.GetJsonAsync<MemoryListResponse>(path, onOk, onErr);
        }

        public IEnumerator CreateAsync(string content, string importance, Action<ApiResponse<MemoryCreateResponse>> onOk, Action<BackendException> onErr)
        {
            var req = new MemoryCreateRequest { user_id = config.userId, content = content, importance = importance ?? "normal" };
            return api.PostJsonAsync<MemoryCreateRequest, MemoryCreateResponse>("/memories", req, onOk, onErr);
        }

        public IEnumerator DeleteAsync(long id, Action<ApiResponse<MemoryCreateResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/memories/{id}?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.DeleteAsync<MemoryCreateResponse>(path, onOk, onErr);
        }
    }
}
