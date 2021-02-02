const ALIGN_LEFT = 0;
const ALIGN_CENTER = 1;
const ALIGN_RIGHT = 2;
const ALIGN_JUSTIFIED = 3;

const LIST_TYPE_NONE = 0;
const LIST_TYPE_BULLETS = 1;
const LIST_TYPE_NUMBERING = 2;

function debounce(interval, callback) {
    let debounceTimeoutId;
    return function (...args) {
        clearTimeout(debounceTimeoutId);
        debounceTimeoutId = setTimeout(() => callback.apply(this, args), interval);
    };
}

function getCurrentFormat() {
    //const selection = document.getSelection();

    // get the font size in pt
    //let fontSizeStr = window.getComputedStyle(selection.anchorNode.parentElement, null).fontSize;
    //let fontSize = parseInt(fontSizeStr.substring(0, fontSizeStr.length - 2)) * 72 / 96;

    // get font names as array of string
    let fontNamesStr = document.queryCommandValue('fontname');
    let fontNames = [];
    if (fontNamesStr !== null) {
        fontNames = fontNamesStr.split(",").map(x => x.replace(/['"]+/g, ''));
    }

    // get alignment
    let alignment = ALIGN_LEFT;
    if (document.queryCommandValue('justifyCenter')) {
        alignment = ALIGN_CENTER;
    } else if (document.queryCommandValue('justifyRight')) {
        alignment = ALIGN_RIGHT;
    } else if (document.queryCommandValue('justifyFull')) {
        alignment = ALIGN_JUSTIFIED;
    }

    let listType = LIST_TYPE_NONE;
    if (document.queryCommandValue('insertUnorderedList')) {
        listType = LIST_TYPE_BULLETS;
    } else if (document.queryCommandValue('insertOrderedList')) {
        listType = LIST_TYPE_NUMBERING;
    }

    return JSON.stringify({
        isBold: document.queryCommandValue('bold'),
        isItalic: document.queryCommandValue('italic'),
        isUnderlined: document.queryCommandValue('underline'),
        isStrikethrough: document.queryCommandValue('strikethrough'),
        isSuperscript: document.queryCommandValue('superscript'),
        isSubscript: document.queryCommandValue('subscript'),
        fontNames: fontNames,
        fontSize: parseInt(document.queryCommandValue('fontsize')),
        foreground: document.queryCommandValue('forecolor'),
        background: document.queryCommandValue('backcolor'),
        alignment: alignment,
        listType: listType,
    });
}

function setCurrentFormat(formatName, value = undefined) {
    try {
        document.execCommand(formatName, false, value);
    } catch (error) {
        console.log(error);
    }
    return getCurrentFormat();
}

function getHtml() {
    document.designMode = 'off';
    document.body.removeAttribute('contenteditable');
    return document.documentElement.outerHTML;
}

function insertTable(rowCount, columnCount) {
    rowCount = parseInt(rowCount);
    columnCount = parseInt(columnCount);

    const tbl = document.createElement('table');
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

    document.execCommand('insertHtml', false, tbl.outerHTML);
}

document.designMode = 'on';
document.body.contentEditable = true;

document.addEventListener('selectionchange', debounce(100, function () {
    window.external.notify(getCurrentFormat());
}));