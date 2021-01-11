/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/bootstrap/index.d.ts" />
/// <reference path="typings/main.d.ts" />
var _da;
var _daEditor;
var hasEditor;
var wnvPdfConverter;
//Muuri layout
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
        if (itemIndex > -1 && newItems.indexOf(currentItems[itemIndex]) == -1) {
            newItems.push(currentItems[itemIndex]);
        }
    }
    if (layout.length == newItems.length)
        grid.sort(newItems, { layout: 'instant' });
    else
        grid.layout(true);
}
function redrawDashboard() {
    $("#nav_popupmenu").css("opacity", "0");
    if (_da._pendingRequest <= 0) {
        if ($(".item").css("opacity") == "1")
            $(".item,.group-name,h1").css("opacity", "0.4");
        setTimeout(function () {
            redrawNVD3Charts();
            redrawDataTables();
            _da.reorderItems(true);
            if ($(".item").css("opacity") != "1")
                $(".item,.group-name,h1,#nav_popupmenu").css("opacity", "0.6");
        }, 500);
        setTimeout(function () {
            $(".item,.group-name,h1,#nav_popupmenu").css("opacity", "1");
            _da.enableControls();
        }, 900);
    }
}
var SWIDashboard = /** @class */ (function () {
    function SWIDashboard() {
        this._dashboards = [];
        this._ids = [];
        this._gridOrders = [];
        this._grids = [];
        this._gridsById = [];
        this._refreshTimers = [];
        this._pendingRequest = 0;
    }
    SWIDashboard.prototype.reorderItems = function (init) {
        if (!_da || !_da._dashboard)
            return;
        if (init)
            _da._gridsById = []; //Force rebuild of grids
        _da._grids = [];
        $('.grid' + _da._dashboard.guid).each(function () {
            var gridId = $(this).attr("id");
            var grid = _da._gridsById[gridId];
            if (!grid) {
                grid = new Muuri('#' + gridId, {
                    dragEnabled: hasEditor && _da._dashboard.editable,
                    layout: {
                        fillGaps: true,
                    },
                    dragStartPredicate: {
                        distance: 10,
                        delay: 80
                    },
                    dragSort: true,
                });
                _da._gridsById[gridId] = grid;
                if (hasEditor && _da._dashboard.editable) {
                    _daEditor.initGridItemOrder(grid);
                }
            }
            _da._grids.push(grid);
            var gridOrder = _da._gridOrders[gridId];
            if (gridOrder) {
                loadLayout(grid, gridOrder);
            }
            else {
                grid.layout(true);
            }
        });
    };
    SWIDashboard.prototype.enableControls = function () {
        /*        var editDashboard = $("#dashboards-nav-item");
                var addWidgets = $("#add-widget-nav-item");
                var exportDashboard = $("#export-nav-item");
        
                SWIUtil.ShowHideControl(editDashboard, hasEditor && _main._profile.dashboardfolders.length > 0);
                SWIUtil.EnableButton(editDashboard, hasEditor && _main._profile.dashboardfolders.length > 0);
                SWIUtil.ShowHideControl(addWidgets, hasEditor);
                SWIUtil.EnableButton(addWidgets, hasEditor && _da._dashboard && _da._dashboard.editable);
                SWIUtil.ShowHideControl(exportDashboard, true);
                SWIUtil.EnableButton(exportDashboard, _da._dashboard);
        
                if (!_da._dashboard) {
                    $("#report-body").empty();
                    _da._lastGUID = null;
                    _da._dashboard = null;
                    _main._profile.lastdashboard = null;
                    $("#dashboard-name").html("");
                }
                else {
                    $("#dashboard-name").html(_da._dashboard.name);
                }*/
    };
    SWIDashboard.prototype.handleDashboardResult = function (data) {
        var panel = $("#" + data.itemguid);
        var panelHeader = panel.children(".panel-heading");
        //Set description and hyper link
        var nameLink = panelHeader.find("a");
        nameLink.attr("title", data.description);
        //Set content
        var panelBody = panel.children(".panel-body");
        panelBody.empty();
        panelBody.html(data.content);
        //Dynamic properties
        if (data.dynamic) {
            var newIcon = $(data.content).find("#new-widget-icon").val();
            if (newIcon) {
                var spanIcon = panelHeader.children(".glyphicon");
                spanIcon.removeClass();
                spanIcon.addClass(newIcon);
            }
            var newColor = $(data.content).find("#new-widget-color").val();
            if (newColor) {
                panel.removeClass();
                panel.addClass("item panel panel-" + newColor);
            }
            var newName = $(data.content).find("#new-widget-name").val();
            if (newName) {
                panelHeader.find("a").text(" " + newName);
            }
        }
        panelHeader.children(".fa-spinner").hide();
        //Refresh button
        $("#rb" + data.itemguid).attr("title", data.lastexec);
        //Auto-refresh
        if (data.refresh > 0)
            _da._refreshTimers[data.itemguid] = setTimeout(function () { _da.refreshDashboardItem(data.dashboardguid, data.itemguid, false, true); }, 1000 * data.refresh);
        initNavCells(data.executionguid, "#" + data.itemguid);
        initRestrictions("#" + data.itemguid);
        //        initDashboardRestrictions("#" + data.itemguid);
    };
    SWIDashboard.prototype.refreshDashboardItem = function (guid, itemguid, force, forTimer) {
        if (!forTimer)
            _da._pendingRequest++;
        clearTimeout(_da._refreshTimers[itemguid]);
        $("#nav_popupmenu").css("opacity", "0");
        _gateway.GetDashboardItemResult(guid, itemguid, force, exportFormat, function (data) {
            _da.handleDashboardResult(data);
            if (!forTimer) {
                _da._pendingRequest--;
                //Redraw...
                redrawDashboard();
            }
        });
    };
    SWIDashboard.prototype.initDashboard = function (guid) {
        var dashboard = _da._dashboards[guid];
        if (!dashboard)
            return;
        $("#report-body").empty();
        _da._lastGUID = guid;
        _da._dashboard = dashboard;
        _main._profile.lastdashboard = _da._lastGUID;
        _main.toggleFoldersReport(true);
        $waitDialog.modal();
        _gateway.StartReportExecution(guid, function (data) {
            //         reportInit(
            //            execute(
            $("#report-body").html(data.content);
            initRestrictions("#report-body");
            //            initDashboardRestrictions("#main-dashboard");
            $waitDialog.modal('hide');
        });
        /*
                SWIUtil.ShowHideControl($("#add-widget-nav-item"), false);
                SWIUtil.ShowHideControl($("#dashboards-nav-item"), false);
                SWIUtil.EnableButton($("#export-nav-item"), false);
        
                //re-init order
                $('.grid' + guid).each(function () {
                    var gridId = $(this).attr("id");
                    _da._gridOrders[gridId] = null;
                    _da._gridsById[gridId] = null;
                });
        
                _gateway.GetDashboardItems(guid, _main._exporting, function (data) {
                    var content = $("#main-dashboard");
                    _da.enableControls();
        
                    var currentGroup = "";
                    var grid: JQuery = null;
                    for (var i = 0; i < data.length; i++) {
                        var item = data[i];
        
                        if (currentGroup != item.GroupName || !grid) {
                            if (grid) content.append($("<hr class='group-name' style='margin:5px 2px'>"));
                            //Add current grid
                            grid = $("<div class='grid grid" + dashboard.guid + "'>");
                            grid.attr("id", "g" + dashboard.guid + "-" + item.GroupOrder);
                            grid.attr("group-name", item.GroupName);
                            grid.attr("group-order", item.GroupOrder);
                            _da._gridsById[grid.attr("id")] = null;
                            if (hasEditor && _da._dashboards[guid].editable) {
                                _daEditor.initGrid(grid);
                            }
        
                            if (item.GroupName != "") {
                                //Group name
                                var groupSpan = $("<span for='gn" + item.GUID + "'>").text(item.DisplayGroupName.startsWith("_") ? "" : item.DisplayGroupName).attr("group-name", item.GroupName).addClass("group-name").css("opacity", "0.2");
                                var groupInput = $("<input type='text' id='gn" + item.GUID + "' style='width:250px;' hidden>");
                                var groupDrag = $("<h3 style='margin:0px 5px'>").append(groupSpan);
                                groupDrag.attr("group-order", item.GroupOrder)
                                content.append(groupDrag);
                                content.append(groupInput);
        
                                if (hasEditor && _da._dashboards[guid].editable) {
                                    _daEditor.initGridGroupName(groupSpan, groupInput, groupDrag);
                                }
                            }
                            content.append(grid);
        
                            currentGroup = item.GroupName;
                        }
        
                        //Dashboard item
                        var panel = $("<div class='item panel panel-" + item.Color + "' style='opacity: 0.2;page-break-inside:avoid;'>");
                        panel.attr("id", item.GUID);
                        panel.attr("did", dashboard.guid);
                        var panelHeader = $("<div class='panel-heading text-left' style='padding-right:2px;'>");
                        panel.append(panelHeader);
                        panelHeader.append($("<span class='glyphicon glyphicon-" + item.Icon + "'>"));
        
                        if (!item.DisplayName) item.DisplayName = "";
                        var nameLink = $("<a>)").text(" " + item.DisplayName);
                        var panelName = $("<h3 class='panel-title' style='display:inline'>").append(nameLink);
        
                        panelHeader.append(panelName);
                        panelHeader.append($("<i class='fa fa-spinner fa-spin fa-sm fa-fw'></i>"));
        
                        var refreshButton = $("<button class='btn btn-sm btn-info' type='button' style='margin-left:2px;margin-right:0px;padding:0px 6px;'><span class='glyphicon glyphicon-refresh'></span></button>");
                        var panelButtons = $("<div style='display:none;float:right;white-space:nowrap;'>");
        
                        refreshButton.attr("id", "rb" + item.GUID);
        
                        panelButtons.append(refreshButton);
                        if (hasEditor && dashboard.editable) {
                            var buttons = _daEditor.getEditButtons();
                            for (var j = 0; j < buttons.length; j++) {
                                panelButtons.append(buttons[j]);
                            }
                        }
        
                        panelHeader.append(panelButtons);
        
                        var panelBody = $("<div class='panel-body'>");
                        panel.append(panelBody);
        
                        panelBody.append($("<i class='fa fa-spinner fa-spin fa-2x fa-fw'></i>"));
                        panelBody.append($("<h4 style='display:inline'></h4>").html(SWIUtil.tr("Processing") + "..."));
        
                        _da.refreshDashboardItem(guid, item.GUID, false, false);
        
                        //Size
                        if (item.Width > 0) panel.width(item.Width);
                        if (item.Height > 0) panel.height(item.Height);
                        panel.css("overflow", "auto");
        
                        //Panel buttons
                        if (!_main._exporting) {
                            panelHeader
                                .mouseenter(function (e) {
                                    var panelHeading = $(this).closest('.panel-heading');
                                    if (!panelHeading.children(".fa-spinner").is(":visible")) {
                                        var tl = getTopLeft($(this)[0]);
                                        var buttons = $(this).children("div");
                                        buttons.css("position", "absolute");
                                        buttons.css("left", tl[0] + $(this).width() - Math.max(buttons.width(), buttons.height()) + 15);
                                        buttons.css("top", tl[1] + 10);
                                        buttons.show();
                                    }
        
                                })
                                .mouseleave(function () {
                                    $(this).children("div").hide();
                                });
        
                            //Refresh item
                            refreshButton.unbind("click").on("click", function (e) {
                                SWIUtil.HideMessages();
                                var dashboardGuid = $(this).closest('.panel').attr('did');
                                var itemGuid = $(this).closest('.panel').attr('id');
        
                                var panelHeading = $(this).closest('.panel-heading');
                                panelHeading.children(".fa-spinner").show();
        
                                _da.refreshDashboardItem(dashboardGuid, itemGuid, true, false);
                            });
                        }
        
                        grid.append(panel);
                    } //for
        
                    if (_da._dashboard && guid == _da._dashboard.guid) _da.reorderItems(false);
        
                    _main.refreshMenu();
                    _da.enableControls();
                });
                */
        _da.enableControls();
    };
    SWIDashboard.prototype.init = function () {
        _da = this;
        _da._dashboard = null;
        _main._profile.dashboards.forEach(function (value) {
            _da._dashboards[value.guid] = value;
        });
        if (!_da._lastGUID)
            _da._lastGUID = _main._profile.lastdashboard;
        if (_da._lastGUID)
            _da._dashboard = _da._dashboards[_da._lastGUID];
        if (_daEditor)
            _daEditor.init();
        //Export
        $("#export-nav-item").unbind("click").on("click", function () {
            SWIUtil.HideMessages();
            $("#export-title").val(_da._dashboard.name);
            var select = $("#export-format");
            select.unbind("change").selectpicker("destroy").empty();
            select.append(SWIUtil.GetOption("htmlprint", SWIUtil.tr("HTML Print"), ""));
            select.append(SWIUtil.GetOption("pdf", SWIUtil.tr("PDF"), ""));
            select.append(SWIUtil.GetOption("pdflandscape", SWIUtil.tr("PDF Landscape"), ""));
            select.append(SWIUtil.GetOption("excel", SWIUtil.tr("Excel"), ""));
            select.selectpicker("refresh");
            SWIUtil.InitNumericInput();
            $("#dashboard-export").unbind("click").on("click", function () {
                SWIUtil.HideMessages();
                if (_da._lastGUID)
                    _gateway.ExportDashboards(_da._lastGUID, $("#export-format").val(), $("#export-title").val(), $("#export-delay").val());
            });
            $("#export-dialog").modal();
        });
        //Export end
        $(document).ajaxStop(function () {
        });
        if (hasEditor)
            _daEditor.initMenu();
        _da.enableControls();
    };
    return SWIDashboard;
}());
//# sourceMappingURL=swi-dashboard.js.map