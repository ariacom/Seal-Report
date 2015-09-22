//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RazorEngine;
using System.Xml.Serialization;
using Seal.Helpers;
using System.ComponentModel;
using System.Drawing.Design;
using Seal.Converter;
using DynamicTypeDescriptor;

namespace Seal.Model
{
    public class ReportSource : MetaSource
    {
        public const string DefaultRepositoryConnectionGUID = "1";
        public const string DefaultReportConnectionGUID = "2";

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("ConnectionGUID").SetIsBrowsable(true);

                GetProperty("MetaSourceName").SetIsBrowsable(!string.IsNullOrEmpty(MetaSourceGUID));

                GetProperty("PreSQL").SetIsBrowsable(!IsNoSQL);
                GetProperty("PostSQL").SetIsBrowsable(!IsNoSQL);
                GetProperty("IgnorePrePostError").SetIsBrowsable(!IsNoSQL);
                GetProperty("IsNoSQL").SetIsBrowsable(true);


                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);

                GetProperty("PreSQL").SetIsReadOnly(!string.IsNullOrEmpty(MetaSourceGUID));
                GetProperty("PostSQL").SetIsReadOnly(!string.IsNullOrEmpty(MetaSourceGUID));
                GetProperty("IgnorePrePostError").SetIsReadOnly(!string.IsNullOrEmpty(MetaSourceGUID));
                GetProperty("IsNoSQL").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _metaSourceGUID;
        public string MetaSourceGUID
        {
            get { return _metaSourceGUID; }
            set { _metaSourceGUID = value; }
        }

        private string _metaSourceName;
        [XmlIgnore]
        [Category("General"), DisplayName("Repository Data Source"), Description("The name of the repository data source"), Id(1, 1)]
        public string MetaSourceName
        {
            get { return _metaSourceName; }
        }
        //use to store the default repository connection
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

        [XmlIgnore]
        public override MetaConnection Connection
        {
            get
            {
                if (_connectionGUID == DefaultRepositoryConnectionGUID) return RepositoryConnection;
                return base.Connection;
            }
        }

        static public ReportSource Create(Repository repository, bool createConnection)
        {
            ReportSource result = new ReportSource() { GUID = Guid.NewGuid().ToString(), Name = "Data Source", Repository = repository };
            //Add master table
            MetaTable master = MetaTable.Create();
            master.DynamicColumns = true;
            master.IsEditable = true;
            master.Alias = MetaData.MasterTableName;
            master.Source = result;
            result.MetaData.Tables.Add(master);

            if (createConnection) result.AddDefaultConnection(repository); 

            return result;
        }

        public void LoadRepositoryMetaSources(Repository repository)
        {
            foreach (var connection in Connections)
            {
                connection.IsEditable = true;
            }
            foreach (var table in MetaData.Tables)
            {
                table.IsEditable = true;
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
                    _metaSourceName = source.Name;
                    foreach (var item in source.Connections)
                    {
                        item.IsEditable = false;
                        Connections.Add(item);
                    }
                    foreach (var item in source.MetaData.Tables)
                    {
                        item.IsEditable = false;
                        MetaData.Tables.Add(item);
                    }
                    foreach (var item in source.MetaData.Joins)
                    {
                        item.IsEditable = false;
                        MetaData.Joins.Add(item);
                    }
                    foreach (var item in source.MetaData.Enums)
                    {
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
        }

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
        public List<MetaJoin> TempJoins = new List<MetaJoin>();
        [XmlIgnore]
        public List<MetaEnum> TempEnums = new List<MetaEnum>();    
    }


}
