"use strict";
var _server = WebApplicationName;
var _errorServer = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'";
class SWIGateway {
    GetVersions(callback, errorcb) {
        $.post(_server + "SWIGetVersions", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    Login(user, password, callback, errorcb) {
        $.post(_server + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    CheckSecurityCode(code, callback, errorcb) {
        $.post(_server + "SWICheckSecurityCode", {
            code: code
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    Logout(callback, errorcb) {
        $.post({
            url: _server + "SWILogout", xhrFields: {
                withCredentials: true
            }
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetRootMenu(callback, errorcb) {
        $.post(_server + "SWIGetRootMenu")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetConfiguration(callback, errorcb) {
        $.post(_server + "SWIGetConfiguration")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    SetConfiguration(configuration, callback, errorcb) {
        $.post(_server + "SWISetConfiguration", {
            configuration: configuration
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    ResetPassword(id, callback, errorcb) {
        $.post(_server + "SWIResetPassword", {
            id: id
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    ResetPassword2(guid, token, password1, password2, callback, errorcb) {
        $.post(_server + "SWIResetPassword2", {
            guid: guid,
            token: token,
            password1: password1,
            password2: password2,
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    ChangePassword(password, password1, password2, callback, errorcb) {
        $.post(_server + "SWIChangePassword", {
            password: password,
            password1: password1,
            password2: password2,
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetUserProfile(callback, errorcb) {
        $.post(_server + "SWIGetUserProfile")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetCultures(callback, errorcb) {
        $.post(_server + "SWIGetCultures")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    SetUserProfile(culture, onstartup, startupreport, startupreportname, executionmode, connections, callback, errorcb) {
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
    }
    DeleteFiles(paths, callback, errorcb) {
        $.post(_server + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    MoveFile(source, destination, copy, callback, errorcb) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetRootFolders(callback, errorcb) {
        $.post(_server + "SWIGetRootFolders", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    GetFolderDetail(path, callback, errorcb) {
        $.post(_server + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    CreateFolder(path, callback, errorcb) {
        $.post(_server + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    DeleteFolder(path, callback, errorcb) {
        $.post(_server + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    RenameFolder(source, destination, callback, errorcb) {
        $.post(_server + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    Search(path, pattern, callback, errorcb) {
        $.post(_server + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    ExecuteReport(path, viewGUID, outputGUID) {
        postForm(_server + "SWExecuteReport", "_blank", { path: path, viewGUID: viewGUID, outputGUID: outputGUID });
    }
    ExecuteReportFromMenu(path, viewGUID, outputGUID, callback, errorcb) {
        $.post(_server + "SWExecuteReport", {
            path: path,
            viewGUID: viewGUID,
            outputGUID: outputGUID,
            fromMenu: true
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    ViewFile(path) {
        postForm(_server + "SWViewFile", "_blank", { path: path });
    }
    GetReportDetail(path, callback, errorcb) {
        $.post(_server + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    MarkFavorite(path, callback, errorcb) {
        $.post(_server + "SWMarkFavorite", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
    UploadFile(data, callback, errorcb) {
        $.ajax({
            url: _server + "SWUploadFile",
            type: 'POST',
            data: data,
            processData: false,
            contentType: false,
            success: function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); },
            error: function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); }
        });
    }
}
//# sourceMappingURL=swi-gateway.js.map