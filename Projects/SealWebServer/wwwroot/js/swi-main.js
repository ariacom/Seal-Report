"use strict";
/// <reference path="typings/jquery/JQuery.d.ts" />
/// <reference path="typings/datatables/types.d.ts" />
/// <reference path="typings/main.d.ts" />
var $waitDialog;
var $editDialog;
var $loginModal;
var $securityModal;
var $passwordResetModal;
var $passwordResetModal2;
var $outputPanel;
var $propertiesPanel;
var $elementDropDown;
var _gateway;
var _main;
var _editor;
$(document).ready(function () {
    _gateway = new SWIGateway();
    _main = new SWIMain();
    _main.Process();
});
class SWIMain {
    constructor() {
        this._profile = null;
        this._canEdit = false;
        this._folder = null;
        this._searchMode = false;
        this._clipboard = [];
        this._clipboardCut = false;
        this._folderpath = "\\";
        this._reportPath = "";
        this._lastReport = new Object();
        this._currentView = "folders";
        this._newWindow = true;
        this._reportIcon = "";
    }
    Process() {
        $waitDialog = $("#wait-dialog");
        $editDialog = $("#edit-dialog");
        $loginModal = $("#login-modal");
        $securityModal = $("#security-modal");
        $passwordResetModal = $("#password-reset-modal");
        $passwordResetModal2 = $("#password-reset-modal2");
        $outputPanel = $("#output-panel");
        $propertiesPanel = $("#properties-panel");
        $elementDropDown = $("#element-dropdown");
        $("#menu-main-button").hide();
        SWIUtil.ShowModal($waitDialog);
        $("#search-pattern").keypress(function (e) {
            if ((e.keyCode || e.which) == 13)
                _main.search();
        });
        $("#password,#username").keypress(function (e) {
            if ((e.keyCode || e.which) == 13)
                _main.login();
        });
        $("#securitycode").keypress(function (e) {
            if ((e.keyCode || e.which) == 13)
                _main.checkSecurityCode();
        });
        SWIUtil.ShowHideControl($("#disconnect-nav-item,#main-container,#report-body,#menu-view-report,#nav_badge,.reportview,.folderview,#menu-main-button,#profile-nav-item,#config-nav-item,#menu-agent-button,#search-pattern,#search-nav-item"), false);
        $("#login-modal-submit").unbind("click").on("click", function () {
            _main.login();
        });
        $("#security-modal-submit").unbind("click").on("click", function () {
            _main.checkSecurityCode();
        });
        $("#login-password-reset").unbind("click").on("click", function (e) {
            e.preventDefault();
            SWIUtil.HideModal($loginModal);
            $("#password-reset-name").text("");
            SWIUtil.ShowModal($passwordResetModal);
        });
        $("#password-reset-submit").unbind("click").on("click", function () {
            _gateway.ResetPassword($("#password-reset-name").val(), function (data) {
                SWIUtil.HideModal($passwordResetModal);
                SWIUtil.ShowMessage("alert-success", SWIUtil.tr("If your identifier is valid, an email has been sent to reset your password."), 5000);
                _main.showLogin();
            });
        });
        $("#security-back-to-login").unbind("click").on("click", function (e) {
            e.preventDefault();
            window.location.reload();
        });
        SWIUtil.InitVersion();
        //Reset password
        const token = new URLSearchParams(window.location.search).get('ptoken');
        const guid = new URLSearchParams(window.location.search).get('guid');
        if (token && guid) {
            SWIUtil.HideModal($waitDialog);
            $("#password-reset-submit2").unbind("click").on("click", function () {
                _gateway.ResetPassword2(guid, token, $("#password-reset1").val(), $("#password-reset2").val(), function (data) {
                    if (data.error)
                        SWIUtil.ShowMessage("alert-danger", data.error, -1);
                    else {
                        SWIUtil.HideModal($passwordResetModal2);
                        SWIUtil.ShowMessage("alert-success", SWIUtil.tr("Your password has been changed."), 5000);
                        _main.showLogin();
                    }
                }, function (data) {
                    if (data.error)
                        SWIUtil.ShowMessage("alert-danger", data.error, -1);
                });
            });
            SWIUtil.ShowModal($passwordResetModal2);
            return;
        }
        _gateway.GetUserProfile(function (data) {
            SWIUtil.ShowHideControl($("#login-password-reset"), data.showresetpassword);
            var login = sessionStorage.getItem('login');
            sessionStorage.removeItem('login');
            if (login) {
                _main.loginFailure(data, true);
            }
            else if (data.authenticated != null && data.authenticated == false) {
                //Try to login without authentication
                _gateway.Login("", "", function (data) { _main.loginSuccess(data); }, function (data) { _main.loginFailure(data, true); });
            }
            else {
                //User already connected
                _main.loginSuccess(data);
            }
        });
        //General handlers
        $(window).unbind("resize").on('resize', function () {
            _main.resize();
        });
    }
    loginSuccess(data) {
        if (data.securitycoderequired) {
            _main.showSecurityCode(data);
            return;
        }
        //check if we have to reload translations
        if ((_main._profile && _main._profile.culture && _main._profile.culture != data.culture) || (languageName != data.language)) {
            $("body").css("opacity", "0.1");
            location.reload();
        }
        _main._profile = data;
        SWIUtil.ShowHideControl($("#ai-panel-toggle, #ai-chat-panel"), data.hasagent);
        _main._reportPath = "";
        _main._folder = null;
        _main._searchMode = false;
        _main._clipboard = [];
        _main._clipboardCut = false;
        _main.clearFilesTable();
        _main._newWindow = (_main._profile.executionmode === 1 || (_main._profile.executionmode === 0 && _main._profile.groupexecutionmode === 1));
        _main._reportIcon = (_main._newWindow ? "fa-solid fa-right-to-bracket" : "fa-solid fa-up-right-from-square");
        //disable current window, 0=Default, 1=NewWindow, 2=SameWindow, 3=AlwaysNewWindow
        if (_main._profile.executionmode === 3 || (_main._profile.executionmode === 0 && _main._profile.groupexecutionmode === 3)) {
            _main._newWindow = true;
            _main._reportIcon = "";
        }
        //Reset init state
        $("#menu-main-button").show();
        $("#nav_button,#brand-id").css("pointer-events", "");
        $("#brand-id").unbind("click").on("click", function () {
            window.location.href = WebApplicationName;
        });
        _main.showTreeView(true);
        $("#reload-nav-item,#execute_button,#restrictions_button").addClass("reportview");
        $("#search-pattern").val("");
        $("body").children(".modal-backdrop").remove();
        SWIUtil.HideModal($loginModal);
        SWIUtil.HideMessages();
        $(".navbar-right").show();
        $("#footer-div").hide();
        SWIUtil.ShowHideControl($("#search-pattern,#search-nav-item"), true);
        SWIUtil.ShowHideControl($("#login-password-reset"), data.showresetpassword);
        $("#password").val("");
        $("#securitycode").val("");
        $("#login-modal-error").text("");
        $("#security-modal-error").text("");
        $("#main-container").css("display", "block");
        SWIUtil.RefreshMenu(_main);
        SWIUtil.InitSpinning();
        _main._currentView = "folders";
        if (_main._profile.showfolders) {
            _main.loadFolderTree();
            $("#nav_button").attr("title", SWIUtil.tr2("Toggle Report or Folders view"));
            $("#nav_button").removeClass("nopointer");
        }
        else {
            _main._currentView = "report";
            $("#nav_button").attr("title", "");
            $("#nav_button").addClass("nopointer");
        }
        if (_editor) {
            _editor.brand();
            _editor.agentMenu();
        }
        //Refresh
        $("#refresh-nav-item").unbind("click").on("click", function () {
            _main.ReloadReportsTable();
            if (_editor)
                _editor.agentMenu();
        });
        //Reload and execute
        $("#reload-nav-item").unbind("click").on("click", function () {
            SWIUtil.ShowModal($waitDialog);
            _main.executeReportFromMenu(_main._lastReport.path, _main._lastReport.viewGUID, _main._lastReport.outputGUID, _main._lastReport.name);
        });
        //Folders
        $("#folders-nav-item").unbind("click").on("click", function () {
            $outputPanel.hide();
            $("#create-folder-name").val("");
            $("#rename-folder-name").val(_main._folder.path);
            SWIUtil.ShowHideControl($("#folder-rename").parent(), _main._folder.manage == 2);
            SWIUtil.ShowHideControl($("#folder-delete").parent(), _main._folder.isEmpty && _main._folder.manage == 2);
            $("#folder-create").unbind("click").on("click", function () {
                SWIUtil.HideModal($("#folder-dialog"));
                var newpath = _main._folder.path + (_main._folder.path == dirSeparator ? "" : dirSeparator) + $("#create-folder-name").val();
                _gateway.CreateFolder(newpath, function () {
                    _main._profile.folder = newpath;
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been created"), 5000);
                });
            });
            $("#folder-rename").unbind("click").on("click", function () {
                SWIUtil.HideModal($("#folder-dialog"));
                var newpath = $("#rename-folder-name").val();
                _gateway.RenameFolder(_main._folder.path, newpath, function () {
                    _main._profile.folder = newpath;
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been renamed"), 5000);
                });
            });
            $("#folder-delete").unbind("click").on("click", function () {
                SWIUtil.HideModal($("#folder-dialog"));
                _gateway.DeleteFolder(_main._folder.path, function () {
                    _main._profile.folder = SWIUtil.GetDirectoryName(_main._folder.path);
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been deleted"), 5000);
                });
            });
            SWIUtil.ShowModal($("#folder-dialog"));
        });
        //Search
        $("#search-pattern").unbind("keypress").on("keypress", function (e) {
            if ((e.keyCode || e.which) == 13) {
                $outputPanel.hide();
                _main.search();
                return false;
            }
        });
        $("#search-nav-item").unbind("click").on("click", function () {
            $outputPanel.hide();
            _main.search();
        });
        //Profile
        SWIUtil.InitProfile(_main._profile);
        //Configuration
        _main.initConfiguration(_main._profile);
        //Disconnect
        SWIUtil.ShowHideControl($("#disconnect-nav-item"), true);
        $("#disconnect-nav-item").unbind("click").on("click", function () {
            SWIUtil.HideMessages();
            $outputPanel.hide();
            $("#report-body").empty();
            _gateway.Logout(function () {
                $("#report-body").empty();
                $("#nav_button").text("");
                SWIUtil.ShowHideControl($("#disconnect-nav-item,#main-container,#report-body,#menu-view-report,#nav_badge,.reportview,.folderview,#menu-main-button,#profile-nav-item,#config-nav-item,#menu-agent-button,#search-pattern,#search-nav-item"), false);
                if (window.aiPanel)
                    window.aiPanel.reset();
                _main.showLogin();
            });
        });
        //Delete reports
        $("#report-delete-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            var checked = $(".report-checkbox:checked").length;
            $("#message-title").html(SWIUtil.tr("Warning"));
            $("#message-text").html(SWIUtil.tr("Do you really want to delete the reports or files selected ?"));
            $("#message-cancel-button").html(SWIUtil.tr("Cancel"));
            $("#message-ok-button").html(SWIUtil.tr("OK"));
            $("#message-ok-button").unbind("click").on("click", function () {
                SWIUtil.HideModal($("#message-dialog"));
                SWIUtil.ShowModal($waitDialog);
                var paths = "";
                $(".report-checkbox:checked").each(function (key, value) {
                    paths += $(value).data("path") + "\n";
                });
                _gateway.DeleteFiles(paths, function () {
                    SWIUtil.ShowMessage("alert-success", checked + " " + SWIUtil.tr("report(s) or file(s) have been deleted"), 5000);
                    _main.ReloadReportsTable();
                    _main.refreshMenus();
                    SWIUtil.HideModal($waitDialog);
                });
            });
            SWIUtil.ShowModal($("#message-dialog"));
        });
        //Rename
        $("#report-rename-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            var source = $(".report-checkbox:checked").first().data("path");
            if (source) {
                var filename = source.split(dirSeparator).pop();
                var extension = filename.split('.').pop();
                $("#report-name-save").unbind("click").on("click", function () {
                    SWIUtil.ShowModal($waitDialog);
                    var folder = _main._folder.path;
                    var destination = (folder != dirSeparator ? folder : "") + dirSeparator + $("#report-name").val() + "." + extension;
                    SWIUtil.HideModal($("#report-name-dialog"));
                    _gateway.MoveFile(source, destination, false, function () {
                        _main.ReloadReportsTable();
                        _main.refreshMenus();
                        SWIUtil.HideModal($waitDialog);
                        SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The report or file has been renamed"), 5000);
                    });
                });
                $("#report-name").val(filename?.replace(/\.[^/.]+$/, ""));
                SWIUtil.ShowModal($("#report-name-dialog"));
            }
        });
        //Copy
        $("#report-copy-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            _main._clipboard = [];
            $(".report-checkbox:checked").each(function (key, value) {
                _main._clipboard[key] = $(value).data("path");
            });
            _main._clipboardCut = false;
            _main.enableControls();
            SWIUtil.ShowMessage("alert-success", _main._clipboard.length.toString() + " " + SWIUtil.tr("report(s) or files(s) copied in the clipboard"), 5000);
        });
        //Cut
        $("#report-cut-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            _main._clipboard = [];
            $(".report-checkbox:checked").each(function (key, value) {
                _main._clipboard[key] = $(value).data("path");
            });
            _main._clipboardCut = true;
            _main.enableControls();
            SWIUtil.ShowMessage("alert-success", _main._clipboard.length.toString() + " " + SWIUtil.tr("report(s) or file(s) cut in the clipboard"), 5000);
        });
        //Paste
        $("#report-paste-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            if (_main._clipboard.length > 0) {
                SWIUtil.ShowModal($waitDialog);
                _main._clipboard.forEach(function (value, index) {
                    var newName = value.split(dirSeparator).pop();
                    var folder = _main._folder.path;
                    var destination = (folder != dirSeparator ? folder : "") + dirSeparator + newName;
                    _gateway.MoveFile(value, destination, !_main._clipboardCut, function () {
                        if (index == _main._clipboard.length - 1) {
                            setTimeout(function () {
                                _main.ReloadReportsTable();
                                _main.refreshMenus();
                                SWIUtil.HideModal($waitDialog);
                                SWIUtil.ShowMessage("alert-success", _main._clipboard.length.toString() + " " + SWIUtil.tr("report(s) or file(s) processed"), 5000);
                            }, 2000);
                        }
                    });
                });
            }
        });
        //Paste as shortcut
        $("#report-shortcut-lightbutton").unbind("click").on("click", (event) => {
            if (!SWIUtil.IsEnabled($(event.currentTarget)))
                return;
            $outputPanel.hide();
            if (_main._clipboard.length > 0) {
                SWIUtil.ShowModal($waitDialog);
                _main._clipboard.forEach(function (value, index) {
                    var newName = value.split(dirSeparator).pop();
                    var folder = _main._folder.path;
                    var destination = (folder != dirSeparator ? folder : "") + dirSeparator + newName;
                    _gateway.CreateShortcut(value, destination, function () {
                        if (index == _main._clipboard.length - 1) {
                            setTimeout(function () {
                                _main.ReloadReportsTable();
                                _main.refreshMenus();
                                SWIUtil.HideModal($waitDialog);
                                SWIUtil.ShowMessage("alert-success", _main._clipboard.length.toString() + " " + SWIUtil.tr("shortcut(s) created"), 5000);
                            }, 2000);
                        }
                    });
                });
            }
        });
        //Upload
        $("#report-upload-lightbutton").unbind("click").on("click", function (e) {
            e.preventDefault();
            $("#upload-file").click();
        });
        $("#upload-file").on("change", function (e) {
            const fileInput = e.target;
            if (fileInput.files && fileInput.files.length > 0) {
                const formData = new FormData();
                formData.append('file', fileInput.files[0]);
                formData.append('path', _main._folder.path);
                _gateway.UploadFile(formData, function (data) {
                    if (data.Status) {
                        _main.ReloadReportsTable();
                        _main.refreshMenus();
                        SWIUtil.ShowMessage("alert-success", data.Message, 5000);
                    }
                    else {
                        SWIUtil.ShowMessage("alert-danger", data.Message, 0);
                    }
                    // Clear the file input
                    fileInput.value = '';
                });
            }
        });
        _main.enableControls();
        _main.resize();
        //Start last report
        if (_main._profile.report) {
            _main.executeReportFromMenu(_main._profile.report, "", "", _main._profile.reportname);
            $("#brand-id").unbind("click").on("click", function () {
                if (_main._reportPath == _main._profile.report)
                    _main.toggleFoldersReport(true);
                else
                    _main.executeReportFromMenu(_main._profile.report, "", "", _main._profile.reportname);
            });
        }
        else {
            _main.toggleFoldersReport(false);
            SWIUtil.HideModal($waitDialog);
        }
        SWIUtil.InitVersion();
    }
    initConfiguration(profile) {
        SWIUtil.ShowHideControl($("#config-nav-item"), profile.editconfiguration);
        $("#config-nav-item").unbind("click").on("click", function () {
            $outputPanel.hide();
            _gateway.GetConfiguration(function (data) {
                _main._config = data;
                SWIUtil.InitStandardInput("#config-webproduct-name", _main._config.productname, null, function (val) { _main._config.productname = val; });
                _main.initDropDownGroups();
                _main.initDropDownLogins();
                $("#config-save").unbind("click").on("click", function (e) {
                    if (profile.editconfiguration) {
                        SWIUtil.ShowModal($waitDialog);
                        _gateway.SetConfiguration(JSON.stringify(_main._config), function () {
                            SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The configuration has been saved. Please login again if you are impacted by the security."), 5000);
                            SWIUtil.HideModal($waitDialog);
                            SWIUtil.HideModal($("#config-dialog"));
                        }, function (data) {
                            SWIUtil.ShowMessage("alert-danger", data.error, 0);
                            SWIUtil.HideModal($waitDialog);
                        });
                    }
                });
                SWIUtil.ShowModal($("#config-dialog"));
            });
        });
    }
    initDropDownGroups() {
        if (_main._configGroup != null)
            _main._configGroup = _main._config.groups.find((i) => i.Name === _main._configGroup.Name);
        if (!_main._configGroup && _main._config.groups.length > 0)
            _main._configGroup = _main._config.groups[0];
        var $ddname = $("#config-groups-dropdown");
        $ddname.empty();
        $.each(_main._config.groups, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Name, value.Name, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New group"), "", "group", "fa-solid fa-circle-plus")));
        if (_main._configGroup && _main._config.groups.length > 1) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configGroup.Name, "", "remove", "fa-solid fa-circle-minus")));
        }
        $("#config-groups-dropdown > li > a").unbind("click").on("click", (event) => {
            var type = $(event.currentTarget).prop("type");
            var id = $(event.currentTarget).prop("id");
            if (type == "select") {
                _main._configGroup = _main._config.groups.find((v) => v.Name === id);
            }
            else if (type == "group") {
                var newGroup = {
                    GUID: SWIUtil.Newguid(),
                    Name: SWIUtil.UniqueName(SWIUtil.tr2("New group"), _main._config.groups),
                    Folders: [],
                    EditConfiguration: false,
                    EditProfile: true,
                    PersFolderRight: 2,
                    AgentGUIDs: []
                };
                _main._config.groups.push(newGroup);
                _main._configGroup = newGroup;
            }
            else {
                var index = _main._config.groups.indexOf(_main._configGroup);
                if (index != -1)
                    _main._config.groups.splice(index, 1);
                _main._configGroup = null;
            }
            _main.initDropDownGroups();
            _main.initSecurityGroupDetail();
            _main.initLoginDetail();
        });
        _main.initSecurityGroupDetail();
    }
    initSecurityGroupDetail() {
        var detail = _main._configGroup;
        if (!detail.Folders)
            detail.Folders = [];
        SWIUtil.InitStandardInput("#config-group-name", detail.Name, null, function (val) { detail.Name = val; });
        SWIUtil.InitBoolSelect("#config-group-editconfiguration", detail.EditConfiguration, SWIUtil.tr("Yes (User is administrator of the Web Server)"), SWIUtil.tr("No"), function (val) { detail.EditConfiguration = val; });
        SWIUtil.InitBoolSelect("#config-group-editprofile", detail.EditProfile, SWIUtil.tr("Yes (User can edit his profile)"), SWIUtil.tr("No"), function (val) { detail.EditProfile = val; });
        var $select = $("#config-group-personalfolder");
        $select.unbind("change");
        $select.selectpicker("destroy");
        $select.empty();
        $select.append(SWIUtil.GetOption("0", SWIUtil.tr("No personal folder"), detail.PersFolderRight));
        $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Personal folder for files only"), detail.PersFolderRight));
        $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Personal folder for reports and files"), detail.PersFolderRight));
        $select.unbind("change").on("change", (event) => {
            detail.PersFolderRight = $(event.target).val();
        });
        $select.selectpicker();
        var $selectAgent = $("#config-group-agent");
        $selectAgent.unbind("change");
        $selectAgent.selectpicker("destroy");
        $selectAgent.empty();
        if (_main._config.agents) {
            var guids = detail.AgentGUIDs || [];
            $.each(_main._config.agents, function (key, value) {
                var $opt = $("<option>").attr("value", value.Key).html(value.Value);
                if (guids.indexOf(value.Key) >= 0)
                    $opt.attr("selected", "true");
                $selectAgent.append($opt);
            });
        }
        $selectAgent.unbind("change").on("change", (event) => {
            detail.AgentGUIDs = $(event.target).val() || [];
        });
        $selectAgent.selectpicker();
        _main.initDropDownGroupsFolders();
    }
    initDropDownGroupsFolders() {
        if (_main._configGroupFolder != null)
            _main._configGroupFolder = _main._configGroup.Folders.find((i) => i.Path === _main._configGroupFolder.Path);
        if (!_main._configGroupFolder && _main._configGroup.Folders.length > 0)
            _main._configGroupFolder = _main._configGroup.Folders[0];
        var $ddname = $("#config-group-folders-dropdown");
        $ddname.empty();
        $.each(_main._configGroup.Folders, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Path, value.Path, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New folder configuration"), "", "folder", "fa-solid fa-circle-plus")));
        if (_main._configGroupFolder && _main._configGroup.Folders.length > 0) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configGroupFolder.Path, "", "remove", "fa-solid fa-circle-minus")));
        }
        $("#config-group-folders-dropdown > li > a").unbind("click").on("click", (event) => {
            var type = $(event.target).prop("type");
            var id = $(event.target).prop("id");
            if (type == "select") {
                _main._configGroupFolder = _main._configGroup.Folders.find((v) => v.Path === id);
            }
            else if (type == "folder") {
                var newFolder = {
                    Path: "\\",
                    FolderRight: 4,
                    ManageFolder: true,
                    UseSubFolders: true,
                    DownloadUpload: 0,
                    Icon: "",
                };
                _main._configGroup.Folders.push(newFolder);
                _main._configGroupFolder = newFolder;
            }
            else {
                var index = _main._configGroup.Folders.indexOf(_main._configGroupFolder);
                if (index != -1)
                    _main._configGroup.Folders.splice(index, 1);
                _main._configGroupFolder = null;
            }
            _main.initDropDownGroupsFolders();
            _main.initSecurityFolderDetail();
        });
        _main.initSecurityFolderDetail();
    }
    initSecurityFolderDetail() {
        var detail = _main._configGroupFolder;
        SWIUtil.ShowHideControl($(".config-group-folder"), detail);
        if (!detail) {
            $("#config-group-folder-name").val("<" + SWIUtil.tr2("No folder configuration") + ">");
        }
        else {
            $("#config-group-folder-name").val(SWIUtil.tr2("Configuration for") + " " + detail.Path);
            var $select = $("#config-group-folder-select");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $.each(_main._config.folders, function (key, value) {
                $select.append(SWIUtil.GetOption(value.Key, value.Key, _main._configGroupFolder.Path, "fa fa-folder-o"));
            });
            $select.unbind("change").on("change", (event) => {
                _main._configGroupFolder.Path = $(event.target).val();
                _main.initDropDownGroupsFolders();
            });
            $select.selectpicker();
            var $select = $("#config-group-folder-right");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("No right"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Execute reports / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute reports and outputs / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Edit schedules / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("4", SWIUtil.tr("Edit reports / Manage files"), detail.FolderRight));
            $select.unbind("change").on("change", (event) => {
                _main._configGroupFolder.FolderRight = $(event.target).val();
            });
            $select.selectpicker();
            SWIUtil.InitBoolSelect("#config-group-folder-manage", detail.ManageFolder, SWIUtil.tr("Yes (User can create and edit sub-folders)"), SWIUtil.tr("No"), function (val) { detail.ManageFolder = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-showsub", detail.UseSubFolders, SWIUtil.tr("Yes (User can browse sub-folders)"), SWIUtil.tr("No"), function (val) { detail.UseSubFolders = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-expand", detail.ExpandSubFolders, SWIUtil.tr("Yes (Folder is expanded in the tree view)"), SWIUtil.tr("No"), function (val) { detail.ExpandSubFolders = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-filesonly", detail.FilesOnly, SWIUtil.tr("Yes (Only files can be stored)"), SWIUtil.tr("No"), function (val) { detail.FilesOnly = val; });
            var $select = $("#config-group-folder-downloadupload");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("No download (except files) or upload"), detail.DownloadUpload));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("User can download reports"), detail.DownloadUpload));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("User can download reports and upload reports and files"), detail.DownloadUpload));
            $select.unbind("change").on("change", (event) => {
                detail.DownloadUpload = $(event.target).val();
            });
            $select.selectpicker();
            var $icon = $("#config-group-folder-icon");
            $icon.val(detail.Icon || "");
            $icon.unbind("change").on("change", (event) => {
                detail.Icon = $(event.target).val();
            });
        }
    }
    initDropDownLogins() {
        if (_main._configLogin != null)
            _main._configLogin = _main._config.logins.find((i) => i.Id === _main._configLogin.Id);
        if (!_main._configLogin && _main._config.logins.length > 0)
            _main._configLogin = _main._config.logins[0];
        var $ddname = $("#config-logins-dropdown");
        $ddname.empty();
        $.each(_main._config.logins, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Id, value.Id, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New login"), "", "login", "fa-solid fa-circle-plus")));
        if (_main._configLogin) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").append($("<hr/>").addClass("dropdown-divider")));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configLogin.Id, "", "remove", "fa-solid fa-circle-minus")));
        }
        $("#config-logins-dropdown > li > a").unbind("click").on("click", (event) => {
            var type = $(event.target).prop("type");
            var id = $(event.target).prop("id");
            if (type == "select") {
                _main._configLogin = _main._config.logins.find((v) => v.Id === id);
            }
            else if (type == "login") {
                var newFolder = {
                    GUID: SWIUtil.Newguid(),
                    Id: "new login",
                };
                _main._config.logins.push(newFolder);
                _main._configLogin = newFolder;
            }
            else {
                var index = _main._config.logins.indexOf(_main._configLogin);
                if (index != -1)
                    _main._config.logins.splice(index, 1);
                _main._configLogin = null;
            }
            _main.initDropDownLogins();
            _main.initLoginDetail();
        });
        _main.initLoginDetail();
    }
    initLoginDetail() {
        var detail = _main._configLogin;
        SWIUtil.ShowHideControl($(".config-login"), detail);
        if (!detail) {
            $("#config-login-id").val("<" + SWIUtil.tr2("No login") + ">");
        }
        else {
            SWIUtil.InitStandardInput("#config-login-id", detail.Id, null, function (val) { detail.Id = val; });
            SWIUtil.InitStandardInput("#config-login-name", detail.Name, null, function (val) { detail.Name = val; });
            SWIUtil.InitStandardInput("#config-login-email", detail.Email, null, function (val) { detail.Email = val; });
            SWIUtil.InitStandardInput("#config-login-password", "", null, function (val) { detail.Password = detail.HashedPassword + val; });
            var select = $("#config-login-groups");
            select.selectpicker("destroy");
            select.empty();
            $.each(_main._config.groups, function (index, value) {
                select.append(SWIUtil.GetOption(value.GUID, value.Name, (detail.GroupIds && detail.GroupIds.some((i) => i === value.GUID)) ? value.GUID : ""));
            });
            select.unbind("change").on("change", (event) => {
                _main._configLogin.GroupIds = $(event.target).val();
            });
            select.selectpicker();
        }
    }
    search() {
        SWIUtil.ShowModal($waitDialog);
        _gateway.Search(_main._folder.path, $("#search-pattern").val(), function (data) {
            _main._searchMode = true;
            _main.buildReportsTable(data);
            SWIUtil.HideModal($waitDialog);
        });
    }
    loginFailure(data, firstTry) {
        SWIUtil.HideModal($waitDialog);
        if (!firstTry)
            $("#login-modal-error").text(data.error);
        _main.showLogin();
        _main.enableControls();
    }
    showLogin() {
        SWIUtil.HideModal($waitDialog);
        $("body").children(".modal-backdrop").remove();
        $("#footer-div").show();
        SWIUtil.ShowModal($loginModal);
    }
    showSecurityCode(data) {
        if (data)
            $("#security-code-message").text(data.message);
        SWIUtil.HideModal($waitDialog);
        $("body").children(".modal-backdrop").remove();
        $securityModal.show();
        SWIUtil.ShowModal($securityModal);
    }
    login() {
        SWIUtil.HideModal($loginModal);
        SWIUtil.ShowModal($waitDialog);
        _gateway.Login($("#username").val(), $("#password").val(), function (data) {
            $("#password").val("");
            if (data.securitycoderequired) {
                _main.showSecurityCode(data);
            }
            else {
                _main.loginSuccess(data);
            }
        }, function (data) {
            _main.loginFailure(data, false);
        });
    }
    checkSecurityCode() {
        SWIUtil.HideModal($securityModal);
        SWIUtil.ShowModal($waitDialog);
        _gateway.CheckSecurityCode($("#securitycode").val(), function (data) {
            $("#securitycode").val("");
            $("#security-modal-error").text("");
            if (data.login) {
                _main.showLogin();
            }
            else {
                _main.loginSuccess(data);
            }
        }, function (data) {
            SWIUtil.HideModal($waitDialog);
            $("#security-modal-error").text(data.error);
            _main.showSecurityCode(null);
            _main.enableControls();
        });
    }
    resize() {
        const wh = $(window).height() ?? 0;
        if (!SWIUtil.IsMobile()) {
            //Size each scrollable panel from its actual top to the bottom of the viewport,
            //so the panel itself scrolls and the window never overflows (no spurious scrollbar).
            const bottomMargin = 12;
            const $tree = $("#folder-tree");
            if ($tree.is(":visible"))
                $tree.height(wh - ($tree.offset()?.top ?? 0) - bottomMargin);
            const $table = $("#file-table-view");
            if ($table.is(":visible"))
                $table.height(wh - ($table.offset()?.top ?? 0) - bottomMargin);
        }
        else {
            $("#folder-tree").css("max-height", wh / 2 - 45);
        }
    }
    enableControls() {
        let right = 0; //1 Execute,2 Shedule,3 Edit
        let files = false;
        let showFolders = false;
        if (_main._folder) {
            right = _main._folder.right;
            files = _main._folder.files;
        }
        if (_main._profile) {
            showFolders = _main._profile.showfolders;
        }
        $outputPanel.hide();
        SWIUtil.HideTooltip($('#back-to-top'));
        SWIUtil.EnableButton($("#report-edit-lightbutton"), right >= folderRightEdit && !files);
        SWIUtil.ShowHideControl($("#report-edit-lightbutton"), hasEditor);
        const checked = $(".report-checkbox:checked").length;
        SWIUtil.EnableButton($("#report-rename-lightbutton"), checked == 1 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-delete-lightbutton"), checked != 0 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-cut-lightbutton"), checked != 0 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-copy-lightbutton"), checked != 0 && right > 0);
        SWIUtil.EnableButton($("#report-paste-lightbutton"), (this._clipboard != null && this._clipboard.length > 0) && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-shortcut-lightbutton"), (this._clipboard != null && this._clipboard.length > 0) && right >= folderRightEdit);
        SWIUtil.ShowHideControl($("#report-upload-lightbutton"), _main._folder && _main._folder.downloadupload > 1 && right >= folderRightEdit);
        SWIUtil.ShowHideControl($("#folders-nav-item"), _main._folder ? _main._folder.manage > 0 : false);
        SWIUtil.ShowHideControl($("#file-menu"), _main._canEdit);
        SWIUtil.ShowHideControl($("#nav_button"), _main._reportPath != null && _main._reportPath != "");
        //Report or folders view
        const reportShown = _main._reportPath != "" && _main._currentView == "report";
        SWIUtil.ShowHideControl($("#menu-view-folders"), _main._currentView == "report" && showFolders);
        SWIUtil.ShowHideControl($("#menu-view-report"), _main._reportPath != "" && _main._currentView != "report");
        SWIUtil.ShowHideControl($(".reportview"), reportShown);
        SWIUtil.ShowHideControl($(".folderview"), _main._currentView != "report");
        //Dividers
        SWIUtil.ShowHideControl($(".menu-divider-folders-report"), (_main._reportPath != "" || _main._currentView == "report") && showFolders);
        //title color
        $("#nav_button").css("color", reportShown ? "#fff" : "#9d9d9d").css("outline", "none");
        $("#search-pattern").css("background", _main._searchMode ? "orange" : "white");
        //the visible view just changed (folders <-> report): re-size the scrollable panels
        _main.resize();
    }
    loadFolderTree() {
        _gateway.GetRootFolders(function (data) {
            if (!_main._tree)
                _main._tree = new SWIFolderTree(document.getElementById("folder-tree"));
            //build the tree; load() returns whether any folder grants the edit right
            _main._canEdit = _main._tree.load(data);
            _main._tree.onChanged(function () {
                _main.ReloadReportsTable();
            });
            setTimeout(function () {
                if (!_main._profile.folder || _main._profile.folder == "" || !_main._tree.getNode(_main._profile.folder))
                    _main._profile.folder = "";
                _main._folderpath = _main._profile.folder;
                _main._tree.deselectAll();
                if (_main._folderpath)
                    _main._tree.selectNode(_main._folderpath);
            }, 500);
        });
    }
    ReloadReportsTable() {
        _main.LoadReports(_main._tree.getSelected());
    }
    LoadReports(path) {
        if (!path)
            return;
        SWIUtil.StartSpinning();
        _gateway.GetFolderDetail(path, function (data) {
            _main._searchMode = false;
            _main._folder = data.folder;
            var selNode = _main._tree.getSelectedNode();
            _main._folder.isEmpty = (data.files.length == 0 && (!selNode || selNode.children.length == 0));
            _main.buildReportsTable(data);
            _main._profile.folder = path;
            SWIUtil.StopSpinning();
        });
    }
    refreshMenus() {
        setTimeout(function () {
            SWIUtil.RefreshMenu(_main);
            if (_editor)
                _editor.agentMenu();
        }, 1000);
    }
    executeReport(path, viewGUID, outputGUID) {
        _gateway.ExecuteReport(path, viewGUID, outputGUID);
        _main.refreshMenus();
    }
    executeReportFromMenu(path, viewGUID, outputGUID, name) {
        $(".navbar-header,#navbar").addClass("disabled");
        $(".fixedHeader-floating").empty();
        $("#report-body").empty();
        $(window).unbind("scroll");
        $("#nav_button").html(name);
        _main._reportPath = path;
        _main._lastReport.path = path;
        _main._lastReport.viewGUID = viewGUID;
        _main._lastReport.outputGUID = outputGUID;
        _main._lastReport.name = name;
        _main.toggleFoldersReport(true);
        //clear all timers
        var maxId = setTimeout(function () { }, 0);
        for (var i = 0; i < maxId; i += 1) {
            clearTimeout(i);
        }
        _gateway.ExecuteReportFromMenu(_main._reportPath, viewGUID, outputGUID, function (data) {
            _main.toggleFoldersReport(true);
            $(".navbar-header,#navbar").removeClass("disabled");
            $("#report-body").html(data);
            initScrollReport();
            _main.enableControls();
            SWIUtil.HideModal($waitDialog);
            setTimeout(function () {
                _main.refreshMenus();
                _main._lastReport.name = $("#nav_button").text();
            }, 1000);
        });
    }
    clearFilesTable() {
        var $tableHead = $("#file-table-head");
        var $tableBody = $("#file-table-body");
        if (!$("#file-table-head").is(':empty')) {
            const table = new DataTable('#file-table');
            table.destroy();
        }
        $tableHead.empty();
        $tableBody.empty();
    }
    buildReportsTable(data) {
        var $tableHead = $("#file-table-head");
        var $tableBody = $("#file-table-body");
        _main.clearFilesTable();
        //Header
        var $tr = $("<tr>");
        $tableHead.append($tr);
        if (_main._canEdit)
            $tr.append($("<th style='width:22px;' class='nosort d-none d-sm-table-cell'><input id='selectall-checkbox' type='checkbox'/></th>"));
        $tr.append($("<th>").html(SWIUtil.tr("Report")));
        $tr.append($("<th style='width:200px;min-width:200px;text-align:right;white-space:nowrap' class='d-none d-sm-table-cell'>").html(SWIUtil.tr("Last modification")));
        //Body
        for (var i = 0; i < data.files.length; i++) {
            var file = data.files[i];
            if (file.right == 0)
                continue;
            $tr = $("<tr>");
            $tableBody.append($tr);
            //the checkbox (cut/copy/delete/rename) always acts on the file itself; for a shortcut the actions act on the resolved target
            if (_main._canEdit)
                $tr.append($("<td class='d-none d-sm-table-cell' style='padding:8px'>").append($("<input>").addClass("report-checkbox").prop("type", "checkbox").data("path", file.path)));
            var actionPath = (file.isshortcut && !file.broken && file.targetpath) ? file.targetpath : file.path;
            var $nameTd = $("<td>").data("path", actionPath).data("name", file.name).data("isReport", file.isreport);
            var $nameWrapper = $("<div>").addClass("report-name-cell");
            if (file.isshortcut) {
                var scTitle = file.broken ? SWIUtil.tr("Shortcut target not found") : (SWIUtil.tr("Shortcut to") + " " + file.targetpath);
                $nameWrapper.append($("<span>").addClass("report-shortcut-icon " + (file.broken ? "fa-solid fa-triangle-exclamation report-broken" : "fa-solid fa-share-nodes")).prop("title", scTitle));
            }
            $nameWrapper.append($("<a>").addClass("report-name" + (file.broken ? " report-broken" : "")).data("path", actionPath).data("isReport", file.isreport).text(file.name));
            var $td = $("<div>").addClass("report-actions").data("path", actionPath).data("name", file.name).data("isReport", file.isreport);
            $nameWrapper.append($td);
            $nameTd.append($nameWrapper);
            $tr.append($nameTd);
            if (file.isreport && !file.broken) {
                if (_main._reportIcon !== null) {
                    var iconButton = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2(_main._newWindow ? "Execute report in the current window" : "Execute report in a new window")).addClass("btn btn-secondary btn-table report-execute");
                    iconButton.append($("<span class='" + _main._reportIcon + "'></span>"));
                    $td.append(iconButton);
                }
                var button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Views and outputs")).addClass("btn btn-secondary btn-table report-output");
                button.append($("<span class='fa-solid fa-table-list'></span>"));
                $td.append(button);
                if (file.right >= folderRightSchedule && hasEditor) {
                    button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Edit report")).addClass("btn btn-secondary btn-table report-edit d-none d-sm-inline-block");
                    button.append($("<span class='fa-solid fa-pencil'></span>"));
                    $td.append(button);
                }
                if (_main._folder && _main._folder.downloadupload > 0) {
                    button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Download report")).addClass("btn btn-secondary btn-table report-download d-none d-sm-inline-block");
                    button.append($("<span class='fa-solid fa-circle-down'></span>"));
                    $td.append(button);
                }
                button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Mark as favorite")).addClass("btn btn-secondary btn-table report-favorite d-none d-sm-inline-block");
                if (file.isfavorite)
                    button.append($("<span class='fa-solid fa-star'></span>"));
                else
                    button.append($("<span class='fa-regular fa-star'></span>"));
                $td.append(button);
            }
            $tr.append($("<td>").css({ "text-align": "right", "white-space": "nowrap" }).addClass("d-none d-sm-table-cell").text(file.last));
        }
        if (_main._canEdit) {
            var $cb = $("#selectall-checkbox");
            $cb.prop("checked", false);
            $cb.unbind("click").bind("click", function () {
                $(".report-checkbox").each(function (key, value) {
                    var isChecked = $cb.is(':checked');
                    $(value).prop("checked", isChecked);
                });
                _main.enableControls();
            });
        }
        $(".report-name").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var $target = $(e.currentTarget);
            if ($target.hasClass("report-broken")) {
                SWIUtil.ShowMessage("alert-danger", SWIUtil.tr("Shortcut target not found"), 5000);
                return;
            }
            const path = $target.data("path");
            const name = $target.data("name");
            if ($target.data("isReport")) {
                if (_main._newWindow) {
                    _main.executeReport(path, "", "");
                }
                else {
                    SWIUtil.ShowModal($waitDialog);
                    _main.executeReportFromMenu(path, "", "", name);
                }
            }
            else
                _gateway.ViewFile(path);
        });
        $(".report-execute").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            const path = $(e.currentTarget).parent().data("path");
            const name = $(e.currentTarget).parent().data("name");
            if (!_main._newWindow) {
                _main.executeReport(path, "", "");
            }
            else {
                _main.executeReportFromMenu(path, "", "", name);
            }
        });
        $(".report-download").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            const path = $(e.currentTarget).parent().data("path");
            _gateway.ViewFile(path);
        });
        $(".report-favorite").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            const path = $(e.currentTarget).parent().data("path");
            _gateway.MarkFavorite(path, function (data) {
                SWIUtil.ShowMessage("alert-success", data.Message, 5000);
                var icon = $(e.currentTarget).children("span");
                icon.toggleClass("fa-solid");
                icon.toggleClass("fa-regular");
                SWIUtil.RefreshMenu(_main);
            });
        });
        $(".report-output").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var $target = $(e.currentTarget);
            var $tableBody = $("#output-panel-content");
            var top = ($target.offset()?.top ?? 0) - 30;
            $tableBody.empty();
            $tableBody.append($("<div class='output-item output-loading'>").append($("<span class='spinner-border spinner-border-sm me-1 align-middle' role='status' aria-hidden='true'>")).append($("<span>").html(" " + SWIUtil.tr("Please wait") + "...")));
            $outputPanel.css({
                'display': 'inline',
                'position': 'absolute',
                'z-index': '10000',
                'left': ($target.offset()?.left ?? 0) - ($outputPanel.width() ?? 0),
                'top': top,
                'opacity': 0.6
            }).show();
            $("#output-panel-close").unbind("click").on("click", function () {
                $outputPanel.hide();
            });
            $outputPanel.unbind("mouseleave").on("mouseleave", function () {
                $outputPanel.hide();
            });
            _gateway.GetReportDetail($target.parent().data("path"), function (data) {
                $outputPanel.css("opacity", "1");
                $tableBody.empty();
                for (var i = 0; i < data.views.length; i++) {
                    var $item = $("<div>").addClass("output-item");
                    $item.append($("<a>").data("viewguid", data.views[i].guid).addClass("output-name").text(data.views[i].displayname));
                    $item.append($("<span>").addClass("output-type-badge").html(SWIUtil.tr("View")));
                    if (_main._reportIcon !== null) {
                        var button = $("<button>").data("viewguid", data.views[i].guid).data("name", data.views[i].displayname).prop("type", "button").addClass("btn btn-secondary btn-table output-execute");
                        button.append($("<span class='" + _main._reportIcon + "'></span>"));
                        $item.append(button);
                    }
                    $tableBody.append($item);
                }
                for (var i = 0; i < data.outputs.length; i++) {
                    var $item = $("<div>").addClass("output-item");
                    $item.append($("<a>").data("outputguid", data.outputs[i].guid).addClass("output-name").text(data.outputs[i].displayname));
                    $item.append($("<span>").addClass("output-type-badge output-type-output").html(SWIUtil.tr("Output")));
                    if (_main._reportIcon !== null) {
                        var button = $("<button>").data("outputguid", data.outputs[i].guid).data("name", data.outputs[i].displayname).prop("type", "button").addClass("btn btn-secondary btn-table output-execute");
                        button.append($("<span class='" + _main._reportIcon + "'></span>"));
                        $item.append(button);
                    }
                    $tableBody.append($item);
                }
                //adjust position with the final size
                top = ($target.offset()?.top ?? 0) + 40 - ($outputPanel.height() ?? 0);
                $outputPanel.css({ top: top, left: ($target.offset()?.left ?? 0) - ($outputPanel.width() ?? 0), position: 'absolute' });
                $(".output-execute").unbind("click").on("click", function (e) {
                    $outputPanel.hide();
                    if (!_main._newWindow) {
                        _main.executeReport($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"));
                    }
                    else {
                        SWIUtil.ShowModal($waitDialog);
                        _main.executeReportFromMenu($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"), $target.parent().data("name"));
                    }
                });
                $(".output-name").unbind("click").on("click", function (e) {
                    $outputPanel.hide();
                    if (!_main._newWindow) {
                        SWIUtil.ShowModal($waitDialog);
                        _main.executeReportFromMenu($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"), $target.parent().data("name"));
                    }
                    else {
                        _main.executeReport($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"));
                    }
                });
            }, function (data) {
                SWIUtil.ShowMessage("alert-danger", data.error, 0);
                $outputPanel.hide();
            });
        });
        $("#file-table-view").scroll(function () {
            $outputPanel.hide();
        });
        if (_editor)
            _editor.init();
        var isMobile = SWIUtil.IsMobile();
        new DataTable($('#file-table'), {
            bSort: true,
            stateSave: true,
            aaSorting: [],
            bPaginate: !isMobile,
            sPaginationType: "full_numbers",
            iDisplayLength: 25,
            bInfo: !isMobile,
            bFilter: !isMobile,
            bAutoWidth: false,
            responsive: true,
            oLanguage: {
                oPaginate: {
                    sFirst: "|&lt;",
                    sPrevious: "&lt;&lt;",
                    sNext: ">>",
                    sLast: ">|"
                },
                sZeroRecords: SWIUtil.tr("No report"),
                sLengthMenu: SWIUtil.tr("Show _MENU_ reports"),
                sInfoPostFix: "",
            },
            columnDefs: [{
                    targets: 'nosort',
                    orderable: false
                }]
        });
        $(".report-checkbox").unbind("click").on("click", function () {
            _main.enableControls();
        });
        $('#file-table').unbind("page.dt").on('page.dt', function () {
            setTimeout(function () {
                $(".report-checkbox").unbind("click").on("click", function () {
                    _main.enableControls();
                });
            }, 200);
        });
        _main.enableControls();
    }
    showTreeView(show) {
        if (!show) {
            $("#folder-view").hide();
            $("#file-view").removeClass("col-sm-8").addClass("col-sm-12");
        }
        else {
            $("#folder-view").show();
            $("#file-view").removeClass("col-sm-12").addClass("col-sm-8");
        }
    }
    toggleFoldersReport(viewreport, foldertoselect = "") {
        _main._currentView = (viewreport || !_main._profile.showfolders ? "report" : "folders");
        if (foldertoselect) { //Select a folder in the treeview
            _main._folderpath = foldertoselect;
            _main._tree.deselectAll();
            _main._tree.selectNode(_main._folderpath);
        }
        if (!viewreport)
            redrawDataTables();
        _main.enableControls();
        //transition
        const reportShown = _main._reportPath && _main._currentView == "report";
        $(!reportShown ? ".reportview" : ".folderview").css("opacity", "0.2");
        $(reportShown ? ".reportview" : ".folderview").css("opacity", "1");
    }
}
//Lightweight folder treeview built on plain DOM + Bootstrap 5 styling.
//Replaces the former jQuery jstree plugin; exposes only the small API the web
//interface actually used (load / selection / node lookup / change notification).
class SWIFolderTree {
    constructor(container) {
        this._nodes = {};
        this._selected = null;
        this._onChanged = null;
        //Font Awesome icon per folder type (was the jstree "types" config)
        this._icons = {
            "default": "fa-regular fa-folder",
            "bin": "fa-regular fa-trash-can",
            "personal": "fa-solid fa-user",
            "reports": "fa-solid fa-chart-bar",
            "repository": "fa-regular fa-folder"
        };
        this._container = container;
    }
    onChanged(cb) { this._onChanged = cb; }
    fireChanged() { if (this._onChanged)
        this._onChanged(); }
    //(Re)build the tree from the nested folder data returned by GetRootFolders.
    //Returns true if any folder grants the edit right (folder.right == 4).
    load(data) {
        var self = this;
        this._container.innerHTML = "";
        this._nodes = {};
        this._selected = null;
        var canEdit = false;
        var rootUl = document.createElement("ul");
        var build = function (folders, parentUl) {
            for (var i = 0; i < folders.length; i++) {
                var f = folders[i];
                if (f.right == 4)
                    canEdit = true;
                var hasChildren = f.folders && f.folders.length > 0;
                var li = document.createElement("li");
                var row = document.createElement("div");
                row.className = "swi-tree-node";
                row.setAttribute("role", "treeitem");
                var toggle = document.createElement("span");
                toggle.className = "swi-tree-toggle fa fa-caret-" + (f.expand ? "down" : "right") + (hasChildren ? "" : " empty");
                row.appendChild(toggle);
                var icon = document.createElement("span");
                icon.className = "swi-tree-icon " + (f.icon ? f.icon : (self._icons[f.type] || self._icons["default"]));
                row.appendChild(icon);
                var label = document.createElement("span");
                label.className = "swi-tree-label";
                label.textContent = (f.name == "" ? "Reports" : f.name);
                row.appendChild(label);
                li.appendChild(row);
                var node = {
                    path: f.path, name: f.name, type: f.type || "default", expand: !!f.expand,
                    children: [], row: row, childUl: null, toggle: hasChildren ? toggle : null
                };
                self._nodes[f.path] = node;
                (function (path) {
                    row.addEventListener("click", function () { self.select(path, true); });
                })(f.path);
                if (hasChildren) {
                    var childUl = document.createElement("ul");
                    childUl.className = "swi-tree-children" + (f.expand ? "" : " collapsed");
                    node.childUl = childUl;
                    (function (path) {
                        toggle.addEventListener("click", function (e) { e.stopPropagation(); self.toggleNode(path); });
                    })(f.path);
                    for (var j = 0; j < f.folders.length; j++)
                        node.children.push(f.folders[j].path);
                    li.appendChild(childUl);
                    build(f.folders, childUl);
                }
                parentUl.appendChild(li);
            }
        };
        build(data, rootUl);
        this._container.appendChild(rootUl);
        //pre-select the root folder (empty name) silently, as jstree did via state.selected
        for (var p in this._nodes) {
            if (this._nodes[p].name == "") {
                this.select(p, false);
                break;
            }
        }
        return canEdit;
    }
    toggleNode(path) {
        var n = this._nodes[path];
        if (!n || !n.childUl)
            return;
        n.expand = !n.expand;
        if (n.expand)
            n.childUl.classList.remove("collapsed");
        else
            n.childUl.classList.add("collapsed");
        if (n.toggle) {
            n.toggle.classList.remove("fa-caret-down", "fa-caret-right");
            n.toggle.classList.add(n.expand ? "fa-caret-down" : "fa-caret-right");
        }
    }
    //Expand every ancestor <ul> so the given node is visible.
    expandAncestors(path) {
        var n = this._nodes[path];
        if (!n)
            return;
        var el = n.row.parentElement ? n.row.parentElement.parentElement : null; //li -> ul
        while (el) {
            if (el.classList && el.classList.contains("swi-tree-children")) {
                el.classList.remove("collapsed");
                var parentLi = el.parentElement;
                var t = parentLi ? parentLi.querySelector(".swi-tree-node > .swi-tree-toggle") : null;
                if (t) {
                    t.classList.remove("fa-caret-right");
                    t.classList.add("fa-caret-down");
                }
            }
            el = el.parentElement;
        }
    }
    //Select a node by path. Fires the change callback only when fire is true.
    select(path, fire) {
        if (!this._nodes[path])
            return;
        if (this._selected && this._nodes[this._selected])
            this._nodes[this._selected].row.classList.remove("selected");
        this._selected = path;
        var node = this._nodes[path];
        node.row.classList.add("selected");
        this.expandAncestors(path);
        if (node.row.scrollIntoView)
            node.row.scrollIntoView({ block: "nearest" });
        if (fire)
            this.fireChanged();
    }
    //Public alias matching the previous jstree call sites (always notifies).
    selectNode(path) { this.select(path, true); }
    deselectAll() {
        var had = this._selected != null;
        if (this._selected && this._nodes[this._selected])
            this._nodes[this._selected].row.classList.remove("selected");
        this._selected = null;
        if (had)
            this.fireChanged();
    }
    getSelected() { return this._selected; }
    getSelectedNode() { return this._selected ? this._nodes[this._selected] : null; }
    getNode(path) { return this._nodes[path]; }
}
//# sourceMappingURL=swi-main.js.map