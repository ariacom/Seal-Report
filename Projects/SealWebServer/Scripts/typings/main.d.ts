// Type definitions for Seal
interface JQuery {
    selectpicker(): JQuery;
    selectpicker(options: any): JQuery;
    selectpicker(options: any, options2: any): JQuery;

    autocomplete(options: any): JQuery;
    datepicker(options: any): JQuery;
    multipleSelect(options: any): JQuery;

    dataTable(options: any): JQuery;
    dataTable(): any;
}


interface ReportEditorInterface {
    init(): void;
}