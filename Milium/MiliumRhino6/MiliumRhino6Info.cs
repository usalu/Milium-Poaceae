using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace MiliumRhino6
{
    public class MiliumRhino6Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "MiliumRhino6";
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
                return "MiliumRhino6 allows to synchronize Grasshopper with Midi.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("f1a27c07-6f82-49da-b39c-df4b482dc964");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Ueli Saluz";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "usaluz@outlook.de";
            }
        }
    }
}
