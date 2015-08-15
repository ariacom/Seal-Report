//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
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
