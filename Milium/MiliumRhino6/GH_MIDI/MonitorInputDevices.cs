using System;
using System.Timers;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI
{
    /// <summary>
    ///     This class is responsible to keep track of all connected and active input devices.
    ///     Sadly there is no event handler on device changes in winmm.dll
    ///     Therefore I had to come up with an ugly polling solution.
    ///     Every 50ms the device count gets checked and it updates if it detects a change.
    /// </summary>
    public static class MonitorInputDevices
    {
        private static int _deviceCount;
        private static readonly Timer Timer;
        private static bool _run;

        public static InputDevice[] ConnectedInputDevices;
        public static bool[] IsInputDeviceActive;

        public static event EventHandler InputDeviceEvent;

        static MonitorInputDevices()
        {
            Timer = new Timer();
            Timer.Interval = 50;
            Timer.Elapsed += OnTimerElapsed;
            Timer.AutoReset = true;
            _run = false;
        }

        /// <summary>
        ///     Makes sure the monitor is not running when it is not needed. When started the devices get initialized and before
        ///     closing all devices are disposed.
        /// </summary>
        public static bool Run
        {
            get => _run;
            set
            {
                if (value && !_run)
                {
                    InitializeAllDevices();
                    Timer.Start();
                    _run = true;
                }

                if (!value && _run)
                {
                    DisposeAllDevices();
                    Timer.Stop();
                    _run = false;
                }
            }
        }

        /// <summary>
        ///     Create new instances of midi input devices. After the initialization it is possible to receive midi messages.
        /// </summary>
        public static void InitializeAllDevices()
        {
            _deviceCount = InputDevice.DeviceCount;
            ConnectedInputDevices = new InputDevice[_deviceCount];
            IsInputDeviceActive = new bool[_deviceCount];
            for (var i = 0; i < _deviceCount; i++)
                ConnectedInputDevices[i] = new InputDevice(i);
        }

        /// <summary>
        ///     Dispose all midi input devices. After that the new instances need to be created to receive a midi signal.
        /// </summary>
        public static void DisposeAllDevices()
        {
            for (var i = 0; i < _deviceCount; i++)
            {
                var device = ConnectedInputDevices[i];
                if (IsInputDeviceActive[i]) device.StopRecording();
                device.Dispose();
            }

            _deviceCount = 0;
            ConnectedInputDevices = new InputDevice[0];
            IsInputDeviceActive = new bool[0];
        }

        /// <summary>
        ///     Once it detected change it will dispose all old instances of input devices
        ///     and reinitialize them. Like this the correct order will be ensured all the time.
        /// </summary>
        private static void OnTimerElapsed(object sender, EventArgs e)
        {
            if (_deviceCount != InputDevice.DeviceCount)
            {
                DisposeAllDevices();
                InitializeAllDevices();
                InputDeviceEvent?.Invoke(typeof(MonitorInputDevices), EventArgs.Empty);
            }
        }
    }
}