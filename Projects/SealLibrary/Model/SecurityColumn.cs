using DynamicTypeDescriptor;
using Seal.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;

namespace Seal.Model
{
    public class SecurityColumn : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Tag").SetIsBrowsable(true);
                GetProperty("Category").SetIsBrowsable(true);
                GetProperty("Rights").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _tag = "";
        [Category("Definition"), DisplayName("\tSecurity Tag"), Description("The name of the security tag (must match with the tags defined in the columns)."), Id(1,1)]
        public string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        string _category = "";
        [Category("Definition"), DisplayName("\tCategory"), Description("The name of the category (must match with categories defined in the columns)."), Id(2, 1)]
        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }

        ColumnRight _right = ColumnRight.Edit;
        [Category("Rights"), DisplayName("\tColumn Rights"), Description("The right applied for the columns having this security tag or this category."), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnRight Rights
        {
            get { return _right; }
            set {
                _right = value;
            }
        }
    }
}
