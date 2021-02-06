/// <reference path='utils.ts'/>

namespace SelectionUtils {
    export const MOVE_CARET_BEFORE_ON_ENTER_ELEMENTS_MAP = Utils.makeMap(
        'td th iframe video audio object script code table ' +
        'area base basefont br col frame hr img input isindex link ' +
        'meta param embed source wbr track', ' ');

    export function moveEndPoint(rng: Range, node: Node, start?: boolean) {
        const root = node, walker = new Utils.TreeWalker(node, root);
        const moveCaretBeforeOnEnterElementsMap = Utils.Obj.filter(MOVE_CARET_BEFORE_ON_ENTER_ELEMENTS_MAP, (_, key) => {
            return !Utils.Arr.contains(['td', 'th', 'table'], key)
        });

        do {
            if (Utils.NodeType.isText(node) && Utils.trim(node.nodeValue).length !== 0) {
                if (start) {
                    rng.setStart(node, 0);
                } else {
                    rng.setEnd(node, node.nodeValue.length);
                }

                return;
            }

            // BR/IMG/INPUT elements but not table cells
            if (moveCaretBeforeOnEnterElementsMap[node.nodeName]) {
                if (start) {
                    rng.setStartBefore(node);
                } else {
                    if (node.nodeName === 'BR') {
                        rng.setEndBefore(node);
                    } else {
                        rng.setEndAfter(node);
                    }
                }

                return;
            }
        } while ((node = (start ? walker.next() : walker.prev())));

        if (root.nodeName === 'BODY') {
            if (start) {
                rng.setStart(root, 0);
            } else {
                rng.setEnd(root, root.childNodes.length);
            }
        }
    }

    function isNativeIESelection(rng: any): boolean {
        return !!(rng).select;
    }

    function isAttachedToDom(node: Node): boolean {
        return !!(node && node.ownerDocument) && node.ownerDocument.contains(node);
    }

    function isValidRange(rng: Range): boolean {
        if (!rng) {
            return false;
        } else if (isNativeIESelection(rng)) {
            return true;
        } else {
            return isAttachedToDom(rng.startContainer) && isAttachedToDom(rng.endContainer);
        }
    }

    export function selectElement(node: Node, content?: boolean) : Range {
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

    export function getRange(): Range {
        const sel = document.getSelection();
        const rng = sel.rangeCount > 0 ? sel.getRangeAt(0) : document.createRange();

        // if range is at start of document then move it to start of body
        if (rng.setStart && rng.startContainer.nodeType === 9 && rng.collapsed) {
            const body = document.body;
            rng.setStart(body, 0);
            rng.setEnd(body, 0);
        }

        return rng;
    }

    export function setRange(rng: Range, forward?: boolean) {
        if (!isValidRange(rng)) {
            return;
        }

        const ieRange: any = isNativeIESelection(rng) ? rng : null;
        if (ieRange) {
            try { ieRange.select(); }
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

        // WebKit egde case selecting images works better using setBaseAndExtent when the image is floated
        //if (!rng.collapsed && rng.startContainer === rng.endContainer && sel.setBaseAndExtent && !Env.ie) {
        //    if (rng.endOffset - rng.startOffset < 2) {
        //        if (rng.startContainer.hasChildNodes()) {
        //            node = rng.startContainer.childNodes[rng.startOffset];
        //            if (node && node.tagName === 'IMG') {
        //                sel.setBaseAndExtent(
        //                    rng.startContainer,
        //                    rng.startOffset,
        //                    rng.endContainer,
        //                    rng.endOffset
        //                );

        //                // Since the setBaseAndExtent is fixed in more recent Blink versions we
        //                // need to detect if it's doing the wrong thing and falling back to the
        //                // crazy incorrect behavior api call since that seems to be the only way
        //                // to get it to work on Safari WebKit as of 2017-02-23
        //                if (sel.anchorNode !== rng.startContainer || sel.focusNode !== rng.endContainer) {
        //                    sel.setBaseAndExtent(node, 0, node, 1);
        //                }
        //            }
        //        }
        //    }
        //}
    }

    export function select(node: Node, content?: boolean) {
        const range = selectElement(node, content);
        setRange(range);
        return node;
    }

    export function collapse(toStart?: boolean) {
        const rng = getRange();
        rng.collapse(!!toStart);
        setRange(rng);
    }
}