//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

namespace Seal.Model
{
    public class ServerOptions
    {
        public static bool HasEditor()
        {
#if EDITOR
            return true;
#else
            return false;
#endif
        }

        public static bool HasMinifiedScripts()
        {
#if MINIFIED
            return true;
#else
            return false;
#endif
        }
    }
}