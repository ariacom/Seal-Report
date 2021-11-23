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

        static void fillFromBsonArray(string prefix, BsonArray docs, DataTable dt, DataRow refDataRow, string[] keyNames, string arrayName)
        {
            foreach (var doc in docs)
            {
                BsonDocument bsonDoc;
                if (!doc.IsBsonDocument)
                {
                    //Array of 1 value
                    bsonDoc = new BsonDocument(new BsonElement("value", doc.AsBsonValue));
                }
                else
                {
                    //Array of object
                    bsonDoc = doc.AsBsonDocument;
                }

                fillFromBsonDocument(prefix, bsonDoc, dt, keyNames, arrayName, null, refDataRow);
            }
        }


        static void fillFromBsonDocument(string prefix, BsonDocument doc, DataTable dt, string[] keyNames, string arrayName, DataRow currentRow = null, DataRow refDataRow = null)
        {
            var dr = currentRow ?? dt.NewRow();
            if (refDataRow != null) dr.ItemArray = refDataRow.ItemArray;
            bool processArray = false; //Process array at last to init first values of the row
            while (true)
            {
                foreach (BsonElement el in doc.Elements) 
                {
                    var colName = prefix + el.Name;
                    bool process = string.IsNullOrEmpty(arrayName) || keyNames.Contains(colName);
                    bool isDocumentArray = el.Value.IsBsonArray && el.Value.AsBsonArray.FirstOrDefault() != null && el.Value.AsBsonArray.First().IsBsonDocument; //Is an Array of Document
                    bool isSingleArray = !isDocumentArray && el.Value.IsBsonArray && colName == arrayName; //Is the Array wanted

                    if (string.IsNullOrEmpty(arrayName) && keyNames.Contains(colName))
                    {
                        //conflict of the name with a parent (e.g _id), add .
                        colName = MongoSeparator + el.Name;
                    }

                    if (!dt.Columns.Contains(colName))
                    {
                        bool isDT = true, isDouble = true, isInteger = true;

                        if (el.Value.IsBsonDocument)
                        {
                            //Sub-documents
                            fillFromBsonDocument(colName + MongoSeparator, el.Value.AsBsonDocument, dt, keyNames, arrayName, dr);
                            continue;
                        }
                        if (isDocumentArray || isSingleArray)
                        {
                            if (processArray)
                            {
                                //Duplicate rows for each value
                                fillFromBsonArray(colName == arrayName ? "" : colName + MongoSeparator, el.Value.AsBsonArray, dt, dr, keyNames, colName == arrayName ? "" : arrayName);
                            }
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
                        fillFromBsonDocument(colName + MongoSeparator, el.Value.AsBsonDocument, dt, keyNames, arrayName, dr);
                        continue;
                    }
                    else
                    {
                        if (isDocumentArray || isSingleArray)
                        {
                            if (processArray)
                            {
                                //Duplicate rows for each value
                                fillFromBsonArray(colName == arrayName ? "" : colName + MongoSeparator, el.Value.AsBsonArray, dt, dr, keyNames, colName == arrayName ? "" : arrayName);
                            }
                            continue;
                        }

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
                if (processArray || string.IsNullOrEmpty(arrayName)) break;
                processArray = true;
            }

            if (currentRow == null && string.IsNullOrEmpty(arrayName)) dt.Rows.Add(dr);
        }

        /// <summary>
        /// Convert a list of BsonDocument into a DataTable. If arrayName is set, only the Array is loaded in the table. keyNames[] allow to force a property to be loaded (use _ as separator to specify sub-objects).
        /// e.g. 
        /// new string[] { "account_id" }, "transactions"
        /// new string[] {"_id","host_host_id"}, "host_host_verifications")
        /// </summary>
        static public DataTable FromMongoDB(List<BsonDocument> collection, string[] keyNames = null, string arrayName = null)
        {
            DataTable dt = new DataTable();
            if (collection.Count == 0) return dt;

            foreach (BsonDocument doc in collection)
            {
                fillFromBsonDocument("", doc, dt, keyNames ?? new string[] { }, arrayName);
            }
            return dt;
        }
    }
}
