
function restrictionSelectChange(source) {
    var idSelect = "#" + $(source).attr('id');
    if ($(source).attr('opid') != null) {
        idSelect = "#" + $(source).attr('opid');
    }
    if ($(source).attr('id') == null && $(source).attr('opid') !== null) return;
    if ($(idSelect).val() == null) return;

    var idValue = idSelect.replace("Operator", "Value");

    var op = $(idSelect).val().toLowerCase();
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
        if ($(idValue + "_3").val() == "" && $(idValue + "_4").val() == "") display4 = "none";
        if ($(idValue + "_2").val() == "" && display4 == "none") display3 = "none";
        if ($(idValue + "_1").val() == "" && display3 == "none" && display4 == "none") display2 = "none";
    }

    if ($(idValue + "_1").parent().hasClass("date")) { //date
        $(idValue + "_1").parent().parent().css("display", display1);
        $(idValue + "_2").parent().parent().css("display", display2);
        $(idValue + "_3").parent().parent().css("display", display3);
        $(idValue + "_4").parent().parent().css("display", display4);
    }
    else { //numeric or text
        $(idValue + "_1").css("display", display1);
        $(idValue + "_2").css("display", display2);
        $(idValue + "_3").css("display", display3);
        $(idValue + "_4").css("display", display4);
    }
}

function initNavMenu() {
    var $menu = $('#nav_menu');
    if (!$menu.length) {
        $menu = $("<ul id='nav_menu' class='dropdown-menu' role='menu'/>");
        $("body").append($menu);
        $menu
            .mouseenter(function () {
                $menu.show();
            })
            .mouseleave(function () {
                $menu.hide();
            });
    }
}

function setMessageHeight() {
    if (!printLayout) {
        setTimeout(function () {
            var offset = $("#progress_panel").height() + $("#alert_status").height() + $("#restrictions_div").height() + 110;
            var height = (Math.max(document.documentElement.clientHeight, window.innerHeight || 0) - offset);
            $("#execution_messages").css("height", height + "px");
        }, 100);
    }
}

function scrollMessages() {
    if ($('#message_autoscroll').is(":checked")) {
        var messages = $("#execution_messages");
        setMessageHeight();
        if (messages && messages[0] && messages[0].scrollHeight) {
            setTimeout(function () { messages.scrollTop(messages[0].scrollHeight); }, 200);
        }
    }
}

function resize() {
    if (!printLayout) $("body").css("padding-top", $("#bar_top").height() + 15);
    setMessageHeight();
}

function showNavMenu() {
    $("#nav_menu")
        .show()
        .css({
            position: "absolute",
            "z-index": "1040",
            left: $("#nav_button").offset().left,
            top: $("#nav_button").offset().top + 2 * $("#nav_button").height()
        });
}

function realMouseCoords(event) {
    var totalOffsetX = 0;
    var totalOffsetY = 0;
    var canvasX = 0;
    var canvasY = 0;
    var currentElement = this;

    do {
        totalOffsetX += currentElement.offsetLeft - currentElement.scrollLeft;
        totalOffsetY += currentElement.offsetTop - currentElement.scrollTop;
    }
    while (currentElement = currentElement.offsetParent)

    canvasX = event.pageX - totalOffsetX;
    canvasY = event.pageY - totalOffsetY;
    return { x: canvasX, y: canvasY }
}

//navigation menu
var popupNavMenuTimeout = -1;
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
    $('#nav_popupmenu li').click(function (e) {
        executeReport($(this).attr("nav"));
        $popup.hide();
    });

    var scrollLeft = document.body.scrollLeft + document.documentElement.scrollLeft;
    var scrollTop = document.body.scrollTop + document.documentElement.scrollTop;
    var posLeft = forChart ? source.clientX + scrollLeft : source.offset().left;
    var posTop = forChart ? source.clientY + scrollTop : source.offset().top + source.height() + 3;
    posLeft += Math.min(0, window.innerWidth + scrollLeft - $popup.width() - posLeft);
    posTop += Math.min(0, window.innerHeight + scrollTop - $popup.height() - posTop - 50);

    $popup
        .show()
        .css({
            position: "absolute",
            left: posLeft,
            top: posTop
        });

    if (popupNavMenuTimeout != -1) clearTimeout(popupNavMenuTimeout);
    popupNavMenuTimeout = setTimeout(function () { $popup.hide(); }, 3000);
}


function initNavCells() {
    $("td:not([navigation=''])")
        .mouseenter(function (e) {
            if ($(this).attr("navigation")) {
                showPopupNavMenu($(this), $(this).attr("navigation"), false);
            }
        })
        .mouseleave(function () {
            $("#nav_popupmenu").hide();
        });
}

//message menu
function initMessageMenu() {
    var messages = $("#execution_messages");
    messages.mouseenter(function (e) {
        var $menu = $("#message_popupmenu")
        $menu
            .mouseenter(function () {
                $menu.show();
            })
            .mouseleave(function () {
                $menu.hide();
            });

        $menu
            .show()
            .css({
                position: "absolute",
                "z-index": "140",
                left: $("#message_div").width() - $menu.width() - 80,
                top: $("#message_div").offset().top
            });
        ;
    });
    messages.mouseleave(function () {
        $("#message_popupmenu").hide();
    });

    //autoscroll
    $("#message_autoscroll").click(function () {
        submitViewParameter(rootViewId, "messages_autoscroll", $('#message_autoscroll').is(":checked"));
    });

    //message options
    $("#message_export").click(function () {
        var myWindow = window.open('');
        myWindow.document.write("<head><title>" + messagesText + "</title></head><div id='messages'>" + $("#execution_messages").html() + "<div>");
    });
}

function executeTimer() {
    if (executionTimer != null) {
        var $messages = $("#execution_messages");
        var $form = $("#header_form");
        $form.attr("action", urlPrefix + "ActionRefreshReport");
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionRefreshReport", { execution_guid: $("#execution_guid").val() })
                .done(function (data) {
                    if (data.result_ready) {
                        clearInterval(executionTimer);
                        if ($("#execution_guid").val() != null) {
                            $form.attr("action", urlPrefix + "Result");
                            $form.submit();
                        }
                    }
                    else if (data.progression_message != null) {
                        setProgressBarMessage("#progress_bar", data.progression, data.progression_message, "progress-bar-success");
                        setProgressBarMessage("#progress_bar_tasks", data.progression_tasks, data.progression_tasks_message, "progress-bar-primary");
                        setProgressBarMessage("#progress_bar_models", data.progression_models, data.progression_models_message, "progress-bar-info");
                        if (data.execution_messages != null && $messages.length) {
                            $messages.removeClass('hidden');
                            $messages.html(data.execution_messages);
                            scrollMessages();
                        }
                    }
                    else if (data.error != null) {
                        setProgressBarMessage("#progress_bar", 100, data.error, "progress-bar-error");
                        clearInterval(executionTimer);
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

function setProgressBarMessage(selector, progression, message, classname) {
    $(selector).css('width', progression + '%').css('min-width', '120px').attr('aria-valuenow', progression);
    $(selector).html(message);
    $(selector).removeClass("progress-bar-error").removeClass("progress-bar-warning").removeClass("progress-bar-success").removeClass("progress-bar-primary");
    $(selector).addClass(classname);
}

function executeReport(nav) {
    if (refreshTimer) clearInterval(refreshTimer);

    var url = "";
    if (executionTimer == null) {
        $("#information_div").html("");

        var messages = $("#execution_messages");
        if (messages.length) {
            messages.addClass('hidden');
            messages.html("");
        }

        $(".alert-danger").addClass('hidden');
        $(".alert-danger").html("");

        $("#execute_button").text(cancelText);
        $("#execute_button").removeClass("btn-success").addClass("btn-warning");

        $("#progress_panel").removeClass('hidden');
        $(".progress").removeClass('hidden');
        setProgressBarMessage("#progress_bar", 5, startingExecText, "progress-bar-success");

        url = urlPrefix + (nav == null ? "ActionExecuteReport" : "ActionNavigate");
        if (nav == null || urlPrefix == "") executionTimer = setInterval(function () { executeTimer() }, 1200);
    }
    else {
        url = urlPrefix + "ActionCancelReport";
    }

    $("#navigation_id").val(nav);
    $("#header_form").attr("target", "");
    if (urlPrefix != "") {
        $.post(url, $("#header_form").serialize()).done(function (data) {
            if (nav != null) $('body').html(data);
        });
    }
    else {
        $("#header_form").attr("action", url);
        $("#header_form").submit();
    }
    //disable controls during execution
    $('#restrictions_div input').prop("disabled", true);
    $('#restrictions_div select').attr("disabled", true);
    $('#restrictions_div textarea').prop("disabled", true);
    $('#restrictions_div select').selectpicker('refresh');
    $('.view').css("display", "none");
    $("#nav_button").attr("disabled", "disabled");

    setMessageHeight();
}

function submitViewParameter(viewId, parameterName, parameterValue) {
    if (generateHTMLDisplay) {
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionUpdateViewParameter", { execution_guid: $("#execution_guid").val(), parameter_view_id: viewId, parameter_view_name: parameterName, parameter_view_value: parameterValue });
        }
        else {
            $("#parameter_view_id").val(viewId);
            $("#parameter_view_name").val(parameterName);
            $("#parameter_view_value").val(parameterValue);
            $("#header_form").attr("action", "ActionUpdateViewParameter");
            $("#header_form").submit();
        }
    }
}

function getTableData(datatable, guid, viewid, pageid, data, callback, settings) {
    try {
        var params = data.draw + "§" + settings.aaSorting + "§" + settings.oPreviousSearch.sSearch.replace("<", "&lt;").replace(">", "&gt;") + "§" + settings._iDisplayLength + "§" + settings._iDisplayStart
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionGetTableData", { execution_guid: guid, viewid: viewid, pageid: pageid, parameters: params })
                .done(function (data) {
                    try {
                        var json = jQuery.parseJSON(data);
                        callback(json);
                        initNavCells();
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

function mainInit() {
    //force execute
    $("input").keydown(function (event) {
        if (event.keyCode == 13) $("#execute_button").focus()
    });

    $("#execute_button").click(function () {
        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
        executeReport();
    });

    $("#restrictions_button").click(function () {
        submitViewParameter(rootViewId, "restriction_button", !$("#restrictions_div").hasClass("in"));
        $("#restrictions_button").toggleClass("active");
        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //print layout
    if (printLayout) {
        $("nav").removeClass("navbar-fixed-top");
        $("body").css("padding-top", "0px");
    }

    //tabs buttons
    $(".sr_tab").click(function () {
        submitViewParameter(rootViewId, "information_button", false);
        if ($("#message_button").length) {
            submitViewParameter(rootViewId, "message_button", false);
            scrollMessages();
        }
        submitViewParameter(rootViewId, $(this).attr("id"), true);

        setTimeout(function () { //redraw dataTables
            $($.fn.dataTable.tables(true)).DataTable().columns.adjust().responsive.recalc();
        }, 200);

        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //result links
    $(".sr_result").click(function () {
        $("#header_form").attr("target", urlPrefix != "" ? "_blank" : "");
        $("#header_form").attr("action", urlPrefix + $(this).attr("id"));
        $("#header_form").submit();
        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //operator change
    $(".form-control").change(function () {
        restrictionSelectChange(this);
    }).change();

    $(".form-control").keyup(function () {
        restrictionSelectChange(this);
    });

    //validation
    $(".numeric_input").keyup(function () {
        var v = this.value;
        if (!$.isNumeric(v)) {
            this.value = this.value.slice(0, -1);
        }
    });

    //navigation
    if (hasNavigation) {
        $("#nav_button")
            .mouseenter(function () {
                if (urlPrefix != "") {
                    $.post(urlPrefix + "ActionGetNavigationLinks", { execution_guid: $("#execution_guid").val() })
                        .done(function (data) {
                            if (data.links != null && data.links != "") {
                                initNavMenu();
                                $("#nav_menu").html(data.links);
                                showNavMenu();
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
            .mouseleave(function () {
                $("#nav_menu").hide();
            });

        $("#nav_badge").removeClass("hidden");
    }
}

//Enum select picker
function requestEnumData(filter, forceNoMessage) {
    var result;

    if (urlPrefix != "") {
        $.post(urlPrefix + "ActionGetEnumValues", { execution_guid: $("#execution_guid").val(), enum_id: $("#id_enumload").val(), filter: filter })
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


function fillEnumSelect(data, noMessage) {
    var id = "#" + $("#id_enumload").val();

    //Add selected items
    $(id + " option:selected").each(function () {
        var found = false;
        for (var i = 0; !found && i < data.length; i++) {
            if ($(this).val() == data[i].v) {
                data[i].Selected = true;
                found = true;
            }
        }

        if (!found) {
            data.push({ v: $(this).val(), t: $(this).text(), Selected: true });
        }
    });

    var $enum = $(id);
    if (data.length > 0 || $enum[0].length > 0) {
        $enum.empty();
        for (var i = 0; i < data.length; i++) {
            $enum.append(
                $("<option" + (data[i].Selected ? " selected" : "") + "></option>").attr("id", data[i].v).attr("value", data[i].v).text(data[i].t)
            );
        }
        $enum.selectpicker("refresh");
    }

    var $message = $("#enum-message");
    if ($message) $message.text("");
    if (!noMessage && $enum.attr("message")) {
        //Add info message
        if ($message.length == 0) {
            $message = $("<li>").attr("id", "enum-message").addClass("no-results");
        }
        $message.text($enum.attr("message"));
        $enum.parent().children("div").children("ul").append($message);
    }
}

function initEnums() {
    $(".enum").selectpicker({
        "liveSearch": true,
        "actionsBox": true
    });

    $(".enum").on('hide.bs.select', function (e, clickedIndex, isSelected, previousValue) {
        //send current values
        var ids = "";
        $("#" + $(this).attr("id") + " option:selected").each(function () {
            ids += $(this).val() + ",";
        });
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionUpdateEnumValues", { execution_guid: $("#execution_guid").val(), enum_id: $(this).attr("id"), values: ids });
        }
        else {
            $("#id_enumload").val($(this).attr("id"));
            $("#values_enumload").val(ids);
            $("#header_form").attr("action", "ActionUpdateEnumValues");
            $("#header_form").submit();
        }
    });

    $(".enum_dynamic").on('shown.bs.select', function (e, clickedIndex, isSelected, previousValue) {
        if ($(this).attr("id")) {
            $("#id_enumload").val($(this).attr("id"));

            var data = [];
            if ($(this).attr("dependencies")) requestEnumData("", false);

            var filter = "";
            $(".bs-searchbox input").on("input", function (evt) {
                var $search = $(evt.target);
                if ($search.val() !== filter) { // search value is changed
                    filter = $search.val();
                    if (filter.length >= $("#" + $("#id_enumload").val()).attr("filterchars")) { // more than xx characters
                        requestEnumData(filter, true);
                    }
                }
            });
        }
    });
}

$(document).ready(function () {
    mainInit();

    if ((forceExecution || !hasRestrictions) && !isExecuting && !isCancel) executeReport();

    //Select Picker
    $(".operator_select").selectpicker('refresh');
    initEnums();

    //Date Picker
    $(".datepicker_datetime").datetimepicker({
        showClose: true,
        showClear: true,
        format: shortDateTimeFormat,
        tooltips: dtTooltips
    });

    $(".datepicker_date").datetimepicker({
        showClose: true,
        showClear: true,
        format: shortDateFormat,
        tooltips: dtTooltips
    });

    $('.datepicker_date,.datepicker_datetime').datetimepicker({
        locale: languageName
    });

    //resize handler
    $(window).on('resize', function () {
        resize();
    });
    resize();

    if (!executionTimer && refreshRate > 0) refreshTimer = setInterval(executeReport, refreshRate * 1000);

    //back to top
    if (!printLayout) {
        $(window).scroll(function () {
            if ($(this).scrollTop() > 50) $('#back-to-top').fadeIn();
            else $('#back-to-top').fadeOut();
        });

        $('#back-to-top').click(function () {
            $('#back-to-top').tooltip('hide');
            $('body,html').animate({
                scrollTop: 0
            }, 800);
            return false;
        });

        $('#back-to-top-close').click(function () {
            $('#back-to-top').tooltip('hide');
            $('#back-to-top').fadeOut();
            return false;
        });

        $('#back-to-top').tooltip('show');
    }

    initMessageMenu();
    scrollMessages();

    $("#main_container").css("display", "block");
});
