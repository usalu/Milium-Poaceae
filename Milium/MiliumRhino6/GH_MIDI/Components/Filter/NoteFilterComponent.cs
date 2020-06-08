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
    ///     This component will provide a specific filter that can filter noteOn, noteOff messages or both at the same time
    ///     under certain criteria (pitches, velocities and channels).
    ///     Send this filter to the filter component to apply this specific filter to noteOn, noteOff messages messages.
    /// </summary>
    public class NoteFilterComponent : GH_Component
    {
        public NoteFilterComponent()
            : base("NoteFilter", "NoteFilter", "Set a filter rule for noteOn and/or noteOff messages.", "Milium",
                "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.Note_Icon;

        public override Guid ComponentGuid => new Guid("dff300e0-2b94-4e4c-a004-88ef58c96a93");

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("NoteType", "NT", "Note type: 0 = NoteOn, 1 = NoteOff, 2 = Both.",
                GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("Pitches", "P", "Pitches to apply the rule to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Velocities,", "V", "Velocities to apply the rule to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Channels", "C", "Channels to apply the rule to.", GH_ParamAccess.list);

            var paramNoteType = (Param_Integer) pManager[0];
            foreach (int noteType in Enum.GetValues(typeof(AffectedNoteMessagesType)))
                paramNoteType.AddNamedValue(((AffectedNoteMessagesType) noteType).ToString(), noteType);

            var paramPitches = (Param_Integer) pManager[1];
            foreach (int pitch in Enum.GetValues(typeof(Pitch)))
                paramPitches.AddNamedValue(((Pitch) pitch).ToString(), pitch);
            paramPitches.SetPersistentData(Enum.GetValues(typeof(Pitch)));

            var paramVelocities = (Param_Integer) pManager[2];
            paramVelocities.SetPersistentData(Enumerable.Range(0, 128).ToArray());

            var paramChannel = (Param_Integer) pManager[3];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);
            paramChannel.SetPersistentData(Enum.GetValues(typeof(Channel)));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filter rule", "FR",
                "Note on/off filter rule. Can be inserted to filter component.", 0);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var affectedNoteMessagesType = 2;
            var pitches = new List<int>();
            var velocities = new List<int>();
            var channels = new List<int>();

            DA.GetData(0, ref affectedNoteMessagesType);
            DA.GetDataList(1, pitches);
            DA.GetDataList(2, velocities);
            DA.GetDataList(3, channels);

            Message = ((AffectedNoteMessagesType) affectedNoteMessagesType).ToString();

            DA.SetData(0,
                new NoteFilter(channels.ConvertAll(x => (Channel) x), pitches.ConvertAll(x => (Pitch) x), velocities,
                    (AffectedNoteMessagesType) affectedNoteMessagesType));
        }
    }
}