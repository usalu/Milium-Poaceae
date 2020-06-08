using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MiliumRhino5.GH_MIDI.Models;
using MiliumRhino5.Properties;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Components
{
    /// <summary>
    ///     This component is there for better understanding MIDI channel messages.
    ///     It will be probably only used in the beginning and for debugging
    ///     or understand what messages are sent by the controller.
    /// </summary>
    public class DecodeMidiInComponent : GH_Component
    {
        public DecodeMidiInComponent()
            : base("DecodeMIDIMessage", "Decode", "Decode encoded channel MIDI messages.", "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.Decode_Icon;

        public override Guid ComponentGuid => new Guid("4c23d89c-7cf2-43f5-8e8b-c8212bea8091");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MIDIMessage", "M", "Encoded MIDIIn message", 0);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MessageType", "T", "Type of decoded Message.", 0);
            pManager.AddGenericParameter("Channel", "C", "Channel of decoded Message.", 0);
            pManager.AddGenericParameter("CategoryValue", "CV", "Category value of decoded Message.", 0);
            pManager.AddIntegerParameter("IntensityValue", "IV", "Intensity value of decoded Message.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //For the custom type IMIDIMessage there is a wrapper needed in order to transfer the object between components.
            var obj = new GH_ObjectWrapper();

            if (!DA.GetData(0, ref obj)) return;
            if (obj == null) return;

            var midiMessage = (IMidiMessage)obj.Value;

            if (midiMessage.MessageType == MessageType.Channel)
            {
                var channelMessage = (ChannelMessage)midiMessage;
                dynamic data1 = null;
                switch (channelMessage.Command)
                {
                    case ChannelCommand.NoteOn:
                        data1 = (Pitch)channelMessage.Data1;
                        break;
                    case ChannelCommand.NoteOff:
                        data1 = (Pitch)channelMessage.Data1;
                        break;
                    case ChannelCommand.Controller:
                        data1 = (ControllerType)channelMessage.Data1;
                        break;
                    case ChannelCommand.ProgramChange:
                        data1 = (Instrument)channelMessage.Data1;
                        break;
                    case ChannelCommand.PolyPressure:
                        data1 = (Pitch)channelMessage.Data1;
                        break;
                    default:
                        data1 = channelMessage.Data1;
                        break;
                }

                DA.SetData(0, channelMessage.Command.ToString());
                DA.SetData(1, (Channel)channelMessage.MidiChannel);
                DA.SetData(2, data1);
                DA.SetData(3, channelMessage.Data2);
            }
            else if (midiMessage.MessageType == MessageType.SystemExclusive)
            {
                DA.SetData(0, ((SysExMessage)midiMessage).SysExType);
                DA.SetData(2, ((SysExMessage)midiMessage).Status);
                DA.SetData(3, ((SysExMessage)midiMessage).Length);
            }
        }
    }
}