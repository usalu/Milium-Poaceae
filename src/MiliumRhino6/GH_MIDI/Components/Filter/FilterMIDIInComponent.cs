using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using MiliumRhino6.GH_MIDI.Models;
using MiliumRhino6.GH_MIDI.Models.Filters;
using MiliumRhino6.Properties;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI.Components.Filter
{
    /// <summary>
    ///     This component makes it possible to apply general and mainly special filter rules.
    ///     Any combination of values and types can be targeted with this component.
    ///     Therefore there is one additional special component for each type of message.
    ///     This filter will include a message if:
    ///     -the midi channel is included in the the general midi channels
    ///     -there is no special filter rule defined for this type
    ///     -or a specific filter rules includes the message
    ///     otherwise the message will be excluded.
    /// </summary>
    public class FilterMidiInComponent : GH_Component
    {
        public FilterMidiInComponent()
            : base("FilterMIDI", "Filter",
                "Filter MIDI channel messages. General channels can be specified. Messages with other channels land instantly outside.\n" +
                "Specified filter rules can be provided. It is possible to provide multiple filter rules for the same type.\n" +
                "NOTE: If you insert one or more filter rules for a specific type (like control change message)\n" +
                "all messages from this type that do not fit one of the rules will land outside.", "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.Filter_Icon;

        public override Guid ComponentGuid => new Guid("82c3a0db-2bd8-4bf1-a490-b3216ed03e9a");

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MIDIMessage", "M", "Encoded MIDI message", 0);
            pManager.AddIntegerParameter("Channels", "C", "General channels to listen to.", (GH_ParamAccess) 1);
            pManager.AddGenericParameter("Filter rules", "FR", "Specific filter rules can be added.",
                (GH_ParamAccess) 1);

            var paramChannel = (Param_Integer) pManager[1];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));

            var paramFilter = (Param_GenericObject) pManager[2];
            paramFilter.Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MessageInsideFilter", "MIF", "Encoded MIDI message that is inside the filter",
                0);
            pManager.AddGenericParameter("MessageOutsideFilter", "MOF",
                "Encoded MIDI message that is outside the filter", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.DisableGapLogic();

            var obj = new GH_ObjectWrapper();

            if (!DA.GetData(0, ref obj)) return;
            if (obj == null) return;

            var midiMessage = (IMidiMessage) obj.Value;

            var channels = new List<int>();
            DA.GetDataList(1, channels);

            if (midiMessage.MessageType != MessageType.Channel) return;

            if (channels.Contains(((ChannelMessage) midiMessage).MidiChannel))
            {
                var filterMidiInList = new List<FilterMidiIn>();
                if (DA.GetDataList(2, filterMidiInList))
                    foreach (var filterMidiIn in filterMidiInList)
                        switch (filterMidiIn.MessageAffectionType(midiMessage))
                        {
                            case FilterAffectionType.MessageDoesNotFit:
                                DA.SetData(0, midiMessage);
                                return;
                            case FilterAffectionType.MessageFitsType:
                                DA.SetData(1, midiMessage);
                                return;
                            case FilterAffectionType.MessageFitsTypeAndValue:
                                DA.SetData(0, midiMessage);
                                return;
                            default:
                                continue;
                        }
                else
                    DA.SetData(0, midiMessage);
            }
            else
            {
                DA.SetData(1, midiMessage);
            }
        }
    }
}