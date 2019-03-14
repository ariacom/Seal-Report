/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/bootstrap/index.d.ts" />
/// <reference path="typings/jstree/jstree.d.ts" />
/// <reference path="typings/main.d.ts" />
var _da;
var _dashboardEditor;
var hasEditor;
//Default units
var WidgetWidthUnit = 200;
var WidgetHeightUnit = 140;
function serializeLayout(grid) {
    var itemIds = grid.getItems().map(function (item) {
        return item.getElement().getAttribute('id');
    });
    return JSON.stringify(itemIds);
}
function loadLayout(grid, serializedLayout) {
    var layout = JSON.parse(serializedLayout);
    var currentItems = grid.getItems();
    var currentItemIds = currentItems.map(function (item) {
        return item.getElement().getAttribute('id');
    });
    var newItems = [];
    var itemId;
    var itemIndex;
    for (var i = 0; i < layout.length; i++) {
        itemId = layout[i];
        itemIndex = currentItemIds.indexOf(itemId);
        if (itemIndex > -1) {
            newItems.push(currentItems[itemIndex]);
        }
    }
    if (layout.length == newItems.length)
        grid.sort(newItems, { layout: 'instant' });
    else
        grid.layout(true);
}
var SWIDashboard = (function () {
    function SWIDashboard() {
        this._dashboards = [];
        this._gridOrders = [];
    }
    SWIDashboard.prototype.reorderItems = function (id) {
        $('.grid' + id).each(function (index, element) {
            var gridId = $(this).attr("id");
            var grid = new Muuri('#' + gridId, {
                dragEnabled: true,
                layoutOnInit: false,
                dragStartPredicate: {
                    distance: 10,
                    delay: 80
                }
            });
            var gridOrder = _da._gridOrders[gridId];
            if (gridOrder) {
                loadLayout(grid, gridOrder);
            }
            else {
                grid.layout(true);
            }
            grid.on('dragReleaseEnd', function (item) {
                var items = grid.getItems();
                var initalSort = _da._gridOrders[$(item._element).parent().attr("id")];
                var sort = ""; //TODO changer la serialisation
                var sortIds = [];
                for (var i = 0; i < items.length; i++) {
                    var item = items[i];
                    sortIds.push($(item._element).attr("id"));
                    sort += $(item._element).attr("id") + ";" + i + "\n";
                }
                if (JSON.stringify(sortIds) != initalSort) {
                    _da._gridOrders[$(item._element).parent().attr("id")] = serializeLayout(grid);
                    _gateway.SaveDashboardItemsOrder(_da._dashboard.GUID, sort, null);
                }
            });
        });
        /*
        var dragSortOptions = {
            action: 'swap',
            threshold: 50
        };

        var grid = new Muuri('.' + gridId, {
            dragEnabled: true,
            dragStartPredicate: function (item, event) {
                // Prevent first item from being dragged.
                if (grid.getItems().indexOf(item) === 0) {
                    // return false;
                }
                // For other items use the default drag start predicate.
                return Muuri.ItemDrag.defaultStartPredicate(item, event);
            },http://localhost:17178/seal/Main#4264c077-7cab-43d5-a27b-acff5603e6be
            dragSortPredicate: function (item) {
                var result = Muuri.ItemDrag.defaultSortPredicate(item, dragSortOptions);
                return result && result.index === 0 ? false : result;
            }
        });*/
    };
    SWIDashboard.prototype.enableControls = function () {
        var canEditDashboard = _da._dashboard && ((_main._profile.role == 1 /*Private Designer*/ && _da._dashboard.IsPrivate) || _main._profile.role == 2 /*Public Designer*/);
        SWIUtil.EnableButton($("#dashboard-add-widget"), canEditDashboard);
    };
    SWIDashboard.prototype.handleDashboardResult = function (data) {
        var panel = $("#" + data.itemguid);
        var panelHeader = panel.children(".panel-heading");
        //Set description and hyper link
        var nameLink = panelHeader.find("a");
        nameLink.attr("title", data.description);
        if (data.path) {
            nameLink.attr("path", data.path);
            nameLink.unbind('click').on("click", function (e) {
                _gateway.ExecuteReport($(e.currentTarget).attr("path"), false, null, null);
            });
        }
        //Set content
        var panelBody = panel.children(".panel-body");
        panelBody.empty();
        panelBody.html(data.content);
        //Dynamic properties
        if (panel.attr("w-dynamic") == "true") {
            var newIcon = $(data.content).children("#new-widget-icon").val();
            if (newIcon) {
                var spanIcon = panelHeader.children(".glyphicon");
                spanIcon.removeClass();
                spanIcon.addClass("glyphicon glyphicon-" + newIcon);
            }
            var newColor = $(data.content).children("#new-widget-color").val();
            if (newColor) {
                panel.removeClass();
                panel.addClass("item panel panel-" + newColor);
            }
            var newName = $(data.content).children("#new-widget-name").val();
            if (newName) {
                panelHeader.find("a").text(" " + newName);
            }
        }
        panelHeader.children(".fa-spinner").hide();
        //Refresh button
        $("#rb" + data.itemguid).attr("title", data.lastexec);
    };
    SWIDashboard.prototype.initDashboardItems = function (guid) {
        var dashboard = _da._dashboards[guid];
        if (dashboard) {
            //re-init order
            $('.grid' + guid).each(function (index, element) {
                var gridId = $(this).attr("id");
                _da._gridOrders[gridId] = null;
            });
        }
        _gateway.GetDashboardItems(guid, function (data) {
            var content = $("#" + guid);
            content.empty();
            var currentGroup = "";
            var grid = null;
            for (var i = 0; i < data.length; i++) {
                var item = data[i];
                if (currentGroup != item.GroupName || !grid) {
                    content.append($("<hr style='margin: 5px 2px'>"));
                    if (item.GroupName != "") {
                        var groupSpan = $("<span class2='label label-default' for='gn" + item.GUID + "'>").text(item.GroupName);
                        var groupInput = $("<input type='text' id='gn" + item.GUID + "' style='width:250px;' hidden>");
                        groupSpan.click(function () {
                            "use strict";
                            $(this).hide();
                            $('#' + $(this).attr('for'))
                                .val($(this).text())
                                .toggleClass("form-control")
                                .show().focus();
                        });
                        groupInput.blur(function () {
                            "use strict";
                            $(this)
                                .hide()
                                .toggleClass("form-control");
                            var myid = (this).id;
                            var span = $('span[for=' + myid + ']');
                            if (span.text() != $(this).val()) {
                                _gateway.UpdateDashboardItemsGroupName(guid, span.text(), $(this).val(), function (data) {
                                    _da.init();
                                });
                            }
                            span.text($(this).val()).show();
                        });
                        var groupDrag = $("<div style='display:inline'>").append($("<h4 style='margin:0px 5px'>").append(groupSpan));
                        content.append(groupDrag);
                        content.append(groupInput);
                        //Drag and drop for group name
                        groupDrag.on("dragstart", function (e) {
                            var aa = $(this).attr("did");
                            // e.dataTransfer.setData("text", e.target.id);
                        });
                        groupDrag.prop("draggable", "true");
                        groupDrag.on("dragover", function (e) {
                            e.preventDefault();
                        });
                        groupDrag.on("drop", function (e) {
                            var bb = $(this).attr("did");
                            /*                            var sourceid = _da._dashboard.GUID;
                                                        var did = $(this).children("a").attr("did");
                                                        _gateway.SwapDashboardOrder(_da._lastGUID, did, function (data) {
                                                            _da.init();
                                                        });*/
                        });
                        //content.append($("<h4 style='margin: 5px 2px'>").text(item.GroupName));
                    }
                    grid = $("<div class='grid grid" + dashboard.GUID + "' id='g" + dashboard.GUID + i + "'>");
                    content.append(grid);
                    currentGroup = item.GroupName;
                }
                var panel = $("<div class='item panel panel-" + item.Color + "' id='" + item.GUID + "'>");
                panel.attr("widgetguid", item.WidgetGUID);
                panel.attr("w-name", item.Name);
                panel.attr("w-icon", item.Icon);
                panel.attr("w-group", item.GroupName);
                panel.attr("w-group-order", item.GroupOrder);
                panel.attr("w-color", item.Color);
                panel.attr("w-width", item.Width);
                panel.attr("w-height", item.Height);
                panel.attr("w-dynamic", item.Dynamic);
                var panelHeader = $("<div class='panel-heading text-left' style='padding-right:2px;'>");
                panel.append(panelHeader);
                panelHeader.append($("<span class='glyphicon glyphicon-" + item.Icon + "'>"));
                var nameLink = $("<a>)").text(" " + item.Name);
                var panelName = $("<h3 class='panel-title' style='display:inline'>").append(nameLink);
                panelHeader.append(panelName);
                panelHeader.append($("<i class='fa fa-spinner fa-spin fa-sm fa-fw'></i>"));
                //var closeButton = $("<button class='close' aria-label='Close' type='button' data-dismiss='modal'><span aria-hidden='true'>×</span></button>");
                var refreshButton = $("<button class='btn btn-sm btn-info' type='button' style='margin-left:2px;margin-right:0px;padding:0px 6px;'><span class='glyphicon glyphicon-refresh'></span></button>");
                var panelButtons = $("<div style='display:none;float:right;'>");
                refreshButton.attr("id", "rb" + item.GUID);
                refreshButton.attr("title", "Refresh widget data");
                panelButtons.append(refreshButton);
                if (hasEditor && dashboard.Editable) {
                    var buttons = _dashboardEditor.getEditButtons();
                    for (var j = 0; j < buttons.length; j++) {
                        panelButtons.append(buttons[j]);
                    }
                }
                panelHeader.append(panelButtons);
                var panelBody = $("<div class='panel-body text-center'>");
                panel.append(panelBody);
                panelBody.append($("<i class='fa fa-spinner fa-spin fa-2x fa-fw'></i><h4 style='display:inline'>Processing...</h4>"));
                _gateway.GetDashboardResult(guid, item.GUID, function (data) {
                    _da.handleDashboardResult(data);
                });
                var guid2 = dashboard.GUID;
                //Size
                panel.width(Math.floor(item.Width * WidgetWidthUnit));
                panel.height(Math.floor(item.Height * WidgetHeightUnit));
                //Panel buttons
                panelHeader
                    .mouseenter(function (e) {
                    var rect = $(this)[0].getBoundingClientRect();
                    var obj = $(this)[0];
                    var curleft = 0;
                    var curtop = 0;
                    if (obj.offsetParent) {
                        do {
                            curleft += obj.offsetLeft;
                            curtop += obj.offsetTop;
                        } while (obj == obj.offsetParent);
                    }
                    //return [curleft,curtop];
                    var buttons = $(this).children("div");
                    buttons.css("position", "absolute");
                    buttons.css("left", curtop + $(this).width() - buttons.width() + 15);
                    buttons.css("top", curleft + 10);
                    buttons.show();
                })
                    .mouseleave(function () {
                    $(this).children("div").hide();
                });
                //Refresh item
                refreshButton.unbind('click').on("click", function (e) {
                    setTimeout(function () {
                        _da.reorderItems(guid2); //evite bug, sur le 2 items d'1 deucième tab...à debugger
                    }, 200);
                    var itemGuid = $(this).closest('.panel').attr('id');
                    var panelHeading = $(this).closest('.panel-heading');
                    panelHeading.children(".fa-spinner").show();
                    _gateway.GetDashboardResult(guid2, itemGuid, function (data) {
                        _da.handleDashboardResult(data);
                        _da.reorderItems(guid2); //evite bug, sur le 2 items d'1 deucième tab...à debugger
                    });
                });
                grid.append(panel);
            } //for
            _da.reorderItems(guid);
        });
    };
    SWIDashboard.prototype.init = function () {
        _da = this;
        _da._dashboard = null;
        if (!_da._lastGUID)
            _da._lastGUID = _main._profile.dashboard;
        if (_dashboardEditor)
            _dashboardEditor.init();
        $waitDialog.modal();
        _gateway.GetUserDashboards(function (data) {
            _da._dashboards = [];
            $("#menu-dashboard").empty();
            $("#content-dashboard").empty();
            for (var i = 0; i < data.length; i++) {
                var dashboard = data[i];
                _da._dashboards[dashboard.GUID] = dashboard;
                var menu = $("<a data-toggle='pill' href='#" + dashboard.GUID + "' did='" + dashboard.GUID + "'>");
                if (dashboard.IsPrivate)
                    menu.addClass("private");
                menu.text(dashboard.Name);
                var li = $("<li>");
                //Drag and drop for title
                li.on("dragstart", function (e) {
                    _da._lastGUID = $(this).children("a").attr("did");
                    // e.dataTransfer.setData("text", e.target.id);
                });
                li.prop("draggable", "true");
                li.on("dragover", function (e) {
                    e.preventDefault();
                });
                li.on("drop", function (e) {
                    var sourceid = _da._dashboard.GUID;
                    var did = $(this).children("a").attr("did");
                    _gateway.SwapDashboardOrder(_da._lastGUID, did, function (data) {
                        _da.init();
                    });
                });
                var isActive = false;
                if (_da._lastGUID && dashboard.GUID == _da._lastGUID)
                    isActive = true;
                else if (!_da._lastGUID && i == 0)
                    isActive = true;
                if (isActive)
                    li.addClass("active");
                $("#menu-dashboard").append(li.append(menu));
                //Menu click
                menu.unbind('click').click(function (e) {
                    var id = $(this).attr("did");
                    _da._lastGUID = id;
                    _da._dashboard = _da._dashboards[id];
                    _da.enableControls();
                    _gateway.SetLastDashboard(_da._lastGUID, function (data) {
                    });
                    //redraw nvd3 charts
                    /*                    setTimeout(function () { nvd3UpdateCharts(); }, 200);
                                        setTimeout(function () { //redraw dt
                                            $($.fn.dataTable.tables(true)).DataTable().columns.adjust().responsive.recalc();
                                        }, 200);
                                        */
                    // $("#" + id).hide();
                    setTimeout(function () {
                        // $("#" + id).show();
                        nvd3UpdateCharts(); //TODO faire update seulement sur le chart visible
                        $($.fn.dataTable.tables(true)).DataTable().columns.adjust().responsive.recalc();
                        _da.reorderItems(id);
                        //setTimeout(function () {
                        //}, 100);
                    }, 200);
                });
                //TODO: gérer le resize de la window
                var content = $("<div id='" + dashboard.GUID + "' class='tab-pane fade'>");
                $("#content-dashboard").append(content);
                if (isActive)
                    content.addClass("in active");
                _da.initDashboardItems(dashboard.GUID);
            }
            if (_da._lastGUID)
                _da._dashboard = _da._dashboards[_da._lastGUID];
            if (!_da._dashboard && data.length > 0) {
                _da._dashboard = data[0];
            }
            //Manage
            $("#dashboards-nav-item").unbind('click').on("click", function (e) {
                _gateway.GetDashboards(function (data) {
                    var select = $("#dashboard-user");
                    select.unbind("change").selectpicker("destroy").empty();
                    for (var j = 0; j < data.length; j++) {
                        var pubDashboard = data[j];
                        select.append(SWIUtil.GetOption(pubDashboard.GUID, pubDashboard.FullName, ""));
                    }
                    select.selectpicker({
                        "liveSearch": true
                    });
                    //Add
                    SWIUtil.ShowHideControl($("#dashboard-add").parent(), data.length > 0);
                    $("#dashboard-add").unbind('click').on("click", function (e) {
                        if (!$("#dashboard-user").val())
                            return;
                        $("#dashboard-dialog").modal('hide');
                        _gateway.AddDashboard($("#dashboard-user").val(), function (data) {
                            _da._lastGUID = null;
                            _da.init();
                            SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The dashboards has been added to your view"), 5000);
                        });
                    });
                    //Remove
                    SWIUtil.ShowHideControl($("#dashboard-remove").parent(), _da._dashboard);
                    if (_da._dashboard) {
                        $("#dashboard-remove")
                            .text("'" + _da._dashboard.FullName + "' : " + SWIUtil.tr("Remove the dashboard from your view"))
                            .unbind('click').on("click", function (e) {
                            $("#dashboard-dialog").modal('hide');
                            _gateway.RemoveDashboard(_da._dashboard.GUID, function (data) {
                                _da._lastGUID = null;
                                _da.init();
                                SWIUtil.ShowMessage("alert-success", SWIUtil.tr("The dashboard has been removed from your view"), 5000);
                            });
                        });
                    }
                    if (hasEditor && _main._profile.role > 0) {
                        _dashboardEditor.initDashboardMenu();
                    }
                    $("#dashboard-dialog").modal();
                });
            });
            if (hasEditor && _main._profile.role > 0) {
                _dashboardEditor.initMenu();
            }
            _da.enableControls();
        });
        _da.enableControls();
        $waitDialog.modal('hide');
    };
    return SWIDashboard;
}());
//# sourceMappingURL=swi-dashboard.js.map