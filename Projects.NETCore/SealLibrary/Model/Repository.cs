//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
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
using System.Reflection;
using System.Drawing;

namespace Seal.Model
{
    /// <summary>
    /// The Repository class defines all the default values, configurations and security of the application and contains the current MetaSource and OutputDevice objects .
    /// </summary>
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
        public const string SealReportsRepositoryKeyword = "%SEALREPORTSREPOSITORY%";
        public const string SealPersonalRepositoryKeyword = "%SEALPERSONALREPOSITORY%";
        public const string SealReportDisplayNameKeyword = "%SEALREPORTDISPLAYNAME%";
        public const string SealReportDisplayNameKeyword2 = "{Report Name}";
        public const string CommonRestrictionKeyword = "{CommonRestriction_";
        public const string CommonValueKeyword = "{CommonValue_";
        public const string EnumFilterKeyword = "{EnumFilter";
        public const string EnumValuesKeyword = "{EnumValues_";
        public const string JoinAutoName = "<AutomaticJoinName>";

        public static object PathLock = new object();

        public const string SealWebPublishTemp = "temp";

        /// <summary>
        /// Repository path got from the configuration file
        /// </summary>
        public static string RepositoryConfigurationPath = "";

        /// <summary>
        /// Current path
        /// </summary>
        public string RepositoryPath { get; private set; }

        /// <summary>
        /// Product version
        /// </summary>
        public static string ProductVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

#if !NETCOREAPP
        /// <summary>
        /// Product icon
        /// </summary>
        public static Icon ProductIcon
        {
            get { return Path.GetFileName(Application.ExecutablePath).ToLower() == SealServerManager.ToLower() ? Properties.Resources.serverManager : Properties.Resources.reportDesigner; }
        }
#endif

        private string _licenseText = null;
        /// <summary>
        /// License text
        /// </summary>
        public string LicenseText
        {
            get
            {
                if (_licenseText == null)
                {
                    var converter = SealPdfConverter.Create();
                    _licenseText = converter.GetLicenseText();
                }

                if (_licenseText == null) _licenseText = "";
                return _licenseText;
            }
        }

        /// <summary>
        /// List of MetaSource in the repository
        /// </summary>
        public List<MetaSource> Sources { get; private set; } = new List<MetaSource>();

        /// <summary>
        /// List of OutputDevice in the repository
        /// </summary>
        public List<OutputDevice> Devices { get; private set; } = new List<OutputDevice>();


        CultureInfo _cultureInfo = null;
        /// <summary>
        /// Default CultureInfo
        /// </summary>
        public CultureInfo CultureInfo
        {
            get
            {

                if (_cultureInfo == null && !string.IsNullOrEmpty(Configuration.DefaultCulture)) _cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == Configuration.DefaultCulture);
                if (_cultureInfo == null)
                {
                    _cultureInfo = CultureInfo.CurrentCulture;
                    if (_cultureInfo.TwoLetterISOLanguageName == "iv") _cultureInfo = new CultureInfo("en");
                }

                return _cultureInfo;
            }
        }

        /// <summary>
        /// Short Date format for MomentJS
        /// </summary>
        public string MomentJSShortDateFormat
        {
            get
            {
                return Helper.ToMomentJSFormat(CultureInfo, CultureInfo.DateTimeFormat.ShortDatePattern);
            }
        }

        /// <summary>
        /// Short Date Time format for MomentJS
        /// </summary>
        public string MomentJSShortDateTimeFormat
        {
            get
            {
                return Helper.ToMomentJSFormat(CultureInfo, CultureInfo.DateTimeFormat.ShortDatePattern + ' ' + CultureInfo.DateTimeFormat.LongTimePattern);
            }
        }

        /// <summary>
        /// Set culture from a name, returns true if the change is done.
        /// </summary>
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
        /// <summary>
        /// Current configuration
        /// </summary>
        public SealServerConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = SealServerConfiguration.LoadFromFile(ConfigurationPath, true);
                    if (_configuration == null)
                    {
                        _configuration = new SealServerConfiguration();
                    }
                    _configuration.Repository = this;
                }
                return _configuration;
            }
            set
            {
                _configuration = value;
            }
        }

        /// <summary>
        /// True if the Seal Scheduler is used instead of the Windows Tasks Scheduler
        /// </summary>
        public bool UseWebScheduler
        {
            get
            {
                return Configuration.UseSealScheduler;
            }
        }

        /// <summary>
        /// Forces a configuration reload
        /// </summary>
        public void ReloadConfiguration()
        {
            _configuration = null;
        }

        SealSecurity _security = null;

        /// <summary>
        /// Current security
        /// </summary>
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

        /// <summary>
        /// Forces a security reload for Web
        /// </summary>
        public void ReloadSecurity()
        {
            _security = null;
        }

        static string FindDebugRepository(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 2) return null;

            string result = Path.Combine(Path.GetDirectoryName(path), "Repository");
            if (Directory.Exists(result)) return result;
            return null;
        }


        /// <summary>
        /// Find the current repository path (normally defined in the .config file)
        /// </summary>
        /// <returns></returns>
        public static string FindRepository()
        {
            string path = RepositoryConfigurationPath;
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

            if (!Directory.Exists(path) || path == Path.GetPathRoot(path)) path = "";
#endif
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                //Missing repository, try in wwwroot (to easy Azure deployment...)
                path = Repository.DefaultRepository;
                if (!Directory.Exists(path) || path == Path.GetPathRoot(path))
                {
                    //Set default
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SealDefaultRepository);
                }
            }
            return path;
        }

        public static string DefaultRepository
        {
            get
            {
#if !NETCOREAPP
                return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""))), "Repository");
#else
                return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file://", "")), "Repository");
#endif
            }
        }


        static Repository _instance = null;
        /// <summary>
        /// A general static instance of the repository
        /// </summary>
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

        /// <summary>
        /// Force a reload of the repository Instance
        /// </summary>
        public static Repository ReloadInstance()
        {
            {
                _instance = Create();
                return Instance;
            }
        }

        /// <summary>
        /// Creates a basic repository
        /// </summary>
        public static Repository Create()
        {
            Repository result = null;

            string path = FindRepository();
            if (Directory.Exists(path))
            {
                result = new Repository();
                result.Init(path);

                //Assign default instance
                if (_instance == null) _instance = result;
            }
            if (result == null) throw new Exception(string.Format("Unable to find or create a Repository from '{0}'. Please check your configuration file or copy your repository files in '{1}'.", RepositoryConfigurationPath, Repository.DefaultRepository));

            return result;
        }

        /// <summary>
        /// Creates a basic repository: properties are shared or cloned for performance reasons
        /// </summary>
        public Repository CreateFast()
        {
            //check if some files have changed, in this case -> full reload
            if (MustReload()) return Create();

            //Fast load
            Repository result = null;
            string path = FindRepository();
            if (Directory.Exists(path))
            {
                result = new Repository();
                //Just clone the sources as connections and enums can be updated
                result.Sources = (List<MetaSource>)Helper.Clone(Sources);
                foreach (var source in result.Sources) source.InitReferences(result);
                //Others collections should remain static/unchanged an can be shared...
                result._translations = Translations;
                result.Devices = Devices;
                result._configuration = Configuration;
                result.RepositoryPath = path;
                //plus defaults...
                result._cultureInfo = CultureInfo;
            }
            return result;
        }

        static bool _assembliesLoaded = false;
        /// <summary>
        /// Init the repository from a given path
        /// </summary>
        public void Init(string path)
        {
            RepositoryPath = path;
            RepositoryServer.ViewsFolder = ViewsFolder;
            RepositoryServer.TableTemplatesFolder = TableTemplatesFolder;

            CheckFolders();
            //Data sources
            if (Sources.Count == 0)
            {
                foreach (var file in Directory.GetFiles(SourcesFolder, "*." + SealConfigurationFileExtension))
                {
                    try
                    {
                        var source = MetaSource.LoadFromFile(file);
                        Sources.Add(source);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                foreach (var source in Sources)
                {
                    source.InitReferences(this);
                }
            }

            if (Devices.Count == 0)
            {
                //Devices, add a default folder device, then the other devices
                Devices.Add(OutputFolderDevice.Create());
                foreach (var file in Directory.GetFiles(DevicesEmailFolder, "*." + SealConfigurationFileExtension))
                {
                    try
                    {
                        Devices.Add(OutputEmailDevice.LoadFromFile(file, true));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                foreach (var file in Directory.GetFiles(DevicesFileServerFolder, "*." + SealConfigurationFileExtension))
                {
                    try
                    {
                        Devices.Add(OutputFileServerDevice.LoadFromFile(file, true));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
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


        /// <summary>
        /// True if the configuration or security file has been modified
        /// </summary>
        public bool MustReload()
        {
            if (Configuration.FilePath != null && File.GetLastWriteTime(Configuration.FilePath) != Configuration.LastModification) return true;
            if (Security.FilePath != null && File.GetLastWriteTime(Security.FilePath) != Security.LastModification) return true;
            foreach (var item in Sources) if (File.GetLastWriteTime(item.FilePath) != item.LastModification) return true;
            foreach (var item in Devices.Where(i => i is OutputEmailDevice)) if (item.FilePath != null && File.GetLastWriteTime(item.FilePath) != ((OutputEmailDevice)item).LastModification) return true;
            //Check for new files
            foreach (var file in Directory.GetFiles(SourcesFolder, "*." + SealConfigurationFileExtension))
            {
                if (!Sources.Exists(i => i.FilePath == file)) return true;
            }
            foreach (var file in Directory.GetFiles(DevicesEmailFolder, "*." + SealConfigurationFileExtension))
            {
                if (!Devices.Exists(i => i.FilePath == file)) return true;
            }
            foreach (var file in Directory.GetFiles(DevicesFileServerFolder, "*." + SealConfigurationFileExtension))
            {
                if (!Devices.Exists(i => i.FilePath == file)) return true;
            }

            return false;
        }

        /// <summary>
        /// Check repository folders and create them if necessary
        /// </summary>
        public void CheckFolders()
        {
            try
            {
                if (!Directory.Exists(ViewsFolder)) Directory.CreateDirectory(ViewsFolder);
                if (!Directory.Exists(SourcesFolder)) Directory.CreateDirectory(SourcesFolder);
                if (!Directory.Exists(DevicesFolder)) Directory.CreateDirectory(DevicesFolder);
                if (!Directory.Exists(DevicesEmailFolder)) Directory.CreateDirectory(DevicesEmailFolder);
                if (!Directory.Exists(DevicesFileServerFolder)) Directory.CreateDirectory(DevicesFileServerFolder);
                if (!Directory.Exists(ReportsFolder)) Directory.CreateDirectory(ReportsFolder);
                if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
                if (!Directory.Exists(SecurityFolder)) Directory.CreateDirectory(SecurityFolder);
                if (!Directory.Exists(SecurityProvidersFolder)) Directory.CreateDirectory(SecurityProvidersFolder);
                if (!Directory.Exists(AssembliesFolder)) Directory.CreateDirectory(AssembliesFolder);
                if (!Directory.Exists(SubReportsFolder)) Directory.CreateDirectory(SubReportsFolder);
                if (!Directory.Exists(SpecialsFolder)) Directory.CreateDirectory(SpecialsFolder);
                if (!Directory.Exists(PersonalFolder)) Directory.CreateDirectory(PersonalFolder);
                if (!Directory.Exists(SchedulesFolder)) Directory.CreateDirectory(SchedulesFolder);
            }
            catch { }
        }

        /// <summary>
        /// Views folder
        /// </summary>
        public string ViewsFolder
        {
            get { return Path.Combine(RepositoryPath, "Views"); }
        }

        /// <summary>
        /// Sources folder
        /// </summary>
        public string SourcesFolder
        {
            get { return Path.Combine(RepositoryPath, "Sources"); }
        }

        /// <summary>
        /// Table templates folder
        /// </summary>
        public string TableTemplatesFolder
        {
            get { return Path.Combine(SourcesFolder, "TableTemplates"); }
        }


        /// <summary>
        /// Devices folder
        /// </summary>
        public string DevicesFolder
        {
            get { return Path.Combine(RepositoryPath, "Devices"); }
        }

        /// <summary>
        /// Devices Email folder
        /// </summary>
        public string DevicesEmailFolder
        {
            get { return Path.Combine(RepositoryPath, string.Format("Devices{0}Email", Path.DirectorySeparatorChar)); }
        }

        /// <summary>
        /// Devices File Server folder
        /// </summary>
        public string DevicesFileServerFolder
        {
            get { return Path.Combine(RepositoryPath, string.Format("Devices{0}FileServer", Path.DirectorySeparatorChar)); }
        }

        /// <summary>
        /// Settings folder
        /// </summary>
        public string SettingsFolder
        {
            get { return Path.Combine(RepositoryPath, "Settings"); }
        }

        /// <summary>
        /// Security folder
        /// </summary>
        public string SecurityFolder
        {
            get { return Path.Combine(RepositoryPath, "Security"); }
        }

        /// <summary>
        /// Security Providers folder
        /// </summary>
        public string SecurityProvidersFolder
        {
            get { return Path.Combine(SecurityFolder, "Providers"); }
        }

        /// <summary>
        /// Reports folder
        /// </summary>
        public string ReportsFolder
        {
            get { return Path.Combine(RepositoryPath, "Reports"); }
        }

        /// <summary>
        /// Logs folder
        /// </summary>
        public string LogsFolder
        {
            get { return Path.Combine(RepositoryPath, "Logs"); }
        }

        /// <summary>
        /// View Images folder
        /// </summary>
        public string ViewImagesFolder
        {
            get { return Path.Combine(ViewsFolder, "Images"); }
        }

        /// <summary>
        /// View Scripts folder
        /// </summary>
        public string ViewScriptsFolder
        {
            get { return Path.Combine(ViewsFolder, "Scripts"); }
        }

        /// <summary>
        /// View Content folder
        /// </summary>
        public string ViewContentFolder
        {
            get { return Path.Combine(ViewsFolder, "Content"); }
        }

        /// <summary>
        /// Assemblies folder
        /// </summary>
        public string AssembliesFolder
        {
            get
            {
#if NETCOREAPP
                return Path.Combine(RepositoryPath, Path.Combine("Assemblies","NETCore")); 
#else
                return Path.Combine(RepositoryPath, "Assemblies");
#endif
            }
        }

        /// <summary>
        /// SealConverter assembly path
        /// </summary>
        public string SealConverterPath
        {
            get
            {
#if NETCOREAPP
                return Path.Combine(AssembliesFolder, "SealConverter_NetCore.dll");
#else
                return Path.Combine(AssembliesFolder, "SealConverter.dll");
#endif
            }
        }

        /// <summary>
        /// SubReports folder
        /// </summary>
        public string SubReportsFolder
        {
            get { return Path.Combine(RepositoryPath, "SubReports"); }
        }

        /// <summary>
        /// SpecialFolders folder
        /// </summary>
        public string SpecialsFolder
        {
            get { return Path.Combine(RepositoryPath, "SpecialFolders"); }
        }

        /// <summary>
        /// SpecialsFolder Personal folder
        /// </summary>
        public string PersonalFolder
        {
            get { return Path.Combine(SpecialsFolder, "Personal"); }
        }

        /// <summary>
        /// SpecialsFolder Schedules folder
        /// </summary>
        public string SchedulesFolder
        {
            get { return Path.Combine(SpecialsFolder, "Schedules"); }
        }

        /// <summary>
        /// Returns the personal folder path of a user
        /// </summary>
        public string GetPersonalFolder(SecurityUser user)
        {
            //add hash to the end of the name
            var name = user.GetPersonalFolderName();
            var hash = Math.Abs(Helper.CalculateHash(name));
            string result = Path.Combine(PersonalFolder, string.Format("{0}_{1}", FileHelper.CleanFilePath(name), hash));
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            return result;
        }

        /// <summary>
        /// Returns the personal folder name of a user
        /// </summary>
        public string GetPersonalFolderName(SecurityUser user)
        {
            return TranslateWeb("Personal") + string.Format(" ({0})", user.GetPersonalFolderName()); ;
        }

        /// <summary>
        /// Translations file name pattern
        /// </summary>
        public string TranslationsPattern
        {
            get { return "Translations*.csv"; }
        }

        /// <summary>
        /// Repository translations file name
        /// </summary>
        public string RepositoryTranslationsPath
        {
            get { return Path.Combine(SettingsFolder, "RepositoryTranslations.csv"); }
        }

        /// <summary>
        /// Configuration file path
        /// </summary>
        public string ConfigurationPath
        {
            get { return Path.Combine(SettingsFolder, "Configuration.xml"); }
        }

        /// <summary>
        /// Security file path
        /// </summary>
        public string SecurityPath
        {
            get { return Path.Combine(SecurityFolder, "Security.xml"); }
        }

        /// <summary>
        /// Replace the repository keywords in a string
        /// </summary>
        public string ReplaceRepositoryKeyword(string inputFolder)
        {
            if (string.IsNullOrEmpty(inputFolder)) return "";
            return inputFolder.Replace(Repository.SealRepositoryKeyword, RepositoryPath).Replace(SealPersonalRepositoryKeyword, PersonalFolder).Replace(SealReportsRepositoryKeyword, ReportsFolder);
        }

        #region Translations and Cultures

        //Translations, one dictionary per context
        Dictionary<string, RepositoryTranslation> _translations = null;

        /// <summary>
        /// Current translations
        /// </summary>
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
        /// <summary>
        /// Current JavaScript translations
        /// </summary>
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
        /// <summary>
        /// Current NVD3 translations (for NVD3 charts)
        /// </summary>
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


        public List<string> GetInstalledTranslationLanguages()
        {
            var result = new List<string>();
            if (Translations.Count > 0)
            {
                result = Translations.Values.First().Translations.Keys.ToList();
            }
            return result;
        }

        /// <summary>
        /// Translate a reference text in a context
        /// </summary>
        public string Translate(string context, string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, context, reference);
        }

        /// <summary>
        /// Translate a reference text in a Web context
        /// </summary>
        public string TranslateWeb(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "Web", reference);
        }

        /// <summary>
        /// Translate a reference text in a WebJS context
        /// </summary>
        public string TranslateWebJS(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "WebJS", reference);
        }

        /// <summary>
        /// Translate a reference text in a Report context
        /// </summary>
        public string TranslateReport(string reference)
        {
            return Translate(CultureInfo.TwoLetterISOLanguageName, "Report", reference);
        }

        /// <summary>
        /// Translate a reference text in a Report context for JavaScript use
        /// </summary>
        public string TranslateReportToJS(string reference)
        {
            return Helper.ToJS(Translate(CultureInfo.TwoLetterISOLanguageName, "Report", reference));
        }

        Dictionary<string, RepositoryTranslation> _repositoryTranslations = null;
        /// <summary>
        /// Current repository translations
        /// </summary>
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

        /// <summary>
        /// Translate a reference text in a repository context
        /// </summary>
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

        /// <summary>
        /// Translate a reference text in a repository context with the current culture
        /// </summary>
        public string RepositoryTranslate(string context, string instance, string reference)
        {
            return RepositoryTranslate(CultureInfo.TwoLetterISOLanguageName, context, instance, reference);
        }

        /// <summary>
        /// Translate an Element
        /// </summary>
        public string TranslateColumn(MetaColumn col)
        {
            return RepositoryTranslate("Element", col.Category + '.' + col.DisplayName, col.DisplayName);
        }

        /// <summary>
        /// Translate a Category
        /// </summary>
        public string TranslateCategory(string instance, string reference)
        {
            return RepositoryTranslate("Category", instance, reference);
        }

        /// <summary>
        /// Translate a Device
        /// </summary>
        public string TranslateDevice(string instance, string reference)
        {
            return RepositoryTranslate("Device", instance, reference);
        }

        /// <summary>
        /// Translate a full Folder Path
        /// </summary>
        public string TranslateFolderPath(string path)
        {
            if (path == Path.DirectorySeparatorChar.ToString()) return path;

            string path2 = path;
            string result = "";
            while (!string.IsNullOrEmpty(path2) && path2 != Path.DirectorySeparatorChar.ToString())
            {
                result = Path.DirectorySeparatorChar.ToString() + TranslateFolderName(path2) + result;
                path2 = Path.GetDirectoryName(path2);
            }
            return result;
        }

        /// <summary>
        /// Translate a Folder^Name
        /// </summary>
        public string TranslateFolderName(string path)
        {
            return RepositoryTranslate("FolderName", path.StartsWith(ReportsFolder) ? path.Substring(ReportsFolder.Length) : path, Path.GetFileName(path));
        }

        /// <summary>
        /// Translate a column name
        /// </summary>
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

        /// <summary>
        /// Translate a reference text
        /// </summary>
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

        #region Helpers
        //Helpers
        /// <summary>
        /// Find and load report form its identifier
        /// </summary>
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

