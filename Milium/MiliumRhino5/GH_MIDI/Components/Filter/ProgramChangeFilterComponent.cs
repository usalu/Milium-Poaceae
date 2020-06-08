using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MiliumRhino5.GH_MIDI.Models;
using MiliumRhino5.GH_MIDI.Models.Filters;
using MiliumRhino5.Properties;

namespace MiliumRhino5.GH_MIDI.Components.Filter
{
    /// <summary>
    ///     This component will provide a specific filter that can filter program change messages under certain criteria
    ///     (instrument, percussion and channels).
    ///     Send this filter to the filter component to apply this specific filter to program change messages.
    /// </summary>
    public class ProgramChangeFilterComponent : GH_Component
    {
        public ProgramChangeFilterComponent()
            : base("ProgramChangeFilter", "PCFilter", "Set a filter rule for program change messages.", "Milium",
                "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.ProgramChange_Icon;

        public override Guid ComponentGuid => new Guid("62e5f70f-7988-40c1-a7c3-b4abdf441bd3");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Instruments,", "I", "Instruments to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Percussions,", "P", "Percussions to apply the rule to.", (GH_ParamAccess) 1);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", (GH_ParamAccess) 1);

            var paramInstruments = (Param_Integer) pManager[0];
            foreach (int instrument in Enum.GetValues(typeof(Instrument)))
                paramInstruments.AddNamedValue(((Instrument) instrument).ToString(), instrument);
            paramInstruments.SetPersistentData(Enum.GetValues(typeof(Instrument)));

            var paramPercussions = (Param_Integer) pManager[1];
            foreach (int percussion in Enum.GetValues(typeof(Percussion)))
                paramPercussions.AddNamedValue(((Percussion) percussion).ToString(), percussion);
            paramPercussions.SetPersistentData(Enum.GetValues(typeof(Percussion)));

            var paramChannel = (Param_Integer) pManager[2];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filter rule", "FR",
                "Program change filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var instruments = new List<int>();
            var percussions = new List<int>();
            var channels = new List<int>();

            DA.GetDataList(0, instruments);
            DA.GetDataList(1, percussions);
            DA.GetDataList(2, channels);

            DA.SetData(0,
                new ProgramChangeFilter(channels.ConvertAll(x => (Channel) x),
                    instruments.ConvertAll(x => (Instrument) x), percussions.ConvertAll(x => (Percussion) x)));
        }
    }
}