using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MiliumRhino6.GH_MIDI.Models;
using MiliumRhino6.Properties;

namespace MiliumRhino6.GH_MIDI.Components.Enums
{
    /// <summary>
    ///     Creates a component which let's you pick midi instruments from the enum Instrument.
    ///     Instruments are used in program change messages.
    /// </summary>
    public class SelectInstrumentComponent : GH_Component
    {
        public SelectInstrumentComponent()
            : base("Instrument", "Instrument",
                "Select an instrument.",
                "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.ProgramChange_Icon;

        public override Guid ComponentGuid => new Guid("dd5d5b3e-18c2-444e-8465-7193d2012f36");

        public override GH_Exposure Exposure => GH_Exposure.senary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Category", "C", "Category for instrument", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Index", "i", "Index from category", GH_ParamAccess.item, 0);

            var paramInstrumentCategory = (Param_Integer) pManager[0];
            var i = 0;
            foreach (var instrumentCategory in Enum.GetNames(typeof(InstrumentCategory)))
            {
                paramInstrumentCategory.AddNamedValue(instrumentCategory, i);
                i++;
            }

            var paramIndex = (Param_Integer) pManager[1];
            foreach (var index in Enumerable.Range(0, 8))
                paramIndex.AddNamedValue($"Index {index}", index);
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Instrument", "I", "Instrument", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var category = 0;
            var index = 0;
            DA.GetData(0, ref category);
            DA.GetData(1, ref index);
            DA.SetData(0, (Instrument) (category * 8 + index));
        }
    }
}