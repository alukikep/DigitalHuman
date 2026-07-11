using System;
using System.Text;
using DigitalHuman.Core;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Net
{
    public static class JsonExtensions
    {
        /// <summary>JsonUtility 不能直接序列化顶层是字典/数组的对象，本方法允许把对象序列化为 JSON 字符串。</summary>
        public static string ToJson(object obj) => JsonUtility.ToJson(obj);

        /// <summary>反序列化为 T；失败返回 default。</summary>
        public static T FromJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (Exception e)
            {
                Logger.Error("json", $"FromJson<{typeof(T).Name}> failed: {e.Message}\n{json}");
                return null;
            }
        }

        /// <summary>从原始 JSON 文本里抽出 "data" 子对象的 JSON 字符串。</summary>
        public static string ExtractDataJson(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return null;
            int idx = rawJson.IndexOf("\"data\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = rawJson.IndexOf(':', idx);
            if (colon < 0) return null;
            int p = colon + 1;
            while (p < rawJson.Length && (rawJson[p] == ' ' || rawJson[p] == '\n' || rawJson[p] == '\r' || rawJson[p] == '\t')) p++;
            if (p >= rawJson.Length) return null;
            char ch = rawJson[p];

            if (ch == '{' || ch == '[') return ExtractBalanced(rawJson, p);
            return null;
        }

        private static string ExtractBalanced(string s, int start)
        {
            char open = s[start];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            bool inStr = false;
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if (inStr)
                {
                    if (c == '\\' && i + 1 < s.Length) { i++; continue; }
                    if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0) return s.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        public static byte[] Utf8(string s) => string.IsNullOrEmpty(s) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(s);
    }
}
