//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Office2010.Excel;
using Amazon.Runtime.Internal.Transform;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Microsoft.CodeAnalysis;
using SharpCompress.Common;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Text;

#if WINDOWS
using System.Windows.Forms;
using System.Drawing;
#endif

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
        public const string SealConverterDll = "SealConverter.dll";
        public const string SealConverterWinDll = "SealConverterWin.dll";


        //appsettings.json
        public const string SealConfigurationSectionKeyword = "SealConfiguration";
        public const string SealConfigurationRepositoryPathKeyword = "RepositoryPath";
        public const string SealConfigurationMaxWorkingSetKeyword = "MaxWorkingSet";

        //core installation subdirectory
        public const string CoreInstallationSubDirectory = "Core";

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

        /// <summary>
        /// The SealTaskScheduler executable full path
        /// </summary>
        public string SealTaskSchedulerPath
        {
            get
            {
#if DEBUG
                return Path.Combine(@"C:\_dev\Seal-Report\Projects\SealTaskScheduler\bin\Debug\net8.0", SealTaskScheduler);
#else
                return Path.Combine(Configuration.InstallationDirectory + "\\" + CoreInstallationSubDirectory, SealTaskScheduler);
#endif
            }
        }

#if WINDOWS
        /// <summary>
        /// Product icon
        /// </summary>
        public static Icon ProductIcon
        {
            get { return IsServerManager ? Properties.Resources.serverManager : Properties.Resources.reportDesigner; }
        }


        /// <summary>
        /// Is Server Manager
        /// </summary>
        public static bool IsServerManager = false;

#endif

        public bool LicenseInvalid = false;

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
                    _licenseText = "";
                    //Get current licenses
                    string licensePath = Path.Combine(Instance.SettingsFolder, "License.srl");
                    _licenseText = Helper.GetLicenseText(licensePath, out bool licenseInvalid);
                    LicenseInvalid = licenseInvalid;
                }
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

        void initCultureSeparators()
        {
            if (!string.IsNullOrEmpty(Configuration.NumberGroupSeparator)) _cultureInfo.NumberFormat.NumberGroupSeparator = Configuration.NumberGroupSeparator;
            if (!string.IsNullOrEmpty(Configuration.NumberDecimalSeparator)) _cultureInfo.NumberFormat.NumberDecimalSeparator = Configuration.NumberDecimalSeparator;
            if (!string.IsNullOrEmpty(Configuration.DateSeparator)) _cultureInfo.DateTimeFormat.DateSeparator = Configuration.DateSeparator;
            if (!string.IsNullOrEmpty(Configuration.TimeSeparator)) _cultureInfo.DateTimeFormat.TimeSeparator = Configuration.TimeSeparator;
        }

        CultureInfo _cultureInfo = null;
        /// <summary>
        /// Default CultureInfo
        /// </summary>
        public CultureInfo CultureInfo
        {
            get
            {
                if (_cultureInfo == null)
                {
                    if (!string.IsNullOrEmpty(Configuration.DefaultCulture))
                    {
                        _cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == Configuration.DefaultCulture);
                    }
                    if (_cultureInfo == null)
                    {
                        _cultureInfo = CultureInfo.CurrentCulture;
                        if (_cultureInfo.TwoLetterISOLanguageName == "iv") _cultureInfo = new CultureInfo("en");
                    }
                    _cultureInfo = new CultureInfo(_cultureInfo.Name); //Make the instance writable

                    initCultureSeparators();
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
                    _cultureInfo = new CultureInfo(newCulture.Name);
                    _nvd3Translations = null;
                    _jsTranslations = null;
                    initCultureSeparators();
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
                        _configuration.InitDefaultKeyValues();
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
        public bool UseSealScheduler
        {
            get
            {
                return Configuration.SchedulerMode != SchedulerMode.Windows;
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
            path = Path.GetDirectoryName(typeof(Report).Assembly.Location.Replace("file:///", ""));
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
            if (!Directory.Exists(path) || (!path.StartsWith(@"\\") && path == Path.GetPathRoot(path))) path = RepositoryConfigurationPath;
#endif
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                //Missing repository, try in wwwroot (to easy Azure deployment...)
                path = DefaultRepository;
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
                return "Repository";
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

        public static bool IsInstanceCreated
        {
            get
            {
                return _instance != null;
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
                result._cultureInfo = new CultureInfo(CultureInfo.Name);
                result.initCultureSeparators();
            }
            return result;
        }

        /// <summary>
        /// True is Assemblies have been loaded. Can be set to true at startup to disable Assemblies load.
        /// </summary>
        public static bool AssembliesLoaded = false;

        /// <summary>
        /// True is Dynamic Assemblies have been loaded. Can be set to true at startup to disable Dynamic Assemblies load.
        /// </summary>
        public static bool DynamicAssembliesLoaded = false;

        /// <summary>
        /// Init the repository from a given path
        /// </summary>
        public void Init(string path)
        {
            RepositoryPath = path;
            RepositoryServer.ViewsFolder = ViewsFolder;
            RepositoryServer.TableTemplatesFolder = TableTemplatesFolder;
            RepositoryServer.TaskTemplatesFolder = TaskTemplatesFolder;

            CheckFolders();

            //Alternate temporary directories
            if (!string.IsNullOrEmpty(Configuration.AlternateTempDirectory))
            {
                var tempDir = ReplaceRepositoryKeyword(Configuration.AlternateTempDirectory);
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                RazorEngine.Engine.AlternateTemporaryDirectory = tempDir;
                FileHelper.AlternateTemporaryDirectory = tempDir;
            }

            //Razor cache directory
            if (Configuration.EnableRazorCache)
            {
                RazorHelper.RazorCacheDirectory = Configuration.IsUsingSealLibraryWin ? RazorCacheWinFolder : RazorCacheFolder;
            }

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
                        Helper.WriteLogException("Repository.Init: OutputEmailDevice", ex);
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
                        Helper.WriteLogException("Repository.Init: OutputEmailDevice", ex);
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
                        Helper.WriteLogException("Repository.Init: OutputFileServerDevice", ex);
                    }
                }
            }

            lock (PathLock)
            {
                if (!AssembliesLoaded)
                {
                    //Load extra assemblies defined in Repository
                    var assemblies = Directory.GetFiles(AssembliesFolder, "*.dll");
                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            if (Path.GetFileName(assembly) != SealConverterDll && Path.GetFileName(assembly) != SealConverterWinDll)
                            {
                                Assembly.LoadFrom(assembly);
                            }
                        }
                        catch (Exception ex)
                        {
                            Helper.WriteLogException("Repository.Init: Assemblies", ex);
                        }
                    }

                    //Add this assembly resolve necessary when executing Razor scripts
                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    currentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolve);

                    AssembliesLoaded = true;
                }

                if (!DynamicAssembliesLoaded)
                {
                    //Load extra dynamic assemblies
                    var csFiles = Directory.GetFiles(DynamicsFolder, "*.cs");
                    foreach (var csFile in csFiles)
                    {
                        try
                        {
                            var dllPath = Path.Combine(DynamicsFolder, Path.GetFileNameWithoutExtension(csFile) + ".dll");
                            if (File.GetLastWriteTime(csFile) > File.GetLastWriteTime(dllPath))
                            {
                                //Compile the file and save the dll
                                var code = File.ReadAllText(csFile);
                                // Compile the code
                                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                                var references = AppDomain.CurrentDomain.GetAssemblies()
                                    .Where(a => !a.IsDynamic && a.Location != dllPath)
                                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                                    .Cast<MetadataReference>();

                                var compilation = CSharpCompilation.Create(
                                    Path.GetFileNameWithoutExtension(csFile),
                                    new[] { syntaxTree },
                                    references,
                                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                );

                                var symbolsPath = Path.ChangeExtension(dllPath, "pdb");
                                EmitResult result = compilation.Emit(dllPath, symbolsPath);
                                if (!result.Success)
                                {
                                    var diag = "";
                                    foreach (var diagnostic in result.Diagnostics)
                                    {
                                        diag += diagnostic.ToString() + "\r\n";
                                    }
                                    if (File.Exists(dllPath)) File.Delete(dllPath);

                                    throw new Exception($"Error compiling '{csFile}':\r\n{diag}");
                                }
                            }
                            Assembly.LoadFrom(dllPath);
                        }
                        catch (Exception ex)
                        {
                            Helper.WriteLogException("Repository.Init: DynamicAssemblies", ex);
                        }
                    }

                    DynamicAssembliesLoaded = true;
                }
            }
        }

        Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(AssembliesFolder, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) assemblyPath = Path.Combine(DynamicsFolder, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
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
                if (!Directory.Exists(DynamicsFolder)) Directory.CreateDirectory(DynamicsFolder);
                if (!Directory.Exists(RazorCacheFolder)) Directory.CreateDirectory(RazorCacheFolder);
                if (!Directory.Exists(RazorCacheWinFolder)) Directory.CreateDirectory(RazorCacheWinFolder);
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
        /// Task templates folder
        /// </summary>
        public string TaskTemplatesFolder
        {
            get { return Path.Combine(SourcesFolder, "TaskTemplates"); }
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
                return Path.Combine(RepositoryPath, "Assemblies");
            }
        }

        /// <summary>
        /// Dynamic assemblies folder
        /// </summary>
        public string DynamicsFolder
        {
            get
            {
                return Path.Combine(AssembliesFolder, "Dynamics");
            }
        }

        /// <summary>
        /// Razor cache folder for compiled assemblies
        /// </summary>
        public string RazorCacheFolder
        {
            get
            {
                return Path.Combine(AssembliesFolder, "RazorCache");
            }
        }

        /// <summary>
        /// Razor cache folder for compiled assemblies for Windows applications
        /// </summary>
        public string RazorCacheWinFolder
        {
            get
            {
                return Path.Combine(AssembliesFolder, "RazorCache\\Win");
            }
        }

        /// <summary>
        /// SealConverter assembly path
        /// </summary>
        public string SealConverterPath
        {
            get
            {
#if WINDOWS
                return Path.Combine(AssembliesFolder, "SealConverterWin.dll");
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
            var name = user.GetPersonalFolderName();
            if (Configuration.HostForPersonalFolder && !string.IsNullOrEmpty(user.WebHostName)) name = user.WebHostName + "_" + name;
            name = FileHelper.CleanFilePath(name, "_");
            if (string.IsNullOrEmpty(name)) name = "_";
            string result = Path.Combine(PersonalFolder, name);
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
        /// Send an Email using the first Notification Email Device.
        /// </summary>

        public bool SendNotificationEmail(string from, string to, string subject, bool isHtmlBody, string body)
        {
            var device = Devices.OfType<OutputEmailDevice>().FirstOrDefault(i => i.UsedForNotification);
            if (device == null) device = Devices.OfType<OutputEmailDevice>().FirstOrDefault();
            if (device == null)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "No email device is defined in the repository to send notifications. Please use the Server Manager application to define at least an Email Device.");
            }
            else
            {
                try
                {
                    device.SendEmail(from, to, subject, isHtmlBody, body);
                    return true;
                }
                catch (Exception emailEx)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Error, string.Format("Try 1: Error got trying sending notification email using device '{0}'.\r\n{1}", device.FullName, emailEx.Message + (emailEx.InnerException != null ? "\r\n" + emailEx.InnerException.Message : "")));
                    try
                    {
                        if (body.Length > 10000) body = body.Substring(1, 10000); //Body is perhaps too big
                        device.SendEmail(from, to, subject, isHtmlBody, body);
                        return true;
                    }
                    catch (Exception emailEx2)
                    {
                        Helper.WriteLogEntryScheduler(EventLogEntryType.Error, string.Format("Try 2: Error got trying sending notification email using device '{0}'.\r\n{1}", device.FullName, emailEx2.Message + (emailEx2.InnerException != null ? "\r\n" + emailEx2.InnerException.Message : "")));
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Translations file name pattern
        /// </summary>
        public string TranslationsPattern
        {
            get { return "Translations*.csv"; }
        }

        /// <summary>
        /// Translations file name pattern
        /// </summary>
        public string TranslationsExcelPattern
        {
            get { return "Translations*.xlsx"; }
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

        /// <summary>
        /// All report formats available
        /// </summary>
        public List<ReportFormat> ResultAllFormats
        {
            get {
                bool hasConverter = File.Exists(SealConverterPath);
                var result = new List<ReportFormat>();
                foreach (ReportFormat format in Enum.GetValues(typeof(ReportFormat)))
                {
                    result.Add(format);
                }
                return result;
            }
        }

        /// <summary>
        /// Result report formats allowed by the configuration
        /// </summary>
        public List<ReportFormat> ResultAllowedFormats
        {
            get
            {
                if (Configuration.ReportFormats.Count == 0) return ResultAllFormats;
                return (from f in Configuration.ReportFormats select (ReportFormat) Enum.Parse(typeof(ReportFormat), f)).ToList();
            }
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
                            var excelPath = Path.ChangeExtension(path, "xlsx");
                            if (!File.Exists(excelPath)) RepositoryTranslation.InitFromCSV(_translations, path, false);
                        }
                        foreach (string path in Directory.GetFiles(SettingsFolder, TranslationsExcelPattern))
                        {
                            RepositoryTranslation.InitFromExcel(_translations, path, false);
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

        Dictionary<string, RepositoryTranslation> _repositoryWildCharTranslations = null;

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
                        var excelPath = Path.ChangeExtension(RepositoryTranslationsPath, "xlsx");
                        if (File.Exists(excelPath)) RepositoryTranslation.InitFromExcel(_repositoryTranslations, excelPath, true);
                        else RepositoryTranslation.InitFromCSV(_repositoryTranslations, RepositoryTranslationsPath, true);

                        //Execute script if any
                        if (!string.IsNullOrEmpty(Configuration.RepositoryTranslationsScript))
                        {
                            RazorHelper.CompileExecute(Configuration.RepositoryTranslationsScript, this);
                        }
                    }
                }
                return _repositoryTranslations;
            }
        }

        /// <summary>
        /// Load repository translations from a data table having columns: Context,Instance,Reference,en,fr,etc.
        /// </summary>
        public void LoadRepositoryTranslationsFromDataTable(DataTable dt)
        {
            RepositoryTranslation.InitFromDataTable(RepositoryTranslations, dt, true);        
        }

        /// <summary>
        /// Force reload of repository translations
        /// </summary>
        public void ReloadRepositoryTranslations()
        {
            _repositoryTranslations = null;
        }

        /// <summary>
        /// Find a repository translation from a culture, context, instance and reference
        /// </summary>
        public RepositoryTranslation FindRepositoryTranslation(string context, string instance, string reference)
        {
            RepositoryTranslation result = null;
            if (string.IsNullOrEmpty(reference)) return null;

            try
            {
                var key = context + "\r" + reference + "\r" + instance;
                result = RepositoryTranslations.ContainsKey(key) ? RepositoryTranslations[key] : null;
                if (result == null)
                {
                    //Wild char management
                    if (_repositoryWildCharTranslations == null)
                    {
                        _repositoryWildCharTranslations = new Dictionary<string, RepositoryTranslation>();
                        foreach (var k in RepositoryTranslations.Keys.Where(i => i.Contains('*')))
                        {
                            _repositoryWildCharTranslations.Add(k, RepositoryTranslations[k]);
                        }
                    }

                    foreach (var t in _repositoryWildCharTranslations.Values.Where(i => i.Context == context && i.Reference == reference))
                    {
                        if (Helper.IsMatchWildcard(instance, t.Instance))
                        {
                            result = t;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("FindTranslation", ex);
            }
            return result;
        }

        /// <summary>
        /// Translate a reference text in a repository context
        /// </summary>
        public string RepositoryTranslate(string culture, string context, string instance, string reference)
        {
            string result = null;
            try
            {
                RepositoryTranslation myTranslation = FindRepositoryTranslation(context, instance, reference);
                if (!string.IsNullOrEmpty(culture) && myTranslation != null)
                {
                    if (myTranslation.Translations.ContainsKey(culture))
                    {
                        result = myTranslation.Translations[culture];
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("RepositoryTranslate", ex);
            }
            if (string.IsNullOrEmpty(result)) result = reference;
            if (string.IsNullOrEmpty(result)) result = "";
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
        /// Translate a full category path
        /// </summary>
        public string TranslateCategoryPath(string instance)
        {
            string result = "", current = "";
            string[] categories = instance.Split('/');
            foreach (var category in categories)
            {
                current += (!string.IsNullOrEmpty(current) ? "/" : "") +category;
                result += (!string.IsNullOrEmpty(result) ? "/" : "") + TranslateCategory(current, category);
            }
            return result;
        }

        /// <summary>
        /// Translate a Device
        /// </summary>
        public string TranslateDevice(string instance, string reference)
        {
            return RepositoryTranslate("Device", instance, reference);
        }

        /// <summary>
        /// Translate a Connection
        /// </summary>
        public string TranslateConnection(MetaConnection connection)
        {
            return RepositoryTranslate("Connection", connection.Source.Name + '.' + connection.Name, connection.Name);
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
        /// Translate a folder name
        /// </summary>
        public string TranslateFolderName(string path)
        {
            return RepositoryTranslate("FolderName", path.StartsWith(ReportsFolder) ? path.Substring(ReportsFolder.Length) : path, Path.GetFileName(path));
        }

        /// <summary>
        /// Translate a file name
        /// </summary>
        public string TranslateFileName(string path)
        {
            return RepositoryTranslate("FileName", path.StartsWith(ReportsFolder) ? path.Substring(ReportsFolder.Length) : path, Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// Translate a report display name
        /// </summary>
        public string TranslateReportDisplayName(string instance, string name)
        {
            return RepositoryTranslate("ReportDisplayName", instance, name);
        }

#if DEBUG
        static public List<RepositoryTranslation> UnkownTranslations = new List<RepositoryTranslation>();

        public void FlushTranslationUsage()
        {
            /*
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
            }*/
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

        #region Encryption keys

        /// <summary>
        /// Decrypt a value using the key name and the encryption mode
        /// </summary>
        public string DecryptValue(string value, string keyName, bool useAES = false)
        {
            return Configuration.DecryptValue(value, keyName, useAES);
        }

        /// <summary>
        /// Encrypt a value using the key name and the encryption mode
        /// </summary>
        public string EncryptValue(string value, string keyName, bool useAES = false)
        {
            return Configuration.EncryptValue(value, keyName, useAES);
        }

        /// <summary>
        /// Get an application key or password
        /// </summary>
        public string GetApplicationKey(string keyName)
        {
            return Configuration.GetApplicationKey(keyName);
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
