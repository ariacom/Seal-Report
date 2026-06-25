// Type definitions for Seal

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

