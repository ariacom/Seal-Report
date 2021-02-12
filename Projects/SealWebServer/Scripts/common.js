//Globals
var _executionTimer = null;
var _refreshTimer = null;
var _prevScrollpos = window.pageYOffset;
var _inExecution = false;
var _popupNavMenuTimeout = -1;

//Restrictions
function restrictionSelectChange(source) {
    var group = $(source).closest(".restrictions_group").parent();

    var idSelect = "#" + $(source).attr('id');

    if ($(source).attr('opid') != null) {
        idSelect = "#" + $(source).attr('opid');
    }
    if ($(source).attr('id') == null && $(source).attr('opid') !== null) return;
    if ($(idSelect).val() == null) return;

    var idValue = idSelect.replace("Operator", "Value");
    var value1 = group.find(idValue + "_1");
    if (value1.length == 0) {
        idValue = idSelect.replace("Operator", "Option_Value");
        value1 = group.find(idValue);
    }
    var value2 = group.find(idValue + "_2");
    var value3 = group.find(idValue + "_3");
    var value4 = group.find(idValue + "_4");

    var op = group.find(idSelect).val();
    if (op) {
        op = op.toLowerCase();
        var display1 = "inline", display2 = "inline", display3 = "inline", display4 = "inline";
        if (op == 'isnull' || op == 'isnotnull' || op == 'isempty' || op == 'isnotempty') {
            display1 = "none", display2 = "none"; display3 = "none"; display4 = "none";
        }
        else if (op == 'greater' || op == 'greaterequal' || op == 'smaller' || op == 'smallerequal') {
            display2 = "none"; display3 = "none"; display4 = "none";
        }
        else if (op == 'between' || op == 'notbetween') {
            display3 = "none"; display4 = "none";
        }
        else {
            if (value3.val() == "" && value4.val() == "") display4 = "none";
            if (value2.val() == "" && display4 == "none") display3 = "none";
            if (value1.val() == "" && display3 == "none" && display4 == "none") display2 = "none";
        }

        if (value1.parent().hasClass("date")) { //date
            value1.parent().parent().css("display", display1);
            value2.parent().parent().css("display", display2);
            value3.parent().parent().css("display", display3);
            value4.parent().parent().css("display", display4);
        }
        else if (value1.hasClass("enum")) { //enum, select picker
            value1.selectpicker(display1 == "none" ? "hide" : "show");
        }
        else if (value1.hasClass("btn-group")) { //enum, buttons
            value1.css("display", display1 == "none" ? "none" : "inline-block");
        }
        else { //numeric or text
            value1.css("display", display1);
            value2.css("display", display2);
            value3.css("display", display3);
            value4.css("display", display4);
        }
    }
}

function executeFromTrigger(source /* trigger from a control */, form /* trigger from button */, button /* trigger from button */) {
    if (_inExecution) return;

    var container = null;
    if (source) container = source.closest(".restrictions_group");
    if (container && container.hasClass("main_restriction")) { //Trigger from a restriction in main panel
        executeReport();
    }
    else {
        if (container) form = container.closest("form");
        var target = form.attr("target_window").replace("<view_id>", form.attr("id"));
        if (_urlPrefix !== "") {
            if (target) {
                form.attr("action", _urlPrefix + "ActionExecuteFromTriggerNewWindow");
                form.attr("target", target);
                form.submit();
                return;
            }
            else {
                _inExecution = true;
                form.addClass("disabled");
                if (button) button.removeClass("btn-success").addClass("btn-warning");
                if (container) {
                    container.addClass("disabled");
                    container.children(".glyphicon").css("display", "inline");
                }
                $.post(_urlPrefix + "ActionExecuteFromTrigger", form.serialize() + "&execution_guid=" + form.attr("execguid") + "&form_id=" + form.attr("id"))
                    .done(function (data) {
                        //Update each view involved
                        data.forEach(function (value) {
                            if (form.attr("id") != $(value).attr("id")) {
                                var viewId = "#" + $(value).attr("id");
                                $(viewId).html($(value).html());
                                initRestrictions(viewId);
                                initWidgetsRestrictions();
                            }
                        });
                        form.removeClass("disabled");
                        if (button) button.removeClass("btn-warning").addClass("btn-success");
                        if (container) {
                            container.removeClass("disabled");
                            container.children(".glyphicon").css("display", "none");
                        }
                        _inExecution = false;
                    });
            }
        }
        else {
            if (target) alert('Execution in an new Window is not supported from the Report Designer');
            else {
                $("#id_load").val(form.attr("id"));
                $("#header_form").attr("action", "ActionExecuteFromTrigger");
                $("#header_form").submit();
            }
        }
    }
}

function initRestrictions(parent) {
    if (!parent) parent = "";
    else parent += " ";

    //operator change
    $(parent + ".form-control").change(function () {
        restrictionSelectChange(this);
    }).change();

    $(parent + ".form-control").keyup(function () {
        restrictionSelectChange(this);
    });

    //validation
    $(parent + ".numeric_input").keyup(function () {
        var v = this.value;
        if (!$.isNumeric(v)) {
            this.value = this.value.slice(0, -1);
        }
    });

    //Select Picker
    $(parent + ".operator_select").selectpicker('refresh');

    processInitDateTimePicker(parent);

    $(parent + ".datepicker_date," + parent + ".datepicker_datetime").unbind("dp.change").on("dp.change", function (e) {
        restrictionSelectChange(this.children[0]);
    });

    //trigger enum from select
    $(parent + ".enum").unbind('changed.bs.select').on('changed.bs.select', function (e) {
        if ($(this).attr("id")) {
            if ($(this).hasClass("trigger_enum")) {
                executeFromTrigger($(this));
            }
            else {
                var action = "ActionUpdateEnumValues";
                var form = $(this).closest("form");

                //send current values
                var id = $(this).attr("id");
                var ids = "";
                $("#" + id + " option:selected").each(function () {
                    ids += $(this).val() + "\n";
                });
                if (_urlPrefix !== "") {
                    $.post(_urlPrefix + action, { execution_guid: form.attr("execguid"), id: id, values: ids })
                        .done(function (data) {
                        });
                }
                else {
                    $("#id_load").val(id);
                    $("#values_load").val(ids);
                    $("#header_form").attr("action", action);
                    $("#header_form").submit();
                }
            }
        }
    });

    //trigger enum from buttons
    $(parent + "input.trigger_enum").change(function () {
        if ($(this).attr("name")) {
            executeFromTrigger($(this));
        }
    });

    //trigger input: text, date, numeric
    $(parent + "input.trigger," + parent + "textarea.trigger").change(function (e) {
        if ($(this).attr("name")) {
            executeFromTrigger($(this));
        }
    });

    //trigger input numeric: force if empty
    $(parent + ".numeric_input.trigger").blur(function () {
        if ($(this).val() == "" && $(this).attr("name")) {
            executeFromTrigger($(this));
        }
    })
    //trigger input: date picker
    setTimeout(function () {
        $(parent + ".datepicker_date.trigger," + parent + ".datepicker_datetime.trigger").unbind("dp.change").on("dp.change", function (e) {
            var input = $(this.children[0]);
            if (input && (!e.date || e.date !== e.oldDate) && input.attr("name")) {
                executeFromTrigger($(this));
            }
        });
    }, 500);

    //dynamic filter for enums
    $(parent + ".enum_dynamic").unbind("shown.bs.select").on('shown.bs.select', function () {
        if ($(this).attr("id")) {
            $("#id_load").val($(this).attr("id"));

            if ($(this).attr("dependencies") == "true" && $(this).attr("filterchars") == 0) requestEnumData("", false);
            else setEnumMessage($(this).attr("id"));

            var filter = "";
            $(parent + ".bs-searchbox input").unbind("input").on("input", function (evt) {
                var $search = $(evt.target);
                if ($search.val() !== filter) { // search value is changed
                    filter = $search.val();
                    if (filter.length >= $("#" + $("#id_load").val()).attr("filterchars")) { // more than xx characters
                        requestEnumData(filter, true);
                    }
                }
            });
        }
    });

    //Update button for view restrictions
    $(parent + ".update_view_restrictions").unbind("click").on("click", function () {
        var formId = $(this).attr("id").replace("button_", "form_");
        var form = $("#" + formId);
        var button = $(this);
        executeFromTrigger(null, form, button);
        return false;
    });
}

//navigation menu
function showPopupNavMenu(source, content, forChart) {
    var $popup = $('#nav_popupmenu');
    if (!$popup.length) {
        $popup = $("<ul id='nav_popupmenu' class='dropdown-menu' role='menu'/>");
        $("body").append($popup);
        $popup
            .mouseenter(function () {
                $popup.show();
            })
            .mouseleave(function () {
                $popup.hide();
            });
    }

    $popup.html(content);
    $('#nav_popupmenu li,#nav_popupmenu span').unbind("click").on("click", function (e) {
        var nav = $(this).attr("nav");
        var target = "";
        if (!nav) {
            e.stopPropagation();
            nav = $(this).closest("li").attr("nav");
            target = "_blank";
        }
        executeReportNavigation(nav, target);
        $popup.hide();
    });

    var scrollLeft = document.body.scrollLeft + document.documentElement.scrollLeft;
    var scrollTop = document.body.scrollTop + document.documentElement.scrollTop;
    var posLeft = forChart ? source.clientX + scrollLeft : source.offset().left;
    var posTop = forChart ? source.clientY + scrollTop : source.offset().top + source.height() + 1;
    if (!forChart && source.height() > 30) posTop -= (source.height() - 30);
    posLeft += Math.min(0, window.innerWidth + scrollLeft - $popup.width() - posLeft);
    posTop += Math.min(0, window.innerHeight + scrollTop - $popup.height() - posTop - 50);
    if ($(source).hasClass("text-right")) posLeft += source.width() - 120;
    $popup
        .show()
        .css({
            position: "absolute",
            left: posLeft,
            top: posTop,
            opacity: 1
        });

    if (_popupNavMenuTimeout != -1) clearTimeout(_popupNavMenuTimeout);
    _popupNavMenuTimeout = setTimeout(function () { $popup.hide(); }, 4000);
}

//navigation initialization 
function initNavCells(parentSelector) {
    var selector = "td:not([navigation=''])";
    if (parentSelector) selector = parentSelector + " " + selector;
    $(selector)
        .mouseenter(function (e) {
            var nav = $(this).attr("navigation");
            if (nav) {
                showPopupNavMenu($(this), nav, false);
            }
        })
        .mouseleave(function () {
            $("#nav_popupmenu").hide();
        });
}

//data tables, server pagination
function getTableData(datatable, guid, viewid, pageid, data, callback, settings) {
    try {
        var params = data.draw + "§" + settings.aaSorting + "§" + settings.oPreviousSearch.sSearch.replace("<", "&lt;").replace(">", "&gt;") + "§" + settings._iDisplayLength + "§" + settings._iDisplayStart;
        if (_urlPrefix != "") {
            $.post(_urlPrefix + "ActionGetTableData", { execution_guid: guid, viewid: viewid, pageid: pageid, parameters: params })
                .done(function (data) {
                    try {
                        var json = jQuery.parseJSON(data);
                        callback(json);
                        initNavCells("[viewid='" + viewid + "']");
                    }
                    catch (ex) {
                        datatable[0].innerHTML = "Error loading data..." + "<br>" + ex.message;
                    }
                });
        }
        else {
            $("#header_form").attr("action", "ActionGetTableData");
            $("#parameter_tableload").html(params);
            $("#viewid_tableload").val(viewid);
            $("#pageid_tableload").val(pageid);
            $("#header_form").submit();
            var json = jQuery.parseJSON($("#parameter_tableload").text());
            callback(json);
            $("#parameter_tableload").html("");
            initNavCells();
        }
    }
    catch (ex2) {
        datatable[0].innerHTML = "Error loading data..." + "<br>" + ex2.message;
    }
}


//Enum select picker
function requestEnumData(filter, forceNoMessage) {
    var result;

    if (_urlPrefix != "") {
        $.post(_urlPrefix + "ActionGetEnumValues", { execution_guid: _executionGUID, enum_id: $("#id_load").val(), filter: filter })
            .done(function (data) {
                result = jQuery.parseJSON(data);
                fillEnumSelect(result, forceNoMessage || result.length > 0);
            });
    }
    else {
        $("#header_form").attr("action", "ActionGetEnumValues");
        $("#filter_enumload").val(filter);
        $("#header_form").submit();
        result = jQuery.parseJSON($("#parameter_enumload").text());
        fillEnumSelect(result, forceNoMessage || result.length > 0);
    }
    return result;
}


// Show Hide Navbar
function showHideNavbar() {
    const currentScrollPos = window.pageYOffset;
    if (_prevScrollpos > currentScrollPos) {
        $('#bar_top').fadeIn();
        $("#bar_top").css("top", 0);
    }
    else if (currentScrollPos > 0) {
        $('#bar_top').fadeOut();
        $("#bar_top").css("top", -1 * $("#bar_top").height());
    }
    _prevScrollpos = currentScrollPos;
}


function executeTimer() {
    if (_executionTimer != null) {
        var $messages = $("#execution_messages");
        var $form = $("#header_form");
        $form.attr("action", _urlPrefix + "ActionRefreshReport");
        if (_urlPrefix != "") {
            $.post(_urlPrefix + "ActionRefreshReport", { execution_guid: _executionGUID })
                .done(function (data) {
                    if (data.result_ready) {
                        clearInterval(_executionTimer);
                        if (_executionGUID != null) {
                            if (_reportStandalone) {
                                $form.attr("action", _urlPrefix + "Result");
                                $form.submit();
                            }
                            else {
                                //Execution from the menu
                                $.post(_urlPrefix + "Result", { execution_guid: _executionGUID })
                                    .done(function (data) {
                                        $("#report-body").html(data);
                                        processReportExecuted();
                                    });
                            }
                        }
                    }
                    else if (data.progression_message != null) {
                        setProgressBarMessage("#progress_bar", data.progression, data.progression_message, "progress-bar-success");
                        setProgressBarMessage("#progress_bar_tasks", data.progression_tasks, data.progression_tasks_message, "progress-bar-primary");
                        setProgressBarMessage("#progress_bar_models", data.progression_models, data.progression_models_message, "progress-bar-info");
                        if (data.execution_messages != null && $messages.length) {
                            $messages.removeClass('hidden');
                            $messages.html(data.execution_messages);
                            scrollMessages(_printLayout);
                        }
                    }
                    else if (data.error != null) {
                        setProgressBarMessage("#progress_bar", 100, data.error, "progress-bar-danger");
                        clearInterval(_executionTimer);
                        $("#execute_button").css("display", "none");
                    }
                });
        }
        else {
            $messages.removeClass('hidden');
            $form.submit();
        }
    }
}

function executeReportNavigation(nav, target) {
    //check execution triggers
    if (_inExecution) return;

    var form = $("#header_form");
    if (nav != null && nav.startsWith("HL:")) { //Hyperlink
        window.open(nav.replace("HL:", ""), ', ');
        return;
    }

    if (nav != null && nav.startsWith("RE:")) { //Report execution
        if (_urlPrefix == "") alert('Execution in an new Window is not supported from the Report Designer');
        else {
            postForm(_urlPrefix + "SWExecuteReport",
                "_blank",
                { path: nav.replace("RE:", "") }
            );
        }
        return;
    }

    $("#navigation_id").val(nav);
    $("#navigation_target").val(target);

    if (nav != null && target) { //Navigation to other window
        form.attr("target", target);
        form.attr("action", _urlPrefix + "ActionNavigate");
        form.submit();
        return;
    }

    if (nav != null && nav.startsWith("FD:")) { //File download
        form.attr("action", _urlPrefix + "ActionNavigate");
        form.submit();
        return;
    }

    executeReport(nav);
}

function executeReport(nav) {
    //check execution triggers
    if (_inExecution) return;

    var form = $("#header_form");
    _inExecution = true;
    if (_refreshTimer) clearInterval(_refreshTimer);

    var url = "";
    if (_executionTimer == null) {
        $("#information_div").html("");

        var messages = $("#execution_messages");
        if (messages.length) {
            messages.addClass('hidden');
            messages.html("");
        }

        processInitProgressBar();

        url = _urlPrefix + (nav == null ? "ActionExecuteReport" : "ActionNavigate");
        if (nav == null || _urlPrefix == "") _executionTimer = setInterval(function () { executeTimer(); }, 1200);
    }
    else {
        url = _urlPrefix + "ActionCancelReport";
    }

    form.attr("target", "");
    if (_urlPrefix != "") {
        $.post(url, form.serialize()).done(function (data) {
            if (nav != null) {
                $(_reportStandalone ? 'body' : '#report-body').html(data);
            }
        });
    }
    else {
        form.attr("action", url);
        form.submit();
    }
    //spinner
    if ($("#nav_button").children().length == 0) $("#nav_button").append($("<i class='fa fa-spinner fa-spin fa-sm'></i>"));
    //disable controls during execution
    $('#restrictions_div').addClass("disabled");
    $('.view').css("display", "none");
    $("#nav_button").attr("disabled", "disabled");

    if (!_printLayout) setMessageHeight();

    processShowMsgDuringExec();
}

function mainInit() {
    _executionTimer = null;
    _refreshTimer = null;
    _prevScrollpos = window.pageYOffset;
    _inExecution = false;
    _popupNavMenuTimeout = -1;

    //force execute
    $("input").unbind("keydown").on("keydown", function (event) {
        if (event.keyCode == 13 && !$(this).hasClass("trigger")) $("#execute_button").focus();
    });

    $("#execute_button").unbind("click").on("click", function () {
        //Collapse navbar
        if ($('.navbar-toggle').css('display') !== 'none') $('.navbar-toggle').click();
        _inExecution = false;
        executeReport();
    });

    //restriction button
    $("#restrictions_button").unbind("click").on("click", function () {
        var showRestriction = !$("#restrictions_div").hasClass("in");
        var hasContentDiv = $("#content_div").length > 0;
        if (_generateHTMLDisplay) processSubmitViewParameter("restriction_button", showRestriction);

        if (hasContentDiv) {
            setTimeout(function () {
                $("#restrictions_div").collapse('toggle');
                $("#restrictions_button").toggleClass("active");
            }, showRestriction ? 400 : 10);

            setTimeout(function () {
                $("#content_div").removeClass();
                if (!showRestriction) {
                    $("#content_div").addClass("col-md-12");
                }
                else {
                    $("#content_div").addClass($("#content_div").attr("classori"));
                }
            }, showRestriction ? 10 : 400);
        }
        else {
            $("#restrictions_div").collapse('toggle');
            $("#restrictions_button").toggleClass("active");
        }
    });

    //widget title
    $(".widget-title").unbind("click").on("click", function () {
        if (_urlPrefix == "") alert('Execution in an new Window is not supported from the Report Designer');
        else {
            postForm(_urlPrefix + "SWExecuteReport",
                "_blank",
                { path: $(this).attr("path"), viewGUID: $(this).attr("viewGUID") }
            );
        }
    });

    //print layout
    if (_printLayout) {
        $("nav").removeClass("navbar-fixed-top");
        $("#report_body_container").css("padding-top", "0px");
    }

    //tabs buttons
    $(".sr_tab").unbind("click").on("click", function () {
        var buttonId = $(this).attr("id");
        if (_generateHTMLDisplay) processSubmitViewParameter("information_button", buttonId == "information_button");
        if ($("#message_button").length) {
            var messageMode = "enabled";
            if (buttonId == "message_button") {
                messageMode = "enabledshown";
                scrollMessages(_printLayout);
            }
            if (_generateHTMLDisplay) processSubmitViewParameter("messages_mode", messageMode);
        }
        redrawDataTables();

        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //result links
    $(".result_item").unbind("click").on("click", function () {
        var form = $("#header_form");
        form.attr("target", _urlPrefix != "" ? "_blank" : "");
        form.attr("action", _urlPrefix + $(this).attr("id"));
        form.submit();
        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //navigation
    if (_hasNavigation) {
        $("#nav_button").unbind("mouseenter").on("mouseenter", function () {
            if (_urlPrefix != "") {
                $.post(_urlPrefix + "ActionGetNavigationLinks", { execution_guid: _executionGUID })
                    .done(function (data) {
                        if (data.links != null && data.links != "") {
                            initNavMenu();
                            $("#nav_menu").html(data.links);
                            if ($("#nav_menu").children().length > 1) {
                                $("#nav_menu li a").unbind("click").on("click", function () {
                                    if (_reportStandalone) {
                                        var $form = $("#header_form");
                                        $("#execution_guid").val($(this).attr("execution_guid"));
                                        $form.attr("action", _urlPrefix + "HtmlResultFile");
                                        $form.submit();
                                    }
                                    else {
                                        //Execution from the menu
                                        _main.toggleFoldersReport(true);
                                        $.post(_urlPrefix + "HtmlResultFile", { execution_guid: $(this).attr("execution_guid") })
                                            .done(function (data) {
                                                $("#report-body").html(data);
                                                processReportExecuted();
                                            });
                                    }
                                });
                                showNavMenu();
                            }
                        }
                    });
            }
            else {
                $("#header_form").attr("action", "ActionGetNavigationLinks");
                initNavMenu();
                $("#header_form").submit();
                showNavMenu();
            }
        })
            .unbind("mouseleave").on("mouseleave", function () {
                $("#nav_menu").hide();
            });

        $("#nav_badge").removeClass("hidden");
    }
    else {
        $("#nav_badge").addClass("hidden");
    }
    initNavCells();

    if (!_printLayout) {
        initScrollReport();
    }
    initResize(_printLayout);
}
