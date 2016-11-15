var _sealServer = WebApplicationName + (WebApplicationName == "/" ? "" : "/");
var _errorServer = SWIUtil.tr("Error: Unable to connect to the server") + ": '" + _sealServer + "'";
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
function failure() {
    SWIUtil.ShowMessage("alert-danger", _errorServer, 0);
}
var SWIGateway = (function () {
    function SWIGateway() {
    }
    SWIGateway.prototype.GetVersions = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetVersions", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.Login = function (user, password, callback, errorcb) {
        $.post(_sealServer + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.Logout = function (callback, errorcb) {
        $.post(_sealServer + "SWILogout")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetUserProfile = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetUserProfile")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetCultures = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetCultures")
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.SetUserProfile = function (culture, callback, errorcb) {
        $.post(_sealServer + "SWISetUserProfile", {
            culture: culture
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.DeleteFiles = function (paths, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.MoveFile = function (source, destination, copy, callback, errorcb) {
        $.post(_sealServer + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetRootFolders = function (callback, errorcb) {
        $.post(_sealServer + "SWIGetRootFolders", {})
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetFolders = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetFolders", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetFolderDetail = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.CreateFolder = function (path, callback, errorcb) {
        $.post(_sealServer + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.DeleteFolder = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.RenameFolder = function (source, destination, callback, errorcb) {
        $.post(_sealServer + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.Search = function (path, pattern, callback, errorcb) {
        $.post(_sealServer + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.ExecuteReport = function (path, render, viewGUID, outputGUID, callback, errorcb) {
        $.post(_sealServer + "SWIExecuteReport", {
            path: path, render: render, viewGUID: viewGUID, outputGUID: outputGUID
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.ExecuteReportDefinition = function (report, render, viewGUID, outputGUID, callback, errorcb) {
        $.post(_sealServer + "SWIExecuteReportDefinition", {
            report: report, render: render, viewGUID: viewGUID, outputGUID: outputGUID
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.ViewFile = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIViewFile", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetReportDetail = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.NewReportDefinition = function (path, callback, errorcb) {
        $.post(_sealServer + "SWINewReportDefinition", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.GetReportDefinition = function (path, callback, errorcb) {
        $.post(_sealServer + "SWIGetReportDefinition", {
            path: path
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    SWIGateway.prototype.SaveReportDefinition = function (path, check, report, callback, errorcb) {
        $.post(_sealServer + "SWISaveReportDefinition", {
            path: path, check: check, report: report
        })
            .done(function (data) { callbackHandler(data, callback, errorcb); })
            .fail(function () { failure(); });
    };
    return SWIGateway;
}());
//# sourceMappingURL=swi-gateway.js.map