using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Robot2Win.Services
{
    public class SocketClient
    {
        private readonly string _url;
        private readonly string _token;
        private SocketIO? _socket;

        private string _connectionState = "disconnected";
        private (int L, int R)? _lastMotors = null;

        public event Action<string>? ConnectionStateChanged;
        public event Action<int?>? BatteryUpdated;

        public SocketClient(string url, string bearerToken)
        {
            _url = url.TrimEnd('/');
            _token = bearerToken;
        }

        public async Task ConnectAsync()
        {
            if (_socket != null) return;

            _socket = new SocketIO(_url, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {_token}" }
                },
                Reconnection = true,
                ReconnectionAttempts = int.MaxValue,
                ReconnectionDelay = 1000
            });

            _socket.OnConnected += (_, __) =>
            {
                _connectionState = "connected";
                ConnectionStateChanged?.Invoke(_connectionState);
            };
            _socket.OnDisconnected += (_, __) =>
            {
                _connectionState = "disconnected";
                ConnectionStateChanged?.Invoke(_connectionState);
            };

            _socket.On("sensor_update", resp =>
            {
                try
                {
                    var obj = resp.GetValue<Dictionary<string, object?>>();
                    if (obj != null && obj.TryGetValue("battery", out var bObj) && bObj != null)
                    {
                        if (int.TryParse(bObj.ToString(), out var batt))
                            BatteryUpdated?.Invoke(batt);
                        else
                            BatteryUpdated?.Invoke(null);
                    }
                }
                catch
                {
                    BatteryUpdated?.Invoke(null);
                }
            });

            await _socket.ConnectAsync();
        }

        public async Task ReconnectAsync()
        {
            try
            {
                if (_socket != null)
                {
                    await _socket.DisconnectAsync();
                    _socket = null;
                }
            }
            catch { }

            await ConnectAsync();
        }

        public async void SendCommand(string cmd)
        {
            if (_socket?.Connected != true) return;
            await _socket.EmitAsync("control", new { command = cmd });
        }

        public async void SendMotor(int left, int right)
        {
            if (_socket?.Connected != true) return;

            var current = (left, right);
            if (_lastMotors.HasValue && _lastMotors.Value == current) return;

            _lastMotors = current;
            string cmd = $"M{left},{right}";
            await _socket.EmitAsync("control", new { command = cmd });
        }
    }
}
