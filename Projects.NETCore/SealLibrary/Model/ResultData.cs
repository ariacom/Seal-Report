//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

namespace Seal.Model
{
    /// <summary>
    /// Arrays of ResultCell stored in the ResultPage generated after a report model execution
    /// </summary>
    public class ResultData
    {
        /// <summary>
        /// Array of ResultCell for Row
        /// </summary>
        public ResultCell[] Row;
        /// <summary>
        /// Array of ResultCell for Column
        /// </summary>
        public ResultCell[] Column;
        /// <summary>
        /// Array of ResultCell for Data
        /// </summary>
        public ResultCell[] Data;
        /// <summary>
        /// Array of ResultCell for Hidden cells (for navigation)
        /// </summary>
        public ResultCell[] Hidden;
    }
}

