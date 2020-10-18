using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using MiliumRhino5.GH_MIDI.Models;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Components
{
    /// <summary>
    ///     This component is a comfortable way to control toggles with midi channel messages.
    ///     Define on which event the toggle should be set to true or to false. It will from there on be synchronized.
    ///     It is not possible to double select a toggle from two different components
    ///     but it is possible to trigger the same value it with multiple events.
    /// </summary>
    public class SynchronizeTogglesComponent : SelectSpecificGhObjectsComponent
    {
        public SynchronizeTogglesComponent()
            : base("SynchronizeToggles", "SynchronizeToggles",
                "Synchronize boolean toggles with incoming MIDI messages.",
                "Milium", "MIDI", "GetSelectedToggles")
        {
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("62dbaeb4-0000-4d57-9818-39682393ba8e");

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MidiMessage", "M", "Encoded MIDI message", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Channel", "C", "Channel that should be synchronized with.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("CategoryValues", "CV", "Category values to use for synchronization.",
                GH_ParamAccess.list, Enumerable.Range(0, 128));
            pManager.AddIntegerParameter("OnMessageType", "OnMT",
                "Message type that should be synchronized with to set the toggle on true..",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("OnValue", "OnV",
                "Value to use for synchronization to set the toggle on true..",
                GH_ParamAccess.item, 64);
            pManager.AddIntegerParameter("OffMessageType", "OffMT",
                "Message type that should be synchronized with to set the toggle on false.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("OffValue", "OffV",
                "Value to use for synchronization to set the toggle on false.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Order", "O", "Order selected toggles in a specific way.",
                GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Run", "R", "Run component and start synchronizing the toggles",
                GH_ParamAccess.item, false);

            pManager[0].Optional = true;

            var paramChannel = (Param_Integer) pManager[1];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);

            foreach (int channelCommand in Enum.GetValues(typeof(ReorderedChannelCommand)))
            {
                ((Param_Integer) pManager[3]).AddNamedValue(((ReorderedChannelCommand) channelCommand).ToString(),
                    channelCommand);
                ((Param_Integer) pManager[5]).AddNamedValue(((ReorderedChannelCommand) channelCommand).ToString(),
                    channelCommand);
            }

            var paramOrder = (Param_Integer) pManager[7];
            foreach (int order in Enum.GetValues(typeof(OrderType)))
                paramOrder.AddNamedValue(((OrderType) order).ToString(), order);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ToggleOrder", "TO", "Final order of selected toggles", GH_ParamAccess.list);
        }

        /// <summary>
        ///     Get all selected boolean toggles instead of all selected objects
        /// </summary>
        /// <returns></returns>
        protected override IGH_DocumentObject[] GetSelectedObjects()
        {
            return OnPingDocument().SelectedObjects().Where(x => x is IGH_ActiveObject && x is GH_BooleanToggle)
                .ToArray();
        }

        /// <summary>
        ///     This checks if the midi message matches the requirements for valueOn or valueOff and sets the toggle to this state
        ///     if so.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);

            DA.DisableGapLogic();

            var ghObjectWrapper = new GH_ObjectWrapper();
            var channel = 0;
            var indices = new List<int>();
            var messageTypeOn = 0;
            var valueOn = 0;
            var messageTypeOff = 0;
            var valueOff = 0;
            var orderType = 0;
            var run = false;

            DA.GetData(0, ref ghObjectWrapper);
            DA.GetData(1, ref channel);
            DA.GetDataList(2, indices);
            DA.GetData(3, ref messageTypeOn);
            DA.GetData(4, ref valueOn);
            DA.GetData(5, ref messageTypeOff);
            DA.GetData(6, ref valueOff);
            DA.GetData(7, ref orderType);
            DA.GetData(8, ref run);

            var selectedToggles = SelectedDocumentObjects.Where(x => x is GH_BooleanToggle).Cast<GH_BooleanToggle>()
                .ToArray();

            if (selectedToggles.Length == 0)
            {
                Message = "Select \ntoggles";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "No toggles selected. Select toggles and click the GetSelectedToggle button.");
                return;
            }

            if (indices.Count < selectedToggles.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Not enough indices are provided. Only the first {indices.Count} toggles will be synchronized.");
            else if (indices.Count > selectedToggles.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Too much indices are provided. Only the first {selectedToggles.Length} indices will be synchronized.");

            var orderedToggles = new GH_BooleanToggle[0];

            switch ((OrderType) orderType)
            {
                case OrderType.CreationTime:
                    orderedToggles = selectedToggles;
                    break;

                case OrderType.Name:
                    orderedToggles = selectedToggles.OrderBy(x => x.NickName).ToArray();
                    break;

                case OrderType.CanvasXPosition:
                    orderedToggles = selectedToggles.OrderBy(x => x.Attributes.Pivot.X).ToArray();
                    break;

                case OrderType.CanvasYPosition:
                    orderedToggles = selectedToggles.OrderBy(x => x.Attributes.Pivot.Y).ToArray();
                    break;
            }

            DA.SetDataList(0, orderedToggles.Select(x => x.NickName).ToList());

            if (!run)
            {
                Message = $"{selectedToggles.Length} toggles\nselected";
                return;
            }

            Message = $"Synchronizing:\n{selectedToggles.Length} toggles";

            var midiMessage = (IMidiMessage) ghObjectWrapper.Value;
            if (midiMessage == null)
                return;

            if (midiMessage.MessageType != MessageType.Channel)
                return;

            var midiChannelMessage = (ChannelMessage) midiMessage;

            switch (midiChannelMessage.Command)
            {
                //In these cases there is no category value. Therefore only 16 different toggles can be synchronized in total with the 16 channels.
                case ChannelCommand.PitchWheel:
                case ChannelCommand.ProgramChange:
                case ChannelCommand.ChannelPressure:

                    //Check if midi message triggers ValueOn
                    if (midiChannelMessage.Command.ToString() == ((ReorderedChannelCommand) messageTypeOn).ToString() &&
                        midiChannelMessage.Data1 == valueOn)
                    {
                        orderedToggles[0].Value = true;
                        orderedToggles[0].ExpireSolution(true);
                    }
                    //Check if midi message triggers ValueOff
                    else if (midiChannelMessage.Command.ToString() ==
                             ((ReorderedChannelCommand) messageTypeOff).ToString() &&
                             midiChannelMessage.Data1 == valueOff)
                    {
                        orderedToggles[0].Value = false;
                        orderedToggles[0].ExpireSolution(true);
                    }

                    break;

                default:
                    if (indices.Contains(midiChannelMessage.Data1))
                    {
                        var index = indices.IndexOf(midiChannelMessage.Data1);
                        if (index < orderedToggles.Length)
                        {
                            //Check if midi message triggers ValueOn
                            if (midiChannelMessage.Command.ToString() ==
                                ((ReorderedChannelCommand) messageTypeOn).ToString() &&
                                midiChannelMessage.Data2 == valueOn)
                            {
                                orderedToggles[index].Value = true;
                                orderedToggles[index].ExpireSolution(true);
                            }
                            //Check if midi message triggers ValueOff
                            else if (midiChannelMessage.Command.ToString() ==
                                     ((ReorderedChannelCommand) messageTypeOff).ToString() &&
                                     midiChannelMessage.Data2 == valueOff)
                            {
                                orderedToggles[index].Value = false;
                                orderedToggles[index].ExpireSolution(true);
                            }
                        }
                    }

                    break;
            }
        }
    }
}