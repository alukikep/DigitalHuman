using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalHuman.Net
{
    /// <summary>手写 multipart/form-data 编码；Unity 6+ 才内置 MultipartFormSection，这里避免额外依赖。</summary>
    public class MultipartBuilder
    {
        private readonly string _boundary;
        private readonly List<byte[]> _parts = new List<byte[]>();
        private readonly byte[] _trailer;

        public string Boundary => _boundary;

        public MultipartBuilder()
        {
            _boundary = "----DigitalHumanFormBoundary" + Guid.NewGuid().ToString("N");
            _trailer = Encoding.ASCII.GetBytes($"--{_boundary}--\r\n");
        }

        public void AddField(string name, string value)
        {
            var sb = new StringBuilder();
            sb.Append("--").Append(_boundary).Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"").Append(name).Append("\"\r\n\r\n");
            sb.Append(value).Append("\r\n");
            _parts.Add(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        public void AddFile(string name, string filename, byte[] content, string contentType)
        {
            var sb = new StringBuilder();
            sb.Append("--").Append(_boundary).Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"").Append(name).Append("\"; filename=\"").Append(filename).Append("\"\r\n");
            sb.Append("Content-Type: ").Append(contentType).Append("\r\n\r\n");
            var head = Encoding.UTF8.GetBytes(sb.ToString());
            var tail = Encoding.ASCII.GetBytes("\r\n");
            // 合并 head + content + tail
            var combined = new byte[head.Length + content.Length + tail.Length];
            Buffer.BlockCopy(head, 0, combined, 0, head.Length);
            Buffer.BlockCopy(content, 0, combined, head.Length, content.Length);
            Buffer.BlockCopy(tail, 0, combined, head.Length + content.Length, tail.Length);
            _parts.Add(combined);
        }

        public byte[] Build()
        {
            int total = _trailer.Length;
            foreach (var p in _parts) total += p.Length;
            var result = new byte[total];
            int offset = 0;
            foreach (var p in _parts)
            {
                Buffer.BlockCopy(p, 0, result, offset, p.Length);
                offset += p.Length;
            }
            Buffer.BlockCopy(_trailer, 0, result, offset, _trailer.Length);
            return result;
        }
    }
}
