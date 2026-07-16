const activeTables = new Map();

function cleanCell(value) {
    return value.replaceAll("\u00a0", " ").trim();
}

function normalizeRows(rows) {
    const columnCount = Math.max(0, ...rows.map((row) => row.length));

    return rows.map((row) => [
        ...row.map(cleanCell),
        ...Array.from({length: columnCount - row.length}).fill("")
    ]);
}

function parseTabSeparatedText(text) {
    if (!text) {
        return [];
    }

    const rows = [[]];
    let cell = "";
    let quoted = false;

    for (let index = 0; index < text.length; index++) {
        const character = text[index];

        if (character === '"' && quoted) {
            if (quoted && text[index + 1] === '"') {
                cell += '"';
                index++;
            } else {
                quoted = false;
            }
        } else if (character === '"' && cell === "") {
            quoted = true;
        } else if (character === "\t" && !quoted) {
            rows.at(-1).push(cell);
            cell = "";
        } else if ((character === "\n" || character === "\r") && !quoted) {
            if (character === "\r" && text[index + 1] === "\n") {
                index++;
            }

            rows.at(-1).push(cell);
            rows.push([]);
            cell = "";
        } else {
            cell += character;
        }
    }

    rows.at(-1).push(cell);

    if (rows.length > 1 && rows.at(-1).length === 1 && rows.at(-1)[0] === "") {
        rows.pop();
    }

    return normalizeRows(rows);
}

function parseHTMLTable(html) {
    if (!html || typeof DOMParser === "undefined") {
        return [];
    }

    const startMarker = "<!--StartFragment-->";
    const endMarker = "<!--EndFragment-->";
    const fragmentStart = html.indexOf(startMarker);
    const fragmentEnd = html.indexOf(endMarker);
    let source = html;

    if (fragmentStart !== -1 && fragmentEnd > fragmentStart) {
        const fragment = html.slice(fragmentStart + startMarker.length, fragmentEnd);
        const firstRowIndex = fragment.search(/<tr(?:\s|>)/i);
        const firstTableIndex = fragment.search(/<table(?:\s|>)/i);

        if (firstRowIndex !== -1 && (firstTableIndex === -1 || firstRowIndex < firstTableIndex)) {
            source = `<table>${fragment}</table>`;
        }
    }

    const document = new DOMParser().parseFromString(source, "text/html");
    const table = document.querySelector("table");

    if (!table) {
        return [];
    }

    const rows = Array.from(table.querySelectorAll(":scope > thead > tr, :scope > tbody > tr, :scope > tfoot > tr, :scope > tr")).map((row) =>
        Array.from(row.querySelectorAll(":scope > th, :scope > td")).map((cell) => ({
            value: cell.textContent ?? "",
            colspan: cell.getAttribute("colspan"),
            rowspan: cell.getAttribute("rowspan")
        }))
    );

    return rows.filter((row) => row.length > 0);
}

function parseSpreadsheetClipboard(plainText, html) {
    const htmlRows = parseHTMLTable(html);
    return htmlRows.length > 0 ? htmlRows : parseTabSeparatedText(plainText);
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;")
        .replaceAll("`", "&#96;");
}

function toCellHtml(cell, tagName) {
    const value = typeof cell === "string" ? cell : cell.value;
    const colspan = typeof cell === "string" ? null : cell.colspan;
    const rowspan = typeof cell === "string" ? null : cell.rowspan;
    const colspanAttribute = colspan ? ` colspan="${escapeHtml(colspan)}"` : "";
    const rowspanAttribute = rowspan ? ` rowspan="${escapeHtml(rowspan)}"` : "";

    return `<${tagName}${colspanAttribute}${rowspanAttribute}>${escapeHtml(value)}</${tagName}>`;
}

function toTableHtml(rows) {
    if (rows.length === 0) {
        return "";
    }

    const headerRow = rows[0];
    const bodyRows = rows.slice(1);

    const header = `<thead><tr>${headerRow.map((cell) => toCellHtml(cell, "th")).join("")}</tr></thead>`;
    const body = bodyRows.length > 0
        ? `<tbody>${bodyRows.map((row) => `<tr>${row.map((cell) => toCellHtml(cell, "td")).join("")}</tr>`).join("")}</tbody>`
        : "";

    return `<table>${header}${body}</table>`;
}

function handlePaste(event, state) {
    event.preventDefault();
    const clipboardData = event.clipboardData;
    const html = clipboardData?.getData("text/html") ?? "";
    const plainText = clipboardData?.getData("text/plain") ?? "";
    const tableData = parseSpreadsheetClipboard(plainText, html);
    const tableHtml = toTableHtml(tableData);

    void state.dotNetReference.invokeMethodAsync("NotifyValueChanged", tableHtml).catch(() => {
        // Ignore disconnected/teardown races.
    });
}

export function initialize(textareaElement, dotNetReference) {
    const handler = (event) => handlePaste(event, state);
    const state = {
        textareaElement,
        dotNetReference,
        handler
    };

    textareaElement.addEventListener("paste", handler);
    activeTables.set(textareaElement, state);
}

export function dispose(textareaElement) {
    const state = activeTables.get(textareaElement);
    if (!state) {
        return;
    }

    textareaElement.removeEventListener("paste", state.handler);
    activeTables.delete(textareaElement);
}
