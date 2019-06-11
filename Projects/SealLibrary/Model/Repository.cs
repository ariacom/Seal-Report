//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Seal.Helpers;
using System.Globalization;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace Seal.Model
{
    public class Repository
    {
        public const string SealRootProductName = "Seal";
        public const string SealConfigurationFileExtension = "scfx";
        public const string SealReportFileExtension = "srex";
        public const string SealDashboardExtension = "sdax";
        public const string SealTaskScheduler = "SealTaskScheduler.exe";
        public const string SealReportDesigner = "SealReportDesigner.exe";
        public const string SealServerManager = "SealServerManager.exe";
        public const string SealDefaultRepository = "Seal Report Repository";
        public const string SealRepositoryKeyword = "%SEALREPOSITORY%";
        public const string SealReportsRepositoryKeyword = "%SEALREPORTSREPOSITORY%";
        public const string SealPersonalRepositoryKeyword = "%SEALPERSONALREPOSITORY%";
        public const string SealReportDisplayNameKeyword = "%SEALREPORTDISPLAYNAME%";
        public const string SealReportDisplayNameKeyword2 = "{Report Name}";
        public const string CommonRestrictionKeyword = "{CommonRestriction_";
        public const string CommonValueKeyword = "{CommonValue_";
        public const string EnumFilterKeyword = "{EnumFilter";
        public const string EnumValuesKeyword = "{EnumValues_";
        public const string JoinAutoName = "<AutomaticJoinName>";

        public const string SealWebPublishTemp = "temp";

        string _path;
        public string RepositoryPath
        {
            get { return _path; }
        }


        public static string ProductVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public static Icon ProductIcon
        {
            get { return Path.GetFileName(Application.ExecutablePath).ToLower() == SealServerManager.ToLower() ? Properties.Resources.serverManager : Properties.Resources.reportDesigner; }
        }

        private string _licenseText = null;
        public string LicenseText
        {
            get
            {
                if (_licenseText == null)
                {
                    var converter = SealPdfConverter.Create(ApplicationPath);
                    _licenseText = converter.GetLicenseText();
                }

                if (_licenseText == null) _licenseText = "";
                return _licenseText;
            }
        }


        List<MetaSource> _sources = new List<MetaSource>();
        public List<MetaSource> Sources
        {
            get { return _sources; }
        }

        List<OutputDevice> _devices = new List<OutputDevice>();
        public List<OutputDevice> Devices
        {
            get { return _devices; }
        }


        CultureInfo _cultureInfo = null;
        public CultureInfo CultureInfo
        {
            get
            {

                if (_cultureInfo == null && !string.IsNullOrEmpty(Configuration.DefaultCulture)) _cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == Configuration.DefaultCulture);
                if (_cultureInfo == null) _cultureInfo = CultureInfo.CurrentCulture;

                return _cultureInfo;
            }
        }

        public string MomentJSShortDateFormat
        {
            get
            {
                return Helper.ToMomentJSFormat(CultureInfo, CultureInfo.DateTimeFormat.ShortDatePattern);
            }
        }

        public string MomentJSShortDateTimeFormat
        {
            get
            {
                return Helper.ToMomentJSFormat(CultureInfo, CultureInfo.DateTimeFormat.ShortDatePattern + ' ' + CultureInfo.DateTimeFormat.LongTimePattern);
            }
        }

        public bool SetCultureInfo(string cultureName)
        {
            bool result = false;
            try
            {
                var newCulture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == cultureName);
                if (newCulture == null) newCulture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.Name == cultureName);
                if (newCulture != null)
                {
                    result = true;
                    _cultureInfo = newCulture;
                    _nvd3Translations = null;
                    _jsTranslations = null;
                }
            }
            catch { }

            return result;
        }

        SealServerConfiguration _configuration = null;
        public SealServerConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = SealServerConfiguration.LoadFromFile(ConfigurationPath, true);
                    if (_configuration == null) _configuration = new SealServerConfiguration();
                    _configuration.Repository = this;
                    try
                    {
                        //save install directory if necessary
                        if (string.IsNullOrEmpty(_configuration.InstallationDirectory)) _configuration.SaveToFile();
                    }
                    catch { }
                }
                return _configuration;
            }
            set { _configuration = value; }
        }

        public void ReloadConfiguration()
        {
            _configuration = null;
        }

        SealSecurity _security = null;
        public SealSecurity Security
        {
            get
            {
                if (_security == null)
                {
                    _security = SealSecurity.LoadFromFile(SecurityPath, false);
                    if (_security == null) _security = new SealSecurity();
                    _security.Repository = this;
                }
                return _security;
            }
            set { _security = value; }
        }

        public void ReloadSecurity()
        {
            _security = null;
        }

        public static string FindDebugRepository(string path)
        {
            string result = Path.Combine(Path.GetDirectoryName(path), "Repository");
            if (Directory.Exists(result)) return result;
            return null;
        }


        public static string FindRepository()
        {
            string path = Properties.Settings.Default.RepositoryPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
#if DEBUG
                //Try to get the Repository from the dev env.
                path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""));
                while (Path.GetPathRoot(path) != path)
                {
                    string newPath = FindDebugRepository(path);
                    if (newPath != null)
                    {
                        path = newPath;
                        break;
                    }
                    path = Path.GetDirectoryName(path);
                }
#endif
                if (!Directory.Exists(path) || path == Path.GetPathRoot(path))
                {
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SealDefaultRepository);
                }
            }
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }


        //The general static instance...
        static Repository _instance = null;
        public static Repository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Create();
                }
                return _instance;
            }
        }

        public static Repository Create(string path)
        {
            Repository result = null;

            if (Directory.Exists(path))
            {
                result = new Repository();
                result.Init(path);
            }
            if (result == null) throw new Exception(string.Format("Unable to find or create a Repository from '{0}'.", path));

            return result;
        }


        public static Repository Create()
        {
            Repository result = null;

            string path = FindRepository();
            if (Directory.Exists(path))
            {
                result = new Repository();
                result.Init(path);
            }
            if (result == null) throw new Exception(string.Format("Unable to find or create a Repository from '{0}'. Please check your configuration file.", Properties.Settings.Default.RepositoryPath));

            return result;
        }


        //Same as create but we shared or clone properties for performance reasons...
        public Repository CreateFast()
        {
            //check if some files have changed, in this case -> full reload
            if (MustReload()) return Repository.Create();

            //Fast load
            Repository result = null;
            string path = FindRepository();
            if (Directory.Exists(path))
            {
                result = new Repository();
                //Just clone the sources as connections and enums can be updated
                result._sources = (List<MetaSource>)Helper.Clone(Sources);
                foreach (var source in result.Sources) source.InitReferences(result);
                //Others collections should remain static/unchanged an can be shared...
                result._translations = Translations;
                result._devices = Devices;
                result._configuration = Configuration;
                result._path = path;
                //plus defaults...
                result._cultureInfo = CultureInfo;
            }
            return result;
        }

        static bool _assembliesLoaded = false;
        public void Init(string path)
        {
            _path = path;
            CheckFolders();
            //Data sources
            if (_sources.Count == 0)
            {
                foreach (var file in Directory.GetFiles(SourcesFolder, "*." + SealConfigurationFileExtension))
                {
                    try
                    {
                        var source = MetaSource.LoadFromFile(file, this);
                        _sources.Add(source);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }

            if (_devices.Count == 0)
            {
                //Devices, add a default folder device, then the other devices
                _devices.Add(OutputFolderDevice.Create());
                foreach (var file in Directory.GetFiles(DevicesEmailFolder, "*." + SealConfigurationFileExtension))
                {
                    try
                    {
                        var device = OutputEmailDevice.LoadFromFile(file, true);
                        _devices.Add(device);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }

            if (!_assembliesLoaded)
            {
                //Load extra assemblies defined in Repository
                var assemblies = Directory.GetFiles(AssembliesFolder, "*.dll");
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        Assembly.LoadFrom(assembly);
                    }
                    catch (Exception Exception)
                    {
                        Debug.WriteLine(Exception.Message);
                    }
                }

                //Add this assembly resolve necessary when executing Razor scripts
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolve);

                _assembliesLoaded = true;
            }
        }

        Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(AssembliesFolder, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }



        public bool MustReload()
        {
            if (Configuration.FilePath != null && File.GetLastWriteTime(Configuration.FilePath) != Configuration.LastModification) return true;
            if (Security.FilePath != null && File.GetLastWriteTime(Security.FilePath) != Security.LastModification) return true;
            foreach (var item in Sources) if (File.GetLastWriteTime(item.FilePath) != item.LastModification) return true;
            foreach (var item in Devices.Where(i => i is OutputEmailDevice)) if (item.FilePath != null && File.GetLastWriteTime(item.FilePath) != ((OutputEmailDevice)item).LastModification) return true;
            return false;
        }

        public void CheckFolders()
        {
            try
            {
                if (!Directory.Exists(ViewsFolder)) Directory.CreateDirectory(ViewsFolder);
                if (!Directory.Exists(SourcesFolder)) Directory.CreateDirectory(SourcesFolder);
                if (!Directory.Exists(DevicesFolder)) Directory.CreateDirectory(DevicesFolder);
                if (!Directory.Exists(DevicesEmailFolder)) Directory.CreateDirectory(DevicesEmailFolder);
                if (!Directory.Exists(ReportsFolder)) Directory.CreateDirectory(ReportsFolder);
                if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
                if (!Directory.Exists(SecurityFolder)) Directory.CreateDirectory(SecurityFolder);
                if (!Directory.Exists(SecurityProvidersFolder)) Directory.CreateDirectory(SecurityProvidersFolder);
                if (!Directory.Exists(AssembliesFolder)) Directory.CreateDirectory(AssembliesFolder);
                if (!Directory.Exists(SubReportsFolder)) Directory.CreateDirectory(SubReportsFolder);
                if (!Directory.Exists(SpecialsFolder)) Directory.CreateDirectory(SpecialsFolder);
                if (!Directory.Exists(PersonalFolder)) Directory.CreateDirectory(PersonalFolder);
                if (!Directory.Exists(DashboardPublicFolder)) Directory.CreateDirectory(DashboardPublicFolder);
            }
            catch { }
        }

        public string ViewsFolder
        {
            get { return Path.Combine(_path, "Views"); }
        }

        public string SourcesFolder
        {
            get { return Path.Combine(_path, "Sources"); }
        }

        public string DevicesFolder
        {
            get { return Path.Combine(_path, "Devices"); }
        }

        public string DevicesEmailFolder
        {
            get { return Path.Combine(_path, "Devices\\Email"); }
        }

        public string SettingsFolder
        {
            get { return Path.Combine(_path, "Settings"); }
        }

        public string SecurityFolder
        {
            get { return Path.Combine(_path, "Security"); }
        }

        public string SecurityProvidersFolder
        {
            get { return Path.Combine(SecurityFolder, "Providers"); }
        }

        public string ReportsFolder
        {
            get { return Path.Combine(_path, "Reports"); }
        }

        public string LogsFolder
        {
            get { return Path.Combine(_path, "Logs"); }
        }

        public string ViewImagesFolder
        {
            get { return Path.Combine(ViewsFolder, "Images"); }
        }

        public string ViewScriptsFolder
        {
            get { return Path.Combine(ViewsFolder, "Scripts"); }
        }

        public string ViewContentFolder
        {
            get { return Path.Combine(ViewsFolder, "Content"); }
        }

        public string AssembliesFolder
        {
            get { return Path.Combine(_path, "Assemblies"); }
        }

        public string SubReportsFolder
        {
            get { return Path.Combine(_path, "SubReports"); }
        }

        public string SpecialsFolder
        {
            get { return Path.Combine(_path, "SpecialFolders"); }
        }

        public string PersonalFolder
        {
            get { return Path.Combine(SpecialsFolder, "Personal"); }
        }

        public string GetPersonalFolder(SecurityUser user)
        {
            //add hash to the end of the name
            var name = user.GetPersonalFolderName();
            var hash = Math.Abs(Helper.CalculateHash(name));
            string result = Path.Combine(PersonalFolder, string.Format("{0}_{1}", FileHelper.CleanFilePath(name), hash));
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            return result;
        }

        public string GetPersonalFolderName(SecurityUser user)
        {
            return TranslateWeb("Personal") + string.Format(" ({0})", user.GetPersonalFolderName()); ;
        }

        public string GetDashboardPersonalFolder(SecurityUser user)
        {
            return Path.Combine(GetPersonalFolderName(user), "Dashboards");
        }

        public string DashboardPublicFolder
        {
            get { return Path.Combine(_path, "Dashboards"); }
        }

        public string TranslationsPattern
        {
            get { return "Translations*.csv"; }
        }

        public string RepositoryTranslationsPath
        {
            get { return Path.Combine(SettingsFolder, "RepositoryTranslations.csv"); }
        }

        public string ConfigurationPath
        {
            get { return Path.Combine(SettingsFolder, "Configuration.xml"); }
        }

        public string SecurityPath
        {
            get { return Path.Combine(SecurityFolder, "Security.xml"); }
        }

        public string ReplaceRepositoryKeyword(string inputFolder)
        {
            if (string.IsNullOrEmpty(inputFolder)) return "";
            return inputFolder.Replace(Repository.SealRepositoryKeyword, _path).Replace(SealPersonalRepositoryKeyword, PersonalFolder).Replace(SealReportsRepositoryKeyword, ReportsFolder);
        }

        #region Translations

        //Translations, one dictionary per context
        Dictionary<string, RepositoryTranslation> _translations = null;
        public Dictionary<string, RepositoryTranslation> Translations
        {
            get
            {
                if (_translations == null)
                {
                    lock (this)
                    {
                        _translations = new Dictionary<string, RepositoryTranslation>();
                        foreach (string path in Directory.GetFiles(SettingsFolder, TranslationsPattern))
                        {
                            RepositoryTranslation.InitFromCSV(_translations, path, false);
                        }
                    }
                }
                return _translations;
            }
        }

        Dictionary<string, string> _jsTranslations = null;
        public Dictionary<string, string> JSTranslations
        {
            get
            {
                if (_jsTranslations == null)
                {
                    lock (this)
                    {
                        _jsTranslations = new Dictionary<string, string>();
                        foreach (var translation in Translations.Values.Where(i => i.Context == "WebJS"))
                        {
                            var value = translation.Reference;
                            if (translation.Translations.ContainsKey(CultureInfo.TwoLetterISOLanguageName))
                            {
                                value = translation.Translations[CultureInfo.TwoLetterISOLanguageName];
                                if (string.IsNullOrEmpty(value)) value = translation.Reference;
                            }
                            if (!_jsTranslations.ContainsKey(translation.Reference)) _jsTranslations.Add(translation.Reference, value);
                        }
                    }
                }
                return _jsTranslations;
            }
        }

        Dictionary<string, string> _nvd3Translations = null;
        public Dictionary<string, string> NVD3Translations
        {
            get
            {
                if (_nvd3Translations == null)
                {
                    lock (this)
                    {
                        _nvd3Translations = new Dictionary<string, string>();
                        foreach (var translation in Translations.Values.Where(i => i.Context == "NVD3"))
                        {
                            var value = translation.Reference;
                            if (translation.Translations.ContainsKey(CultureInfo.TwoLetterISOLanguageName))
                            {
                                value = translation.Translations[CultureInfo.TwoLetterISOLanguageName];
                                if (string.IsNullOrEmpty(value)) value = translation.Reference;
                            }
                            if (!_nvd3Translations.ContainsKey(translation.Reference)) _nvd3Translations.Add(translation.Reference, value);
                        }
                    }
                }
                return _nvd3Translations;
            }
        }

        public string Translate(string context, string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, context, reference);
        }

        public string TranslateWeb(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "Web", reference);
        }

        public string TranslateWebJS(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "WebJS", reference);
        }

        public string TranslateReport(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "Report", reference);
        }


        Dictionary<string, RepositoryTranslation> _repositoryTranslations = null;
        public Dictionary<string, RepositoryTranslation> RepositoryTranslations
        {
            get
            {
                if (_repositoryTranslations == null)
                {
                    lock (this)
                    {
                        _repositoryTranslations = new Dictionary<string, RepositoryTranslation>();
                        RepositoryTranslation.InitFromCSV(_repositoryTranslations, RepositoryTranslationsPath, true);
                    }
                }
                return _repositoryTranslations;
            }
        }

        public string RepositoryTranslate(string culture, string context, string instance, string reference)
        {
            if (string.IsNullOrEmpty(reference)) return "";

            string result = reference;
            try
            {
                var key = context + "\r" + reference + "\r" + instance;
                RepositoryTranslation myTranslation = RepositoryTranslations.ContainsKey(key) ? RepositoryTranslations[key] : null;
                if (myTranslation != null)
                {
                    if (!string.IsNullOrEmpty(culture) && myTranslation.Translations.ContainsKey(culture))
                    {
                        result = myTranslation.Translations[culture];
                        if (string.IsNullOrEmpty(result)) result = reference;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result;
        }

        public string RepositoryTranslate(string context, string instance, string reference)
        {
            return RepositoryTranslate(CultureInfo.TwoLetterISOLanguageName, context, instance, reference);
        }

        public string TranslateColumn(MetaColumn col)
        {
            return RepositoryTranslate("Element", col.Category + '.' + col.DisplayName, col.DisplayName);
        }

        public string TranslateCategory(string instance, string reference)
        {
            return RepositoryTranslate("Category", instance, reference);
        }

        public string TranslateDevice(string instance, string reference)
        {
            return RepositoryTranslate("Device", instance, reference);
        }

        public string TranslateDashboardFolder(string instance, string reference)
        {
            return RepositoryTranslate("DashboardFolder", instance, reference);
        }

        public string TranslateDashboardName(string instance, string reference)
        {
            return RepositoryTranslate("DashboardName", instance, reference);
        }

        public string TranslateDashboardItemName(string instance, string reference)
        {
            return RepositoryTranslate("DashboardItemName", instance, reference);
        }

        public string TranslateDashboardItemGroupName(string instance, string reference)
        {
            return RepositoryTranslate("DashboardItemGroupName", instance, reference);
        }

        public string TranslateWidgetName(string instance, string reference)
        {
            return RepositoryTranslate("WidgetName", instance, reference);
        }

        public string TranslateWidgetDescription(string instance, string reference)
        {
            return RepositoryTranslate("WidgetDescription", instance, reference);
        }

        public string TranslateFolderPath(string path)
        {
            if (path == "\\") return path;

            string path2 = path;
            string result = "";
            while (!string.IsNullOrEmpty(path2) && path2 != "\\")
            {
                result = "\\" + TranslateFolderName(path2) + result;
                path2 = Path.GetDirectoryName(path2);
            }
            return result;
        }

        public string TranslateFolderName(string path)
        {
            return RepositoryTranslate("FolderName", path.StartsWith(ReportsFolder) ? path.Substring(ReportsFolder.Length) : path, Path.GetFileName(path));
        }

        public string TranslateFileName(string path)
        {
            return RepositoryTranslate("FileName", path.StartsWith(ReportsFolder) ? path.Substring(ReportsFolder.Length) : path, Path.GetFileNameWithoutExtension(path));
        }

#if DEBUG
        static public List<RepositoryTranslation> UnkownTranslations = new List<RepositoryTranslation>();

        public void FlushTranslationUsage()
        {
            if (Translations.Count > 0)
            {
                if (UnkownTranslations.Count > 0)
                {
                    Debug.WriteLine("\r\nUnknown translations, you should complete the Translations.csv file with:");
                    string context = "";
                    foreach (var translation in UnkownTranslations.OrderBy(i => i.Context))
                    {
                        if (context != translation.Context)
                        {
                            context = translation.Context;
                            Debug.WriteLine(string.Format("For Context -> {0}", translation.Context));
                        }
                        Debug.WriteLine(string.Format("{0}", translation.Reference));
                    }
                }
                Debug.WriteLine("\r\nTranslations usage: consider to remove translations not used from the Translations.csv file:");
                foreach (var translation in Translations.Values.OrderBy(i => i.Usage)) Debug.WriteLine(string.Format("Used {0} time(s): (Context:{1}) {2}", translation.Usage, translation.Context, translation.Reference));
            }
        }
#endif

        public string Translate(string culture, string context, string reference)
        {
            string result = reference;
            try
            {
                var key = context + "\r" + reference;
                RepositoryTranslation myTranslation = Translations.ContainsKey(key) ? Translations[key] : null;
                if (myTranslation != null)
                {
#if DEBUG
                    myTranslation.Usage++;
#endif
                    if (!string.IsNullOrEmpty(culture) && myTranslation.Translations.ContainsKey(culture))
                    {
                        result = myTranslation.Translations[culture];
                        if (string.IsNullOrEmpty(result)) result = reference;
                    }
                }
                else
                {
#if DEBUG
                    if (!UnkownTranslations.Exists(i => i.Context == context && i.Reference == reference)) UnkownTranslations.Add(new RepositoryTranslation() { Context = context, Reference = reference });
#endif
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result;
        }

        #endregion

        #region Web publishing
        //Web publishing
        public string WebApplicationPath;
        public string ApplicationPath
        {
            get
            {
                return !string.IsNullOrEmpty(WebApplicationPath) ? WebApplicationPath : Path.GetDirectoryName(Application.ExecutablePath);
            }
        }

        #endregion

        #region Helpers
        //Helpers
        public Report FindReport(string folder, string GUID)
        {
            Report result = null;
            foreach (string reportPath in Directory.GetFiles(folder, "*." + SealReportFileExtension))
            {
                try
                {
                    Report report = Report.LoadFromFile(reportPath, this);
                    if (report.GUID == GUID)
                    {
                        result = report;
                        break;
                    }
                }
                catch { }
            }

            if (result == null)
            {
                foreach (string subFolder in Directory.GetDirectories(folder))
                {
                    Report report = FindReport(subFolder, GUID);
                    if (report != null)
                    {
                        result = report;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
