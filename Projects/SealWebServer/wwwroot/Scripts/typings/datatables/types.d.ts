// Type definitions for DataTables
//
// Project: https://datatables.net
// Definitions by:
//   SpryMedia
//   Kiarash Ghiaseddin <https://github.com/Silver-Connection>
//   Omid Rad <https://github.com/omidkrad>
//   Armin Sander <https://github.com/pragmatrix>
//   Craig Boland <https://github.com/CNBoland>

/// <reference types="../jquery/JQuery" />

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Types
 */

/**
 * DataTable's internal settings object. DO NOT USE! The parameters
 * in this object are considered private. They can, do, and will
 * change! Always use the API methods for manipulating the table.
 */
export type InternalSettings = {[key: string]: any};

export type DomSelector =
    string |
    Node |
    JQuery;

export type InstSelector =
    DomSelector |
    InternalSettings;

export type RowIdx = number;
export type RowSelector<T> =
    RowIdx |
    string |
    Node |
    JQuery |
    ((idx: RowIdx, data: T, node: Node | null) => boolean) |
    RowSelector<T>[];

export type ColumnIdx = number;
export type ColumnSelector =
    ColumnIdx |
    string |
    Node |
    JQuery |
    ((idx:ColumnIdx, data: any, node: Node) => boolean) |
    ColumnSelector[];

export type CellIdx = {
    row: number;
    column: number;
};
export type CellSelector =
    CellIdx |
    string |
    Node |
    JQuery |
    ((idx: CellIdx, data: any, node: Node | null) => boolean) |
    CellSelector[];


export type CellIdxWithVisible = {
    row: number;
    column: number;
    columnVisible: number;
}

export type SearchInput<T> = string | RegExp | ((data: string, rowData: T) => boolean);
export type SearchInputColumn<T> = string | RegExp | ((data: string, rowData: T, column: number) => boolean);

export type HeaderStructure = {
    cell: HTMLElement;
    colspan: number;
    rowspan: number;
    title: string;
}

/**
 * DataTables search options.
 * 
 * @see https://datatables.net/reference/type/DataTables.SearchOptions
 */
export interface SearchOptions {
    /** Match from the start of words (ASCII) */
	boundary?: boolean,

    /** Case insensitive search */
	caseInsensitive?: boolean,

    /** Exact matching */
	exact?: boolean,

    /** Treat the input as regex (true) or not (false) */
	regex?: boolean,

    /** Use DataTables smart search */
	smart?: boolean
}

export interface OrderIdx {
	idx: number;
	dir: 'asc' | 'desc';
}

export interface OrderName {
	name: string;
	dir: 'asc' | 'desc';
}

export type OrderArray = [number, 'asc' | 'desc' | ''];

export type OrderCombined = OrderIdx | OrderName | OrderArray;

export type Order = OrderCombined | OrderCombined[];

export interface DataType {
    className?: string;
    detect?: ExtTypeSettingsDetect;
    order?: {
        pre?: ((a: any, b: any) => number);
        asc?: ((a: any, b: any) => number);
        desc?: ((a: any, b: any) => number);
    }
    render?: ((data: any, type: string, row: any) => string | number | HTMLElement);
    search?: ((data: any) => string);
}

export interface Feature {
    /** A simple `<div>` that can contain your own content */
    div?: {
        /** Class name for the div */
        className?: string;

        /** ID to give the div */
        id?: string;

        /** HTML content for the div (cannot be used as well as textContent) */
        html?: string;

        /** Text content for the div (cannot be used as well as innerHTML) */
        text?: string;
    }

    /** Table information display */
    info?: {
        /** Information display callback */
        callback?: (settings: InternalSettings, start: number, end: number, max: number, total: number, pre: string) => string;

        /** Empty table text */
        empty?: string;

        /** Information string postfix */
        postfix?: string;

        /** Appended to the info string when searching is active */
        search?: string;

        /** Table summary information display string */
        text?: string;
    }

    /** Paging length control */
    pageLength?: {
        /** Text for page length control */
        menu?: Array<number | {label: string; value: number}>;

        /** Text for page length control */
        text?: string;
    }

    /** Pagination buttons */
    paging?: {
        /** Set the maximum number of paging number buttons */
        buttons?: number;

        /** Paging button display options */
        type?: 'numbers' | 'simple' | 'simple_numbers' | 'full' | 'full_numbers' | 'first_last_numbers';

        /** Display the buttons in the pager (default true) */
        numbers?: boolean;
        
        /** Display the first and last buttons in the pager (default true) */
        firstLast?: boolean;

        /** Display the previous and next buttons in the pager (default true) */
        previousNext?: boolean;

        /** Include the extreme page numbers, if separated by ellipsis, or not */
        boundaryNumbers?: boolean;
    }

    /** Global search input */
    search?: {
        /** Placeholder for the input element */
        placeholder?: string;

        /** Show the processing icon when searching */
        processing?: boolean;

        /** Text for search control */
        text?: string;
    }
}

type LayoutNumber = '' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9';

type LayoutSide = 'top' | 'bottom';

type LayoutEdge = 'Start' | 'End';

type LayoutKeys = `${LayoutSide}${LayoutNumber}${LayoutEdge}` | `${LayoutSide}${LayoutNumber}`;

type LayoutFeatures = keyof Feature | Feature | Array<keyof Feature> | Feature[];

type LayoutElement = {
    /** Class to apply to the CELL in the layout grid */
    className?: string;

    /** ID to apply to the CELL in the layout grid */
    id?: string;

    /** Class to apply to the ROW in the layout grid */
    rowClass?: string;

    /** ID to apply to the ROW in the layout grid */
    rowId?: string;

    /** List of features to show in this cell */
    features: LayoutFeatures;
}

type Layout = Partial<Record<LayoutKeys,
    LayoutElement |
    LayoutFeatures |
    (() => HTMLElement) |
    HTMLElement |
    JQuery<HTMLElement> |
    null
>>;


/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Main interfaces
 */

/**
 * DataTables class
 */
declare interface DataTables<T> extends DataTablesStatic {
    /**
     * Create a new DataTable
     * @param selector Selector to pick one or more `<table>` elements
     * @param opts Configuration settings
     */
    new <T=any>(
        selector: InstSelector,
        opts?: Config
    ): Api<T>

    /**
     * CommonJS factory. This only applies to CommonJS environments,
     * in all others it would throw an error.
     */
    (win?: Window, jQuery?: JQuery): DataTables<T>;
}

declare const DataTables: DataTables<any>;
export default DataTables;

// The `$().dataTable()` method adds a `.api()` method to the object
// to allow access to the DataTables API
interface JQueryDataTables extends JQuery {
    /**
     * Returns DataTables API instance
     * Usage:
     * $( selector ).dataTable().api();
     */
    api(): Api<any>;
}

// Extend the jQuery object with DataTables' construction methods
declare global {
    interface JQueryDataTableApi extends DataTablesStatic {
        <T = any>(opts?: Config): Api<T>;
    }

    interface JQueryDataTableJq extends DataTablesStatic {
        (opts?: Config): JQueryDataTables;
    }

    interface JQuery {
        /**
         * Create a new DataTable, returning a DataTables API instance.
         * @param opts Configuration settings
         */
        DataTable: JQueryDataTableApi;

        /**
         * Create a new DataTable, returning a jQuery object, extended
         * with an `api()` method which can be used to access the
         * DataTables API.
         * @param opts Configuration settings
         */
        //dataTable: JQueryDataTableJq;
    }
}

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Options
 */

export interface Config {
    /**
     * Load data for the table's content from an Ajax source.
     */
    ajax?: string | AjaxSettings | FunctionAjax;

    /**
     * Feature control DataTables' smart column width handling.
     */
    autoWidth?: boolean;

    /**
     * Set a `caption` for the table. This can be used to describe the contents
     * of the table to the end user. A caption tag can also be read from HTML.
     */
    caption?: string;

    /**
     * Data to use as the display data for the table.
     */
    columns?: ConfigColumns[];

    /**
     * Assign a column definition to one or more columns.
     */
    columnDefs?: ConfigColumnDefs[];

    /**
     * HTML element to use for the title elements (including order indicator)
     */
    columnTitleTag?: string;

    /**
     * Data to use as the display data for the table.
     */
    data?: any[];

    /**
     * Delay the loading of server-side data until second draw
     */
    deferLoading?: number | number[];
 
    /**
     * Feature control deferred rendering for additional speed of initialisation.
     */
    deferRender?: boolean;

    /**
     * Destroy any existing table matching the selector and replace with the new options.
     */
    destroy?: boolean;

    /**
     * Initial paging start point.
     */
    displayStart?: number;

    /**
     * Define the table control elements to appear on the page and in what order.
     * 
     * @deprecated Use `layout` instead
     */
    dom?: string;

    /**
     * Feature control table information display field.
     */
    info?: boolean;

    /**
     * Language configuration object
     */
    language?: ConfigLanguage;

    /**
     * 
     */
    layout?: Layout;

    /**
     * Feature control the end user's ability to change the paging display length of the table.
     */
    lengthChange?: boolean;

    /**
     * Change the options in the page length select list.
     */
    lengthMenu?: Array<(number | string)> | Array<Array<(number | string)>>;

    /**
     * Add event listeners during the DataTables startup
     */
    on?: {
        [name: string]: ((this: HTMLElement, e: Event, ...args: any[]) => void);
    };

    /**
     * Control which cell the order event handler will be applied to in a column.
     */
    orderCellsTop?: boolean;

    /**
     * Highlight the columns being ordered in the table's body.
     */
    orderClasses?: boolean;

    /**
     * Reverse the initial data order when `desc` ordering
     */
    orderDescReverse?: boolean;

    /**
     * Initial order (sort) to apply to the table.
     */
    order?: Order | Order[];

    /**
     * Ordering to always be applied to the table.
     */
    orderFixed?: Order | Order[] | {
        pre?: Order | Order[],
        post: Order | Order[]
    };

    /**
     * Feature control ordering (sorting) abilities in DataTables.
     */
    ordering?: boolean | {
        /**
         * Control the showing of the ordering icons in the table header.
         */
        indicators?: boolean;

        /**
         * Control the addition of a click event handler on the table headers to activate
         * ordering.
         */
        handler?: boolean;
    };

    /**
     * Multiple column ordering ability control.
     */
    orderMulti?: boolean;

    /**
     * Change the initial page length (number of rows per page).
     */
    pageLength?: number;

    /**
     * Enable or disable table pagination.
     */
    paging?: boolean;

    /**
     * Pagination button display options. Basic Types: numbers (1.10.8) simple, simple_numbers, full, full_numbers
     */
    pagingType?: string;

    /**
     * Feature control the processing indicator.
     */
    processing?: boolean;

    /**
     * Display component renderer types.
     */
    renderer?: string | ConfigRenderer;

    /**
     * Retrieve an existing DataTables instance.
     */
    retrieve?: boolean;

    /**
     * Data property name that DataTables will use to set <tr> element DOM IDs. Since: 1.10.8
     */
    rowId?: string;

    /**
     * Allow the table to reduce in height when a limited number of rows are shown.
     */
    scrollCollapse?: boolean;

    /**
     * Horizontal scrolling.
     */
    scrollX?: boolean;

    /**
     * Vertical scrolling. Since: 1.10 Exp: "200px"
     */
    scrollY?: string;

    /**
     * Set an initial filter in DataTables and / or filtering options.
     */
    search?: ConfigSearch | boolean;

    /**
     * Define an initial search for individual columns.
     */
    searchCols?: ConfigSearch[];

    /**
     * Set a throttle frequency for searching.
     */
    searchDelay?: number;

    /**
     * Feature control search (filtering) abilities
     */
    searching?: boolean;

    /**
     * Feature control DataTables' server-side processing mode.
     */
    serverSide?: boolean;

    /**
     * Saved state validity duration.
     */
    stateDuration?: number;

    /**
     * State saving - restore table state on page reload.
     */
    stateSave?: boolean;

    /**
     * Set the zebra stripe class names for the rows in the table.
     */
    stripeClasses?: string[];

    /**
     * Tab index control for keyboard navigation.
     */
    tabIndex?: number;

    /**
     * Callback for whenever a TR element is created for the table's body.
     */
    createdRow?: FunctionCreateRow;

    /**
     * Function that is called every time DataTables performs a draw.
     */
    drawCallback?: FunctionDrawCallback;

    /**
     * Footer display callback function.
     */
    footerCallback?: FunctionFooterCallback;

    /**
     * Number formatting callback function.
     */
    formatNumber?: FunctionFormatNumber;

    /**
     * Header display callback function.
     */
    headerCallback?: FunctionHeaderCallback;

    /**
     * Table summary information display callback.
     */
    infoCallback?: FunctionInfoCallback;

    /**
     * Initialisation complete callback.
     */
    initComplete?: FunctionInitComplete;

    /**
     * Pre-draw callback.
     */
    preDrawCallback?: FunctionPreDrawCallback;

    /**
     * Row draw callback..
     */
    rowCallback?: FunctionRowCallback;

    /**
     * Callback that defines where and how a saved state should be loaded.
     */
    stateLoadCallback?: FunctionStateLoadCallback;

    /**
     * State loaded callback.
     */
    stateLoaded?: FunctionStateLoaded;

    /**
     * State loaded - data manipulation callback.
     */
    stateLoadParams?: FunctionStateLoadParams;

    /**
     * Callback that defines how the table state is stored and where.
     */
    stateSaveCallback?: FunctionStateSaveCallback;

    /**
     * State save - data manipulation callback.
     */
    stateSaveParams?: FunctionStateSaveParams;
}


export interface ConfigLanguage {
    emptyTable?: string;
    entries?: string | object;
    info?: string;
    infoEmpty?: string;
    infoFiltered?: string;
    infoPostFix?: string;
    decimal?: string;
    thousands?: string;

    /** Labels for page length entries */
    lengthLabels?: { [key: string | number]: string};
    lengthMenu?: string;
    loadingRecords?: string;
    processing?: string;
    search?: string;
    searchPlaceholder?: string;
    zeroRecords?: string;
    paginate?: {
        first?: string;
        last?: string;
        next?: string;
        previous?: string;
    };
    aria?: {
        orderable?: string;
        orderableReverse?: string;
        orderableRemove?: string;
        paginate?: {
            first?: string;
            last?: string;
            next?: string;
            previous?: string;
            number?: string;
        };
    };
    url?: string;
}


export interface ConfigColumns {
    /**
     * Set the column's aria-label title. Since: 1.10.25
     */
    ariaTitle?: string;

    /**
     * Cell type to be created for a column. th/td
     */
    cellType?: string;

    /**
     * Class to assign to each cell in the column.
     */
    className?: string;

    /**
     * Add padding to the text content used when calculating the optimal with for a table.
     */
    contentPadding?: string;

    /**
     * Cell created callback to allow DOM manipulation.
     */
    createdCell?: FunctionColumnCreatedCell;

    /**
     * Class to assign to each cell in the column.
     */
    data?: number | string | ObjectColumnData | FunctionColumnData | null;

    /**
     * Set default, static, content for a column.
     */
    defaultContent?: string;

    /**
     * Text to display in the table's footer for this column.
     */
    footer?: string;

    /**
     * Set a descriptive name for a column.
     */
    name?: string;

    /**
     * Enable or disable ordering on this column.
     */
    orderable?: boolean;

    /**
     * Define multiple column ordering as the default order for a column.
     */
    orderData?: number | number[];

    /**
     * Live DOM sorting type assignment.
     */
    orderDataType?: string;

    /**
     * Ordering to always be applied to the table. Since 1.10
     *
     * Array type is prefix ordering only and is a two-element array:
     * 0: Column index to order upon.
     * 1: Direction so order to apply ("asc" for ascending order or "desc" for descending order).
     */
    orderFixed?: any[] | OrderFixed;

    /**
     * Order direction application sequence.
     */
    orderSequence?: Array<'asc' | 'desc' | ''>;

    /**
     * Render (process) the data for use in the table.
     */
    render?: number | string | ObjectColumnData | FunctionColumnRender | ObjectColumnRender;

    /**
     * Enable or disable filtering on the data in this column.
     */
    searchable?: boolean;

    /**
     * Set the column title.
     */
    title?: string;

    /**
     * Set the column type - used for filtering and sorting string processing.
     */
    type?: string;

    /**
     * Enable or disable the display of this column.
     */
    visible?: boolean;

    /**
     * Column width assignment.
     */
    width?: string;
}

export type ConfigColumnDefs = ConfigColumnDefsMultiple | ConfigColumnDefsSingle;

export interface ConfigColumnDefsMultiple extends ConfigColumns {
    /**
     * Target column(s). Either this or `target` must be specified.
     */
    targets: string | number | Array<(number | string)>;
}

export interface ConfigColumnDefsSingle extends ConfigColumns {
    /**
     * Single column target. Either this or `targets` must be specified. Since: 1.12
     */
    target: string | number;
}

export interface ConfigRenderer {
    header?: string;
    pageButton?: string;
}

export interface ConfigSearch {
    /**
     * Control case-sensitive filtering option.
     */
    caseInsensitive?: boolean;

    /**
     * Enable / disable escaping of regular expression characters in the search term.
     */
    regex?: boolean;

    /**
     * Enable / disable DataTables' smart filtering.
     */
    smart?: boolean;

    /**
     * Set an initial filtering condition on the table.
     */
    search?: string;

    /**
     * Set a placeholder attribute for input type="text" tag elements. Since: 1.10.1
     */
    searchPlaceholder?: string;
}


/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * API
 */

export interface Api<T=any> {
    /**
     * API should be array-like
     */
    [key: number]: any;

    /**
     * Returns DataTables API instance
     *
     * @param table Selector string for table
     */
    (selector: string | Node | Node[] | JQuery): Api<T>;

    /**
     * Get jquery object
     * 
     * @param selector jQuery selector to perform on the nodes inside the table's tbody tag.
     * @param modifier Option used to specify how the content's of the selected columns should be ordered, and if paging or filtering in the table should be taken into account. 
     * @returns JQuery object with the matched elements in it's results set
     */
    $(selector: string | Node | Node[] | JQuery, modifier?: ApiSelectorModifier): JQuery;

    /**
     * Ajax Methods
     */
    ajax: ApiAjax;

    /**
     * Get a boolean value to indicate if there are any entries in the API instance's result set (i.e. any data, selected rows, etc).
     * @returns true if there are one or more items in the result set, false otherwise.
     */
    any(): boolean;

    /**
     * Get the contents of the `caption` element for the table.
     */
    caption(): string;

    /**
     * Set the contents of the `-tag caption` element. If the table doesn't have 
     * a `-tag caption` element, one will be created automatically.
     * 
     * @param string The value to show in the table's `caption` tag.
     * @param side `top` or `bottom` to set where the table will be shown on the
     *   table. If not given the previous value will be used (can also be set in
     *   CSS).
     * @returns DataTables API instance for chaining.
     */
    caption(set: string, side?: 'top' | 'bottom'): Api<T>;

    /**
     * Cell (single) selector and methods
     */
    cell: ApiCell<T>;

    /**
     * Cells (multiple) selector and methods
     */
    cells: ApiCells<T>;

    /**
     * Clear the table of all data.
     * 
     * @returns DataTables Api instance.
     */
    clear(): Api<T>;

    /**
     * Column Methods / object
     */
    column: ApiColumn<T>;

    /**
     * Columns Methods / object
     */
    columns: ApiColumns<T>;

    /**
     * Concatenate two or more API instances together
     *
     * @param a API instance to concatenate to the initial instance.
     * @param b Additional API instance(s) to concatenate to the initial instance.
     * @returns New API instance with the values from all passed in instances concatenated into its result set.
     */
    concat(a: object, ...b: object[]): Api<any>;

    /**
     * The table setting objects that are manipulated by this API instance
     * 
     * @private
     */
    context: InternalSettings[],

    /**
     * Get the number of entries in an API instance's result set, regardless of multi-table grouping (e.g. any data, selected rows, etc). Since: 1.10.8
     * 
     * @returns The number of items in the API instance's result set
     */
    count(): number;

    /**
     * Get the data for the whole table.
     * 
     * @returns DataTables Api instance with the data for each row in the result set
     */
    data(): Api<T>;

    /**
     * Destroy the DataTables in the current context.
     *
     * @param remove Completely remove the table from the DOM (true) or leave it in the DOM in its original plain un-enhanced HTML state (default, false).
     * @returns DataTables Api instance
     */
    destroy(remove?: boolean): Api<T>;

    /**
     * Redraw the DataTables in the current context, optionally updating ordering, searching and paging as required.
     *
     * @param paging This parameter is used to determine what kind of draw DataTables will perform.
     * @returns DataTables Api instance
     */
    draw(paging?: boolean | string): Api<T>;

    /**
     * Iterate over the contents of the API result set.
     *
     * @param fn Callback function which is called for each item in the API instance result set. The callback is called with three parameters
     * @returns Original API instance that was used. For chaining.
     */
    each(fn: ((value: any, index: number, dt: Api<any>) => void)): Api<any>;

    /**
     * Reduce an Api instance to a single context and result set.
     *
     * @param idx Index to select
     * @returns New DataTables API instance with the context and result set containing the table and data for the index specified, or null if no matching index was available.
     */
    eq(idx: number): Api<any>;

    /**
     * Show an error message to the end user / developer through the DataTables logging settings.
     *
     * @param msg Error message to show
     */
    error(msg: string): Api<T>;

    /**
     * Iterate over the result set of an API instance and test each item, creating a new instance from those items which pass.
     *
     * @param fn Callback function which is called for each item in the API instance result set. The callback is called with three parameters.
     * @returns New API instance with the values from the result set which passed the test in the callback.
     */
    filter(fn: ((value: any, index: number, dt: Api<any>) => boolean)): Api<Array<any>>;

    /**
     * Flatten a 2D array structured API instance to a 1D array structure.
     * 
     * @returns New API instance with the 2D array values reduced to a 1D array.
     */
    flatten(): Api<Array<any>>;

    /** 
     * Look up a language token that was defined in the DataTables' language initialisation object.
     *
     * @param token The language token to lookup from the language object.
     * @param def The default value to use if the DataTables initialisation has not specified a value. This can be a string for simple cases, or an object for plurals.
     * @param numeric If handling numeric output, the number to be presented should be given in this parameter.
     *
     * @returns Resulting internationalised string.
     */
    i18n(token: string, def: object | string, numeric?: number): string;

    /**
     * Find the first instance of a value in the API instance's result set.
     *
     * @param value Value to find in the instance's result set.
     * @returns The index of the item in the result set, or -1 if not found.
     */
    indexOf(value: any): number;

    /**
     * Get the initialisation options used for the table. Since: DataTables 1.10.6
     * 
     * @returns Configuration object
     */
    init(): Config;

    /**
     * Iterate over a result set of table, row, column or cell indexes
     * 
     * @param type Iterator type
     * @param callback Callback function that is executed on each iteration. For the parameters passed to the function, please refer to the documentation above. As of this is executed in the scope of an API instance which has its context set to only the table in question.
     * @param returns Indicate if the callback function will return values or not. If set to true a new API instance will be returns with the return values from the callback function in its result set. If not set, or false the original instance will be returned for chaining, if no values are returned by the callback method.
     * @returns Original API instance if the callback returns no result (i.e. undefined) or a new API instance with the result set being the results from the callback, in order of execution.
     */
    iterator(type: 'table', callback: IteratorTable, returns?: boolean): Api<any>;
    iterator(type: 'cell', callback: IteratorCell, returns?: boolean): Api<any>;
    iterator(type: 'column-rows', callback: IteratorColumnRows, returns?: boolean): Api<any>;
    iterator(type: 'column', callback: IteratorColumn, returns?: boolean): Api<any>;
    iterator(type: 'columns', callback: IteratorColumns, returns?: boolean): Api<any>;
    iterator(type: 'row', callback: IteratorRow, returns?: boolean): Api<any>;
    iterator(type: 'rows', callback: IteratorRows, returns?: boolean): Api<any>;

    /**
     * Iterate over a result set of table, row, column or cell indexes
     * 
     * @param flatten If true the result set of the returned API instance will be a 1D array (i.e. flattened into a single array). If false (or not specified) each result will be concatenated to the instance's result set. Note that this is only relevant if you are returning arrays from the callback.
     * @param type Iterator type
     * @param callback Callback function that is executed on each iteration. For the parameters passed to the function, please refer to the documentation above. As of this is executed in the scope of an API instance which has its context set to only the table in question.
     * @param returns Indicate if the callback function will return values or not. If set to true a new API instance will be returns with the return values from the callback function in its result set. If not set, or false the original instance will be returned for chaining, if no values are returned by the callback method.
     * @returns Original API instance if the callback returns no result (i.e. undefined) or a new API instance with the result set being the results from the callback, in order of execution.
     */
    iterator(flatten: boolean, type: 'table', callback: IteratorTable, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'cell', callback: IteratorCell, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'column-rows', callback: IteratorColumnRows, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'column', callback: IteratorColumn, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'columns', callback: IteratorColumns, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'row', callback: IteratorRow, returns?: boolean): Api<any>;
    iterator(flatten: boolean, type: 'rows', callback: IteratorRows, returns?: boolean): Api<any>;

    /**
     * Join the elements in the result set into a string.
     *
     * @param separator The string that will be used to separate each element of the result set.
     * @returns Contents of the instance's result set joined together as a single string.
     */
    join(separator: string): string;

    /**
     * Find the last instance of a value in the API instance's result set.
     *
     * @param value Value to find in the instance's result set.
     * @returns The index of the item in the result set, or -1 if not found.
     */
    lastIndexOf(value: any): number;

    /**
     * Number of elements in an API instance's result set.
     */
    length: number;

    /**
     * Iterate over the result set of an API instance, creating a new API instance from the values returned by the callback.
     *
     * @param fn Callback function which is called for each item in the API instance result set. The callback is called with three parameters.
     * @returns New API instance with the values in the result set as those returned by the callback.
     */
    map(fn: ((value: any, index: number, dt: Api<any>) => any)): Api<any>;

    /**
     * Remove event listeners that have previously been added with on().
     *
     * @param event Event name to remove.
     * @param callback Specific callback function to remove if you want to unbind a single event listener.
     * @returns DataTables Api instance
     */
    off(event: string, callback?: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Remove event handlers from selected elements
     *
     * @param event Event name to remove.
     * @param selector Element selector
     * @param callback Specific callback function to remove if you want to unbind a single event listener.
     * @returns DataTables Api instance
     */
    off(event: string, selector: string, callback?: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Table events listener.
     *
     * @param event Event to listen for.
     * @param callback Event handler.
     * @returns DataTables Api instance
     */
    on(event: string, callback: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Listen for events from selected elements
     *
     * @param event Event to listen for.
     * @param selector Element selector
     * @param callback Event handler.
     * @returns DataTables Api instance
     */
    on(event: string, selector: string, callback: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Listen for a table event once and then remove the listener.
     *
     * @param event Event to listen for.
     * @param callback Event handler.
     * Listen for events from tables and fire a callback when they occur
     * @returns DataTables Api instance
     */
    one(event: string, callback: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Listen for events from a selected element and trigger only once then remove the listener.
     *
     * @param event Event to listen for.
     * @param selector Element selector
     * @param callback Event handler.
     * @returns DataTables Api instance
     */
    one(event: string, selector: string, callback: ((this: HTMLElement, e: Event, ...args: any[]) => void)): Api<T>;

    /**
     * Order Methods / object
     */
    order: ApiOrder;

    /**
     * Page Methods / object
     */
    page: ApiPage;

    /**
     * Iterate over the result set of an API instance, creating a new API instance from the values retrieved from the original elements.
     *
     * @param property object property name to use from the element in the original result set for the new result set.
     * @returns New API instance with the values in the result retrieved from the source object properties defined by the property being plucked.
     */
    pluck(property: number | string): Api<any>;

    /**
     * Remove the last item from an API instance's result set.
     * 
     * @returns Item removed form the result set (was previously the last item in the result set).
     */
    pop(): any;

    /**
     * Add one or more items to the end of an API instance's result set.
     *
     * @param value_1 Item to add to the API instance's result set.
     * @returns The length of the modified API instance
     */
    push(value_1: any, ...value_2: any[]): number;

    /**
     * Determine if the DataTable is ready or not
     */
    ready(): boolean;

    /**
     * Execute a function when the DataTable becomes ready (or immediately if it already is)
     *
     * @param fn Function to execute
     */
    ready(fn: ((this: Api<T>) => void)): Api<T>;

    /**
     * Apply a callback function against and accumulator and each element in the Api's result set (left-to-right).
     *
     * @param fn Callback function which is called for each item in the API instance result set. The callback is called with four parameters.
     * @param initialValue Value to use as the first argument of the first call to the fn callback.
     * @returns Result from the final call to the fn callback function.
     */
    reduce(fn: (current: T, value: T, index: number, dt: Api<any>) => T): T;
    reduce(fn: (current: T, value: T, index: number, dt: Api<any>) => T, initialValue: T): T;
    reduce<U>(fn: (current: U, value: T, index: number, dt: Api<any>) => U, initialValue: U): U;

    /**
     * Apply a callback function against and accumulator and each element in the Api's result set (right-to-left).
     *
     * @param fn Callback function which is called for each item in the API instance result set. The callback is called with four parameters.
     * @param initialValue Value to use as the first argument of the first call to the fn callback.
     * @returns Result from the final call to the fn callback function.
     */
    reduceRight(fn: (current: T, value: T, index: number, dt: Api<any>) => T): T;
    reduceRight(fn: (current: T, value: T, index: number, dt: Api<any>) => T, initialValue: T): T;
    reduceRight<U>(fn: (current: U, value: T, index: number, dt: Api<any>) => U, initialValue: U): U;

    /**
     * Reverse the result set of the API instance and return the original array.
     * 
     * @returns The original API instance with the result set in reversed order.
     */
    reverse(): Api<any>;

    /**
     * Row Methods / object
     */
    row: ApiRow<T>;

    /**
     * Rows Methods / object
     */
    rows: ApiRows<T>;

    /**
     * Search Methods / object
     */
    search: ApiSearch<T>;

    /**
     * Obtain the table's settings object
     * 
     * @returns DataTables API instance with the settings objects for the tables in the context in the result set
     */
    settings(): Api<InternalSettings>;

    /**
     * Remove the first item from an API instance's result set.
     * 
     * @returns Item removed form the result set (was previously the first item in the result set).
     */
    shift(): any;

    /**
     * Create an independent copy of the API instance.
     * 
     * @returns DataTables API instance
     */
    slice(): Api<any>;

    /**
     * Sort the elements of the API instance's result set.
     *
     * @param fn This is a standard JavaScript sort comparison function. It accepts two parameters.
     * @returns The original API instance with the result set sorted as defined by the sorting conditions used.
     */
    sort(fn?: ((value1: any, value2: any) => number)): Api<Array<any>>;

    /**
     * Modify the contents of an Api instance's result set, adding or removing items from it as required.
     *
     * @param index Index at which to start modifying the Api instance's result set.
     * @param howMany Number of elements to remove from the result set.
     * @param value_1 Item to add to the result set at the index specified by the first parameter.
     * @returns An array of the items which were removed. If no elements were removed, an empty array is returned.
     */
    splice(index: number, howMany: number, value_1?: any, ...value_2: any[]): any[];

    /**
     * Page Methods / object
     */
    state: ApiState<T>;

    /**
     * Select a table based on a selector from the API's context
     *
     * @param tableSelector Table selector.
     * @returns DataTables API instance with selected table in its context.
     */
    table(tableSelector?: any): ApiTableMethods<T>;

    /**
     * Select tables based on the given selector
     *
     * @param tableSelector Table selector.
     * @returns DataTables API instance with all tables in the current context.
     */
    tables(tableSelector?: any): ApiTablesMethods<T>;

    /**
     * Convert the API instance to a jQuery object, with the objects from the instance's result set in the jQuery result set.
     * 
     * @returns jQuery object which contains the values from the API instance's result set.
     */
    to$(): JQuery;

    /**
     * Create a native JavaScript array object from an API instance.
     * 
     * @returns JavaScript array which contains the values from the API instance's result set.
     */
    toArray(): any[];

    /**
     * Convert the API instance to a jQuery object, with the objects from the instance's result set in the jQuery result set.
     * 
     * @returns jQuery object which contains the values from the API instance's result set.
     */
    toJQuery(): JQuery;

    /**
     * Trigger a DataTables related event.
     *
     * @param name The event name.
     * @param args An array of the arguments to send to the event.
     * @param bubbles Indicate if the event should bubble up the document in the
     *   same way that DOM events usually do, or not. There is a performance
     *   impact for bubbling events.
     * @returns Api instance for chaining
     */
    trigger(name: string, args?: any[], bubbles?: boolean): Api<T>;

    /**
     * Create a new API instance containing only the unique items from the elements in an instance's result set.
     * 
     * @returns New Api instance which contains the unique items from the original instance's result set, in its own result set.
     */
    unique(): Api<any>;

    /**
     * Add one or more items to the start of an API instance's result set.
     *
     * @param value_1 Item to add to the API instance's result set.
     * @returns The length of the modified API instance
     */
    unshift(value_1: any, ...value_2: any[]): number;
}


export interface ApiSelectorModifier {
    /**
     * The order in which the resolved columns should be returned in.
     * 
     * * `implied` - the order given in the selector (default)
     * * `index` - column index order
     */
    columnOrder?: 'index' | 'implied';
    /**
     * The order modifier provides the ability to control which order the rows are
     * processed in. Can be one of 'current', 'applied', 'index', 'original', or
     * the column index that you want the order to be applied from.
     */
    order?: 'current' | 'applied' | 'index' | 'original' | number;

    /**
     * The search modifier provides the ability to govern which rows are used by the selector using the search options that are applied to the table.
     * Values: 'none', 'applied', 'removed'
     */
    search?: 'none' | 'applied' | 'removed';

    /**
     * The searchPlaceholder modifier provides the ability to provide informational text for an input control when it has no value.
     */
    searchPlaceholder?: string;

    /**
     * The page modifier allows you to control if the selector should consider all data in the table, regardless of paging, or if only the rows in the currently disabled page should be used.
     * Values: 'all', 'current'
     */
    page?: 'all' | 'current';
}

export interface AjaxMethods extends Api<any> {
    /**
     * Reload the table data from the Ajax data source.
     *
     * @param callback Function which is executed when the data as been reloaded and the table fully redrawn.
     * @param resetPaging Reset (default action or true) or hold the current paging position (false).
     * @returns DataTables Api instance
     */
    load(callback?: ((json: any) => void), resetPaging?: boolean): Api<any>;
}

export interface ApiSearch<T> {
    /**
     * Get current search
     * 
     * @returns The currently applied global search. This may be an empty string if no search is applied.
     */
    (): SearchInput<T>;

    /**
     * Set the global search to use on the table. Note this doesn't actually perform the search.
     *
     * @param input Search string to apply to the table.
     * @param regex Treat as a regular expression (true) or not (default, false).
     * @param smart Perform smart search.
     * @param caseInsen Do case-insensitive matching (default, true) or not (false).
     * @returns DataTables API instance
     */
    (input: SearchInput<T>, regex?: boolean, smart?: boolean, caseInsen?: boolean): Api<any>;

    /**
     * Set the global search to use on the table. Note this doesn't actually perform the search.
     *
     * @param input Search string to apply to the table.
     * @param options Configuration options for how the search should be performed
     * @returns DataTables API instance
     */
    (input: SearchInput<T>, options: SearchOptions): Api<any>;

    /**
     * Get a list of the names of searches applied to the table.
     * 
     * @returns API instance containing the fixed search terms
     */
    fixed(): Api<string>;
    
    /**
     * Get the search term used for the given name.
     *
     * @param name Fixed search term to get.
     * @returns The search term for the name given or undefined if not set.
     */
    fixed( name: string ): SearchInput<T> | undefined;
    
    /**
     * Set a search term to apply to the table, using a name to uniquely identify it.
     *
     * @param name Name to give the fixed search term
     * @param search The search term to apply to the table or `null` to delete
     *   an existing search term by the given name.
     * @returns API for chaining
     */
    fixed( name: string, search: SearchInput<T> | null ): Api<T>;
}


export interface ApiPageInfo {
    page: number;
    pages: number;
    start: number;
    end: number;
    length: number;
    recordsTotal: number;
    recordsDisplay: number;
    serverSide: boolean;
}

export interface State {
    time: number;
    start: number;
    length: number;
    order: Array<Array<(string | number)>>;
    search: ConfigSearch;
    columns: Array<{
        name: string;
        search: ConfigSearch;
        visible: boolean;
    }>;
}

/**
 * "table" - loop over the context's (i.e. the tables) for the instance
 * 
 * @param settings Table settings object
 * @param counter Loop counter
 */
type IteratorTable = (settings: InternalSettings, counter: number) => any;

/**
 * "cell" - loop over each table and cell in the result set
 * 
 * @param settings Table settings object
 * @param rowIndex Row index
 * @param columnIndex Column index
 * @param tableCounter Table counter (outer)
 * @param cellCounter Cell counter (inner)
 */        
type IteratorCell = (settings: InternalSettings, rowIndex: number, columnIndex: number, tableCounter: number, cellCounter: number) => any;

/**
 * "columns" - loop over each item in the result set
 * 
 * @param settings Table settings object
 * @param resultItem Result set item
 * @param counter Loop counter
 */
type IteratorColumns = (settings: InternalSettings, resultItem: any, counter: number) => any;

/**
 * "column" - loop over each table and column in the result set
 * 
 * @param settings Table settings object
 * @param columnIndex Column index 
 * @param tableCounter Table counter (outer)
 * @param columnCounter Column counter (inner)
 */
type IteratorColumn = (settings: InternalSettings, columnIndex: number, tableCounter: number, columnCounter: number) => any;

/**
 * "column-rows" - loop over each table, column and row in the result set applying selector-modifier.
 * 
 * @param settings Table settings object
 * @param columnIndex Column index
 * @param tableCounter Table counter (outer)
 * @param columnCounter Column counter (inner)
 * @param rowIndexes Row indexes
 */
type IteratorColumnRows = (settings: InternalSettings, columnIndex: number, tableCounter: number, columnCounter: number, rowIndexes: number[]) => any;

/**
 * "row" - loop over each table and row in the result set
 * 
 * @param settings Table settings object
 * @param rowIndex Row index
 * @param tableCounter Table counter (outer)
 * @param rowCounter Row counter (inner)
 */
type IteratorRow = (settings: InternalSettings, rowIndex: number, tableCounter: number, rowCounter: number) => any;

/**
 * "rows" - loop over each item in the result set
 * 
 * @param settings Table settings object
 * @param resultItem Result set item
 * @param counter Loop counter
 */
type IteratorRows = (settings: InternalSettings, resultItem: any, counter: number) => any;

export interface ApiAjax {
    /**
     * Get the latest JSON data obtained from the last Ajax request DataTables made
     * 
     * @returns JSON object that was last loaded/
     */
    json(): object;

    /**
     * Get the data submitted by DataTables to the server in the last Ajax request
     * 
     * @returns object containing the data submitted by DataTables
     */
    params(): object;

    /**
     * Reload the table data from the Ajax data source.
     *
     * @param callback Function which is executed when the data as been reloaded and the table fully redrawn.
     * @param resetPaging Reset (default action or true) or hold the current paging position (false).
     * @returns DataTables Api
     */
    reload(callback?: ((json: any) => void), resetPaging?: boolean): Api<any>;

    /**
     * Reload the table data from the Ajax data source
     * 
     * @returns URL set as the Ajax data source for the table.
     */
    url(): string;

    /**
     * Reload the table data from the Ajax data source
     *
     * @param url URL to set to be the Ajax data source for the table.
     * @returns DataTables Api instance for chaining or further ajax.url() methods
     */
    url(url: string): AjaxMethods;
}

export interface ApiPage {
    /**
     * Get the current page of the table.
     * 
     * @returns Currently displayed page number
     */
    (): number;

    /**
     * Set the current page of the table.
     *
     * @param page Index or 'first', 'next', 'previous', 'last'
     * @returns DataTables API instance
     */
    (page: number | string): Api<any>;

    /**
     * Get paging information about the table
     * 
     * @returns Object with information about the table's paging state.
     */
    info(): ApiPageInfo;

    /**
     * Get the table's page length.
     * 
     * @returns Current page length.
     */
    len(): number;

    /**
     * Set the table's page length.
     *
     * @param length Page length to set. use -1 to show all records.
     * @returns DataTables API instance.
     */
    len(length: number): Api<any>;
}

export interface ApiOrder {
    /**
     * Get the ordering applied to the table.
     * 
     * @returns Array of arrays containing information about the currently applied sort. This 2D array is the same format as the array used for setting the order to apply to the table
     */
    (): OrderArray[];

    /**
     * Set the ordering applied to the table.
     *
     * @param order Order Model
     * @returns DataTables Api instance
     */
    (order?: Order | Order[]): Api<any>;
    (order: Order, ...args: Order[]): Api<any>;

    /**
     * Get the fixed ordering that is applied to the table. If there is more than one table in the API's context,
     * the ordering of the first table will be returned only (use table() if you require the ordering of a different table in the API's context).
     * @returns object describing the ordering that is applied to the table
     */
    fixed(): OrderFixed;

    /**
     * Set the table's fixed ordering. Note this doesn't actually perform the order, but rather queues it up - use draw() to perform the ordering.
     * 
     * @param order Used to indicate whether the ordering should be performed before or after the users own ordering.
     * @returns DataTables Api instance
     */
    fixed(order: OrderFixed): Api<any>;

    /**
     * Add an ordering listener to an element, for a given column.
     *
     * @param node Selector
     * @param column Column index
     * @param callback Callback function
     * @returns DataTables API instance with the current order in the result set
     */
    listener(
        node: string | Node | JQuery,
        column: HTMLElement | number | number[] | (() => number[]),
        callback: (() => void)
    ): Api<any>;
}

export interface ApiState<T> {
    /**
     * Get the last saved state of the table
     * 
     * @returns State saved object
     */
    (): State;

    /**
     * Clear the saved state of the table.
     * 
     * @returns The API instance that was used, available for chaining.
     */
    clear(): Api<any>;

    /**
     * Get the table state that was loaded during initialisation.
     * 
     * @returns State saved object. See state() for the object format.
     */
    loaded(): State;

    /**
     * Trigger a state save.
     * 
     * @returns The API instance that was used, available for chaining.
     */
    save(): Api<T>;
}



export interface ApiCell<T> {
    /**
     * Select the cell found by a cell selector
     *
     * @param cellSelector Cell selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering
     * @returns DataTables API instance with selected cell
     */
    (cellSelector: CellSelector, modifier?: ApiSelectorModifier): ApiCellMethods<T>;

    /**
     * Select the cell found by a cell selector
     *
     * @param rowSelector Row selector.
     * @param columnSelector Column selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering
     * @returns DataTables API instance with selected cell
     */
    (rowSelector: RowSelector<T>, columnSelector: ColumnSelector, modifier?: ApiSelectorModifier): ApiCellMethods<T>;
}

export interface ApiCellMethods<T> extends Omit<Api<T>, 'render' | 'select'> {
    /**
     * Get the DataTables cached data for the selected cell
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @returns DataTables API instance with the cached data for each selected cell in the result set
     */
    cache(type: string): Api<T>;

    /**
     * Get data for the selected cell
     * 
     * @returns the data from the cell
     */
    data(): any;

    /**
     * Get data for the selected cell
     *
     * @param data Value to assign to the data for the cell
     * @returns DataTables Api instance
     */
    data(data: any): Api<T>;

    /**
     * Get index information about the selected cell
     * 
     * @returns Object with index information for the selected cell.
     */
    index(): CellIdxWithVisible;

    /**
     * Invalidate the data held in DataTables for the selected cell
     *
     * @param source Data source to read the new data from.
     * @returns DataTables API instance with selected cell references in the result set
     */
    invalidate(source?: string): Api<T>;

    /**
     * Get the DOM element for the selected cell
     * 
     * @returns The TD / TH cell the selector resolved to
     */
    node(): HTMLTableCellElement;

    /**
     * Get data for the selected cell
     *
     * @param type Data type to get. This can be one of: 'display', 'filter', 'sort', 'type'
     * @returns Rendered data for the requested type
     */
    render(type: string): any;
}

export interface ApiCells<T> {
    /**
     * Select all cells
     *
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering
     * @returns DataTables API instance with selected cells
     */
    (modifier?: ApiSelectorModifier): ApiCellsMethods<T>;

    /**
     * Select cells found by a cell selector
     *
     * @param cellSelector Cell selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering
     * @returns DataTables API instance with selected cells
     */
    (cellSelector: CellSelector, modifier?: ApiSelectorModifier): ApiCellsMethods<T>;
 
    /**
     * Select cells found by both row and column selectors
     *
     * @param rowSelector Row selector.
     * @param columnSelector Column selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering
     * @returns DataTables API instance with selected cells
     */
    (rowSelector: RowSelector<T>, columnSelector: ColumnSelector, modifier?: ApiSelectorModifier): ApiCellsMethods<T>;
}

export interface ApiCellsMethods<T> extends Omit<Api<T>, 'data' | 'render' | 'select'> {
    /**
     * Get the DataTables cached data for the selected cells
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @returns DataTables API instance with the cached data for each selected cell in the result set
     */
    cache(type: string): Api<T>;

    /**
     * Get data for the selected cells
     * 
     * @returns DataTables API instance with data for each cell in the selected columns in the result set. This is a 1D array with each entry being the data for the cells from the selected column.
     */
    data(): Api<Array<T>>;

    /**
     * Iterate over each selected cell, with the function context set to be the cell in question. Since: DataTables 1.10.6
     *
     * @param fn Function to execute for every cell selected.
     */
    every(fn: (this: ApiCellsMethods<T>, cellRowIdx: number, cellColIdx: number, tableLoop: number, cellLoop: number) => void): Api<any>;

    /**
     * Get index information about the selected cells
     */
    indexes(): Api<CellIdxWithVisible>;

    /**
     * Invalidate the data held in DataTables for the selected cells
     *
     * @param source Data source to read the new data from.
     * @returns DataTables API instance with selected cell references in the result set
     */
    invalidate(source?: string): Api<T>;

    /**
     * Get the DOM elements for the selected cells
     */
    nodes(): Api<HTMLTableCellElement>;

    /**
     * Get data for the selected cell
     *
     * @param type Data type to get. This can be one of: 'display', 'filter', 'sort', 'type'
     * @returns Rendered data for the requested type
     */
    render(type: string): any;

    /**
     * Get the title text for a column
     *
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return Column title
     */
    title( row?: number ): string;

    /**
     * Set the title text for a column
     *
     * @param title Title to set
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return DataTables API instance for chaining
     */
    title( title: string, row?: number ): Api<T>;

    /**
     * Get the column's data type (auto detected or configured).
     * 
     * @return The column's data type.
     */
    type(): string;

    /**
     * Compute the width of a column as it is shown.
     * 
     * @return The width of the column in pixels or `null` if there is no data
     *   in the table.
     */
    width(): number | null;
}




export interface ApiColumn<T> {
    /**
     * Select the column found by a column selector
     *
     * @param columnSelector Column selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     */
    (columnSelector: ColumnSelector, modifier?: ApiSelectorModifier): ApiColumnMethods<T>;

    /**
     * Convert from the input column index type to that required.
     *
     * @param type The type on conversion that should take place: 'fromVisible', 'toData', 'fromData', 'toVisible'
     * @param index The index to be converted
     * @returns Calculated column index
     */
    index(type: string, index: number): number;
}

export interface ApiColumnMethods<T> extends Omit<Api<T>, 'init' | 'data' | 'order' | 'render' | 'search'> {
    /**
     * Get the DataTables cached data for the selected column(s)
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @return DataTables Api instance with an caches data for the selected column(s)
     */
    cache(type: string): Api<any>;

    /**
     * Get the data for the cells in the selected column.
     * 
     * @returns DataTables API instance with data for each cell in the selected columns in the result set. This is a 1D array with each entry being the data for the cells from the selected column.
     */
    data(): Api<Array<any>>;

    /**
     * Get the data source property for the selected column.
     * 
     * @returns the data source property
     */
    dataSrc(): number | string | (() => string);

    /**
     * Get the footer th / td cell for the selected column.
     * 
     * @param row Indicate which row in the footer the cell should be read from
     *  when working with multi-row footers.
     * @returns HTML element for the footer of the column
     */
    footer(row?: number): HTMLElement;

    /**
     * Get the header th / td cell for a column.
     * 
     * @param row Indicate which row in the header the cell should be read from
     *  when working with multi-row headers.
     * @returns HTML element for the header of the column
     */
    header(row?: number): HTMLElement;

    /**
     * Get the column index of the selected column.
     *
     * @param type Specify if you want to get the column data index (default) or the visible index (visible).
     * @returns The column index for the selected column.
     */
    index(type?: string): number;

    /**
     * Get the initialisation object used for the selected column.
     *
     * @returns Column configuration object
     */
    init(): ConfigColumns;

    /**
     * Get the name for the selected column (set by `columns.name`).
     * 
     * @returns Column name or null if not set.
     */
    name(): string | null;

    /**
     * Obtain the th / td nodes for the selected column
     * 
     * @returns DataTables API instance with each cell's node from the selected columns in the result set. This is a 1D array with each entry being the node for the cells from the selected column.
     */
    nodes(): Api<Array<HTMLTableCellElement>>;

    /**
     * Order the table, in the direction specified, by the column selected by the column() selector.
     *
     * @param direction Direction of sort to apply to the selected column - desc (descending) or asc (ascending).
     * @returns DataTables API instance
     */
    order(direction: string): Api<any>;

    /**
     * Get the orderable state for the column (from `columns.orderable`).
     */
    orderable(): boolean;

    /**
     * Get a list of the column ordering directions (from `columns.orderSequence`).
     */
    orderable(directions: true): Api<string>;

    /**
     * Get rendered data for the selected column.
     * @param type Data type to get. Typically `display`, `filter`, `sort` or `type`
     *   although can be anything that the rendering functions expect.
     */
    render(type?: string): Api<T>;

    /**
     * Search methods and properties
     */
    search: ApiColumnSearch<T>;

    /**
     * Get the title text for the selected column
     *
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return Column titles in API instance's data set
     */
    title( row?: number ): string;

    /**
     * Set the title text for the selected column
     *
     * @param title Title to set
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return DataTables API instance for chaining
     */
    title( title: string, row?: number ): Api<T>;

    /**
     * Get the data type for the selected column (auto detected or configured).
     * 
     * @return DataTables API instance with column types in its data set
     */
    type(): string;

    /**
     * Get the visibility of the selected column.
     * 
     * @returns true if the column is visible, false if it is not.
     */
    visible(): boolean;

    /**
     * Set the visibility of the selected column.
     *
     * @param show Specify if the column should be visible (true) or not (false).
     * @param redrawCalculations Indicate if DataTables should recalculate the column layout (true - default) or not (false).
     * @returns DataTables API instance with selected column in the result set.
     */
    visible(show: boolean, redrawCalculations?: boolean): Api<any>;

    /**
     * Compute the width of the selected column as they are shown.
     * 
     * @return Api instance with the width of each column in pixels or `null` if
     *   there is no data in the table.
     */
    width(): number | null;
}

export interface ApiColumnSearch<T> {
    /**
     * Get the currently applied column search.
     * 
     * @returns the currently applied column search.
     */
    (): string;

    /**
     * Set the search term for the matched column.
     *
     * @param input Search apply.
     * @param regex Treat as a regular expression (true) or not (default, false).
     * @param smart Perform smart search.
     * @param caseInsen Do case-insensitive matching (default, true) or not (false).
     * @returns DataTables API instance
     */
    (input: SearchInputColumn<T>, regex?: boolean, smart?: boolean, caseInsen?: boolean): Api<any>;

    /**
     * Set the search term for the matched column.
     *
     * @param input Search to apply.
     * @param Search Search configuration options
     * @returns DataTables API instance
     */
    (input: SearchInputColumn<T>, options: SearchOptions): Api<any>;

    /**
     * Get a list of the names of searches applied to the column
     * 
     * @returns API instance containing the column's fixed search terms
     */
    fixed(): Api<string>;
    
    /**
     * Get the search term for the column used for the given name.
     *
     * @param name Fixed search term to get.
     * @returns The search term for the name given or undefined if not set.
     */
    fixed( name: string ): SearchInputColumn<T> | undefined;
    
    /**
     * Set a search term to apply to the column, using a name to uniquely identify it.
     *
     * @param name Name to give the fixed search term
     * @param search The search term to apply to the column or `null` to delete
     *   an existing search term by the given name.
     * @returns API for chaining
     */
    fixed( name: string, search: SearchInputColumn<T> | null ): Api<T>;
}

export interface ApiColumns<T> {
    /**
     * Select all columns
     *
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     * @returns DataTables API instance with selected columns in the result set.
     */
    (modifier?: ApiSelectorModifier): ApiColumnsMethods<T>;
    
    /**
     * Select columns found by a cell selector
     *
     * @param columnSelector Column selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     * @returns DataTables API instance with selected columns
     */
    (columnSelector: ColumnSelector, modifier?: ApiSelectorModifier): ApiColumnsMethods<T>;

    /**
     * Recalculate the column widths for layout.
     * 
     * @returns DataTables API instance.
     */
    adjust(): Api<T>;
}


export interface ApiColumnsMethods<T> extends Omit<Api<T>, 'init' | 'data' | 'order' | 'render' | 'search'> {
    /**
     * Get the DataTables cached data for the selected columna
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @return DataTables Api instance with an caches data for the selected columna
     */
    cache(type: string): Api<any>;

    /**
     * Obtain the data for the columns from the selector
     * 
     * @returns DataTables API instance with data for each cell in the selected columns in the result set. This is a 2D array with the top level array entries for each column matched by the columns() selector.
     */
    data(): Api<Array<Array<any>>>;

    /**
     * Get the data source property for the selected columns.
     * 
     * @returns API instance with the result set containing the data source parameters for the selected columns as configured by
     */
    dataSrc(): Api<any>;

    /**
     * Iterate over each selected column, with the function context set to be the column in question. Since: DataTables 1.10.6
     *
     * @param fn Function to execute for every column selected.
     * @returns DataTables API instance of the selected columns.
     */
    every(fn: (this: ApiColumnMethods<T>, colIdx: number, tableLoop: number, colLoop: number) => void): Api<any>;

    /**
     * Get the footer th / td cell for the selected columns.
     * 
     * @param row Indicate which row in the footer the cell should be read from
     *  when working with multi-row footers.
     * @returns HTML element for the footer of the columns
     */
    footer(row?: number): Api<HTMLElement>;

    /**
     * Get the header th / td cell for a columns.
     * 
     * @param row Indicate which row in the header the cell should be read from
     *  when working with multi-row headers.
     * @returns HTML element for the header of the columns
     */
    header(row?: number): Api<HTMLElement>;

    /**
     * Get the column indexes of the selected columns.
     *
     * @param type Specify if you want to get the column data index (default) or the visible index (visible).
     * @returns DataTables API instance with selected columns' indexes in the result set.
     */
    indexes(type?: string): Api<Array<number>>;

    /**
     * Get the initialisation objects used for the selected columns.
     *
     * @returns Api instance of column configuration objects
     */
    init(): Api<ConfigColumns>;

    /**
     * Get the names for the selected columns (set by `columns.name`).
     * 
     * @returns Column names (each entry can be null if not set).
     */
    names(): string | null;

    /**
     * Obtain the th / td nodes for the selected columns
     * 
     * @returns DataTables API instance with each cell's node from the selected columns in the result set. This is a 2D array with the top level array entries for each column matched by the columns() selector.
     */
    nodes(): Api<Array<Array<HTMLTableCellElement>>>;

    /**
     * Order the table, in the direction specified, by the columns selected by the column() selector.
     *
     * @param direction Direction of sort to apply to the selected columna - desc (descending) or asc (ascending).
     * @returns DataTables API instance
     */
    order(direction: string): Api<any>;

    /**
     * Get the orderable state for the selected columns (from `columns.orderable`).
     */
    orderable(): Api<boolean>;

    /**
     * Get a list of the column ordering directions (from `columns.orderSequence`).
     */
    orderable(directions: true): Api<Array<string>>;

    /**
     * Get rendered data for the selected columns.
     * @param type Data type to get. Typically `display`, `filter`, `sort` or `type`
     *   although can be anything that the rendering functions expect.
     */
    render(type?: string): Api<Array<T>>;

    /**
     * Search methods and properties
     */
    search: ApiColumnsSearch<T>;

    /**
     * Get the title text for the selected columns
     *
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return Column titles in API instance's data set
     */
    titles( row?: number ): Api<string>;

    /**
     * Set the title text for the selected columns
     *
     * @param title Title to set
     * @param row Indicate which row in the header the title should be read from
     *  when working with multi-row headers.
     * @return DataTables API instance for chaining
     */
    titles( title: string, row?: number ): Api<T>;

    /**
     * Get the data type for the selected columns (auto detected or configured).
     * 
     * @return DataTables API instance with column types in its data set
     */
    types(): Api<string>;

    /**
     * Get the visibility of the selected columns.
     * 
     * @returns true if the columns is visible, false if it is not.
     */
    visible(): boolean;

    /**
     * Set the visibility of the selected columns.
     *
     * @param show Specify if the columns should be visible (true) or not (false).
     * @param redrawCalculations Indicate if DataTables should recalculate the columns layout (true - default) or not (false).
     * @returns DataTables API instance with selected columns in the result set.
     */
    visible(show: boolean, redrawCalculations?: boolean): Api<any>;

    /**
     * Compute the width of the selected columns as they are shown.
     * 
     * @return Api instance with the width of each column in pixels or `null` if
     *   there is no data in the table.
     */
    widths(): Api<number | null>;
}

export interface ApiColumnsSearch<T> {
    /**
     * Get the currently applied columns search.
     * 
     * @returns the currently applied columns search.
     */
    (): Api<SearchInputColumn<T>[]>;

    /**
     * Set the search term for the columns from the selector. Note this doesn't actually perform the search.
     * 
     * @param input Search to apply to the selected columns.
     * @param regex Treat as a regular expression (true) or not (default, false).
     * @param smart Perform smart search (default, true) or not (false). 
     * @param caseInsen Do case-insensitive matching (default, true) or not (false).
     * @returns DataTables Api instance.
     */
    (input: SearchInputColumn<T>, regex?: boolean, smart?: boolean, caseInsen?: boolean): Api<any>;

    /**
     * Set the search term for the matched columns.
     *
     * @param input Search to apply.
     * @param Search Search configuration options
     * @returns DataTables API instance
     */
    (input: SearchInputColumn<T>, options: SearchOptions): Api<any>;

    /**
     * Get a list of the names of searches applied to the matched columns
     * 
     * @returns API instance containing the column's fixed search terms
     */
    fixed(): Api<string[]>;
    
    /**
     * Get the search term for the matched columns used for the given name.
     *
     * @param name Fixed search term to get.
     * @returns The search term for the name given or undefined if not set.
     */
    fixed( name: string ): Api<SearchInputColumn<T> | undefined>;

    /**
     * Set a search term to apply to the matched columns, using a name to
     * uniquely identify it.
     *
     * @param name Name to give the fixed search term
     * @param search The search term to apply to the column or `null` to delete
     *   an existing search term by the given name.
     * @returns API for chaining
     */
    fixed( name: string, search: SearchInputColumn<T> | null ): Api<T>;
}


export interface ApiRowChildMethods<T> {
    /**
     * Get the child row(s) that have been set for a parent row
     * 
     * @returns Query object with the child rows for the parent row in its result set, or undefined if there are no child rows set for the parent yet.
     */
    (): JQuery;

    /**
     * Get the child row(s) that have been set for a parent row
     *
     * @param showRemove This parameter can be given as true or false
     * @returns DataTables Api instance.
     */
    (showRemove: boolean): RowChildMethods<T>;

    /**
     * Set the data to show in the child row(s). Note that calling this method will replace any child rows which are already attached to the parent row.
     *
     * @param data The data to be shown in the child row can be given in multiple different ways.
     * @param className Class name that is added to the td cell node(s) of the child row(s). As of 1.10.1 it is also added to the tr row node of the child row(s).
     * @returns DataTables Api instance
     */
    (data: (string | Node | JQuery) | Array<(string | number | JQuery)>, className?: string): RowChildMethods<T>;

    /**
     * Hide the child row(s) of a parent row
     * 
     * @returns DataTables API instance.
     */
    hide(): Api<any>;

    /**
     * Check if the child rows of a parent row are visible
     * 
     * @returns boolean indicating whether the child rows are visible.
     */
    isShown(): boolean;

    /**
     * Remove child row(s) from display and release any allocated memory
     * 
     * @returns DataTables API instance.
     */
    remove(): Api<any>;

    /**
     * Show the child row(s) of a parent row
     * 
     * @returns DataTables API instance.
     */
    show(): Api<any>;
}

export interface RowChildMethods<T> extends Api<T> {
    /**
     * Hide the child row(s) of a parent row
     * 
     * @returns DataTables API instance.
     */
    hide(): Api<any>;

    /**
     * Remove child row(s) from display and release any allocated memory
     * 
     * @returns DataTables API instance.
     */
    remove(): Api<any>;

    /**
     * Make newly defined child rows visible
     * 
     * @returns DataTables API instance.
     */
    show(): Api<any>;
}

export interface ApiRow<T> {
    /**
     * Select a row found by a row selector
     *
     * @param rowSelector Row selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     * @returns DataTables API instance with selected row in the result set
     */
    (rowSelector: RowSelector<T>, modifier?: ApiSelectorModifier): ApiRowMethods<T>;

    /**
     * Add a new row to the table using the given data
     *
     * @param data Data to use for the new row. This may be an array, object or JavaScript object instance, but must be in the same format as the other data in the table+
     * @returns DataTables API instance with the newly added row in its result set.
     */
    add(data: any[] | object): ApiRowMethods<T>;
}

export interface ApiRowMethods<T> extends Omit<Api<T>, 'data' | 'select'> {
    /**
     * Get the DataTables cached data for the selected row(s)
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @returns DataTables API instance with data for each cell in the selected row in the result set. This is a 1D array with each entry being the data for the cells from the selected row.
     */
    cache(type: string): Api<Array<any>> | Api <Array<Array<any>>>;

    /**
     * Order Methods / object
     */
    child: ApiRowChildMethods<T>;

    /**
     * Get the data for the selected row
     * 
     * @returns Data source object for the data source of the row.
     */
    data(): T;

    /**
     * Set the data for the selected row
     *
     * @param d Data to use for the row.
     * @returns DataTables API instance with the row retrieved by the selector in the result set.
     */
    data(d: any[] | object): Api<T>;

    /**
     * Get the id of the selected row. Since: 1.10.8
     *
     * @param hash true - Append a hash (#) to the start of the row id. This can be useful for then using the id as a selector
     * false - Do not modify the id value.
     * @returns Row id. If the row does not have an id available 'undefined' will be returned.
     */
    id(hash?: boolean): string;

    /**
     * Get the row index of the row column.
     * 
     * @returns Row index
     */
    index(): number;

    /**
     * Obtain the th / td nodes for the selected row(s)
     *
     * @param source Data source to read the new data from. Values: 'auto', 'data', 'dom'
     */
    invalidate(source?: string): Api<Array<any>>;

    /**
     * Obtain the tr node for the selected row
     * 
     * @returns tr element of the selected row or null if the element is not yet available
     */
    node(): HTMLTableRowElement;

    /**
     * Delete the selected row from the DataTable.
     * 
     * @returns DataTables API instance with removed row reference in the result set
     */
    remove(): Api<T>;
}

export interface ApiRows<T> {
    /**
     * Select all rows
     *
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     * @returns DataTables API instance with selected rows
     */
    (modifier?: ApiSelectorModifier): ApiRowsMethods<T>;

    /**
     * Select rows found by a row selector
     *
     * @param rowSelector Row selector.
     * @param modifier Option used to specify how the cells should be ordered, and if paging or filtering in the table should be taken into account.
     * @returns DataTables API instance with selected rows in the result set
     */
    (rowSelector: RowSelector<T>, modifier?: ApiSelectorModifier): ApiRowsMethods<T>;

    /**
     * Add new rows to the table using the data given
     *
     * @param data Array of data elements, with each one describing a new row to be added to the table
     * @returns DataTables API instance with the newly added rows in its result set.
     */
    add(data: T[]): ApiRowsMethods<T>;
}

export interface ApiRowsMethods<T> extends Omit<Api<T>, 'select'> {
    /**
     * Get the DataTables cached data for the selected row(s)
     *
     * @param type Specify which cache the data should be read from. Can take one of two values: search or order
     * @returns DataTables API instance with data for each cell in the selected row in the result set. This is a 1D array with each entry being the data for the cells from the selected row.
     */
    cache(type: string): Api<Array<any>> | Api <Array<Array<any>>>;

    /**
     * Get the data for the selected rows
     *
     * @returns DataTables API instance with data for each row from the selector in the result set.
     */
    data(): Api<T>;

    /**
     * Iterate over each selected row, with the function context set to be the row in question. Since: DataTables 1.10.6
     *
     * @param fn Function to execute for every row selected.
     * @returns DataTables API instance of the selected rows.
     */
    every(fn: (this: ApiRowMethods<T>, rowIdx: number, tableLoop: number, rowLoop: number) => void): Api<any>;

    /**
     * Get the ids of the selected rows. Since: 1.10.8
     *
     * @param hash true - Append a hash (#) to the start of each row id. This can be useful for then using the ids as selectors
     * false - Do not modify the id value.
     * @returns Api instance with the selected rows in its result set. If a row does not have an id available 'undefined' will be returned as the value.
     */
    ids(hash?: boolean): Api<Array<any>>;

    /**
     * Get the row indexes of the selected rows.
     * 
     * @returns DataTables API instance with selected row indexes in the result set.
     */
    indexes(): Api<Array<number>>;

    /**
     * Obtain the th / td nodes for the selected row(s)
     *
     * @param source Data source to read the new data from. Values: 'auto', 'data', 'dom'
     */
    invalidate(source?: string): Api<Array<any>>;

    /**
     * Obtain the tr nodes for the selected rows
     * 
     * @returns DataTables API instance with each row's node from the selected rows in the result set.
     */
    nodes(): Api<Array<HTMLTableRowElement>>;

    /**
     * Delete the selected rows from the DataTable.
     * 
     * @returns DataTables API instance with references for the removed rows in the result set
     */
    remove(): Api<Array<any>>;
}


export interface ApiTableMethods<T> extends Api<T> {
    footer: {
        /**
         * Get the tfoot node for the table in the API's context
         * 
         * @returns HTML tbody node.
         */
        (): HTMLTableSectionElement;

        /**
         * Get an array that represents the structure of the footer
         *
         * @param selector Column selector
         */
        structure(selector?: string): HeaderStructure[][];
    }

    header: {
        /**
         * Get the thead node for the table in the API's context
         * 
         * @returns HTML thead node.
         */
        (): HTMLTableSectionElement;

        /**
         * Get an array that represents the structure of the header
         *
         * @param selector Column selector
         */
        structure(selector?: string): HeaderStructure[][];
    }

    /**
     * Get the tbody node for the table in the API's context
     * 
     * @returns HTML tfoot node.
     */
    body(): HTMLTableSectionElement;

    /**
     * Get the div container node for the table in the API's context
     * 
     * @returns HTML div node.
     */
    container(): HTMLDivElement;

    /**
     * Get the table node for the table in the API's context
     * 
     * @returns HTML table node for the selected table.
     */
    node(): HTMLTableElement;
}

export interface ApiTablesMethods<T> extends Api<T> {
    /**
     * Get the tfoot nodes for the tables in the API's context
     * 
     * @returns Array of HTML tfoot nodes for all table in the API's context
     */
    footer(): Api<Array<HTMLTableSectionElement>>;

    /**
     * Get the thead nodes for the tables in the API's context
     * 
     * @returns Array of HTML thead nodes for all table in the API's context
     */
    header(): Api<Array<HTMLTableSectionElement>>;

    /**
     * Get the tbody nodes for the tables in the API's context
     * 
     * @returns Array of HTML tbody nodes for all table in the API's context
     */
    body(): Api<Array<HTMLTableSectionElement>>;

    /**
     * Get the div container nodes for the tables in the API's context
     * 
     * @returns Array of HTML div nodes for all table in the API's context
     */
    containers(): Api<Array<HTMLDivElement>>;

    /**
     * Iterate over each selected table, with the function context set to be the table in question. Since: DataTables 1.10.6
     *
     * @param fn Function to execute for every table selected.
     */
    every(fn: (this: ApiTableMethods<T>, tableIndex: number) => void): Api<any>;


    /**
     * Get the table nodes for the tables in the API's context
     * 
     * @returns Array of HTML table nodes for all table in the API's context
     */
    nodes(): Api<Array<HTMLTableElement>>;
}


export interface DataTablesStatic {
    /**
     * Get DataTable API instance
     *
     * @param table Selector string for table
     */
    Api: ApiStatic;

    /**
     * Default Settings
     */
    defaults: Config;

    /**
     * Default Settings
     */
    ext: DataTablesStaticExt;

    /** Feature control namespace */
    feature: {
        /**
         * Create a new feature that can be used for layout
         *
         * @param name The name of the new feature.
         * @param construct A function that will create the elements and event listeners for the feature being added.
         */
        register(name: string, construct: (dt: InternalSettings, options: any) => HTMLElement | JQuery): void;
    }

    /**
     * Check if a table node is a DataTable already or not.
     *
     * @param table The table to check.
     * @returns true the given table is a DataTable, false otherwise
     */
    isDataTable(table: string | Node | JQuery | Api<any>): boolean;

    /**
     * Helpers for `columns.render`.
     *
     * The options defined here can be used with the `columns.render` initialisation
     * option to provide a display renderer.
     */
    render: DataTablesStaticRender;

    /**
     * Get all DataTable tables that have been initialised - optionally you can select to get only currently visible tables and / or retrieve the tables as API instances.
     *
     * @param visible As a boolean value this options is used to indicate if you want all tables on the page should be returned (false), or visible tables only (true).
     * Since 1.10.8 this option can also be given as an object.
     * @returns Array or DataTables API instance containing all matching DataTables
     */
    tables(visible?: {
        /**
         * Get only visible tables (true) or all tables regardless of visibility (false).
         */
        visible: boolean;
    
        /**
         * Return a DataTables API instance for the selected tables (true) or an array (false).
         */
        api: boolean;
    } | boolean): Array<Api<any>>| Api<any>;

    /**
     * Get the data type definition object for a specific registered data type.
     *
     * @param name Data type name to get the definition of
     */
    type(name: string): DataType;

    /**
     * Set one or more properties for a specific data type.
     *
     * @param name Data type name to set values for
     * @param definition Object containing the values to set
     */
    type(name: string, definition: DataType): void;

    /**
     * Set a class name for a given data type
     *
     * @param name Data type name to set a property value for
     * @param property Name of the data type property to set
     * @param val Class name to set
     */
    type(name: string, property: 'className', val: DataType['className']): void;

    /**
     * Set the detection function(s) for a given data type
     *
     * @param name Data type name to set a property value for
     * @param property Name of the data type property to set
     * @param val Detection function / object to set
     */
    type(name: string, property: 'detect', val: DataType['detect']): void;

    /**
     * Set the order functions for a given data type
     *
     * @param name Data type name to set a property value for
     * @param property Name of the data type property to set
     * @param val Object of functions to set
     */
    type(name: string, property: 'order', val: DataType['order']): void;

    /**
     * Set a rendering function for a given data type
     *
     * @param name Data type name to set a property value for
     * @param property Name of the data type property to set
     * @param val Rendering function
     */
    type(name: string, property: 'render', val: DataType['render']): void;

    /**
     * Set a search data renderer for a given data type
     *
     * @param name Data type name to set a property value for
     * @param property Name of the data type property to set
     * @param val Function to set
     */
    type(name: string, property: 'search', val: DataType['search']): void;


    /**
     * Get the names of the registered data types DataTables can work with.
     */
    types(): string[];

    /**
     * Get the libraries that DataTables uses, or the global objects.
     *
     * @param type The library needed
     */
    use(type: 'lib' | 'win' | 'datetime' | 'luxon' | 'moment'): any;

    /**
     * Set the libraries that DataTables uses, or the global objects, with automatic
     * detection of what the library is. Used for module loading environments.
     */
    use(library: any): void;

    /**
     * Set the libraries that DataTables uses, or the global objects, explicity stating
     * what library is to be considered. Used for module loading environments.
     *
     * @param type Indicate the library that is being loaded.
     * @param library The library (e.g. Moment or Luxon)
     */
    use(type: 'lib' | 'win' | 'datetime' | 'luxon' | 'moment', library: any): void;

    /**
     * Utils
     */
    util: DataTablesStaticUtil;

    /**
     * Version number compatibility check function
     *
     * Usage:
     * $.fn.dataTable.versionCheck("1.10.0");
     * @param version Version string
     * @returns true if this version of DataTables is greater or equal to the required version, or
     *   false if this version of DataTables is not suitable
     */
    versionCheck(version: string): boolean;
}

export type ApiStaticRegisterFn<T> = (this: Api<T>, ...args: any[]) => any;

export interface ApiStatic {
    /**
     * Create a new API instance to an existing DataTable. Note that this
     * does not create a new DataTable.
     */
    new (selector: string | Node | Node[] | JQuery | InternalSettings): Api<any>;

    register<T=any>(name: string, fn: ApiStaticRegisterFn<T>): void;
    registerPlural<T=any>(pluralName: string, singleName: string, fn: ApiStaticRegisterFn<T>): void;
}

export interface OrderFixed {
    /**
     * Two-element array:
     * 0: Column index to order upon.
     * 1: Direction so order to apply ("asc" for ascending order or "desc" for descending order).
     */
    pre?: any[];

    /**
     * Two-element array:
     * 0: Column index to order upon.
     * 1: Direction so order to apply ("asc" for ascending order or "desc" for descending order).
     */
    post?: any[];
}

export interface DataTablesStaticRender {
    /**
     * Format an ISO8061 date in auto locale detected format
     */
    date(): ObjectColumnRender;

    /**
     * Format an ISO8061 date value using the specified format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    date(to: string, locale?: string): ObjectColumnRender;

    /**
     * Format a date value
     * @param from Data format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    date(from?: string, to?: string, locale?: string, def?: string): ObjectColumnRender;

    /**
     * Format an ISO8061 datetime in auto locale detected format
     */
    datetime(): ObjectColumnRender;

    /**
     * Format an ISO8061 datetime value using the specified format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    datetime(to: string, locale?: string): ObjectColumnRender;

    /**
     * Format a datetime value
     * @param from Data format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    datetime(from?: string, to?: string, locale?: string, def?: string): ObjectColumnRender;

    /**
     * Render a number with auto-detected locale thousands and decimal
     */
    number(): ObjectColumnRender;

    /**
     * Will format numeric data (defined by `columns.data`) for display, retaining the original unformatted data for sorting and filtering.
     *
     * @param thousands Thousands grouping separator. `null` for auto locale
     * @param decimal Decimal point indicator. `null` for auto locale
     * @param precision Integer number of decimal points to show.
     * @param prefix Prefix (optional).
     * @param postfix Postfix (/suffix) (optional).
     */
    number(thousands: string | null, decimal: string | null, precision: number, prefix?: string, postfix?: string): ObjectColumnRender;

    /**
     * Escape HTML to help prevent XSS attacks. It has no optional parameters.
     */
    text(): ObjectColumnRender;

    /**
     * Format an ISO8061 date in auto locale detected format
     */
    time(): ObjectColumnRender;

    /**
     * Format an ISO8061 time value using the specified format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    time(to: string, locale?: string): ObjectColumnRender;

    /**
     * Format a time value
     * @param from Data format
     * @param to Display format
     * @param locale Locale
     * @param def Default value if empty
     */
    time(from?: string, to?: string, locale?: string, def?: string): ObjectColumnRender;
}

export interface DataTablesStaticUtil {
    /**
     * Normalise diacritic characters in a string.
     *
     * @param str String to have diacritic characters replaced
     * @param appendOriginal Append the original string to the result
     *   (`true`) or not (`false`)
     * @returns Updated string
     */
    diacritics(str: string, appendOriginal?: boolean): string;

    /**
     * Set the diacritic removal function
     *
     * @param replacement Removal function
     */
    diacritics(replacement: (str: string, appendOriginal: boolean) => string): void;

    /**
     * Escape special characters in a regular expression string. Since: 1.10.4
     *
     * @param str String to escape
     * @returns Escaped string
     */
    escapeRegex(str: string): string;

    /**
     * Escape entities in a string.
     *
     * @param str String to have HTML entities escaped
     * @returns Sanitized string
     */
    escapeHtml(str: string): string;

    /**
     * Set the HTML escaping function.
     *
     * @param escapeFunction Function to use for HTML escaping in DataTables.
     */
    escapeHtml(escapeFunction: (str: string) => string): void;

    /**
     * Create a read function from a descriptor. Since 1.11
     * @param source A descriptor that is used to define how to read the data from the source object.
     */
    get<T=any, D=any>(source: string | number | object | Function | null): ((data: D, type?: string, val?: T, meta?: CellMetaSettings) => T);

    /**
     * Create a write function from a descriptor. Since 1.11
     * @param source A descriptor that is used to define how to write data to a source object
     */
    set<T=any, D=any>(source: string | number | object | Function | null): ((data: D, val: T, meta?: CellMetaSettings) => void);

    /**
     * Remove mark up from a string
     *
     * @param str String to have HTML tags stripped from.
     * @returns Stripped string
     */
    stripHtml(str: string): string;

    /**
     * Set the HTML stripping function to be used by DataTables.
     *
     * @param stripFunction Function to use for HTML stripping in DataTables.
     */
    stripHtml(stripFunction: (str: string) => string): void;

    /**
     * Throttle the calls to a method to reduce call frequency. Since: 1.10.3
     *
     * @param fn Function
     * @param period ms
     * @returns Wrapper function that can be called and will automatically throttle calls to the passed in function to the given period.
     */
    throttle(fn: (data: any) => void, period?: number): (() => void);

    /**
     * Get unique values from an array.
     * 
     * @returns Array with unique values
     */
    unique<T=any>(input: Array<T>): Array<T>;
}



export interface AjaxData {
    draw: number;
    start: number;
    length: number;
    data: any;
    order: AjaxDataOrder[];
    columns: AjaxDataColumn[];
    search: AjaxDataSearch;
}

export interface AjaxDataSearch {
    value: string;
    regex: boolean;
}

export interface AjaxDataOrder {
    column: number;
    dir: string;
}

export interface AjaxDataColumn {
    data: string | number;
    name: string;
    searchable: boolean;
    orderable: boolean;
    search: AjaxDataSearch;
}

export interface AjaxResponse {
    draw?: number;
    recordsTotal?: number;
    recordsFiltered?: number;
    data: any;
    error?: string;
}

export type AjaxDataSrc = string | ((data: any) => any[]);

export interface AjaxSettings extends JQueryAjaxSettings {
    /**
     * Add or modify data submitted to the server upon an Ajax request.
     */
    data?: object | FunctionAjaxData;

    /**
     * Data property or manipulation method for table data.
     */
    dataSrc?: AjaxDataSrc | {
        /** Mapping for `data` property */
        data: AjaxDataSrc;

        /** Mapping for `draw` property */
        draw: AjaxDataSrc;

        /** Mapping for `recordsTotal` property */
        recordsTotal: AjaxDataSrc;

        /** Mapping for `recordsFiltered` property */
        recordsFiltered: AjaxDataSrc;
    };

    /** Format to submit the data parameters as in the Ajax request */
    submitAs?: 'http' | 'json';
}

interface FunctionColumnData {
    (row: any, type: 'set', s: any, meta: CellMetaSettings): void;
    (row: any, type: 'display' | 'sort' | 'filter' | 'type', s: undefined, meta: CellMetaSettings): any;
}

export interface ObjectColumnData {
    _: string | number | FunctionColumnData;
    filter?: string | number | FunctionColumnData;
    display?: string | number | FunctionColumnData;
    type?: string | number | FunctionColumnData;
    sort?: string | number | FunctionColumnData;
}

export interface ObjectColumnRender {
    _?: string | number | FunctionColumnRender;
    filter?: string | number | FunctionColumnRender;
    display?: string | number | FunctionColumnRender;
    type?: string | number | FunctionColumnRender;
    sort?: string | number | FunctionColumnRender;
}

export interface CellMetaSettings {
    row: number;
    col: number;
    settings: InternalSettings;
}

export interface DataTablesStaticExt {
    builder: string;
    buttons: DataTablesStaticExtButtons;
    ccContent: IColumnControlContent;
    classes: ExtClassesSettings;
    errMode: string;
    escape: {
        attributes: boolean;
    };
    feature: any[];
    iApiIndex: number;
    internal: object;
    legacy: object;
    oApi: object;
    order: object;
    oSort: object;
    pager: object;
    renderer: object;
    search: any[];
    selector: object;
    /**
     * Type based plug-ins.
     */
    type: ExtTypeSettings;
}

export interface DataTablesStaticExtButtons {
    // Intentionally empty, completed in Buttons extension
}

export interface IColumnControlContent {
    [name: string]: any;
}

/**
 * Classes used by DataTables. Used for styling integration. Note
 * that these all use legacy Hungarian notation.
 */
 export interface ExtClassesSettings {
    /** Class to apply to the container for all DataTables controlled elements */
	container: string;

    /** Empty row classes */
	empty: {
        /** Empty row class */
		row: string;
	};

    /** Info feature classes */
	info: {
        /** Container `<div>` class */
		container: string;
	};

    /** Layout grid classes */
    layout: {
		row: string;
		cell: string;
		tableRow: string;
		tableCell: string;
		start: string;
		end: string;
		full: string;
    },

    /** Length feature classes */
	length: {
        /** Container `<div>` class */
		container: string;

        /** Applied to the `<select>` element */
		select: string;
	};

    /** Table header cell ordering classes */
	order: {
        /** Applied if a column can sort asc */
		canAsc: string;

        /** Applied if a column can sort desc */
		canDesc: string;

        /** Applied if a column is sorting asc */
		isAsc: string;

        /** Applied if a column is sorting desc */
		isDesc: string;

        /** Applied if there is no sorting available on a column */
		none: string;

        /** Position class */
		position: string;
	};

    /** Processing indicator classes */
	processing: {
        /** Container `<div>` class */
		container: string;
	};

    /** Classes for scrolling tables (`scrollX` / `scrollY`) */
	scrolling: {
        /** Body table wrapper `<div>` */
		body: string;

        /** Applied to the wrapper for all scrolling elements */
		container: string;

        /** Scrolling footer classes */
		footer: {
            /** Applied to the footer container `<div>` */
			self: string;

            /** Applied to the inner footer `<div>` */
			inner: string;
		};

        /** Scrolling header classes */
		header: {
            /** Applied to the header container `<div>` */
			self: string;

            /** Applied to the inner header `<div>` */
			inner: string;
		}
	};

    /** Search feature classes */
	search: {
        /** Applied to the search container `<div>` */
		container: string;

        /** Class to add to `<input>` element */
		input: string;
	};

    /** Class to add to the `<table>` when DataTables is initialised on it */
	table: string;

    /** Table body classes */
	tbody: {
        /** Applied to all cells in the table body */
		cell: string;

        /** Applied to all `tr` element in the table body */
		row: string;
	};

    /** Table header classes */
	thead: {
        /** Applied to `td`/`th` elements in the table header */
		cell: string;

        /** Applied to `tr` elements in the table header */
		row: string;
	};

    /** Table footer classes */
	tfoot: {
        /** Applied to `td`/`th` elements in the footer */
		cell: string;

        /** Applied to `tr` elements in the footer */
		row: string;
	};

    /** Paging feature classes */
	paging: {
        /** Button shows the current page */
		active: string;

        /** Applied to all buttons */
		button: string;

        /** Container class */
		container: string;

        /** Button unavailable class */
		disabled: string;
	};
}


export interface ExtTypeSettings {
    /**
     * Type detection functions for plug-in development.
     *
     * @see https://datatables.net/manual/plug-ins/type-detection
     */
    detect: ExtTypeSettingsDetect[];

    /**
     * Type based ordering functions for plug-in development.
     *
     * @see https://datatables.net/manual/plug-ins/sorting
     * @default {}
     */
    order: any;

    /**
     * Type based search formatting for plug-in development.
     *
     * @default {}
     * @example
     *   $.fn.dataTable.ext.type.search['title-numeric'] = function ( d ) {
     *     return d.replace(/\n/g," ").replace( /<.*?>/g, "" );
     *   }
     */
    search: any;
}

/**
 * @param data Data from the column cell to be analysed.
 * @param DataTables settings object.
 */
export type ExtTypeSettingsDetect = ((data: any, settings: InternalSettings) => (boolean | string | null)) | {
    /**
     * All data points in the column must pass this function to allow a column
     * to take this data type.
     */
    allOf: (data: any, settings: InternalSettings) => boolean,

    /**
     * At least one of the data points in the column must pass this function to
     * allow the column to take this data type.
     */
    oneOf: (data: any, settings: InternalSettings) => boolean,

    /**
     * Run when type detection starts, to see if a column can be assigned a data type
     * based on a property of the column other than the data.
     */
    init?: (settings: InternalSettings, column: any, index: number) => boolean
};

type FunctionAjax = (this: JQueryDataTables, data: object, callback: ((data: any) => void), settings: InternalSettings) => void;

type FunctionAjaxData = (this: JQueryDataTables, data: AjaxData, settings: InternalSettings) => string | object;

type FunctionColumnRender = (this: JQueryDataTables, data: any, type: any, row: any, meta: CellMetaSettings) => any;

type FunctionColumnCreatedCell = (this: JQueryDataTables, cell: HTMLTableCellElement, cellData: any, rowData: any, row: number, col: number) => void;

type FunctionCreateRow = (this: JQueryDataTables, row: HTMLTableRowElement, data: any[] | object, dataIndex: number, cells: HTMLTableCellElement[]) => void;

type FunctionDrawCallback = (this: JQueryDataTables, settings: InternalSettings) => void;

type FunctionFooterCallback = (this: JQueryDataTables, tr: HTMLTableRowElement, data: any[], start: number, end: number, display: any[]) => void;

type FunctionFormatNumber = (this: JQueryDataTables, formatNumber: number) => void;

type FunctionHeaderCallback = (this: JQueryDataTables, tr: HTMLTableRowElement, data: any[], start: number, end: number, display: any[]) => void;

type FunctionInfoCallback = (this: JQueryDataTables, settings: InternalSettings, start: number, end: number, max: number, total: number, pre: string) => void;

type FunctionInitComplete = (this: JQueryDataTables, settings: InternalSettings, json: object) => void;

type FunctionPreDrawCallback = (this: JQueryDataTables, settings: InternalSettings) => void;

type FunctionRowCallback = (this: JQueryDataTables, row: HTMLTableRowElement, data: any[] | object, index: number) => void;

type FunctionStateLoadCallback = (this: JQueryDataTables, settings: InternalSettings, callback: ((state: State) => void)) => undefined | null | object;

type FunctionStateLoaded = (this: JQueryDataTables, settings: InternalSettings, data: object) => void;

type FunctionStateLoadParams = (this: JQueryDataTables, settings: InternalSettings, data: object) => void;

type FunctionStateSaveCallback = (this: JQueryDataTables, settings: InternalSettings, data: object) => void;

type FunctionStateSaveParams = (this: JQueryDataTables, settings: InternalSettings, data: object) => void;
