
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
        if (generateHTMLDisplay) submitViewParameter(executionGUID, rootViewId, "messages_autoscroll", $('#message_autoscroll').is(":checked"));
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
            $.post(urlPrefix + "ActionRefreshReport", { execution_guid: executionGUID })
                .done(function (data) {
                    if (data.result_ready) {
                        clearInterval(executionTimer);
                        if (executionGUID != null) {
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
                        setProgressBarMessage("#progress_bar", 100, data.error, "progress-bar-danger");
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
    $(selector).removeClass("progress-bar-danger").removeClass("progress-bar-warning").removeClass("progress-bar-success").removeClass("progress-bar-primary");
    $(selector).addClass(classname);
}

function executeReport(nav, target) {
    //check execution triggers
    if (inExecution) return;

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

    if (nav != null && target) { //Navigation to other window
        form.attr("target", target);
        form.attr("action", urlPrefix + "ActionNavigate");
        form.submit();
        return;
    }

    if (nav != null && nav.startsWith("FD:")) { //File download
        form.attr("action", urlPrefix + "ActionNavigate");
        form.submit();
        return;
    }

    inExecution = true;
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
        $.post(urlPrefix + "ActionGetEnumValues", { execution_guid: executionGUID, enum_id: $("#id_load").val(), filter: filter })
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

    if (!noMessage) setEnumMessage($("#id_load").val());
}

function setEnumMessage(id) {
    var $enum = $("#" + id);
    var $message = $("#enum-message");
    if ($message) $message.text("");
    if ($enum.attr("message")) {
        //Add info message
        if ($message.length == 0) {
            $message = $("<li>").attr("id", "enum-message").addClass("no-results");
        }
        $message.text($enum.attr("message"));
        $enum.parent().children("div").children("ul").append($message);
    }
}

function mainInit() {
    //force execute
    $("input").keydown(function (event) {
        if (event.keyCode == 13) $("#execute_button").focus();
    });

    $("#execute_button").click(function () {
        //Collapse navbar
        if ($('.navbar-toggle').css('display') !== 'none') $('.navbar-toggle').click();
        inExecution = false;
        executeReport();
    });

    //restriction button
    $("#restrictions_button").click(function () {
        var showRestriction = !$("#restrictions_div").hasClass("in");
        var hasContentDiv = $("#content_div").length > 0;
        if (generateHTMLDisplay) submitViewParameter(executionGUID, rootViewId, "restriction_button", showRestriction);

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
        if (generateHTMLDisplay) submitViewParameter(executionGUID, rootViewId, "information_button", buttonId == "information_button");
        if ($("#message_button").length) {
            var messageMode = "enabled";
            if (buttonId == "message_button") {
                messageMode = "enabledshown";
                scrollMessages();
            }
            if (generateHTMLDisplay) submitViewParameter(executionGUID, rootViewId, "messages_mode", messageMode);
        }
        redrawDataTables();

        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //result links
    $(".sr_result").click(function () {
        var target = $(this).attr("newtarget")
        $("#header_form").attr("target", urlPrefix != "" && target ? target : "");
        $("#header_form").attr("action", urlPrefix + $(this).attr("id"));
        $("#header_form").submit();
        //Collapse navbar
        if ($('.navbar-toggle').css('display') != 'none') $('.navbar-toggle').click();
    });

    //navigation
    if (hasNavigation) {
        $("#nav_button")
            .mouseenter(function () {
                if (urlPrefix != "") {
                    $.post(urlPrefix + "ActionGetNavigationLinks", { execution_guid: executionGUID })
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

    initRestrictions();

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
