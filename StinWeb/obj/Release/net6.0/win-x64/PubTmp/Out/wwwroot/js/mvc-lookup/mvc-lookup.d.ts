/*!
 * Mvc.Lookup 5.1.0
 * https://github.com/NonFactors/AspNetCore.Lookup
 *
 * Copyright © NonFactors
 *
 * Licensed under the terms of the MIT License
 * https://www.opensource.org/licenses/mit-license.php
 */
export interface MvcLookupData {
    selected: MvcLookupDataRow[];
    columns: MvcLookupColumn[];
    rows: MvcLookupDataRow[];
}
export interface MvcLookupColumn {
    key: string;
    header: string;
    hidden: boolean;
    cssClass: string;
}
export interface MvcLookupDataRow {
    Id: string;
    Label: string;
    [column: string]: string | null;
}
export interface MvcLookupOptions {
    readonly: boolean;
    searchDelay: number;
    loadingDelay: number;
    dialog: Partial<MvcLookupDialogOptions>;
    autocomplete: Partial<MvcLookupAutocompleteOptions>;
}
export interface MvcLookupDialogOptions {
    preserveSearch: boolean;
    openDelay: number;
    rows: Partial<{
        min: number;
        max: number;
    }>;
}
export interface MvcLookupAutocompleteOptions {
    minLength: number;
    addHandler: boolean;
    rows: number;
    sort: string;
    order: "Asc" | "Desc" | "";
}
export interface MvcLookupLanguage {
    add: string;
    more: string;
    error: string;
    search: string;
    select: string;
    noData: string;
}
export declare class MvcLookupFilter {
    lookup: MvcLookup;
    search: string;
    sort: string;
    order: "Asc" | "Desc" | "";
    rows: number;
    offset: number;
    additional: string[];
    ids: HTMLInputElement[];
    checkIds: HTMLInputElement[];
    selected: MvcLookupDataRow[];
    constructor(lookup: MvcLookup);
    formUrl(search: Partial<MvcLookupFilter>): string;
}
export declare class MvcLookupDialog {
    static current: MvcLookupDialog | null;
    lookup: MvcLookup;
    element: HTMLElement;
    error: HTMLDivElement;
    rows: HTMLInputElement;
    loader: HTMLDivElement;
    table: HTMLTableElement;
    header: HTMLSpanElement;
    search: HTMLInputElement;
    overlay: MvcLookupOverlay;
    footer: HTMLButtonElement;
    selector: HTMLButtonElement;
    closeButton: HTMLButtonElement;
    tableHead: HTMLTableSectionElement;
    tableBody: HTMLTableSectionElement;
    options: MvcLookupDialogOptions;
    selected: MvcLookupDataRow[];
    title: string;
    constructor(lookup: MvcLookup);
    open(): void;
    close(): void;
    closeWithoutSave(): void;
    refresh(): void;
    private render;
    private renderHeader;
    private renderBody;
    private createHeaderCell;
    private createDataRow;
    private limitRows;
    private searchChanged;
    private rowsChanged;
    private loadMore;
    private bind;
}
export declare class MvcLookupOverlay {
    element: HTMLElement;
    constructor(dialog: MvcLookupDialog);
    show(): void;
    hide(): void;
    private findOverlay;
    private onMouseUp;
    private onKeyDown;
    private bind;
}
export declare class MvcLookupAutocomplete {
    lookup: MvcLookup;
    element: HTMLUListElement;
    activeItem: HTMLLIElement | null;
    options: MvcLookupAutocompleteOptions;
    constructor(lookup: MvcLookup);
    search(term: string): void;
    previous(): void;
    next(): void;
    hide(): void;
    resize(): void;
    private bind;
}
export declare class MvcLookup {
    static instances: MvcLookup[];
    static lang: MvcLookupLanguage;
    url: URL;
    for: string;
    multi: boolean;
    readonly: boolean;
    searchTimerId: number;
    loadingTimerId: number;
    group: HTMLElement;
    error: HTMLDivElement;
    items: HTMLDivElement[];
    control: HTMLDivElement;
    dialog: MvcLookupDialog;
    filter: MvcLookupFilter;
    search: HTMLInputElement;
    options: MvcLookupOptions;
    values: HTMLInputElement[];
    controller: AbortController;
    selected: MvcLookupDataRow[];
    valueContainer: HTMLDivElement;
    browser: HTMLButtonElement | null;
    autocomplete: MvcLookupAutocomplete;
    constructor(element: HTMLElement, options?: Partial<MvcLookupOptions>);
    set(options: Partial<MvcLookupOptions>): this;
    setReadonly(readonly: boolean): void;
    browse(): void;
    reload(triggerChanges?: boolean): void;
    select(data: MvcLookupDataRow[], triggerChanges?: boolean): void;
    selectFirst(triggerChanges?: boolean): void;
    selectSingle(triggerChanges?: boolean): void;
    fetch(search: Partial<MvcLookupFilter>, resolved: (data: MvcLookupData) => void): void;
    private createSelectedItems;
    private createValues;
    private bindDeselect;
    private findLookup;
    private cleanUp;
    private resize;
    private bind;
}
