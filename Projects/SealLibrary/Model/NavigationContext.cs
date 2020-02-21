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
using System.Web;

namespace Seal.Model
{
    public class NavigationContext
    {
        public Dictionary<string, Navigation> Navigations = new Dictionary<string, Navigation>();

        public ReportExecution Navigate(string navigation, Report rootReport)
        {
            var parameters = HttpUtility.ParseQueryString(navigation);
            string reportPath = parameters.Get("rpa"); //For subreports
            string executionGuid = parameters.Get("exe"); //For drill
            bool isSubReport = !string.IsNullOrEmpty(reportPath);
            bool isDrill = !string.IsNullOrEmpty(executionGuid);

            Report newReport = null;

            //Check if the same navigation with the same execution GUID occured
            var previousNav = Navigations.Values.FirstOrDefault(i => i.Execution.NavigationParameter == navigation && i.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID);
            if (previousNav != null) newReport = previousNav.Execution.Report;

            string destLabel = "", srcRestriction = "", srcGUID = "";
            if (Navigations.Count(i => i.Value.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID) == 1)
            {
                //For the first navigation, we update the JS file in the result to show up the button
                string html = File.ReadAllText(rootReport.ResultFilePath);
                html = html.Replace("var hasNavigation = false;/*SRKW do not modify*/", "var hasNavigation = true;");
                rootReport.ResultFilePath = Helpers.FileHelper.GetUniqueFileName(rootReport.ResultFilePath);
                File.WriteAllText(rootReport.ResultFilePath, html, System.Text.Encoding.UTF8);
                rootReport.IsNavigating = true;
                rootReport.HasNavigation = true;
                Navigations.First(i => i.Value.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID).Value.Link.Href = !string.IsNullOrEmpty(rootReport.WebUrl) ? ReportExecution.ActionViewHtmlResultFile + "?execution_guid=" + rootReport.ExecutionGUID : rootReport.ResultFilePath;
            }

            if (!string.IsNullOrEmpty(reportPath))
            {
                //Sub-Report
                if (newReport == null)
                {
                    string path = reportPath.Replace(Repository.SealRepositoryKeyword, rootReport.Repository.RepositoryPath);
                    newReport = Report.LoadFromFile(path, rootReport.Repository);

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
                var els = new Dictionary<ReportElement, string>();
                if (newReport == null)
                {
                    if (Navigations.ContainsKey(executionGuid)) newReport = Navigations[executionGuid].Execution.Report.Clone();
                    else
                    {
                        //Drill from dashboard
                        newReport = rootReport.Clone();
                    }
                    newReport.ExecutionGUID = Guid.NewGuid().ToString();
                    string src = parameters.Get("src");
                    string dst = parameters.Get("dst");
                    string val = parameters.Get("val");
                    newReport.DrillParents.Add(src);

                    foreach (var model in newReport.Models)
                    {
                        foreach (var element in model.Elements.Where(i => i.MetaColumnGUID == src))
                        {
                            //Has already restriction ? if down check src, if up check dest
                            bool hasAlreadyRestriction = model.Restrictions.Exists(i => i.MetaColumnGUID == (val != null ? src : dst));
                            if (element != null || hasAlreadyRestriction)
                            {
                                if (val != null)
                                {
                                    //Drill Down: Add restriction 
                                    ReportRestriction restriction = ReportRestriction.CreateReportRestriction();
                                    restriction.Source = model.Source;
                                    restriction.Report = newReport;
                                    restriction.Model = model;
                                    restriction.MetaColumnGUID = src;
                                    restriction.SetDefaults();
                                    restriction.Operator = Operator.Equal;
                                    if (val == "") restriction.Operator = Operator.IsEmpty;
                                    else if (val == null) restriction.Operator = Operator.IsNull;
                                    restriction.SetNavigationValue(val);
                                    model.Restrictions.Add(restriction);
                                    if (!string.IsNullOrEmpty(model.Restriction)) model.Restriction = string.Format("({0}) AND ", model.Restriction);
                                    model.Restriction += ReportRestriction.kStartRestrictionChar + restriction.GUID + ReportRestriction.kStopRestrictionChar;

                                    srcRestriction = restriction.GeNavigationDisplayValue();
                                    srcGUID = restriction.MetaColumnGUID;

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
                                        model.Restriction = model.Restriction.Replace(ReportRestriction.kStartRestrictionChar + restr.GUID + ReportRestriction.kStopRestrictionChar, "1=1");
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
                                //Set new GUID
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

            if (newReport == null) throw new Exception("Invalid Navigation");

            newReport.WebUrl = rootReport.WebUrl;
            newReport.IsNavigating = true;
            newReport.HasNavigation = true;

            if (previousNav == null)
            {
                if (!string.IsNullOrEmpty(srcRestriction)) newReport.DisplayName = string.Format("{0} > {1}", newReport.ExecutionName, srcRestriction);
                else newReport.DisplayName = string.Format("{0} < {1}", newReport.ExecutionName, destLabel);
            }
            return new ReportExecution() { NavigationParameter = navigation, Report = newReport, RootReport = rootReport };
        }

        public string NavigateScript(string navigation, Report report, NameValueCollection parameters)
        {
            var linkGUID = navigation.Substring(3);
            var result = "";
            if (report.NavigationLinks.ContainsKey(linkGUID))
            {
                var link = report.NavigationLinks[linkGUID];
                link.Parameters = parameters;
                if (link.Cell != null && link.Cell.Element != null) //Cell Navigation Script
                {
                    RazorHelper.CompileExecute(link.Cell.Element.NavigationScript, link);
                    result = link.ScriptResult;
                }
                else if (link.Cell == null && link.Report != null) //Report Navigation Script
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
            navigation.Link.Href = !string.IsNullOrEmpty(execution.Report.WebUrl) ? ReportExecution.ActionViewHtmlResultFile + "?execution_guid=" + execution.Report.ExecutionGUID : execution.Report.ResultFilePath;

            //set root report here
            if (execution.RootReport == null) execution.RootReport = execution.Report;
        }

        public string GetNavigationLinksHTML(Report rootReport)
        {
            string links = "";
            foreach (var navigation in Navigations.Values.Where(i => i.Execution.RootReport.ExecutionGUID == rootReport.ExecutionGUID))
            {
                links += string.Format("<li><a href='{0}'>{1}</a></li>", HttpUtility.HtmlEncode(navigation.Link.Href), HttpUtility.HtmlEncode(navigation.Link.Text));
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
    }
}
