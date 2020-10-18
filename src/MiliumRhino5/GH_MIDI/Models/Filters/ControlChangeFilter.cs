using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for midi control change messages. Controls, values and channels can be specified.
    /// </summary>
    public class ControlChangeFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public List<ControllerType> AffectedControls = new List<ControllerType>();
        public List<int> AffectedValues = new List<int>();

        public ControlChangeFilter(
            List<Channel> channels,
            List<ControllerType> controls,
            List<int> values)
        {
            AffectedChannels = channels;
            AffectedControls = controls;
            AffectedValues = values;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.Channel) return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).Command != ChannelCommand.Controller)
                return FilterAffectionType.MessageDoesNotFit;

            if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                AffectedControls.Contains((ControllerType) ((ChannelMessage) midiMessage).Data1) &&
                AffectedValues.Contains(((ChannelMessage) midiMessage).Data2))
                return FilterAffectionType.MessageFitsTypeAndValue;

            return FilterAffectionType.MessageFitsType;
        }
    }
}