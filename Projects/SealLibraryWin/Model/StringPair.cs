using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;

namespace Seal.Model
{
    /// <summary>
    /// Serializable pair of a key and a value string
    /// </summary>
    [Serializable]
    public class StringPair
    {
        /// <summary>
        /// Key of the pair
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Value of the pair
        /// </summary>
        public string Value { get; set; }

        // Parameterless constructor for serialization
        /// <summary>
        /// Parameterless constructor for serialization
        /// </summary>
        public StringPair() { }

        /// <summary>
        /// Constructor with the key and the value
        /// </summary>
        public StringPair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Returns the pair as 'Key: Value'
        /// </summary>
        public override string ToString()
        {
            return $"{Key}: {Value}";
        }
    }
}
