declare var WebApplicationName: string;
var _server: string = WebApplicationName;
var _errorServer: string = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'"; 

function callbackHandler(data: any, callback: (data: any) => void, errorcb?: (data: any) => void) {
    if (!data.error) {
        if (callback) callback(data);
    }
    else {
        if (errorcb) errorcb(data);
        else {
            SWIUtil.ShowMessage("alert-danger", data.error, 0);
            if (!data.authenticated) {
                location.reload(true);
            }
        }
    }
}

function failure(xhr, status, error) {
    SWIUtil.ShowMessage("alert-danger", error +". " + _errorServer, 0);
}

class SWIGateway {
    private _execForm: JQuery = null;
    private getExecForm(action : string, target : string): JQuery {
        if (this._execForm == null) this._execForm = $('<form method="post"/>').appendTo('body');
        this._execForm.find("input").remove();
        this._execForm.attr('target', target);
        this._execForm.attr('action', _server + action);
        return this._execForm;
    }

    public GetVersions(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetVersions", { })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public Login(user: string, password: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public Logout(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWILogout")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetUserProfile(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetUserProfile")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetCultures(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetCultures")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SetUserProfile(culture: string, defaultView: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISetUserProfile", {
            culture: culture,
            defaultView: defaultView
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public DeleteFiles(paths: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public MoveFile(source: string, destination: string, copy : boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetRootFolders(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetRootFolders", {
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetFolderDetail(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public CreateFolder(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public DeleteFolder(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public RenameFolder(source: string, destination : string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public Search(path: string, pattern: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public ExecuteReport(path: string, render: boolean, viewGUID: string, outputGUID: string) {
        var f = this.getExecForm("SWExecuteReport", "_blank");
        f.append($('<input />').attr('name', 'path').attr('value', path));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    }

    public ExecuteReportDefinition(report: any, render: boolean, viewGUID: string, outputGUID: string) {
        var f = this.getExecForm("SWExecuteReportDefinition", "_blank");
        f.append($('<input />').attr('name', 'report').attr('value', report));
        f.append($('<input />').attr('name', 'render').attr('value', JSON.stringify(render)));
        f.append($('<input />').attr('name', 'viewGUID').attr('value', viewGUID));
        f.append($('<input />').attr('name', 'outputGUID').attr('value', outputGUID));
        f.children('input').attr('type', 'hidden');
        f.submit();
    }

    public ViewFile(path: string) {
        var f = this.getExecForm("SWViewFile", "_blank");
        f.append($('<input />').attr('type', 'hidden').attr('name', 'path').attr('value', path));
        f.submit();
    }

    public GetReportDetail(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public NewReportDefinition(path: string, sqlmodel : boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWINewReportDefinition", {
            path: path,
            sqlmodel: sqlmodel
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetReportDefinition(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetReportDefinition", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SaveReportDefinition(path: string, check: boolean, report: any, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISaveReportDefinition", {
            path: path, check: check, report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public RefreshSQLModel(report: any, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRefreshSQLModel", {
            report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetUserDashboards(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetUserDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetDashboards(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetDashboards")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetDashboardItems(guid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetDashboardItems", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetDashboardItem(guid: string, itemguid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetWidgets(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetWidgets")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public GetDashboardResult(guid: string, itemguid: string, force: boolean, format: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetDashboardResult", {
            guid: guid,
            itemguid: itemguid,
            force: force,
            format: format
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public AddDashboard(guids: string[], callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIAddDashboard", {
            guids: guids
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public RemoveDashboard(guid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRemoveDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public CreateDashboard(name: string, path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWICreateDashboard", {
            name: name,
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public DeleteDashboard(guid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public MoveDashboard(guid: string, name: string, path : string, copy: boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIMoveDashboard", {
            guid: guid,
            name: name,
            path: path,
            copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public AddDashboardItems(guid: string, widgetguids: string, group: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIAddDashboardItems", {
            guid: guid,
            widgetguids: widgetguids,
            group: group
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SaveDashboardItem(guid: string, itemguid: string, name: string, groupname: string, color: string, icon: string, width: number, height: number, refresh: number, dynamic:boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
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
    }

    public UpdateDashboardItemsGroupName(guid: string, oldname: string, newname: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIUpdateDashboardItemsGroupName", {
            guid: guid,
            oldname: oldname,
            newname: newname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public DeleteDashboardItem(guid: string, itemguid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteDashboardItem", {
            guid: guid,
            itemguid: itemguid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SwapDashboardOrder(guid1: string, guid2: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISwapDashboardOrder", {
            guid1: guid1,
            guid2: guid2
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SwapDashboardGroupOrder(guid: string, source: number, destination: number, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISwapDashboardGroupOrder", {
            guid: guid,
            source: source,
            destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SetLastDashboard(guid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISetLastDashboard", {
            guid: guid
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public SaveDashboardItemsOrder(guid: string, orders: string[], itemguid: string, groupname: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISaveDashboardItemsOrder", {
            guid: guid,
            orders: orders,
            itemguid: itemguid,
            groupname: groupname
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public ExportDashboards(dashboards: string, format: string) {
        var f = this.getExecForm("SWExportDashboards", format == "htmlprint" ? "_blank" : "");
        f.append($('<input />').attr('name', 'dashboards').attr('value', dashboards));
        f.append($('<input />').attr('name', 'format').attr('value', format));
        f.children('input').attr('type', 'hidden');
        f.submit();
    }
/*
        $.post(_server + "DashboardsExport", {
            dashboards: dashboards,
            format: format
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });*/
  
}
