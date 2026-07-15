document.querySelectorAll('[data-price-list-search]').forEach(search => {
    const page = search.closest('.price-list-page, .sale-page');
    const input = search.querySelector('input');
    const clearButton = search.querySelector('button');
    const emptyMessage = search.querySelector('.price-list-search-empty');

    function filterRows() {
        const query = input.value.trim().toLocaleLowerCase();
        let visibleRowCount = 0;

        page.querySelectorAll('table tr').forEach(row => {
            if (!row.querySelector('th')) {
                row.hidden = query.length > 0 && !row.textContent.toLocaleLowerCase().includes(query);
                if (!row.hidden) {
                    visibleRowCount += 1;
                }
            }
        });

        clearButton.disabled = query.length === 0;
        emptyMessage.hidden = query.length === 0 || visibleRowCount > 0;
    }

    input.addEventListener('input', filterRows);
    clearButton.addEventListener('click', () => {
        input.value = '';
        filterRows();
        input.focus();
    });
});
