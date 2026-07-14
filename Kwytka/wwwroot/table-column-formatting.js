function formatCountColumns(container) {
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
            matchingIndexes.forEach(index => row.children[index]?.classList.add('count-column'));
        });
    });
}

function formatAllCountColumns() {
    document.querySelectorAll('[data-count-columns]').forEach(formatCountColumns);
}

formatAllCountColumns();

new MutationObserver(formatAllCountColumns).observe(document.body, {
    attributes: true,
    attributeFilter: ['data-count-columns'],
    childList: true,
    subtree: true
});
