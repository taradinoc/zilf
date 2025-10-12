// ZIL (Zork Implementation Language) syntax definition for Monaco Editor
// Converted from TextMate grammar to Monarch

(function() {
    'use strict';

    function registerZilLanguage() {
        console.log('ZIL language registration starting...');

        // Register the ZIL language
        monaco.languages.register({ id: 'zil' });
        console.log('ZIL language registered');

    // Set the language configuration (brackets, comments, etc.)
    monaco.languages.setLanguageConfiguration('zil', {
        // No line comments in ZIL - semicolon is a prefix operator
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

    // Define the Monarch syntax highlighting rules
    monaco.languages.setMonarchTokensProvider('zil', {
        defaultToken: '',
        ignoreCase: true,
        brackets: [
            ['<', '>', 'delimiter.bracket'],
            ['(', ')', 'delimiter.bracket'],
            ['[', ']', 'delimiter.bracket'],
            // ['!<', '!>', 'delimiter.bracket'],
            ['!<', '>', 'delimiter.bracket'],
        ],

        // Keywords and operators
        keywords: [
            // Control flow
            'COND', 'BIND', 'PROG', 'REPEAT', 'DO', 'MAPF', 'MAPR', 'MAP-CONTENTS', 'MAP-DIRECTIONS',
            'AGAIN', 'RETURN', 'RTRUE', 'RFALSE', 'CATCH', 'THROW', 'EVAL', 'AND', 'OR', 'NOT',
            
            // Output
            'TELL', 'TELL-TOKENS', 'ADD-TELL-TOKENS', 'CRLF', 'PRINT', 'PRINTI', 'PRINTR', 
            'PRINTB', 'PRINC', 'PRIN1',
            
            // Z-machine model
            'FSET', 'FSET?', 'FCLEAR', 'MOVE', 'REMOVE', 'IN?', 'FIRST?', 'NEXT?',
            'PUTP', 'GETP', 'PROPDEF', 'GETPT', 'PTSIZE', 'INTBL?',
            'TABLE', 'PTABLE', 'LTABLE', 'ITABLE', 'GET', 'GETB', 'GET/B', 'PUT', 'PUTB', 'PUT/B',
            'ZGET', 'ZPUT', 'VOC', 'SYNONYM', 'VERB-SYNONYM', 'PREP-SYNONYM', 'ADJ-SYNONYM',
            'DIR-SYNONYM', 'BIT-SYNONYM', 'DIRECTIONS', 'BUZZ',
            
            // Meta
            'INSERT-FILE', 'PACKAGE', 'ENDPACKAGE', 'USE', 'ENTRY', 'RENTRY', 'VERSION',
            'COMPILATION-FLAG', 'COMPILATION-FLAG-DEFAULT', 'REPLACE-DEFINITION', 
            'DELAY-DEFINITION', 'DEFAULT-DEFINITION',
            
            // Type operations
            'CHTYPE', 'TYPE', 'TYPE?', 'PRIMTYPE', 'NEWTYPE', 'DEFSTRUCT', 'APPLYTYPE',
            'EVALTYPE', 'PRINTTYPE', 'TYPEPRIM',
            
            // Definition keywords
            'DEFINE', 'DEFINE20', 'DEFMAC', 'ROUTINE', 'OBJECT', 'ROOM',
            'SETG', 'CONSTANT', 'GLOBAL', 'GASSIGNED?', 'GUNASSIGN',
            'SET', 'ASSIGNED?', 'UNASSIGN', 'SYNTAX',
            
            // Boolean
            'T', 'ELSE',
        ],

        // Arithmetic operators
        arithmetic: [
            '+', '-', '*', '/', 'MOD', 'MIN', 'MAX', 'OR?', 'AND?'
        ],

        // Bitwise operators
        bitwise: [
            'BAND', 'BOR', 'ANDB', 'ORB', 'LSH', 'XORB', 'EQVB'
        ],

        // Comparison operators
        comparison: [
            '=', '==', 'N=', 'N==', 'L', 'L=', 'G', 'G=', '0?', '1?', 'T?', 'F?'
        ],

        // Property names
        properties: [
            'IN', 'LOC', 'DESC', 'SYNONYM', 'ADJECTIVE', 'FLAGS',
            'GLOBAL', 'GENERIC', 'ACTION', 'DESCFCN', 'CONTFCN', 'LDESC', 'FDESC',
            'NORTH', 'SOUTH', 'EAST', 'WEST', 'OUT', 'UP', 'DOWN', 'NW', 'SW', 'NE', 'SE'
        ],

        // Argument separators
        argSeparators: [
            'AUX', 'EXTRA', 'OPT', 'OPTIONAL', 'ARGS', 'TUPLE', 'NAME', 'BIND'
        ],

        tokenizer: {
            root: [
                // Comment prefix - comments out the next expression
                [/;/, { token: 'comment.prefix', next: '@comment' }],

                // Quote prefix - quotes the next expression
                [/'/, { token: 'keyword.quote', next: '@quoted' }],

                // Quasiquote prefix - quasiquotes the next expression
                [/`/, { token: 'keyword.quasiquote', next: '@quasiquoted' }],

                // Strings (multi-line allowed in ZIL)
                [/"/, 'string', '@string'],

                // Characters
                [/!\\\./, 'constant.character'],

                // Numbers
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number'],  // decimal
                [/\*[0-7]+\*(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.octal'],  // octal
                [/#\s*0*2\s+[01]+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.binary'],  // binary

                // Prefixes
                [/%{1,2}/, 'keyword.macro'],  // macro prefix
                [/~/, 'keyword.unquote'],  // unquote/evaluation prefix in quasiquote
                
                // Variable prefixes with names (colorize the whole thing)
                [/\.[^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'variable.local'],  // local variable
                [/,[^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'variable.global'],  // global variable

                // Segment opening (check before form to prioritize !<)
                [/!</, { token: '@brackets', bracket: '@open', next: '@segment' }],

                // Angle brackets - just regular brackets, no special form handling
                [/</, { token: '@brackets', bracket: '@open' }],
                [/>/, { token: '@brackets', bracket: '@close' }],
                
                // Other structures
                [/\(/, { token: '@brackets', bracket: '@open' }],
                [/\)/, { token: '@brackets', bracket: '@close' }],
                [/\[/, { token: '@brackets', bracket: '@open' }],
                [/\]/, { token: '@brackets', bracket: '@close' }],

                // Atoms (identifiers)
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, {
                    cases: {
                        '@keywords': 'keyword',
                        '@arithmetic': 'keyword.operator.arithmetic',
                        '@bitwise': 'keyword.operator.bitwise',
                        '@comparison': 'keyword.operator.comparison',
                        '@properties': 'storage.property',
                        '@argSeparators': 'keyword.separator',
                        '@default': 'identifier'
                    }
                }],

                // Escaped characters in atoms
                [/\\\./, 'constant.character.escape'],
            ],

            // Comment state - parse one complete expression and treat it as comment
            comment: [
                [/\s+/, 'comment'],
                
                // Strings in comments (can be multi-line) - switch to commentString (replaces comment on stack)
                [/"/, { token: 'comment', switchTo: '@commentString' }],
                
                // Characters in comments
                [/!\\\./, { token: 'comment', next: '@pop' }],
                
                // Numbers in comments
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'comment', next: '@pop' }],
                [/\*[0-7]+\*(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'comment', next: '@pop' }],
                [/#\s*0*2\s+[01]+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'comment', next: '@pop' }],
                
                // Nested comment prefix
                [/;/, 'comment'],
                
                // Structures in comments - need to track nesting
                [/</, { token: 'comment', next: '@commentForm' }],
                [/!?\(/, { token: 'comment', next: '@commentList' }],
                [/!?\[/, { token: 'comment', next: '@commentVector' }],
                
                // Prefixes in comments
                [/%{1,2}/, 'comment'],
                [/[.,']/, 'comment'],
                
                // Atoms in comments - this completes a simple comment
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'comment', next: '@pop' }],
                
                // Escaped characters
                [/\\\./, 'comment'],
            ],

            // Comment form - everything until matching >
            commentForm: [
                [/[^<>]+/, 'comment'],
                [/</, { token: 'comment', next: '@commentForm' }],  // nested form
                [/>/, { token: 'comment', next: '@pop' }],
            ],

            // Comment list - everything until matching )
            commentList: [
                [/[^()]+/, 'comment'],
                [/!?\(/, { token: 'comment', next: '@commentList' }],  // nested list
                [/!?\)/, { token: 'comment', next: '@pop' }],
            ],

            // Comment vector - everything until matching ]
            commentVector: [
                [/[^\[\]]+/, 'comment'],
                [/!?\[/, { token: 'comment', next: '@commentVector' }],  // nested vector
                [/!?\]/, { token: 'comment', next: '@pop' }],
            ],

            // Comment string - multi-line string inside a comment
            commentString: [
                [/[^\\"]+/, 'comment'],
                [/\\./, 'comment'],  // escaped character
                [/"/, { token: 'comment', next: '@pop' }],
            ],

            segment: [
                // Segment can be closed with either > or !>
                [/!>/, { token: '@brackets', bracket: '@close', next: '@pop' }],
                [/>/, { token: '@brackets', bracket: '@close', next: '@pop' }],
                
                // Everything else in segment
                { include: 'root' }
            ],

            // Quoted expressions ('expr) - highlight entire expression as quoted
            // Simple atoms/values pop immediately after consuming
            quoted: [
                // Whitespace is ignored
                [/\s+/, ''],
                // Strings complete the quote
                [/"/, 'string.quoted', '@quotedString'],
                // Forms - need to consume entire form
                [/</, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedForm' }],
                // Lists/vectors - need to consume entire structure
                [/\(/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                // Comments within quoted - still quoted color (consume one expression)
                [/;/, { token: 'comment.quoted', next: '@quotedComment' }],
                // Variable prefixes with names - treated as single quoted atom
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, { token: 'identifier.quoted', next: '@pop' }],
                // Single atom completes the quote
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'identifier.quoted', next: '@pop' }],
                // Single number completes the quote
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'number.quoted', next: '@pop' }],
                // Single character completes the quote
                [/!\\\./, { token: 'constant.character.quoted', next: '@pop' }],
            ],

            quotedString: [
                [/[^\\"]+/, 'string.quoted'],
                [/\\./, 'string.quoted'],
                [/"/, { token: 'string.quoted', next: '@pop' }],
            ],

            quotedForm: [
                // Closing > ends the form and returns to quoted context (which will then pop)
                [/>/, { token: 'delimiter.bracket.quoted', bracket: '@close', next: '@pop' }],
                // Everything inside stays quoted-colored
                [/"/, 'string.quoted', '@quotedString'],
                [/</, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedForm' }],
                [/\(/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                [/;/, { token: 'comment.quoted', next: '@quotedComment' }],
                // Variable prefixes - just part of identifier in quoted context
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quoted'],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quoted'],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.quoted'],
                [/!\\\./, 'constant.character.quoted'],
                [/\s+/, ''],
            ],

            quotedNested: [
                // Closing bracket ends the list/vector and returns to quoted context
                [/\)/, { token: 'delimiter.bracket.quoted', bracket: '@close', next: '@pop' }],
                [/\]/, { token: 'delimiter.bracket.quoted', bracket: '@close', next: '@pop' }],
                // Everything inside stays quoted-colored
                [/"/, 'string.quoted', '@quotedString'],
                [/</, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedForm' }],
                [/\(/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quoted', bracket: '@open', next: '@quotedNested' }],
                [/;/, { token: 'comment.quoted', next: '@quotedComment' }],
                // Variable prefixes - just part of identifier in quoted context
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quoted'],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quoted'],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.quoted'],
                [/!\\\./, 'constant.character.quoted'],
                [/\s+/, ''],
            ],

            quotedComment: [
                [/\s+/, ''],
                [/"/, 'comment.quoted', '@quotedCommentString'],
                [/</, 'comment.quoted', '@quotedCommentForm'],
                [/\(/, 'comment.quoted', '@quotedCommentNested'],
                [/\[/, 'comment.quoted', '@quotedCommentNested'],
                // Variable prefixes
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, { token: 'comment.quoted', next: '@pop' }],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'comment.quoted', next: '@pop' }],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'comment.quoted', next: '@pop' }],
                [/!\\\./, { token: 'comment.quoted', next: '@pop' }],
            ],

            quotedCommentString: [
                [/[^\\"]+/, 'comment.quoted'],
                [/\\./, 'comment.quoted'],
                [/"/, { token: 'comment.quoted', next: '@pop' }],
            ],

            quotedCommentForm: [
                [/>/, { token: 'comment.quoted', next: '@pop' }],
                [/./, 'comment.quoted'],
            ],

            quotedCommentNested: [
                [/\)/, { token: 'comment.quoted', next: '@pop' }],
                [/\]/, { token: 'comment.quoted', next: '@pop' }],
                [/./, 'comment.quoted'],
            ],

            // Quasiquoted expressions (`expr) - similar to quoted but allows ~ to unquote
            quasiquoted: [
                [/\s+/, ''],
                // Unquote returns to normal evaluation
                [/~/, { token: 'keyword.unquote', next: '@root' }],
                // Strings complete the quasiquote
                [/"/, 'string.quasiquoted', '@quasiquotedString'],
                // Forms - need to consume entire form, watching for ~ inside
                [/</, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedForm' }],
                // Lists/vectors - need to consume entire structure
                [/\(/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                // Comments within quasiquoted
                [/;/, { token: 'comment.quasiquoted', next: '@quasiquotedComment' }],
                // Variable prefixes with names
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, { token: 'identifier.quasiquoted', next: '@pop' }],
                // Single atom completes the quasiquote
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'identifier.quasiquoted', next: '@pop' }],
                // Single number completes
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'number.quasiquoted', next: '@pop' }],
                // Single character completes
                [/!\\\./, { token: 'constant.character.quasiquoted', next: '@pop' }],
            ],

            quasiquotedString: [
                [/[^\\"]+/, 'string.quasiquoted'],
                [/\\./, 'string.quasiquoted'],
                [/"/, { token: 'string.quasiquoted', next: '@pop' }],
            ],

            quasiquotedForm: [
                [/>/, { token: 'delimiter.bracket.quasiquoted', bracket: '@close', next: '@pop' }],
                // Unquote: parse next expression normally then return to quasiquoted
                [/~/, { token: 'keyword.unquote', next: '@unquoteInQuasiquoted' }],
                [/"/, 'string.quasiquoted', '@quasiquotedString'],
                [/</, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedForm' }],
                [/\(/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                [/;/, { token: 'comment.quasiquoted', next: '@quasiquotedComment' }],
                // Variable prefixes
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quasiquoted'],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quasiquoted'],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.quasiquoted'],
                [/!\\\./, 'constant.character.quasiquoted'],
                [/\s+/, ''],
            ],

            quasiquotedNested: [
                [/\)/, { token: 'delimiter.bracket.quasiquoted', bracket: '@close', next: '@pop' }],
                [/\]/, { token: 'delimiter.bracket.quasiquoted', bracket: '@close', next: '@pop' }],
                [/~/, { token: 'keyword.unquote', next: '@unquoteInQuasiquoted' }],
                [/"/, 'string.quasiquoted', '@quasiquotedString'],
                [/</, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedForm' }],
                [/\(/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                [/\[/, { token: 'delimiter.bracket.quasiquoted', bracket: '@open', next: '@quasiquotedNested' }],
                [/;/, { token: 'comment.quasiquoted', next: '@quasiquotedComment' }],
                // Variable prefixes
                [/[.,][^\s\t\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quasiquoted'],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, 'identifier.quasiquoted'],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, 'number.quasiquoted'],
                [/!\\\./, 'constant.character.quasiquoted'],
                [/\s+/, ''],
            ],

            // Unquote within quasiquote: parse one expression normally, then pop back
            unquoteInQuasiquoted: [
                [/\s+/, ''],
                // Parse the next expression with normal coloring
                [/"/, 'string', '@unquoteString'],
                [/</, { token: '@brackets', bracket: '@open', next: '@unquoteForm' }],
                [/\(/, { token: '@brackets', bracket: '@open', next: '@unquoteNested' }],
                [/\[/, { token: '@brackets', bracket: '@open', next: '@unquoteNested' }],
                [/\.[^\s\t\r,#':;%()\[\]<>{}".!\\]+/, { token: 'variable.local', next: '@pop' }],
                [/,[^\s\t\r,#':;%()\[\]<>{}".!\\]+/, { token: 'variable.global', next: '@pop' }],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'identifier', next: '@pop' }],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'number', next: '@pop' }],
                [/!\\\./, { token: 'constant.character', next: '@pop' }],
            ],

            unquoteString: [
                [/[^\\"]+/, 'string'],
                [/\\./, 'string.escape'],
                [/"/, { token: 'string', next: '@pop' }],
            ],

            unquoteForm: [
                [/>/, { token: '@brackets', bracket: '@close', next: '@pop' }],
                { include: 'root' }
            ],

            unquoteNested: [
                [/\)/, { token: '@brackets', bracket: '@close', next: '@pop' }],
                [/\]/, { token: '@brackets', bracket: '@close', next: '@pop' }],
                { include: 'root' }
            ],

            quasiquotedComment: [
                [/\s+/, ''],
                [/"/, 'comment.quasiquoted', '@quasiquotedCommentString'],
                [/</, { token: 'comment.quasiquoted', next: '@quasiquotedCommentForm' }],
                [/!?[([]/, { token: 'comment.quasiquoted', next: '@quasiquotedCommentNested' }],
                [/[^\s\t-\r,#':;%()\[\]<>{}".!\\]+/, { token: 'comment.quasiquoted', next: '@pop' }],
                [/-?\d+(?![^\s\t-\r,#':;%()\[\]<>{}"])/, { token: 'comment.quasiquoted', next: '@pop' }],
                [/!\\\./, { token: 'comment.quasiquoted', next: '@pop' }],
            ],

            quasiquotedCommentString: [
                [/[^\\"]+/, 'comment.quasiquoted'],
                [/\\./, 'comment.quasiquoted'],
                [/"/, { token: 'comment.quasiquoted', next: '@pop' }],
            ],

            quasiquotedCommentForm: [
                [/>/, { token: 'comment.quasiquoted', next: '@pop' }],
                [/./, 'comment.quasiquoted'],
            ],

            quasiquotedCommentNested: [
                [/!?[)\]]/, { token: 'comment.quasiquoted', next: '@pop' }],
                [/./, 'comment.quasiquoted'],
            ],

            string: [
                [/[^\\"]+/, 'string'],
                [/\\./, 'string.escape'],
                [/"/, 'string', '@pop']
            ],
        },
    });

    // Define a theme for ZIL (optional - uses default theme if not specified)
    monaco.editor.defineTheme('zil-theme', {
        base: 'vs-dark',
        inherit: true,
        rules: [
            { token: 'comment', foreground: '6A9955' },
            { token: 'keyword', foreground: 'C586C0' },
            { token: 'keyword.definition', foreground: 'C586C0', fontStyle: 'bold' },
            { token: 'keyword.operator', foreground: 'D4D4D4' },
            { token: 'keyword.macro', foreground: 'DCDCAA' },
            { token: 'keyword.quote', foreground: 'C586C0' },
            { token: 'keyword.quasiquote', foreground: 'C586C0' },
            { token: 'keyword.unquote', foreground: 'C586C0' },
            { token: 'string', foreground: 'CE9178' },
            { token: 'string.quoted', foreground: 'CE9178', fontStyle: 'italic' },
            { token: 'string.quasiquoted', foreground: 'CE9178', fontStyle: 'italic' },
            { token: 'identifier.quoted', foreground: 'C586C0' },
            { token: 'identifier.quasiquoted', foreground: 'C586C0' },
            { token: 'number.quoted', foreground: 'C586C0' },
            { token: 'number.quasiquoted', foreground: 'C586C0' },
            { token: 'comment.quoted', foreground: '6A9955' },
            { token: 'comment.quasiquoted', foreground: '6A9955' },
            { token: 'number', foreground: 'B5CEA8' },
            { token: 'entity.name.function', foreground: 'DCDCAA' },
            { token: 'entity.name.object', foreground: '4EC9B0' },
            { token: 'entity.name.type', foreground: '4EC9B0' },
            { token: 'variable.global', foreground: '9CDCFE' },
            { token: 'variable.local', foreground: '9CDCFE' },
            { token: 'storage.property', foreground: 'C586C0' },
            { token: 'constant.character', foreground: 'CE9178' },
            { token: 'punctuation', foreground: 'D4D4D4' },
            { token: 'delimiter.bracket', foreground: 'FFD700' },
            { token: 'delimiter.bracket.quoted', foreground: 'C586C0' },
            { token: 'delimiter.bracket.quasiquoted', foreground: 'C586C0' },
        ],
        colors: {}
    });
    
    console.log('ZIL language and theme setup complete!');
    }

    // Try to register immediately if Monaco is already loaded
    if (typeof monaco !== 'undefined') {
        registerZilLanguage();
    } else if (typeof require !== 'undefined') {
        // Otherwise wait for Monaco to load via AMD
        require(['vs/editor/editor.main'], registerZilLanguage);
    } else {
        // Last resort: wait for window load
        window.addEventListener('load', function() {
            if (typeof monaco !== 'undefined') {
                registerZilLanguage();
            } else {
                console.error('Monaco editor not found!');
            }
        });
    }
})();
