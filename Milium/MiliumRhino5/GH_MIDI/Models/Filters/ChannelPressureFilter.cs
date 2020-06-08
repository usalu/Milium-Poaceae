using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for midi channel pressure messages. Values and channels can be specified.
    /// </summary>
    public class ChannelPressureFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public List<int> AffectedValues = new List<int>();

        public ChannelPressureFilter(List<Channel> channels, List<int> values)
        {
            AffectedChannels = channels;
            AffectedValues = values;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.SystemExclusive) return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).Command != ChannelCommand.ChannelPressure)
                return FilterAffectionType.MessageDoesNotFit;

            if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                AffectedValues.Contains(((ChannelMessage) midiMessage).Data1))
                return FilterAffectionType.MessageFitsTypeAndValue;

            return FilterAffectionType.MessageFitsType;
        }
    }
}