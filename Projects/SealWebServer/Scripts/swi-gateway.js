var _server = WebApplicationName;
var _errorServer = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'";
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
        /*
                $.post(_server + "DashboardsExport", {
                    dashboards: dashboards,
                    format: format
                })
                    .done(function (data) { callbackHandler(data, callback, errorcb); })
                    .fail(function (xhr, status, error) { failure(xhr, status, error); });*/
    }
    SWIGateway.prototype.getExecForm = function (action, target) {
        if (this._execForm == null)
            this._execForm = $('<form method="post"/>').appendTo('body');
        this._execForm.find("input").remove();
        this._execForm.attr('target', target);
        this._execForm.attr('action', _server + action);
        return this._execForm;
    };
    SWIGateway.prototype.GetVersions = function (callback, errorcb) {
        $.post(_server + "SWIGetVersions", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Login = function (user, password, callback, errorcb) {
        $.post(_server + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Logout = function (callback, errorcb) {
        $.post(_server + "SWILogout")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetUserProfile = function (callback, errorcb) {
        $.post(_server + "SWIGetUserProfile")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetCultures = function (callback, errorcb) {
        $.post(_server + "SWIGetCultures")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SetUserProfile = function (culture, defaultView, callback, errorcb) {
        $.post(_server + "SWISetUserProfile", {
            culture: culture,
            defaultView: defaultView
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFiles = function (paths, callback, errorcb) {
        $.post(_server + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.MoveFile = function (source, destination, copy, callback, errorcb) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetRootFolders = function (callback, errorcb) {
        $.post(_server + "SWIGetRootFolders", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetFolderDetail = function (path, callback, errorcb) {
        $.post(_server + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.CreateFolder = function (path, callback, errorcb) {
        $.post(_server + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFolder = function (path, callback, errorcb) {
        $.post(_server + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RenameFolder = function (source, destination, callback, errorcb) {
        $.post(_server + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.Search = function (path, pattern, callback, errorcb) {
        $.post(_server + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.ExecuteReport = function (path, render, viewGUID, outputGUID) {
        var f = this.getExecForm("SWExecuteReport", "_blank");
        f.append($('<input />').attr('name', 'path').attr('value', path));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    };
    SWIGateway.prototype.ExecuteReportDefinition = function (report, render, viewGUID, outputGUID) {
        var f = this.getExecForm("SWExecuteReportDefinition", "_blank");
        f.append($('<input />').attr('name', 'report').attr('value', report));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    };
    SWIGateway.prototype.ViewFile = function (path) {
        var f = this.getExecForm("SWViewFile", "_blank");
        f.append($('<input />').attr('type', 'hidden').attr('name', 'path').attr('value', path));
        f.submit();
    };
    SWIGateway.prototype.GetReportDetail = function (path, callback, errorcb) {
        $.post(_server + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.NewReportDefinition = function (path, sqlmodel, callback, errorcb) {
        $.post(_server + "SWINewReportDefinition", {
            path: path,
            sqlmodel: sqlmodel
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetReportDefinition = function (path, callback, errorcb) {
        $.post(_server + "SWIGetReportDefinition", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveReportDefinition = function (path, check, report, callback, errorcb) {
        $.post(_server + "SWISaveReportDefinition", {
            path: path, check: check, report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RefreshSQLModel = function (report, callback, errorcb) {
        $.post(_server + "SWIRefreshSQLModel", {
            report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetUserDashboards = function (callback, errorcb) {
        $.post(_server + "SWIGetUserDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboards = function (callback, errorcb) {
        $.post(_server + "SWIGetDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardItems = function (guid, callback, errorcb) {
        $.post(_server + "SWIGetDashboardItems", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardItem = function (guid, itemguid, callback, errorcb) {
        $.post(_server + "SWIGetDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetWidgets = function (callback, errorcb) {
        $.post(_server + "SWIGetWidgets")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.GetDashboardResult = function (guid, itemguid, force, format, callback, errorcb) {
        $.post(_server + "SWIGetDashboardResult", {
            guid: guid,
            itemguid: itemguid,
            force: force,
            format: format
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.AddDashboard = function (guids, callback, errorcb) {
        $.post(_server + "SWIAddDashboard", {
            guids: guids
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.RemoveDashboard = function (guid, callback, errorcb) {
        $.post(_server + "SWIRemoveDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.CreateDashboard = function (name, path, callback, errorcb) {
        $.post(_server + "SWICreateDashboard", {
            name: name,
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteDashboard = function (guid, callback, errorcb) {
        $.post(_server + "SWIDeleteDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.MoveDashboard = function (guid, name, path, copy, callback, errorcb) {
        $.post(_server + "SWIMoveDashboard", {
            guid: guid,
            name: name,
            path: path,
            copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.AddDashboardItems = function (guid, widgetguids, group, callback, errorcb) {
        $.post(_server + "SWIAddDashboardItems", {
            guid: guid,
            widgetguids: widgetguids,
            group: group
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveDashboardItem = function (guid, itemguid, name, groupname, color, icon, width, height, refresh, dynamic, callback, errorcb) {
        $.post(_server + "SWISaveDashboardItem", {
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
        $.post(_server + "SWIUpdateDashboardItemsGroupName", {
            guid: guid,
            oldname: oldname,
            newname: newname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteDashboardItem = function (guid, itemguid, callback, errorcb) {
        $.post(_server + "SWIDeleteDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SwapDashboardOrder = function (guid1, guid2, callback, errorcb) {
        $.post(_server + "SWISwapDashboardOrder", {
            guid1: guid1,
            guid2: guid2
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SwapDashboardGroupOrder = function (guid, source, destination, callback, errorcb) {
        $.post(_server + "SWISwapDashboardGroupOrder", {
            guid: guid,
            source: source,
            destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SetLastDashboard = function (guid, callback, errorcb) {
        $.post(_server + "SWISetLastDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.SaveDashboardItemsOrder = function (guid, orders, itemguid, groupname, callback, errorcb) {
        $.post(_server + "SWISaveDashboardItemsOrder", {
            guid: guid,
            orders: orders,
            itemguid: itemguid,
            groupname: groupname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.ExportDashboards = function (dashboards, format) {
        var f = this.getExecForm("SWExportDashboards", format == "htmlprint" ? "_blank" : "");
        f.append($('<input />').attr('name', 'dashboards').attr('value', dashboards));
        f.append($('<input />').attr('name', 'format').attr('value', format));
        f.children('input').attr('type', 'hidden');
        f.submit();
    };
    return SWIGateway;
}());
//# sourceMappingURL=swi-gateway.js.map