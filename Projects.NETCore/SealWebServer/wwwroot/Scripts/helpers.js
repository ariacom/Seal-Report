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
        if (value1.hasClass("enum")) { //enum
            value1.selectpicker(display1 == "none" ? "hide" : "show");
        }
        else { //numeric or text
            value1.css("display", display1);
            value2.css("display", display2);
            value3.css("display", display3);
            value4.css("display", display4);
        }
    }
}

var inExecution = false;
function executeFromTrigger(source) {
    if (inExecution) return;
    var container = source.closest(".restrictions_group");
    if (container.hasClass("main_restriction")) { //Trigger from main panel
        executeReport();
    }
    else {
        var action = "ActionExecuteFromTrigger";
        var form = container.closest("form");
        container.addClass("disabled");
        container.children(".glyphicon").css("display", "inline");
        if (urlPrefix !== "") {
            $.post(urlPrefix + action, form.serialize() + "&execution_guid=" + form.attr("execguid"))
                .done(function (data) {
                    //Update each view involved
                    data.forEach(function (value) {
                        if (form.attr("id") != $(value).attr("id")) {
                            var viewId = "#" + $(value).attr("id");
                            $(viewId).html($(value).html());
                            initRestrictions(viewId);
                        }
                    });
                    container.removeClass("disabled");
                    container.children(".glyphicon").css("display", "none");
                    inExecution = false;
                });
        }
        else {
            $("#id_load").val(form.attr("id"));
            $("#header_form").attr("action", action);
            $("#header_form").submit();
        }
    }
    inExecution = true;
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

    //Date Picker
    $(parent + ".datepicker_datetime").datetimepicker({
        showClose: true,
        showClear: true,
        format: shortDateTimeFormat,
        tooltips: dtTooltips,
        useCurrent: false
    });

    $(parent + ".datepicker_date").datetimepicker({
        showClose: true,
        showClear: true,
        format: shortDateFormat,
        tooltips: dtTooltips,
        useCurrent: false
    });

    $(parent + ".datepicker_date," + parent + ".datepicker_datetime").datetimepicker({
        locale: languageName,
    });

    $(parent + ".datepicker_date," + parent + ".datepicker_datetime").on("dp.change", function (e) {
        restrictionSelectChange(this.children[0]);
    });

    //trigger enum from select
    $(parent + ".enum").on('hide.bs.select', function () {
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
                if (urlPrefix !== "") {
                    $.post(urlPrefix + action, { execution_guid: form.attr("execguid"), id: id, values: ids })
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
            if ($(this).parent().hasClass("date")) executeFromTrigger($(this));
            else executeFromTrigger($(this));
        }
    });

    //trigger input numeric: force if empty
    $(parent + ".numeric_input.trigger").blur(function () {
        if ($(this).val() === "" && $(this).attr("name")) {
            executeFromTrigger($(this));
        }
    })
    //trigger input: date picker
    setTimeout(function () {
        $(parent + ".datepicker_date.trigger," + parent + ".datepicker_datetime.trigger").on("dp.change", function (e) {
            var input = $(this.children[0]);
            if (input && (!e.date || e.date !== e.oldDate) && input.attr("name")) {
                executeFromTrigger($(this));
            }
        });
    }, 500);

    //dynamic filter for enums
    $(parent + ".enum_dynamic").on('shown.bs.select', function () {
        if ($(this).attr("id")) {
            $("#id_load").val($(this).attr("id"));

            if ($(this).attr("dependencies") == "true" && $(this).attr("filterchars") == 0) requestEnumData("", false);
            else setEnumMessage($(this).attr("id"));

            var filter = "";
            $(parent + ".bs-searchbox input").on("input", function (evt) {
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
    $(parent + ".update_view_restrictions").click(function () {
        var formId = $(this).attr("id").replace("button_", "form_");
        var form = $("#" + formId);
        var button = $(this);
        var action = "ActionExecuteFromTrigger";
        form.addClass("disabled");
        button.removeClass("btn-success").addClass("btn-warning");
        if (urlPrefix !== "") {
            $.post(urlPrefix + action, form.serialize() + "&execution_guid=" + form.attr("execguid"))
                .done(function (data) {
                    //Update each view involved
                    data.forEach(function (value) {
                        if (form.attr("id") != $(value).attr("id")) {
                            var viewId = "#" + $(value).attr("id");
                            $(viewId).html($(value).html());
                            initRestrictions(viewId);
                        }
                    });
                    form.removeClass("disabled");
                    button.removeClass("btn-warning").addClass("btn-success");
                    inExecution = false;
                });
        }
        else {
            setTimeout(function () {
                if (inExecution) return false;
                inExecution = true;
                $("#id_load").val(formId);
                $("#header_form").attr("action", action);
                $("#header_form").submit();
            }, 200);
        }
        return false;
    });
}


function getTopLeft(item) {
    var obj = item;
    var curleft = 0;
    var curtop = 0;
    if (obj.offsetParent) {
        do {
            curleft += obj.offsetLeft;
            curtop += obj.offsetTop;
        } while (obj == obj.offsetParent);
    }
    return [curtop, curleft];
}

//d3 formatting
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


//navigation menu
var popupNavMenuTimeout = -1;
function showPopupNavMenu(source, content, forChart, executionguid) {
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

    //For web add option to navigate in another tab
    if (urlPrefix != "" && inReport) {
        content = content.replace(new RegExp('</a>', 'g'), '<span class="external-navigation glyphicon glyphicon-share"></span></a>');
    }

    $popup.html(content);
    $('#nav_popupmenu li,#nav_popupmenu span').click(function (e) {
        var nav = $(this).attr("nav");
        var target = "";
        if (!nav) {
            e.stopPropagation();
            nav = $(this).closest("li").attr("nav");
            target = "_blank";
        }
        if (!inReport) {
            //Navigation from dashboard
            var f = $('<form method="post" target="' + executionguid + '"/>').appendTo('body');
            f.attr('action', _server + "ActionNavigate");
            f.append($('<input />').attr('name', 'execution_guid').attr('value', executionguid));
            f.append($('<input />').attr('name', 'navigation_id').attr('value', nav));
            f.children('input').attr('type', 'hidden');
            f.submit();
        }
        else {
            executeReport(nav, target);
        }
        $popup.hide();
    });

    var scrollLeft = document.body.scrollLeft + document.documentElement.scrollLeft;
    var scrollTop = document.body.scrollTop + document.documentElement.scrollTop;
    var posLeft = forChart ? source.clientX + scrollLeft : source.offset().left;
    var posTop = forChart ? source.clientY + scrollTop : source.offset().top + source.height() + 1;
    if (!forChart && source.height() > 30) posTop -= (source.height() - 30);
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
    popupNavMenuTimeout = setTimeout(function () { $popup.hide(); }, 4000);
}


//navigation initialization 
function initNavCells(executionguid, parentSelector) {
    var selector = "td:not([navigation=''])";
    if (parentSelector) selector = parentSelector + " " + selector;
    $(selector)
        .mouseenter(function (e) {
            var nav = $(this).attr("navigation");
            if (nav) {
                showPopupNavMenu($(this), nav, false, executionguid);
            }
        })
        .mouseleave(function () {
            $("#nav_popupmenu").hide();
        });
}

//redraw datatables
function redrawDataTables() {
    setTimeout(function () {
        try {
            $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
            $.fn.dataTable.tables({ visible: true, api: true }).responsive.recalc();
            $.fn.dataTable.tables({ visible: true, api: true }).fixedColumns().relayout();
        }
        catch (ex) { console.log(ex); }
    }, 200);
}

//data tables
function dtCreatedCell(td, cellData, rowData, row, col) {
    if (cellData) {
        var cellDatas = cellData.split('§');
        if (cellDatas.length > 5) {
            var html = "";
            for (var i = 5; i < cellDatas.length; i++) {
                html += (html != "" ? "§" : "") + cellDatas[i];
            }
            $(td).html(html);
        }
        else $(td).html(cellDatas[5]);
        if (cellDatas[4]) $(td).attr("class", cellDatas[4]);
        if (cellDatas[3]) $(td).attr("style", cellDatas[3]);
        if (cellDatas[2]) $(td).attr("navigation", cellDatas[2]);
        if (cellDatas[1]) $(td).parent().attr("class", cellDatas[1]);
        if (cellDatas[0]) $(td).parent().attr("style", cellDatas[0]);
    }
}

//data tables
function dtRenderer(api, rowIdx, columns) {
    var data = $.map(columns, function (col, i) {
        var cellDatas = col.data.split('§', 6);
        return col.hidden ?
            '<tr data-dt-row="' + col.rowIndex + '" data-dt-column="' + col.columnIndex + '">' +
            '<th>' + col.title + (col.title != '' ? ':' : '') + '</th> ' +
            (cellDatas.length == 1 ? '<td>' + col.data : '<td style="' + cellDatas[3] + '" class="' + cellDatas[4] + '">' + cellDatas[5]) + '</td>' +
            '</tr>' :
            '';
    }).join('');

    return data ? $('<table/>').append(data) : false;
}

//data tables, server pagination
function getTableData(datatable, guid, viewid, pageid, data, callback, settings) {
    try {
        var params = data.draw + "§" + settings.aaSorting + "§" + settings.oPreviousSearch.sSearch.replace("<", "&lt;").replace(">", "&gt;") + "§" + settings._iDisplayLength + "§" + settings._iDisplayStart;
        if (urlPrefix != "") {
            $.post(urlPrefix + "ActionGetTableData", { execution_guid: guid, viewid: viewid, pageid: pageid, parameters: params })
                .done(function (data) {
                    try {
                        var json = jQuery.parseJSON(data);
                        callback(json);
                        initNavCells(guid, "[viewid='" + viewid + "']");
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


function submitViewParameter(executionId, viewId, parameterName, parameterValue) {
    if (urlPrefix != "") {
        $.post(urlPrefix + "ActionUpdateViewParameter", { execution_guid: executionId, parameter_view_id: viewId, parameter_view_name: parameterName, parameter_view_value: parameterValue });
    }
    else {
        $("#parameter_view_id").val(viewId);
        $("#parameter_view_name").val(parameterName);
        $("#parameter_view_value").val(parameterValue);
        $("#header_form").attr("action", "ActionUpdateViewParameter");
        $("#header_form").submit();
    }
}