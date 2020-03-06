using Newtonsoft.Json;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Seal.Model
{
    public class WebServerSchedule
    {
        public SWESchedule Schedule;

        /// <summary>
        /// Current repository of the web schedule
        /// </summary>
        [JsonIgnore]
        public Repository Repository = null;

        /// <summary>
        /// Current file path of the web schedule
        /// </summary>
        [JsonIgnore] 
        public string FilePath = "";

        /// <summary>
        /// Last modification date of the web schedule
        /// </summary>
        [JsonIgnore] 
        public DateTime LastModification;

        /// <summary>
        /// Load a web schedule from a file
        /// </summary>
        static public WebServerSchedule LoadFromFile(string path, Repository repository)
        {
            WebServerSchedule result = null;
            try
            {
                using (var sr = new StreamReader(path))
                {
                    var js = new JsonSerializer();
                    result = js.Deserialize(sr,typeof(WebServerSchedule)) as WebServerSchedule;
                }
                result.FilePath = path;
                result.Repository = repository;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

        /// <summary>
        /// Save the current web schedule to its file
        /// </summary>
        public void SaveToFile()
        {
            using (var sw = new StreamWriter(FilePath))
            {
                var js = new JsonSerializer();
                js.Serialize(sw, this);
            }
        }
    }
}