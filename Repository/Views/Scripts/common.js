
function restrictionSelectChange(source) {
    var group = $(source).closest(".main_restriction").parent();
    if (group.length === 0) group = $(source).closest(".view_restriction").parent();

    var idSelect = "#" + $(source).attr('id');

    if ($(source).attr('opid') != null) {
        idSelect = "#" + $(source).attr('opid');
    }
    if ($(source).attr('id') == null && $(source).attr('opid') !== null) return;
    if ($(idSelect).val() == null) return;

    var idValue = idSelect.replace("Operator", "Value");
    var value1 = group.find(idValue + "_1");
    var value2 = group.find(idValue + "_2");
    var value3 = group.find(idValue + "_3");
    var value4 = group.find(idValue + "_4");

    var op = group.find(idSelect).val().toLowerCase();
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
    else { //numeric or text
        value1.css("display", display1);
        value2.css("display", display2);
        value3.css("display", display3);
        value4.css("display", display4);
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
    if (!printLayout) setTimeout(function () { $("body").css("padding-top", $("#bar_top").height() + 15); }, 200);
    setMessageHeight();
    redrawDataTables();
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

//message menu
function initMessageMenu() {
    var messages = $("#execution_messages");
    messages.mouseenter(function (e) {
        var $menu = $("#message_popupmenu");
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
        if (generateHTMLDisplay) submitViewParameter($("#execution_guid").val(), rootViewId, "messages_autoscroll", $('#message_autoscroll').is(":checked"));
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
    var form = $("#header_form");
    if (nav != null && nav.startsWith("HL:")) { //Hyperlink
        window.open(nav.replace("HL:", ""), '_blank');
        return;
    }

    if (nav != null && nav.startsWith("RE:")) { //Report execution
        if (urlPrefix == "") alert('Not supported from the Report Designer');
        else {
            form.attr("target", "_blank");
            form.attr('action', urlPrefix + "SWExecuteReport");
            form.append($('<input />').attr('type', 'hidden').attr('name', 'path').attr('value', nav.replace("RE:", "")));
            form.submit();
        }
        return;
    }

    $("#navigation_id").val(nav);
    if (nav != null && nav.startsWith("FD:")) { //File download
        form.attr("action", urlPrefix + "ActionNavigate");
        form.submit();
        return;
    }

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
        if (nav == null || urlPrefix == "") executionTimer = setInterval(function () { executeTimer(); }, 1200);
    }
    else {
        url = urlPrefix + "ActionCancelReport";
    }

    form.attr("target", "");
    if (urlPrefix != "") {
        $.post(url, form.serialize()).done(function (data) {
            if (nav != null) $('body').html(data);
        });
    }
    else {
        form.attr("action", url);
        form.submit();
    }
    //disable controls during execution
    $('#restrictions_div').addClass("disabled");
    $('.view').css("display", "none");
    $("#nav_button").attr("disabled", "disabled");

    setMessageHeight();

    if (showMsgDuringExec) {
        $("#" + rootViewId).removeClass("active in");
        $("#report_button").parent().removeClass("active");
        $("#information_button").parent().removeClass("active");
        $("#message_div").addClass("active in");
        $("#message_button").parent().addClass("active");
    }
}

//Enum select picker
function requestEnumData(filter, forceNoMessage) {
    var result;

    if (urlPrefix != "") {
        $.post(urlPrefix + "ActionGetEnumValues", { execution_guid: $("#execution_guid").val(), enum_id: $("#id_load").val(), filter: filter })
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
    var id = "#" + $("#id_load").val();

    //Add selected items
    $(id + " option:selected").each(function () {
        var found = false;
        for (var i = 0; !found && i < data.length; i++) {
            if ($(this).val() === data[i].v) {
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

var inTrigger = false;
function executeFromTrigger(container) {
    if (inTrigger) return;
    inTrigger = true;
    if (container.hasClass("main_restriction")) { //Trigger from main panel
        executeReport();
    }
    else {
        var action = "ActionExecuteFromTrigger";
        var form = container.parent().parent().parent().parent().parent();
        container.addClass("disabled");
        container.children(".glyphicon").css("display", "inline");
        if (urlPrefix !== "") {
            $.post(urlPrefix + action, form.serialize() + "&execution_guid=" + $("#execution_guid").val())
                .done(function (data) {
                    container.removeClass("disabled");
                    container.children(".glyphicon").css("display", "none");
                    //Update each view involved
                    data.forEach(function (value) {
                        var viewId = "#" + $(value).attr("id");
                        $(viewId).html($(value).html());
                    });
                    inTrigger = false;
                });
        }
        else {
            $("#id_load").val(form.attr("id"));
            $("#header_form").attr("action", action);
            $("#header_form").submit();
        }
    }
}

function initRestrictions() {
    $(".enum").selectpicker({
        "actionsBox": true
    });

    //enum from select
    $(".enum").on('hide.bs.select', function () {
        if ($(this).attr("id")) {
            if ($(this).hasClass("trigger_enum")) {
                executeFromTrigger($(this).parent().parent());
            }
            else {
                var action = "ActionUpdateEnumValues";
                //send current values
                var id = $(this).attr("id");
                var ids = "";
                $("#" + id + " option:selected").each(function () {
                    ids += $(this).val() + "\n";
                });
                if (urlPrefix !== "") {
                    $.post(urlPrefix + action, { execution_guid: $("#execution_guid").val(), id: id, values: values, operator_value: operator })
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

    //enum from buttons
    $("input.trigger_enum").change(function () {
        if ($(this).attr("name")) {
            executeFromTrigger($(this).parent().parent().parent());
        }
    });

    //input: text, date, numeric
    $("input.trigger,textarea.trigger").change(function (e) {
        if ($(this).attr("name")) {
            if ($(this).parent().hasClass("date")) executeFromTrigger($(this).parent().parent().parent());
            else executeFromTrigger($(this).parent());
        }
    });

    //input numeric: force if empty
    $(".numeric_input.trigger").blur(function () {
        if ($(this).val() === "" && $(this).attr("name")) {
            executeFromTrigger($(this).parent());
        }
    })
    //input: date picker
    setTimeout(function () {
        $('.datepicker_date.trigger,.datepicker_datetime.trigger').on("dp.change", function (e) {
            var input = $(this.children[0]);
            if (input && e.oldDate && e.date !== e.oldDate && input.attr("name") && input.val()) {
                executeFromTrigger($(this).parent().parent());
            }
        });
    }, 500);

    //dynamic filter for enums
    $(".enum_dynamic").on('shown.bs.select', function () {
        if ($(this).attr("id")) {
            $("#id_load").val($(this).attr("id"));

            var data = [];
            if ($(this).attr("dependencies")) requestEnumData("", false);

            var filter = "";
            $(".bs-searchbox input").on("input", function (evt) {
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

    //Update view restrictions
    $(".update_view_restrictions").click(function () {
        var formId = $(this).attr("id").replace("button_", "form_");
        var form = $("#" + formId);
        var action = "ActionExecuteFromTrigger";
        form.addClass("disabled");
        if (urlPrefix !== "") {
            $.post(urlPrefix + action, form.serialize())
                .done(function (data) {
                    form.removeClass("disabled");
                    //                form.children(".glyphicon").css("display", "none");
                    //Update each view involved
                    data.forEach(function (value) {
                        $(viewId).html("#" + $(value).attr("id"));
                    });
                    //Then information
                    inTrigger = false;
                });
        }
        else {
            setTimeout(function () {
                if (inTrigger) return false;
                inTrigger = true;
                $("#id_load").val(formId);
                $("#header_form").attr("action", action);
                $("#header_form").submit();
            }, 200);
        }
        return false;
    });
}

function mainInit() {
    //force execute
    $("input").keydown(function (event) {
        if (event.keyCode == 13) $("#execute_button").focus();
    });

    $("#execute_button").click(function () {
        //Collapse navbar
        if ($('.navbar-toggle').css('display') !== 'none') $('.navbar-toggle').click();
        executeReport();
    });

    $("#restrictions_button").click(function () {
        var showRestriction = !$("#restrictions_div").hasClass("in");
        var hasContentDiv = $("#content_div").length > 0;
        if (generateHTMLDisplay) submitViewParameter($("#execution_guid").val(), rootViewId, "restriction_button", showRestriction);

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

    //print layout
    if (printLayout) {
        $("nav").removeClass("navbar-fixed-top");
        $("body").css("padding-top", "0px");
    }

    //tabs buttons
    $(".sr_tab").click(function () {
        var buttonId = $(this).attr("id");
        if (generateHTMLDisplay) submitViewParameter($("#execution_guid").val(), rootViewId, "information_button", buttonId == "information_button");
        if ($("#message_button").length) {
            var messageMode = "enabled";
            if (buttonId == "message_button") {
                messageMode = "enabledshown";
                scrollMessages();
            }
            if (generateHTMLDisplay) submitViewParameter($("#execution_guid").val(), rootViewId, "messages_mode", messageMode);
        }
        redrawDataTables();

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
    initNavCells();
}

$(document).ready(function () {
    mainInit();

    //Execute if force execution or no restriction 
    if ((forceExecution || !hasRestrictions) && !isExecuting && !isCancel) {
        executeReport();
    }

    //Select Picker
    $(".operator_select").selectpicker('refresh');
    initRestrictions();

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

    $('.datepicker_date,.datepicker_datetime').on("dp.change", function (e) {
        restrictionSelectChange(this.children[0]);
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
