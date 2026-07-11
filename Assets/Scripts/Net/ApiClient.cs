using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DigitalHuman.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace DigitalHuman.Net
{
    /// <summary>
    /// 后端 REST 客户端封装。所有响应都是 {code, message, data}。
    /// 失败条件：HTTP 非 2xx 或 code != 0，抛 BackendException。
    ///
    /// 在场景中挂到任意 GameObject 上。建议挂在 BackendRoot/ApiClient 物体上。
    /// Inspector 中需要把 DigitalHumanApp 注入或在代码里手动 Init(cfg)。
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        [AddComponentMenu("DigitalHuman/Api Client")]
        private BackendConfig _config;
        private int _defaultTimeoutSec = 20;

        public void Init(BackendConfig cfg) { _config = cfg; }

        public IEnumerator GetJsonAsync<T>(string path, Action<ApiResponse<T>> onOk, Action<BackendException> onErr) where T : class
            => SendAsync<T>(UnityWebRequest.kHttpVerbGET, path, null, onOk, onErr);

        public IEnumerator PostJsonAsync<TReq, TRes>(string path, TReq body, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr)
            where TReq : class where TRes : class
            => SendAsync<TRes>(UnityWebRequest.kHttpVerbPOST, path, body, onOk, onErr);

        public IEnumerator PutJsonAsync<TReq, TRes>(string path, TReq body, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr)
            where TReq : class where TRes : class
            => SendAsync<TRes>(UnityWebRequest.kHttpVerbPUT, path, body, onOk, onErr);

        public IEnumerator DeleteAsync<TRes>(string path, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr) where TRes : class
            => SendAsync<TRes>(UnityWebRequest.kHttpVerbDELETE, path, null, onOk, onErr);

        public IEnumerator PostMultipartAsync<TRes>(string path, byte[] body, string boundary, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr) where TRes : class
        {
            if (_config == null) { onErr?.Invoke(new BackendException(-1, "ApiClient not initialized", 0)); yield break; }

            string url = _config.httpBase + path;
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.uploadHandler.contentType = $"multipart/form-data; boundary={boundary}";
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = 60;
                yield return req.SendWebRequest();

                HandleResponse(req, onOk, onErr);
            }
        }

        private IEnumerator SendAsync<TRes>(string verb, string path, object body, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr) where TRes : class
        {
            if (_config == null) { onErr?.Invoke(new BackendException(-1, "ApiClient not initialized", 0)); yield break; }

            string url = _config.httpBase + path;
            string bodyJson = body == null ? null : JsonUtility.ToJson(body);

            using (var req = new UnityWebRequest(url, verb))
            {
                if (bodyJson != null)
                {
                    var raw = Encoding.UTF8.GetBytes(bodyJson);
                    req.uploadHandler = new UploadHandlerRaw(raw) { contentType = "application/json" };
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Accept", "application/json");
                if (bodyJson != null) req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = _defaultTimeoutSec;
                yield return req.SendWebRequest();

                HandleResponse(req, onOk, onErr);
            }
        }

        private void HandleResponse<TRes>(UnityWebRequest req, Action<ApiResponse<TRes>> onOk, Action<BackendException> onErr) where TRes : class
        {
            long status = req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : null;
            bool httpOk = req.result == UnityWebRequest.Result.Success && status >= 200 && status < 300;

            if (!httpOk)
            {
                onErr?.Invoke(new BackendException(
                    -1,
                    $"HTTP {status} {req.error}\n{body}",
                    (int)status));
                return;
            }

            // 解析统一响应
            var envelope = JsonUtility.FromJson<ApiResponse<Wrapper>>(body);
            if (envelope == null)
            {
                onErr?.Invoke(new BackendException(-1, $"Bad envelope JSON: {body}", (int)status));
                return;
            }
            if (envelope.code != 0)
            {
                onErr?.Invoke(new BackendException(envelope.code, envelope.message, (int)status));
                return;
            }

            string dataJson = JsonExtensions.ExtractDataJson(body);
            TRes data = null;
            if (!string.IsNullOrEmpty(dataJson)) data = JsonUtility.FromJson<TRes>(dataJson);

            onOk?.Invoke(new ApiResponse<TRes>
            {
                code = envelope.code,
                message = envelope.message,
                data = data
            });
        }

        // JsonUtility 不支持泛型 data 字段。用一个空壳。
        [Serializable] private class Wrapper { public int code; public string message; }
    }

    public class BackendException : Exception
    {
        public int Code { get; }
        public int HttpStatus { get; }
        public BackendException(int code, string message, int httpStatus) : base(message)
        {
            Code = code;
            HttpStatus = httpStatus;
        }
    }
}
