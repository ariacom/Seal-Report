// Type definitions for Seal

// jQuery global declarations ($, jQuery, JQuery namespace), referenced explicitly: TypeScript 7 no longer resolves them through the relative reference types directive of the DataTables typings
/// <reference path="jquery/JQueryStatic.d.ts" />
/// <reference path="jquery/JQuery.d.ts" />
/// <reference path="jquery/misc.d.ts" />
/// <reference path="jquery/legacy.d.ts" />

interface MenuItem {
    name: string;
    path?: string;
    viewGUID?: string;
    outputGUID?: string;
}

interface AiPanel {
    clearConversation(): void;
    setFavorite(state: boolean): void;
    setFavorites(items: MenuItem[]): void;
    setRecents(items: MenuItem[]): void;
    reset(): void;
}

interface Window {
    aiPanel: AiPanel;
}

interface JQuery {
    selectpicker(): JQuery;
    selectpicker(options: any): JQuery;
    selectpicker(options: any, options2: any): JQuery;

    autocomplete(options: any): JQuery;
    datetimepicker(options: any): JQuery;

    dataTable(options: any): JQuery;
    dataTable(): any;
}

