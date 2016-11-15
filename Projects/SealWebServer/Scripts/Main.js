/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/bootstrap/bootstrap.d.ts" />
/// <reference path="typings/main.d.ts" />
var sealServer = "/";
var connected = false;
$(document).ready(function () {
    $.post(sealServer + "SWIGetVersions")
        .done(function (data) {
        if (!data.error) {
            $("#brand-id").attr("title", "Version " + data.SRVersion);
        }
    });
    /*
        $.post(sealServer + "SWIGetUserProfile")
            .done(function (data) {
                if (!data.error) {
                    $("#get_up_label").html("SWIGetUserProfile Done.<br>" + JSON.stringify(data));
                }
                else {
                    $("#get_up_label").text("Error: " + data.error);
                }
            });
            */
    $.post(sealServer + "SWIGetFolders", {
        path: "\\"
    })
        .done(function (data) {
        if (!data.error) {
            $("#folders_label").html("SWIGetFolders Done: " + data.folders.length + " subfolders<br>" + JSON.stringify(data));
        }
        else {
            $("#folders_label").text("Error: " + data.error);
        }
    });
    /*
var treeData = [
    { "id": "ajson1", "parent": "#", "text": "Reports", "state": { "opened": true } },
    { "id": "ajson2", "parent": "ajson1", "text": "Product Reports" },
    { "id": "ajson3", "parent": "ajson1", "text": "Order Reports", type: "default" },
    { "id": "ajson4", "parent": "ajson2", "text": "Work", },
    { "id": "ajson5", "parent": "ajson2", "text": "Samples", },
];
*/
    var treeData = [];
    treeData[0] = { "id": "ajson1", "parent": "#", "text": "Reports", "state": { "opened": true } };
    treeData[1] = { "id": "ajson2", "parent": "ajson1", "text": "Product Reports" };
    $("#folder-tree").jstree({
        core: {
            "animation": 0,
            "themes": { "stripes": true },
            check_callback: function (operation, node, node_parent, node_position, more) {
                if (operation == 'move_node') {
                    return false;
                }
            },
            'data': treeData
        },
        /*
        dnd: {
            is_draggable: function (node) {
                if (node[0].type == 'leaf') {
                  //  alert('this type is not draggable');
                    return true;
                }
                return false;
            }
        },*/
        types: {
            "default": {
                "icon": "fa fa-folder-o"
            },
            "leaf": {
                "icon": "fa fa-cube"
            }
        },
        plugins: ["types", "wholerow"]
    });
});
