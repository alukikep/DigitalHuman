using System;
using System.Threading.Tasks;
using DigitalHuman.Core;
using DigitalHuman.Net;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Services
{
    /// <summary>独立 WS，订阅 /ws/reminders/{user_id}。</summary>
    [AddComponentMenu("DigitalHuman/Services/Reminder Channel")]
    public class ReminderChannel : MonoBehaviour
    {
        public BackendConfig config;
        public WebSocketChannel channel;

        public event Action<ReminderPushData> OnReminder;
        public event Action<string> OnError;

        public void Inject(BackendConfig cfg, WebSocketChannel ch)
        {
            config = cfg;
            channel = ch;
        }

        public async Task ConnectAsync()
        {
            if (config == null || channel == null) { OnError?.Invoke("not initialized"); return; }
            channel.OnText -= HandleText;
            channel.OnError -= HandleErr;
            channel.OnText += HandleText;
            channel.OnError += HandleErr;
            try { await channel.ConnectAsync(config.ReminderWsUrl()); }
            catch (Exception e) { OnError?.Invoke(e.Message); }
        }

        public async Task CloseAsync()
        {
            try { await channel.CloseAsync(); } catch { }
        }

        private void HandleText(string text)
        {
            try
            {
                string dataJson = JsonExtensions.ExtractDataJson(text);
                if (string.IsNullOrEmpty(dataJson)) return;
                var data = JsonUtility.FromJson<ReminderPushData>(dataJson);
                if (data != null) OnReminder?.Invoke(data);
            }
            catch (Exception e) { Logger.Error("reminder", e.Message); }
        }

        private void HandleErr(string err)
        {
            Logger.Warn("reminder", err);
            OnError?.Invoke(err);
        }

        private void OnDestroy()
        {
            if (channel != null)
            {
                channel.OnText -= HandleText;
                channel.OnError -= HandleErr;
            }
        }
    }
}
