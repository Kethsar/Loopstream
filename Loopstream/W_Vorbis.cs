using System;
using VorbisEncode;
using System.IO;
using System.Threading;

namespace Loopstream
{
    /// <summary>
    /// Thin <see cref="Stream"/> around <see cref="VorbisEncoder"/>
    /// </summary>
    public class VorbisEncoderStream : Stream, IDisposable
    {
        private VorbisEncoder m_ve;
        private bool m_eos;
        private readonly object m_lock = new object();

        public VorbisEncoderStream()
        {
            m_ve = new VorbisEncoder();
            m_eos = false;
        }

        public VorbisEncoderStream(int channels, int samplerate, float quality) : this()
        {
            m_ve.Channels = channels;
            m_ve.SampleRate = samplerate;
            m_ve.Quality = quality;
            m_ve.Mode = VorbisEncoder.BitrateMode.VBR;
        }

        public VorbisEncoderStream(int channels, int samplerate, int bitrate) : this()
        {
            m_ve.Channels = channels;
            m_ve.SampleRate = samplerate;
            m_ve.Bitrate = bitrate;
            m_ve.Mode = VorbisEncoder.BitrateMode.CBR;
        }

        public VorbisEncoder Encoder
        {
            get
            {
                return m_ve;
            }
        }

        public override bool CanRead
        {
            get
            {
                return m_ve.Buffer != null && !m_ve.Buffer.IsEmpty();
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_ve.Buffer == null || !m_ve.Buffer.IsFull(); // The buffer is created on the first attempt to write to it if it is still null
            }
        }

        public override long Length
        {
            get
            {
                return (m_ve.Buffer != null) ? m_ve.Buffer.Size : 0;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }

            set
            {
                throw new InvalidOperationException("Let the VorbisEncoder internal buffer handle position.");
            }
        }

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            lock (m_lock)
            {
                while (!CanRead && !m_eos)
                {
                    Monitor.Wait(m_lock);
                }

                bytesRead = m_ve.GetBytes(buffer, offset, count);

                Monitor.PulseAll(m_lock);
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This is just a wrapper around an encoder and its shitty RingBuffer, what do you want?");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Non-resizable stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (m_lock)
            {
                while (!CanWrite && !m_ve.Buffer.CanWrite(count))
                    Monitor.Wait(m_lock);

                if (count > 0)
                    m_ve.PutBytes(buffer, offset, count);
                else
                    m_eos = true;

                Monitor.PulseAll(m_lock);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            m_ve.Dispose();
            base.Dispose(disposing);
        }
    }
}
