function viewResult(source) {
    $("#header_form").attr("target", urlPrefix != "" ? "_blank" : "");
    $("#header_form").attr("action", urlPrefix + $(source).attr("id"));
    $("#header_form").submit();
}

function restrictionSelectChange(source) {
    var op = $(source).val().toLowerCase();
    var display1 = "inline", display2 = "inline", display3 = "inline", display4 = "inline", displayMore = "inline";
    if (op == 'isnull' || op == 'isnotnull') {
        display1 = "none", display2 = "none"; display3 = "none"; display4 = "none"; displayMore = "none";
    }
    else if (op == 'greater' || op == 'greaterequal' || op == 'smaller' || op == 'smallerequal' || op == 'valueonly') {
        display2 = "none"; display3 = "none"; display4 = "none"; displayMore = "none";
    }
    else if (op == 'between' || op == 'notbetween' || op == 'valueonly') {
        display3 = "none"; display4 = "none"; displayMore = "none";
    }
    else {
        if (restrictionsToShow <= 3) display4 = "none";
        if (restrictionsToShow <= 2) display3 = "none";
        if (restrictionsToShow <= 1) display2 = "none";
    }
    var idValue = $(source).attr('id').replace("Operator", "Value");
    $("#" + idValue + "_1").css("display", display1);
    $("#" + idValue + "_2").css("display", display2);
    $("#" + idValue + "_3").css("display", display3);
    $("#" + idValue + "_4").css("display", display4);
    var idButton = $(source).attr('id').replace("Operator", "MoreButton");
    $("#" + idButton).css("display", displayMore);
    $("#" + idButton).attr("disabled", restrictionsToShow == 4);
}

function moreRestrictions(source) {
    restrictionsToShow++;
    $(".operator_select").change();
}

function mainInit() {
    //init buttons
    $("#show_button_span").buttonset();

    $("#execute_button")
	.button()
	.css('cursor', 'pointer')
	.click(function () { executeReport(); });

    //force execute
    $("input").keydown(function (event) {
        if (event.keyCode == 13) {
            $("#execute_button").focus()
        }
    });

    $(".view_result")
		.button({
		    icons: { primary: "ui-icon-newwin" }
		})
		.click(function () { viewResult(this) });

    $(".more_restrictions")
		.click(function () { moreRestrictions(this) });

    $(".datepicker").autocomplete({
        source: availableDateKeywords
    });

    //operator change
    $(".operator_select").change(function () {
        restrictionSelectChange(this);
    }).change();

    //datepicker
    $(".datepicker").datepicker({ constrainInput: false });
    if (languageName.indexOf("French") == 0) $.datepicker.setDefaults($.datepicker.regional['fr']);
    else if (languageName.indexOf("German") == 0) $.datepicker.setDefaults($.datepicker.regional['de']);
    else if (languageName.indexOf("Spanish") == 0) $.datepicker.setDefaults($.datepicker.regional['es']);
    else if (languageName.indexOf("Italian") == 0) $.datepicker.setDefaults($.datepicker.regional['it']);
    else if (languageName.indexOf("English") == 0 && !isUSdate) $.datepicker.setDefaults($.datepicker.regional['en']);
    else $.datepicker.setDefaults($.datepicker.regional['']);
    $.datepicker.setDefaults({ dateFormat: dateFormat });
}

function initButtons() {
    for (var i = 0; i < showButtonIds.length; i++) {
        if ($(showButtonIds[i] + "_button") != null) {
            $(showButtonIds[i] + "_button").attr("disabled", false);
            $(showButtonIds[i] + "_button").button({
                text: false,
                icons: { primary: showButtonIcons[i] }
            })
			.click(function () {
			    $("#body_div").animate({ scrollTop: 0 }, "slow");
			    if ($(this).attr("id") == "home_button") return;
			    var divId = $(this).attr("id").replace("_button", "_div");
			    if (window.chrome) {
			        if ($("#" + divId).css('display') == 'none') $("#" + divId).show();
			        else $("#" + divId).hide();
			    }
			    else {
			        $("#" + divId).toggle("blind");
			    }
			    setButtonLabels();
			    submitViewParameter(rootViewId, $(this).attr("id"), !$(this).prop("checked"));
			});
            $(showButtonIds[i] + "_div").css("display", (showButtonInitValues[i] ? "inline" : "none"));
            $(showButtonIds[i] + "_button").prop("checked", !showButtonInitValues[i]);
            $(showButtonIds[i] + "_button").button("refresh");
        }
    }

    $("#home_button").button({ text: false, label: backToTopText, icons: { primary: "ui-icon-home" } })
        .click(function () {
            $("#body_div").animate({ scrollTop: 0 }, "slow");
        });

    $("#nav_button").button({ text: false, label: ">", icons: { primary: "ui-icon-arrow-2-n-s" } })
            .mouseenter(function () {
                $("#nav_menu").empty();
                if ($("#nav_button").attr("disabled") == "disabled") return;
                if (urlPrefix != "") {
                    $.post(urlPrefix + "ActionGetNavigationLinks", { execution_guid: $("#execution_guid").val() })
                    .done(function (data) {
                        if (data.links != null && data.links != "") {
                            $("#nav_menu").html(data.links);
                            $("#nav_menu").menu("refresh");
                            setNavMenuSizeAndPosition()
                            $("#nav_menu").css("display", "inline");
                            $("#nav_menu").css("top", $("#nav_button").offset().top + $("#nav_button").height());
                            $("#nav_menu").css("left", $("#nav_button").offset().left);
                        }
                    });
                }
                else {
                    $("#header_form").attr("action", "ActionGetNavigationLinks");
                    $("#header_form").submit();
                    $("#nav_menu").menu("refresh");
                    setNavMenuSizeAndPosition()
                    $("#nav_menu").css("display", "inline");
                    $("#nav_menu").css("top", $(this).offset().top + $(this).height());
                    $("#nav_menu").css("left", $(this).offset().left);
                }
            });
    $("#nav_button").css("display", hasNavigation ? "inline" : "none");
}


var canvas_ctx = null;
function getTextSize(txt) {
    if (canvas_ctx == null) {
        try {
            canvas_ctx = document.createElement('canvas').getContext('2d');
            canvas_ctx.font = "bold 12pt arial";
        }
        catch (err) { };
    }
    return canvas_ctx == null ? 5 + 8 * txt.length : canvas_ctx.measureText(txt).width;
}

function setNavMenuSize() {
    var menu = $("#nav_menu");
    var maxSize = 8;
    menu.children().each(function () {
        var size = getTextSize($(this).text());
        if (size > maxSize) maxSize = size;
    });
    menu.css("width", maxSize + 10);
}

function setNavMenuSizeAndPosition() {
    var menu = $("#nav_menu");
    menu.menu("refresh");
    setNavMenuSize();
    menu.css("display", "inline");
    menu.css("top", $("#nav_button").offset().top + $("#nav_button").height());
    menu.css("left", $("#nav_button").offset().left);
}

function initNavMenu() {
    $("body").append("<ul id='nav_menu'/>");
    $("#nav_menu").menu();

    $("#nav_menu").mouseenter(function () {
        $("#nav_menu").css("display", "inline");
    });

    $("#nav_menu").mouseleave(function () {
        $("#nav_menu").css("display", "none");
    });

    initNavCells();
}

function initNavCells() {
    $(".cell_value").mouseenter(function () {
        if ($(this).attr("navigation")) {
            $("#nav_menu").empty();
            $("#nav_menu").append($(this).attr("navigation"));
            $('#nav_menu li').click(function (e) {
                executeReport($(this).attr("nav"));
                $("#nav_menu").css("display", "none");
            });
            $("#nav_menu").menu("refresh");
            setNavMenuSize();

            $("#nav_menu").css("display", "inline");
            $("#nav_menu").css("top", $(this).offset().top + $(this).height() + 3);
            $("#nav_menu").css("left", $(this).offset().left);
        }
    });

    $(".cell_value").mouseleave(function () {
        $("#nav_menu").css("display", "none");
    });
}

function executeTimer() {
    if (executionTimer != null) {
        $("#header_form").attr("action", urlPrefix + "ActionRefreshReport");
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionRefreshReport", { execution_guid: $("#execution_guid").val() })
		        .done(function (data) {
		            if (data.result_ready) {
		                clearInterval(executionTimer);
		                $("#header_form").attr("action", urlPrefix + "Result");
                        $("#header_form").submit();
		            }
		            else if (data.processing_message != null && data.execution_messages != null) {
		                $("#processing_message").html(data.processing_message);
		                if (displayMessages) {
		                    $("#execution_messages").html(data.execution_messages);
		                    $("#body_div").scrollTop($("#body_div")[0].scrollHeight);
		                }
		            }
		            else if (data.error != null) {
		                $("#processing_message").html(data.error);
		                clearInterval(executionTimer);
		                $("#wait_image").css("display", "none");
		                $("#execute_button").css("display", "none");
		            }
		        });
        }
        else {
            $("#header_form").submit();
        }
    }
}

function executeReport(nav) {
    if (hasErrors || isCancel) $("#information_div").css("display", "none");
    var url = "";
    if (executionTimer == null) {
        $("#processing_message").html(startingExecText);
        $("#processing_message").css("display", "inline");
        $("#wait_image").css("display", "inline");
        $("#execute_button").button({ label: cancelText });
        url = urlPrefix + (nav == null ? "ActionExecuteReport" : "ActionNavigate");
        if (nav == null || urlPrefix == "") executionTimer = setInterval(function () { executeTimer() }, 1200);
    }
    else {
        url = urlPrefix + "ActionCancelReport";
        $("#form_action").val("ActionCancelReport");
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
    $('.view_result').attr("disabled", "disabled");
    $('#restriction_div').attr("disabled", "disabled");
    $('#restriction_div input').attr("disabled", "disabled");
    $('.view').css("display", "none");
    $("#nav_button").attr("disabled", "disabled");
}


function setButtonLabels() {
    for (var i = 0; i < showButtonIds.length; i++) {
        if ($(showButtonIds[i] + "_button") != null) {
            var isVisible = $(showButtonIds[i] + "_button").prop("checked");
            $(showButtonIds[i] + "_button").button({ label: (!isVisible ? hideText : showText) + " " + showButtonLabels[i] });
        }
    }
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

function setMultipleSelect(className, single) {
    $(className).multipleSelect({
        isOpen: true,
        keepOpen: true,
        filter: true,
        single: single,
        maxHeight: 92,
        allSelected: allSelectedText,
        countSelected: countSelectedText,
        selectAllText: selectAllText
    });
}

function getTableData(datatable, guid, viewid, pageid, aoData, oSettings, callback) {
    try {
        var params = aoData[0].value + "§" + oSettings.aaSorting + "§" + oSettings.oPreviousSearch.sSearch + "§" + oSettings._iDisplayLength + "§" + oSettings._iDisplayStart
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


$(document).ready(function () {
    mainInit();

    if (isCancel) {
        $("#processing_message").css("display", "inline");
        $("#processing_message").text(reportCancelledText);
    }
    else $("#processing_message").css("display", "none");
    $("#header_form").css("display", "inline");
    $("#button_toolbar").css("display", "inline");

    initButtons();

    if ((forceExecution || !hasRestrictions) && !isExecuting && !isCancel) executeReport();
    if (!hasRestrictions) $('#restriction_button, label[for="restriction_button"]').hide();

    //multiselect -> small ajustements
    setMultipleSelect(".enum", false);
    setMultipleSelect(".enum_single", true);

    $(".ms-choice").css("cursor", "default").off('onClick').off('click');
    $(".ms-choice").off('onClick').off('click');

    setButtonLabels();

    initNavMenu();
});
