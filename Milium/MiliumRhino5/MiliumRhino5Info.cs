using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Milium
{
    public class MiliumRhino5Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Milium";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Milium allows to synchronize Grasshopper with Midi.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("fc1d5877-ba79-4f56-9fd8-a48e655acca8");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Hewlett-Packard";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
