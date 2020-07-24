using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Data;
using System.Text;


namespace Seal.Helpers
{
    /// <summary>
    /// Helper to load data tables to define No SQL table sources
    /// </summary>   
    public class DataTableLoader
    {
        /// Load a DataTable from an Excel file. A start and end row, and/or colum can be specified. If hasHeader is false, column names are automatic. 
        static public DataTable FromExcel(string excelPath, string tabName = "", int startRow = 1, int startCol = 1, int endCol = 0, int endRow = 0, bool hasHeader = true)
        {
            return ExcelHelper.LoadDataTableFromExcel(excelPath, tabName, startRow, startCol, endCol, endRow, hasHeader);
        }

        /// <summary>
        /// Returns a DataTable from a CSV file
        /// </summary>
        static public DataTable FromCSV(string csvPath, char? separator = null, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.Default;
            return ExcelHelper.LoadDataTableFromCSV(csvPath, separator, encoding);
        }

        /// <summary>
        /// Returns a DataTable from a CSV file using the Microsoft VB Parser
        /// </summary>
        static public DataTable FromCSVVBParser(string csvPath, char? separator = null, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.Default;
            return ExcelHelper.LoadDataTableFromCSVVBParser(csvPath, separator, encoding);
        }

        /// <summary>
        /// Returns a DataTable from an AdomdCommand reader (MDX Query in an OLAP Cube)
        /// </summary>
        static public DataTable FromAdomdCommand(AdomdCommand command)
        {
            AdomdDataReader dr = command.ExecuteReader();
            DataTable result = new DataTable("Data");

            // output the rows in the DataReader
            DataTable dtSchema = dr.GetSchemaTable();
            foreach (DataRow schemarow in dtSchema.Rows)
            {
                var columnName = schemarow.ItemArray[0].ToString().Replace("[", "").Replace("]", "").Replace(" ", "");
                result.Columns.Add(columnName, Type.GetType(schemarow.ItemArray[5].ToString()));
            }

            while (dr.Read())
            {
                object[] ColArray = new object[dr.FieldCount];
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    if (dr[i] != null) ColArray[i] = dr[i];
                }
                result.LoadDataRow(ColArray, true);
            }
            dr.Close();
            return result;
        }
    }
}

