//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Model;

namespace Seal.Forms
{
    public interface IEntityHandler
    {
        void SetModified();
        void InitEntity(object entity);
        void EditSchedule(ReportSchedule schedule);
        void RefreshModelTreeView();
    }
}
