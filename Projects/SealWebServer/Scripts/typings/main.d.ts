// Type definitions for Seal
interface JQuery {
    selectpicker(): JQuery;
    selectpicker(options: any): JQuery;
    selectpicker(options: any, options2: any): JQuery;

    autocomplete(options: any): JQuery;
    datetimepicker(options: any): JQuery;

    dataTable(options: any): JQuery;
    dataTable(): any;
}


interface ReportEditorInterface {
    init(): void;
}

interface DashboardEditorInterface {
    init(): void;
    initGrid(grid: any);
    initGridGroupName(groupSpan: any, groupInput: any, groupDrag: any);
    initGridItemOrder(grid: any);
    initMenu();
    initDashboardMenu();
    getEditButtons();
}
