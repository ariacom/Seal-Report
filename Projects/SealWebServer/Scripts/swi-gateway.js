var _sealServer = WebApplicationName;
var _errorServer = SWIUtil.tr("Unexpected error on server") + ": '" + _sealServer + "'";
function callbackHandler(data, callback, errorcb) {
    if (!data.error) {
        if (callback)
            callback(data);
    }
    else {
        if (errorcb)
            errorcb(data);
        else {
            SWIUtil.ShowMessage("alert-danger", data.error, 0);
            if (!data.authenticated) {
                location.reload(true);
            }
        }
    }
}
function failure(xhr, status, error) {
    SWIUtil.ShowMessage("alert-danger", error + ". " + _errorServer, 0);
}
var SWIGateway = /** @class */ (function () {
    function SWIGateway() {
        this._execForm = null;
    }
    SWIGateway.prototype.getExecForm = function (action) {
        if (this._execForm == null)
            this._execForm = $('<form method="post" target="_blank"/>').appendTo('body');
        this._execForm.find("input").remove();
        this._execForm.attr('action', _sealServer + action);
        return this._execForm;
    };
    SWIGateway.prototype.GetVersions = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetVersions", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Login = function (user, password, callback, errorcb) {
        $.post(_sealServer + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Logout = function (callback, errorcb) {
        $.post(_sealServer + "SWILogout")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetUserProfile = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetUserProfile")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetCultures = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetCultures")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SetUserProfile = function (culture, defaultView, callback, errorcb) {
        $.post(_sealServer + "SWISetUserProfile", {
            culture: culture,
            defaultView: defaultView
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFiles = function (paths, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.MoveFile = function (source, destination, copy, callback, errorcb) {
        $.post(_sealServer + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetRootFolders = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetRootFolders", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetFolders = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetFolders", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetFolderDetail = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.CreateFolder = function (path, callback, errorcb) {
        $.post(_sealServer + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFolder = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RenameFolder = function (source, destination, callback, errorcb) {
        $.post(_sealServer + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Search = function (path, pattern, callback, errorcb) {
        $.post(_sealServer + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.ExecuteReport = function (path, render, viewGUID, outputGUID) {
        var f = this.getExecForm("SWExecuteReport");
        f.append($('<input />').attr('name', 'path').attr('value', path));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    };
    SWIGateway.prototype.ExecuteReportDefinition = function (report, render, viewGUID, outputGUID) {
        var f = this.getExecForm("SWExecuteReportDefinition");
        f.append($('<input />').attr('name', 'report').attr('value', report));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    };
    SWIGateway.prototype.ViewFile = function (path) {
        var f = this.getExecForm("SWViewFile");
        f.append($('<input />').attr('type', 'hidden').attr('name', 'path').attr('value', path));
        f.submit();
    };
    SWIGateway.prototype.GetReportDetail = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.NewReportDefinition = function (path, sqlmodel, callback, errorcb) {
        $.post(_sealServer + "SWINewReportDefinition", {
            path: path,
            sqlmodel: sqlmodel
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetReportDefinition = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetReportDefinition", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveReportDefinition = function (path, check, report, callback, errorcb) {
        $.post(_sealServer + "SWISaveReportDefinition", {
            path: path, check: check, report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RefreshSQLModel = function (report, callback, errorcb) {
        $.post(_sealServer + "SWIRefreshSQLModel", {
            report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetUserDashboards = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetUserDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboards = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardItems = function (guid, callback, errorcb) {
        $.post(_sealServer + "SWIGetDashboardItems", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardItem = function (guid, itemguid, callback, errorcb) {
        $.post(_sealServer + "SWIGetDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetWidgets = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetWidgets")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardResult = function (guid, itemguid, force, callback, errorcb) {
        $.post(_sealServer + "SWIGetDashboardResult", {
            guid: guid,
            itemguid: itemguid,
            force: force
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.AddDashboard = function (guids, callback, errorcb) {
        $.post(_sealServer + "SWIAddDashboard", {
            guids: guids
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RemoveDashboard = function (guid, callback, errorcb) {
        $.post(_sealServer + "SWIRemoveDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.CreateDashboard = function (name, path, callback, errorcb) {
        $.post(_sealServer + "SWICreateDashboard", {
            name: name,
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteDashboard = function (guid, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.MoveDashboard = function (guid, name, path, copy, callback, errorcb) {
        $.post(_sealServer + "SWIMoveDashboard", {
            guid: guid,
            name: name,
            path: path,
            copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.AddDashboardItems = function (guid, widgetguids, group, callback, errorcb) {
        $.post(_sealServer + "SWIAddDashboardItems", {
            guid: guid,
            widgetguids: widgetguids,
            group: group
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveDashboardItem = function (guid, itemguid, name, groupname, color, icon, width, height, refresh, dynamic, callback, errorcb) {
        $.post(_sealServer + "SWISaveDashboardItem", {
            guid: guid,
            itemguid: itemguid,
            name: name,
            groupname: groupname,
            color: color,
            icon: icon,
            width: width,
            height: height,
            refresh: refresh,
            dynamic: dynamic
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.UpdateDashboardItemsGroupName = function (guid, oldname, newname, callback, errorcb) {
        $.post(_sealServer + "SWIUpdateDashboardItemsGroupName", {
            guid: guid,
            oldname: oldname,
            newname: newname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteDashboardItem = function (guid, itemguid, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SwapDashboardOrder = function (guid1, guid2, callback, errorcb) {
        $.post(_sealServer + "SWISwapDashboardOrder", {
            guid1: guid1,
            guid2: guid2
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SwapDashboardGroupOrder = function (guid, source, destination, callback, errorcb) {
        $.post(_sealServer + "SWISwapDashboardGroupOrder", {
            guid: guid,
            source: source,
            destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SetLastDashboard = function (guid, callback, errorcb) {
        $.post(_sealServer + "SWISetLastDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveDashboardItemsOrder = function (guid, orders, itemguid, groupname, callback, errorcb) {
        $.post(_sealServer + "SWISaveDashboardItemsOrder", {
            guid: guid,
            orders: orders,
            itemguid: itemguid,
            groupname: groupname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    return SWIGateway;
}());
//# sourceMappingURL=swi-gateway.js.map