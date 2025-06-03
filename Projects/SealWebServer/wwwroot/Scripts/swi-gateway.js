var _server = WebApplicationName;
var _errorServer = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'";
var SWIGateway = /** @class */ (function () {
    function SWIGateway() {
    }
    SWIGateway.prototype.GetVersions = function (callback, errorcb) {
        $.post(_server + "SWIGetVersions", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.Login = function (user, password, callback, errorcb) {
        $.post(_server + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.CheckSecurityCode = function (code, callback, errorcb) {
        $.post(_server + "SWICheckSecurityCode", {
            code: code
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.Logout = function (callback, errorcb) {
        $.post({
            url: _server + "SWILogout", xhrFields: {
                withCredentials: true
            }
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetRootMenu = function (callback, errorcb) {
        $.post(_server + "SWIGetRootMenu")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetConfiguration = function (callback, errorcb) {
        $.post(_server + "SWIGetConfiguration")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.SetConfiguration = function (configuration, callback, errorcb) {
        $.post(_server + "SWISetConfiguration", {
            configuration: configuration
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.ResetPassword = function (id, callback, errorcb) {
        $.post(_server + "SWIResetPassword", {
            id: id
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.ResetPassword2 = function (guid, token, password1, password2, callback, errorcb) {
        $.post(_server + "SWIResetPassword2", {
            guid: guid,
            token: token,
            password1: password1,
            password2: password2,
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.ChangePassword = function (password, password1, password2, callback, errorcb) {
        $.post(_server + "SWIChangePassword", {
            password: password,
            password1: password1,
            password2: password2,
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetUserProfile = function (callback, errorcb) {
        $.post(_server + "SWIGetUserProfile")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetCultures = function (callback, errorcb) {
        $.post(_server + "SWIGetCultures")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.SetUserProfile = function (culture, onstartup, startupreport, startupreportname, executionmode, connections, callback, errorcb) {
        $.post(_server + "SWISetUserProfile", {
            culture: culture,
            onstartup: onstartup,
            startupreport: startupreport,
            startupreportname: startupreportname,
            executionmode: executionmode,
            connections: connections
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFiles = function (paths, callback, errorcb) {
        $.post(_server + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.MoveFile = function (source, destination, copy, callback, errorcb) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetRootFolders = function (callback, errorcb) {
        $.post(_server + "SWIGetRootFolders", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.GetFolderDetail = function (path, callback, errorcb) {
        $.post(_server + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.CreateFolder = function (path, callback, errorcb) {
        $.post(_server + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.DeleteFolder = function (path, callback, errorcb) {
        $.post(_server + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.RenameFolder = function (source, destination, callback, errorcb) {
        $.post(_server + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.Search = function (path, pattern, callback, errorcb) {
        $.post(_server + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.ExecuteReport = function (path, viewGUID, outputGUID) {
        postForm(_server + "SWExecuteReport", "_blank", { path: path, viewGUID: viewGUID, outputGUID: outputGUID });
    };
    SWIGateway.prototype.ExecuteReportFromMenu = function (path, viewGUID, outputGUID, callback, errorcb) {
        $.post(_server + "SWExecuteReport", {
            path: path,
            viewGUID: viewGUID,
            outputGUID: outputGUID,
            fromMenu: true
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.ViewFile = function (path) {
        postForm(_server + "SWViewFile", "_blank", { path: path });
    };
    SWIGateway.prototype.GetReportDetail = function (path, callback, errorcb) {
        $.post(_server + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    };
    SWIGateway.prototype.UploadFile = function (data, callback, errorcb) {
        $.ajax({
            url: _server + "SWUploadFile",
            type: 'POST',
            data: data,
            processData: false,
            contentType: false,
            success: function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); },
            error: function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); }
        });
    };
    return SWIGateway;
}());
//# sourceMappingURL=swi-gateway.js.map