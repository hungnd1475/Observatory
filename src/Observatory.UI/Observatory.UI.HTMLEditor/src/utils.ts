namespace Utils {
    export namespace Obj {
        export function enumerate<T>(obj: T, f: (value: T[keyof T], key: string) => void) {
            const props = Object.keys(obj);
            for (let k = 0; k < props.length; ++k) {
                const i = props[k];
                const x = obj[i];
                f(x, i);
            }
        }

        export function filter<V>(obj: Record<string, V>, pred: (value: V, key: string) => boolean): Record<string, V> {
            const r: Record<string, V> = {};
            enumerate(obj, (x, i) => {
                if (pred(x, i)) {
                    r[i] = x;
                }
            });
            return r;
        }
    }

    export namespace Arr {
        export function contains<T>(arr: ArrayLike<T>, value: T): boolean {
            return Array.prototype.indexOf.call(arr, value) > -1;
        }
    }
    
    export namespace NodeType {
        export function isText(node: Node): boolean {
            return !!node && node.nodeType === 3;
        }
    }

    export class TreeWalker {
        private readonly rootNode: Node;
        private node: Node;

        public constructor(startNode: Node, rootNode: Node) {
            this.node = startNode;
            this.rootNode = rootNode;

            this.current = this.current.bind(this);
        }

        public current(): Node {
            return this.node;
        }

        public next(shallow?: boolean): Node {
            this.node = this.findSibling(this.node, 'firstChild', 'nextSibling', shallow);
            return this.node;
        }

        public prev(shallow?: boolean): Node {
            this.node = this.findSibling(this.node, 'lastChild', 'previousSibling', shallow);
            return this.node;
        }

        private findSibling(node: Node, startName: 'firstChild' | 'lastChild', siblingName: 'nextSibling' | 'previousSibling', shallow?: boolean): Node {
            let sibling: Node, parent: Node;

            if (node) {
                // Walk into nodes if it has a start
                if (!shallow && node[startName]) {
                    return node[startName];
                }

                // Return the sibling if it has one
                if (node !== this.rootNode) {
                    sibling = node[siblingName];
                    if (sibling) {
                        return sibling;
                    }

                    // Walk up the parents to look for siblings
                    for (parent = node.parentNode; parent && parent !== this.rootNode; parent = parent.parentNode) {
                        sibling = parent[siblingName];
                        if (sibling) {
                            return sibling;
                        }
                    }
                }
            }
        }

        private findPreviousNode(node: Node, startName: 'lastChild', siblingName: 'previousSibling', shallow?: boolean): Node {
            let sibling: Node, parent: Node, child: Node;

            if (node) {
                sibling = node[siblingName];
                if (this.rootNode && sibling === this.rootNode) {
                    return;
                }

                if (sibling) {
                    if (!shallow) {
                        // Walk down to the most distant child
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

    export function debounce(interval: number, callback: (...args: any[]) => void): (...args: any[]) => void {
        let debounceTimeoutId: number;
        return (...args: any[]) => {
            clearTimeout(debounceTimeoutId);
            debounceTimeoutId = window.setTimeout(() => callback.apply(this, args), interval);
        };
    }

    const WHITESPACE_REGEX = /^\s*|\s*$/g;

    export function trim(str: string): string {
        return (str === null || str === undefined) ? '' : ('' + str).replace(WHITESPACE_REGEX, '');
    }

    export function makeMap(items: string | string[], delim?: string | RegExp, map?: Record<string, object>): Record<string, object> {
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

    export function getNodeIndex(node: Node, normalized?: boolean): number {
        let idx = 0, lastNodeType, nodeType;

        if (node) {
            for (lastNodeType = node.nodeType, node = node.previousSibling; node; node = node.previousSibling) {
                nodeType = node.nodeType;

                // Normalize text nodes
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
}

