using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using MiliumRhino6.GH_MIDI.Models;
using Rhino.Geometry;
using Sanford.Multimedia.Midi;

namespace MiliumRhino6.GH_MIDI.Components
{
    public class SynchronizeSlidersRelative : SelectSpecificGhObjectsComponent
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SynchronizeSlidersRelative()
            : base("SynchronizeSlidersRelative", "SyncSliderRel",
                "Synchronize sliders with 2 separate with incoming MIDI messages.",
                "Milium", "MIDI", "GetSelectedSlider")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MidiMessage", "M", "Encoded MIDI message", GH_ParamAccess.item);
            pManager.AddIntegerParameter("StepSize", "SS", "Relative step size of increment or decrement.",
                GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Channel+", "C+", "Channel of the message that triggers the incremental event.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("MessageType+", "MT+", "Message type of the message that triggers the incremental event.",
                GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("CategoryValues+", "CV+", "Category values of the message that triggers the incremental event.",
                GH_ParamAccess.list, Enumerable.Range(0, 128));
            pManager.AddIntegerParameter("Value+", "V+", "Value of the message that triggers the incremental event.",
                GH_ParamAccess.item, 127);
            pManager.AddIntegerParameter("Channel-", "C-", "Channel of the message that triggers the decremental event.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("MessageType-", "MT-", "Message type of the message that triggers the decremental event.",
                GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("CategoryValues-", "CV-", "Category values of the message that triggers the decremental event.",
                GH_ParamAccess.list, Enumerable.Range(0, 128));
            pManager.AddIntegerParameter("Value-", "V-", "Value of the message that triggers the decremental event.",
                GH_ParamAccess.item, 127);
            pManager.AddBooleanParameter("Wrap", "W", "Wrapping means that slider restarts after the maximum or minimum is reached.",
                GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Order", "O", "Order selected sliders in a specific way.",
                GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Run", "R", "Run component and start synchronizing the sliders",
                GH_ParamAccess.item, false);

            pManager[0].Optional = true;

            var paramChannel = (Param_Integer)pManager[2];
            var paramChannel2 = (Param_Integer)pManager[6];
            foreach (int channel in Enum.GetValues(typeof(Channel)))
            {
                paramChannel.AddNamedValue(((Channel)channel).ToString(), channel);
                paramChannel2.AddNamedValue(((Channel)channel).ToString(), channel);
            }

            var paramMessageType = (Param_Integer)pManager[3];
            var paramMessageType2 = (Param_Integer)pManager[7];
            foreach (int channelCommand in Enum.GetValues(typeof(ReorderedChannelCommand)))
            {
                paramMessageType.AddNamedValue(((ReorderedChannelCommand)channelCommand).ToString(), channelCommand);
                paramMessageType2.AddNamedValue(((ReorderedChannelCommand)channelCommand).ToString(), channelCommand);
            }

            var paramOrder = (Param_Integer)pManager[11];
            foreach (int order in Enum.GetValues(typeof(OrderType)))
                paramOrder.AddNamedValue(((OrderType)order).ToString(), order);
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
            var stepSize = 1;
            var channelIncrement = 0;
            var messageTypeIncrement = 0;
            var indicesIncrement = new List<int>();
            var valueIncrement = 0;
            var channelDecrement = 0;
            var messageTypeDecrement = 0;
            var indicesDecrement = new List<int>();
            var valueDecrement = 0;
            var wrap = false;
            var orderType = 0;
            var run = false;

            DA.GetData(0, ref ghObjectWrapper);
            DA.GetData(1, ref stepSize);
            DA.GetData(2, ref channelIncrement);
            DA.GetData(3, ref messageTypeIncrement);
            DA.GetDataList(4, indicesIncrement);
            DA.GetData(5, ref valueIncrement);
            DA.GetData(6, ref channelDecrement);
            DA.GetData(7, ref messageTypeDecrement);
            DA.GetDataList(8, indicesDecrement);
            DA.GetData(9, ref valueDecrement);
            DA.GetData(10, ref wrap);
            DA.GetData(11, ref orderType);
            DA.GetData(12, ref run);

            if (indicesIncrement.Count != indicesDecrement.Count)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"There are {indicesIncrement.Count} incremental indices but only {indicesDecrement.Count} decremental indices. {Math.Abs(indicesIncrement.Count- indicesDecrement.Count)} sliders can only grow in one dimension.");
            if (indicesIncrement.Count < selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Not enough indices for increment are provided. Only the first {indicesIncrement.Count} sliders will be synchronized.");
            else if (indicesIncrement.Count > selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Too much increment indices are provided. Only the first {selectedSliders.Length} indices will be synchronized.");
            if (indicesDecrement.Count < selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Not enough indices for decrement are provided. Only the first {indicesIncrement.Count} sliders will be synchronized.");
            else if (indicesDecrement.Count > selectedSliders.Length)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Too much decrement indices are provided. Only the first {selectedSliders.Length} indices will be synchronized.");

            var orderedSliders = new GH_NumberSlider[0];

            switch ((OrderType)orderType)
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

            var midiMessage = (IMidiMessage)ghObjectWrapper.Value;
            if (midiMessage == null)
                return;

            if (midiMessage.MessageType != MessageType.Channel)
                return;

            var midiChannelMessage = (ChannelMessage)midiMessage;

            if (midiChannelMessage.MidiChannel != channelIncrement || midiChannelMessage.MidiChannel != channelDecrement)
                return;

            switch (midiChannelMessage.Command)
            {
                //In these cases there is no category value. Therefore only 16 different toggles can be synchronized in total with the 16 channels.
                case ChannelCommand.PitchWheel:
                case ChannelCommand.ProgramChange:
                case ChannelCommand.ChannelPressure:

                    //Check if midi message triggers increment
                    if (midiChannelMessage.Command.ToString() == ((ReorderedChannelCommand)messageTypeIncrement).ToString() &&
                        midiChannelMessage.Data1 == valueIncrement)
                    {
                        var slider = orderedSliders[0];
                        var newValue = slider.Slider.Value + slider.Slider.Epsilon;
                        slider.Slider.Value = newValue < slider.Slider.Maximum ? newValue : slider.Slider.Minimum;
                    }
                    //Check if midi message triggers decrement
                    else if (midiChannelMessage.Command.ToString() ==
                             ((ReorderedChannelCommand)messageTypeDecrement).ToString() &&
                             midiChannelMessage.Data1 == valueDecrement)
                    {
                        var slider = orderedSliders[0];
                        var newValue = slider.Slider.Value - slider.Slider.Epsilon;
                        slider.Slider.Value = newValue < slider.Slider.Minimum ? newValue : slider.Slider.Maximum;
                    }

                    break;

                default:
                    if (indicesIncrement.Contains(midiChannelMessage.Data1))
                    {
                        var index = indicesIncrement.IndexOf(midiChannelMessage.Data1);
                        if (index < orderedSliders.Length)
                        {
                            //Check if midi message triggers increment
                            if (midiChannelMessage.Command.ToString() ==
                                ((ReorderedChannelCommand) messageTypeIncrement).ToString() &&
                                midiChannelMessage.Data2 == valueIncrement)
                            {
                                var slider = orderedSliders[index];
                                var newValue = slider.Slider.Value + slider.Slider.Epsilon;
                                slider.Slider.Value =
                                    newValue < slider.Slider.Maximum ? newValue : slider.Slider.Minimum;
                            }
                        }
                    }
                    if (indicesDecrement.Contains(midiChannelMessage.Data1))
                    {
                        var index = indicesDecrement.IndexOf(midiChannelMessage.Data1);
                        if (index < orderedSliders.Length)
                        {
                            if (midiChannelMessage.Command.ToString() ==
                                     ((ReorderedChannelCommand)messageTypeDecrement).ToString() &&
                                     midiChannelMessage.Data2 == valueDecrement)
                            {
                                var slider = orderedSliders[index];
                                var newValue = slider.Slider.Value - slider.Slider.Epsilon;
                                slider.Slider.Value = newValue > slider.Slider.Minimum ? newValue : slider.Slider.Maximum;
                            }
                        }
                    }
                    break;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("d31b7d02-ef01-4de1-a98f-65a97a284025"); }
        }
    }
}