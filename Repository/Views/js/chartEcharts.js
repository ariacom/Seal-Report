// Seal Report - Apache ECharts helper
// Keeps track of all ECharts instances rendered in the page so they can be
// resized when the window, a tab or a model menu changes.

var echartsCharts = [];

// Register a chart instance (called from the ChartEcharts view once a chart is built)
function registerEChartsChart(chart) {
    if (chart) echartsCharts.push(chart);
}

// Resize every registered chart
function resizeEChartsCharts() {
    for (var i = 0; i < echartsCharts.length; i++) {
        try { echartsCharts[i].resize(); } catch (e) { }
    }
}

window.addEventListener('resize', function () {
    resizeEChartsCharts();
});
