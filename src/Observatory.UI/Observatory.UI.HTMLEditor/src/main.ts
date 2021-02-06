/// <reference path='utils.ts'/>
/// <reference path='format.ts'/>

interface External {
    notify(msg: string);
}

function getCurrentFormat(): string {
    return JSON.stringify(Format.getCurrentFormat());
}

function setCurrentFormat(formatName: string, value?: string): string {
    return JSON.stringify(Format.setCurrentFormat(formatName, value));
}

function insertTable(rowCount: string, columnCount: string) {
    Table.insertTable(parseInt(rowCount), parseInt(columnCount));
}

function getHtml(): string {
    document.designMode = 'off';
    document.body.removeAttribute('contentEditable');
    return document.documentElement.outerHTML;
}

document.designMode = 'on';
document.body.contentEditable = 'true';

document.addEventListener('selectionchange', Utils.debounce(100, () => {
    window.external.notify(getCurrentFormat());
}));