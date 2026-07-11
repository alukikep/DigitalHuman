using System;
using System.IO;
using System.Text;

namespace DigitalHuman.Realtime
{
    /// <summary>写一个 16 kHz / mono / 16-bit PCM 的 WAV 文件。可用于 FileAsrUploader 上传音频。</summary>
    public static class WavFileWriter
    {
        public static byte[] Build(int sampleRate, short channels, short bitsPerSample, byte[] pcm16Le)
        {
            int dataSize = pcm16Le.Length;
            int totalSize = 44 + dataSize;
            var ms = new MemoryStream(totalSize);
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(totalSize - 8);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);                           // PCM fmt chunk size
                bw.Write((short)1);                     // PCM format
                bw.Write(channels);
                bw.Write(sampleRate);
                bw.Write(sampleRate * channels * bitsPerSample / 8);
                bw.Write((short)(channels * bitsPerSample / 8));
                bw.Write(bitsPerSample);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                bw.Write(pcm16Le);
            }
            return ms.ToArray();
        }
    }
}
