using System;
using System.Timers;
using Windows.Gaming.Input;

namespace Robot2Win.Services
{
    public class ControllerPoller
    {
        private readonly Timer _timer = new(1000.0 / 60.0);
        private Gamepad? _gamepad;
        private bool _present;

        public class ButtonPress
        {
            public bool ButtonSouth { get; set; }
            public bool ButtonEast { get; set; }
        }

        public event Action<bool>? GamepadStateChanged;
        public event Action<double, double, double, double>? SticksSampled;
        public event Action<ButtonPress>? ButtonsPressed;

        public ControllerPoller()
        {
            _timer.Elapsed += (_, __) => Tick();
            Gamepad.GamepadAdded += (_, __) => RefreshGamepad();
            Gamepad.GamepadRemoved += (_, __) => RefreshGamepad();
            RefreshGamepad();
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void RefreshGamepad()
        {
            _gamepad = Gamepad.Gamepads.Count > 0 ? Gamepad.Gamepads[0] : null;
            var nowPresent = _gamepad != null;
            if (nowPresent != _present)
            {
                _present = nowPresent;
                GamepadStateChanged?.Invoke(_present);
            }
        }

        private void Tick()
        {
            if (_gamepad == null)
            {
                RefreshGamepad();
                return;
            }

            var reading = _gamepad.GetCurrentReading();

            double lx = reading.LeftThumbstickX;
            double ly = reading.LeftThumbstickY;
            double rx = reading.RightThumbstickX;
            double ry = reading.RightThumbstickY;

            (lx, ly) = Deadzone(lx, ly);
            (rx, ry) = Deadzone(rx, ry);

            SticksSampled?.Invoke(lx, ly, rx, ry);

            var bp = new ButtonPress
            {
                ButtonSouth = reading.Buttons.HasFlag(GamepadButtons.A),
                ButtonEast  = reading.Buttons.HasFlag(GamepadButtons.B)
            };
            if (bp.ButtonSouth || bp.ButtonEast)
                ButtonsPressed?.Invoke(bp);
        }

        private static (double, double) Deadzone(double x, double y, double dz = 0.12)
        {
            double mag = Math.Sqrt(x * x + y * y);
            if (mag < dz) return (0, 0);
            double scale = (mag - dz) / (1 - dz);
            double nx = (x / mag) * scale;
            double ny = (y / mag) * scale;
            return (nx, ny);
        }
    }
}
