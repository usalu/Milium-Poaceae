using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using MiliumRhino5.GH_MIDI.Models;
using MiliumRhino5.Properties;

namespace MiliumRhino5.GH_MIDI.Components.Enums
{
    /// <summary>
    ///     Creates a ValueList which let's you pick midi percussions from the enum Percussion.
    ///     Percussions are used inside program change messages.
    ///     Note that midi percussion are midi instruments on channel 10.
    ///     This channel is reserved only for percussion and replaces the default instrument enumeration list.
    /// </summary>
    public class PercussionValueList : GH_ValueList
    {
        public PercussionValueList()
        {
            Category = "Milium";
            SubCategory = "MIDI";
            Name = "Percussion";
            NickName = "Percussion";
            Description = "Select a percussion.";
            ListItems.Clear();
            foreach (int percussion in Enum.GetValues(typeof(Percussion)))
                ListItems.Add(new GH_ValueListItem(((Percussion) percussion).ToString(), percussion.ToString()));
        }

        protected override Bitmap Icon => Resources.Percussion_Icon;

        public override Guid ComponentGuid => new Guid("15ea8272-72e0-442a-b376-b8c8e3ecad29");

        public override GH_Exposure Exposure => GH_Exposure.senary;
    }
}