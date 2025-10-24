using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System;

namespace Robot2Win.Services
{
    public class VlcService : IDisposable
    {
        private LibVLC? _lib;
        private MediaPlayer? _mpA;
        private MediaPlayer? _mpB;

        public void Attach(VideoView a, VideoView b)
        {
            _lib = new LibVLC(enableDebugLogs: false);
            _mpA = new MediaPlayer(_lib);
            _mpB = new MediaPlayer(_lib);
            a.MediaPlayer = _mpA;
            b.MediaPlayer = _mpB;
        }

        public void Play(string rtspA, string rtspB)
        {
            if (_lib == null || _mpA == null || _mpB == null) return;

            var opts = new[]
            {
                ":network-caching=150",
                ":clock-synchro=0",
                ":live-caching=150",
                ":rtsp-tcp"
            };

            using var mA = new Media(_lib, rtspA, FromType.FromLocation);
            foreach (var o in opts) mA.AddOption(o);
            _mpA.Play(mA);

            using var mB = new Media(_lib, rtspB, FromType.FromLocation);
            foreach (var o in opts) mB.AddOption(o);
            _mpB.Play(mB);
        }

        public void Dispose()
        {
            try { _mpA?.Dispose(); } catch {}
            try { _mpB?.Dispose(); } catch {}
            try { _lib?.Dispose(); } catch {}
        }
    }
}
