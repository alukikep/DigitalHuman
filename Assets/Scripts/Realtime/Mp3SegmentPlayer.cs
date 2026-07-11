using System;
using System.Collections;
using System.Collections.Generic;
using DigitalHuman.Core;
using UnityEngine;
using UnityEngine.Networking;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Realtime
{
    [AddComponentMenu("DigitalHuman/Mp3 Segment Player")]
    [RequireComponent(typeof(AudioSource))]
    public class Mp3SegmentPlayer : MonoBehaviour
    {
        public event System.Action<string> OnError;
        public event System.Action OnQueueEmpty;

        private AudioSource _source;
        private MemoryStreamBackedSegment _current;
        private readonly Queue<byte[]> _pending = new Queue<byte[]>();
        private bool _playing;
        private Coroutine _loop;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
        }

        public void BeginSegment(string segmentId)
        {
            if (_current != null) _current.Dispose();
            _current = new MemoryStreamBackedSegment(segmentId);
        }

        public void TakeMp3Bytes(byte[] bytes)
        {
            if (_current == null) return;
            try { _current.Stream.Write(bytes, 0, bytes.Length); }
            catch (Exception e) { Logger.Error("mp3", $"segment write: {e.Message}"); }
        }

        public void EndSegment(string segmentId)
        {
            if (_current == null) return;
            byte[] mp3 = _current.ToArray();
            _current.Dispose();
            _current = null;
            if (mp3.Length > 0) _pending.Enqueue(mp3);
            EnsurePlayLoop();
        }

        public void Cancel()
        {
            _pending.Clear();
            if (_current != null) { _current.Dispose(); _current = null; }
            if (_source.isPlaying) _source.Stop();
            _playing = false;
            if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        }

        private void EnsurePlayLoop()
        {
            if (_loop != null) return;
            _loop = StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            while (_pending.Count > 0)
            {
                var mp3 = _pending.Dequeue();
                yield return LoadAndPlay(mp3);
                if (_source.clip != null) { while (_source.isPlaying) yield return null; }
            }
            _playing = false;
            _loop = null;
            try { OnQueueEmpty?.Invoke(); } catch (Exception e) { Logger.Error("mp3", e.Message); }
        }

        private IEnumerator LoadAndPlay(byte[] mp3)
        {
            string tmp = System.IO.Path.Combine(Application.temporaryCachePath, $"dh_{Guid.NewGuid():N}.mp3");
            try
            {
                System.IO.File.WriteAllBytes(tmp, mp3);
                using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + tmp, AudioType.MPEG))
                {
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        OnError?.Invoke($"MP3 load failed: {req.error}");
                        yield break;
                    }
                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip == null) { OnError?.Invoke("MP3 GetContent returned null"); yield break; }
                    _source.clip = clip;
                    _source.Play();
                }
            }
            finally
            {
                try { System.IO.File.Delete(tmp); } catch { }
            }
        }

        private class MemoryStreamBackedSegment
        {
            public System.IO.MemoryStream Stream { get; }
            public string SegmentId { get; }
            public MemoryStreamBackedSegment(string id) { SegmentId = id; Stream = new System.IO.MemoryStream(); }
            public void Dispose() { try { Stream.Dispose(); } catch { } }
            public byte[] ToArray() => Stream.ToArray();
        }
    }
}
