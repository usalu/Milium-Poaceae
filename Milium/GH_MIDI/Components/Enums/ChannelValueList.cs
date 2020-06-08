using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Milium.GH_MIDI.Models;

namespace Milium.GH_MIDI.Components.Enums
{
    /// <summary>
    ///     Creates a ValueList which let's you pick midi channels from the enum Channel.
    ///     Channels are used in all channel messages.
    /// </summary>
    public class ChannelValueList : GH_ValueList
    {
        public ChannelValueList()
        {
            Category = "Milium";
            SubCategory = "MIDI";
            Name = "Channel";
            NickName = "Channel";
            Description = "Select a channel.";
            ListItems.Clear();
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                ListItems.Add(new GH_ValueListItem(((Channel) channel).ToString(), channel.ToString()));
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("2e035551-3b67-40ff-8623-6efe1235eeee");

        public override GH_Exposure Exposure => GH_Exposure.senary;
    }
}