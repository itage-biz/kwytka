function formatTableRows(container) {
    const countColumns = (container.dataset.countColumns ?? '')
        .split(',')
        .map(value => value.trim().toLocaleLowerCase())
        .filter(Boolean);

    container.querySelectorAll('table').forEach(table => {
        table.querySelectorAll('.count-column').forEach(cell => cell.classList.remove('count-column'));

        const headerCells = table.querySelector('tr:has(th)')?.querySelectorAll('th') ?? [];
        const matchingIndexes = [...headerCells]
            .map((header, index) => countColumns.some(value => header.textContent.toLocaleLowerCase().includes(value)) ? index : -1)
            .filter(index => index >= 0);

        table.querySelectorAll('tr').forEach(row => {
            const isEmpty = [...row.children]
                .every(cell => !cell.textContent.replaceAll('\u00a0', ' ').trim());

            row.classList.toggle('empty-row', isEmpty);
            matchingIndexes.forEach(index => row.children[index]?.classList.add('count-column'));
        });
    });
}

function formatAllTables() {
    document.querySelectorAll('[data-count-columns]').forEach(formatTableRows);
}

formatAllTables();

new MutationObserver(formatAllTables).observe(document.body, {
    attributes: true,
    attributeFilter: ['data-count-columns'],
    childList: true,
    subtree: true
});
