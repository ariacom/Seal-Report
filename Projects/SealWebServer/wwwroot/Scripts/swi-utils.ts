declare var tra: Record<string, string>;
declare var availableDateKeywords : any;
declare var shortDateFormat: string;
declare var shortDateTimeFormat: string;
declare var languageName: string;

namespace SWIUtil {
    // Native Bootstrap 5 helpers. Bootstrap 5 dropped the jQuery plugin API
    // ($.fn.modal/tooltip/dropdown/alert/...); these thin wrappers call the native
    // bootstrap.* API on the underlying DOM element so the calling code stays jQuery-based.
    function bs(): any { return (window as any).bootstrap; }

    export function ShowModal($el: JQuery) {
        var el = $el.get(0);
        if (el) bs().Modal.getOrCreateInstance(el).show();
    }

    export function HideModal($el: JQuery) {
        var el = $el.get(0);
        if (el) bs().Modal.getOrCreateInstance(el).hide();
    }

    export function ToggleDropdown($el: JQuery) {
        var el = $el.get(0);
        if (el) bs().Dropdown.getOrCreateInstance(el).toggle();
    }

    export function HideTooltip($el: JQuery) {
        var el = $el.get(0);
        if (!el) return;
        var t = bs().Tooltip.getInstance(el);
        if (t) t.hide();
    }

    export function CloseAlerts($els: JQuery) {
        $els.each(function () { bs().Alert.getOrCreateInstance(this).close(); });
    }

    export function tr(reference: string): string {
        var result : string = tra[reference];
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
        return (path.split(dirSeparator).pop() ?? "").replace(".srex", "");
    }

    export function GetDirectoryName(path: string): string {
        return path.substring(0, path.lastIndexOf(dirSeparator));
    }

    export function ShowMessage(alertClass: string, message: string, timeout: number) {
        setTimeout(function () {
            SWIUtil.HideModal($waitDialog);
            SWIUtil.HideMessages();
            //Contextual Font Awesome 6 icon matching the alert level
            var icon = "fa-circle-info";
            if (alertClass.indexOf("alert-success") >= 0) icon = "fa-circle-check";
            else if (alertClass.indexOf("alert-danger") >= 0) icon = "fa-circle-exclamation";
            else if (alertClass.indexOf("alert-warning") >= 0) icon = "fa-triangle-exclamation";
            var $alert = $("<div class='alert sr-alert " + alertClass + "' role='alert'>"
                + "<span class='sr-alert-icon fa-solid " + icon + "'></span>"
                + "<p class='sr-alert-text'>" + message + "</p>"
                + "<button type='button' class='btn-close sr-alert-close' aria-label='close'></button>"
                + "</div>");
            $alert.find(".sr-alert-close").on("click", function () { SWIUtil.CloseAlerts($alert); });
            $("body").append($alert);
            if (timeout == 0) timeout = 15000;
            if (timeout > 0) setTimeout(function () { SWIUtil.CloseAlerts($alert); }, timeout);
        }, 200);
    }

    export function HideMessages() {
        SWIUtil.CloseAlerts($('.sr-alert'));
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
        var $a = $("<a/>").addClass("dropdown-item").text(text);
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

    // Bootstrap 5 responsive breakpoints (Bootstrap 5 dropped the .hidden-xs/.visible-* probe
    // classes the old detection relied on, so resolve the breakpoint from the viewport width).
    export function FindBootstrapEnvironment() {
        var width = window.innerWidth;
        if (width < 576) return 'xs';
        if (width < 768) return 'sm';
        if (width < 992) return 'md';
        if (width < 1200) return 'lg';
        if (width < 1400) return 'xl';
        return 'xxl';
    }

    export function IsMobile() {
        return window.innerWidth < 768;
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

    export function GatewayFailure(xhr: any, status: any, error: any) {
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

    export function addReportMenu(main: any, parent: any, value: any) {
        let aref = $("<a href='#'>").addClass('menu-report').attr('path', value.path).attr('viewGUID', value.viewGUID).attr('outputGUID', value.outputGUID).html(value.name);
        if (main && main._reportIcon !== null) aref.append($("<span class='external-navigation " + main._reportIcon + "'></span>"));
        aref.addClass(value.classes);
        parent.append($("<li class='menu-reports'>").append(aref));
    }

    // Styled Favorites/Recents row (mirrors the AI chat dropdown look): a clickable
    // report link plus hover-revealed action buttons (favorite toggle, remove from recents).
    export function addFavRecentMenu(main: any, parent: any, value: any, isFavorite: boolean) {
        const li = $("<li class='menu-reports menu-fav-item'>");

        const aref = $("<a href='#'>").addClass('menu-report menu-fav-link').attr('path', value.path).attr('viewGUID', value.viewGUID).attr('outputGUID', value.outputGUID).html(value.name);
        if (main && main._reportIcon !== null) aref.append($("<span class='external-navigation " + main._reportIcon + "'></span>"));
        aref.addClass(value.classes);
        li.append(aref);

        const actions = $("<span class='menu-fav-actions'>");
        const starBtn = $("<button type='button' class='menu-fav-btn menu-fav-star'>")
            .attr('path', value.path)
            .attr('title', isFavorite ? SWIUtil.tr("Remove from favorites") : SWIUtil.tr("Mark as favorite"))
            .html(isFavorite ? "<i class='fas fa-star'></i>" : "<i class='far fa-star'></i>");
        actions.append(starBtn);
        if (isFavorite) {
            const renameBtn = $("<button type='button' class='menu-fav-btn menu-fav-rename'>")
                .attr('path', value.path)
                .attr('title', SWIUtil.tr("Rename"))
                .html("<i class='fa fa-pencil'></i>");
            actions.append(renameBtn);
        }
        if (!isFavorite) {
            const removeBtn = $("<button type='button' class='menu-fav-btn menu-fav-remove'>")
                .attr('path', value.path)
                .attr('title', SWIUtil.tr("Remove from recents"))
                .html("<i class='fa fa-trash'></i>");
            actions.append(removeBtn);
        }
        li.append(actions);

        parent.append(li);
    }

    export function initMenu(main: any, parent: any, items: any) {
        items.forEach(function (value : any) {
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

    export function RefreshMenu(main : any) {
        //Menu
        _gateway.GetRootMenu(function (menu) {
            if (menu.favorites.length != 0 || menu.recentreports.length != 0) $("#menu-main-button").removeClass("disabled");
            else $("#menu-main-button").addClass("disabled");

            var parent = $("#menu-main");
            parent.children(".menu-reports").remove();
            //Favorites
            if (menu.favorites.length > 0) {
                parent.append($("<li class='menu-fav-label menu-reports'>").html('<i class="fa fa-star"></i><span>' + SWIUtil.tr("Favorites") + '</span>'));
                menu.favorites.forEach(function (value : any) {
                    SWIUtil.addFavRecentMenu(main, parent, value, true);
                });
            }
            //Recent reports
            if (menu.recentreports.length > 0) {
                parent.append($("<li class='menu-fav-label menu-reports'>").html('<i class="far fa-clock"></i><span>' + SWIUtil.tr("Recents") + '</span>'));
                menu.recentreports.forEach(function (value: any) {
                    SWIUtil.addFavRecentMenu(main, parent, value, false);
                });
                if (main && main._reportPath != "") parent.append($("<li class='menu-divider-recent-report divider menu-reports'>"));
            }
            if (main) {
                parent.append($("<li class='menu-reports'>").append($("<a id='menu-view-folders' href='#'>").html(SWIUtil.tr("View Folders"))));
                parent.append($("<li class='menu-reports'>").append($("<a id='menu-view-report' href='#'>").html(SWIUtil.tr("View Report"))));
            }

            //Toggle report/folders
            $("#menu-view-folders, #menu-view-report, #nav_button").unbind("click").on("click", function () {
                main.toggleFoldersReport(_main._currentView != "report");
            });
            $("#nav_button").unbind("mouseover").on("mouseover", function () {
                if ($("#menu-main").parent().hasClass("open")) SWIUtil.ToggleDropdown($("#menu-main"));
            });

            //Execute reports from menu
            $("a.menu-report").unbind("click").on("click", (event) => {
                const $a = $(event.currentTarget);
                if (main && !_main._newWindow) {
                    SWIUtil.ShowModal($waitDialog);
                    main.executeReportFromMenu($a.attr("path"), $a.attr("viewGUID"), $a.attr("outputGUID"), $a.text());
                }
                else {
                    SWIUtil.executeReport(main, $a.attr("path") as string, $a.attr("viewGUID") as string, $a.attr("outputGUID") as string);
                }
            });
            $("a.menu-report span").unbind("click").on("click", function () {
                const parent = $(this).parent();
                if (!main || !main._newWindow) {
                    SWIUtil.executeReport(main, parent.attr("path") as string, parent.attr("viewGUID") as string, parent.attr("outputGUID") as string);
                }
                else {
                    main.executeReportFromMenu(parent.attr("path"), parent.attr("viewGUID"), parent.attr("outputGUID"), parent.text());
                    SWIUtil.ToggleDropdown($("#menu-main"));
                }
                return false;
            });

            //Favorite toggle from the menu (stopPropagation keeps the dropdown open)
            $("#menu-main .menu-fav-star").unbind("click").on("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                _gateway.MarkFavorite($(this).attr("path") as string, function () {
                    SWIUtil.RefreshMenu(main);
                });
                return false;
            });

            //Remove a report from the recents list
            $("#menu-main .menu-fav-remove").unbind("click").on("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                _gateway.RemoveRecentReport($(this).attr("path") as string, function () {
                    SWIUtil.RefreshMenu(main);
                });
                return false;
            });

            //Rename a favorite: inline edit of the personal label (the report file is not changed)
            $("#menu-main .menu-fav-rename").unbind("click").on("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                const path = $(this).attr("path") as string;
                const li = $(this).closest("li.menu-fav-item");
                const link = li.find("a.menu-fav-link");
                if (li.find("input.menu-fav-rename-input").length > 0) return false;

                const currentName = link.contents().filter(function () { return this.nodeType === 3; }).text().trim() || link.text().trim();
                const input = $("<input type='text' class='menu-fav-rename-input'>").attr("maxlength", 60).val(currentName);
                link.hide();
                li.prepend(input);
                (input[0] as HTMLInputElement).focus();
                (input[0] as HTMLInputElement).select();

                let done = false;
                function finish(commit: boolean) {
                    if (done) return;
                    done = true;
                    const newName = (input.val() as string).trim();
                    input.remove();
                    link.show();
                    if (commit && newName && newName !== currentName) {
                        _gateway.RenameFavoriteReport(path, newName, function () {
                            SWIUtil.RefreshMenu(main);
                        });
                    }
                }
                input.on("click", function (e) { e.stopPropagation(); });
                input.on("blur", function () { finish(true); });
                input.on("keydown", function (ev) {
                    if (ev.key === "Enter") { ev.preventDefault(); finish(true); }
                    else if (ev.key === "Escape") { ev.preventDefault(); finish(false); }
                    ev.stopPropagation();
                });
                return false;
            });

            if (main) main.enableControls();
        });
    }

    export function executeReport(main : any, path: string, viewGUID: string, outputGUID: string) {
        _gateway.ExecuteReport(path, viewGUID, outputGUID);
        setTimeout(function () {
            if (main) main.refreshMenus();
        }, 1000);
    }

    export function InitProfile(profile : any) {
        SWIUtil.ShowHideControl($("#profile-nav-item"), true);
        $("#profile-nav-item").unbind("click").on("click", function () {
            $outputPanel.hide();
            $("#profile-user").val(profile.name);
            $("#profile-groups").val(profile.group.replaceAll(";", "\r"));
            SWIUtil.ShowHideControl($("#profile-change-password-option"), profile.editprofile && profile.changepassword);

            SWIUtil.HideModal($waitDialog);

            if (profile.editprofile && profile.changepassword) {
                $("#profile-change-password").unbind("click").on("click", function (e) {
                    SWIUtil.HideModal($("#profile-dialog"));
                    $("#change-password-submit").unbind("click").on("click", function () {
                        _gateway.ChangePassword(
                            $("#password-change").val() as string, $("#password-change1").val() as string, $("#password-change2").val() as string,
                            function (data) {
                                if (data.error) SWIUtil.ShowMessage("alert-danger", data.error, -1);
                                else {
                                    SWIUtil.HideModal($("#change-password-modal"));
                                    SWIUtil.ShowMessage("alert-success", SWIUtil.tr("Your password has been changed."), 5000);
                                }
                            }
                        );
                    });

                    SWIUtil.ShowModal($("#change-password-modal"));
                });
            }

            $("#profile-save").unbind("click").on("click", function (e) {
                SWIUtil.HideModal($("#profile-dialog"));
                if (profile.editprofile) {
                    var onstartup = $("#onstartup-select").val() as string;
                    var startupreport = profile.startupreport;
                    if (onstartup == "4") {
                        onstartup = "3"; //Execute report
                        startupreport = _main._lastReport.path;

                    }
                    var startupreportname = $("#onstartup-reportname").val() as string;
                    var executionmode = $("#executionmode-select").val() as string;

                    //connections
                    var connections : any[] = [];
                    profile.sources.forEach(function (source : any) {
                        connections.push(source.GUID + "\r" + $("#" + source.GUID).val())
                    });
                    _gateway.SetUserProfile($("#culture-select").val() as string, onstartup, startupreport, startupreportname, executionmode, connections, function () {
                        location.reload();
                    });
                }
            });

            SWIUtil.ShowHideControl($(".edit-profile"), profile.editprofile);
            SWIUtil.ShowHideControl($("#profile-close"), !profile.editprofile);

            var $select = $("#onstartup-select");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("Default startup"), profile.onstartup));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Do not execute report"), profile.onstartup));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute the last report"), profile.onstartup));
            if (profile.startupreportname) $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Execute the report") + " '" + profile.report + "'", profile.onstartup));
            if (_main._lastReport.name && _main._lastReport.name != profile.startupreportname) $select.append(SWIUtil.GetOption("4", SWIUtil.tr("Execute the report") + " '" + _main._lastReport.path + "'", profile.onstartup));
            $select.selectpicker();

            $("#onstartup-reportname").val(profile.startupreportname);

            $select = $("#executionmode-select");
            $select.selectpicker("destroy");
            $select.empty();
            $select.append(SWIUtil.GetOption("0", SWIUtil.tr("Default mode"), profile.executionmode));
            $select.append(SWIUtil.GetOption("1", SWIUtil.tr("Execute report in a new window"), profile.executionmode));
            $select.append(SWIUtil.GetOption("2", SWIUtil.tr("Execute report in the current window"), profile.executionmode));
            $select.append(SWIUtil.GetOption("3", SWIUtil.tr("Allow only execution in a new window"), profile.executionmode));
            $select.selectpicker();

            $select = $("#culture-select");
            var showCulturePicker = function () {
                // Rebuild the picker from scratch: select the value on the underlying
                // <select> first, then initialise bootstrap-select exactly once. A
                // selectpicker('refresh') after (re)init runs buildData() a second time,
                // which appends to the picker's internal model and doubles the rendered
                // text/options (1.14-beta3 behaviour) - see swi-ai.ts agent dropdown.
                $select.selectpicker("destroy");
                $select.val(profile.culture);
                $select.selectpicker();
                SWIUtil.ShowModal($("#profile-dialog"));
            };
            if ($select.children("option").length == 0) {
                _gateway.GetCultures(function (data) {
                    $select.append(SWIUtil.GetOption("", SWIUtil.tr("Default culture"), profile.culture));
                    for (var i = 0; i < data.length; i++) {
                        $select.append(SWIUtil.GetOption(data[i].id, data[i].val, profile.culture));
                    }
                    showCulturePicker();
                });
            }
            else {
                showCulturePicker();
            }

            const $connections = $("#default-connections");
            $("#default-connections").empty();
            if (profile.sources.length === 0 || !profile.editprofile) {
                $("#default-connections").parent().hide();
                $("#default-connections-title").parent().hide();
            }
            else {
                $("#default-connections").parent().show();
                $("#default-connections-title").parent().show();
                profile.sources.forEach(function (source : any) {
                    const $connectionDiv = $("<div class='row'>");
                    $connectionDiv.append($("<div class='col-sm-4' style='margin-top:8px'>").append($("<span>").html(source.name)));
                    const $connectionSelect = $("<select id='" + source.GUID + "' data-container='body' data-width='100%'></select>");
                    source.connections.forEach(function (connection : any) {
                        $connectionSelect.append(SWIUtil.GetOption(connection.GUID, connection.name, source.connectionGUID));
                    });
                    $connectionDiv.append($("<div class='col-sm-8' style='padding-bottom:5px;'>").append($connectionSelect));
                    $connections.append($connectionDiv);
                    $connectionSelect.selectpicker();
                });
            }
        });
    }

    export function InitStandardInput(id: string, val: string, modif: any, handler: (val: string) => any, numeric ?: boolean) {
        var $input = $(id);
        $input.val(val);
        $input.unbind("input keyup change").on("input keyup change", (event) => {
            if (event.type == "change" && modif) modif.setModified(true);
            const target = event.target as HTMLInputElement;
            if (numeric) target.value = target.value.replace(/[^0-9\.\,\r\n\-]/g, '');
            handler(target.value);
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
        $select.unbind("change").on("change", (event) => {
            handler($(event.target).val() == "1");
        });
        // Initialise once after destroy: a 'refresh' here would run buildData() twice
        // and double the rendered text/options (bootstrap-select 1.14-beta3).
        $select.selectpicker();
        return $select;
    }
}