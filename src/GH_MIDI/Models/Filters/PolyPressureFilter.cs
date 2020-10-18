using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace Milium.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for midi poly pressure messages. Pitches, values and channels can be specified.
    /// </summary>
    public class PolyPressureFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public List<Pitch> AffectedPitches = new List<Pitch>();
        public List<int> AffectedValues = new List<int>();

        public PolyPressureFilter(List<Channel> channels, List<Pitch> pitches, List<int> values)
        {
            AffectedChannels = channels;
            AffectedPitches = pitches;
            AffectedValues = values;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.Channel) return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).Command != ChannelCommand.PolyPressure)
                return FilterAffectionType.MessageDoesNotFit;

            if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                AffectedPitches.Contains((Pitch) ((ChannelMessage) midiMessage).Data1) &&
                AffectedValues.Contains(((ChannelMessage) midiMessage).Data2))
                return FilterAffectionType.MessageFitsTypeAndValue;

            return FilterAffectionType.MessageFitsType;
        }
    }
}