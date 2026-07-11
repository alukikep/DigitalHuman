using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>GET /care/next</summary>
    [AddComponentMenu("DigitalHuman/Services/Care")]
    public class CareService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator NextAsync(Action<ApiResponse<CareNextResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/care/next?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.GetJsonAsync<CareNextResponse>(path, onOk, onErr);
        }
    }
}
