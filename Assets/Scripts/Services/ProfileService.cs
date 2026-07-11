using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>POST /profile, GET /profile/{user_id}</summary>
    [AddComponentMenu("DigitalHuman/Services/Profile")]
    public class ProfileService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator UpsertAsync(UserProfile profile, Action<ApiResponse<UserProfile>> onOk, Action<BackendException> onErr)
            => api.PostJsonAsync<UserProfile, UserProfile>("/profile", profile, onOk, onErr);

        public IEnumerator GetAsync(Action<ApiResponse<UserProfile>> onOk, Action<BackendException> onErr)
        {
            string path = $"/profile/{UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.GetJsonAsync<UserProfile>(path, onOk, onErr);
        }
    }
}
