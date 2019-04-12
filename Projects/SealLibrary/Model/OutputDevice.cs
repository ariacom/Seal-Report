//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;

namespace Seal.Model
{
    public abstract class OutputDevice : RootComponent
    {
        public virtual string FullName { get; set; }

        public abstract string Process(Report report);
        public abstract void Validate();

        [XmlIgnore]
        public string FilePath;

        public abstract void SaveToFile();
        public abstract void SaveToFile(string path);
    }
}
