var Commands;
(function (Commands) {
    Commands.FONT_NAME = 'fontname';
    Commands.JUSTIFY_LEFT = 'justifyLeft';
    Commands.JUSTIFY_CENTER = 'justifyCenter';
})(Commands || (Commands = {}));
var Utils;
(function (Utils) {
    let Obj;
    (function (Obj) {
        function enumerate(obj, f) {
            const props = Object.keys(obj);
            for (let k = 0; k < props.length; ++k) {
                const i = props[k];
                const x = obj[i];
                f(x, i);
            }
        }
        Obj.enumerate = enumerate;
        function filter(obj, pred) {
            const r = {};
            enumerate(obj, (x, i) => {
                if (pred(x, i)) {
                    r[i] = x;
                }
            });
            return r;
        }
        Obj.filter = filter;
    })(Obj = Utils.Obj || (Utils.Obj = {}));
    let Arr;
    (function (Arr) {
        function contains(arr, value) {
            return Array.prototype.indexOf.call(arr, value) > -1;
        }
        Arr.contains = contains;
    })(Arr = Utils.Arr || (Utils.Arr = {}));
    let NodeType;
    (function (NodeType) {
        function isText(node) {
            return !!node && node.nodeType === 3;
        }
        NodeType.isText = isText;
    })(NodeType = Utils.NodeType || (Utils.NodeType = {}));
    class TreeWalker {
        constructor(startNode, rootNode) {
            this.node = startNode;
            this.rootNode = rootNode;
            this.current = this.current.bind(this);
        }
        current() {
            return this.node;
        }
        next(shallow) {
            this.node = this.findSibling(this.node, 'firstChild', 'nextSibling', shallow);
            return this.node;
        }
        prev(shallow) {
            this.node = this.findSibling(this.node, 'lastChild', 'previousSibling', shallow);
            return this.node;
        }
        findSibling(node, startName, siblingName, shallow) {
            let sibling, parent;
            if (node) {
                if (!shallow && node[startName]) {
                    return node[startName];
                }
                if (node !== this.rootNode) {
                    sibling = node[siblingName];
                    if (sibling) {
                        return sibling;
                    }
                    for (parent = node.parentNode; parent && parent !== this.rootNode; parent = parent.parentNode) {
                        sibling = parent[siblingName];
                        if (sibling) {
                            return sibling;
                        }
                    }
                }
            }
        }
        findPreviousNode(node, startName, siblingName, shallow) {
            let sibling, parent, child;
            if (node) {
                sibling = node[siblingName];
                if (this.rootNode && sibling === this.rootNode) {
                    return;
                }
                if (sibling) {
                    if (!shallow) {
                        for (child = sibling[startName]; child; child = child[startName]) {
                            if (!child[startName]) {
                                return child;
                            }
                        }
                    }
                    return sibling;
                }
                parent = node.parentNode;
                if (parent && parent !== this.rootNode) {
                    return parent;
                }
            }
        }
    }
    Utils.TreeWalker = TreeWalker;
    function debounce(interval, callback) {
        let debounceTimeoutId;
        return (...args) => {
            clearTimeout(debounceTimeoutId);
            debounceTimeoutId = window.setTimeout(() => callback.apply(this, args), interval);
        };
    }
    Utils.debounce = debounce;
    const WHITESPACE_REGEX = /^\s*|\s*$/g;
    function trim(str) {
        return (str === null || str === undefined) ? '' : ('' + str).replace(WHITESPACE_REGEX, '');
    }
    Utils.trim = trim;
    function makeMap(items, delim, map) {
        items = items || [];
        delim = delim || ',';
        if (typeof items === 'string') {
            items = items.split(delim);
        }
        map = map || {};
        let i = items.length;
        while (i--) {
            map[items[i]] = {};
        }
        return map;
    }
    Utils.makeMap = makeMap;
    function getNodeIndex(node, normalized) {
        let idx = 0, lastNodeType, nodeType;
        if (node) {
            for (lastNodeType = node.nodeType, node = node.previousSibling; node; node = node.previousSibling) {
                nodeType = node.nodeType;
                if (normalized && nodeType === 3) {
                    if (nodeType === lastNodeType || !node.nodeValue.length) {
                        continue;
                    }
                }
                idx++;
                lastNodeType = nodeType;
            }
        }
        return idx;
    }
    Utils.getNodeIndex = getNodeIndex;
})(Utils || (Utils = {}));
var SelectionUtils;
(function (SelectionUtils) {
    SelectionUtils.MOVE_CARET_BEFORE_ON_ENTER_ELEMENTS_MAP = Utils.makeMap('td th iframe video audio object script code table ' +
        'area base basefont br col frame hr img input isindex link ' +
        'meta param embed source wbr track', ' ');
    function moveEndPoint(rng, node, start) {
        const root = node, walker = new Utils.TreeWalker(node, root);
        const moveCaretBeforeOnEnterElementsMap = Utils.Obj.filter(SelectionUtils.MOVE_CARET_BEFORE_ON_ENTER_ELEMENTS_MAP, (_, key) => {
            return !Utils.Arr.contains(['td', 'th', 'table'], key);
        });
        do {
            if (Utils.NodeType.isText(node) && Utils.trim(node.nodeValue).length !== 0) {
                if (start) {
                    rng.setStart(node, 0);
                }
                else {
                    rng.setEnd(node, node.nodeValue.length);
                }
                return;
            }
            if (moveCaretBeforeOnEnterElementsMap[node.nodeName]) {
                if (start) {
                    rng.setStartBefore(node);
                }
                else {
                    if (node.nodeName === 'BR') {
                        rng.setEndBefore(node);
                    }
                    else {
                        rng.setEndAfter(node);
                    }
                }
                return;
            }
        } while ((node = (start ? walker.next() : walker.prev())));
        if (root.nodeName === 'BODY') {
            if (start) {
                rng.setStart(root, 0);
            }
            else {
                rng.setEnd(root, root.childNodes.length);
            }
        }
    }
    SelectionUtils.moveEndPoint = moveEndPoint;
    function isNativeIESelection(rng) {
        return !!(rng).select;
    }
    function isAttachedToDom(node) {
        return !!(node && node.ownerDocument) && node.ownerDocument.contains(node);
    }
    function isValidRange(rng) {
        if (!rng) {
            return false;
        }
        else if (isNativeIESelection(rng)) {
            return true;
        }
        else {
            return isAttachedToDom(rng.startContainer) && isAttachedToDom(rng.endContainer);
        }
    }
    function selectElement(node, content) {
        const idx = Utils.getNodeIndex(node);
        const rng = document.createRange();
        rng.setStart(node.parentNode, idx);
        rng.setEnd(node.parentNode, idx + 1);
        if (content) {
            moveEndPoint(rng, node, true);
            moveEndPoint(rng, node, false);
        }
        return rng;
    }
    SelectionUtils.selectElement = selectElement;
    function getRange() {
        const sel = document.getSelection();
        const rng = sel.rangeCount > 0 ? sel.getRangeAt(0) : document.createRange();
        if (rng.setStart && rng.startContainer.nodeType === 9 && rng.collapsed) {
            const body = document.body;
            rng.setStart(body, 0);
            rng.setEnd(body, 0);
        }
        return rng;
    }
    SelectionUtils.getRange = getRange;
    function setRange(rng, forward) {
        if (!isValidRange(rng)) {
            return;
        }
        const ieRange = isNativeIESelection(rng) ? rng : null;
        if (ieRange) {
            try {
                ieRange.select();
            }
            catch (ex) { }
            return;
        }
        let sel = document.getSelection();
        if (sel) {
            try {
                sel.removeAllRanges();
                sel.addRange(rng);
            }
            catch (ex) { }
            if (forward === false && sel.extend) {
                sel.collapse(rng.endContainer, rng.endOffset);
                sel.extend(rng.startContainer, rng.startOffset);
            }
        }
    }
    SelectionUtils.setRange = setRange;
    function select(node, content) {
        const range = selectElement(node, content);
        setRange(range);
        return node;
    }
    SelectionUtils.select = select;
    function collapse(toStart) {
        const rng = getRange();
        rng.collapse(!!toStart);
        setRange(rng);
    }
    SelectionUtils.collapse = collapse;
})(SelectionUtils || (SelectionUtils = {}));
var Table;
(function (Table) {
    function findParentTable(element) {
        let parent = element;
        while (parent) {
            if (parent.matches('table')) {
                return parent;
            }
            parent = parent.parentElement;
        }
        return null;
    }
    Table.findParentTable = findParentTable;
    function createTable(rowCount, columnCount, id) {
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
    Table.createTable = createTable;
    function placeCaretInCell(cell) {
        SelectionUtils.select(cell, true);
        SelectionUtils.collapse(true);
    }
    function selectFirstCellInTable(tbl) {
        const cell = tbl.querySelector('td,th');
        placeCaretInCell(cell);
    }
    function insertTable(rowCount, columnCount) {
        const id = 'new-table';
        let tbl = createTable(rowCount, columnCount, id);
        document.execCommand('insertHtml', false, tbl.outerHTML);
        tbl = document.getElementById(id);
        tbl.removeAttribute('id');
        tbl.addEventListener('mouseenter', function (event) {
            console.log('mouseenter');
            tbl.addEventListener('mouseleave', function (event) {
                console.log('mouseleave');
            }, { once: true });
        });
        selectFirstCellInTable(tbl);
    }
    Table.insertTable = insertTable;
})(Table || (Table = {}));
var Format;
(function (Format) {
    let TextAlignment;
    (function (TextAlignment) {
        TextAlignment[TextAlignment["Left"] = 0] = "Left";
        TextAlignment[TextAlignment["Center"] = 1] = "Center";
        TextAlignment[TextAlignment["Right"] = 2] = "Right";
        TextAlignment[TextAlignment["Justified"] = 3] = "Justified";
    })(TextAlignment = Format.TextAlignment || (Format.TextAlignment = {}));
    let ListType;
    (function (ListType) {
        ListType[ListType["None"] = 0] = "None";
        ListType[ListType["Bullets"] = 1] = "Bullets";
        ListType[ListType["Numbering"] = 2] = "Numbering";
    })(ListType = Format.ListType || (Format.ListType = {}));
    function getFontNames() {
        const fontNames = document.queryCommandValue(Commands.FONT_NAME);
        if (fontNames !== null) {
            return fontNames.split(",").map(x => x.replace(/['"]+/g, ''));
        }
        return null;
    }
    function getAlignment() {
        if (document.queryCommandValue('justifyCenter')) {
            return TextAlignment.Center;
        }
        else if (document.queryCommandValue('justifyRight')) {
            return TextAlignment.Right;
        }
        else if (document.queryCommandValue('justifyFull')) {
            return TextAlignment.Justified;
        }
        return TextAlignment.Left;
    }
    function getListType() {
        if (document.queryCommandValue('insertUnorderedList')) {
            return ListType.Bullets;
        }
        else if (document.queryCommandValue('insertOrderedList')) {
            return ListType.Numbering;
        }
        return ListType.None;
    }
    function queryIsTable() {
        const selection = document.getSelection();
        if (selection.anchorNode instanceof HTMLElement) {
            return Table.findParentTable(selection.anchorNode) !== null;
        }
        else {
            return Table.findParentTable(selection.anchorNode.parentElement) !== null;
        }
    }
    function getCurrentFormat() {
        return {
            isBold: document.queryCommandState('bold'),
            isItalic: document.queryCommandState('italic'),
            isUnderlined: document.queryCommandState('underline'),
            isStrikethrough: document.queryCommandState('strikethrough'),
            isSuperscript: document.queryCommandState('superscript'),
            isSubscript: document.queryCommandState('subscript'),
            fontNames: getFontNames(),
            fontSize: parseInt(document.queryCommandValue('fontsize')),
            foreground: document.queryCommandValue('forecolor'),
            background: document.queryCommandValue('backcolor'),
            alignment: getAlignment(),
            listType: getListType(),
            isTable: queryIsTable(),
        };
    }
    Format.getCurrentFormat = getCurrentFormat;
    function setCurrentFormat(formatName, value) {
        try {
            document.execCommand(formatName, false, value);
        }
        catch (error) {
            console.log(error);
        }
        return getCurrentFormat();
    }
    Format.setCurrentFormat = setCurrentFormat;
})(Format || (Format = {}));
function getCurrentFormat() {
    return JSON.stringify(Format.getCurrentFormat());
}
function setCurrentFormat(formatName, value) {
    return JSON.stringify(Format.setCurrentFormat(formatName, value));
}
function insertTable(rowCount, columnCount) {
    Table.insertTable(parseInt(rowCount), parseInt(columnCount));
}
function getHtml() {
    document.designMode = 'off';
    document.body.removeAttribute('contentEditable');
    return document.documentElement.outerHTML;
}
document.designMode = 'on';
document.body.contentEditable = 'true';
document.addEventListener('selectionchange', Utils.debounce(100, () => {
    window.external.notify(getCurrentFormat());
}));
//# sourceMappingURL=HTMLEditor.js.map