using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for midi program change messages. Instruments(relevant for all channels except channel 10),
    ///     percussions (obviously only relevant for channel 10), velocities and channels can be specified.
    /// </summary>
    public class ProgramChangeFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public List<Instrument> AffectedInstruments = new List<Instrument>();
        public List<Percussion> AffectedPercussions = new List<Percussion>();

        public ProgramChangeFilter(
            List<Channel> channels,
            List<Instrument> instruments,
            List<Percussion> percussions)
        {
            AffectedChannels = channels;
            AffectedInstruments = instruments;
            AffectedPercussions = percussions;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.Channel) return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).Command != ChannelCommand.ProgramChange)
                return FilterAffectionType.MessageDoesNotFit;

            if (((ChannelMessage) midiMessage).MidiChannel == 9)
            {
                if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                    AffectedPercussions.Contains((Percussion) ((ChannelMessage) midiMessage).Data1))
                    return FilterAffectionType.MessageFitsTypeAndValue;
                return FilterAffectionType.MessageFitsType;
            }

            if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel)
                && AffectedInstruments.Contains((Instrument) ((ChannelMessage) midiMessage).Data1))
                return FilterAffectionType.MessageFitsTypeAndValue;

            return FilterAffectionType.MessageFitsType;
        }
    }
}