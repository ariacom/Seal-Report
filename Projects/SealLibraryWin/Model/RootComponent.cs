//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Model
{
    /// <summary>
    /// Base class containing a GUID and a Name
    /// </summary>
    public class RootComponent : RootEditor
    {
        protected string _GUID;
        /// <summary>
        /// The unique identifier
        /// </summary>
        virtual public string GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }


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
