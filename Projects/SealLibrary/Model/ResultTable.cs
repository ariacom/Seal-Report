//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seal.Model
{
    public class ResultTable
    {
        public int BodyStartRow = 0;
        public int BodyEndRow = 0;
        public int BodyStartColumn = 0;
        public List<ResultTotalCell> TotalCells = new List<ResultTotalCell>();

        public List<ResultCell[]> Lines = new List<ResultCell[]>();
    }

}
