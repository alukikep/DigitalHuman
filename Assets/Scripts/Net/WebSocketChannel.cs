using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DigitalHuman.Core;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Net
{
    /// <summary>
    /// 轻量 WebSocket 通道。事件回调统一在 Unity 主线程（Update 内 flush）。
    /// </summary>
    [AddComponentMenu("DigitalHuman/WebSocket Channel")]
    public class WebSocketChannel : MonoBehaviour
    {
        public event System.Action<string> OnText;
        public event System.Action<byte[]> OnBinary;
        public event System.Action<WebSocketState> OnStateChanged;
        public event System.Action<string> OnError;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _mainThreadTextQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<byte[]> _mainThreadBinaryQueue = new ConcurrentQueue<byte[]>();
        private volatile WebSocketState _state = WebSocketState.None;
        private readonly object _sendLock = new object();

        public WebSocketState State => _state;
        public bool IsOpen => _state == WebSocketState.Open;

        public async Task ConnectAsync(string url, int timeoutSec = 10)
        {
            try
            {
                _socket = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                SetState(WebSocketState.Connecting);
                var connectTask = _socket.ConnectAsync(new Uri(url), token);
                var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutSec * 1000, token));
                if (completed != connectTask) throw new TimeoutException($"WebSocket connect timeout: {url}");
                await connectTask;
                SetState(_socket.State);
                _ = Task.Run(() => PumpLoop(token), token);
                Logger.Info("ws", $"connected {url}");
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                Logger.Error("ws", $"connect failed: {e.Message}");
                SetState(WebSocketState.Closed);
                throw;
            }
        }

        public void SendText(string text)
        {
            if (!IsOpen) { Logger.Warn("ws", "SendText skipped: not open"); return; }
            var bytes = Encoding.UTF8.GetBytes(text ?? "");
            lock (_sendLock)
            {
                if (_socket == null) return;
                _ = _socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void SendBinary(byte[] data)
        {
            if (!IsOpen) { Logger.Warn("ws", "SendBinary skipped: not open"); return; }
            lock (_sendLock)
            {
                if (_socket == null) return;
                _ = _socket.SendAsync(data ?? Array.Empty<byte>(), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        public async Task CloseAsync()
        {
            try
            {
                if (_socket != null && (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived))
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client close", CancellationToken.None);
                }
            }
            catch { }
            finally
            {
                _cts?.Cancel();
                _socket?.Dispose();
                _socket = null;
                SetState(WebSocketState.Closed);
            }
        }

        private void OnDestroy()
        {
            try { _cts?.Cancel(); } catch { }
            try { _socket?.Dispose(); } catch { }
        }

        private void SetState(WebSocketState s)
        {
            if (_state == s) return;
            _state = s;
            var captured = s;
            UnityMainThread.Post(() => OnStateChanged?.Invoke(captured));
        }

        private async Task PumpLoop(CancellationToken token)
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!token.IsCancellationRequested && _socket != null && _socket.State == WebSocketState.Open)
                {
                    var seg = new ArraySegment<byte>(buffer);
                    using (var ms = new System.IO.MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _socket.ReceiveAsync(seg, token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Logger.Info("ws", "server closed");
                                SetState(WebSocketState.CloseReceived);
                                UnityMainThread.Post(() => OnError?.Invoke("server closed"));
                                return;
                            }
                            ms.Write(seg.Array, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var msg = Encoding.UTF8.GetString(ms.ToArray());
                            _mainThreadTextQueue.Enqueue(msg);
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            var bytes = ms.ToArray();
                            var copy = new byte[bytes.Length];
                            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
                            _mainThreadBinaryQueue.Enqueue(copy);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Logger.Error("ws", $"pump error: {e.Message}");
                UnityMainThread.Post(() => OnError?.Invoke(e.Message));
            }
            finally
            {
                SetState(WebSocketState.Closed);
            }
        }

        private void Update()
        {
            while (_mainThreadTextQueue.TryDequeue(out var s))
            {
                try { OnText?.Invoke(s); }
                catch (Exception e) { Logger.Error("ws", $"OnText handler: {e.Message}"); }
            }
            while (_mainThreadBinaryQueue.TryDequeue(out var b))
            {
                try { OnBinary?.Invoke(b); }
                catch (Exception e) { Logger.Error("ws", $"OnBinary handler: {e.Message}"); }
            }
        }
    }

    public static class UnityMainThread
    {
        private static readonly ConcurrentQueue<System.Action> Queue = new ConcurrentQueue<System.Action>();
        private static int _mainThreadId = -1;
        public static void Bind() { _mainThreadId = Thread.CurrentThread.ManagedThreadId; }
        public static void Post(System.Action a)
        {
            if (a == null) return;
            if (_mainThreadId == Thread.CurrentThread.ManagedThreadId) { a(); return; }
            Queue.Enqueue(a);
        }
        public static void Pump()
        {
            while (Queue.TryDequeue(out var a))
            {
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("DigitalHuman/Main Thread Pump")]
    public class UnityMainThreadPump : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBoot()
        {
            var go = new GameObject("[UnityMainThreadPump]");
            DontDestroyOnLoad(go);
            go.AddComponent<UnityMainThreadPump>();
        }
        private void Awake() { UnityMainThread.Bind(); }
        private void Update() { UnityMainThread.Pump(); }
    }
}
