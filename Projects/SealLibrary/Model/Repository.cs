//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Seal.Helpers;
using RazorEngine;
using System.Xml.Serialization;
using System.Globalization;
using System.Data;
using System.Text.RegularExpressions;
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
        public const string SealTaskScheduler = "SealTaskScheduler.exe";
        public const string SealReportDesigner = "SealReportDesigner.exe";
        public const string SealServerManager = "SealServerManager.exe";
        public const string SealDefaultRepository = "Seal Report Repository";
        public const string SealRepositoryKeyword = "%SEALREPOSITORY%";

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

        List<Theme> _themes = null;
        public List<Theme> Themes
        {
            get
            {
                if (_themes == null)
                {
                    _themes = Theme.LoadThemes(ThemesFolder);
                }
                return _themes;
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

        List<ReportViewTemplate> _viewTemplates = null;
        public List<ReportViewTemplate> ViewTemplates
        {
            get
            {
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(ViewsFolder);
                }
                return _viewTemplates;
            }
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

        public void SetCultureInfo(string cultureName)
        {
            try
            {
                var newCulture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == cultureName);
                if (newCulture != null) _cultureInfo = newCulture;
            }
            catch { }
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
                }
                return _configuration;
            }
            set { _configuration = value; }
        }

        SealSecurity _security = null;
        public SealSecurity Security
        {
            get
            {
                if (_security == null)
                {
                    _security = SealSecurity.LoadFromFile(SecurityPath, true);
                    if (_security == null) _security = new SealSecurity();
                    _security.Repository = this;
                }
                return _security;
            }
            set { _security = value; }
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

        public static Repository Create()
        {
            Repository result = null;

            string path = FindRepository();
            if (Directory.Exists(path))
            {
                result = new Repository();
                result.Init(path);
            }
            if (result == null) throw new Exception(string.Format("Unable to find or create a Repository from '{0}'. Please check your configuration file", Properties.Settings.Default.RepositoryPath));

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
                result._viewTemplates = ViewTemplates;// (List<ReportViewTemplate>)Helper.Clone(ViewTemplates);-> Note that Parameters of ViewTemplates are not cloned correctely
                result._themes = Themes;
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
                _devices.Add(new OutputFolderDevice() { Name = "Folder Device" });
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
                if (!Directory.Exists(ThemesFolder)) Directory.CreateDirectory(ThemesFolder);
                if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
                if (!Directory.Exists(SecurityFolder)) Directory.CreateDirectory(SecurityFolder);
                if (!Directory.Exists(SecurityProvidersFolder)) Directory.CreateDirectory(SecurityProvidersFolder);
                if (!Directory.Exists(AssembliesFolder)) Directory.CreateDirectory(AssembliesFolder);
            }
            catch { }
        }

        public string ViewsFolder
        {
            get { return Path.Combine(_path, "Views"); }
        }

        public string ThemesFolder
        {
            get { return Path.Combine(_path, "Themes"); }
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

        public string ViewImagesFolder
        {
            get { return Path.Combine(ViewsFolder, "Images"); }
        }

        public string ViewScriptsFolder
        {
            get { return Path.Combine(ViewsFolder, "Scripts"); }
        }

        public string AssembliesFolder
        {
            get { return Path.Combine(_path, "Assemblies"); }
        }

        public string TranslationsPath
        {
            get { return Path.Combine(SettingsFolder, "Translations.csv"); }
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
            return inputFolder.Replace(Repository.SealRepositoryKeyword, _path);
        }

        #region Translations

        //Translations, one dictionary per context
        List<RepositoryTranslation> _translations = null;
        public List<RepositoryTranslation> Translations
        {
            get
            {
                if (_translations == null)
                {
                    _translations = new List<RepositoryTranslation>();
                    _translations = RepositoryTranslation.InitFromCSV(TranslationsPath, false);
                }
                return _translations;
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

        public string TranslateReport(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "Report", reference);
        }

        List<RepositoryTranslation> _repositoryTranslations = null;
        public List<RepositoryTranslation> RepositoryTranslations
        {
            get
            {
                if (_repositoryTranslations == null)
                {
                    _repositoryTranslations = new List<RepositoryTranslation>();
                    _repositoryTranslations = RepositoryTranslation.InitFromCSV(RepositoryTranslationsPath, true);
                }
                return _repositoryTranslations;
            }
        }

        public string RepositoryTranslate(string culture, string context, string instance, string reference)
        {
            string result = reference;
            try
            {
                RepositoryTranslation myTranslation = RepositoryTranslations.FirstOrDefault(i => i.Context == context && i.Instance == instance && i.Reference == reference);
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

        public string TranslateElement(ReportElement element, string reference)
        {
            return RepositoryTranslate("Element", element.MetaColumn.Category + '.' + element.DisplayNameEl, reference);
        }

        public string TranslateEnum(string instance, string reference)
        {
            return RepositoryTranslate("Enum", instance, reference);
        }

        public string TranslateFolderName(string path)
        {
            if (path.Length < ReportsFolder.Length) return Path.GetFileName(path);
            return RepositoryTranslate("FolderName", path.Substring(ReportsFolder.Length), Path.GetFileName(path));
        }

        public string TranslateFolderPath(string path)
        {
            if (path.Length < ReportsFolder.Length) return path;
            return RepositoryTranslate("FolderPath", path.Substring(ReportsFolder.Length), path.Substring(ReportsFolder.Length));
        }

        public string TranslateFileName(string path)
        {
            if (path.Length < ReportsFolder.Length) return Path.GetFileName(path);
            return RepositoryTranslate("FileName", path.Substring(ReportsFolder.Length), Path.GetFileNameWithoutExtension(path));
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
                foreach (var translation in Translations.OrderBy(i => i.Usage)) Debug.WriteLine(string.Format("Used {0} time(s): (Context:{1}) {2}", translation.Usage, translation.Context, translation.Reference));
            }
        }
#endif

        public string Translate(string culture, string context, string reference)
        {
            string result = reference;
            try
            {
                RepositoryTranslation myTranslation = Translations.FirstOrDefault(i => i.Context == context && i.Reference == reference);
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
        public string WebPublishFolder;
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

        public string AttachScriptFile(string fileName, string cdnPath = "")
        {
            if (!string.IsNullOrEmpty(cdnPath) && !Configuration.IsLocal) return string.Format("<script type='text/javascript' src='{0}'></script>", cdnPath);
            string result = "<script type='text/javascript'>\r\n";
            string sourceFilePath = Path.Combine(ViewScriptsFolder, fileName);
            result += File.ReadAllText(sourceFilePath);
            result += "\r\n</script>\r\n";
            return result;
        }

        public string AttachCSSFile(string fileName, string cdnPath = "")
        {
            if (!string.IsNullOrEmpty(cdnPath) && Configuration.IsLocal) return string.Format("<link type='text/css' href='{0}' rel='stylesheet'/>", cdnPath);

            string result = "<style type='text/css'>\r\n";
            string sourceFilePath = Path.Combine(ViewScriptsFolder, fileName);
            result += File.ReadAllText(sourceFilePath);
            result += "\r\n</style>\r\n";
            return result;
        }

        public string ConvertToRepositoryPath(string path)
        {
            string result = path.Replace(ReportsFolder, "");
            if (string.IsNullOrEmpty(result)) result = "\\";
            return result;
        }

        #endregion


    }
}
