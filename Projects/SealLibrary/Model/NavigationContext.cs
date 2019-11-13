//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
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
                            foreach (var restriction in model.Restrictions.Where(i => i.MetaColumnGUID == res && i.Prompt != PromptType.None))
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
                        ReportElement element = model.Elements.FirstOrDefault(i => i.MetaColumnGUID == src);
                        //Has already restriction ? if down check src, if up check dest
                        bool hasAlreadyRestriction = model.Restrictions.Exists(i => i.MetaColumnGUID == (val != null ? src : dst));
                        if (element != null || hasAlreadyRestriction)
                        {
                            if (element != null)
                            {
                                string initialLabel = element.DisplayNameEl;
                                element.ChangeColumnGUID(dst);
                                destLabel = element.DisplayNameElTranslated;
                                newReport.ExecutionView.ReplaceInParameterValues("nvd3_chart_title", "%" + initialLabel + "%", "%" + element.DisplayNameEl + "%");
                            }

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

        public string NavigateScript(string navigation, Report report)
        {
            var linkGUID = navigation.Replace(NavigationLink.FileDownloadPrefix, "");
            var result = "";
            if (report.NavigationLinks.ContainsKey(linkGUID))
            {
                var link = report.NavigationLinks[linkGUID];
                if (link.Cell.Element != null)
                {
                    RazorHelper.CompileExecute(link.Cell.Element.NavigationScript, link);
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
    }
}
