//redraw datatables
function redrawDataTables() {
    setTimeout(function () {
        try {
            $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
            $.fn.dataTable.tables({ visible: true, api: true }).responsive.recalc();
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
        // bootstrap-select 1.14 (BS5) nests the options <ul> inside a <div class="inner">,
        // so it is no longer a direct child of .dropdown-menu — target the inner <ul> directly.
        $enum.parent().find("ul.inner").append($message);
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
        //jQuery reports the would-be height of display:none elements, so only count elements actually shown
        function visibleHeight(selector) { var $e = $(selector); return $e.is(":visible") ? $e.height() : 0; }
        //in the left/right layout (#content_div present) the restrictions panel is a side column and does not reduce the vertical space
        var restrictionsOffset = $("#content_div").length ? 0 : visibleHeight("#restrictions_div");
        var offset = visibleHeight("#progress_panel") + visibleHeight("#alert_status") + restrictionsOffset + 110;
        var viewHeight = Math.max(document.documentElement.clientHeight, window.innerHeight || 0);
        var height = viewHeight - offset;
        var $messages = $("#execution_messages");
        if ($messages.is(":visible")) {
            //size from the real position of the box: its top plus the bottom margins/paddings/borders of its ancestors must fit in the viewport (no page scroll bar)
            var bottom = parseFloat($messages.css("margin-bottom")) || 0;
            $messages.parents().each(function () {
                if (this === document.documentElement) return false;
                var $p = $(this);
                bottom += (parseFloat($p.css("padding-bottom")) || 0) + (parseFloat($p.css("border-bottom-width")) || 0) + (parseFloat($p.css("margin-bottom")) || 0);
            });
            height = Math.floor(viewHeight - $messages.offset().top - bottom) - 2;
        }
        $messages.css("height", height + "px");
        if ($messages.is(":visible")) {
            //safety net: shrink by any remaining page overflow (collapsed margins, late layout changes)
            var overflow = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            if (overflow > 0) $messages.css("height", (height - overflow) + "px");
        }
    }, 100);
}

function resize(printLayout) {
    if (!printLayout) {
        //size the messages once the top padding is set: it moves the messages box down
        setTimeout(function () {
            $("#report_body_container").css("padding-top", $("#bar_top").height() + 15);
            setMessageHeight();
        }, 200);
    }
    redrawDataTables();
}

function showNavMenu() {
    //Anchor the menu just below the fixed top bar. Under Bootstrap 5 the brand
    //(#nav_button) is a flex child of a stretched .container, so its height now
    //equals the full bar height; the old "2 * height" heuristic (tuned for BS3's
    //short brand) pushed the menu a whole bar-height below the bar into the content.
    var $button = $("#nav_button");
    var $bar = $("#bar_top");
    var barBottom = $bar.length ? $bar.offset().top + $bar.outerHeight() : $button.offset().top + $button.outerHeight();
    $("#nav_menu")
        .show()
        .css({
            position: "absolute",
            "z-index": "1040",
            left: $button.offset().left,
            top: barBottom
        });
}

function setProgressBarMessage(selector, progression, message, classname) {
    $(selector).css('width', progression + '%').attr('aria-valuenow', progression);
    $(selector).html(message);
    $(selector).removeClass("bg-danger bg-warning bg-success bg-primary bg-info");
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
    //Flatpickr renders its calendar on document.body (not clipped by the widget overflow), so no show/hide overflow handling is needed.
}
function initScrollReport() {
    //scroll
    $(window).unbind("scroll").scroll(function () {
        //back to top
        $('#back-to-top').toggleClass('show', $(this).scrollTop() > 50);
        //nav bar
        showHideNavbar();
        //alerts
        $('.sr-alert').each(function () { bootstrap.Alert.getOrCreateInstance(this).close(); });
    });

    $('#back-to-top').unbind("click").on("click", function () {
        (function (b) { if (b) { var t = bootstrap.Tooltip.getInstance(b); if (t) t.hide(); } })($('#back-to-top')[0]);
        window.scrollTo({ top: 0, behavior: 'smooth' });
        return false;
    });

    //instantiate the hover tooltip (never auto-shown)
    (function (b) { if (b) bootstrap.Tooltip.getOrCreateInstance(b); })($('#back-to-top')[0]);
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

//D3 categorical palettes (the former d3 v3 d3.scale.category10/20/20b/20c ranges), used by the ChartJS and Plotly views
var d3Palettes = {
    category10: ['#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', '#9467bd', '#8c564b', '#e377c2', '#7f7f7f', '#bcbd22', '#17becf'],
    category20: ['#1f77b4', '#aec7e8', '#ff7f0e', '#ffbb78', '#2ca02c', '#98df8a', '#d62728', '#ff9896', '#9467bd', '#c5b0d5', '#8c564b', '#c49c94', '#e377c2', '#f7b6d2', '#7f7f7f', '#c7c7c7', '#bcbd22', '#dbdb8d', '#17becf', '#9edae5'],
    category20b: ['#393b79', '#5254a3', '#6b6ecf', '#9c9ede', '#637939', '#8ca252', '#b5cf6b', '#cedb9c', '#8c6d31', '#bd9e39', '#e7ba52', '#e7cb94', '#843c39', '#ad494a', '#d6616b', '#e7969c', '#7b4173', '#a55194', '#ce6dbd', '#de9ed6'],
    category20c: ['#3182bd', '#6baed6', '#9ecae1', '#c6dbef', '#e6550d', '#fd8d3c', '#fdae6b', '#fdd0a2', '#31a354', '#74c476', '#a1d99b', '#c7e9c0', '#756bb1', '#9e9ac8', '#bcbddc', '#dadaeb', '#636363', '#969696', '#bdbdbd', '#d9d9d9']
};

//d3-format: keep the en-US separators (localized afterwards by String.valueFormat) and use an ASCII minus (d3-format 2+ defaults to U+2212)
if (window.d3 && d3.formatDefaultLocale) d3.formatDefaultLocale({ decimal: ".", thousands: ",", grouping: [3], currency: ["$", ""], minus: "-" });