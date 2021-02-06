/// <reference path='selection.ts'/>

namespace Table {
    export function findParentTable(element: HTMLElement) {
        let parent = element;
        while (parent) {
            if (parent.matches('table')) {
                return parent;
            }
            parent = parent.parentElement;
        }
        return null;
    }

    export function createTable(rowCount: number, columnCount: number, id: string): HTMLTableElement {
        const tbl = document.createElement('table');
        tbl.id = id;
        tbl.border = '1';
        tbl.style.width = '100%';
        tbl.style.borderCollapse = 'collapse';

        const columnWidth = 100 / columnCount + '%';

        for (let i = 0; i < rowCount; i++) {
            const tr = tbl.insertRow();
            for (let j = 0; j < columnCount; j++) {
                const td = tr.insertCell();
                td.appendChild(document.createElement('br'));
                td.style.padding = '4px';
                td.style.width = columnWidth;
            }
        }

        return tbl;
    }

    function placeCaretInCell(cell: HTMLTableDataCellElement | HTMLTableHeaderCellElement) {
        SelectionUtils.select(cell, true);
        SelectionUtils.collapse(true);
    }

    function selectFirstCellInTable(tbl: HTMLTableElement) {
        const cell = tbl.querySelector('td,th') as HTMLTableDataCellElement | HTMLTableHeaderCellElement;
        placeCaretInCell(cell);
    }

    export function insertTable(rowCount: number, columnCount: number) {
        const id = 'new-table';
        let tbl = createTable(rowCount, columnCount, id);
        document.execCommand('insertHtml', false, tbl.outerHTML);

        tbl = document.getElementById(id) as HTMLTableElement;
        tbl.removeAttribute('id');
        tbl.addEventListener('mouseenter', function (event) {
            console.log('mouseenter');
            tbl.addEventListener('mouseleave', function (event) {
                console.log('mouseleave');
            }, { once: true });
        });
        selectFirstCellInTable(tbl);
    }
}