/*!
 * Mvc.Lookup 5.0.1
 * https://github.com/NonFactors/AspNetCore.Lookup
 *
 * Copyright © NonFactors
 *
 * Licensed under the terms of the MIT License
 * http://www.opensource.org/licenses/mit-license.php
 */
class MvcLookupFilter {
    constructor(lookup) {
        const filter = this;
        const data = lookup.group.dataset;

        filter.offset = 0;
        filter.lookup = lookup;
        filter.sort = data.sort || "";
        filter.order = data.order || "";
        filter.search = data.search || "";
        filter.rows = parseInt(data.rows) || 20;
        filter.additional = (data.filters || "").split(",").filter(Boolean);
    }

    formUrl(search) {
        const filter = Object.assign({ ids: [], checkIds: [], selected: [] }, this, search);
        const url = new URL(this.lookup.url.href);
        const query = url.searchParams;

        for (const name of filter.additional) {
            for (const additional of document.querySelectorAll(`[name="${name}"]`)) {
                query.append(name, additional.value);
            }
        }

        for (const selected of filter.selected) {
            query.append("selected", selected.Id);
        }

        for (const id of filter.checkIds) {
            query.append("checkIds", id.value);
        }

        for (const id of filter.ids) {
            query.append("ids", id.value);
        }

        query.set("search", filter.search);
        query.set("offset", filter.offset);
        query.set("order", filter.order);
        query.set("sort", filter.sort);
        query.set("rows", filter.rows);
        query.set("_", Date.now());

        return url.href;
    }
}

class MvcLookupDialog {
    constructor(lookup) {
        const dialog = this;
        const element = document.getElementById(lookup.group.dataset.dialog || "MvcLookupDialog");

        dialog.lookup = lookup;
        dialog.element = element;
        dialog.controller = new AbortController();
        dialog.title = lookup.group.dataset.title || "";
        dialog.options = { preserveSearch: true, rows: { min: 1, max: 99 }, openDelay: 100 };

        dialog.overlay = new MvcLookupOverlay(this);
        dialog.table = element.querySelector("table");
        dialog.tableHead = element.querySelector("thead");
        dialog.tableBody = element.querySelector("tbody");
        dialog.rows = element.querySelector(".mvc-lookup-rows");
        dialog.header = element.querySelector(".mvc-lookup-title");
        dialog.search = element.querySelector(".mvc-lookup-search");
        dialog.footer = element.querySelector(".mvc-lookup-footer");
        dialog.selector = element.querySelector(".mvc-lookup-selector");
        dialog.closeButton = element.querySelector(".mvc-lookup-close");
        dialog.error = element.querySelector(".mvc-lookup-dialog-error");
        dialog.loader = element.querySelector(".mvc-lookup-dialog-loader");
    }

    open() {
        const dialog = this;
        const filter = dialog.lookup.filter;

        MvcLookupDialog.current = dialog;

        filter.offset = 0;
        filter.search = dialog.options.preserveSearch ? filter.search : "";

        dialog.error.style.display = "none";
        dialog.loader.style.display = "none";
        dialog.header.innerText = dialog.title;
        dialog.error.innerHTML = MvcLookup.lang.error;
        dialog.footer.innerText = MvcLookup.lang.more;
        dialog.selected = dialog.lookup.selected.slice();
        dialog.search.placeholder = MvcLookup.lang.search;
        dialog.rows.value = dialog.limitRows(filter.rows);
        dialog.selector.style.display = dialog.lookup.multi ? "" : "none";
        dialog.selector.innerText = MvcLookup.lang.select.replace("{0}", dialog.lookup.selected.length.toString());

        dialog.bind();
        dialog.refresh();
        dialog.search.value = filter.search;

        setTimeout(() => {
            if (dialog.loadingTimerId) {
                dialog.loader.style.opacity = "1";
                dialog.loader.style.display = "";
            }

            dialog.overlay.show();
            dialog.search.focus();
        }, dialog.options.openDelay);
    }
    close() {
        const dialog = MvcLookupDialog.current;

        dialog.lookup.select(dialog.selected, true);
        dialog.controller.abort();
        dialog.overlay.hide();

        if (dialog.lookup.browser) {
            dialog.lookup.browser.focus();
        }

        clearTimeout(dialog.loadingTimerId);
        clearTimeout(dialog.searchTimerId);

        MvcLookupDialog.current = null;
    }
    refresh() {
        const dialog = this;

        clearTimeout(dialog.loadingTimerId);

        dialog.loadingTimerId = setTimeout(() => {
            dialog.loader.style.opacity = "1";
            dialog.footer.style.opacity = "0";
        }, dialog.lookup.options.loadingDelay);
        dialog.loader.style.display = "";
        dialog.error.style.opacity = "0";
        dialog.error.style.display = "";

        dialog.controller.abort();
        dialog.controller = new AbortController();

        fetch(dialog.lookup.filter.formUrl({ selected: dialog.selected, rows: dialog.lookup.filter.rows + 1 }), {
            signal: dialog.controller.signal,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).then(response => {
            if (!response.ok) {
                throw new Error(`Invalid response status: ${response.status}`);
            }

            return response.json();
        }).then(data => {
            clearTimeout(dialog.loadingTimerId);

            dialog.loader.style.display = "none";
            dialog.error.style.display = "none";
            dialog.loader.style.opacity = "0";
            dialog.loadingTimerId = 0;
            dialog.render(data);
        }).catch(reason => {
            if (reason.name == "AbortError") {
                return Promise.resolve();
            }

            clearTimeout(dialog.loadingTimerId);

            dialog.footer.style.display = "none";
            dialog.loader.style.display = "none";
            dialog.loader.style.opacity = "0";
            dialog.error.style.opacity = "1";
            dialog.tableBody.innerHTML = "";
            dialog.tableHead.innerHTML = "";
            dialog.loadingTimerId = 0;

            dialog.search.focus();

            return Promise.reject(reason);
        });
    }

    render(data) {
        const dialog = this;

        if (!dialog.lookup.filter.offset) {
            dialog.tableBody.innerHTML = "";
            dialog.tableHead.innerHTML = "";
        }

        if (!dialog.lookup.filter.offset) {
            dialog.renderHeader(data.columns);
        }

        dialog.renderBody(data);

        if (data.rows.length <= dialog.lookup.filter.rows) {
            dialog.footer.style.display = "none";
        } else {
            dialog.footer.style.opacity = "";
            dialog.footer.style.display = "";
        }
    }
    renderHeader(columns) {
        const row = document.createElement("tr");

        for (const column of columns.filter(col => !col.hidden)) {
            row.appendChild(this.createHeaderCell(column));
        }

        row.appendChild(document.createElement("th"));
        this.tableHead.appendChild(row);
    }
    renderBody(data) {
        const dialog = this;

        for (const selected of data.selected) {
            const row = dialog.createDataRow(data.columns, selected);

            row.className = "selected";

            dialog.tableBody.appendChild(row);
        }

        if (data.selected.length) {
            const separator = document.createElement("tr");
            const content = document.createElement("td");

            content.colSpan = data.columns.length + 1;
            separator.className = "mvc-lookup-split";

            dialog.tableBody.appendChild(separator);
            separator.appendChild(content);
        }

        for (let i = 0; i < data.rows.length && i < dialog.lookup.filter.rows; i++) {
            dialog.tableBody.appendChild(dialog.createDataRow(data.columns, data.rows[i]));
        }

        if (!data.rows.length && !dialog.lookup.filter.offset) {
            const container = document.createElement("tr");
            const empty = document.createElement("td");

            container.className = "mvc-lookup-empty";
            empty.innerHTML = MvcLookup.lang.noData;
            empty.colSpan = data.columns.length + 1;

            dialog.tableBody.appendChild(container);
            container.appendChild(empty);
        }
    }

    createHeaderCell(column) {
        const filter = this.lookup.filter;
        const header = document.createElement("th");

        if (column.cssClass) {
            header.classList.add(column.cssClass);
        }

        if (filter.sort == column.key) {
            header.classList.add(`mvc-lookup-${filter.order.toLowerCase()}`);
        }

        header.innerText = column.header || "";
        header.addEventListener("click", () => {
            filter.order = filter.sort == column.key && filter.order == "Asc" ? "Desc" : "Asc";
            filter.sort = column.key;
            filter.offset = 0;

            this.refresh();
        });

        return header;
    }
    createDataRow(columns, data) {
        const dialog = this;
        const lookup = dialog.lookup;
        const row = document.createElement("tr");

        for (const column of columns.filter(col => !col.hidden)) {
            const cell = document.createElement("td");

            cell.innerText = data[column.key] || "";
            cell.className = column.cssClass || "";

            row.appendChild(cell);
        }

        row.appendChild(document.createElement("td"));

        row.addEventListener("click", function (e) {
            if (!window.getSelection().isCollapsed) {
                return;
            }

            if (lookup.multi) {
                const index = dialog.selected.findIndex(selected => selected.Id == data.Id);

                if (index >= 0) {
                    dialog.selected.splice(index, 1);

                    this.classList.remove("selected");
                } else {
                    dialog.selected.push(data);

                    this.classList.add("selected");
                }

                dialog.selector.innerText = MvcLookup.lang.select.replace("{0}", dialog.selected.length.toString());
            } else {
                if (e.ctrlKey && dialog.selected.findIndex(selected => selected.Id == data.Id) >= 0) {
                    dialog.selected = [];
                } else {
                    dialog.selected = [data];
                }

                dialog.close();
            }
        });

        return row;
    }

    limitRows(value) {
        const rows = Math.max(this.options.rows.min, Math.min(parseInt(value), this.options.rows.max));

        return (isNaN(rows) ? this.lookup.filter.rows : rows).toString();
    }

    searchChanged(e) {
        const dialog = MvcLookupDialog.current;

        dialog.controller.abort();
        clearTimeout(dialog.searchTimerId);

        if (e.keyCode == 13) {
            dialog.lookup.filter.search = this.value;
            dialog.lookup.filter.offset = 0;

            dialog.refresh();
        } else {
            dialog.searchTimerId = setTimeout(() => {
                if (dialog.lookup.filter.search != this.value) {
                    dialog.lookup.filter.search = this.value;
                    dialog.lookup.filter.offset = 0;

                    dialog.refresh();
                }
            }, dialog.lookup.options.searchDelay);
        }
    }
    rowsChanged() {
        const rows = this;
        const dialog = MvcLookupDialog.current;

        rows.value = dialog.limitRows(rows.value);

        if (dialog.lookup.filter.rows.toString() != rows.value) {
            dialog.lookup.filter.rows = parseInt(rows.value);
            dialog.lookup.filter.offset = 0;

            dialog.refresh();
        }
    }
    loadMore() {
        const dialog = MvcLookupDialog.current;

        dialog.lookup.filter.offset += dialog.lookup.filter.rows;

        dialog.refresh();
    }
    bind() {
        const dialog = this;

        dialog.selector.addEventListener("click", dialog.close);
        dialog.footer.addEventListener("click", dialog.loadMore);
        dialog.rows.addEventListener("change", dialog.rowsChanged);
        dialog.closeButton.addEventListener("click", dialog.close);
        dialog.search.addEventListener("keyup", dialog.searchChanged);
    }
}

class MvcLookupOverlay {
    constructor(dialog) {
        this.element = this.findOverlay(dialog.element);
        this.bind();
    }

    show() {
        const body = document.body.getBoundingClientRect();

        if (body.left + body.right < window.innerWidth) {
            const scrollWidth = window.innerWidth - document.body.clientWidth;
            const paddingRight = parseFloat(getComputedStyle(document.body).paddingRight);

            document.body.style.paddingRight = `${paddingRight + scrollWidth}px`;
        }

        document.body.classList.add("mvc-lookup-open");
        this.element.style.display = "block";
    }
    hide() {
        document.body.classList.remove("mvc-lookup-open");
        document.body.style.paddingRight = "";
        this.element.style.display = "";
    }

    findOverlay(element) {
        const overlay = element.closest(".mvc-lookup-overlay");

        if (!overlay) {
            throw new Error("Lookup dialog has to be inside a mvc-lookup-overlay.");
        }

        return overlay;
    }
    onMouseDown(e) {
        const targetClasses = e.target.classList;

        if (targetClasses.contains("mvc-lookup-overlay") || targetClasses.contains("mvc-lookup-wrapper")) {
            MvcLookupDialog.current.close();
        }
    }
    onKeyDown(e) {
        if (e.which == 27 && MvcLookupDialog.current) {
            MvcLookupDialog.current.close();
        }
    }
    bind() {
        this.element.addEventListener("mousedown", this.onMouseDown);
        document.addEventListener("keydown", this.onKeyDown);
    }
}

class MvcLookupAutocomplete {
    constructor(lookup) {
        const autocomplete = this;

        autocomplete.lookup = lookup;
        autocomplete.element = document.createElement("ul");
        autocomplete.element.className = "mvc-lookup-autocomplete";
        autocomplete.options = {
            minLength: 1,
            addHandler: autocomplete.lookup.group.dataset.addHandler == "True",

            rows: 20,
            sort: lookup.filter.sort,
            order: lookup.filter.order
        };
    }

    search(term) {
        const autocomplete = this;
        const lookup = autocomplete.lookup;

        autocomplete.hide();
        lookup.controller.abort();
        clearTimeout(autocomplete.searchTimerId);
        lookup.group.classList.remove("mvc-lookup-error");

        lookup.fetch({
            search: term,
            selected: lookup.multi ? lookup.selected : [],
            sort: autocomplete.options.sort,
            order: autocomplete.options.order,
            offset: 0,
            rows: autocomplete.options.rows
        }, data => {
            autocomplete.searchTimerId = 0;
            clearTimeout(lookup.loadingTimerId);
            lookup.group.classList.remove("mvc-lookup-error");
            lookup.group.classList.remove("mvc-lookup-loading");

            for (const row of data.rows) {
                const item = document.createElement("li");

                item.innerText = row.Label;
                item.dataset.id = row.Id;

                autocomplete.element.appendChild(item);
                autocomplete.bind(item, [row]);

                if (row == data.rows[0]) {
                    autocomplete.activeItem = item;
                    item.classList.add("active");
                }
            }

            if (!data.rows.length) {
                const noData = document.createElement("li");

                if (autocomplete.options.addHandler && term.length) {
                    noData.className = "mvc-lookup-autocomplete-add";
                    noData.innerText = MvcLookup.lang.add;
                    noData.classList.add("active");

                    noData.addEventListener("mousedown", e => {
                        e.preventDefault();
                    });

                    noData.addEventListener("click", () => {
                        lookup.group.dispatchEvent(new CustomEvent("lookupadd", {
                            detail: { lookup },
                            bubbles: true
                        }));

                        autocomplete.hide();
                    });

                    autocomplete.activeItem = noData;
                } else {
                    noData.className = "mvc-lookup-autocomplete-no-data";
                    noData.innerText = MvcLookup.lang.noData;
                }

                autocomplete.element.appendChild(noData);
            }

            autocomplete.resize();

            document.body.appendChild(autocomplete.element);
        });
    }
    previous() {
        const autocomplete = this;

        if (!autocomplete.element.parentElement || !autocomplete.activeItem) {
            if (!autocomplete.searchTimerId) {
                autocomplete.searchTimerId = 1;

                autocomplete.search(autocomplete.lookup.search.value);
            }

            return;
        }

        autocomplete.activeItem.classList.remove("active");
        autocomplete.activeItem = autocomplete.activeItem.previousElementSibling || autocomplete.element.lastElementChild;
        autocomplete.activeItem.classList.add("active");
    }
    next() {
        const autocomplete = this;

        if (!autocomplete.element.parentElement || !autocomplete.activeItem) {
            if (!autocomplete.searchTimerId) {
                autocomplete.searchTimerId = 1;

                autocomplete.search(autocomplete.lookup.search.value);
            }

            return;
        }

        autocomplete.activeItem.classList.remove("active");
        autocomplete.activeItem = autocomplete.activeItem.nextElementSibling || autocomplete.element.firstElementChild;
        autocomplete.activeItem.classList.add("active");
    }
    hide() {
        const autocomplete = this;

        autocomplete.activeItem = null;
        autocomplete.element.innerHTML = "";

        if (autocomplete.element.parentElement) {
            document.body.removeChild(autocomplete.element);
        }
    }
    resize() {
        const style = this.element.style;
        const control = this.lookup.control.getBoundingClientRect();

        style.left = `${control.left + window.pageXOffset}px`;
        style.width = getComputedStyle(this.lookup.control).width;
        style.top = `${control.bottom + window.pageYOffset - 3}px`;
    }

    bind(item, data) {
        const autocomplete = this;
        const lookup = autocomplete.lookup;

        item.addEventListener("mousedown", e => {
            e.preventDefault();
        });

        item.addEventListener("click", () => {
            if (lookup.multi) {
                lookup.select(lookup.selected.concat(data), true);
            } else {
                lookup.select(data, true);
            }

            autocomplete.hide();
        });

        item.addEventListener("mouseenter", function () {
            if (autocomplete.activeItem) {
                autocomplete.activeItem.classList.remove("active");
            }

            this.classList.add("active");
            autocomplete.activeItem = this;
        });
    }
}

class MvcLookup {
    constructor(element, options = {}) {
        const lookup = this;
        const group = lookup.findLookup(element);

        if (group.dataset.id) {
            return MvcLookup.instances[parseInt(group.dataset.id)].set(options);
        }

        lookup.items = [];
        lookup.group = group;
        lookup.selected = [];
        lookup.for = group.dataset.for;
        lookup.controller = new AbortController();
        lookup.multi = group.dataset.multi == "True";
        lookup.readonly = group.dataset.readonly == "True";
        lookup.url = new URL(group.dataset.url, location.href);
        lookup.options = { searchDelay: 300, loadingDelay: 300 };
        lookup.group.dataset.id = MvcLookup.instances.length.toString();

        lookup.search = group.querySelector(".mvc-lookup-input");
        lookup.browser = group.querySelector(".mvc-lookup-browser");
        lookup.control = group.querySelector(".mvc-lookup-control");
        lookup.error = group.querySelector(".mvc-lookup-control-error");
        lookup.valueContainer = group.querySelector(".mvc-lookup-values");
        lookup.values = [].map.call(lookup.valueContainer.querySelectorAll(".mvc-lookup-value"), value => value);

        lookup.filter = new MvcLookupFilter(lookup);
        lookup.dialog = new MvcLookupDialog(lookup);
        lookup.autocomplete = new MvcLookupAutocomplete(lookup);

        lookup.set(options).reload(false);
        lookup.cleanUp();
        lookup.bind();

        MvcLookup.instances.push(lookup);
    }

    set(options) {
        const lookup = this;

        lookup.options.loadingDelay = options.loadingDelay == null ? lookup.options.loadingDelay : options.loadingDelay;
        lookup.options.searchDelay = options.searchDelay == null ? lookup.options.searchDelay : options.searchDelay;
        lookup.autocomplete.options = Object.assign(lookup.autocomplete.options, options.autocomplete);
        lookup.setReadonly(options.readonly == null ? lookup.readonly : options.readonly);
        lookup.dialog.options = Object.assign(lookup.dialog.options, options.dialog);

        return lookup;
    }
    setReadonly(readonly) {
        const lookup = this;

        lookup.readonly = readonly;

        if (readonly) {
            lookup.search.tabIndex = -1;
            lookup.search.readOnly = true;
            lookup.group.classList.add("mvc-lookup-readonly");

            if (lookup.browser) {
                lookup.browser.tabIndex = -1;
            }
        } else {
            lookup.search.removeAttribute("readonly");
            lookup.search.removeAttribute("tabindex");
            lookup.group.classList.remove("mvc-lookup-readonly");

            if (lookup.browser) {
                lookup.browser.removeAttribute("tabindex");
            }
        }

        lookup.resize();
    }

    browse() {
        const lookup = this;

        if (!lookup.readonly) {
            if (lookup.browser) {
                lookup.browser.blur();
            }

            lookup.group.classList.remove("mvc-lookup-loading");
            lookup.group.classList.remove("mvc-lookup-error");
            clearTimeout(lookup.loadingTimerId);
            lookup.controller.abort();

            lookup.dialog.open();
        }
    }
    reload(triggerChanges = true) {
        const lookup = this;
        const ids = lookup.values.filter(element => element.value);

        if (ids.length) {
            lookup.fetch({ ids: ids, offset: 0, rows: ids.length }, data => {
                lookup.select(data.rows, triggerChanges);
            });
        } else {
            const originalValue = lookup.search.value;

            lookup.select([], triggerChanges);

            if (!lookup.multi && lookup.search.name) {
                lookup.search.value = originalValue;
            }
        }
    }
    select(data, triggerChanges = true) {
        const lookup = this;
        let trigger = triggerChanges;
        const cancelled = !lookup.group.dispatchEvent(new CustomEvent("lookupselect", {
            detail: { lookup, data, triggerChanges },
            cancelable: true,
            bubbles: true
        }));

        if (cancelled) {
            return;
        }

        if (trigger && data.length == lookup.selected.length) {
            trigger = false;

            for (let i = 0; i < data.length && !trigger; i++) {
                trigger = data[i].Id != lookup.selected[i].Id;
            }
        }

        lookup.selected = data;

        if (lookup.multi) {
            lookup.search.value = "";
            lookup.valueContainer.innerHTML = "";

            for (const item of lookup.items) {
                item.parentElement.removeChild(item);
            }

            lookup.items = lookup.createSelectedItems(data);

            for (const item of lookup.items) {
                lookup.control.insertBefore(item, lookup.search);
            }

            lookup.values = lookup.createValues(data);
            lookup.values.forEach(value => lookup.valueContainer.appendChild(value));

            lookup.resize();
        } else if (data.length) {
            lookup.values[0].value = data[0].Id;
            lookup.search.value = data[0].Label;
        } else {
            lookup.values[0].value = "";
            lookup.search.value = "";
        }

        if (trigger) {
            const change = new Event("change");

            lookup.search.dispatchEvent(change);
            lookup.values.forEach(value => value.dispatchEvent(change));
        }
    }
    selectFirst(triggerChanges = true) {
        this.fetch({ search: "", offset: 0, rows: 1 }, data => {
            this.select(data.rows, triggerChanges);
        });
    }
    selectSingle(triggerChanges = true) {
        this.fetch({ search: "", offset: 0, rows: 2 }, data => {
            if (data.rows.length == 1) {
                this.select(data.rows, triggerChanges);
            } else {
                this.select([], triggerChanges);
            }
        });
    }

    fetch(search, resolved) {
        const lookup = this;

        lookup.controller.abort();
        lookup.controller = new AbortController();
        lookup.loadingTimerId = setTimeout(() => {
            lookup.group.classList.add("mvc-lookup-loading");
        }, lookup.options.loadingDelay);
        lookup.group.classList.remove("mvc-lookup-error");

        fetch(lookup.filter.formUrl(search), {
            signal: lookup.controller.signal,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).then(response => {
            if (response.ok) {
                return response.json();
            }

            return Promise.reject(new Error(`Invalid response status: ${response.status}`));
        }).then(data => {
            resolved(data);

            clearTimeout(lookup.loadingTimerId);

            lookup.group.classList.remove("mvc-lookup-loading");
        }).catch(reason => {
            if (reason.name == "AbortError") {
                return Promise.resolve();
            }

            clearTimeout(lookup.loadingTimerId);

            lookup.error.title = MvcLookup.lang.error;
            lookup.group.classList.add("mvc-lookup-error");
            lookup.group.classList.remove("mvc-lookup-loading");

            return Promise.reject(reason);
        });
    }

    createSelectedItems(data) {
        return data.map(selection => {
            const button = document.createElement("button");

            button.className = "mvc-lookup-deselect";
            button.innerText = "×";
            button.type = "button";

            const item = document.createElement("div");

            item.innerText = selection.Label || "";
            item.className = "mvc-lookup-item";
            item.appendChild(button);

            this.bindDeselect(button, selection.Id);

            return item;
        });
    }
    createValues(data) {
        return data.map(value => {
            const input = document.createElement("input");

            input.className = "mvc-lookup-value";
            input.value = value.Id;
            input.type = "hidden";
            input.name = this.for;

            return input;
        });
    }
    bindDeselect(close, id) {
        close.addEventListener("click", () => {
            this.select(this.selected.filter(value => value.Id != id), true);

            this.search.focus();
        });
    }
    findLookup(element) {
        const lookup = element.closest(".mvc-lookup");

        if (!lookup) {
            throw new Error("Lookup can only be created from within mvc-lookup structure.");
        }

        return lookup;
    }

    cleanUp() {
        const data = this.group.dataset;

        delete data.readonly;
        delete data.filters;
        delete data.dialog;
        delete data.search;
        delete data.multi;
        delete data.order;
        delete data.title;
        delete data.rows;
        delete data.sort;
        delete data.url;
    }
    resize() {
        const lookup = this;

        if (lookup.items.length) {
            let style = getComputedStyle(lookup.control);
            let contentWidth = lookup.control.clientWidth;
            const lastItem = lookup.items[lookup.items.length - 1];

            contentWidth -= parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);
            let widthLeft = Math.floor(contentWidth - lastItem.offsetLeft - lastItem.offsetWidth);

            if (widthLeft > contentWidth / 3) {
                style = getComputedStyle(lookup.search);
                widthLeft -= parseFloat(style.marginLeft) + parseFloat(style.marginRight) + 5;
                lookup.search.style.width = `${widthLeft}px`;
            } else {
                lookup.search.style.width = "";
            }
        } else {
            lookup.search.style.width = "";
        }
    }
    bind() {
        const lookup = this;
        const autocomplete = lookup.autocomplete;

        window.addEventListener("resize", () => {
            autocomplete.resize();
            lookup.resize();
        });

        lookup.search.addEventListener("focus", () => {
            lookup.group.classList.add("mvc-lookup-focus");
        });

        lookup.search.addEventListener("blur", function () {
            const originalValue = this.value;

            autocomplete.hide();
            lookup.group.classList.remove("mvc-lookup-error");
            lookup.group.classList.remove("mvc-lookup-focus");

            if (autocomplete.searchTimerId) {
                lookup.group.classList.remove("mvc-lookup-loading");
                clearTimeout(autocomplete.searchTimerId);
                clearTimeout(lookup.loadingTimerId);
                autocomplete.searchTimerId = 0;
                lookup.controller.abort();
            }

            if (!lookup.multi && lookup.selected.length) {
                if (lookup.selected[0].Label != this.value) {
                    lookup.select([], true);
                }
            } else {
                this.value = "";
            }

            if (!lookup.multi && lookup.search.name) {
                this.value = originalValue;
            }
        });

        lookup.search.addEventListener("keydown", function (e) {
            switch (e.which) {
                case 8:
                    if (!this.value.length && lookup.selected.length) {
                        lookup.select(lookup.selected.slice(0, -1), true);
                    }

                    break;
                case 9:
                    if (autocomplete.activeItem) {
                        if (lookup.browser) {
                            lookup.browser.tabIndex = -1;

                            setTimeout(() => {
                                lookup.browser.removeAttribute("tabindex");
                            }, 100);
                        }

                        autocomplete.activeItem.click();
                    }

                    break;
                case 13:
                    if (autocomplete.activeItem) {
                        e.preventDefault();

                        autocomplete.activeItem.click();
                    }

                    break;
                case 38:
                    e.preventDefault();

                    autocomplete.previous();

                    break;
                case 40:
                    e.preventDefault();

                    autocomplete.next();

                    break;
            }
        });

        lookup.search.addEventListener("input", function () {
            if (!this.value.length && !lookup.multi && lookup.selected.length) {
                lookup.select([], true);
            }

            autocomplete.hide();
            lookup.controller.abort();
            clearTimeout(lookup.loadingTimerId);
            clearTimeout(autocomplete.searchTimerId);
            lookup.group.classList.remove("mvc-lookup-error");
            lookup.group.classList.remove("mvc-lookup-loading");

            if (!lookup.readonly && autocomplete.options.minLength <= this.value.length) {
                autocomplete.searchTimerId = setTimeout(() => {
                    autocomplete.search(this.value);
                }, lookup.options.searchDelay);
            }
        });

        if (lookup.browser) {
            lookup.browser.addEventListener("click", () => {
                lookup.browse();
            });
        }

        for (const additional of lookup.filter.additional) {
            for (const input of document.querySelectorAll(`[name="${additional}"]`)) {
                input.addEventListener("change", () => {
                    const cancelled = !input.dispatchEvent(new CustomEvent("filterchange", {
                        detail: { lookup },
                        cancelable: true,
                        bubbles: true
                    }));

                    if (cancelled) {
                        return;
                    }

                    lookup.filter.offset = 0;

                    const ids = lookup.values.filter(element => element.value);

                    if (ids.length || lookup.selected.length) {
                        lookup.fetch({ checkIds: ids, offset: 0, rows: ids.length }, data => {
                            lookup.select(data.rows, true);
                        });
                    }
                });
            }
        }
    }
}

MvcLookup.instances = [];
MvcLookup.lang = {
    add: "+ Add",
    more: "More...",
    search: "Search...",
    select: "Select ({0})",
    noData: "No data found",
    error: "Error while retrieving records"
};
