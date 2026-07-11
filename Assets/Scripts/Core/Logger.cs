using UnityEngine;

namespace DigitalHuman.Core
{
    /// <summary>统一前缀的日志工具，方便日志过滤。</summary>
    public static class Logger
    {
        private const string Prefix = "[DH]";

        public static void Info(string tag, string msg) => Debug.Log($"{Prefix} [{tag}] {msg}");
        public static void Warn(string tag, string msg) => Debug.LogWarning($"{Prefix} [{tag}] {msg}");
        public static void Error(string tag, string msg) => Debug.LogError($"{Prefix} [{tag}] {msg}");
    }
}
