//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Model
{
    /// <summary>
    /// Base class containing a GUID and a Name
    /// </summary>
    public class RootComponent : RootEditor
    {
        /// <summary>
        /// Field of the GUID property
        /// </summary>
        protected string _GUID;
        /// <summary>
        /// The unique identifier
        /// </summary>
        virtual public string GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }


        /// <summary>
        /// Field of the Name property
        /// </summary>
        protected string _name;
        /// <summary>
        /// The name
        /// </summary>
        virtual public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }

}
