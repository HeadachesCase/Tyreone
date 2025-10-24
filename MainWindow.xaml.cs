using LibVLCSharp.Shared;
using System;
using System.Windows;
using Robot2Win.Services;
using Windows.Gaming.Input;

namespace Robot2Win
{
    public partial class MainWindow : Window
    {
        private const string ROBOT_HOST = "192.168.18.5"; // or "Robot2.local"
        private const string AUTH_TOKEN = "2izgVR3WPWoTYXSD0Hn8FAEDFuL_6DCctXpWpZbuNqS7FmcPh";

        private readonly VlcService _vlc = new();
        private readonly SocketClient _socket;
        private readonly ControllerPoller _pad;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();

            _vlc.Attach(VideoViewA, VideoViewB);
            var camA = $"rtsp://{ROBOT_HOST}:8554/test";
            var camB = $"rtsp://{ROBOT_HOST}:8555/test";
            _vlc.Play(camA, camB);

            var wsUrl = $"http://{ROBOT_HOST}:5000";
            _socket = new SocketClient(wsUrl, AUTH_TOKEN);
            _socket.ConnectionStateChanged += s => Dispatcher.Invoke(() => WsStatus.Text = s);
            _socket.BatteryUpdated += b => Dispatcher.Invoke(() => BatteryText.Text = b is null ? "—" : $"{b}%");
            _socket.ConnectAsync();

            _pad = new ControllerPoller();
            _pad.GamepadStateChanged += (present) =>
                Dispatcher.Invoke(() => PadStatus.Text = present ? "connected" : "not found");

            _pad.SticksSampled += (lx, ly, rx, ry) =>
            {
                int left = ScaleToPwm(-ly);
                int right = ScaleToPwm(-ry);
                _socket.SendMotor(left, right);
            };

            _pad.ButtonsPressed += bp =>
            {
                if (bp.ButtonSouth) _socket.SendCommand("LEDON");
                if (bp.ButtonEast)  _socket.SendCommand("LEDOFF");
            };

            _pad.Start();
        }

        private static int ScaleToPwm(double axis)
        {
            var v = (int)Math.Round(axis * 255.0);
            v = Math.Max(-255, Math.Min(255, v));
            return v;
        }

        private void LedOn_Click(object sender, RoutedEventArgs e)  => _socket.SendCommand("LEDON");
        private void LedOff_Click(object sender, RoutedEventArgs e) => _socket.SendCommand("LEDOFF");
        private void Reconnect_Click(object sender, RoutedEventArgs e) => _socket.ReconnectAsync();
    }
}
