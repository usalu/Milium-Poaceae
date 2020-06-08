using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     A base to filter midi messages.
    /// </summary>
    public abstract class FilterMidiIn
    {
        public abstract FilterAffectionType MessageAffectionType(IMidiMessage midiMessage);
    }
}