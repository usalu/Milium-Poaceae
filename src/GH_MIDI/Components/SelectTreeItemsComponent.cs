using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace Milium.GH_MIDI.Components
{
    /// <summary>
    ///     This is a slightly changed version from the native select tree item component.
    ///     Main feature is that it only expires the downstream objects if the value has changed.
    ///     This component will automatically adjust the outputs according the indices.
    ///     The indices do not have to be ordered.
    ///     It will allow to get single results according to the buttons and knobs of the midi device.
    ///     Even if the midi control has an unordered and weird button or knob layout
    ///     this component will allow to work around this problem easily.
    /// </summary>
    public class SelectTreeItemsComponent : GH_Component, IGH_VariableParameterComponent
    {
        public bool[] AreOutputsExpiring = new bool[1];

        public List<int> Indices = new List<int>();
        public IGH_Goo[] SelectedItems = new IGH_Goo[1];

        public SelectTreeItemsComponent()
            : base("SelectTreeItems", "Items",
                "Select items from a data tree and get the depending amount of single outputs.\n" +
                "NOTE: Only outputs where the values change will expire.\n" +
                "All unaffected outputs will not send new data.",
                "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.SelectTreeItems_Icon;

        public override Guid ComponentGuid => new Guid("6922e903-847c-4426-a79b-52f62379f136");

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /*
         * Override Methods that are normally used for a ZUI Component. Here there will be no option to add outputs by hand.
         * The reason why I chose to inherit this Interface is that it takes care of some background for you and adds
         * the possibility to expand control if new features should be added.
         */

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject();
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("DataTree", "T", "Data tree to select items from.", GH_ParamAccess.tree);
            pManager.AddPathParameter("Path", "P", "Data tree branch path to select items from.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Indices", "i", "Item indices to select.",
                GH_ParamAccess.list, 0);
            pManager.AddBooleanParameter("WrapIndices", "W", "Wrap the indices to list bounds.", GH_ParamAccess.item,
                true);
            ((Param_StructurePath) pManager[1]).SetPersistentData(new GH_Path(0));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter($"Item{0}", "0", $"Selected Item{0} from data tree.", GH_ParamAccess.item);
        }

        protected override void PostConstructor()
        {
            base.PostConstructor();
            AreOutputsExpiring[0] = true;
            SelectedItems[0] = null;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.DisableGapLogic();

            var newIndices = new List<int>();
            GH_Structure<IGH_Goo> dataTree;
            var path = new GH_Path();
            var areIndicesWrapped = true;

            DA.GetDataTree(0, out dataTree);
            DA.GetData(1, ref path);
            DA.GetDataList(2, newIndices);
            DA.GetData(3, ref areIndicesWrapped);

            /*
             It checks not only for the same values but for the right sequence.
             If it detected a different sequence it will update the component with
             the right amount of outputs and the proper descriptions and names.
            */
            if (!Indices.SequenceEqual(newIndices))
            {
                Indices = newIndices;
                if (Params.Output.Count != Indices.Count)
                    AdjustAmountOutputParameters(Indices.Count);
                SelectedItems = Indices.Select(x => (IGH_Goo) null).ToArray();
                AreOutputsExpiring = Indices.Select(x => false).ToArray();
                RenameOutputsToIndices();
                ExpireSolution(true);
                return;
            }

            if (!dataTree.PathExists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Selected path does not exist in data tree.");
                return;
            }

            //Checks if the outputs are the same and controls if an output should expire.
            for (var i = 0; i < Indices.Count; i++)
            {
                var index = Indices[i];
                AreOutputsExpiring[i] = true;
                if (index < dataTree[path].Count)
                {
                    if (SelectedItems[i] != null && dataTree[path][index] != null)
                        if (dataTree[path][index].ScriptVariable().ToString() == SelectedItems[i].ToString())
                            AreOutputsExpiring[i] = false;
                    SelectedItems[i] = dataTree[path][index];
                }
                else if (areIndicesWrapped)
                {
                    SelectedItems[i] = dataTree[path][index % dataTree[path].Count];
                }
                else
                {
                    continue;
                }

                DA.SetData(i, SelectedItems[i]);
            }

            ExpireDownStreamObjects();
            AreOutputsExpiring = AreOutputsExpiring.Select(x => false).ToArray();
        }

        /// <summary>
        ///     Manages the proper amount of outputs. If there are outputs missing it will create some new ones.
        ///     If there are too little ones it will delete some.
        /// </summary>
        /// <param name="count">Number of new outputs</param>
        private void AdjustAmountOutputParameters(int count)
        {
            if (Params.Output.Count == count) return;

            if (Params.Output.Count < count)
                for (var i = Params.Output.Count; i < count; i++)
                    Params.RegisterOutputParam(CreateParameter(GH_ParameterSide.Output, i));
            else
                for (var i = Params.Output.Count; i > count; i--)
                    Params.UnregisterOutputParameter(Params.Output[i - 1]);
            Params.OnParametersChanged();
            VariableParameterMaintenance();
        }

        private void RenameOutputsToIndices()
        {
            var i = 0;
            foreach (var param in Params.Output)
            {
                if (i < Indices.Count)
                {
                    var index = Indices[i];
                    param.Name = $"Item{index}";
                    param.NickName = index.ToString();
                    param.Description = $"Selected Item{index} from data tree.";
                }

                i++;
            }
        }

        /// <summary>
        ///     Only expire the depending down stream objects that depend on outputs that have changed.
        /// </summary>
        protected override void ExpireDownStreamObjects()
        {
            var i = 0;
            foreach (var outParam in Params.Output)
            {
                if (i < AreOutputsExpiring.Length)
                    if (AreOutputsExpiring[i])
                        foreach (var recipient in outParam.Recipients)
                            recipient.ExpireSolution(true);
                i++;
            }
        }
    }
}