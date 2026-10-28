"use strict";
var SWISchedule;
(function (SWISchedule) {
    var _edit = null;
    var _onSaved = null;
    //Repeat choices in minutes (0 = no repetition / indefinitely)
    const RepeatIntervals = [0, 5, 10, 15, 30, 60, 120, 240, 480, 720];
    const RepeatDurations = [0, 60, 120, 240, 480, 720, 1440];
    /** Refreshes the reports of the current folder: the schedule icon of the report may have changed */
    function refreshReports() {
        if (_main._currentView == "folders" && _main._folder)
            _main.ReloadReportsTable();
    }
    function minutesText(minutes, zeroText) {
        if (minutes == 0)
            return SWIUtil.tr(zeroText);
        if (minutes % 1440 == 0)
            return (minutes / 1440) + " " + SWIUtil.tr(minutes == 1440 ? "day" : "days");
        if (minutes % 60 == 0)
            return (minutes / 60) + " " + SWIUtil.tr(minutes == 60 ? "hour" : "hours");
        return minutes + " " + SWIUtil.tr("minutes");
    }
    function fillSelect($select, items, value) {
        $select.empty();
        items.forEach(function (item) {
            $select.append($("<option>").val(item.id).text(item.val));
        });
        if (value !== undefined && value !== null)
            $select.val(String(value));
    }
    function fillChecks($container, prefix, items, values) {
        $container.empty();
        items.forEach(function (item) {
            var id = prefix + item.id;
            var $div = $("<div class='form-check form-check-inline'>");
            $div.append($("<input type='checkbox' class='form-check-input'>").attr("id", id).val(item.id).prop("checked", values && values.indexOf(parseInt(item.id)) >= 0));
            $div.append($("<label class='form-check-label small'>").attr("for", id).text(item.val));
            $container.append($div);
        });
    }
    function getChecks($container) {
        var result = [];
        $container.find("input:checked").each(function () { result.push(parseInt($(this).val())); });
        return result;
    }
    function currentDevice() {
        var guid = $("#webschedule-device").val();
        return _edit.devices.find(function (d) { return d.guid == guid; });
    }
    /** Fills the folder list according to the device: folders of the user for a Folder device, directories for a File Server or SharePoint device */
    function initFolders(value) {
        var device = currentDevice();
        var items = [];
        if (device && device.type == "folder")
            items = _edit.folders;
        else if (device && device.directories)
            items = device.directories.map(function (d) { return { id: d, val: d }; });
        fillSelect($("#webschedule-folder"), items, value);
        if (!$("#webschedule-folder").val() && items.length > 0)
            $("#webschedule-folder").val(items[0].id);
        SWIUtil.ShowHideControl($("#webschedule-folder").parent(), items.length > 0);
    }
    function refreshControls() {
        var device = currentDevice();
        var isEmail = device && device.type == "email";
        SWIUtil.ShowHideControl($(".webschedule-email"), isEmail);
        SWIUtil.ShowHideControl($(".webschedule-file"), device && !isEmail);
        SWIUtil.ShowHideControl($(".webschedule-zip"), $("#webschedule-zip").is(":checked"));
        SWIUtil.ShowHideControl($("#webschedule-removezippassword-div"), _edit.schedule.haszippassword);
        var type = parseInt($("#webschedule-type").val());
        SWIUtil.ShowHideControl($(".webschedule-interval"), type == 2 || type == 3);
        SWIUtil.ShowHideControl($(".webschedule-weekly"), type == 3);
        SWIUtil.ShowHideControl($(".webschedule-monthly"), type == 4);
        $("#webschedule-interval-label").text(SWIUtil.tr(type == 3 ? "Recur every (weeks)" : "Recur every (days)"));
    }
    function showError(data) {
        $("#webschedule-error").removeClass("d-none").text($("<div/>").html(data.error).text());
        //the error is displayed at the top of the dialog body, which may be scrolled down to the Save button
        $("#webschedule-dialog .modal-body").scrollTop(0);
        if (!data.authenticated)
            location.reload();
    }
    /** Opens the schedule dialog: a new schedule of the report 'path' if guid is empty */
    function Edit(path, guid, onSaved) {
        $outputPanel.hide();
        _onSaved = onSaved || null;
        SWIUtil.ShowModal($waitDialog);
        _gateway.GetWebSchedule(path, guid, function (data) {
            SWIUtil.HideModal($waitDialog);
            _edit = data;
            var s = data.schedule;
            $("#webschedule-error").addClass("d-none").text("");
            $("#webschedule-title").text((s.guid ? SWIUtil.tr2("Schedule") : SWIUtil.tr2("New schedule")) + ": " + s.reportname);
            $("#webschedule-name").val(s.name);
            $("#webschedule-enabled").prop("checked", s.enabled);
            fillSelect($("#webschedule-device"), data.devices.map(function (d) { return { id: d.guid, val: d.name }; }), s.deviceguid);
            fillSelect($("#webschedule-format"), data.formats, s.format);
            $("#webschedule-to").val(s.to);
            $("#webschedule-cc").val(s.cc);
            $("#webschedule-bcc").val(s.bcc);
            $("#webschedule-subject").val(s.subject);
            $("#webschedule-body").val(s.body);
            $("#webschedule-html").prop("checked", s.html);
            initFolders(s.folder);
            $("#webschedule-file").val(s.file);
            $("#webschedule-zip").prop("checked", s.zip);
            $("#webschedule-zippassword").val("").attr("placeholder", SWIUtil.tr2(s.haszippassword ? "Password unchanged" : "Zip password"));
            $("#webschedule-removezippassword").prop("checked", false);
            $("#webschedule-cancel").prop("checked", s.cancel);
            $("#webschedule-type").val(String(s.type));
            $("#webschedule-start").val(s.start);
            $("#webschedule-end").val(s.end);
            $("#webschedule-interval").val(s.interval);
            fillChecks($("#webschedule-weekdays"), "webschedule-weekday-", data.weekdays, s.weekdays);
            fillChecks($("#webschedule-months"), "webschedule-month-", data.months, s.months);
            var days = [];
            for (var i = 1; i <= 31; i++)
                days.push({ id: String(i), val: String(i) });
            days.push({ id: "32", val: SWIUtil.tr("Last") });
            fillChecks($("#webschedule-days"), "webschedule-day-", days, s.days);
            fillSelect($("#webschedule-repeatinterval"), RepeatIntervals.map(function (m) { return { id: String(m), val: minutesText(m, "None") }; }), s.repeatinterval || 0);
            fillSelect($("#webschedule-repeatduration"), RepeatDurations.map(function (m) { return { id: String(m), val: minutesText(m, "Indefinitely") }; }), s.repeatduration || 0);
            //custom values set in another tool
            if ($("#webschedule-repeatinterval").val() === null)
                $("#webschedule-repeatinterval").append($("<option>").val(s.repeatinterval).text(minutesText(s.repeatinterval, "None"))).val(String(s.repeatinterval));
            if ($("#webschedule-repeatduration").val() === null)
                $("#webschedule-repeatduration").append($("<option>").val(s.repeatduration).text(minutesText(s.repeatduration, "Indefinitely"))).val(String(s.repeatduration));
            if (s.repeatinterval > 0)
                $("#webschedule-advanced").addClass("show");
            else
                $("#webschedule-advanced").removeClass("show");
            $("#webschedule-device").unbind("change").on("change", function () {
                initFolders("");
                refreshControls();
            });
            $("#webschedule-type,#webschedule-zip").unbind("change").on("change", refreshControls);
            //File names proposed in the editable combo: the name is formatted with the execution date time ({0:format})
            var $files = $("#webschedule-file-dropdown");
            $files.empty();
            [displayNameKeyword, s.reportname].forEach(function (name) {
                ["", "_{0:yyyy_MM_dd}", "_{0:yyyy_MM_dd HH_mm_ss}"].forEach(function (suffix) {
                    $files.append($("<li>").append($("<a class='dropdown-item' href='#'>").text(name + suffix)));
                });
            });
            $files.find("a").unbind("click").on("click", function (e) {
                e.preventDefault();
                $("#webschedule-file").val($(this).text()).trigger("focus");
            });
            SWIUtil.ShowHideControl($("#webschedule-delete"), s.guid);
            $("#webschedule-delete").unbind("click").on("click", function () {
                confirmDelete(s, function () { SWIUtil.HideModal($("#webschedule-dialog")); });
            });
            //Restrictions: the values are edited on the restrictions panel of the report, opened in another window
            var setRestrictionsText = function (text) {
                $("#webschedule-restrictions").text(text ? text : SWIUtil.tr2("Default values of the report"));
                SWIUtil.ShowHideControl($("#webschedule-restrictions-reset"), !!(s.guid && text));
            };
            setRestrictionsText(s.restrictions);
            //no restriction block if the report has no prompted restriction
            SWIUtil.ShowHideControl($("#webschedule-restrictions-block"), s.hasrestrictions);
            $("#webschedule-restrictions-edit").unbind("click").on("click", function () {
                $("#webschedule-error").addClass("d-none");
                //window opened during the click (not blocked as a popup), the report is loaded once the schedule is saved
                var target = "webschedule_restrictions_" + Date.now();
                var win = window.open("about:blank", target);
                _gateway.SaveWebSchedule(s.path, readSchedule(), function (result) {
                    SWIUtil.HideModal($("#webschedule-dialog"));
                    _gateway.EditWebScheduleRestrictions(result.path, result.guid, target);
                    SWIUtil.ShowMessage("alert-info", SWIUtil.tr("Edit the restrictions in the report window, then click on 'Save for the schedule'"), 10000);
                    if (_onSaved)
                        _onSaved();
                    refreshReports();
                }, function (data) {
                    if (win)
                        win.close();
                    showError(data);
                });
            });
            $("#webschedule-restrictions-reset").unbind("click").on("click", function () {
                _gateway.ResetWebScheduleRestrictions(s.guid, function (data) {
                    setRestrictionsText("");
                    SWIUtil.ShowMessage("alert-success", data.Message, 5000);
                }, showError);
            });
            var readSchedule = function () {
                return {
                    guid: s.guid,
                    name: $("#webschedule-name").val(),
                    enabled: $("#webschedule-enabled").is(":checked"),
                    deviceguid: $("#webschedule-device").val(),
                    format: parseInt($("#webschedule-format").val()),
                    folder: $("#webschedule-folder").val(),
                    file: $("#webschedule-file").val(),
                    to: $("#webschedule-to").val(),
                    cc: $("#webschedule-cc").val(),
                    bcc: $("#webschedule-bcc").val(),
                    subject: $("#webschedule-subject").val(),
                    body: $("#webschedule-body").val(),
                    html: $("#webschedule-html").is(":checked"),
                    zip: $("#webschedule-zip").is(":checked"),
                    zippassword: $("#webschedule-zippassword").val(),
                    removezippassword: $("#webschedule-removezippassword").is(":checked"),
                    cancel: $("#webschedule-cancel").is(":checked"),
                    type: parseInt($("#webschedule-type").val()),
                    start: $("#webschedule-start").val(),
                    end: $("#webschedule-end").val(),
                    interval: parseInt($("#webschedule-interval").val()) || 0,
                    weekdays: getChecks($("#webschedule-weekdays")),
                    months: getChecks($("#webschedule-months")),
                    days: getChecks($("#webschedule-days")),
                    repeatinterval: parseInt($("#webschedule-repeatinterval").val()) || 0,
                    repeatduration: parseInt($("#webschedule-repeatduration").val()) || 0
                };
            };
            $("#webschedule-save").unbind("click").on("click", function () {
                $("#webschedule-error").addClass("d-none");
                _gateway.SaveWebSchedule(s.path, readSchedule(), function (result) {
                    SWIUtil.HideModal($("#webschedule-dialog"));
                    var message = SWIUtil.tr("The schedule has been saved");
                    if (result.nextexecution)
                        message += ". " + SWIUtil.tr("Next execution") + ": " + result.nextexecution;
                    SWIUtil.ShowMessage("alert-success", message, 5000);
                    if (_onSaved)
                        _onSaved();
                    refreshReports();
                }, showError);
            });
            refreshControls();
            SWIUtil.ShowModal($("#webschedule-dialog"));
        }, function (data) {
            SWIUtil.HideModal($waitDialog);
            SWIUtil.ShowMessage("alert-danger", data.error, 0);
        });
    }
    SWISchedule.Edit = Edit;
    function confirmDelete(schedule, onDeleted) {
        $("#message-title").html(SWIUtil.tr("Warning"));
        $("#message-text").text(SWIUtil.tr2("Do you really want to delete the schedule") + " '" + schedule.name + "' ?");
        $("#message-cancel-button").html(SWIUtil.tr("Cancel"));
        $("#message-ok-button").html(SWIUtil.tr("OK"));
        $("#message-ok-button").unbind("click").on("click", function () {
            SWIUtil.HideModal($("#message-dialog"));
            _gateway.DeleteWebSchedule(schedule.guid, function (data) {
                SWIUtil.ShowMessage("alert-success", data.Message, 5000);
                if (onDeleted)
                    onDeleted();
                if (_onSaved)
                    _onSaved();
                refreshReports();
            });
        });
        SWIUtil.ShowModal($("#message-dialog"));
    }
    function actionButton(icon, title, cssClass) {
        return $("<button type='button' class='btn btn-sm py-0 " + cssClass + "'>").attr("title", SWIUtil.tr2(title)).append($("<i>").addClass(icon));
    }
    /** Loads the web schedules of the user in the 'My schedules' dialog */
    function loadList() {
        _gateway.GetWebSchedules(function (schedules) {
            var $body = $("#webschedules-table tbody");
            $body.empty();
            SWIUtil.ShowHideControl($("#webschedules-table"), schedules.length > 0);
            SWIUtil.ShowHideControl($("#webschedules-none"), schedules.length == 0);
            schedules.forEach(function (s) {
                var $last = $("<td>").text(s.lastexecution);
                if (s.lasterror)
                    $last.append($("<i class='fa-solid fa-triangle-exclamation text-danger ms-1'>").attr("title", s.lasterror));
                var $next = $("<td>").text(s.enabled ? s.nextexecution : SWIUtil.tr2("Disabled"));
                if (!s.enabled)
                    $next.addClass("text-muted");
                var $edit = actionButton("fa-solid fa-pen", "Edit", "btn-outline-secondary");
                $edit.on("click", function () { Edit(s.path, s.guid, loadList); });
                var $execute = actionButton("fa-solid fa-play", "Execute now", "btn-outline-success");
                $execute.on("click", function () { _gateway.ExecuteWebSchedule(s.path, s.guid); });
                var $delete = actionButton("fa-solid fa-trash", "Delete", "btn-outline-danger");
                $delete.on("click", function () { confirmDelete(s, loadList); });
                $body.append($("<tr>")
                    .append($("<td>").text(s.name))
                    .append($("<td>").text(s.reportname).attr("title", s.path))
                    .append($("<td>").text(s.devicename + " (" + s.formatname + ")"))
                    .append($("<td>").text(s.recurrence))
                    .append($next)
                    .append($last)
                    .append($("<td class='text-end text-nowrap'>").append($edit).append(" ").append($execute).append(" ").append($delete)));
            });
        });
    }
    /** Opens the 'My schedules' dialog */
    function ShowList() {
        $outputPanel.hide();
        _onSaved = loadList;
        loadList();
        SWIUtil.ShowModal($("#webschedules-dialog"));
    }
    SWISchedule.ShowList = ShowList;
    /** Adds the web schedules of a report and the 'Schedule...' item in the panel of the views and outputs */
    function AddToOutputPanel($panel, path, data) {
        if (!data.canschedule)
            return;
        (data.webschedules || []).forEach(function (s) {
            var $item = $("<div>").addClass("output-item");
            //not 'output-name': its click handler executes a view or an output
            var $name = $("<a href='#'>").addClass("webschedule-link").text(s.name).attr("title", s.recurrence + (s.enabled ? "" : " - " + SWIUtil.tr2("Disabled")));
            if (!s.enabled)
                $name.addClass("text-muted");
            $name.on("click", function (e) { e.preventDefault(); Edit(path, s.guid); });
            $item.append($name);
            $item.append($("<span>").addClass("output-type-badge output-type-schedule").html(SWIUtil.tr("Schedule")));
            var $execute = $("<button type='button' class='btn btn-secondary btn-table'>").attr("title", SWIUtil.tr2("Execute now")).append($("<span class='fa-solid fa-play'></span>"));
            $execute.on("click", function () {
                $outputPanel.hide();
                _gateway.ExecuteWebSchedule(path, s.guid);
            });
            $item.append($execute);
            $panel.append($item);
        });
        var $new = $("<div>").addClass("output-item");
        var $link = $("<a href='#'>").addClass("webschedule-link").html("<i class='fa-solid fa-clock me-1'></i>" + SWIUtil.tr("Schedule..."));
        $link.on("click", function (e) { e.preventDefault(); Edit(path, ""); });
        $new.append($link);
        $panel.append($new);
    }
    SWISchedule.AddToOutputPanel = AddToOutputPanel;
    /** Shows the 'My schedules' button if the user can schedule reports or has schedules */
    function Init(profile) {
        SWIUtil.ShowHideControl($("#webschedules-nav-item"), profile.webschedules);
        $("#webschedules-nav-item").unbind("click").on("click", function () {
            SWIUtil.CollapseNavbar();
            ShowList();
        });
    }
    SWISchedule.Init = Init;
})(SWISchedule || (SWISchedule = {}));
//# sourceMappingURL=swi-schedule.js.map