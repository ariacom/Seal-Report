//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using System;
using System.IO;
using System.Linq;

namespace Test
{
    /// <summary>
    /// Old Excel workbooks (XLS, BIFF) read by ExcelHelper with ExcelDataReader: recognized by their first bytes, opened as a workbook in memory
    /// (GetWorkbook, so LoadDataTableFromExcel and the tasks loading Excel files), converted to XLSX (ConvertXlsToXlsx).
    /// Data\Live Table 313 renewal grants.xls: live table 313 of the MHCLG (renewal grants by region, England, 1996/97 to 2010/11, Open Government Licence v3.0).
    /// </summary>
    [TestClass]
    public class TestExcelXls
    {
        static string _folder, _xls;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _folder = Path.Combine(Path.GetTempPath(), "SealTestExcelXls_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _xls = Path.Combine(AppContext.BaseDirectory, "Data", "Live Table 313 renewal grants.xls");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            try { Directory.Delete(_folder, true); } catch { }
        }

        [TestMethod]
        public void XlsRecognizedByItsFirstBytes()
        {
            Assert.IsTrue(ExcelHelper.IsXls(_xls));
            //an XLSX workbook, an HTML page named .xls (a link to a page instead of the file) and a missing file are not XLS
            var xlsx = Path.Combine(_folder, "book.xlsx");
            using (var workbook = new XLWorkbook()) { workbook.AddWorksheet("Sheet1").Cell(1, 1).Value = "a"; workbook.SaveAs(xlsx); }
            Assert.IsFalse(ExcelHelper.IsXls(xlsx));
            var page = Path.Combine(_folder, "page.xls");
            File.WriteAllText(page, "<!DOCTYPE html><html><head><title>Just a moment...</title></head></html>");
            Assert.IsFalse(ExcelHelper.IsXls(page));
            Assert.IsFalse(ExcelHelper.IsXls(Path.Combine(_folder, "missing.xls")));
        }

        [TestMethod]
        public void XlsOpenedAsWorkbook()
        {
            using (var workbook = ExcelHelper.GetWorkbook(_xls))
            {
                Assert.AreEqual(1, workbook.Worksheets.Count);
                var sheet = workbook.Worksheets.First();
                StringAssert.StartsWith(sheet.Cell(1, 1).GetString(), "Table 313");
                Assert.AreEqual("Financial Year", sheet.Cell(6, 1).GetString());
                Assert.AreEqual("1996/97", sheet.Cell(8, 1).GetString());
                Assert.AreEqual(73940.0, ExcelHelper.GetCellValue(sheet.Cell(8, 2)));
            }
        }

        [TestMethod]
        public void XlsLoadedInDataTable()
        {
            //data rows from row 8 (row 7 is empty), without header: the rows until the first empty row
            var table = ExcelHelper.LoadDataTableFromExcel(_xls, "", 8, 1, 0, 0, false);
            Assert.AreEqual(11, table.Columns.Count);
            Assert.AreEqual(15, table.Rows.Count, "1996/97 to 2010/11");
            Assert.AreEqual("1996/97", table.Rows[0][0].ToString());
            Assert.AreEqual("73940", table.Rows[0][1].ToString());
            Assert.AreEqual("2010/11", table.Rows[14][0].ToString());
        }

        [TestMethod]
        public void XlsConvertedToXlsx()
        {
            var xlsx = ExcelHelper.ConvertXlsToXlsx(_xls, Path.Combine(_folder, "converted.xlsx"));
            Assert.IsTrue(File.Exists(xlsx));
            Assert.IsFalse(ExcelHelper.IsXls(xlsx));
            using (var workbook = new XLWorkbook(xlsx))
            {
                var sheet = workbook.Worksheets.First();
                Assert.AreEqual("Financial Year", sheet.Cell(6, 1).GetString());
                Assert.AreEqual(73940.0, ExcelHelper.GetCellValue(sheet.Cell(8, 2)));
            }
            //a file that is not an old Excel workbook is refused
            Assert.ThrowsExactly<Exception>(() => ExcelHelper.ConvertXlsToXlsx(xlsx));
        }
    }
}
