namespace Milium.GH_MIDI.Models.Filters
{
    /// <summary>
    ///     I needed three different results in order to get my desired effect. If the type doesn't fit it will let it through.
    ///     Same if everything fits.
    ///     Only if the type fits but the values do not should obviously exclude the message.
    /// </summary>
    public enum AffectedNoteMessagesType
    {
        NoteOn,
        NoteOff,
        NoteOnAndNoteOff
    }
}