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


declare function postForm(url: string, target: string, data);

class SWIGateway {
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

    public GetRootMenu(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetRootMenu")
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

    public SetUserProfile(culture: string, onstartup: string, startupreport: string, startupreportname: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISetUserProfile", {
            culture: culture,
            onstartup: onstartup,
            startupreport: startupreport,
            startupreportname: startupreportname
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

    public ExecuteReport(path: string, viewGUID: string, outputGUID: string) {
        postForm(_server + "SWExecuteReport", "_blank", { path: path, viewGUID: viewGUID, outputGUID: outputGUID });
    }

    public ExecuteReportDefinition(report: any, viewGUID: string, outputGUID: string) {
        postForm(_server + "SWExecuteReportDefinition", "_blank", { report: report, viewGUID: viewGUID, outputGUID: outputGUID });
    }

    public ExecuteReportFromMenu(path: string, viewGUID: string, outputGUID: string,  callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWExecuteReport", {
            path: path,
            viewGUID: viewGUID,
            outputGUID: outputGUID,
            fromMenu: true
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    }

    public ViewFile(path: string) {
        postForm(_server + "SWViewFile", "_blank", { path: path });
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
}
