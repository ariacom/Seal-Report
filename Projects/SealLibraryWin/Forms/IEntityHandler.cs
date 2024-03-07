//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Windows.Forms;

namespace Seal.Forms
{
    public interface IEntityHandler
    {
        bool IsInitialized();
        void SetModified();
        void InitEntity(object entity);
        void RefreshModelTreeView();
        void UpdateModelNode(TreeNode currentNode = null);
    }
}
