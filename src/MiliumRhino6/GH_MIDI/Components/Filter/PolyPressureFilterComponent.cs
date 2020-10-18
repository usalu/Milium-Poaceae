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
    ///     This component will provide a specific filter that can filter poly pressure messages under certain criteria
    ///     (pitches, values and channels).
    ///     Send this filter to the filter component to apply this specific filter to poly pressure messages.
    /// </summary>
    public class PolyPressureFilterComponent : GH_Component
    {
        public PolyPressureFilterComponent()
            : base("PolyPressureFilter", "PPFilter", "Set a filter rule for poly pressure messages.", "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.PolyPressure_Icon;

        public override Guid ComponentGuid => new Guid("c85c7226-c226-4605-84d7-ab063988cc87");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Pitches", "P", "Pitches to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Values,", "V", "Values to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", (GH_ParamAccess) 1);

            var paramPitches = (Param_Integer) pManager[0];
            foreach (int pitch in Enum.GetValues(typeof(Pitch)))
                paramPitches.AddNamedValue(((Pitch) pitch).ToString(), pitch);
            paramPitches.SetPersistentData(Enum.GetValues(typeof(Pitch)));

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
                "Poly pressure filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var pitches = new List<int>();
            var values = new List<int>();
            var channels = new List<int>();

            DA.GetDataList(0, pitches);
            DA.GetDataList(1, values);
            DA.GetDataList(2, channels);

            DA.SetData(0,
                new PolyPressureFilter(channels.ConvertAll(x => (Channel) x), pitches.ConvertAll(x => (Pitch) x),
                    values));
        }
    }
}