declare var WebApplicationName: string;
var _server: string = WebApplicationName;
var _errorServer: string = SWIUtil.tr("Unexpected error on server") + ": '" + _server + "'";

declare function postForm(url: string, target: string, data : any) : any;

class SWIGateway {
    public GetVersions(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetVersions", {})
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

    public ResetPassword(id: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIResetPassword", {
            id: id
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public ResetPassword2(guid: string, token: string, password1: string, password2: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIResetPassword2", {
            guid: guid,
            token: token,
            password1: password1,
            password2: password2,
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public ChangePassword(password: string, password1: string, password2: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIChangePassword", {
            password: password,
            password1: password1,
            password2: password2,
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

    public MoveFile(source: string, destination: string, copy: boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIMoveFile", {
            source: source, destination: destination, copy: copy
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public CreateShortcut(source: string, destination: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWICreateShortcut", {
            source: source, destination: destination
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

    public EmptyRecycleBin(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIEmptyRecycleBin", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RenameFolder(source: string, destination: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
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

    public ExecuteReportFromMenu(path: string, viewGUID: string, outputGUID: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
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

    public MarkFavorite(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWMarkFavorite", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RemoveRecentReport(path: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRemoveRecentReport", {
            path: path
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RenameFavoriteReport(path: string, newName: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRenameFavoriteReport", {
            path: path,
            newName: newName
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public UploadFile(data: FormData, callback: (data: any) => void, errorcb?: (data: any) => void) {
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

    public GetUserAgents(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetUserAgents", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public SelectAgent(guid: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWISelectAgent", { guid: guid })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public ClearAIAgent(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIClearAIAgent", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    private _currentAIXHR: JQueryXHR | null = null;

    public GetAIAgentResponse(message: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        this._currentAIXHR = $.post(_server + "SWIGetAIAgentResponse", {
            message: message
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { if (status !== 'abort') SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetAIAgentProgress(callback: (data: any) => void) {
        // Best-effort polling: silently ignore failures (the request may have just finished).
        $.post(_server + "SWIGetAIAgentProgress", {})
            .done(function (data) { if (data && data.messages) callback(data); })
            .fail(function () { });
    }

    public GetAIAgentSamplePrompts(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIGetAIAgentSamplePrompts", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RewindAIAgent(userMessageIndex: number, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SWIRewindAIAgent", {
            userMessageIndex: userMessageIndex
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public CancelAIAgentResponse(callback: ((data: any) => void) | null, errorcb?: ((data: any) => void) | null) {
        if (this._currentAIXHR) {
            this._currentAIXHR.abort();
            this._currentAIXHR = null;
        }
        $.post(_server + "SWICancelAIAgentResponse", {})
            .done(function (data) { if (callback) callback(data); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    // ── Chat persistence (Recents / Favorites) ──────────────────

    public SaveAIAgentChat(name: string, infos: { Key: string; Value: string }[], callback: (data: any) => void, errorcb?: (data: any) => void, generateName?: boolean) {
        $.post(_server + "SAISaveAgentChat", {
            name: name,
            infosJson: JSON.stringify(infos),
            generateName: generateName === true
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public GetAIAgentChats(callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SAIGetAgentChats", {})
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public MarkAIAgentChatFavorite(name: string, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SAIMarkAgentChatFavorite", {
            name: name
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public LoadAIAgentChat(name: string, favorite: boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SAILoadAgentChat", {
            name: name,
            favorite: favorite
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public DeleteAIAgentChat(name: string, favorite: boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SAIDeleteAgentChat", {
            name: name,
            favorite: favorite
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }

    public RenameAIAgentChat(name: string, newName: string, favorite: boolean, callback: (data: any) => void, errorcb?: (data: any) => void) {
        $.post(_server + "SAIRenameAgentChat", {
            name: name,
            newName: newName,
            favorite: favorite
        })
            .done(function (data) { SWIUtil.GatewayCallbackHandler(data, callback, errorcb); })
            .fail(function (xhr, status, error) { SWIUtil.GatewayFailure(xhr, status, error); });
    }
}
