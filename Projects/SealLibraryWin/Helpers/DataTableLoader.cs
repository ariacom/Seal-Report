using Microsoft.AnalysisServices.AdomdClient;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;


namespace Seal.Helpers
{
    /// <summary>
    /// Helper to load data tables to define No SQL table sources
    /// </summary>   
    public class DataTableLoader
    {
        public const string MongoSeparator = ".";

        /// Load a DataTable from an Excel file. A start and end row, and/or colum can be specified. If hasHeader is false, column names are automatic. 
        static public DataTable FromExcel(string excelPath, string tabName = "", int startRow = 1, int startCol = 1, int endCol = 0, int endRow = 0, bool hasHeader = true)
        {
            return ExcelHelper.LoadDataTableFromExcel(excelPath, tabName, startRow, startCol, endCol, endRow, hasHeader);
        }

        /// <summary>
        /// Returns a DataTable from a CSV file
        /// </summary>
        static public DataTable FromCSV(string csvPath, char? separator = null, Encoding encoding = null, bool noHeader = false)
        {
            if (encoding == null) encoding = Encoding.Default;
            return ExcelHelper.LoadDataTableFromCSV(csvPath, separator, encoding, noHeader);
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

        static void fillFromBsonDocument(string prefix, BsonDocument doc, DataTable dt, string[] ignoreNames, DataRow currentRow = null, DataRow refDataRow = null)
        {
            var dr = currentRow ?? dt.NewRow();
            if (refDataRow != null) dr.ItemArray = refDataRow.ItemArray;
            foreach (BsonElement el in doc.Elements)
            {
                var colName = prefix + el.Name;
                if (ignoreNames.Contains(colName)) continue;

                bool isArrayOfDocuments = el.Value.IsBsonArray && el.Value.AsBsonArray.FirstOrDefault() != null && el.Value.AsBsonArray.First().IsBsonDocument; //Is an Array of Document
                bool isSingleArray = !isArrayOfDocuments && el.Value.IsBsonArray;

                bool process = !isArrayOfDocuments && !isSingleArray;

                if (!dt.Columns.Contains(colName))
                {
                    bool isDT = true, isDouble = true, isInteger = true;

                    if (el.Value.IsBsonDocument)
                    {
                        //Sub-documents
                        fillFromBsonDocument(colName + MongoSeparator, el.Value.AsBsonDocument, dt, ignoreNames, dr);
                        continue;
                    }

                    if (!el.Value.IsValidDateTime) isDT = false;
                    if (!el.Value.IsDouble) isDouble = false;
                    if (!el.Value.IsInt32) isInteger = false;

                    var dc = new DataColumn(colName);
                    if (isDT) dc.DataType = typeof(DateTime);
                    else if (isDouble) dc.DataType = typeof(double);
                    else if (isInteger) dc.DataType = typeof(int);

                    if (process) dt.Columns.Add(dc);
                }

                //Values
                if (el.Value.IsBsonDocument && process)
                {
                    //Sub-documents
                    fillFromBsonDocument(colName + MongoSeparator, el.Value.AsBsonDocument, dt, ignoreNames, dr);
                    continue;
                }
                else
                {
                    if (process)
                    {
                        try
                        {
                            string val = el.Value.ToString();
                            if (el.Value.IsBsonArray && val.Length > 2) val = val.Substring(1, val.Length - 2);

                            dr[colName] = el.Value;
                        }
                        catch
                        {
                            dr[colName] = DBNull.Value;
                        }
                    }
                }
            }

            if (currentRow == null) dt.Rows.Add(dr);
        }

        /// <summary>
        /// Convert a list of BsonDocument into a DataTable. ignoreNames allow to skip some columns.
        /// </summary>
        static public DataTable FromMongoDB(List<BsonDocument> collection, string[] ignoreNames = null)
        {
            DataTable dt = new DataTable();
            if (collection.Count == 0) return dt;

            foreach (BsonDocument doc in collection)
            {
                fillFromBsonDocument("", doc, dt, ignoreNames ?? new string[] { });
            }
            return dt;
        }
    }
}
