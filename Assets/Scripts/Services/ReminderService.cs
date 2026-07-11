using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>REST 提醒：GET /reminders/pending, POST /reminders/{id}/ack</summary>
    [AddComponentMenu("DigitalHuman/Services/Reminder")]
    public class ReminderService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator GetPendingAsync(Action<ApiResponse<ReminderListResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/reminders/pending?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.GetJsonAsync<ReminderListResponse>(path, onOk, onErr);
        }

        public IEnumerator AckAsync(long id, Action<ApiResponse<ReminderAckResponse>> onOk, Action<BackendException> onErr)
        {
            var req = new ReminderAckRequest { user_id = config.userId };
            string path = $"/reminders/{id}/ack";
            return api.PostJsonAsync<ReminderAckRequest, ReminderAckResponse>(path, req, onOk, onErr);
        }
    }
}
