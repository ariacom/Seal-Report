//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

namespace Seal.Model
{
    public class RootComponent : RootEditor
    {
        protected string _GUID;
        virtual public string GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }


        protected string _name;
        virtual public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

    }

}
