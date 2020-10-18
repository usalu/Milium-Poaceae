using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     Define a filter for midi noteOn, noteOf messages or both at the same time. Pitches, velocities and channels can be
    ///     specified.
    /// </summary>
    public class NoteFilter : FilterMidiIn
    {
        public List<Channel> AffectedChannels = new List<Channel>();
        public AffectedNoteMessagesType AffectedNoteMessageType;
        public List<Pitch> AffectedPitches = new List<Pitch>();
        public List<int> AffectedVelocities = new List<int>();

        public NoteFilter(
            List<Channel> channels,
            List<Pitch> pitches,
            List<int> velocities,
            AffectedNoteMessagesType noteType = AffectedNoteMessagesType.NoteOnAndNoteOff)
        {
            AffectedNoteMessageType = noteType;
            AffectedChannels = channels;
            AffectedPitches = pitches;
            AffectedVelocities = velocities;
        }

        public override FilterAffectionType MessageAffectionType(IMidiMessage midiMessage)
        {
            if (midiMessage.MessageType != MessageType.Channel) return FilterAffectionType.MessageDoesNotFit;

            bool checkValue;
            switch (AffectedNoteMessageType)
            {
                case AffectedNoteMessagesType.NoteOn:
                    checkValue = ((ChannelMessage) midiMessage).Command == ChannelCommand.NoteOn;
                    break;
                case AffectedNoteMessagesType.NoteOff:
                    checkValue = ((ChannelMessage) midiMessage).Command == ChannelCommand.NoteOff;
                    break;
                case AffectedNoteMessagesType.NoteOnAndNoteOff:
                    checkValue = ((ChannelMessage) midiMessage).Command == ChannelCommand.NoteOff ||
                                 ((ChannelMessage) midiMessage).Command == ChannelCommand.NoteOn;
                    break;
                default:
                    checkValue = false;
                    break;
            }

            if (checkValue)
            {
                if (AffectedChannels.Contains((Channel) ((ChannelMessage) midiMessage).MidiChannel) &&
                    AffectedPitches.Contains((Pitch) ((ChannelMessage) midiMessage).Data1) &&
                    AffectedVelocities.Contains(((ChannelMessage) midiMessage).Data2))
                    return FilterAffectionType.MessageFitsTypeAndValue;

                return FilterAffectionType.MessageFitsType;
            }

            return FilterAffectionType.MessageDoesNotFit;
        }
    }
}