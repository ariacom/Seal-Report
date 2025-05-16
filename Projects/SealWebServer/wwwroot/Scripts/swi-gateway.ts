declare var WebApplicationName: string;
var _server: string = WebApplicationName;
var _errorServer: string = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'"; 

declare function postForm(url: string, target: string, data);

class SWIGateway {
    public GetVersions(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetVersions", { })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public Login(user: string, password: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWILogin", {
            user: user, password: password
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public CheckSecurityCode(code: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWICheckSecurityCode", {
            code: code
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public Logout(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post({
                url: _server + "SWILogout", xhrFields: {
                    withCredentials: true
                }
            })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetRootMenu(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetRootMenu")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetConfiguration(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetConfiguration")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public SetConfiguration(configuration: any, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISetConfiguration", {
            configuration: configuration
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetUserProfile(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetUserProfile")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetCultures(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetCultures")
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public SetUserProfile(culture: string, onstartup: string, startupreport: string, startupreportname: string, executionmode: string, connections: string[], callback: (data: any) => void, errorcb?: (data: any) => void) {
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

    public DeleteFiles(paths: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteFiles", {
            paths: paths
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public MoveFile(source: string, destination: string, copy : boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetRootFolders(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetRootFolders", {
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetFolderDetail(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetFolderDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public CreateFolder(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWICreateFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public DeleteFolder(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIDeleteFolder", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RenameFolder(source: string, destination : string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRenameFolder", {
            source: source, destination: destination
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public Search(path: string, pattern: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISearch", {
            path: path, pattern: pattern
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public ExecuteReport(path: string, viewGUID: string, outputGUID: string) {
        postForm(_server + "SWExecuteReport", "_blank", { path: path, viewGUID: viewGUID, outputGUID: outputGUID });
    }

    public ExecuteReportFromMenu(path: string, viewGUID: string, outputGUID: string,  callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWExecuteReport", {
            path: path,
            viewGUID: viewGUID,
            outputGUID: outputGUID,
            fromMenu: true
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public ViewFile(path: string) {
        postForm(_server + "SWViewFile", "_blank", { path: path });
    }

    public GetReportDetail(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetReportDetail", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
}
