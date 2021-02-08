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
    }
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
    SWIGateway.prototype.GetRootMenu = function (callback, errorcb) {
        $.post(_server + "SWIGetRootMenu")
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
    SWIGateway.prototype.SetUserProfile = function (culture, onstartup, startupreport, startupreportname, callback, errorcb) {
        $.post(_server + "SWISetUserProfile", {
            culture: culture,
            onstartup: onstartup,
            startupreport: startupreport,
            startupreportname: startupreportname
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
    SWIGateway.prototype.ExecuteReport = function (path, viewGUID, outputGUID) {
        postForm(_server + "SWExecuteReport", "_blank", { path: path, viewGUID: viewGUID, outputGUID: outputGUID });
    };
    SWIGateway.prototype.ExecuteReportDefinition = function (report, viewGUID, outputGUID) {
        postForm(_server + "SWExecuteReportDefinition", "_blank", { report: report, viewGUID: viewGUID, outputGUID: outputGUID });
    };
    SWIGateway.prototype.ExecuteReportFromMenu = function (path, viewGUID, outputGUID, callback, errorcb) {
        $.post(_server + "SWExecuteReport", {
            path: path,
            viewGUID: viewGUID,
            outputGUID: outputGUID,
            fromMenu: true
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { failure(xhr, status, error); });
    };
    SWIGateway.prototype.ViewFile = function (path) {
        postForm(_server + "SWViewFile", "_blank", { path: path });
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
    return SWIGateway;
}());
//# sourceMappingURL=swi-gateway.js.map