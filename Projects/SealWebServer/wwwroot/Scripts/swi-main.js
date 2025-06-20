/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/bootstrap/index.d.ts" />
/// <reference path="typings/jstree/jstree.d.ts" />
/// <reference path="typings/main.d.ts" />
var $waitDialog;
var $editDialog;
var $folderTree;
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
    SWIUtil.InitDropDownMenu();
});
var SWIMain = /** @class */ (function () {
    function SWIMain() {
        this._profile = null;
        this._canEdit = false;
        this._folder = null;
        this._searchMode = false;
        this._clipboardCut = false;
        this._folderpath = "\\";
        this._lastReport = new Object();
        this._currentView = "folders";
        this._newWindow = true;
    }
    SWIMain.prototype.Process = function () {
        $waitDialog = $("#wait-dialog");
        $editDialog = $("#edit-dialog");
        $folderTree = $("#folder-tree");
        $loginModal = $("#login-modal");
        $securityModal = $("#security-modal");
        $passwordResetModal = $("#password-reset-modal");
        $passwordResetModal2 = $("#password-reset-modal2");
        $outputPanel = $("#output-panel");
        $propertiesPanel = $("#properties-panel");
        $elementDropDown = $("#element-dropdown");
        $("#menu-main-button").hide();
        $waitDialog.modal('show');
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
        SWIUtil.ShowHideControl($("#disconnect-nav-item,#main-container,#report-body,#menu-view-report,#nav_badge,.reportview,.folderview,#menu-main-button,#profile-nav-item,#config-nav-item,#menu-assistant-button,#search-pattern,#search-nav-item"), false);
        $("#login-modal-submit").unbind("click").on("click", function () {
            _main.login();
        });
        $("#security-modal-submit").unbind("click").on("click", function () {
            _main.checkSecurityCode();
        });
        $("#login-password-reset").unbind("click").on("click", function (e) {
            e.preventDefault();
            $loginModal.modal('hide');
            $("#password-reset-name").text("");
            $passwordResetModal.modal();
        });
        $("#password-reset-submit").unbind("click").on("click", function () {
            _gateway.ResetPassword($("#password-reset-name").val(), function (data) {
                $passwordResetModal.modal('hide');
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
        var token = new URLSearchParams(window.location.search).get('ptoken');
        var guid = new URLSearchParams(window.location.search).get('guid');
        if (token && guid) {
            $waitDialog.modal('hide');
            $("#password-reset-submit2").unbind("click").on("click", function () {
                _gateway.ResetPassword2(guid, token, $("#password-reset1").val(), $("#password-reset2").val(), function (data) {
                    if (data.error)
                        SWIUtil.ShowMessage("alert-danger", data.error, -1);
                    else {
                        $passwordResetModal2.modal('hide');
                        SWIUtil.ShowMessage("alert-success", SWIUtil.tr("Your password has been changed."), 5000);
                        _main.showLogin();
                    }
                }, function (data) {
                    if (data.error)
                        SWIUtil.ShowMessage("alert-danger", data.error, -1);
                });
            });
            $passwordResetModal2.modal();
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
    };
    SWIMain.prototype.loginSuccess = function (data) {
        if (data.securitycoderequired) {
            _main.showSecurityCode(data);
            return;
        }
        if (_main._profile && _main._profile.culture && _main._profile.culture != data.culture) {
            $("body").css("opacity", "0.1");
            location.reload();
        }
        _main._profile = data;
        _main._reportPath = "";
        _main._folder = null;
        _main._searchMode = false;
        _main._clipboard = null;
        _main._clipboardCut = false;
        _main.clearFilesTable();
        _main._newWindow = (_main._profile.executionmode === 1 || (_main._profile.executionmode === 0 && _main._profile.groupexecutionmode === 1));
        _main._reportIcon = (_main._newWindow ? "log-in" : "new-window");
        //disable current window, 0=Default, 1=NewWindow, 2=SameWindow, 3=AlwaysNewWindow
        if (_main._profile.executionmode === 3 || (_main._profile.executionmode === 0 && _main._profile.groupexecutionmode === 3)) {
            _main._newWindow = true;
            _main._reportIcon = null;
        }
        //Reset init state
        $("#menu-main-button").show();
        $("#nav_button,#brand-id").css("pointer-events", "");
        _main.showTreeView(true);
        $("#reload-nav-item,#execute_button,#restrictions_button").addClass("reportview");
        $("#search-pattern").val("");
        $("body").children(".modal-backdrop").remove();
        $loginModal.modal('hide');
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
            _editor.assistantMenu();
        }
        //Refresh
        $("#refresh-nav-item").unbind("click").on("click", function () {
            _main.ReloadReportsTable();
            if (SWIUtil.IsMobile())
                $('.navbar-toggle').click();
            if (_editor)
                _editor.assistantMenu();
        });
        //Reload and execute
        $("#reload-nav-item").unbind("click").on("click", function () {
            $waitDialog.modal();
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
                $("#folder-dialog").modal('hide');
                var newpath = _main._folder.path + (_main._folder.path == dirSeparator ? "" : dirSeparator) + $("#create-folder-name").val();
                _gateway.CreateFolder(newpath, function () {
                    _main._profile.folder = newpath;
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been created"), 5000);
                });
            });
            $("#folder-rename").unbind("click").on("click", function () {
                $("#folder-dialog").modal('hide');
                var newpath = $("#rename-folder-name").val();
                _gateway.RenameFolder(_main._folder.path, newpath, function () {
                    _main._profile.folder = newpath;
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been renamed"), 5000);
                });
            });
            $("#folder-delete").unbind("click").on("click", function () {
                $("#folder-dialog").modal('hide');
                _gateway.DeleteFolder(_main._folder.path, function () {
                    _main._profile.folder = SWIUtil.GetDirectoryName(_main._folder.path);
                    _main.loadFolderTree();
                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The folder has been deleted"), 5000);
                });
            });
            $("#folder-dialog").modal();
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
                SWIUtil.ShowHideControl($("#disconnect-nav-item,#main-container,#report-body,#menu-view-report,#nav_badge,.reportview,.folderview,#menu-main-button,#profile-nav-item,#config-nav-item,#menu-assistant-button,#search-pattern,#search-nav-item"), false);
                _main.showLogin();
                if (SWIUtil.IsMobile())
                    $('.navbar-toggle').click();
            });
        });
        //Delete reports
        $("#report-delete-lightbutton").unbind("click").on("click", function () {
            if (!SWIUtil.IsEnabled($(this)))
                return;
            $outputPanel.hide();
            var checked = $(".report-checkbox:checked").length;
            $("#message-title").html(SWIUtil.tr("Warning"));
            $("#message-text").html(SWIUtil.tr("Do you really want to delete the reports or files selected ?"));
            $("#message-cancel-button").html(SWIUtil.tr("Cancel"));
            $("#message-ok-button").html(SWIUtil.tr("OK"));
            $("#message-ok-button").unbind("click").on("click", function () {
                $("#message-dialog").modal('hide');
                $waitDialog.modal();
                var paths = "";
                $(".report-checkbox:checked").each(function (key, value) {
                    paths += $(value).data("path") + "\n";
                });
                _gateway.DeleteFiles(paths, function () {
                    SWIUtil.ShowMessage("alert-success", checked + " " + SWIUtil.tr("report(s) or file(s) have been deleted"), 5000);
                    _main.ReloadReportsTable();
                    _main.refreshMenus();
                    $waitDialog.modal('hide');
                });
            });
            $("#message-dialog").modal();
        });
        //Rename
        $("#report-rename-lightbutton").unbind("click").on("click", function () {
            if (!SWIUtil.IsEnabled($(this)))
                return;
            $outputPanel.hide();
            var source = $(".report-checkbox:checked").first().data("path");
            if (source) {
                var filename = source.split(dirSeparator).pop();
                var extension = filename.split('.').pop();
                $("#report-name-save").unbind("click").on("click", function () {
                    $waitDialog.modal();
                    var folder = _main._folder.path;
                    var destination = (folder != dirSeparator ? folder : "") + dirSeparator + $("#report-name").val() + "." + extension;
                    $("#report-name-dialog").modal('hide');
                    _gateway.MoveFile(source, destination, false, function () {
                        _main.ReloadReportsTable();
                        _main.refreshMenus();
                        $waitDialog.modal('hide');
                        SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The report or file has been renamed"), 5000);
                    });
                });
                $("#report-name").val(filename.replace(/\.[^/.]+$/, ""));
                $("#report-name-dialog").modal();
            }
        });
        //Copy
        $("#report-copy-lightbutton").unbind("click").on("click", function () {
            if (!SWIUtil.IsEnabled($(this)))
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
        $("#report-cut-lightbutton").unbind("click").on("click", function () {
            if (!SWIUtil.IsEnabled($(this)))
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
        $("#report-paste-lightbutton").unbind("click").on("click", function () {
            if (!SWIUtil.IsEnabled($(this)))
                return;
            $outputPanel.hide();
            if (_main._clipboard && _main._clipboard.length > 0) {
                $waitDialog.modal();
                _main._clipboard.forEach(function (value, index) {
                    var newName = value.split(dirSeparator).pop();
                    var folder = _main._folder.path;
                    var destination = (folder != dirSeparator ? folder : "") + dirSeparator + newName;
                    _gateway.MoveFile(value, destination, !_main._clipboardCut, function () {
                        if (index == _main._clipboard.length - 1) {
                            setTimeout(function () {
                                _main.ReloadReportsTable();
                                _main.refreshMenus();
                                $waitDialog.modal('hide');
                                SWIUtil.ShowMessage("alert-success", _main._clipboard.length.toString() + " " + SWIUtil.tr("report(s) or file(s) processed"), 5000);
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
            var fileInput = e.target;
            if (fileInput.files && fileInput.files.length > 0) {
                var formData = new FormData();
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
            _main.executeReportFromMenu(_main._profile.report, null, null, _main._profile.reportname);
            $("#brand-id").unbind("click").on("click", function () {
                if (_main._reportPath == _main._profile.report)
                    _main.toggleFoldersReport(true);
                else
                    _main.executeReportFromMenu(_main._profile.report, null, null, _main._profile.reportname);
            });
        }
        else {
            _main.toggleFoldersReport(false);
            $waitDialog.modal('hide');
        }
    };
    SWIMain.prototype.initConfiguration = function (profile) {
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
                        $waitDialog.modal();
                        _gateway.SetConfiguration(JSON.stringify(_main._config), function () {
                            SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The configuration has been saved. Please login again if you are impacted by the security."), 5000);
                            $waitDialog.modal('hide');
                            $("#config-dialog").modal('hide');
                        }, function (data) {
                            SWIUtil.ShowMessage("alert-danger", data.error, 0);
                            $waitDialog.modal('hide');
                        });
                    }
                });
                $("#config-dialog").modal();
            });
        });
    };
    SWIMain.prototype.initDropDownGroups = function () {
        if (_main._configGroup != null)
            _main._configGroup = _main._config.groups.find(function (i) { return i.Name === _main._configGroup.Name; });
        if (!_main._configGroup && _main._config.groups.length > 0)
            _main._configGroup = _main._config.groups[0];
        var $ddname = $("#config-groups-dropdown");
        $ddname.empty();
        $.each(_main._config.groups, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Name, value.Name, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New group"), "", "group", "glyphicon glyphicon-plus-sign")));
        if (_main._configGroup && _main._config.groups.length > 1) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configGroup.Name, null, "remove", "glyphicon glyphicon-minus-sign")));
        }
        $("#config-groups-dropdown > li > a").unbind("click").on("click", function () {
            var type = $(this).prop("type");
            var id = $(this).prop("id");
            if (type == "select") {
                _main._configGroup = _main._config.groups.find(function (v) { return v.Name === id; });
            }
            else if (type == "group") {
                var newGroup = {
                    GUID: SWIUtil.Newguid(),
                    Name: SWIUtil.UniqueName(SWIUtil.tr2("New group"), _main._config.groups),
                    Folders: [],
                    EditConfiguration: false,
                    EditProfile: true,
                    PersFolderRight: 2
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
    };
    SWIMain.prototype.initSecurityGroupDetail = function () {
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
        $select.unbind("change").on("change", function (e) {
            detail.PersFolderRight = $(this).val();
        });
        $select.selectpicker('refresh');
        SWIUtil.ShowHideControl($("#config-group-downloadupload-row"), _main._config.downloadupload);
        if (_main._config.downloadupload) {
            var $select = $("#config-group-downloadupload");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("No download (except files) or upload"), detail.DownloadUpload));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("User can download reports"), detail.DownloadUpload));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("User can download reports and upload reports and files"), detail.DownloadUpload));
            $select.unbind("change").on("change", function (e) {
                detail.DownloadUpload = $(this).val();
            });
            $select.selectpicker('refresh');
        }
        _main.initDropDownGroupsFolders();
    };
    SWIMain.prototype.initDropDownGroupsFolders = function () {
        if (_main._configGroupFolder != null)
            _main._configGroupFolder = _main._configGroup.Folders.find(function (i) { return i.Path === _main._configGroupFolder.Path; });
        if (!_main._configGroupFolder && _main._configGroup.Folders.length > 0)
            _main._configGroupFolder = _main._configGroup.Folders[0];
        var $ddname = $("#config-group-folders-dropdown");
        $ddname.empty();
        $.each(_main._configGroup.Folders, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Path, value.Path, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New folder configuration"), "", "folder", "glyphicon glyphicon-plus-sign")));
        if (_main._configGroupFolder && _main._configGroup.Folders.length > 0) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configGroupFolder.Path, null, "remove", "glyphicon glyphicon-minus-sign")));
        }
        $("#config-group-folders-dropdown > li > a").unbind("click").on("click", function () {
            var type = $(this).prop("type");
            var id = $(this).prop("id");
            if (type == "select") {
                _main._configGroupFolder = _main._configGroup.Folders.find(function (v) { return v.Path === id; });
            }
            else if (type == "folder") {
                var newFolder = {
                    Path: "\\",
                    FolderRight: 4,
                    ManageFolder: true,
                    UseSubFolders: true,
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
    };
    SWIMain.prototype.initSecurityFolderDetail = function () {
        var detail = _main._configGroupFolder;
        SWIUtil.ShowHideControl($(".config-group-folder"), detail);
        if (!detail) {
            $("#config-group-folder-name").text("<" + SWIUtil.tr2("No folder configuration") + ">");
        }
        else {
            $("#config-group-folder-name").text(SWIUtil.tr2("Configuration for ") + " " + detail.Path);
            var $select = $("#config-group-folder-select");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $.each(_main._config.folders, function (key, value) {
                $select.append(SWIUtil.GetOption(value.Key, value.Key, _main._configGroupFolder.Path, "fa fa-folder-o"));
            });
            $select.unbind("change").on("change", function (e) {
                _main._configGroupFolder.Path = $(this).val();
                _main.initDropDownGroupsFolders();
            });
            $select.selectpicker('refresh');
            var $select = $("#config-group-folder-right");
            $select.unbind("change");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("No right"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Execute reports / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute reports and outputs / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Edit schedules / View files"), detail.FolderRight));
            $select.append(SWIUtil.GetOption("4", SWIUtil.tr("Edit reports / Manage files"), detail.FolderRight));
            $select.unbind("change").on("change", function (e) {
                _main._configGroupFolder.FolderRight = $(this).val();
            });
            $select.selectpicker('refresh');
            SWIUtil.InitBoolSelect("#config-group-folder-manage", detail.ManageFolder, SWIUtil.tr("Yes (User can create and edit sub-folders)"), SWIUtil.tr("No"), function (val) { detail.ManageFolder = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-showsub", detail.UseSubFolders, SWIUtil.tr("Yes (User can browse sub-folders)"), SWIUtil.tr("No"), function (val) { detail.UseSubFolders = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-expand", detail.ExpandSubFolders, SWIUtil.tr("Yes (Folder is expanded in the tree view)"), SWIUtil.tr("No"), function (val) { detail.ExpandSubFolders = val; });
            SWIUtil.InitBoolSelect("#config-group-folder-filesonly", detail.FilesOnly, SWIUtil.tr("Yes (Only files can be stored)"), SWIUtil.tr("No"), function (val) { detail.FilesOnly = val; });
        }
    };
    SWIMain.prototype.initDropDownLogins = function () {
        if (_main._configLogin != null)
            _main._configLogin = _main._config.logins.find(function (i) { return i.Id === _main._configLogin.Id; });
        if (!_main._configLogin && _main._config.logins.length > 0)
            _main._configLogin = _main._config.logins[0];
        var $ddname = $("#config-logins-dropdown");
        $ddname.empty();
        $.each(_main._config.logins, function (key, value) {
            var fa = "fa fa-users-o";
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(value.Id, value.Id, "select", fa)));
        });
        if ($ddname.children().length > 0)
            $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
        $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("New login"), "", "login", "glyphicon glyphicon-plus-sign")));
        if (_main._configLogin && _main._config.logins.length > 1) {
            if ($ddname.children().length > 0)
                $ddname.append($("<li/>").attr("role", "separator").addClass("divider"));
            $ddname.append($("<li/>").append(SWIUtil.GetAnchorWithIcon(SWIUtil.tr2("Remove") + " " + _main._configLogin.Id, null, "remove", "glyphicon glyphicon-minus-sign")));
        }
        $("#config-logins-dropdown > li > a").unbind("click").on("click", function () {
            var type = $(this).prop("type");
            var id = $(this).prop("id");
            if (type == "select") {
                _main._configLogin = _main._config.logins.find(function (v) { return v.Id === id; });
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
    };
    SWIMain.prototype.initLoginDetail = function () {
        var detail = _main._configLogin;
        SWIUtil.InitStandardInput("#config-login-id", _main._configLogin.Id, null, function (val) { detail.Id = val; });
        SWIUtil.InitStandardInput("#config-login-name", _main._configLogin.Name, null, function (val) { detail.Name = val; });
        SWIUtil.InitStandardInput("#config-login-email", _main._configLogin.Email, null, function (val) { detail.Email = val; });
        SWIUtil.InitStandardInput("#config-login-password", "", null, function (val) { detail.Password = detail.HashedPassword + val; });
        var select = $("#config-login-groups");
        select.selectpicker("destroy");
        select.empty();
        $.each(_main._config.groups, function (index, value) {
            select.append(SWIUtil.GetOption(value.GUID, value.Name, (detail.GroupNames && detail.GroupNames.some(function (i) { return i === value.GUID; })) ? value.GUID : ""));
        });
        select.unbind("change").on("change", function (e) {
            _main._configLogin.GroupNames = $(this).val();
        });
        select.selectpicker('refresh');
    };
    SWIMain.prototype.search = function () {
        $waitDialog.modal();
        _gateway.Search(_main._folder.path, $("#search-pattern").val(), function (data) {
            _main._searchMode = true;
            _main.buildReportsTable(data);
            $waitDialog.modal('hide');
            if (SWIUtil.IsMobile())
                $('.navbar-toggle').click();
        });
    };
    SWIMain.prototype.loginFailure = function (data, firstTry) {
        $waitDialog.modal('hide');
        if (!firstTry)
            $("#login-modal-error").text(data.error);
        _main.showLogin();
        _main.enableControls();
    };
    SWIMain.prototype.showLogin = function () {
        $waitDialog.modal('hide');
        $("body").children(".modal-backdrop").remove();
        $("#footer-div").show();
        $loginModal.modal();
    };
    SWIMain.prototype.showSecurityCode = function (data) {
        if (data)
            $("#security-code-message").text(data.message);
        $waitDialog.modal('hide');
        $("body").children(".modal-backdrop").remove();
        $securityModal.show();
        $securityModal.modal();
    };
    SWIMain.prototype.login = function () {
        $loginModal.modal('hide');
        $waitDialog.modal();
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
    };
    SWIMain.prototype.checkSecurityCode = function () {
        $securityModal.modal('hide');
        $waitDialog.modal();
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
            $waitDialog.modal('hide');
            $("#security-modal-error").text(data.error);
            _main.showSecurityCode(null);
            _main.enableControls();
        });
    };
    SWIMain.prototype.resize = function () {
        if (!SWIUtil.IsMobile()) {
            $("#folder-tree").height($(window).height() - 80);
            $("#file-table-view").height($(window).height() - 125);
        }
        else {
            $("#folder-tree").css("max-height", ($(window).height() / 2 - 45));
        }
    };
    SWIMain.prototype.enableControls = function () {
        var right = 0; //1 Execute,2 Shedule,3 Edit
        var files = false;
        var showFolders = false;
        if (_main._folder) {
            right = _main._folder.right;
            files = _main._folder.files;
        }
        if (_main._profile) {
            showFolders = _main._profile.showfolders;
        }
        $outputPanel.hide();
        $('#back-to-top').tooltip('hide');
        SWIUtil.EnableButton($("#report-edit-lightbutton"), right >= folderRightEdit && !files);
        SWIUtil.ShowHideControl($("#report-edit-lightbutton"), hasEditor);
        var checked = $(".report-checkbox:checked").length;
        SWIUtil.EnableButton($("#report-rename-lightbutton"), checked == 1 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-delete-lightbutton"), checked != 0 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-cut-lightbutton"), checked != 0 && right >= folderRightEdit);
        SWIUtil.EnableButton($("#report-copy-lightbutton"), checked != 0 && right > 0);
        SWIUtil.EnableButton($("#report-paste-lightbutton"), (this._clipboard != null && this._clipboard.length > 0) && right >= folderRightEdit);
        SWIUtil.ShowHideControl($("#report-upload-lightbutton"), _main._profile && _main._profile.downloadupload > 1 && right >= folderRightEdit);
        SWIUtil.ShowHideControl($("#folders-nav-item"), _main._folder ? _main._folder.manage > 0 : false);
        SWIUtil.ShowHideControl($("#file-menu"), _main._canEdit);
        SWIUtil.ShowHideControl($("#nav_button"), _main._reportPath != null && _main._reportPath != "");
        //Report or folders view
        var reportShown = _main._reportPath && _main._currentView == "report";
        SWIUtil.ShowHideControl($("#menu-view-folders"), _main._currentView == "report" && showFolders);
        SWIUtil.ShowHideControl($("#menu-view-report"), _main._reportPath && _main._currentView != "report");
        SWIUtil.ShowHideControl($(".reportview"), reportShown);
        SWIUtil.ShowHideControl($(".folderview"), _main._currentView != "report");
        //Dividers
        SWIUtil.ShowHideControl($(".menu-divider-folders-report"), (_main._reportPath != "" || _main._currentView == "report") && showFolders);
        //title color
        $("#nav_button").css("color", reportShown ? "#fff" : "#9d9d9d");
        $("#search-pattern").css("background", _main._searchMode ? "orange" : "white");
    };
    SWIMain.prototype.toJSTreeFolderData = function (data, result, parent) {
        for (var i = 0; i < data.length; i++) {
            var folder = data[i];
            result[result.length] = { "id": folder.path, "parent": parent, "text": (folder.name == "" ? "Reports" : folder.name), "state": { "opened": folder.expand, "selected": (folder.name == "") } };
            if (folder.folders && folder.folders.length > 0)
                _main.toJSTreeFolderData(folder.folders, result, folder.path);
            if (folder.right == 4)
                _main._canEdit = true;
        }
        return result;
    };
    SWIMain.prototype.loadFolderTree = function () {
        _gateway.GetRootFolders(function (data) {
            _main._canEdit = false;
            var result = [];
            $folderTree.jstree("destroy").empty();
            $folderTree.jstree({
                core: {
                    "animation": 0,
                    "themes": { "responsive": true, "stripes": true },
                    'data': _main.toJSTreeFolderData(data, result, "#")
                },
                types: {
                    "default": {
                        "icon": "fa fa-folder-o"
                    }
                },
                plugins: ["types", "wholerow"]
            });
            $folderTree.unbind("changed.jstree").on("changed.jstree", function () {
                _main.ReloadReportsTable();
            });
            setTimeout(function () {
                if (!_main._profile.folder || _main._profile.folder == "" || !$folderTree.jstree(true).get_node(_main._profile.folder))
                    _main._profile.folder = "";
                _main._folderpath = _main._profile.folder;
                $folderTree.jstree("deselect_all");
                if (_main._folderpath)
                    $folderTree.jstree('select_node', _main._folderpath);
            }, 500);
        });
    };
    SWIMain.prototype.ReloadReportsTable = function () {
        _main.LoadReports($folderTree.jstree("get_selected")[0]);
    };
    SWIMain.prototype.LoadReports = function (path) {
        if (!path)
            return;
        SWIUtil.StartSpinning();
        _gateway.GetFolderDetail(path, function (data) {
            _main._searchMode = false;
            _main._folder = data.folder;
            _main._folder.isEmpty = (data.files.length == 0 && $folderTree.jstree("get_selected", true)[0].children.length == 0);
            _main.buildReportsTable(data);
            _main._profile.folder = path;
            SWIUtil.StopSpinning();
        });
    };
    SWIMain.prototype.refreshMenus = function () {
        setTimeout(function () {
            SWIUtil.RefreshMenu(_main);
            if (_editor)
                _editor.assistantMenu();
        }, 1000);
    };
    SWIMain.prototype.executeReport = function (path, viewGUID, outputGUID) {
        _gateway.ExecuteReport(path, viewGUID, outputGUID);
        _main.refreshMenus();
    };
    SWIMain.prototype.executeReportFromMenu = function (path, viewGUID, outputGUID, name) {
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
            $waitDialog.modal('hide');
            setTimeout(function () {
                _main.refreshMenus();
                _main._lastReport.name = $("#nav_button").text();
            }, 1000);
        });
    };
    SWIMain.prototype.clearFilesTable = function () {
        var $tableHead = $("#file-table-head");
        var $tableBody = $("#file-table-body");
        if (!$("#file-table-head").is(':empty'))
            $('#file-table').dataTable().fnDestroy();
        $tableHead.empty();
        $tableBody.empty();
    };
    SWIMain.prototype.buildReportsTable = function (data) {
        var $tableHead = $("#file-table-head");
        var $tableBody = $("#file-table-body");
        _main.clearFilesTable();
        //Header
        var $tr = $("<tr>");
        $tableHead.append($tr);
        if (_main._canEdit)
            $tr.append($("<th style='width:22px;' class='nosort hidden-xs'><input id='selectall-checkbox' type='checkbox'/></th>"));
        $tr.append($("<th>").html(SWIUtil.tr("Report")));
        $tr.append($("<th id='action-tableheader' class='nosort'>").html(SWIUtil.tr("Actions")));
        $tr.append($("<th style='width:170px;min-width:170px;text-align:right' class='hidden-xs'>").html(SWIUtil.tr("Last modification")));
        //Body
        for (var i = 0; i < data.files.length; i++) {
            var file = data.files[i];
            if (file.right == 0)
                continue;
            $tr = $("<tr>");
            $tableBody.append($tr);
            if (_main._canEdit)
                $tr.append($("<td class='hidden-xs' style='padding:8px'>").append($("<input>").addClass("report-checkbox").prop("type", "checkbox").data("path", file.path)));
            $tr.append($("<td>").append($("<a>").addClass("report-name").data("path", file.path).data("isReport", file.isreport).text(file.name)));
            var $td = $("<td>").css("text-align", "center").data("path", file.path).data("name", file.name).data("isReport", file.isreport);
            $tr.append($td);
            if (file.isreport) {
                if (_main._reportIcon !== null) {
                    var iconButton = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2(_main._newWindow ? "Execute report in the current window" : "Execute report in a new window")).addClass("btn btn-default btn-table report-execute");
                    iconButton.append($("<span class='glyphicon glyphicon-" + _main._reportIcon + "'></span>"));
                    $td.append(iconButton);
                }
                var button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Views and outputs")).addClass("btn btn-default btn-table report-output");
                button.append($("<span class='glyphicon glyphicon-th-list'></span>"));
                $td.append(button);
                if (file.right >= folderRightSchedule && hasEditor) {
                    button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Edit report")).addClass("btn btn-default btn-table report-edit hidden-xs");
                    button.append($("<span class='glyphicon glyphicon-pencil'></span>"));
                    $td.append(button);
                }
                if (_main._profile.downloadupload > 0) {
                    button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Download report")).addClass("btn btn-default btn-table report-download hidden-xs");
                    button.append($("<span class='glyphicon glyphicon-download'></span>"));
                    $td.append(button);
                }
                button = $("<button>").prop("type", "button").prop("title", SWIUtil.tr2("Mark as favorite")).addClass("btn btn-default btn-table report-favorite hidden-xs");
                if (file.isfavorite)
                    button.append($("<span class='glyphicon glyphicon-star'></span>"));
                else
                    button.append($("<span class='glyphicon glyphicon-star-empty'></span>"));
                $td.append(button);
            }
            $tr.append($("<td>").css("text-align", "right").addClass("hidden-xs").text(file.last));
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
            var path = $target.data("path");
            var name = $target.data("name");
            if ($target.data("isReport")) {
                if (_main._newWindow) {
                    _main.executeReport(path, null, null);
                }
                else {
                    $waitDialog.modal();
                    _main.executeReportFromMenu(path, null, null, name);
                }
            }
            else
                _gateway.ViewFile(path);
        });
        $(".report-execute").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var path = $(e.currentTarget).parent().data("path");
            var name = $(e.currentTarget).parent().data("name");
            if (!_main._newWindow) {
                _main.executeReport(path, null, null);
            }
            else {
                _main.executeReportFromMenu(path, null, null, name);
            }
        });
        $(".report-download").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var path = $(e.currentTarget).parent().data("path");
            _gateway.ViewFile(path);
        });
        $(".report-favorite").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var path = $(e.currentTarget).parent().data("path");
            _gateway.MarkFavorite(path, function (data) {
                SWIUtil.ShowMessage("alert-success", data.Message, 5000);
                var icon = $(e.currentTarget).children("span");
                icon.toggleClass("glyphicon-star");
                icon.toggleClass("glyphicon-star-empty");
                SWIUtil.RefreshMenu(_main);
            });
        });
        $(".report-output").unbind("click").on("click", function (e) {
            $outputPanel.hide();
            var $target = $(e.currentTarget);
            var $tableBody = $("#output-table-body");
            var top = $target.offset().top - 30;
            $tableBody.empty();
            $tableBody.append($("<tr>").append($("<td colspan=2>").append($("<i>").addClass("fa fa-spinner fa-spin fa-1x fa-fw")).append($("<span>").html(SWIUtil.tr("Please wait") + "..."))));
            $outputPanel.css({
                'display': 'inline',
                'position': 'absolute',
                'z-index': '10000',
                'left': $target.offset().left - $outputPanel.width(),
                'top': top,
                'opacity': 0.6
            }).show();
            $("#output-panel-close").unbind("click").on("click", function () {
                $outputPanel.hide();
            });
            _gateway.GetReportDetail($target.parent().data("path"), function (data) {
                $outputPanel.css("opacity", "1");
                $tableBody.empty();
                for (var i = 0; i < data.views.length; i++) {
                    var $tr = $("<tr>");
                    $tableBody.append($tr);
                    $tr.append($("<td>").append($("<a>").data("viewguid", data.views[i].guid).addClass("output-name").text(data.views[i].displayname)));
                    if (_main._reportIcon !== null) {
                        var button = $("<button>").data("viewguid", data.views[i].guid).data("name", data.views[i].displayname).prop("type", "button").addClass("btn btn-default btn-table output-execute");
                        button.append($("<span class='glyphicon glyphicon-" + _main._reportIcon + "'></span>"));
                        $tr.append($("<td>").append(button));
                    }
                    else
                        $tr.append($("<td>"));
                    $tr.append($("<td>").html(SWIUtil.tr("View")));
                }
                for (var i = 0; i < data.outputs.length; i++) {
                    var $tr = $("<tr>");
                    $tableBody.append($tr);
                    $tr.append($("<td>").append($("<a>").data("outputguid", data.outputs[i].guid).addClass("output-name").text(data.outputs[i].displayname)));
                    if (_main._reportIcon !== null) {
                        var button = $("<button>").data("outputguid", data.outputs[i].guid).data("name", data.outputs[i].displayname).prop("type", "button").addClass("btn btn-default btn-table output-execute");
                        button.append($("<span class='glyphicon glyphicon-" + _main._reportIcon + "'></span>"));
                        $tr.append($("<td>").append(button));
                    }
                    else
                        $tr.append($("<td>"));
                    $tr.append($("<td>").html(SWIUtil.tr("Output")));
                }
                //adjust position with the final size
                top = $target.offset().top + 40 - $outputPanel.height();
                $outputPanel.css({ top: top, left: $target.offset().left - $outputPanel.width(), position: 'absolute' });
                $(".output-execute").unbind("click").on("click", function (e) {
                    $outputPanel.hide();
                    if (!_main._newWindow) {
                        _main.executeReport($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"));
                    }
                    else {
                        $waitDialog.modal();
                        _main.executeReportFromMenu($target.parent().data("path"), $(e.currentTarget).data("viewguid"), $(e.currentTarget).data("outputguid"), $target.parent().data("name"));
                    }
                });
                $(".output-name").unbind("click").on("click", function (e) {
                    $outputPanel.hide();
                    if (!_main._newWindow) {
                        $waitDialog.modal();
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
        $('#file-table').dataTable({
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
    };
    SWIMain.prototype.showTreeView = function (show) {
        if (!show) {
            $("#folder-view").hide();
            $("#file-view").removeClass("col-sm-8").addClass("col-sm-12");
        }
        else {
            $("#folder-view").show();
            $("#file-view").removeClass("col-sm-12").addClass("col-sm-8");
        }
    };
    SWIMain.prototype.toggleFoldersReport = function (viewreport, foldertoselect) {
        if (foldertoselect === void 0) { foldertoselect = null; }
        _main._currentView = (viewreport || !_main._profile.showfolders ? "report" : "folders");
        if (foldertoselect) { //Select a folder in the treeview
            _main._folderpath = foldertoselect;
            $folderTree.jstree("deselect_all");
            $folderTree.jstree('select_node', _main._folderpath);
        }
        if (!viewreport)
            redrawDataTables();
        _main.enableControls();
        //transition
        var reportShown = _main._reportPath && _main._currentView == "report";
        $(!reportShown ? ".reportview" : ".folderview").css("opacity", "0.2");
        $(reportShown ? ".reportview" : ".folderview").css("opacity", "1");
    };
    return SWIMain;
}());
//# sourceMappingURL=swi-main.js.map