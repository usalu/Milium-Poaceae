using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MiliumRhino6.GH_MIDI.Models;
using MiliumRhino6.GH_MIDI.Models.Filters;
using MiliumRhino6.Properties;

namespace MiliumRhino6.GH_MIDI.Components.Filter
{
    /// <summary>
    ///     This component will provide a specific filter that can filter channel pressure messages under certain criteria
    ///     (values and channels).
    ///     Send this filter to the filter component to apply this specific filter to channel pressure messages.
    /// </summary>
    public class ChannelPressureFilterComponent : GH_Component
    {
        public ChannelPressureFilterComponent()
            : base("ChannelPressureFilter", "CPFilter", "Set a filter rule for channel pressure messages.", "Milium",
                "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.ChannelPressure_Icon;

        public override Guid ComponentGuid => new Guid("745c6751-61b0-4e21-bb0d-b4987a3ef3f5");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Values,", "V", "Values to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", (GH_ParamAccess) 1);

            var paramValues = (Param_Integer) pManager[0];
            paramValues.SetPersistentData(Enumerable.Range(0, 128).ToArray());

            var paramChannel = (Param_Integer) pManager[1];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filter rule", "FR",
                "Channel pressure filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var values = new List<int>();
            var channels = new List<int>();

            DA.GetDataList(0, values);
            DA.GetDataList(1, channels);

            DA.SetData(0, new ChannelPressureFilter(channels.ConvertAll(x => (Channel) x), values));
        }
    }
}