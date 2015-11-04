
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

//nvd3 helpers
function nvd3Translate(texts) {
    for (var i = 0; i < texts.length; i++) $(texts[i]).text(nvd3TranslateText($(texts[i]).text()));
}

function nvd3TranslateTextAxis(text) {
    var suffix = "(right axis)";
    var index = text.lastIndexOf(suffix);
    if (index != -1 && index == text.length - suffix.length) return text.substr(0, index) + nvd3TranslateText(suffix);
    return text;
}
function nvd3TranslateAxis(texts) {
    for (var i = 0; i < texts.length; i++) $(texts[i]).text(nvd3TranslateTextAxis($(texts[i]).text()));
}

var nvd3Charts = [];
function nvd3UpdateCharts() {
    for (var i = 0; i < nvd3Charts.length; i++) nvd3Charts[i].update();
    nvd3Translate($("g.nv-controlsWrap text.nv-legend-text"));
    nvd3TranslateAxis($("g.legendWrap text.nv-legend-text"));
}
nv.utils.windowResize(nvd3UpdateCharts);

