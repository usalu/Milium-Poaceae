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
    ///     This component is a comfortable way to control sliders with midi channel messages.
    ///     Define whith which category value (pitch or control) you want synchronize a slider.
    ///     The intensity value (e.g. velocities for note messages) will be remapped to the slider domain.
    ///     It is not possible to double select a slider from two different components
    ///     but it is possible to trigger the same value it with multiple events.
    /// </summary>
    public class SynchronizeSlidersComponent : SelectSpecificGhObjectsComponent
    {
        public SynchronizeSlidersComponent()
            : base("SynchronizeSliders", "SyncSlider",
                "Synchronize sliders with incoming MIDI messages.",
                "Milium", "MIDI", "GetSelectedSlider")
        {
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("8ba99e65-e4e8-4695-9d8a-59e2ec19b67e");

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MidiMessage", "M", "Encoded MIDI message", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Channel", "C", "Channel that should be synchronized with.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("MessageType", "MT", "Message type that should be synchronized with.",
                GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("CategoryValues", "CV", "Category values to use for synchronization.",
                GH_ParamAccess.list, Enumerable.Range(0, 128));
            pManager.AddIntegerParameter("Order", "O", "Order selected sliders in a specific way.",
                GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Run", "R", "Run component and start synchronizing the sliders",
                GH_ParamAccess.item, false);

            pManager[0].Optional = true;

            var paramChannel = (Param_Integer) pManager[1];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
                paramChannel.AddNamedValue(((Channel) channel).ToString(), channel);

            var paramMessageType = (Param_Integer) pManager[2];
            foreach (int channelCommand in Enum.GetValues(typeof(ReorderedChannelCommand)))
                paramMessageType.AddNamedValue(((ReorderedChannelCommand) channelCommand).ToString(), channelCommand);

            var paramOrder = (Param_Integer) pManager[4];
            foreach (int order in Enum.GetValues(typeof(OrderType)))
                paramOrder.AddNamedValue(((OrderType) order).ToString(), order);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("SliderOrder", "SO", "Final order of selected sliders", GH_ParamAccess.list);
        }

        /// <summary>
        ///     Get all selected number sliders instead of all selected objects
        /// </summary>
        /// <returns></returns>
        protected override IGH_DocumentObject[] GetSelectedObjects()
        {
            return OnPingDocument().SelectedObjects().Where(x => x is IGH_ActiveObject && x is GH_NumberSlider)
                .ToArray();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);

            DA.DisableGapLogic();

            var selectedSliders = SelectedDocumentObjects.Where(x => x is GH_NumberSlider).Cast<GH_NumberSlider>()
                .ToArray();

            if (selectedSliders.Length == 0)
            {
                Message = "Select \nsliders";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "No sliders selected. Select sliders and click the GetSelectedSlider button.");
                return;
            }

            var ghObjectWrapper = new GH_ObjectWrapper();
            var messageType = 0;
            var channel = 0;
            var indices = new List<int>();
            var orderType = 0;
            var run = false;

            DA.GetData(0, ref ghObjectWrapper);
            DA.GetData(1, ref channel);
            DA.GetData(2, ref messageType);
            DA.GetDataList(3, indices);
            DA.GetData(4, ref orderType);
            DA.GetData(5, ref run);

            if (indices.Count < selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Not enough indices are provided. Only the first {indices.Count} sliders will be synchronized.");
            else if (indices.Count > selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Too much indices are provided. Only the first {selectedSliders.Length} indices will be synchronized.");

            var orderedSliders = new GH_NumberSlider[0];

            switch ((OrderType) orderType)
            {
                case OrderType.CreationTime:
                    orderedSliders = selectedSliders;
                    break;

                case OrderType.Name:
                    orderedSliders = selectedSliders.OrderBy(x => x.ImpliedNickName).ToArray();
                    break;

                case OrderType.CanvasXPosition:
                    orderedSliders = selectedSliders.OrderBy(x => x.Attributes.Pivot.X).ToArray();
                    break;

                case OrderType.CanvasYPosition:
                    orderedSliders = selectedSliders.OrderBy(x => x.Attributes.Pivot.Y).ToArray();
                    break;
            }

            DA.SetDataList(0, orderedSliders.Select(x => x.ImpliedNickName).ToList());

            if (!run)
            {
                Message = $"{selectedSliders.Length} sliders\nselected";
                return;
            }

            Message = $"Synchronizing:\n{orderedSliders.Length} sliders";

            var midiMessage = (IMidiMessage) ghObjectWrapper.Value;
            if (midiMessage == null)
                return;

            if (midiMessage.MessageType != MessageType.Channel)
                return;

            var midiChannelMessage = (ChannelMessage) midiMessage;

            if (midiChannelMessage.MidiChannel != channel)
                return;

            switch (midiChannelMessage.Command)
            {
                //In these cases there is no category value. Therefore only 16 different sliders can be synchronized in total with the 16 channels.
                case ChannelCommand.PitchWheel:
                case ChannelCommand.ProgramChange:
                case ChannelCommand.ChannelPressure:
                    if (midiChannelMessage.Command.ToString() == ((ReorderedChannelCommand) messageType).ToString()
                        && indices.Contains(0))
                    {
                        var slider = orderedSliders[0];
                        slider.Slider.NormalizedValue = NormalizeChannelMessageValue(midiChannelMessage);
                        slider.Slider.FixValue();
                    }

                    break;

                default:
                    if (indices.Contains(midiChannelMessage.Data1)
                        && midiChannelMessage.Command.ToString() == ((ReorderedChannelCommand) messageType).ToString())
                    {
                        var index = indices.IndexOf(midiChannelMessage.Data1);
                        if (index < orderedSliders.Length)
                        {
                            var slider = orderedSliders[index];
                            slider.Slider.NormalizedValue = NormalizeChannelMessageValue(midiChannelMessage);
                            slider.Slider.FixValue();
                        }
                    }

                    break;
            }
        }

        /// <summary>
        ///     Normalize the value of a channel midi message to an interval of 0-1.
        ///     In cases where there is no intensity value, the category value gets normalized.
        /// </summary>
        /// <param name="message">Channel message to extract value from</param>
        /// <returns>Normalized value</returns>
        public float NormalizeChannelMessageValue(ChannelMessage message)
        {
            switch (message.Command)
            {
                case ChannelCommand.ProgramChange:
                case ChannelCommand.ChannelPressure:
                    return message.Data1 / 127f;

                case ChannelCommand.PitchWheel:
                    return message.Data1 / 16383f;

                default:
                    return message.Data2 / 127f;
            }
        }
    }
}