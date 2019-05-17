
//nvd3 helpers
function nvd3TranslateAll(id) {
    var prefix = "";
    if (id != null) prefix = id + " ";
    nvd3Translate($(prefix + "g.nv-controlsWrap text.nv-legend-text"));
    nvd3TranslateAxis($(prefix + "g.legendWrap text.nv-legend-text"));
    nvd3TranslateAxis($(prefix + "g.legendWrap g.nv-series title"));
}

function nvd3Translate(texts) {
    for (var i = 0; i < texts.length; i++) $(texts[i]).text(nvd3TranslateText($(texts[i]).text()));
}

function nvd3TranslateTextAxis(text) {
    var suffix = "(right axis)";
    var index = text.lastIndexOf(suffix);
    if (index == -1) {
        index = text.lastIndexOf("...");
        var index2 = text.lastIndexOf("(");
        if (index != -1 && index2 != -1) {
            if (printLayout) return text.substr(0, index2);
            return (text.substr(0, index2) + nvd3TranslateText(suffix)).substring(0,index) + "...";
        }
    }
    if (index != -1 && index == text.length - suffix.length) return text.substr(0, index) + (printLayout ? "" : nvd3TranslateText(suffix));
    return text;
}

function nvd3TranslateAxis(texts) {
    for (var i = 0; i < texts.length; i++) $(texts[i]).text(nvd3TranslateTextAxis($(texts[i]).text()));
}

var nvd3Charts = [];
function nvd3UpdateCharts() {
    for (var i = 0; i < nvd3Charts.length; i++) nvd3Charts[i].update();
    nvd3TranslateAll();
    $.each($(".chartTitleWrap"), function (index, value) {
        //Ajust x of title
        if ($(this).parent().width()) $(this).attr('x', Math.max(1, ($(this).parent().width() - 7 * $(this).text().length) / 2)); 
    });
}
nv.utils.windowResize(nvd3UpdateCharts);

//Hide tooltips when scrolling
window.onscroll = function () {
    for (var i = 0; i < nvd3Charts.length; i++) {
        if (nvd3Charts[i].tooltip) nvd3Charts[i].tooltip.hidden(true);
        if (nvd3Charts[i].interactiveLayer && nvd3Charts[i].interactiveLayer.tooltip) nvd3Charts[i].interactiveLayer.tooltip.hidden(true);
    }
};

function nvd3TooltipGenerator(data, series, xvalformatter, y1formatter, y2formatter) {
		var header =xvalformatter(data.value);
		var headerhtml = '<thead><tr><td colspan=3><strong class="x-value">' + header + '</strong></td></tr></thead>';
		var bodyhtml = '<tbody>';
		data.series.forEach(function(dataSerie) {
			var formatter = y1formatter;
			series.forEach(function(serie) {
				 if (dataSerie.key.indexOf(serie.key) == 0 && serie.yAxis == 2) formatter = y2formatter;
			});	
            bodyhtml = bodyhtml + '<tr><td class="legend-color-guide"><div style="background-color: ' + dataSerie.color + ';"></div></td><td class="key">' + nvd3TranslateTextAxis(dataSerie.key) + '</td><td class="value">' + formatter(dataSerie.value) + '</td></tr>';
		});
		bodyhtml = bodyhtml+'</tbody>';
		return '<table>'+headerhtml+''+bodyhtml+'</table>';
};		

