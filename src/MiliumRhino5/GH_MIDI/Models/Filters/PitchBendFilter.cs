using System.Collections.Generic;
using Sanford.Multimedia.Midi;
using Rhino.Geometry;

namespace MiliumRhino5.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for pitch bend messages. Intervals to test inclusion of the value and channels can be specified.
    /// </summary>
    public class PitchBendFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public List<Interval> AffectedValueIntervals = new List<Interval>();

        public PitchBendFilter(List<Channel> channels, List<Interval> intervals)
        {
            AffectedChannels = channels;
            AffectedValueIntervals = intervals;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.Channel) return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).Command != ChannelCommand.PitchWheel)
                return FilterAffectionType.MessageDoesNotFit;

            foreach (var interval in AffectedValueIntervals)
                if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                    interval.IncludesParameter(((ChannelMessage) midiMessage).Data2))
                    return FilterAffectionType.MessageFitsTypeAndValue;
            return FilterAffectionType.MessageFitsType;
        }
    }
}