using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI.Components.Enums
{
    /// <summary>
    ///     Creates a ValueList which let's you pick midi controllers from the enum ControllerType.
    ///     Controllers are used in control change messages.
    /// </summary>
    public class ControllerValueList : GH_ValueList
    {
        public ControllerValueList()
        {
            Category = "Milium";
            SubCategory = "MIDI";
            Name = "Controller";
            NickName = "Controller";
            Description = "Select a controller.";
            ListItems.Clear();
            foreach (int pitch in Enum.GetValues(typeof(ControllerType)))
                ListItems.Add(new GH_ValueListItem(((ControllerType) pitch).ToString(), pitch.ToString()));
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("85dfbec9-dcee-49f9-b6aa-978fa77fffb7");

        public override GH_Exposure Exposure => GH_Exposure.senary;
    }
}