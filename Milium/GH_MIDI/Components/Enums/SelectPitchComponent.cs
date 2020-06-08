using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Milium.GH_MIDI.Models;

namespace Milium.GH_MIDI.Components.Enums
{
    /// <summary>
    ///     Creates a component which let's you pick midi pitches from the enum Pitch.
    ///     Pitches are used in noteOn, noteOff and polyPressure messages.
    /// </summary>
    public class SelectPitchComponent : GH_Component
    {
        public SelectPitchComponent()
            : base("Pitch", "Pitch",
                "Select a pitch.",
                "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.NoteOn_Icon;

        public override Guid ComponentGuid => new Guid("adc57140-ac83-4a21-934d-ba9d67ec808e");

        public override GH_Exposure Exposure => GH_Exposure.senary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Note", "N", "Note", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Octave", "O", "Octave of pitch", GH_ParamAccess.item, 0);

            var paramNote = (Param_Integer) pManager[0];
            foreach (int note in Enum.GetValues(typeof(Note)))
                paramNote.AddNamedValue(((Note) note).ToString(), note);

            var paramOctave = (Param_Integer) pManager[1];
            foreach (int octave in Enum.GetValues(typeof(Octave)))
                paramOctave.AddNamedValue(((Octave) octave).ToString(), octave);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Pitch", "P", "Pitch", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var note = 0;
            var octave = 0;
            DA.GetData(0, ref note);
            DA.GetData(1, ref octave);
            DA.SetData(0, (Pitch) (octave * Enum.GetValues(typeof(Note)).Length + note));
        }

        private enum Note
        {
            C,
            CSharp,
            D,
            DSharp,
            E,
            F,
            FSharp,
            G,
            GSharp,
            A,
            ASharp,
            B
        }

        private enum Octave
        {
            OctaveNeg1,
            Octave0,
            Octave1,
            Octave2,
            Octave3,
            Octave4,
            Octave5,
            Octave6,
            Octave7,
            Octave8,
            Octave9
        }
    }
}