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
        function find(arr, pred) {
            for (let i = 0; i < arr.length; ++i) {
                const x = arr[i];
                if (pred(x)) {
                    return x;
                }
            }
            return null;
        }
        Arr.find = find;
    })(Arr = Utils.Arr || (Utils.Arr = {}));
    let Strings;
    (function (Strings) {
        function contains(str, substr) {
            return str.indexOf(substr) !== -1;
        }
        Strings.contains = contains;
    })(Strings = Utils.Strings || (Utils.Strings = {}));
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
var Platform;
(function (Platform) {
    const Strings = Utils.Strings;
    const Arr = Utils.Arr;
    let PlatformInfo;
    (function (PlatformInfo) {
        const normalVersionRegex = /.*?version\/\ ?([0-9]+)\.([0-9]+).*/;
        function checkContains(target) {
            return (uastring) => {
                return Strings.contains(uastring, target);
            };
        }
        ;
        PlatformInfo.browsers = [
            {
                name: 'Edge',
                versionRegexes: [/.*?edge\/ ?([0-9]+)\.([0-9]+)$/],
                search: (uastring) => {
                    return Strings.contains(uastring, 'edge/') && Strings.contains(uastring, 'chrome') && Strings.contains(uastring, 'safari') && Strings.contains(uastring, 'applewebkit');
                }
            },
            {
                name: 'Chrome',
                versionRegexes: [/.*?chrome\/([0-9]+)\.([0-9]+).*/, normalVersionRegex],
                search: (uastring) => {
                    return Strings.contains(uastring, 'chrome') && !Strings.contains(uastring, 'chromeframe');
                }
            },
            {
                name: 'IE',
                versionRegexes: [/.*?msie\ ?([0-9]+)\.([0-9]+).*/, /.*?rv:([0-9]+)\.([0-9]+).*/],
                search: (uastring) => {
                    return Strings.contains(uastring, 'msie') || Strings.contains(uastring, 'trident');
                }
            },
            {
                name: 'Opera',
                versionRegexes: [normalVersionRegex, /.*?opera\/([0-9]+)\.([0-9]+).*/],
                search: checkContains('opera')
            },
            {
                name: 'Firefox',
                versionRegexes: [/.*?firefox\/\ ?([0-9]+)\.([0-9]+).*/],
                search: checkContains('firefox')
            },
            {
                name: 'Safari',
                versionRegexes: [normalVersionRegex, /.*?cpu os ([0-9]+)_([0-9]+).*/],
                search: (uastring) => {
                    return (Strings.contains(uastring, 'safari') || Strings.contains(uastring, 'mobile/')) && Strings.contains(uastring, 'applewebkit');
                }
            }
        ];
        PlatformInfo.oses = [
            {
                name: 'Windows',
                search: checkContains('win'),
                versionRegexes: [/.*?windows\ nt\ ?([0-9]+)\.([0-9]+).*/]
            },
            {
                name: 'iOS',
                search: (uastring) => {
                    return Strings.contains(uastring, 'iphone') || Strings.contains(uastring, 'ipad');
                },
                versionRegexes: [/.*?version\/\ ?([0-9]+)\.([0-9]+).*/, /.*cpu os ([0-9]+)_([0-9]+).*/, /.*cpu iphone os ([0-9]+)_([0-9]+).*/]
            },
            {
                name: 'Android',
                search: checkContains('android'),
                versionRegexes: [/.*?android\ ?([0-9]+)\.([0-9]+).*/]
            },
            {
                name: 'OSX',
                search: checkContains('mac os x'),
                versionRegexes: [/.*?mac\ os\ x\ ?([0-9]+)_([0-9]+).*/]
            },
            {
                name: 'Linux',
                search: checkContains('linux'),
                versionRegexes: []
            },
            {
                name: 'Solaris',
                search: checkContains('sunos'),
                versionRegexes: []
            },
            {
                name: 'FreeBSD',
                search: checkContains('freebsd'),
                versionRegexes: []
            },
            {
                name: 'ChromeOS',
                search: checkContains('cros'),
                versionRegexes: [/.*?chrome\/([0-9]+)\.([0-9]+).*/]
            }
        ];
    })(PlatformInfo = Platform.PlatformInfo || (Platform.PlatformInfo = {}));
    let Version;
    (function (Version) {
        function firstMatch(regexes, s) {
            for (let i = 0; i < regexes.length; i++) {
                const x = regexes[i];
                if (x.test(s)) {
                    return x;
                }
            }
            return undefined;
        }
        Version.firstMatch = firstMatch;
        ;
        function find(regexes, agent) {
            const r = firstMatch(regexes, agent);
            if (!r) {
                return { major: 0, minor: 0 };
            }
            const group = (i) => {
                return Number(agent.replace(r, '$' + i));
            };
            return nu(group(1), group(2));
        }
        Version.find = find;
        ;
        function detect(versionRegexes, agent) {
            const cleanedAgent = String(agent).toLowerCase();
            if (versionRegexes.length === 0) {
                return unknown();
            }
            return find(versionRegexes, cleanedAgent);
        }
        Version.detect = detect;
        ;
        function unknown() {
            return nu(0, 0);
        }
        Version.unknown = unknown;
        ;
        function nu(major, minor) {
            return { major, minor };
        }
        Version.nu = nu;
        ;
    })(Version = Platform.Version || (Platform.Version = {}));
    let UaString;
    (function (UaString) {
        function detect(candidates, userAgent) {
            const agent = String(userAgent).toLowerCase();
            return Arr.find(candidates, candidate => candidate.search(agent));
        }
        UaString.detect = detect;
        function detectBrowser(browsers, userAgent) {
            const browser = detect(browsers, userAgent);
            if (browser) {
                const version = Version.detect(browser.versionRegexes, userAgent);
                return {
                    current: browser.name,
                    version
                };
            }
            return null;
        }
        UaString.detectBrowser = detectBrowser;
        ;
        function detectOs(oses, userAgent) {
            const os = detect(oses, userAgent);
            if (os) {
                const version = Version.detect(os.versionRegexes, userAgent);
                return {
                    current: os.name,
                    version
                };
            }
            return null;
        }
        UaString.detectOs = detectOs;
        ;
    })(UaString = Platform.UaString || (Platform.UaString = {}));
    let Browser;
    (function (Browser) {
        const edge = 'Edge';
        const chrome = 'Chrome';
        const ie = 'IE';
        const opera = 'Opera';
        const firefox = 'Firefox';
        const safari = 'Safari';
        function unknown() {
            return nu({
                current: undefined,
                version: Version.unknown()
            });
        }
        Browser.unknown = unknown;
        ;
        function nu(info) {
            const current = info.current;
            const version = info.version;
            const isBrowser = (name) => () => current === name;
            return {
                current,
                version,
                isEdge: isBrowser(edge),
                isChrome: isBrowser(chrome),
                isIE: isBrowser(ie),
                isOpera: isBrowser(opera),
                isFirefox: isBrowser(firefox),
                isSafari: isBrowser(safari)
            };
        }
        Browser.nu = nu;
        ;
    })(Browser = Platform.Browser || (Platform.Browser = {}));
    let OperatingSystem;
    (function (OperatingSystem) {
        const windows = 'Windows';
        const ios = 'iOS';
        const android = 'Android';
        const linux = 'Linux';
        const osx = 'OSX';
        const solaris = 'Solaris';
        const freebsd = 'FreeBSD';
        const chromeos = 'ChromeOS';
        function unknown() {
            return nu({
                current: undefined,
                version: Version.unknown()
            });
        }
        OperatingSystem.unknown = unknown;
        ;
        function nu(info) {
            const current = info.current;
            const version = info.version;
            const isOS = (name) => () => current === name;
            return {
                current,
                version,
                isWindows: isOS(windows),
                isiOS: isOS(ios),
                isAndroid: isOS(android),
                isOSX: isOS(osx),
                isLinux: isOS(linux),
                isSolaris: isOS(solaris),
                isFreeBSD: isOS(freebsd),
                isChromeOS: isOS(chromeos)
            };
        }
        OperatingSystem.nu = nu;
        ;
    })(OperatingSystem = Platform.OperatingSystem || (Platform.OperatingSystem = {}));
    function DeviceType(os, browser, userAgent, mediaMatch) {
        const isiPad = os.isiOS() && /ipad/i.test(userAgent) === true;
        const isiPhone = os.isiOS() && !isiPad;
        const isMobile = os.isiOS() || os.isAndroid();
        const isTouch = isMobile || mediaMatch('(pointer:coarse)');
        const isTablet = isiPad || !isiPhone && isMobile && mediaMatch('(min-device-width:768px)');
        const isPhone = isiPhone || isMobile && !isTablet;
        const iOSwebview = browser.isSafari() && os.isiOS() && /safari/i.test(userAgent) === false;
        const isDesktop = !isPhone && !isTablet && !iOSwebview;
        return {
            isiPad: () => isiPad,
            isiPhone: () => isiPhone,
            isTablet: () => isTablet,
            isPhone: () => isPhone,
            isTouch: () => isTouch,
            isAndroid: os.isAndroid,
            isiOS: os.isiOS,
            isWebView: () => iOSwebview,
            isDesktop: () => isDesktop
        };
    }
    Platform.DeviceType = DeviceType;
    function detect(userAgent = navigator.userAgent, mediaMatch = (query) => window.matchMedia(query).matches) {
        const browsers = PlatformInfo.browsers;
        const oses = PlatformInfo.oses;
        const browserInfo = UaString.detectBrowser(browsers, userAgent);
        const browser = browserInfo ? Browser.nu(browserInfo) : Browser.unknown();
        const osInfo = UaString.detectOs(oses, userAgent);
        const os = osInfo ? OperatingSystem.nu(osInfo) : OperatingSystem.unknown();
        const deviceType = DeviceType(os, browser, userAgent, mediaMatch);
        return {
            browser,
            os,
            deviceType
        };
    }
    Platform.detect = detect;
    ;
})(Platform || (Platform = {}));
const PLATFORM = Platform.detect();
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
        if (!rng.collapsed && rng.startContainer === rng.endContainer && sel.setBaseAndExtent && !(PLATFORM.browser.isIE() || PLATFORM.browser.isEdge())) {
            if (rng.endOffset - rng.startOffset < 2) {
                if (rng.startContainer.hasChildNodes()) {
                    let node = rng.startContainer.childNodes[rng.startOffset];
                    if (node && node.tagName === 'IMG') {
                        sel.setBaseAndExtent(rng.startContainer, rng.startOffset, rng.endContainer, rng.endOffset);
                        if (sel.anchorNode !== rng.startContainer || sel.focusNode !== rng.endContainer) {
                            sel.setBaseAndExtent(node, 0, node, 1);
                        }
                    }
                }
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
    function createTable(rowCount, columnCount) {
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
        let tbl = createTable(rowCount, columnCount);
        tbl.id = id;
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