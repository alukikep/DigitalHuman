using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>CRUD /schedules</summary>
    [AddComponentMenu("DigitalHuman/Services/Schedule")]
    public class ScheduleService : MonoBehaviour
    {
        public ApiClient api;
        public BackendConfig config;
        public void Inject(ApiClient c, BackendConfig cfg) { api = c; config = cfg; }

        public IEnumerator ListAsync(Action<ApiResponse<ScheduleListResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/schedules?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.GetJsonAsync<ScheduleListResponse>(path, onOk, onErr);
        }

        public IEnumerator CreateAsync(ScheduleUpsertRequest req, Action<ApiResponse<ScheduleUpsertResponse>> onOk, Action<BackendException> onErr)
        {
            req.user_id = config.userId;
            return api.PostJsonAsync<ScheduleUpsertRequest, ScheduleUpsertResponse>("/schedules", req, onOk, onErr);
        }

        public IEnumerator UpdateAsync(long id, ScheduleUpsertRequest req, Action<ApiResponse<ScheduleUpsertResponse>> onOk, Action<BackendException> onErr)
        {
            req.user_id = config.userId;
            string path = $"/schedules/{id}";
            return api.PutJsonAsync<ScheduleUpsertRequest, ScheduleUpsertResponse>(path, req, onOk, onErr);
        }

        public IEnumerator DeleteAsync(long id, Action<ApiResponse<ScheduleUpsertResponse>> onOk, Action<BackendException> onErr)
        {
            string path = $"/schedules/{id}?user_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(config.userId)}";
            return api.DeleteAsync<ScheduleUpsertResponse>(path, onOk, onErr);
        }
    }
}
