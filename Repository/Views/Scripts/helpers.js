function getTopLeft(item) {
    var rect = item.getBoundingClientRect();
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
    $popup.html(content);
    $('#nav_popupmenu li').click(function (e) {
        var nav = $(this).attr("nav");
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
            executeReport(nav);
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
        var cellDatas = cellData.split('§', 6);
        $(td).html(cellDatas[5]);
        $(td).attr("class", cellDatas[4]);
        $(td).attr("style", cellDatas[3]);
        $(td).attr("navigation", cellDatas[2]);
        $(td).parent().attr("class", cellDatas[1]);
        $(td).parent().attr("style", cellDatas[0]);
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

