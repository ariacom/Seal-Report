//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using Seal.Helpers;
using System.Web;
using System.Globalization;
using System.IO;

namespace Seal.Model
{
    /// <summary>
    /// A ResultCell defines a cell generated in a table after the execution of a report model
    /// </summary>
    public class ResultCell
    {
        /// <summary>
        /// The object value of the cell
        /// </summary>
        public object Value;

        /// <summary>
        /// The ReportElement of the element model
        /// </summary>
        public ReportElement Element;

        /// <summary>
        /// True if the cell is for a total
        /// </summary>
        public bool IsTotal = false;

        /// <summary>
        /// True if the cell is for a title
        /// </summary>
        public bool IsTitle = false;

        /// <summary>
        /// True if the cell is for a sub total
        /// </summary>
        public bool IsSubTotal = false;

        /// <summary>
        /// True if the cell is for the total of totals
        /// </summary>
        public bool IsTotalTotal = false;

        /// <summary>
        /// True if the cell is for a serie
        /// </summary>
        public bool IsSerie = false;

        //Final Values and CSS, Class if set in the cell script

        /// <summary>
        /// If not empty, the value is used for the cell
        /// </summary>
        public string FinalValue = "";

        /// <summary>
        /// If not empty, the css style is used for the cell
        /// </summary>
        public string FinalCssStyle = "";

        /// <summary>
        /// If not empty, the css class is used for the cell
        /// </summary>
        public string FinalCssClass = "";

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        public object Tag;

        /// <summary>
        /// Default Css Class for a cell
        /// </summary>
        public static string DefaultCellCssClass = "";

        /// <summary>
        /// Default Css Class for a numeric cell
        /// </summary>
        public static string DefaultNumericCellCssClass = "text-right";

        /// <summary>
        /// Default Css Class  for a datetime cell
        /// </summary>
        public static string DefaultDateTimeCellCssClass = "text-right";

        /// <summary>
        /// Default Css Style for a cell
        /// </summary>
        public static string DefaultCellCssStyle = "";

        /// <summary>
        /// Default Css Style for a numeric cell
        /// </summary>
        public static string DefaultNumericCellCssStyle = "";

        /// <summary>
        /// Default Css Style for a datetime cell
        /// </summary>
        public static string DefaultDateTimeCellCssStyle = "";

        /// <summary>
        /// Default Css Class for a title cell
        /// </summary>
        public static string DefaultTitleCssClass = "";

        /// <summary>
        /// Default Css Class for a numeric title cell. Default value is 'text-right'
        /// </summary>
        public static string DefaultNumericTitleCssClass = "text-right";

        /// <summary>
        /// Default Css Class for a datetime title cell. Default value is 'text-right'
        /// </summary>
        public static string DefaultDateTimeTitleCssClass = "text-right";

        /// <summary>
        /// Default Css Style for a title cell
        /// </summary>
        public static string DefaultTitleCssStyle = "";

        /// <summary>
        /// Default Css Style for a numeric title cell. Default value is 'padding-right:25px;'
        /// </summary>
        public static string DefaultNumericTitleCssStyle = "padding-right:25px;";

        /// <summary>
        /// Default Css Style for a datetime title cell. Default value is 'padding-right:25px;'
        /// </summary>
        public static string DefaultDateTimeTitleCssStyle = "padding-right:25px;";

        /// <summary>
        /// HTML value of the cell
        /// </summary>
        public string HTMLValue
        {
            get
            {
                var value = (Element != null && !Element.ContainsHtml) ? HttpUtility.HtmlEncode(DisplayValue) : DisplayValue;
                return !string.IsNullOrEmpty(FinalValue) ? FinalValue : value.Replace("\r", "").Replace("\n", "<br>");
            }
        }

        /// <summary>
        /// CSV value of the cell
        /// </summary>
        public string CSVValue(bool useFormat, string separator)
        {
            return ExcelHelper.ToCsv(useFormat ? DisplayValue : RawDisplayValue, separator);
        }

        /// <summary>
        /// Display value of the cell
        /// </summary>
        public string DisplayValue
        {
            get
            {
                try
                {
                    if (Value == null) return "";
                    if (Element == null) return Value.ToString();
                    if (IsTitle) return Element.Model.Report.TranslateElement(Element, Value.ToString());
                    if (Value is IFormattable) return ((IFormattable)Value).ToString(Element.FormatEl, Element.Model.Report.ExecutionView.CultureInfo);
                }
                catch { }
                return Value.ToString();
            }
        }

        /// <summary>
        /// Display value of the cell without applying the format or translation
        /// </summary>
        public string RawDisplayValue
        {
            get
            {
                if (Value == null) return "";
                if (IsTitle) return Value.ToString();
                return Value.ToString();
            }
        }

        /// <summary>
        /// Deprecated: Kept for compatibility
        /// </summary>
        public string ValueNoHTML //Kept for backward compatibility before 5.0
        {
            get
            {
                return DisplayValue;
            }
        }

        /// <summary>
        /// Double value of the cell if possible
        /// </summary>
        public double? DoubleValue
        {
            get
            {
                if (Value == null || string.IsNullOrEmpty(Value.ToString())) return null;
                double result;
                if (double.TryParse(Value.ToString(), out result)) return result;
                return null;
            }
        }

        /// <summary>
        /// Value used for navigation
        /// </summary>
        public string NavigationValue
        {
            get
            {
                string result = RawDisplayValue;
                if (Element.IsEnum)
                {
                    MetaEV enumValue = null;
                    if (Element.EnumEL.Translate) enumValue = Element.MetaEnumValuesEL.FirstOrDefault(i => Element.Model.EnumDisplayValue(Element.EnumEL, i.Id, false) == result);
                    else enumValue = Element.MetaEnumValuesEL.FirstOrDefault(i => i.DisplayValue == result);
                    if (enumValue != null) result = enumValue.Id;
                }
                else if (Element.IsDateTime && Value is DateTime)
                {
                    result = ((DateTime)Value).ToOADate().ToString(CultureInfo.InvariantCulture);
                }
                return result;
            }
        }

        /// <summary>
        /// Values used for sub report navigation
        /// </summary>
        public List<ResultCell> SubReportValues = new List<ResultCell>();

        /// <summary>
        /// Date time value of the cell if possible
        /// </summary>
        public DateTime? DateTimeValue
        {
            get
            {
                if (Value == null || string.IsNullOrEmpty(Value.ToString()) || !(Value is DateTime)) return null;
                return (DateTime)Value;
            }
        }

        void updateFinalCssClass()
        {
            if (string.IsNullOrEmpty(FinalCssClass) && Element != null && Value != null && Element.IsEnum)
            {
                MetaEV value = Element.MetaEnumValuesEL.FirstOrDefault(i => i.DisplayValue == Value.ToString());
                if (value != null && !string.IsNullOrEmpty(value.Class)) FinalCssClass = value.Class;
            }
        }


        /// <summary>
        /// Css cell class for the summary table
        /// </summary>
        public string CellCssSummaryClass
        {
            get
            {
                updateFinalCssClass();
                if (!string.IsNullOrEmpty(FinalCssClass)) return FinalCssClass;

                string result = "";
                if (Element != null)
                {
                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssClass : DefaultCellCssClass;
                    else if (Element.IsNumeric) result = IsTitle ? DefaultNumericTitleCssClass : DefaultNumericCellCssClass;
                    else if (Element.IsDateTime) result = IsTitle ? DefaultDateTimeTitleCssClass : DefaultDateTimeCellCssClass;
                }
                return result;
            }
        }

        /// <summary>
        /// Css cell class for the page table
        /// </summary>
        public string CellCssPageClass
        {
            get
            {
                updateFinalCssClass();
                if (!string.IsNullOrEmpty(FinalCssClass)) return FinalCssClass;

                string result = "";
                if (Element != null)
                {

                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssClass : DefaultCellCssClass;
                    else if (Element.IsNumeric) result = IsTitle ? "" : DefaultNumericCellCssClass;
                    else if (Element.IsDateTime) result = IsTitle ? "" : DefaultDateTimeCellCssClass;
                }
                return result;
            }
        }


        /// <summary>
        /// Css cell class
        /// </summary>
        public string CellCssClass
        {
            get
            {
                updateFinalCssClass();
                if (!string.IsNullOrEmpty(FinalCssClass)) return FinalCssClass;

                string result = "";
                if (Element != null)
                {
                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssClass : DefaultCellCssClass;
                    else if (Element.IsNumeric) result = IsTitle ? DefaultNumericTitleCssClass : DefaultNumericCellCssClass;
                    else if (Element.IsDateTime) result = IsTitle ? DefaultDateTimeTitleCssClass : DefaultDateTimeCellCssClass;
                }
                return result;
            }
        }

        void updateFinalCssStyle()
        {
            if (string.IsNullOrEmpty(FinalCssClass) && Element != null && Value != null && Element.IsEnum)
            {
                MetaEV value = Element.MetaEnumValuesEL.FirstOrDefault(i => i.DisplayValue == Value.ToString());
                if (value != null && !string.IsNullOrEmpty(value.Css)) FinalCssStyle = value.Css;
            }
        }

        /// <summary>
        /// Css cell style for summary table
        /// </summary>
        public string CellCssSummaryStyle
        {
            get
            {
                updateFinalCssStyle();
                if (!string.IsNullOrEmpty(FinalCssStyle)) return FinalCssStyle;

                string result = "";
                if (Element != null)
                {
                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssStyle : DefaultCellCssStyle;
                    else if (Element.IsNumeric) result = IsTitle ? "" : DefaultNumericCellCssStyle;
                    else if (Element.IsDateTime) result = IsTitle ? "" : DefaultDateTimeCellCssStyle;
                }
                return result;
            }
        }

        /// <summary>
        /// Css cell style for page table
        /// </summary>
        public string CellCssPageStyle
        {
            get
            {
                updateFinalCssStyle();
                if (!string.IsNullOrEmpty(FinalCssStyle)) return FinalCssStyle;

                string result = "";
                if (Element != null)
                {
                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssStyle : DefaultCellCssStyle;
                    else if (Element.IsNumeric) result = IsTitle ? DefaultNumericTitleCssStyle : DefaultNumericCellCssStyle;
                    else if (Element.IsDateTime) result = IsTitle ? DefaultDateTimeTitleCssStyle : DefaultDateTimeCellCssStyle;
                }
                return result;
            }
        }

        /// <summary>
        /// Css cell style
        /// </summary>
        public string CellCssStyle
        {
            get
            {
                updateFinalCssStyle();
                if (!string.IsNullOrEmpty(FinalCssStyle)) return FinalCssStyle;

                string result = "";
                if (Element != null)
                {
                    if (Element.IsText || Element.IsEnum) result = IsTitle ? DefaultTitleCssStyle : DefaultCellCssStyle;
                    else if (Element.IsNumeric) result = IsTitle ? DefaultNumericTitleCssStyle : DefaultNumericCellCssStyle;
                    else if (Element.IsDateTime) result = IsTitle ? DefaultDateTimeTitleCssStyle : DefaultDateTimeCellCssStyle;
                }
                return result;
            }
        }

        /// <summary>
        /// Returns true if at least a Sort is specified
        /// </summary>
        public static bool ShouldSort(List<ResultCell[]> values)
        {
            if (values.Count > 0) return ShouldSort(values[0]);
            return false;
        }

        /// <summary>
        /// Returns true if at least a Sort is specified
        /// </summary>
        public static bool ShouldSort(ResultCell[] values)
        {
            if (values != null) return values.FirstOrDefault(i => i.Element.FinalSortOrder != null) != null;
            return false;
        }

        /// <summary>
        /// Compares 2 cells arrays
        /// </summary>
        public static int CompareCells(ResultCell[] a, ResultCell[] b)
        {
            if (a.Length == 0 || a.Length != b.Length) return 0;
            ReportModel model = a[0].Element.Model;

            foreach (ReportElement element in model.Elements.Where(i => !string.IsNullOrEmpty(i.FinalSortOrder)).OrderBy(i => i.FinalSortOrder))
            {
                ResultCell aCell = a.FirstOrDefault(i => i.Element == element);
                ResultCell bCell = b.FirstOrDefault(i => i.Element == element);
                if (aCell != null && bCell != null)
                {
                    int result = CompareCell(aCell, bCell);
                    if (result != 0) return (element.FinalSortOrder.Contains(ReportElement.kAscendantSortKeyword) ? 1 : -1) * result;
                }
            }
            return 0;
        }

        /// <summary>
        /// Compares 2 cells arrays
        /// </summary>
        public static int CompareCellsForTableLoad(ResultCell[] a, ResultCell[] b)
        {
            if (a.Length == 0 || a.Length != b.Length) return 0;
            ReportModel model = null;
            int i = a.Length;
            while (--i >= 0 && model == null)
            {
                if (a[i].Element != null) model = a[i].Element.Model;
            }

            ReportElement element = model.Elements.FirstOrDefault(j => j.FinalSortOrder.Contains(" "));
            if (element != null)
            {
                var sortIndex = int.Parse(element.FinalSortOrder.Split(' ')[0]);
                if (sortIndex < a.Length)
                {
                    ResultCell aCell = a[sortIndex];
                    ResultCell bCell = b[sortIndex];
                    int result = CompareCell(aCell, bCell);
                    if (result != 0) return (element.FinalSortOrder.Contains(ReportElement.kAscendantSortKeyword) ? 1 : -1) * result;
                }
            }
            return 0;
        }


        /// <summary>
        /// Compares 2 cells
        /// </summary>
        public static int CompareCell(ResultCell a, ResultCell b)
        {
            if (a.Value == DBNull.Value && b.Value == DBNull.Value) return 0;
            else if (a.Value == DBNull.Value && b.Value != null) return -1;
            else if (a.Value != null && b.Value == DBNull.Value) return 1;

            if (a.Element == null && b.Element == null) return 0;
            else if (a.Element == null && b.Element != null) return -1;
            else if (a.Element != null && b.Element == null) return 1;

            if (a.Element.IsEnum)
            {
                return a.Element.GetEnumSortValue(a.Value.ToString(), true).CompareTo(b.Element.GetEnumSortValue(b.Value.ToString(), true));
            }
            else if (a.Element.IsText)
            {
                return a.Value.ToString().CompareTo(b.Value.ToString());
            }
            else if (a.Element.IsDateTime)
            {
                if (a.DateTimeValue == b.DateTimeValue) return 0;
                return a.DateTimeValue > b.DateTimeValue ? 1 : -1;
            }
            else if (a.Element.IsNumeric)
            {
                if (a.DoubleValue == b.DoubleValue) return 0;
                return a.DoubleValue > b.DoubleValue ? 1 : -1;
            }
            return 0;
        }

        List<NavigationLink> _links;

        public List<NavigationLink> Links
        {
            get
            {
                if (_links == null) initNavigationLinks();
                return _links;
            }
        }

        void initNavigationLinks()
        {
            //exe : execution guid of the source report
            //src : guid element source for drill
            //dst : guid element destination for drill
            //val : value of the restriction
            //res : guid element for a restriction
            //rpa : report path for sub-report
            //dis : display value for sub-report
            if (_links == null)
            {
                _links = new List<NavigationLink>();
                if (!IsTitle && !IsTotal && !IsTotalTotal && Element != null)
                {
                    var report = Element.Source.Report;
                    if (Element.PivotPosition != PivotPosition.Data)
                    {
                        //Get Drill child links
                        var metaData = Element.Source.MetaData;
                        foreach (string childGUID in Element.MetaColumn.DrillChildren)
                        {
                            //Check that the element is not already in the model
                            if (Element.Model.Elements.Exists(i => i.MetaColumnGUID == childGUID && i.PivotPosition == Element.PivotPosition)) continue;

                            var child = metaData.GetColumnFromGUID(childGUID);
                            if (child != null)
                            {
                                NavigationLink link = new NavigationLink();
                                link.Type = NavigationType.Drill;
                                link.Href = string.Format("exe={0}&src={1}&dst={2}&val={3}", report.ExecutionGUID, Element.MetaColumnGUID, childGUID, HttpUtility.UrlEncode(NavigationValue));
                                link.Text = HttpUtility.HtmlEncode(report.Translate("Drill >") + " " + report.Repository.RepositoryTranslate("Element", child.Category + '.' + child.DisplayName, child.DisplayName));

                                _links.Add(link);
                            }
                        }

                        //Get drill parent link
                        foreach (MetaTable table in Element.Source.MetaData.AllTables)
                        {
                            foreach (MetaColumn parentColumn in table.Columns.Where(i => i.DrillChildren.Contains(Element.MetaColumnGUID)))
                            {
                                //Check that the element is not already in the model
                                if (Element.Model.Elements.Exists(i => i.MetaColumnGUID == parentColumn.GUID && i.PivotPosition == Element.PivotPosition)) continue;

                                if (Element.MetaColumn.DrillUpOnlyIfDD)
                                {
                                    //check that the drill down occured
                                    if (!report.DrillParents.Contains(parentColumn.GUID)) continue;
                                }

                                NavigationLink link = new NavigationLink();
                                link.Type = NavigationType.Drill;
                                link.Href = string.Format("exe={0}&src={1}&dst={2}", report.ExecutionGUID, Element.MetaColumnGUID, parentColumn.GUID);
                                link.Text = HttpUtility.HtmlEncode(report.Translate("Drill <") + " " + report.Repository.RepositoryTranslate("Element", parentColumn.Category + '.' + parentColumn.DisplayName, parentColumn.DisplayName));
                                _links.Add(link);
                            }
                        }
                    }

                    //Get sub reports links
                    if (Element.PivotPosition != PivotPosition.Data)
                    {
                        foreach (var subreport in Element.MetaColumn.SubReports.Where(i => i.Restrictions.Count > 0))
                        {
                            string subReportRestrictions = "";
                            int index = 1;
                            foreach (var guid in subreport.Restrictions)
                            {
                                var cellValue = SubReportValues.FirstOrDefault(i => i.Element.MetaColumnGUID == guid);
                                if (cellValue != null && !string.IsNullOrEmpty(cellValue.NavigationValue))
                                {
                                    subReportRestrictions += string.Format("&res{0}={1}&val{0}={2}", index, guid, HttpUtility.UrlEncode(cellValue.NavigationValue));
                                    index++;
                                }
                            }
                            if (!string.IsNullOrEmpty(subReportRestrictions))
                            {
                                NavigationLink link = new NavigationLink();
                                link.Type = NavigationType.SubReport;
                                link.Href = string.Format("rpa={0}", HttpUtility.UrlEncode(subreport.Path));
                                if (subreport.Restrictions.Count > 1 || !subreport.Restrictions.Contains(Element.MetaColumn.GUID))
                                {
                                    //Add the display value if necessary
                                    link.Href += string.Format("&dis={0}", HttpUtility.UrlEncode(DisplayValue));
                                }
                                link.Href += subReportRestrictions;
                                link.Text = report.Repository.RepositoryTranslate("SubReport", Element.MetaColumn.Category + '.' + Element.MetaColumn.DisplayName, subreport.Name);
                                _links.Add(link);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrun the list of navigation links in the View context
        /// </summary>
        public List<NavigationLink> GetNavigationLinks(ReportView view)
        {
            var result = new List<NavigationLink>();
            foreach (var link in Links)
            {
                if (link.Type == NavigationType.Drill && !view.IsDrillEnabled) continue;
                if (link.Type == NavigationType.SubReport && !view.IsSubReportsEnabled) continue;

                var newLink = new NavigationLink() { Cell = link.Cell, Href = link.Href, Parameters = link.Parameters, Report = link.Report, Tag = link.Tag, Text = link.Text, Type = link.Type };
                Helper.CopyProperties(link, newLink);
                if (newLink.Type == NavigationType.Drill)
                {
                    //Add the navigation if exists
                    var modelView = view.ModelView;
                    var navigationView = modelView.GetValue(Parameter.NavigationView);
                    if (!string.IsNullOrEmpty(navigationView)) newLink.Href += string.Format("&view={0}", modelView.GetValue(Parameter.NavigationView));
                }
                result.Add(newLink);
            }
            return result;
        }


        /// <summary>
        /// Add a navigation link from this cell to download a file. The file will be loaded in the Navigation Script of the model.
        /// </summary>
        public void AddNavigationFileDownload(string text, string linkTag = "")
        {
            var guid = Guid.NewGuid().ToString();
            var link = new NavigationLink() { Type = NavigationType.FileDownload, Href = guid, Text = text, Cell = this, Report = Element != null ? Element.Report : null, Tag = linkTag };
            Links.Add(link);
            ContextModel.Report.NavigationLinks.Add(guid, link);
        }

        /// <summary>
        /// Add a navigation link from this cell to open a new page on a web site
        /// </summary>
        public void AddNavigationHyperLink(string href, string text)
        {
            Links.Add(new NavigationLink() { Type = NavigationType.Hyperlink, Href = href, Text = text });
        }

        /// <summary>
        /// Add a navigation link from this cell to execute another report within the current navigation context
        /// </summary>
        public void AddNavigationReportNavigation(string path, string menuText, string titleText = "")
        {
            var link = new NavigationLink() { Type = NavigationType.SubReport, Href = string.Format("rpa={0}", HttpUtility.UrlEncode(path)), Text = menuText };
            if (!string.IsNullOrEmpty(titleText)) link.Href += string.Format("&dis={0}", HttpUtility.UrlEncode(titleText));
            Links.Add(link);
        }

        /// <summary>
        /// Add a navigation link from this cell to execute another report in a new window
        /// </summary>
        public void AddNavigationReportExecution(string path, string menuText)
        {
            Links.Add(new NavigationLink() { Type = NavigationType.ReportExecution, Href = path, Text = menuText });
        }

        //Context to be used for cell script...
        /// <summary>
        /// For cell script execution: current ReportModel
        /// </summary>
        public ReportModel ContextModel;
        /// <summary>
        /// For cell script execution: current ResultPage
        /// </summary>
        public ResultPage ContextPage;
        /// <summary>
        /// For cell script execution: current ResultTable
        /// </summary>
        public ResultTable ContextTable;
        /// <summary>
        /// For cell script execution: current ResultTable
        /// </summary>
        public int ContextRow = -1;
        /// <summary>
        /// For cell script execution: current ResultTable
        /// </summary>
        public int ContextCol = -1;

        /// <summary>
        /// For cell script execution: current line of the table (array of cell)
        /// </summary>
        public ResultCell[] ContextCurrentLine
        {
            get { return ContextTable != null && ContextRow != -1 ? ContextTable.Lines[ContextRow] : null; }
        }

        /// <summary>
        /// For cell script execution: true if the cell is in a page table
        /// </summary>
        public bool ContextIsPageTable
        {
            get { return ContextPage != null && ContextTable != null && ContextPage.PageTable == ContextTable; }
        }

        /// <summary>
        /// For cell script execution: true if the cell is in a summary table
        /// </summary>
        public bool ContextIsSummaryTable
        {
            get { return ContextPage == null; }
        }
    }
}

