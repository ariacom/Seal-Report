using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;

namespace Seal.Model
{
    [Serializable]
    public class StringPair
    {
        public string Key { get; set; }
        public string Value { get; set; }

        // Parameterless constructor for serialization
        public StringPair() { }

        public StringPair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Key}: {Value}";
        }
    }
}
