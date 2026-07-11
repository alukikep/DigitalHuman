using System;
using DigitalHuman.Core;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Realtime
{
    /// <summary>
    /// 麦克风采集 + 重采样到 16 kHz 单声道 PCM16 little-endian。每隔 chunkMs 抛出 OnFrame(byte[])。
    /// </summary>
    [AddComponentMenu("DigitalHuman/Microphone Capture")]
    public class MicrophoneCapture : MonoBehaviour
    {
        [Tooltip("后端配置")]
        public BackendConfig config;
        public event Action<byte[]> OnFrame;
        public event Action<string> OnError;

        private AudioClip _clip;
        private string _device;
        private bool _running;
        private int _lastSamplePos;
        private float _accumSec;
        private float[] _frameBuffer;
        private int _frameBufferPos;

        public bool IsRunning => _running;
        public void Inject(BackendConfig cfg) => config = cfg;

        public void StartRecording()
        {
            if (_running) return;
            if (config == null) { OnError?.Invoke("MicrophoneCapture missing config"); return; }
            try
            {
                _device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
                _clip = Microphone.Start(_device, true, Math.Max(1, config.maxUtteranceSeconds + 1), config.microphoneSampleRate);
                if (_clip == null) { OnError?.Invoke("Microphone.Start returned null"); return; }
                _lastSamplePos = 0; _accumSec = 0f;
                int targetSamples = config.targetSampleRate * config.chunkMs / 1000;
                _frameBuffer = new float[targetSamples];
                _frameBufferPos = 0;
                _running = true;
                Logger.Info("mic", $"start device='{_device}' sr={config.microphoneSampleRate}");
            }
            catch (Exception e) { OnError?.Invoke(e.Message); Logger.Error("mic", $"start failed: {e.Message}"); }
        }

        public void StopRecording()
        {
            if (!_running) return;
            _running = false;
            try { if (Microphone.IsRecording(_device)) Microphone.End(_device); } catch { }
            _clip = null;
            Logger.Info("mic", "stop");
        }

        private void Update()
        {
            if (!_running || _clip == null) return;
            int pos = Microphone.GetPosition(_device);
            if (pos < 0 || _clip.samples <= 0) return;
            int diff = pos - _lastSamplePos;
            if (diff < 0) diff += _clip.samples;
            if (diff == 0) return;

            float secondsAvailable = (float)diff / config.microphoneSampleRate;
            _accumSec += secondsAvailable;
            _lastSamplePos = pos;

            if (_accumSec + 1e-4f >= config.chunkMs / 1000f)
            {
                int targetPerChunk = config.targetSampleRate * config.chunkMs / 1000;
                float[] readBuf = new float[diff];
                int readOffset = pos - diff;
                if (readOffset < 0) readOffset += _clip.samples;
                bool wrapped = (readOffset + diff) > _clip.samples;
                if (!wrapped) _clip.GetData(readBuf, readOffset);
                else
                {
                    int firstLen = _clip.samples - readOffset;
                    var first = new float[firstLen];
                    _clip.GetData(first, readOffset);
                    int secondLen = diff - firstLen;
                    var second = new float[secondLen];
                    _clip.GetData(second, 0);
                    Array.Copy(first, 0, readBuf, 0, firstLen);
                    Array.Copy(second, 0, readBuf, firstLen, secondLen);
                }

                float ratio = (float)config.microphoneSampleRate / config.targetSampleRate;
                int nOut = Mathf.RoundToInt(diff / ratio);
                if (nOut <= 0) { _accumSec = 0f; return; }
                var outBuf = new float[nOut];
                for (int i = 0; i < nOut; i++)
                {
                    float srcIdx = i * ratio;
                    int i0 = (int)srcIdx;
                    int i1 = i0 + 1;
                    if (i1 >= diff) i1 = diff - 1;
                    float t = srcIdx - i0;
                    outBuf[i] = readBuf[i0] * (1f - t) + readBuf[i1] * t;
                }
                AppendToFrameBuffer(outBuf);
                _accumSec = 0f;
                if (_frameBufferPos == _frameBuffer.Length)
                {
                    var bytes = Pcm16LeBytes(_frameBuffer);
                    try { OnFrame?.Invoke(bytes); } catch (Exception e) { Logger.Error("mic", e.Message); }
                    _frameBufferPos = 0;
                }
            }
        }

        private void AppendToFrameBuffer(float[] src)
        {
            int n = src.Length;
            int dstRemain = _frameBuffer.Length - _frameBufferPos;
            int copy = Math.Min(n, dstRemain);
            Array.Copy(src, 0, _frameBuffer, _frameBufferPos, copy);
            _frameBufferPos += copy;
            if (_frameBufferPos == _frameBuffer.Length) return;
        }

        private static byte[] Pcm16LeBytes(float[] pcm)
        {
            byte[] bytes = new byte[pcm.Length * 2];
            for (int i = 0; i < pcm.Length; i++)
            {
                float v = Mathf.Clamp(pcm[i], -1f, 1f);
                short s = (short)Math.Round(v * 32767f);
                int o = i * 2;
                bytes[o] = (byte)(s & 0xff);
                bytes[o + 1] = (byte)((s >> 8) & 0xff);
            }
            return bytes;
        }

        public static byte[] EncodePcm16Le(float[] pcm) => Pcm16LeBytes(pcm);
    }
}
