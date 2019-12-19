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
            executeReport(nav, executionguid);
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
function initNavCells(executionguid, parentId) {
    var selector = "td:not([navigation=''])";
    if (parentId) selector = "#" + parentId + " " + selector;
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

function redrawDataTables() {
    setTimeout(function () { //redraw dt
        try {
            $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
            $.fn.dataTable.tables({ visible: true, api: true }).responsive.recalc();
        }
        catch (ex) { console.log(ex); }
    }, 200);
}


