using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MiliumRhino5.GH_MIDI.Models;
using MiliumRhino5.GH_MIDI.Models.Filters;
using MiliumRhino5.Properties;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Components.Filter
{
    /// <summary>
    ///     This component will provide a specific filter that can filter control change messages under certain criteria
    ///     (controls, values and channels).
    ///     Send this filter to the filter component to apply this specific filter to control change messages.
    /// </summary>
    public class ControlChangeFilterComponent : GH_Component
    {
        public ControlChangeFilterComponent()
            : base("ControlChangeFilter", "CCFilter", "Set a filter rule for control change messages.", "Milium",
                "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.ControlChange_Icon;

        public override Guid ComponentGuid => new Guid("881d04de-a972-40ca-aa2d-d0fc2599b643");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Controls", "Co", "Controls to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Values,", "V", "Values to apply rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", (GH_ParamAccess) 1);

            var paramControls = (Param_Integer) pManager[0];
            foreach (int control in Enum.GetValues(typeof(ControllerType)))
                paramControls.AddNamedValue(((ControllerType) control).ToString(), control);
            paramControls.SetPersistentData(Enumerable.Range(0, 128).ToArray());

            var paramValues = (Param_Integer) pManager[1];
            paramValues.SetPersistentData(Enumerable.Range(0, 128).ToArray());

            var paramChannel = (Param_Integer) pManager[2];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filter rule", "FR",
                "Control change filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var controls = new List<int>();
            var values = new List<int>();
            var channels = new List<int>();

            DA.GetDataList(0, controls);
            DA.GetDataList(1, values);
            DA.GetDataList(2, channels);

            DA.SetData(0,
                new ControlChangeFilter(channels.ConvertAll(x => (Channel) x),
                    controls.ConvertAll(x => (ControllerType) x), values));
        }
    }
}