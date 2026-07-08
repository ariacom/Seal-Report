//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
