using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seal.Model
{
    public class EditorColumnDefinition
    {
        public string Name = "";
        public bool IsHidden = false;
        public bool IsReadOnly = false;
        public bool CanBeEmpty = true;
        public string Type = "";
        public string BlobColumnName = "";
        public string DefaultValue = "";
        public bool SkipSQL = false;
        public string FinalEditorCol = ""; 
        public string FinalTableCol = "";

        public string GetType(ReportElement element)
        {
            var result = Type;
            if (string.IsNullOrEmpty(result))
            {
                if (!string.IsNullOrEmpty(BlobColumnName)) result = "upload";
                else if (element.IsDateTime) result = "datetime";
                else if (element.IsNumeric) result = "";
            }
            return result;
        }
    }

    public class EditorTableDefinition
    {
        public delegate void CustomFieldValidator(dynamic editor, ReportElement element, object value);
        public delegate void CustomMainValidator(dynamic editor, dynamic record);

        public string PkName = "";
        public string AddWhereClause = "";

        public string SPInsert = "";
        public string SPUpdate = "";
        public string SPDelete = "";

        public bool CanInsert = true;
        public bool CanUpdate = true;
        public bool CanDelete = true;

        public string TemplateName = "";

        public bool ReadOnlyByDefault = true;

        public List<EditorColumnDefinition> Cols = new List<EditorColumnDefinition>();
        public Dictionary<string, string> InsertExtraColumnValues = new Dictionary<string, string>();
        public Dictionary<string, string> UpdateExtraColumnValues = new Dictionary<string, string>();

        public EditorColumnDefinition GetColumnDefinition(string colName)
        {
            var result = Cols.FirstOrDefault(i => i.Name == colName);
            if (result == null)
            {
                result = new EditorColumnDefinition() { IsReadOnly = ReadOnlyByDefault };
                Cols.Add(result);
            }
            return result;
        }

        public CustomFieldValidator FieldValidator = null;
        public CustomMainValidator MainValidator = null;

        void initValidator()
        {
            FieldValidator = new CustomFieldValidator(delegate (dynamic editor, ReportElement element, object value)
            {
                if (element.MetaColumn.ColumnName == "aColName")
                {
                    double? d = value as double?;
                    if (d == null || d <= 0) editor.AddError(element.MetaColumn.ColumnName, "Value must be > 0");
                }
            });

            MainValidator = new CustomMainValidator(delegate(dynamic editor, dynamic record)
            {
            });

        }
    }
}

