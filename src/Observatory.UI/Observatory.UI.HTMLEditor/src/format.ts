/// <reference path='table.ts'/>
/// <reference path='commands.ts'/>

namespace Format {
    export enum TextAlignment {
        Left = 0,
        Center,
        Right,
        Justified,
    }

    export enum ListType {
        None,
        Bullets,
        Numbering,
    }

    export interface TextFormat {
        isBold: boolean;
        isItalic: boolean;
        isUnderlined: boolean;
        isStrikethrough: boolean;
        isSuperscript: boolean;
        isSubscript: boolean;
        fontNames: string[];
        fontSize: number;
        foreground: string;
        background: string;
        alignment: TextAlignment;
        listType: ListType;
        isTable: boolean;
    }

    function getFontNames(): string[] {
        const fontNames = document.queryCommandValue(Commands.FONT_NAME);
        if (fontNames !== null) {
            return fontNames.split(",").map(x => x.replace(/['"]+/g, ''));
        }
        return null;
    }

    function getAlignment(): TextAlignment {
        if (document.queryCommandValue('justifyCenter')) {
            return TextAlignment.Center;
        } else if (document.queryCommandValue('justifyRight')) {
            return TextAlignment.Right;
        } else if (document.queryCommandValue('justifyFull')) {
            return TextAlignment.Justified;
        }
        return TextAlignment.Left;
    }

    function getListType(): ListType {
        if (document.queryCommandValue('insertUnorderedList')) {
            return ListType.Bullets;
        } else if (document.queryCommandValue('insertOrderedList')) {
            return ListType.Numbering;
        }
        return ListType.None;
    }

    function queryIsTable(): boolean {
        const selection = document.getSelection();
        if (selection.anchorNode instanceof HTMLElement) {
            return Table.findParentTable(selection.anchorNode) !== null;
        } else {
            return Table.findParentTable(selection.anchorNode.parentElement) !== null;
        }
    }

    export function getCurrentFormat(): TextFormat {
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

    export function setCurrentFormat(formatName: string, value?: string): TextFormat {
        try {
            document.execCommand(formatName, false, value);
        } catch (error) {
            console.log(error);
        }
        return getCurrentFormat();
    }
}