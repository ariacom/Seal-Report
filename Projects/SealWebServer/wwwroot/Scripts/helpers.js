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

//data tables dtCreatedCell
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

//data tables dtRenderer
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
    $("#message_autoscroll").unbind("click").on("click", function () {
        if (_generateHTMLDisplay) processSubmitViewParameter("messages_autoscroll", $('#message_autoscroll').is(":checked"));
    });

    //message options
    $("#message_export").unbind("click").on("click", function () {
        processOpenMessagesInNewWindow();
    });
}

function scrollMessages(printLayout) {
    if ($('#message_autoscroll').is(":checked")) {
        var messages = $("#execution_messages");
        if (!printLayout) setMessageHeight();
        if (messages && messages[0] && messages[0].scrollHeight) {
            setTimeout(function () { messages.scrollTop(messages[0].scrollHeight); }, 200);
        }
    }
}

function setMessageHeight() {
    setTimeout(function () {
        var offset = $("#progress_panel").height() + $("#alert_status").height() + $("#restrictions_div").height() + 110;
        var height = (Math.max(document.documentElement.clientHeight, window.innerHeight || 0) - offset);
        $("#execution_messages").css("height", height + "px");
    }, 100);
}

function resize(printLayout) {
    if (!printLayout) {
        setTimeout(function () { $("#report_body_container").css("padding-top", $("#bar_top").height() + 15); }, 200);
        setMessageHeight();
    }
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

function setProgressBarMessage(selector, progression, message, classname) {
    $(selector).css('width', progression + '%').css('min-width', '120px').attr('aria-valuenow', progression);
    $(selector).html(message);
    $(selector).removeClass("progress-bar-danger").removeClass("progress-bar-warning").removeClass("progress-bar-success").removeClass("progress-bar-primary");
    $(selector).addClass(classname);
}

function initWidgetsRestrictions(parent) {
    if (!parent) parent = "";
    else parent += " ";

    //Handle overflow for restrictions in widgets
    $(parent + ".enum," + parent + ".operator_select").unbind("show.bs.dropdown").on('show.bs.dropdown', function () {
        $('.panel-widget').css("overflow", "visible");
        $('.panel-widget').css("z-index", "0");
        $(this).closest(".panel-widget").css("z-index", "1");
    });
    $(parent + ".enum," + parent + ".operator_select").unbind("hidden.bs.dropdown").on('hidden.bs.dropdown', function () {
        $('.panel-widget').css("overflow", "auto");
        $('.panel-widget').css("z-index", "1");
    });
    $(parent + ".datepicker_date," + parent + ".datepicker_datetime").unbind("dp.show").on("dp.show", function (e) {
        $('.panel-widget').css("overflow", "visible");
        $('.panel-widget').css("z-index", "0");
        $(this).closest(".panel-widget").css("z-index", "1");
    });
    $(parent + ".datepicker_date," + parent + ".datepicker_datetime").unbind("dp.hide").on("dp.hide", function (e) {
        $('.panel-widget').css("overflow", "auto");
        $('.panel-widget').css("z-index", "1");
    });
}
function initScrollReport() {
    //scroll
    $(window).unbind("scroll").scroll(function () {
        //back to top
        if ($(this).scrollTop() > 50) $('#back-to-top').fadeIn();
        else $('#back-to-top').fadeOut();
        //nav bar
        showHideNavbar();
        //alerts
        $('.sr-alert').alert('close');
    });

    $('#back-to-top').unbind("click").on("click", function () {
        $('#back-to-top').tooltip('hide');
        $('body,html').animate({
            scrollTop: 0
        }, 800);
        return false;
    });

    $('#back-to-top-close').unbind("click").on("click", function () {
        $('#back-to-top').tooltip('hide');
        $('#back-to-top').fadeOut();
        return false;
    });

    $('#back-to-top').tooltip('show');
}

function initResize(printLayout) {
    //resize handler
    $(window).unbind('resize').on('resize', function () {
        resize(printLayout);
    });
    resize(printLayout);
}

function postForm(url, target, data) {
    var form = $('<form/>', {
        method: 'POST',
        target: target,
        action: url
    });
    $('body').append(form);
    for (var i in data) {
        form.append($('<input/>', {
            type: 'hidden',
            name: i,
            value: data[i]
        }));
    }
    form.submit();
}