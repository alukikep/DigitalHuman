using System;
using System.Collections;
using DigitalHuman.Core;
using DigitalHuman.Net;
using DigitalHuman.Realtime;
using UnityEngine;

namespace DigitalHuman.Services
{
    /// <summary>文件 ASR 上传：POST /asr/transcribe。</summary>
    [AddComponentMenu("DigitalHuman/Services/File Asr Uploader")]
    public class FileAsrUploader : MonoBehaviour
    {
        public ApiClient api;

        [Serializable]
        public class TranscribeData
        {
            public string text;
            public string language;
            public double duration_sec;
        }

        public void Inject(ApiClient c) { api = c; }

        public IEnumerator TranscribeAsync(string filename, byte[] bytes, string contentType, Action<ApiResponse<TranscribeData>> onOk, Action<BackendException> onErr)
        {
            var mb = new MultipartBuilder();
            mb.AddFile("file", filename, bytes, contentType ?? "application/octet-stream");
            var body = mb.Build();
            return api.PostMultipartAsync<TranscribeData>("/asr/transcribe", body, mb.Boundary, onOk, onErr);
        }

        public IEnumerator TranscribePcm16LeAsync(int sampleRate, byte[] pcm16LeMono, Action<ApiResponse<TranscribeData>> onOk, Action<BackendException> onErr)
        {
            var wav = WavFileWriter.Build(sampleRate, 1, 16, pcm16LeMono);
            return TranscribeAsync("recording.wav", wav, "audio/wav", onOk, onErr);
        }
    }
}
