//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Seal.Model
{
    /// <summary>
    /// Class to perform the navigation (sub-report or drill)
    /// </summary>
    public class NavigationContext
    {
        public Dictionary<string, Navigation> Navigations = new Dictionary<string, Navigation>();

        public ReportExecution Navigate(string navigation, ReportExecution execution, bool newWindow)
        {
            var rootReport = execution.RootReport;
            var parameters = HttpUtility.ParseQueryString(navigation);
            string reportPath = parameters.Get("rpa"); //For subreports
            string executionGuid = parameters.Get("exe"); //For drill

            Report newReport = null;
            string destLabel = "", srcRestriction = "";
            Navigation previousNav = null;

            if (!newWindow)
            {
                //Check if the same navigation with the same execution GUID occured
                previousNav = Navigations.Values.FirstOrDefault(i => i.Execution.NavigationParameter == navigation && i.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID);
                if (previousNav != null) newReport = previousNav.Execution.Report;

                if (Navigations.Count(i => i.Value.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID) == 1)
                {
                    //For the first navigation, we update the JS file in the result to show up the button
                    string html = File.ReadAllText(rootReport.ResultFilePath);
                    html = html.Replace("var _hasNavigation = false;/*SRKW do not modify*/", "var _hasNavigation = true;");
                    rootReport.ResultFilePath = Helpers.FileHelper.GetUniqueFileName(rootReport.ResultFilePath);
                    File.WriteAllText(rootReport.ResultFilePath, html, System.Text.Encoding.UTF8);
                    rootReport.IsNavigating = true;
                    rootReport.HasNavigation = true;
                    Navigations.First(i => i.Value.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID).Value.Link.Href = rootReport.ExecutionGUID; // !string.IsNullOrEmpty(rootReport.WebUrl) ? ReportExecution.ActionViewHtmlResultFile + "?execution_guid=" + rootReport.ExecutionGUID : rootReport.ResultFilePath;
                }
            }

            if (!string.IsNullOrEmpty(reportPath))
            {
                //Sub-Report
                if (newReport == null)
                {
                    string path = FileHelper.ConvertOSFilePath(reportPath.Replace(Repository.SealRepositoryKeyword, rootReport.Repository.RepositoryPath));
                    if (!File.Exists(path)) path = rootReport.Repository.ReportsFolder + path;
                    newReport = Report.LoadFromFile(path, rootReport.Repository);
                    newReport.CurrentViewGUID = newReport.ViewGUID;

                    int index = 1;
                    while (true)
                    {
                        string res = parameters.Get("res" + index.ToString());
                        string val = parameters.Get("val" + index.ToString());
                        if (string.IsNullOrEmpty(res) || string.IsNullOrEmpty(val)) break;
                        foreach (var model in newReport.Models)
                        {
                            foreach (var restriction in model.Restrictions.Where(i => i.MetaColumnGUID == res))
                            {
                                restriction.SetNavigationValue(val);
                                srcRestriction = restriction.GeNavigationDisplayValue();
                            }
                        }
                        index++;
                    }
                    //Get display value
                    string dis = parameters.Get("dis");
                    if (!string.IsNullOrEmpty(dis)) srcRestriction = HttpUtility.HtmlDecode(dis);
                }
            }
            else if (!string.IsNullOrEmpty(executionGuid))
            {
                //Drill
                if (newReport == null)
                {
                    newReport = Navigations[executionGuid].Execution.Report.Clone();
                    newReport.ExecutionGUID = Guid.NewGuid().ToString();

                    //Set view    
                    newReport.CurrentViewGUID = execution.Report.CurrentViewGUID;
                    //Set Navigation view if specified
                    string viewGUID = parameters.Get("view");
                    if (!string.IsNullOrEmpty(viewGUID) && newReport.Views.Exists(i => i.GUID == viewGUID)) newReport.CurrentViewGUID = viewGUID;

                    string src = parameters.Get("src");
                    string dst = parameters.Get("dst");
                    string val = parameters.Get("val");
                    newReport.DrillParents.Add(src);

                    foreach (var model in newReport.Models)
                    {
                        drillModel(model, src, dst, val, ref destLabel, ref srcRestriction, newReport, rootReport);

                        if (model.IsLINQ)
                        {
                            //Handle restriction also for the sub-models
                            foreach (var subModel in model.LINQSubModels) drillModel(subModel, src, dst, val, ref destLabel, ref srcRestriction, newReport, rootReport);
                        }
                    }
                }
            }

            if (newReport == null) throw new Exception("Invalid Navigation");

            newReport.WebUrl = rootReport.WebUrl;
            newReport.IsNavigating = true;
            newReport.HasNavigation = true;

            if (previousNav == null)
            {
                if (!string.IsNullOrEmpty(srcRestriction)) newReport.DisplayName = string.Format("{0} > {1}", newReport.ExecutionName, srcRestriction);
                else
                {
                    newReport.DisplayName = newReport.ExecutionName;
                    if (!string.IsNullOrEmpty(destLabel)) newReport.DisplayName += string.Format(" < {0}", destLabel);
                }
            }
            return new ReportExecution() { NavigationParameter = navigation, Report = newReport, RootReport = rootReport };
        }

        public string NavigateScript(string navigation, Report report, NameValueCollection parameters, HttpRequest request = null)
        {
            var linkGUID = navigation.Substring(3);
            var result = "";
            if (report.NavigationLinks.ContainsKey(linkGUID))
            {
                var link = report.NavigationLinks[linkGUID];
                link.Parameters = parameters;
                link.Request = request;
                if (link.Cell != null && link.Cell.Element != null) //Cell navigation script
                {
                    RazorHelper.CompileExecute(link.Cell.Element.NavigationScript, link);
                    result = link.ScriptResult;
                }
                else if (link.Cell == null && link.Report != null) //Report navigation script
                {
                    RazorHelper.CompileExecute(link.Report.NavigationScript, link);
                    result = link.ScriptResult;
                }
            }
            return result;
        }

        public void SetNavigation(ReportExecution execution)
        {
            Navigation navigation = null;
            if (!Navigations.ContainsKey(execution.Report.ExecutionGUID))
            {
                navigation = new Navigation() { Execution = execution };
                Navigations.Add(execution.Report.ExecutionGUID, navigation);
            }
            else
            {
                navigation = Navigations[execution.Report.ExecutionGUID];
            }

            navigation.Link = new NavigationLink()
            {
                Type = NavigationType.SubReport,
                Text = execution.Report.ExecutionName
            };
            navigation.Link.Href = execution.Report.ExecutionGUID;

            //set root report here
            if (execution.RootReport == null) execution.RootReport = execution.Report;
        }

        public string GetNavigationLinksHTML(Report rootReport)
        {
            string links = "";
            foreach (var navigation in Navigations.Values.Where(i => i.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID))
            {
                if (!string.IsNullOrEmpty(navigation.Execution.Report.WebUrl)) 
                {
                    //Execution from Web
                    links += string.Format("<li><a href='#' execution_guid='{0}'>{1}</a></li>", HttpUtility.HtmlEncode(navigation.Link.Href), HttpUtility.HtmlEncode(navigation.Link.Text));
                }
                else
                {
                    links += string.Format("<li><a href='{0}'>{1}</a></li>", HttpUtility.HtmlEncode(navigation.Execution.Report.ResultFilePath), HttpUtility.HtmlEncode(navigation.Link.Text));
                }
            }
            return links;
        }

        void changeElementGUID(ReportElement element, string newGUID, ReportView view)
        {
            string initialLabel = element.DisplayNameEl;
            element.ChangeColumnGUID(newGUID);
            view.ReplaceInParameterValues("nvd3_chart_title", "%" + initialLabel + "%", "%" + element.DisplayNameEl + "%");
            view.ReplaceInParameterValues("chartjs_title", "%" + initialLabel + "%", "%" + element.DisplayNameEl + "%");
            view.ReplaceInParameterValues("plotly_title", "%" + initialLabel + "%", "%" + element.DisplayNameEl + "%");
        }

        void drillModel(ReportModel model, string src, string dst, string val, ref string destLabel, ref string srcRestriction, Report newReport, Report rootReport)
        {
            //First remove hidden elements
            model.Elements.RemoveAll(i => i.PivotPosition == PivotPosition.Hidden);

            foreach (var element in model.Elements.Where(i => i.MetaColumnGUID == src).ToList())
            {
                var els = new Dictionary<ReportElement, string>();
                if (val != null)
                {
                    //Drill Down, check src
                    ReportRestriction restriction = ReportRestriction.CreateReportRestriction();
                    model.Restrictions.Add(restriction);
                    restriction.Source = model.Source;
                    restriction.Report = newReport;
                    restriction.Model = model;
                    restriction.MetaColumnGUID = src;
                    restriction.Operator = Operator.Equal;
                    restriction.SetDefaults();
                    restriction.IsForNavigation = true;
                    if (val == "") restriction.Operator = Operator.IsEmpty;
                    restriction.SetNavigationValue(val);
                    if (!string.IsNullOrEmpty(model.Restriction)) model.Restriction = string.Format("({0}) {1} ", model.Restriction, model.IsLINQ ? "&&" : "AND");
                    model.Restriction += ReportRestriction.kStartRestrictionChar + restriction.GUID + ReportRestriction.kStopRestrictionChar;

                    srcRestriction = restriction.GeNavigationDisplayValue();

                    if (rootReport.ExecutionView.GetBoolValue(Parameter.DrillAllParameter))
                    {
                        //Check if children are involved

                        //Elements having a child reaching the src -> they are now the src 
                        foreach (var child in model.Elements.Where(i => i.MetaColumn.DrillChildren.Contains(src)))
                        {
                            els.Add(child, src);
                        }

                        //Children elements of the new dst, change dst element to its child 
                        var destColumn = model.Source.MetaData.AllColumns.FirstOrDefault(i => i.GUID == dst);
                        if (destColumn != null && destColumn.DrillChildren.Count > 0)
                        {
                            var childColumn = model.Source.MetaData.AllColumns.Where(i => destColumn.DrillChildren.Contains(i.GUID)).OrderBy(i => i.DisplayOrder).FirstOrDefault();
                            if (childColumn != null)
                            {
                                //Set new GUID for chidren
                                foreach (var child in model.Elements.Where(i => i.MetaColumnGUID == dst))
                                {
                                    els.Add(child, childColumn.GUID);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Drill Up: Remove restrictions
                    var restrictions = model.Restrictions.Where(i => i.MetaColumnGUID == dst).ToList();
                    foreach (var restr in restrictions)
                    {
                        model.Restrictions.Remove(restr);
                        model.Restriction = model.Restriction.Replace(ReportRestriction.kStartRestrictionChar + restr.GUID + ReportRestriction.kStopRestrictionChar, model.IsLINQ ? "true" : "1=1");
                    }

                    //Check if parents are involved
                    if (rootReport.ExecutionView.GetBoolValue(Parameter.DrillAllParameter))
                    {
                        //Check element having a child to dest
                        //First metacolumn sorted by DisplayOrder
                        var parentColumn = model.Source.MetaData.AllColumns.Where(i => i.DrillChildren.Contains(dst)).OrderBy(i => i.DisplayOrder).FirstOrDefault();
                        if (parentColumn != null)
                        {
                            //Set new GUID for parents
                            foreach (var parent in model.Elements.Where(i => i.MetaColumnGUID == dst))
                            {
                                els.Add(parent, parentColumn.GUID);
                            }
                        }

                        //Check element having a child to src
                        foreach (var child in element.MetaColumn.DrillChildren)
                        {
                            foreach (var parent in model.Elements.Where(i => i.MetaColumnGUID == child))
                            {
                                els.Add(parent, src);
                            }
                        }
                    }
                }
                //Set new GUIDs
                if (!model.Elements.Exists(i => i.MetaColumnGUID == dst))
                {
                    if (element != null)
                    {
                        changeElementGUID(element, dst, newReport.ExecutionView);
                        destLabel = element.DisplayNameElTranslated;
                    }

                    foreach (var el in els)
                    {
                        changeElementGUID(el.Key, el.Value, newReport.ExecutionView);
                    }
                }
            }
        }
    }
}
