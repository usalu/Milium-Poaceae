using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MiliumRhino6.Properties;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI.Components
{
    /// <summary>
    ///     This component is the heart of the plugin. It makes sure to send the midi messages from windows to grasshopper.
    ///     In the moment only channel messages are being sent.
    ///     2 different thresholds can be set.
    ///     The first is a general threshold that affects same channel messages.
    ///     Same means the channel and the category value are identical.
    ///     With that threshold you can affect how responsive your grasshoper definition will be.
    ///     This general threshold excludes the min and max value of a channel message.
    ///     A threshold for these values can be set separately.
    /// </summary>
    public class MidiInComponent : GH_Component
    {
        public static int ComponentCount;
        public int BoundaryThresholdChannelMessage;
        public bool[] IsSubscribed = new bool[InputDevice.DeviceCount];
        public ChannelMessage LastChannelMessage;
        public int ThresholdChannelMessage;
        public int?[] Timestamps = new int?[16];

        public MidiInComponent()
            : base("MIDIIn", "MIDIIn", "Responsive MIDI input from a MIDI port. Only channel messages are received.",
                "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.MIDIIn_Icon;

        public override Guid ComponentGuid => new Guid("bd395eb0-28f7-4081-ab92-aedf9d6c9052");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Port", "P", "MIDIIn port", 0);
            pManager.AddIntegerParameter("Threshold", "T",
                "Threshold for same channel messages (excluding values in comparison) in ms. For the min and max values use BoundaryThreshold.",
                0, 300);
            pManager.AddIntegerParameter("BoundaryThreshold", "BT",
                "Boundary threshold is like the threshold except that it only targets channel messages with min and max values(0 and 127).",
                0, 0);
            pManager.AddBooleanParameter("Run", "R", "Run component and start receiving input", 0, false);

            var paramInputDevice = (Param_Integer) pManager[0];
            paramInputDevice.Optional = true;
            for (var deviceId = 0; deviceId < InputDevice.DeviceCount; deviceId++)
                paramInputDevice.AddNamedValue(InputDevice.GetDeviceCapabilities(deviceId).name, deviceId);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MIDIMessage", "M", "Received encoded MIDI message", 0);
        }

        /// <summary>
        ///     When component is created a event handler to input midi devices is created to ensure a responsive UI later.
        ///     Makes sure that the input device monitor is only initialized if there is at least one component active.
        /// </summary>
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            MonitorInputDevices.InputDeviceEvent += UpdateMidiDevices;
            ComponentCount += 1;
            if (ComponentCount == 1)
            {
                MonitorInputDevices.Run = true;
                UpdateMidiDevices(this, new EventArgs());
            }
        }

        /// <summary>
        ///     Clean delete the component and make sure all event handler outside the component are removed.
        ///     If that was the last active component the input device monitor will be turned off.
        /// </summary>
        public override void RemovedFromDocument(GH_Document document)
        {
            MonitorInputDevices.InputDeviceEvent -= UpdateMidiDevices;
            UnsubscribeAll();
            ComponentCount -= 1;
            if (ComponentCount == 0)
                MonitorInputDevices.Run = false;
            base.RemovedFromDocument(document);
        }

        /// <summary>
        ///     The component can only call ExpireSolution from a main UI thread.
        ///     This methods makes sure that this component can be expired from another thread as well.
        /// </summary>
        private void ExpireSolution()
        {
            Control control = Instances.ActiveCanvas;
            if (control == null)
                return;
            var action = new Action<bool>(ExpireSolution);
            control.Invoke(action, true);
        }

        /// <summary>
        ///     Unsubscribe from channel message event to make sure that the component receives only messages from the currently
        ///     selected device.
        /// </summary>
        /// <param name="exceptIndex"></param>
        public void UnsubscribeAll(int? exceptIndex = null)
        {
            for (var i = 0; i < IsSubscribed.Length; i++)
            {
                if (i == exceptIndex) continue;
                if (IsSubscribed[i])
                    try
                    {
                        MonitorInputDevices.ConnectedInputDevices[i].ChannelMessageReceived -= OnChannelMessageReceived;
                        IsSubscribed[i] = false;
                    }
                    catch
                    {
                    }
            }
        }

        /// <summary>
        ///     Updates the component menu and resets all the the values if a change of MIDI devices is detected.
        ///     There will be new named values of all the devices available if you right click on port.
        /// </summary>
        private void UpdateMidiDevices(object sender, EventArgs e)
        {
            LastChannelMessage = null;
            Timestamps = Enumerable.Repeat<int?>(null, 16).ToArray();
            UnsubscribeAll();
            IsSubscribed = new bool[MonitorInputDevices.ConnectedInputDevices.Length];
            var paramInputDevice = (Param_Integer) Params.Input[0];
            paramInputDevice.ClearNamedValues();
            for (var deviceId = 0; deviceId < InputDevice.DeviceCount; deviceId++)
                paramInputDevice.AddNamedValue(InputDevice.GetDeviceCapabilities(deviceId).name, deviceId);
            ExpireSolution();
        }

        /// <summary>
        ///     Is triggered if a channel message is received by the connected device.
        ///     The component only expires if it manages to get over the threshold.
        ///     This way the script can be inert or more responsive.
        /// </summary>
        /// <param name="midiMessage">Channel midi message information</param>
        private void OnChannelMessageReceived(object sender, ChannelMessageEventArgs midiMessage)
        {
            if (Timestamps[midiMessage.Message.MidiChannel] == null
                || midiMessage.Message.Timestamp >=
                Timestamps[midiMessage.Message.MidiChannel] + ThresholdChannelMessage
                || (midiMessage.Message.Data2 == 0 || midiMessage.Message.Data2 == sbyte.MaxValue) &&
                midiMessage.Message.Timestamp >=
                Timestamps[midiMessage.Message.MidiChannel] + BoundaryThresholdChannelMessage)
            {
                LastChannelMessage = midiMessage.Message;
                Timestamps[midiMessage.Message.MidiChannel] = midiMessage.Message.Timestamp;
                ExpireSolution();
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Makes sure to only get a output signal when a midi channel message is received.
            DA.DisableGapLogic();

            if (MonitorInputDevices.ConnectedInputDevices.Length == 0)
            {
                base.Message = "No MIDI devices\n" +
                               "connected.";
                return;
            }

            var port = 0;

            if (!DA.GetData(0, ref port))
            {
                base.Message = "Select a device.";
                return;
            }

            if (MonitorInputDevices.ConnectedInputDevices.Length <= port)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The given port doesn't exist (anymore).");
                base.Message = "Device port\n" +
                               "doesn't exist";
                return;
            }

            var run = false;
            var device = MonitorInputDevices.ConnectedInputDevices[port];

            DA.GetData(1, ref ThresholdChannelMessage);
            DA.GetData(2, ref BoundaryThresholdChannelMessage);
            DA.GetData(3, ref run);

            if (run)
            {
                //Device needs to be recording in order to receive signals
                //NOTE that it will be never closed unless the MIDI input devices change or no more component is active
                //Otherwise if you close it inside this component it will close it all for all other components as well.
                if (!MonitorInputDevices.IsInputDeviceActive[port]) device.StartRecording();
                UnsubscribeAll(port);
                if (!IsSubscribed[port])
                {
                    device.ChannelMessageReceived += OnChannelMessageReceived;
                    IsSubscribed[port] = true;
                    LastChannelMessage = null;
                }
                base.Message = "Receiving:\n" + InputDevice.GetDeviceCapabilities(port).name;
            }

            else
            {
                base.Message = "Connected:\n" + InputDevice.GetDeviceCapabilities(port).name;
                if (IsSubscribed[port])
                {
                    device.ChannelMessageReceived -= OnChannelMessageReceived;
                    IsSubscribed[port] = false;
                    LastChannelMessage = null;
                }
            }

            if (LastChannelMessage != null)
                DA.SetData(0, LastChannelMessage);
        }
    }
}