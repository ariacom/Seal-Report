﻿declare var tra: string[]; 
declare var availableDateKeywords;
declare var shortDateFormat;
declare var shortDateTimeFormat;
declare var languageName;

module SWIUtil {
    export function tr(reference: string): string {
        var result = tra[reference];
        if (!result || result == "") result = reference;
        return result;
    }

    export function tr2(reference: string): string {
        return $('<div/>').html(tr(reference)).text();
    }

    export function Newguid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        })
    }

    export function GetReportName(path: string): string {
        return path.split(dirSeparator).pop().replace(".srex", "");
    }

    export function GetDirectoryName(path: string): string {
        return path.substring(0, path.lastIndexOf(dirSeparator));
    }

    export function ShowMessage(alertClass: string, message: string, timeout: number) {
        setTimeout(function () {
            $waitDialog.modal('hide');
            SWIUtil.HideMessages();
            var $alert = $("<div class='alert sr-alert " + alertClass + "'><a href='#' class='close' data-dismiss='alert' aria-label='close'>&times;</a><p>" + message + "</p></div>");
            $("body").append($alert);
            if (timeout == 0) timeout = 15000;
            if (timeout > 0) setTimeout(function () { $alert.alert('close'); }, timeout);
        }, 200);
    }

    export function HideMessages() {
        $('.sr-alert').alert('close');
    }

    export function EnableButton(button: JQuery, enabled: boolean) {
        if (button.length > 0 && (button[0] as any).type === "button") {
            if (!enabled) button.removeClass("active").addClass("disabled");
            else button.addClass("active").removeClass("disabled");
        }
        else {
            if (!enabled) button.prop('disabled', 'true').addClass("disabled").removeClass("active");
            else button.removeAttr('disabled').removeProp('disabled').removeClass("disabled").addClass("active");
        }
    }

    export function EnableLinkInput(link: JQuery, enabled: boolean) {
        if (!enabled) link.attr('disabled', 'true');
        else link.removeAttr('disabled');
    }

    export function IsEnabled(control: JQuery): boolean {
        return !(control.attr('disabled') || control.prop('disabled'))
    }

    export function ActivatePanel(button: JQuery, panel: JQuery, active: boolean) {
        if (!active) {
            button.removeClass("active");
            panel.hide();
        }
        else {
            button.addClass("active");
            panel.show();
        }
    }

    export function ShowHideControl(control: JQuery, show: boolean) {
        if (!show) control.hide();
        else control.show();
    }

    export function GetOption(val: string, text: string, valSelected: string, icon?: string) {
        var $result = $("<option>").attr("value", val).html(text ? text : val);
        if (icon) $result.attr("data-icon", icon);
        if (val == valSelected) $result.attr("selected", "true");
        return $result;
    }

    export function GetAnchorWithIcon(text: string, id: string, type: string, icon: string): JQuery {
        var $a = $("<a/>").text(text);
        $a.html("<i class='" + icon + "'></i> " + $a.html());
        if (id) $a.prop("id", id);
        if (type) $a.prop("type", type);
        return $a;
    }

    export function UniqueName(name: string, array: any) {
        var result = name;
        var index = 1;
        while (true) {
            var found = false;
            $.each(array, function (key, value) {
                if (value.name == result || value.Name == result) found = true;
            });

            if (found) result = name + index.toString();
            else break;
            index++;
        }
        return result;
    }

    export function GetAggregateName(aggr: number) {
        if (aggr == 1) return SWIUtil.tr2("Minimum of");
        else if (aggr == 2) return SWIUtil.tr2("Maximum of");
        else if (aggr == 3) return SWIUtil.tr2("Average of");
        else if (aggr == 4) return SWIUtil.tr2("Count of");
        else if (aggr == 5) return SWIUtil.tr2("Count distinct of");
        return "";
    }

    export function FindBootstrapEnvironment() {
        var envs = ['xs', 'sm', 'md', 'lg'];

        var $el = $('<div>');
        $el.appendTo($('body'));

        for (var i = envs.length - 1; i >= 0; i--) {
            var env = envs[i];

            $el.addClass('hidden-' + env);
            if ($el.is(':hidden')) {
                $el.remove();
                return env;
            }
        }
    }

    export function IsMobile() {
        return SWIUtil.FindBootstrapEnvironment() == "xs";
    }

    export function InitNumericInput() {
        $(".numeric_input").keyup(function () {
            var v = this.value;
            if (!$.isNumeric(v)) {
                this.value = this.value.slice(0, -1);
            }
        });
    }

    export function StartSpinning() {
        $("#refresh-nav-item").addClass("fa-spin");
        $("#refresh-nav-item").css("display", "inline-block");
    }

    export function StopSpinning() {
        $("#refresh-nav-item").removeClass("fa-spin");
        $("#refresh-nav-item").css("display", "block");
    }

    export function GatewayCallbackHandler(data: any, callback: (data: any) => void, errorcb?: (data: any) => void) {
        if (!data.error) {
            if (callback) callback(data);
        }
        else {
            if (errorcb) errorcb(data);
            else {
                SWIUtil.ShowMessage("alert-danger", data.error, 0);
                if (!data.authenticated) {
                    location.reload();
                }
            }
        }
    }

    export function GatewayFailure(xhr, status, error) {
        SWIUtil.ShowMessage("alert-danger", error + ". " + _errorServer, 0);
    }

    //Static functions
    export function InitSpinning() {
        $(document).ajaxStart(function () {
            SWIUtil.StartSpinning();
        });

        $(document).ajaxStop(function () {
            SWIUtil.StopSpinning();
        });

    }

    export function InitDropDownMenu() {
        //Show dropdown menus on hover
        $('.dropdown-menu-li').hover(
            function () {
                $(this).find('.dropdown-menu').stop(true, true).delay(200).fadeIn(300);
            },
            function () {
                $(this).find('.dropdown-menu').stop(true, true).delay(200).fadeOut(300);
            }
        );
    }

    export function InitVersion() {
        _gateway.GetVersions(
            function (data) {
                var title = SWIUtil.tr2(SWIUtil.tr("Version") + " : " + data.SRVersion + "\n");
                title += data.Info
                if (data.SRAdditionalVersion) title += data.SRAdditionalVersion + "\n";
                $("#brand-id").attr("title", title);
                $("#footer-version").text(data.SWIVersion);
                if (!data.Info.includes("Serial n")) {
                    $("#nav_cr").html(data.Info.replace("\r\n", "<br>"));
                    $("#nav_cr").show();
                }
            }
        );
    }

    export function addReportMenu(main, parent, value) {
        let aref = $("<a href='#'>").addClass('menu-report').attr('path', value.path).attr('viewGUID', value.viewGUID).attr('outputGUID', value.outputGUID).html(value.name);
        if (main && main._reportIcon !== null) aref.append($("<span class='external-navigation glyphicon glyphicon-" + main._reportIcon + "'></span>"));
        aref.addClass(value.classes);
        parent.append($("<li class='menu-reports'>").append(aref));
    }

    export function initMenu(main, parent, items) {
        items.forEach(function (value) {
            if (value.path) {
                SWIUtil.addReportMenu(main, parent, value);
            }
            else {
                const li = $("<li class='menu-reports dropdown dropdown-submenu'>");
                const label = $("<a href='#' class='dropdown-toggle' data-toggle='dropdown'>").html(value.name);
                li.append(label);
                parent.append(li);
                li.addClass(value.classes)
                const ul = $("<ul class='dropdown-menu'>");
                li.append(ul);
                SWIUtil.initMenu(main, ul, value.items);
            }
        });
    }

    export function RefreshMenu(main) {
        //Menu
        _gateway.GetRootMenu(function (menu) {
            if (menu.reports.length != 0 || menu.recentreports.length != 0) $("#menu-main-button").removeClass("disabled");
            else $("#menu-main-button").addClass("disabled");

            //Reports
            var parent = $("#menu-main");
            parent.children(".menu-reports").remove();
            SWIUtil.ShowHideControl($(".divider-menu-reports"), menu.reports.length > 0);
            SWIUtil.initMenu(main, parent, menu.reports);
            if (main) {
                if (menu.reports.length > 0) parent.append($("<li class='menu-divider-folders-report divider menu-reports')>"));
                parent.append($("<li class='menu-reports'>").append($("<a id='menu-view-folders' href='#'>").html(SWIUtil.tr("View Folders"))));
                parent.append($("<li class='menu-reports'>").append($("<a id='menu-view-report' href='#'>").html(SWIUtil.tr("View Report"))));
            }
            //Favorites
            if (menu.favorites.length > 0) {
                if (menu.reports.length > 0 || (main && (main._reportPath != "" || main._currentView == "report") && _main._profile.showfolders)) parent.append($("<li class='menu-divider-recent-report divider menu-reports')>"));
                parent.append($("<li class='menu-divider menu-reports'>").html(SWIUtil.tr("Favorites")));
                menu.favorites.forEach(function (value) {
                    SWIUtil.addReportMenu(main, parent, value);
                });
            }
            //Recent reports
            if (menu.recentreports.length > 0) {
                if (menu.favorites.length > 0 || menu.reports.length > 0 || (main && (main._reportPath != "" || main._currentView == "report") && _main._profile.showfolders)) parent.append($("<li class='menu-divider-recent-report divider menu-reports')>"));
                parent.append($("<li class='menu-divider menu-reports'>").html(SWIUtil.tr("Recents")));
                menu.recentreports.forEach(function (value) {
                    SWIUtil.addReportMenu(main, parent, value);
                });
            }

            $('ul.dropdown-menu [data-toggle=dropdown]').unbind("click").on('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                $(this).parent().siblings().removeClass('open');
                $(this).parent().toggleClass('open');
            });

            //Toggle report/folders
            $("#menu-view-folders, #menu-view-report, #nav_button").unbind("click").on("click", function () {
                main.toggleFoldersReport(_main._currentView != "report");
            });
            $("#nav_button").unbind("mouseover").on("mouseover", function () {
                if ($("#menu-main").parent().hasClass("open")) $("#menu-main").dropdown('toggle');
            });

            //Execute reports from menu
            $("a.menu-report").unbind("click").on("click", function () {
                if (main && !_main._newWindow) {
                    $waitDialog.modal();
                    main.executeReportFromMenu($(this).attr("path"), $(this).attr("viewGUID"), $(this).attr("outputGUID"), $(this).text());
                }
                else {
                    SWIUtil.executeReport(main, $(this).attr("path"), $(this).attr("viewGUID"), $(this).attr("outputGUID"));
                }
            });
            $("a.menu-report span").unbind("click").on("click", function () {
                const parent = $(this).parent();
                if (!main || !main._newWindow) {
                    SWIUtil.executeReport(main, parent.attr("path"), parent.attr("viewGUID"), parent.attr("outputGUID"));
                }
                else {
                    main.executeReportFromMenu(parent.attr("path"), parent.attr("viewGUID"), parent.attr("outputGUID"), parent.text());
                    $("#menu-main").dropdown('toggle');
                }
                return false;
            });

            if (main) main.enableControls();
        });
    }

    export function executeReport(main, path: string, viewGUID: string, outputGUID: string) {
        _gateway.ExecuteReport(path, viewGUID, outputGUID);
        setTimeout(function () {
            if (main) main.refreshMenus();
        }, 1000);
    }

    export function InitProfile(profile) {
        SWIUtil.ShowHideControl($("#profile-nav-item"), true);
        $("#profile-nav-item").unbind("click").on("click", function () {
            $outputPanel.hide();
            $("#profile-user").val(profile.name);
            $("#profile-groups").val(profile.group.replaceAll(";", "\r"));
            SWIUtil.ShowHideControl($("#profile-change-password"), profile.editprofile && profile.changepassword);

            $waitDialog.modal('hide');

            if (profile.editprofile && profile.changepassword) {
                $("#profile-change-password").unbind("click").on("click", function (e) {
                    $("#profile-dialog").modal('hide');
                    $("#change-password-submit").unbind("click").on("click", function () {
                        _gateway.ChangePassword(
                            $("#password-change").val(), $("#password-change1").val(), $("#password-change2").val(),
                            function (data) {
                                if (data.error) SWIUtil.ShowMessage("alert-danger", data.error, -1);
                                else {
                                    $("#change-password-modal").modal('hide');
                                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("Your password has been changed."), 5000);
                                }
                            }
                        );
                    });

                    $("#change-password-modal").modal();
                });
            }

            $("#profile-save").unbind("click").on("click", function (e) {
                $("#profile-dialog").modal('hide');
                if (profile.editprofile) {
                    var onstartup = $("#onstartup-select").val();
                    var startupreport = profile.startupreport;
                    if (onstartup == "4") {
                        onstartup = "3"; //Execute report
                        startupreport = _main._lastReport.path;

                    }
                    var startupreportname = $("#onstartup-reportname").val();
                    var executionmode = $("#executionmode-select").val();

                    //connections
                    var connections = [];
                    profile.sources.forEach(function (source) {
                        connections.push(source.GUID + "\r" + $("#" + source.GUID).val())
                    });
                    _gateway.SetUserProfile($("#culture-select").val(), onstartup, startupreport, startupreportname, executionmode, connections, function () {
                        location.reload();
                    });
                }
            });

            SWIUtil.ShowHideControl($(".edit-profile"), profile.editprofile);
            SWIUtil.ShowHideControl($("#profile-close"), !profile.editprofile);

            var $select = $("#onstartup-select");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("Default startup"), profile.onstartup));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Do not execute report"), profile.onstartup));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute the last report"), profile.onstartup));
            if (profile.startupreportname) $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Execute the report") + " '" + profile.startupreportname + "'", profile.onstartup));
            if (_main._lastReport.name && _main._lastReport.name != profile.startupreportname) $select.append(SWIUtil.GetOption("4", SWIUtil.tr("Execute the report") + " '" + _main._lastReport.name + "'", profile.onstartup));
            $select.selectpicker('refresh');

            $("#onstartup-reportname").val(profile.startupreportname);

            $select = $("#executionmode-select");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("Default mode"), profile.executionmode));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Execute report in a new window"), profile.executionmode));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute report in the current window"), profile.executionmode));
            $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Allow only execution in a new window"), profile.executionmode));
            $select.selectpicker('refresh');

            $select = $("#culture-select");
            if ($select.children("option").length == 0) {
                _gateway.GetCultures(function (data) {
                    $select.append(SWIUtil.GetOption("", SWIUtil.tr("Default culture"), profile.culture));
                    for (var i = 0; i < data.length; i++) {
                        $select.append(SWIUtil.GetOption(data[i].id, data[i].val, profile.culture));
                    }
                    $select.selectpicker('refresh');
                    $("#profile-dialog").modal();
                    if (SWIUtil.IsMobile()) $('.navbar-toggle').click();
                });
            }
            else {
                $select.val(profile.culture).change();
                $select.selectpicker('refresh');
                $("#profile-dialog").modal();
                if (SWIUtil.IsMobile()) $('.navbar-toggle').click();
            }

            const $connections = $("#default-connections");
            $("#default-connections").empty();
            if (profile.sources.length === 0 || !profile.editprofile) {
                $("#default-connections").parent().hide();
            }
            else {
                $("#default-connections").parent().show();
                profile.sources.forEach(function (source) {
                    const $connectionDiv = $("<div class='row'>");
                    $connectionDiv.append($("<div class='col-sm-4' style='margin-top:8px'>").append($("<span>").html(source.name)));
                    const $connectionSelect = $("<select id='" + source.GUID + "' data-width='100%'></select>");
                    source.connections.forEach(function (connection) {
                        $connectionSelect.append(SWIUtil.GetOption(connection.GUID, connection.name, source.connectionGUID));
                    });
                    $connectionDiv.append($("<div class='col-sm-8' style='padding-bottom:5px;'>").append($connectionSelect));
                    $connections.append($connectionDiv);
                    $connectionSelect.selectpicker('refresh');
                });
            }
        });
    }

    export function InitStandardInput(id: string, val: string, modif: any, handler: (val: string) => any, numeric ?: boolean) {
        var $input = $(id);
        $input.val(val);
        $input.unbind("input keyup change").on("input keyup change", function (e) {
            if (e.type == "change" && modif) modif.setModified(true);
            if (numeric) this.value = this.value.replace(/[^0-9\.]/g, '');
            handler($(this).val());
        });

        return $input;
    }

    export function InitBoolSelect(id: string, val: boolean, textTrue: string, textFalse: string, handler: (val: boolean) => any) {
        var $select = $(id);
        var valstr = val ? "1" : "0";
        $select.selectpicker("destroy");
        $select.empty();
        $select.append(SWIUtil.GetOption("1", textTrue, valstr));
        $select.append(SWIUtil.GetOption("0", textFalse, valstr));
        $select.unbind("change").on("change", function (e) {
            handler($(this).val() == "1");
        });
        $select.selectpicker('refresh');
        return $select;
    }
}