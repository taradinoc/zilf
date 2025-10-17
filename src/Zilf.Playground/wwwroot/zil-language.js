// ZIL (Zork Implementation Language) Monarch grammar derived from the TextMate grammar
// Source: EditorSupport/temp/zil.tmLanguage.cson

(function() {
    'use strict';

    function registerZilLanguage2() {
        monaco.languages.register({ id: 'zil' });

        monaco.languages.setLanguageConfiguration('zil', {
            brackets: [
                ['<', '>'],
                ['(', ')'],
                ['[', ']'],
            ],
            autoClosingPairs: [
                { open: '<', close: '>' },
                { open: '(', close: ')' },
                { open: '[', close: ']' },
                { open: '"', close: '"' },
            ],
            surroundingPairs: [
                { open: '<', close: '>' },
                { open: '(', close: ')' },
                { open: '[', close: ']' },
                { open: '"', close: '"' },
            ],
            colorizedBracketPairs: [
                ['<', '>'],
                ['(', ')'],
                ['[', ']'],
            ]
        });

        const decimal = /-?[0-9]+(?![^\s\t-\r,#':;%()\[\]<>{}\"])/;
        const octal = /\*[0-7]+\*(?![^\s\t-\r,#':;%()\[\]<>{}\"])/;
        const binary = /#\s*0*2\s+[01]+(?![^\s\t-\r,#':;%()\[\]<>{}\"])/;
        const char = /!\\\./;
        const atom = /(?:\\\.|[^!.\s\t-\r,#':;%()\[\]<>{}\"])(?:\\\.|[^\s\t-\r,#':;%()\[\]<>{}\"])*/;

        const controlForms = [
            'COND', 'BIND', 'PROG', 'REPEAT', 'DO', 'MAPF', 'MAPR', 'MAP-CONTENTS', 'MAP-DIRECTIONS',
            'AGAIN', 'RETURN', 'RTRUE', 'RFALSE', 'CATCH', 'THROW', 'EVAL', 'AND', 'OR', 'NOT'
        ];

        const outputForms = [
            'TELL', 'TELL-TOKENS', 'ADD-TELL-TOKENS', 'CRLF', 'PRINT', 'PRINTI', 'PRINTR',
            'PRINTB', 'PRINC', 'PRIN1'
        ];

        const zModelForms = [
            'FSET', 'FSET?', 'FCLEAR', 'MOVE', 'REMOVE', 'IN?', 'FIRST?', 'NEXT?',
            'PUTP', 'GETP', 'PROPDEF', 'GETPT', 'PTSIZE', 'INTBL?',
            'TABLE', 'PTABLE', 'LTABLE', 'ITABLE', 'GET', 'GETB', 'GET/B', 'PUT', 'PUTB', 'PUT/B',
            'ZGET', 'ZPUT', 'VOC', 'SYNONYM', 'VERB-SYNONYM', 'PREP-SYNONYM', 'ADJ-SYNONYM',
            'DIR-SYNONYM', 'BIT-SYNONYM', 'DIRECTIONS', 'BUZZ'
        ];

        const metaForms = [
            'INSERT-FILE', 'PACKAGE', 'ENDPACKAGE', 'USE', 'ENTRY', 'RENTRY', 'VERSION',
            'COMPILATION-FLAG', 'COMPILATION-FLAG-DEFAULT', 'REPLACE-DEFINITION',
            'DELAY-DEFINITION', 'DEFAULT-DEFINITION'
        ];

        const definitionFunctions = ['DEFINE', 'DEFINE20', 'DEFMAC', 'ROUTINE'];
        const definitionObjects = ['OBJECT', 'ROOM'];
        const definitionGlobals = ['SETG', 'CONSTANT', 'GLOBAL', 'GASSIGNED?', 'GUNASSIGN'];
        const definitionLocals = ['SET', 'ASSIGNED?', 'UNASSIGN'];
        const typeOperators = ['CHTYPE', 'TYPE', 'TYPE?', 'PRIMTYPE'];
        const typeDefinitions = ['NEWTYPE', 'DEFSTRUCT', 'APPLYTYPE', 'EVALTYPE', 'PRINTTYPE', 'TYPEPRIM'];
        const syntaxKeywords = ['SYNTAX'];

        const arithmeticOps = ['+', '-', '*', '/', 'MOD', 'MIN', 'MAX', 'OR?', 'AND?'];
        const bitwiseOps = ['BAND', 'BOR', 'ANDB', 'ORB', 'LSH', 'XORB', 'EQVB'];
        const comparisonOps = ['=', '==', 'N=', 'N==', 'L', 'L=', 'G', 'G=', '0?', '1?', 'T?', 'F?'];
        const booleanConstants = ['T'];
        const elseKeyword = ['ELSE'];
        const propertyNames = [
            'IN', 'LOC', 'DESC', 'SYNONYM', 'ADJECTIVE', 'FLAGS',
            'GLOBAL', 'GENERIC', 'ACTION', 'DESCFCN', 'CONTFCN', 'LDESC', 'FDESC',
            'NORTH', 'SOUTH', 'EAST', 'WEST', 'OUT', 'UP', 'DOWN', 'NW', 'SW', 'NE', 'SE'
        ];
        const argSeparators = ['AUX', 'EXTRA', 'OPT', 'OPTIONAL', 'ARGS', 'TUPLE', 'NAME', 'BIND'];

        monaco.languages.setMonarchTokensProvider('zil', {
            defaultToken: '',
            ignoreCase: true,
            brackets: [
                ['<', '>', 'punctuation.definition.form.zil'],
                ['(', ')', 'punctuation.definition.list.zil'],
                ['[', ']', 'punctuation.definition.array.zil'],
                ['!<', '>', 'punctuation.definition.form.zil']
            ],

            controlForms,
            outputForms,
            zModelForms,
            metaForms,
            definitionFunctions,
            definitionObjects,
            definitionGlobals,
            definitionLocals,
            typeOperators,
            typeDefinitions,
            syntaxKeywords,
            arithmeticOps,
            bitwiseOps,
            comparisonOps,
            booleanConstants,
            elseKeyword,
            propertyNames,
            argSeparators,

            tokenizer: {
                root: [
                    { include: '@whitespace' },

                    [/;/, { token: 'punctuation.definition.comment.prefix.zil', next: '@comment' }],
                    [/%%/, { token: 'punctuation.definition.macro.double.prefix.zil', next: '@macro' }],
                    [/%/, { token: 'punctuation.definition.macro.single.prefix.zil', next: '@macro' }],
                    [/\./, { token: 'punctuation.definition.variable.local.prefix.zil', next: '@lval' }],
                    [/,/, { token: 'punctuation.definition.variable.global.prefix.zil', next: '@gval' }],
                    [/'/, { token: 'punctuation.definition.quote.prefix.zil', next: '@quote' }],
                    [/!(?=[.,<])/, { token: 'punctuation.definition.segment.prefix.zil', next: '@segment' }],

                    [/!\(/, { token: 'punctuation.definition.list.begin.zil', bracket: '@open', next: '@bangList' }],
                    [/\(/, { token: 'punctuation.definition.list.begin.zil', bracket: '@open', next: '@list' }],
                    [/!\[/, { token: 'punctuation.definition.array.uvector.begin.zil', bracket: '@open', next: '@uvector' }],
                    [/\[/, { token: 'punctuation.definition.array.vector.begin.zil', bracket: '@open', next: '@vector' }],
                    [/</, { token: 'punctuation.definition.form.begin.zil', bracket: '@open', next: '@form' }],

                    [/"/, { token: 'punctuation.definition.string.begin.zil', next: '@string' }],
                    [char, 'constant.character.zil'],
                    [binary, 'constant.numeric.binary.zil'],
                    [octal, 'constant.numeric.octal.zil'],
                    [decimal, 'constant.numeric.decimal.zil'],

                    [atom, {
                        cases: {
                            '@controlForms': 'keyword.control.zil',
                            '@outputForms': 'keyword.output.zil',
                            '@zModelForms': 'keyword.zmodel.zil',
                            '@metaForms': 'keyword.meta.zil',
                            '@definitionFunctions': 'keyword.definition.function.zil',
                            '@definitionObjects': 'keyword.definition.object.zil',
                            '@definitionGlobals': 'keyword.definition.global.zil',
                            '@definitionLocals': 'keyword.definition.local.zil',
                            '@typeOperators': 'keyword.type.zil',
                            '@typeDefinitions': 'keyword.definition.type.zil',
                            '@syntaxKeywords': 'keyword.definition.vocab.syntax.zil',
                            '@arithmeticOps': 'keyword.operator.arithmetic.zil',
                            '@bitwiseOps': 'keyword.operator.bitwise.zil',
                            '@comparisonOps': 'keyword.operator.comparison.zil',
                            '@booleanConstants': 'constant.language.boolean.true.zil',
                            '@elseKeyword': 'keyword.control.else.zil',
                            '@propertyNames': 'storage.property.zil',
                            '@argSeparators': 'keyword.separator.arguments.zil',
                            '@default': 'meta.symbol.atom.zil'
                        }
                    }],

                    [/!./, 'invalid.illegal.zil'],
                ],

                whitespace: [
                    [/\s+/, 'white']
                ],

                comment: [
                    [/\s+/, 'comment.block.zil'],
                    [/"/, { token: 'comment.block.zil', switchTo: '@commentString' }],
                    [char, { token: 'comment.block.zil', next: '@pop' }],
                    [decimal, { token: 'comment.block.zil', next: '@pop' }],
                    [octal, { token: 'comment.block.zil', next: '@pop' }],
                    [binary, { token: 'comment.block.zil', next: '@pop' }],
                    [/;/, 'comment.block.zil'],
                    [/</, { token: 'comment.block.zil', switchTo: '@commentForm' }],
                    [/!\(/, { token: 'comment.block.zil', switchTo: '@commentList' }],
                    [/\(/, { token: 'comment.block.zil', switchTo: '@commentList' }],
                    [/!\[/, { token: 'comment.block.zil', switchTo: '@commentVector' }],
                    [/\[/, { token: 'comment.block.zil', switchTo: '@commentVector' }],
                    [/%{1,2}/, 'comment.block.zil'],
                    [/[.,']/, 'comment.block.zil'],
                    [atom, { token: 'comment.block.zil', next: '@pop' }],
                    [/\\\./, 'comment.block.zil']
                ],

                commentString: [
                    [/[^\"\\]+/, 'comment.block.zil'],
                    [/\\./, 'comment.block.zil'],
                    [/"/, { token: 'comment.block.zil', next: '@pop' }]
                ],

                commentForm: [
                    [/[^<>]+/, 'comment.block.zil'],
                    [/</, { token: 'comment.block.zil', next: '@commentForm' }],
                    [/!?>/, { token: 'comment.block.zil', next: '@pop' }]
                ],

                commentList: [
                    [/[^()]+/, 'comment.block.zil'],
                    [/!\(/, { token: 'comment.block.zil', next: '@commentList' }],
                    [/\(/, { token: 'comment.block.zil', next: '@commentList' }],
                    [/!\)/, { token: 'comment.block.zil', next: '@pop' }],
                    [/\)/, { token: 'comment.block.zil', next: '@pop' }]
                ],

                commentVector: [
                    [/[^[\]]+/, 'comment.block.zil'],
                    [/!\[/, { token: 'comment.block.zil', next: '@commentVector' }],
                    [/\[/, { token: 'comment.block.zil', next: '@commentVector' }],
                    [/!\]/, { token: 'comment.block.zil', next: '@pop' }],
                    [/\]/, { token: 'comment.block.zil', next: '@pop' }]
                ],

                macro: [
                    [/\s+/, 'meta.macro.zil'],
                    [/"/, { token: 'meta.macro.zil', switchTo: '@macroString' }],
                    [char, { token: 'meta.macro.zil', next: '@pop' }],
                    [decimal, { token: 'meta.macro.zil', next: '@pop' }],
                    [octal, { token: 'meta.macro.zil', next: '@pop' }],
                    [binary, { token: 'meta.macro.zil', next: '@pop' }],
                    [/</, { token: 'meta.macro.zil', switchTo: '@macroForm' }],
                    [/!\(/, { token: 'meta.macro.zil', switchTo: '@macroList' }],
                    [/\(/, { token: 'meta.macro.zil', switchTo: '@macroList' }],
                    [/!\[/, { token: 'meta.macro.zil', switchTo: '@macroVector' }],
                    [/\[/, { token: 'meta.macro.zil', switchTo: '@macroVector' }],
                    [atom, { token: 'meta.macro.zil', next: '@pop' }],
                    [/\\\./, 'meta.macro.zil']
                ],

                macroString: [
                    [/[^\"\\]+/, 'meta.macro.zil'],
                    [/\\./, 'meta.macro.zil'],
                    [/"/, { token: 'meta.macro.zil', next: '@pop' }]
                ],

                macroForm: [
                    [/[^<>]+/, 'meta.macro.zil'],
                    [/</, { token: 'meta.macro.zil', next: '@macroForm' }],
                    [/!?>/, { token: 'meta.macro.zil', next: '@pop' }]
                ],

                macroList: [
                    [/[^()]+/, 'meta.macro.zil'],
                    [/!\(/, { token: 'meta.macro.zil', next: '@macroList' }],
                    [/\(/, { token: 'meta.macro.zil', next: '@macroList' }],
                    [/!\)/, { token: 'meta.macro.zil', next: '@pop' }],
                    [/\)/, { token: 'meta.macro.zil', next: '@pop' }]
                ],

                macroVector: [
                    [/[^[\]]+/, 'meta.macro.zil'],
                    [/!\[/, { token: 'meta.macro.zil', next: '@macroVector' }],
                    [/\[/, { token: 'meta.macro.zil', next: '@macroVector' }],
                    [/!\]/, { token: 'meta.macro.zil', next: '@pop' }],
                    [/\]/, { token: 'meta.macro.zil', next: '@pop' }]
                ],

                lval: [
                    [/\s+/, 'variable.other.local.zil'],
                    [/"/, { token: 'variable.other.local.zil', switchTo: '@lvalString' }],
                    [char, { token: 'variable.other.local.zil', next: '@pop' }],
                    [decimal, { token: 'variable.other.local.zil', next: '@pop' }],
                    [octal, { token: 'variable.other.local.zil', next: '@pop' }],
                    [binary, { token: 'variable.other.local.zil', next: '@pop' }],
                    [/</, { token: 'variable.other.local.zil', switchTo: '@lvalForm' }],
                    [/!\(/, { token: 'variable.other.local.zil', switchTo: '@lvalList' }],
                    [/\(/, { token: 'variable.other.local.zil', switchTo: '@lvalList' }],
                    [/!\[/, { token: 'variable.other.local.zil', switchTo: '@lvalVector' }],
                    [/\[/, { token: 'variable.other.local.zil', switchTo: '@lvalVector' }],
                    [atom, { token: 'variable.other.local.zil', next: '@pop' }],
                    [/\\\./, 'variable.other.local.zil']
                ],

                lvalString: [
                    [/[^\"\\]+/, 'variable.other.local.zil'],
                    [/\\./, 'variable.other.local.zil'],
                    [/"/, { token: 'variable.other.local.zil', next: '@pop' }]
                ],

                lvalForm: [
                    [/[^<>]+/, 'variable.other.local.zil'],
                    [/</, { token: 'variable.other.local.zil', next: '@lvalForm' }],
                    [/!?>/, { token: 'variable.other.local.zil', next: '@pop' }]
                ],

                lvalList: [
                    [/[^()]+/, 'variable.other.local.zil'],
                    [/!\(/, { token: 'variable.other.local.zil', next: '@lvalList' }],
                    [/\(/, { token: 'variable.other.local.zil', next: '@lvalList' }],
                    [/!\)/, { token: 'variable.other.local.zil', next: '@pop' }],
                    [/\)/, { token: 'variable.other.local.zil', next: '@pop' }]
                ],

                lvalVector: [
                    [/[^[\]]+/, 'variable.other.local.zil'],
                    [/!\[/, { token: 'variable.other.local.zil', next: '@lvalVector' }],
                    [/\[/, { token: 'variable.other.local.zil', next: '@lvalVector' }],
                    [/!\]/, { token: 'variable.other.local.zil', next: '@pop' }],
                    [/\]/, { token: 'variable.other.local.zil', next: '@pop' }]
                ],

                gval: [
                    [/\s+/, 'variable.other.global.zil'],
                    [/"/, { token: 'variable.other.global.zil', switchTo: '@gvalString' }],
                    [char, { token: 'variable.other.global.zil', next: '@pop' }],
                    [decimal, { token: 'variable.other.global.zil', next: '@pop' }],
                    [octal, { token: 'variable.other.global.zil', next: '@pop' }],
                    [binary, { token: 'variable.other.global.zil', next: '@pop' }],
                    [/</, { token: 'variable.other.global.zil', switchTo: '@gvalForm' }],
                    [/!\(/, { token: 'variable.other.global.zil', switchTo: '@gvalList' }],
                    [/\(/, { token: 'variable.other.global.zil', switchTo: '@gvalList' }],
                    [/!\[/, { token: 'variable.other.global.zil', switchTo: '@gvalVector' }],
                    [/\[/, { token: 'variable.other.global.zil', switchTo: '@gvalVector' }],
                    [atom, { token: 'variable.other.global.zil', next: '@pop' }],
                    [/\\\./, 'variable.other.global.zil']
                ],

                gvalString: [
                    [/[^\"\\]+/, 'variable.other.global.zil'],
                    [/\\./, 'variable.other.global.zil'],
                    [/"/, { token: 'variable.other.global.zil', next: '@pop' }]
                ],

                gvalForm: [
                    [/[^<>]+/, 'variable.other.global.zil'],
                    [/</, { token: 'variable.other.global.zil', next: '@gvalForm' }],
                    [/!?>/, { token: 'variable.other.global.zil', next: '@pop' }]
                ],

                gvalList: [
                    [/[^()]+/, 'variable.other.global.zil'],
                    [/!\(/, { token: 'variable.other.global.zil', next: '@gvalList' }],
                    [/\(/, { token: 'variable.other.global.zil', next: '@gvalList' }],
                    [/!\)/, { token: 'variable.other.global.zil', next: '@pop' }],
                    [/\)/, { token: 'variable.other.global.zil', next: '@pop' }]
                ],

                gvalVector: [
                    [/[^[\]]+/, 'variable.other.global.zil'],
                    [/!\[/, { token: 'variable.other.global.zil', next: '@gvalVector' }],
                    [/\[/, { token: 'variable.other.global.zil', next: '@gvalVector' }],
                    [/!\]/, { token: 'variable.other.global.zil', next: '@pop' }],
                    [/\]/, { token: 'variable.other.global.zil', next: '@pop' }]
                ],

                quote: [
                    [/\s+/, 'meta.quoted-expression.zil'],
                    [/"/, { token: 'meta.quoted-expression.zil', switchTo: '@quoteString' }],
                    [char, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [decimal, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [octal, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [binary, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [/</, { token: 'meta.quoted-expression.zil', switchTo: '@quoteForm' }],
                    [/!\(/, { token: 'meta.quoted-expression.zil', switchTo: '@quoteList' }],
                    [/\(/, { token: 'meta.quoted-expression.zil', switchTo: '@quoteList' }],
                    [/!\[/, { token: 'meta.quoted-expression.zil', switchTo: '@quoteVector' }],
                    [/\[/, { token: 'meta.quoted-expression.zil', switchTo: '@quoteVector' }],
                    [atom, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [/\\\./, 'meta.quoted-expression.zil']
                ],

                quoteString: [
                    [/[^\"\\]+/, 'meta.quoted-expression.zil'],
                    [/\\./, 'meta.quoted-expression.zil'],
                    [/"/, { token: 'meta.quoted-expression.zil', next: '@pop' }]
                ],

                quoteForm: [
                    [/[^<>]+/, 'meta.quoted-expression.zil'],
                    [/</, { token: 'meta.quoted-expression.zil', next: '@quoteForm' }],
                    [/!?>/, { token: 'meta.quoted-expression.zil', next: '@pop' }]
                ],

                quoteList: [
                    [/[^()]+/, 'meta.quoted-expression.zil'],
                    [/!\(/, { token: 'meta.quoted-expression.zil', next: '@quoteList' }],
                    [/\(/, { token: 'meta.quoted-expression.zil', next: '@quoteList' }],
                    [/!\)/, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [/\)/, { token: 'meta.quoted-expression.zil', next: '@pop' }]
                ],

                quoteVector: [
                    [/[^[\]]+/, 'meta.quoted-expression.zil'],
                    [/!\[/, { token: 'meta.quoted-expression.zil', next: '@quoteVector' }],
                    [/\[/, { token: 'meta.quoted-expression.zil', next: '@quoteVector' }],
                    [/!\]/, { token: 'meta.quoted-expression.zil', next: '@pop' }],
                    [/\]/, { token: 'meta.quoted-expression.zil', next: '@pop' }]
                ],

                segment: [
                    [/\s+/, 'meta.structure.segment.zil'],
                    [/"/, { token: 'meta.structure.segment.zil', switchTo: '@segmentString' }],
                    [char, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [decimal, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [octal, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [binary, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [/</, { token: 'meta.structure.segment.zil', switchTo: '@segmentForm' }],
                    [/!\(/, { token: 'meta.structure.segment.zil', switchTo: '@segmentList' }],
                    [/\(/, { token: 'meta.structure.segment.zil', switchTo: '@segmentList' }],
                    [/!\[/, { token: 'meta.structure.segment.zil', switchTo: '@segmentVector' }],
                    [/\[/, { token: 'meta.structure.segment.zil', switchTo: '@segmentVector' }],
                    [atom, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [/\\\./, 'meta.structure.segment.zil']
                ],

                segmentString: [
                    [/[^\"\\]+/, 'meta.structure.segment.zil'],
                    [/\\./, 'meta.structure.segment.zil'],
                    [/"/, { token: 'meta.structure.segment.zil', next: '@pop' }]
                ],

                segmentForm: [
                    [/[^<>]+/, 'meta.structure.segment.zil'],
                    [/</, { token: 'meta.structure.segment.zil', next: '@segmentForm' }],
                    [/!?>/, { token: 'meta.structure.segment.zil', next: '@pop' }]
                ],

                segmentList: [
                    [/[^()]+/, 'meta.structure.segment.zil'],
                    [/!\(/, { token: 'meta.structure.segment.zil', next: '@segmentList' }],
                    [/\(/, { token: 'meta.structure.segment.zil', next: '@segmentList' }],
                    [/!\)/, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [/\)/, { token: 'meta.structure.segment.zil', next: '@pop' }]
                ],

                segmentVector: [
                    [/[^[\]]+/, 'meta.structure.segment.zil'],
                    [/!\[/, { token: 'meta.structure.segment.zil', next: '@segmentVector' }],
                    [/\[/, { token: 'meta.structure.segment.zil', next: '@segmentVector' }],
                    [/!\]/, { token: 'meta.structure.segment.zil', next: '@pop' }],
                    [/\]/, { token: 'meta.structure.segment.zil', next: '@pop' }]
                ],

                string: [
                    [/[^\"\\]+/, 'string.quoted.double.zil'],
                    [/\\./, 'constant.character.escape.zil'],
                    [/"/, { token: 'punctuation.definition.string.end.zil', next: '@pop' }]
                ],

                list: [
                    [/\s+/, 'white'],
                    [/!\)/, { token: 'punctuation.definition.list.end.zil', bracket: '@close', next: '@pop' }],
                    [/\)/, { token: 'punctuation.definition.list.end.zil', bracket: '@close', next: '@pop' }],
                    { include: '@root' }
                ],

                bangList: [
                    [/\s+/, 'white'],
                    [/!\)/, { token: 'punctuation.definition.list.end.zil', bracket: '@close', next: '@pop' }],
                    [/\)/, { token: 'punctuation.definition.list.end.zil', bracket: '@close', next: '@pop' }],
                    { include: '@root' }
                ],

                vector: [
                    [/\s+/, 'white'],
                    [/!\]/, { token: 'punctuation.definition.array.vector.end.zil', bracket: '@close', next: '@pop' }],
                    [/\]/, { token: 'punctuation.definition.array.vector.end.zil', bracket: '@close', next: '@pop' }],
                    { include: '@root' }
                ],

                uvector: [
                    [/\s+/, 'white'],
                    [/!\]/, { token: 'punctuation.definition.array.uvector.end.zil', bracket: '@close', next: '@pop' }],
                    [/\]/, { token: 'punctuation.definition.array.uvector.end.zil', bracket: '@close', next: '@pop' }],
                    { include: '@root' }
                ],

                form: [
                    [/\s+/, 'white'],
                    [/!?>/, { token: 'punctuation.definition.form.end.zil', bracket: '@close', next: '@pop' }],
                    [/(?:FORM\s+)?(?:\+|-|\*|\/|MOD|MIN|MAX|OR\?|AND\?)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.operator.arithmetic.zil'],
                    [/(?:FORM\s+)?(?:BAND|BOR|ANDB|ORB|LSH|XORB|EQVB)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.operator.bitwise.zil'],
                    [/(?:FORM\s+)?(?:==?|N==?|L=?|G=?|[01TF]\?)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.operator.comparison.zil'],
                    [/(?:FORM\s+)?(?:COND|BIND|PROG|REPEAT|DO|MAPF|MAPR|MAP-CONTENTS|MAP-DIRECTIONS|AGAIN|RETURN|RTRUE|RFALSE|CATCH|THROW|EVAL|AND|OR|NOT)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.control.zil'],
                    [/(?:FORM\s+)?(?:TELL(?:-TOKENS)?|ADD-TELL-TOKENS|CRLF|PRINT[INR]?|PRIN[C1])(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.output.zil'],
                    [/(?:FORM\s+)?(?:FSET\??|FCLEAR|MOVE|REMOVE|IN\?|FIRST\?|NEXT\?|PUTP|GETP|PROPDEF|GETPT|PTSIZE|INTBL\?|P?L?TABLE|ITABLE|GETB?|GET\/B|PUTB?|PUT\/B|ZGET|ZPUT|VOC|SYNONYM|(?:VERB|PREP|ADJ|DIR|BIT)-SYNONYM|DIRECTIONS|BUZZ)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.zmodel.zil'],
                    [/(?:FORM\s+)?(?:INSERT-FILE|PACKAGE|ENDPACKAGE|USE|ENTRY|RENTRY|VERSION|COMPILATION-FLAG(?:-DEFAULT)?|REPLACE-DEFINITION|DELAY-DEFINITION|DEFAULT-DEFINITION)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.meta.zil'],
                    [/(?:FORM\s+)?(?:DEFINE|DEFINE20|DEFMAC|ROUTINE)(?=\s+)(?=[^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.definition.function.zil'],
                    [/(?:FORM\s+)?(?:OBJECT|ROOM)(?=\s+)(?=[^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.definition.object.zil'],
                    [/(?:FORM\s+)?(?:SETG|CONSTANT|GLOBAL|GASSIGNED\?|GUNASSIGN)(?=\s+)(?=[^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.definition.global.zil'],
                    [/(?:FORM\s+)?(?:SET|ASSIGNED\?|UNASSIGN)(?=\s+)(?=[^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.definition.local.zil'],
                    [/(?:FORM\s+)?(?:CHTYPE|TYPE\??|PRIMTYPE)(?![^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.type.zil'],
                    [/(?:FORM\s+)?(?:NEWTYPE|DEFSTRUCT|APPLYTYPE|EVALTYPE|PRINTTYPE|TYPEPRIM)(?=\s+)(?=[^\s\t-\r,#':;%()\[\]<>{}\"])/,
                        'keyword.definition.type.zil'],
                    [/(?:FORM\s+)?SYNTAX(?=\s+)/, 'keyword.definition.vocab.syntax.zil'],
                    { include: '@root' }
                ]
            }
        });

        monaco.editor.defineTheme('zil-theme', {
            base: 'vs-dark',
            inherit: true,
            rules: [
                { token: 'comment.block.zil', foreground: '6A9955' },
                { token: 'meta.macro.zil', foreground: '9CDCFE' },
                { token: 'variable.other.local.zil', foreground: '9CDCFE' },
                { token: 'variable.other.global.zil', foreground: '4FC1FF' },
                { token: 'meta.quoted-expression.zil', foreground: 'C586C0' },
                { token: 'meta.structure.segment.zil', foreground: 'C586C0' },
                { token: 'string.quoted.double.zil', foreground: 'CE9178' },
                { token: 'constant.numeric.decimal.zil', foreground: 'B5CEA8' },
                { token: 'constant.numeric.octal.zil', foreground: 'B5CEA8' },
                { token: 'constant.numeric.binary.zil', foreground: 'B5CEA8' },
                { token: 'keyword.control.zil', foreground: 'C586C0' },
                { token: 'keyword.output.zil', foreground: 'C586C0' },
                { token: 'keyword.zmodel.zil', foreground: 'C586C0' },
                { token: 'keyword.meta.zil', foreground: 'C586C0' },
                { token: 'keyword.operator.arithmetic.zil', foreground: 'DCDCAA' },
                { token: 'keyword.operator.bitwise.zil', foreground: 'DCDCAA' },
                { token: 'keyword.operator.comparison.zil', foreground: 'DCDCAA' },
                { token: 'keyword.definition.function.zil', foreground: 'DCDCAA', fontStyle: 'bold' },
                { token: 'entity.name.function.zil', foreground: 'DCDCAA' },
                { token: 'entity.name.object.zil', foreground: '4EC9B0' },
                { token: 'entity.name.type.zil', foreground: '4EC9B0' },
                { token: 'meta.symbol.atom.zil', foreground: 'D4D4D4' },
                { token: 'storage.property.zil', foreground: 'C586C0' },
                { token: 'keyword.separator.arguments.zil', foreground: 'C586C0' }
            ],
            colors: {}
        });
    }

    if (typeof monaco !== 'undefined') {
        registerZilLanguage2();
    } else if (typeof require !== 'undefined') {
        require(['vs/editor/editor.main'], registerZilLanguage2);
    } else {
        window.addEventListener('load', function() {
            if (typeof monaco !== 'undefined') {
                registerZilLanguage2();
            }
        });
    }
})();
