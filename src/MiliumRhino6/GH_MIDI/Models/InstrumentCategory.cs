namespace MiliumRhino6.GH_MIDI.Models
{
    /// <summary>
    ///     This enums are not used in the moment. I thought about using them inside the program change filter component with
    ///     indices from 1-8.
    ///     In the end I thought it was just being more complicated. The individual instruments wouldn't have been displayed.
    /// </summary>
    public enum InstrumentCategory
    {
        Piano,
        ChromaticPercussion = 8,
        Organ = 16,
        Guitar = 24,
        Bass = 32,
        Strings = 40,
        Ensemble = 48,
        Brass = 56,
        Reed = 64,
        Pipe = 72,
        SynthLead = 80,
        SynthPad = 88,
        SynthEffects = 96,
        Ethnic = 104,
        Percussive = 112,
        SoundEffects = 120
    }
}