using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using MiliumRhino6.GH_MIDI.Models;
using MiliumRhino6.GH_MIDI.Models.Filters;
using MiliumRhino6.Properties;
using Rhino.Geometry;

namespace MiliumRhino6.GH_MIDI.Components.Filter
{
    /// <summary>
    ///     This component will provide a specific filter that can filter pitch bend messages under certain criteria (values
    ///     and channels).
    ///     Send this filter to the filter component to apply this specific filter to pitch bend messages.
    /// </summary>
    public class PitchBendFilterComponent : GH_Component
    {
        public PitchBendFilterComponent()
            : base("PitchBendFilter", "PBFilter", "Set a filter rule for pitch bend messages.", "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.PitchBend_Icon;

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        public override Guid ComponentGuid => new Guid("3b359ed1-3611-4dd3-9474-ed82995aec8c");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntervalParameter("ValueDomains,", "VD", "Value domains to apply the rule to.",
                (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", (GH_ParamAccess) 1);

            var paramInterval = (Param_Interval) pManager[0];
            var ghIntervalList = new List<GH_Interval>();
            ghIntervalList.Add(new GH_Interval(new Interval(0.0, 8190.0)));
            ghIntervalList.Add(new GH_Interval(new Interval(8192.0, 16383.0)));
            paramInterval.SetPersistentData(ghIntervalList);

            var paramChannel = (Param_Integer) pManager[1];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filter rule", "FR",
                "PitchBend filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var intervals = new List<Interval>();
            var channels = new List<int>();
            DA.GetDataList(0, intervals);
            DA.GetDataList(1, channels);
            DA.SetData(0, new PitchBendFilter(channels.ConvertAll(x => (Channel) x), intervals));
        }
    }
}