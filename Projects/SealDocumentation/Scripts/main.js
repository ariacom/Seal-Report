function executeReport(path) {
    var server = "https://sealreport.org/demo/"
    $.post(server + "SWILogin", {
        user: "", // The user name
        password: "" // The password
    })
        .done(function (data) {
            var f = $('<form method="post" target="_blank" />').appendTo('body');
            f.attr('action', server + "SWExecuteReport");
            f.append($('<input />').attr('name', 'path').attr('value', path));  //the report path, if empty the report definition must be specified
            f.children('input').attr('type', 'hidden');
            f.submit();
        });
    return false;
}


$(document).ready(function () {
    SyntaxHighlighter.defaults['toolbar'] = false;
    SyntaxHighlighter.all();

    $(".live-sample").unbind('click').on("click", function (e) {
        executeReport($(this).text().replace("Live Sample: ", "/Samples/") + ".srex");
    });
});
