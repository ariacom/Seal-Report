function viewResult(source) {
    $(headerFormId).attr("target", urlPrefix != "" ? "_blank" : "");
    $(headerFormId).attr("action", urlPrefix + $(source).attr("id"));
    $(headerFormId).submit();
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

   // alert($("#" + idButton));
   // alert(displayMore);
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
	.click(function () { executeReport() });
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
}

//nvd3 formatting
String.prototype.replaceAll = function (find, replace) {
    var str = this;
    return str.replace(new RegExp(find.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'), 'g'), replace);
};
String.prototype.valueFormat = function (value) {
    var str = this;
    return str.replaceAll(',', thousandSeparator).replaceAll('$', currencySymbol).replace('.', decimalSeparator);
};
String.prototype.normalizeNumeric = function (valueStr) {
    return parseFloat(this.replaceAll(',', '').replace(/\s+/g, ''));
};
