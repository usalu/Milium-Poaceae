using System;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MiliumRhino5.Properties;
using Sanford.Multimedia.Midi;

namespace MiliumRhino5.GH_MIDI.Components
{
    /// <summary>
    ///     Store all possible midi values from midi channel messages.
    ///     The branch path represents the channel. {0} = Channel 1
    ///     The index of a list represents the category value. index 75 in a pitch message = pitch 75 (DSharp5).
    ///     NOTE: Only outputs where the values change will expire downstream objects that depend on that output.
    ///     All unaffected outputs will remain unexpired.
    /// </summary>
    public class StoreMidiMessagesComponent : GH_Component
    {
        public DataTree<double?> ChannelPressureValues = new DataTree<double?>();
        public DataTree<double?> ControlValues = new DataTree<double?>();
        private Expire _expire;
        public DataTree<double?> Instruments = new DataTree<double?>();

        public ChannelMessage MidiMessage;

        public DataTree<double?> NoteOffVelocities = new DataTree<double?>();

        /// <summary>
        ///     Persistent data trees that stores channel MIDI messages.
        /// </summary>
        public DataTree<double?> NoteOnVelocities = new DataTree<double?>();

        public DataTree<double?> PitchBendValues = new DataTree<double?>();
        public DataTree<double?> PolyPressureValues = new DataTree<double?>();
        public bool Remap;
        public bool ReplaceNulls;
        public bool Reset;

        public StoreMidiMessagesComponent()
            : base("StoreMIDIMessages", "StoreMIDIMessages",
                "Store encoded MIDI messages according to their Channel and Category.\n" +
                "The channel is represented in the path and the category is represented in the index of the list.\n" +
                "Example: {2}(75) 120 in NoteOnVelocity means: channel3, pitch 75 (DSharp5) and velocity of 120.\n" +
                "NOTE: Only outputs where the values change will expire.\n" +
                " All unaffected outputs will not send new data.", "Milium", "MIDI")
        {
        }

        protected override Bitmap Icon => Resources.Store_Icon;

        public override Guid ComponentGuid => new Guid("5815b3bc-d728-4a27-9724-76b905f88347");

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("MIDIMessage", "M", "Encoded MIDI message", 0);
            pManager[0].Optional = true;
            pManager.AddBooleanParameter("Remap", "RM", "Normalize values.", 0, false);
            pManager.AddBooleanParameter("ReplaceNulls", "RN", "Replace nulls with zeros.", 0, false);
            pManager.AddBooleanParameter("Reset", "RS", "Reset the stored values.", 0, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("NoteOnVelocity", "NOnV", "Velocities of incoming note on messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("NoteOffVelocity", "NOffV", "Velocities of incoming note off messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("ControlValue", "CoV", "Value of incoming control change messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("PitchBendValue", "PBV", "Value of incoming pitch bend messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("Instrument", "I", "Instrument of incoming program change messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("ChannelPressureValue", "ChPV", "Value of incoming channel pressure messages.",
                (GH_ParamAccess) 2);
            pManager.AddNumberParameter("PolyPressureValue", "PPV", "Value of incoming poly pressure messages.",
                (GH_ParamAccess) 2);
        }

        private void InitalizeTrees()
        {
            for (var index = 0; index < 16; ++index)
            {
                NoteOnVelocities.AddRange(Enumerable.Repeat(new double?(), 128), new GH_Path(index));
                NoteOffVelocities.AddRange(Enumerable.Repeat(new double?(), 128), new GH_Path(index));
                ControlValues.AddRange(Enumerable.Repeat(new double?(), 128), new GH_Path(index));
                PitchBendValues.Add(null, new GH_Path(index));
                Instruments.Add(null, new GH_Path(index));
                ChannelPressureValues.Add(null, new GH_Path(index));
                PolyPressureValues.AddRange(Enumerable.Repeat(new double?(), 128), new GH_Path(index));
            }
        }

        public DataTree<double?> RemapDataTree(DataTree<double?> dataTree, float domainEnd)
        {
            var remappeDataTree = new DataTree<double?>();
            foreach (var path in dataTree.Paths)
                remappeDataTree.AddRange(dataTree.Branch(path).Select(x =>
                {
                    if (x.HasValue) return x / domainEnd;
                    return null;
                }), path);
            return remappeDataTree;
        }

        public DataTree<double?> ReplaceNullsWithZeros(DataTree<double?> dataTree)
        {
            var replacedDataTree = new DataTree<double?>();
            foreach (var path in dataTree.Paths)
            foreach (var item in dataTree.Branch(path))
                if (item == null) replacedDataTree.Add(0, path);
                else replacedDataTree.Add(item, path);
            return replacedDataTree;
        }

        protected override void PostConstructor()
        {
            base.PostConstructor();
            InitalizeTrees();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //For the (almost any) custom class there is a wrapper needed inside of Grasshopper in order to transfer the object between components.
            //Otherwise Grasshopper tries to cast it to a generic type.
            var ghObjectWrapper = new GH_ObjectWrapper();

            var newRemap = false;
            var newReplaceNulls = false;
            var newReset = false;

            DA.GetData(0, ref ghObjectWrapper);
            DA.GetData(1, ref newRemap);
            DA.GetData(2, ref newReplaceNulls);
            DA.GetData(3, ref newReset);

            MidiMessage = (ChannelMessage) ghObjectWrapper.Value;

            if (newReset != Reset || newRemap != Remap || newReplaceNulls != ReplaceNulls)
            {
                Reset = newReset;
                Remap = newRemap;
                ReplaceNulls = newReplaceNulls;
                _expire = Expire.FullExpire;
            }

            else if (MidiMessage == null)
            {
                _expire = Expire.NoExpire;
            }

            else
            {
                _expire = Expire.PartExpire;
            }

            if (Reset)
            {
                NoteOnVelocities.ClearData();
                NoteOffVelocities.ClearData();
                ControlValues.ClearData();
                PitchBendValues.ClearData();
                Instruments.ClearData();
                ChannelPressureValues.ClearData();
                PolyPressureValues.ClearData();
                InitalizeTrees();
            }

            StoreMessageValue();

            if (Remap & ReplaceNulls)
            {
                DA.SetDataTree(0, ReplaceNullsWithZeros(RemapDataTree(NoteOnVelocities, 127f)));
                DA.SetDataTree(1, ReplaceNullsWithZeros(RemapDataTree(NoteOffVelocities, 127f)));
                DA.SetDataTree(2, ReplaceNullsWithZeros(RemapDataTree(ControlValues, 127f)));
                DA.SetDataTree(3, ReplaceNullsWithZeros(RemapDataTree(PitchBendValues, 16383f)));
                DA.SetDataTree(4, ReplaceNullsWithZeros(RemapDataTree(Instruments, 127f)));
                DA.SetDataTree(5, ReplaceNullsWithZeros(RemapDataTree(ChannelPressureValues, 127f)));
                DA.SetDataTree(6, ReplaceNullsWithZeros(RemapDataTree(PolyPressureValues, 127f)));
            }
            else if (Remap)
            {
                DA.SetDataTree(0, RemapDataTree(NoteOnVelocities, 127f));
                DA.SetDataTree(1, RemapDataTree(NoteOffVelocities, 127f));
                DA.SetDataTree(2, RemapDataTree(ControlValues, 127f));
                DA.SetDataTree(3, RemapDataTree(PitchBendValues, 16383f));
                DA.SetDataTree(4, RemapDataTree(Instruments, 127f));
                DA.SetDataTree(5, RemapDataTree(ChannelPressureValues, 127f));
                DA.SetDataTree(6, RemapDataTree(PolyPressureValues, 127f));
            }
            else if (ReplaceNulls)
            {
                DA.SetDataTree(0, ReplaceNullsWithZeros(NoteOnVelocities));
                DA.SetDataTree(1, ReplaceNullsWithZeros(NoteOffVelocities));
                DA.SetDataTree(2, ReplaceNullsWithZeros(ControlValues));
                DA.SetDataTree(3, ReplaceNullsWithZeros(PitchBendValues));
                DA.SetDataTree(4, ReplaceNullsWithZeros(Instruments));
                DA.SetDataTree(5, ReplaceNullsWithZeros(ChannelPressureValues));
                DA.SetDataTree(6, ReplaceNullsWithZeros(PolyPressureValues));
            }
            else
            {
                DA.SetDataTree(0, NoteOnVelocities);
                DA.SetDataTree(1, NoteOffVelocities);
                DA.SetDataTree(2, ControlValues);
                DA.SetDataTree(3, PitchBendValues);
                DA.SetDataTree(4, Instruments);
                DA.SetDataTree(5, ChannelPressureValues);
                DA.SetDataTree(6, PolyPressureValues);
            }

            /* Overrides the default way of the way that ExpireDonwStreamObjects works.
            Normally it gets triggered in the beginning which means the result wouldnt be stored yet.
            Instead it will be triggered after the Solution completes.
            */

            ExpireDownStreamObjects();
            _expire = Expire.NoExpire;
        }

        /// <summary>
        ///     Stores incoming channel MIDI message into a DataTree.<br />
        ///     The GH_Path represents the channel.<br />
        ///     The pitches and controllers are represented by the indices of the list.<br />
        ///     The values are stored as the final item.
        /// </summary>
        public void StoreMessageValue()
        {
            if (MidiMessage == null) return;

            switch (MidiMessage.Command)
            {
                case ChannelCommand.NoteOn:
                {
                    NoteOnVelocities[new GH_Path(MidiMessage.MidiChannel), MidiMessage.Data1] = MidiMessage.Data2;
                    break;
                }
                case ChannelCommand.NoteOff:
                {
                    NoteOffVelocities[new GH_Path(MidiMessage.MidiChannel), MidiMessage.Data1] = MidiMessage.Data2;
                    break;
                }
                case ChannelCommand.Controller:
                {
                    ControlValues[new GH_Path(MidiMessage.MidiChannel), MidiMessage.Data1] = MidiMessage.Data2;
                    break;
                }
                case ChannelCommand.PitchWheel:
                {
                    PitchBendValues[new GH_Path(MidiMessage.MidiChannel), 0] = MidiMessage.Data1;
                    break;
                }
                case ChannelCommand.ProgramChange:
                {
                    Instruments[new GH_Path(MidiMessage.MidiChannel), 0] = MidiMessage.Data1;
                    break;
                }
                case ChannelCommand.ChannelPressure:
                {
                    ChannelPressureValues[new GH_Path(MidiMessage.MidiChannel), MidiMessage.Data1] =
                        MidiMessage.Data2;
                    break;
                }
                case ChannelCommand.PolyPressure:
                {
                    PolyPressureValues[new GH_Path(MidiMessage.MidiChannel), MidiMessage.Data1] = MidiMessage.Data2;
                    break;
                }
            }
        }

        /* This approach tries to work around the default expiring behaviour of Grasshopper.
         Normally a new input forces the component to recompute even if the value is the same than the old one.
         Therefore all depending components would be restarted if a new midiMessage is received.
         In combination with CustomSelectTreeItems fully inert outputs are created that only expire on
         a new value. This makes it possible to even use it inside high complex documents.
        */

        /// <summary>
        ///     Expires none, one or all the downStreamObjects.<br />
        ///     It will only expireDownstreamObjects that rely on that output.<br />
        ///     None by default, one if a new midiMessage got stored.<br />
        ///     All if remap, reset or replaceNulls got changed.<br />
        /// </summary>
        protected override void ExpireDownStreamObjects()
        {
            switch (_expire)
            {
                case Expire.NoExpire:
                {
                    return;
                }
                case Expire.PartExpire:
                {
                    switch (MidiMessage.Command)
                    {
                        case ChannelCommand.NoteOn:
                        {
                            foreach (var recipient in Params.Output[0].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.NoteOff:
                        {
                            foreach (var recipient in Params.Output[1].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.Controller:
                        {
                            foreach (var recipient in Params.Output[2].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.PitchWheel:
                        {
                            foreach (var recipient in Params.Output[3].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.ProgramChange:
                        {
                            foreach (var recipient in Params.Output[4].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.ChannelPressure:
                        {
                            foreach (var recipient in Params.Output[5].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        case ChannelCommand.PolyPressure:
                        {
                            foreach (var recipient in Params.Output[6].Recipients)
                                recipient.ExpireSolution(true);
                            return;
                        }
                        default: return;
                    }
                }
                case Expire.FullExpire:
                {
                    foreach (var outParam in Params.Output)
                    foreach (var recipient in outParam.Recipients)
                        recipient.ExpireSolution(true);
                    return;
                }
                default: return;
            }
        }

        private enum Expire
        {
            NoExpire,
            PartExpire,
            FullExpire
        }
    }
}