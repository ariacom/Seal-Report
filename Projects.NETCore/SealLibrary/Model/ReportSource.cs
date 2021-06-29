//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Seal.Model
{
    /// <summary>
    /// A ReportSource is a MetaSource dedicated for a report
    /// </summary>
    public class ReportSource : MetaSource
    {
        public const string DefaultRepositoryConnectionGUID = "1";
        public const string DefaultReportConnectionGUID = "2";


        /// <summary>
        /// Unique identifier of the source
        /// </summary>
        public string MetaSourceGUID { get; set; }

        private string _metaSourceName;
        /// <summary>
        /// Name
        /// </summary>
        [XmlIgnore]
        public string MetaSourceName { 
            get
            {
                if (string.IsNullOrEmpty(_metaSourceName) && !string.IsNullOrEmpty(MetaSourceGUID))
                {
                    var metaSource = Repository.Sources.FirstOrDefault(i => i.GUID == MetaSourceGUID);
                    if (metaSource != null) _metaSourceName = metaSource.Name;
                }
                return _metaSourceName;
            }
        }

        /// <summary>
        /// Reference to the default repository connection
        /// </summary>
        [XmlIgnore]
        public MetaConnection RepositoryConnection
        {
            get
            {
                MetaConnection result = null;
                if (!string.IsNullOrEmpty(MetaSourceGUID) && Repository != null)
                {
                    MetaSource source = Repository.Sources.FirstOrDefault(i => i.GUID == MetaSourceGUID);
                    if (source != null) result = source.Connection;
                }
                return result;
            }
        }

        /// <summary>
        /// Current connection
        /// </summary>
        [XmlIgnore]
        public override MetaConnection Connection
        {
            get
            {
                if (_connectionGUID == DefaultRepositoryConnectionGUID) return RepositoryConnection;
                return base.Connection;
            }
        }

        /// <summary>
        /// Creates a basic ReportSource
        /// </summary>
        static public ReportSource Create(Repository repository, bool createConnection)
        {
            ReportSource result = new ReportSource() { GUID = Guid.NewGuid().ToString(), Name = "Data Source", Repository = repository };

            if (createConnection) result.AddDefaultConnection(repository);

            return result;
        }

        /// <summary>
        /// True if the source has been initialized from the repository
        /// </summary>
        [XmlIgnore]
        public bool Loaded = false;

        /// <summary>
        /// Load the available MetaSources defined in the repository
        /// </summary>
        public void LoadRepositoryMetaSources(Repository repository)
        {
            if (Loaded) return;

            foreach (var connection in Connections)
            {
                connection.IsEditable = true;
            }
            foreach (var table in MetaData.Tables)
            {
                table.IsEditable = true;
            }
            foreach (var link in MetaData.TableLinks)
            {
                link.IsEditable = true;
            }
            foreach (var join in MetaData.Joins)
            {
                join.IsEditable = true;
            }
            foreach (var itemEnum in MetaData.Enums)
            {
                itemEnum.IsEditable = true;
            }

            if (!string.IsNullOrEmpty(MetaSourceGUID))
            {
                MetaSource source = repository.Sources.FirstOrDefault(i => i.GUID == MetaSourceGUID);
                if (source != null)
                {
                    IsDefault = source.IsDefault;
                    IsNoSQL = source.IsNoSQL;
                    InitScript = source.InitScript;
                    _metaSourceName = source.Name;
                    foreach (var item in source.Connections)
                    {
                        item.Source = source;
                        item.IsEditable = false;
                        Connections.Add(item);
                    }
                    foreach (var item in source.MetaData.Tables)
                    {
                        item.Source = source;
                        item.IsEditable = false;
                        MetaData.Tables.Add(item);
                    }
                    foreach (var item in source.MetaData.TableLinks)
                    {
                        item.IsEditable = false;
                        MetaData.TableLinks.Add(item);
                    }
                    foreach (var item in source.MetaData.Joins)
                    {
                        item.Source = source;
                        item.IsEditable = false;
                        MetaData.Joins.Add(item);
                    }
                    foreach (var item in source.MetaData.Enums)
                    {
                        item.Source = source;
                        item.IsEditable = false;
                        MetaData.Enums.Add(item);
                    }

                    PreSQL = source.PreSQL;
                    PostSQL = source.PostSQL;

                    IgnorePrePostError = source.IgnorePrePostError;
                }
                else
                {
                    Report.LoadErrors += string.Format("Unable to find repository source for '{0}' (GUID {1}). Check the data source files in the repository folder.\r\n", Name, MetaSourceGUID);
                }
            }

            if (Connections.Count == 0)
            {
                Connections.Add(MetaConnection.Create(this));
                ConnectionGUID = Connections[0].GUID;
            }

            Loaded = true;
        }

        /// <summary>
        /// Refresh the enumerated list values
        /// </summary>
        public void RefreshEnumsOnDbConnection()
        {
            foreach (var itemEnum in MetaData.Enums.Where(i => i.IsDbRefresh))
            {
                itemEnum.RefreshEnum();
            }
        }

        //Temporary variables to help for report serialization...
        [XmlIgnore]
        public List<MetaConnection> TempConnections = new List<MetaConnection>();
        [XmlIgnore]
        public List<MetaTable> TempTables = new List<MetaTable>();
        [XmlIgnore]
        public List<MetaTableLink> TempLinks = new List<MetaTableLink>();
        [XmlIgnore]
        public List<MetaJoin> TempJoins = new List<MetaJoin>();
        [XmlIgnore]
        public List<MetaEnum> TempEnums = new List<MetaEnum>();
    }

}

