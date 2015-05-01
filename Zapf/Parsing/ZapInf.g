/* Copyright 2010, 2012 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

// INFORM MODE LEXER

// TODO: add a .CUSTOM directive to define new opcodes?

lexer grammar ZapInf;

options {
	language = CSharp3;
	tokenVocab = Zap;
}

tokens { LLABEL; GLABEL; }

@namespace { Zapf.Lexing }

@header {
	using StringBuilder = System.Text.StringBuilder;
}

@members {
	internal System.Collections.IDictionary OpcodeDict;
	
	private bool IsOpcode(string text) {
		return OpcodeDict.Contains(text);
	}
	
	private static string UnquoteString(string text) {
		StringBuilder sb = new StringBuilder(text, 1, text.Length - 2, text.Length - 2);
		
		for (int i = sb.Length - 1; i >= 0; i--)
			if (sb[i] == '"')
				sb.Remove(i--, 1);
		
		return sb.ToString();
	}
}

ALIGN	:	'.ALIGN';
BYTE	:	'.BYTE';
CHRSET	:	'.CHRSET';
DEFSEG	:	'.DEFSEG';
END	:	'.END';
ENDI	:	'.ENDI';
ENDSEG	:	'.ENDSEG';
ENDT	:	'.ENDT';
FALSE	:	'.FALSE';
FSTR	:	'.FSTR';
FUNCT	:	'.FUNCT';
GSTR	:	'.GSTR';
GVAR	:	'.GVAR';
INSERT	:	'.INSERT';
LANG	:	'.LANG';
LEN	:	'.LEN';
NEW	:	'.NEW';
OBJECT	:	'.OBJECT';
OPTIONS	:	'.OPTIONS';
PAGE	:	'.PAGE';
PCSET	:	'.PCSET';
PDEF	:	'.PDEF';
PICFILE	:	'.PICFILE';
PROP	:	'.PROP';
SEGMENT	:	'.SEGMENT';
SEQ	:	'.SEQ';
SOUND	:	'.SOUND';
STR	:	'.STR';
STRL	:	'.STRL';
TABLE	:	'.TABLE';
TIME	:	'.TIME';
TRUE	:	'.TRUE';
VOCBEG	:	'.VOCBEG';
VOCEND	:	'.VOCEND';
WORD	:	'.WORD';
ZWORD	:	'.ZWORD';

DEBUG_ACTION
	:	'.DEBUG-ACTION';
DEBUG_ARRAY
	:	'.DEBUG-ARRAY';
DEBUG_ATTR
	:	'.DEBUG-ATTR';
DEBUG_CLASS
	:	'.DEBUG-CLASS';
DEBUG_FAKE_ACTION
	:	'.DEBUG-FAKE-ACTION';
DEBUG_FILE
	:	'.DEBUG-FILE';
DEBUG_GLOBAL
	:	'.DEBUG-GLOBAL';
DEBUG_LINE
	:	'.DEBUG-LINE';
DEBUG_MAP
	:	'.DEBUG-MAP';
DEBUG_OBJECT
	:	'.DEBUG-OBJECT';
DEBUG_PROP
	:	'.DEBUG-PROP';
DEBUG_ROUTINE
	:	'.DEBUG-ROUTINE';
DEBUG_ROUTINE_END
	:	'.DEBUG-ROUTINE-END';

EQUALS	:	'=';
COMMA	:	',';
PLUS	:	'+';
APOSTROPHE
	:	'\'';
COLON	:	':';
DCOLON	:	'::';

ARROW	:	'->';
QUEST	:	'?';
TILDE	:	'~';

fragment NUM
	:	'-'? '0'..'9'+
	;

fragment NSYMBOL
	:	~('A'..'Z' | 'a'..'z' | '0'..'9' | '-' | '_' | '$' | '#' | '&' | '.')
	;

fragment SYMBOL
	:	('A'..'Z' | 'a'..'z' | '0'..'9' | '-' | '_' | '$' | '#' | '&' | '.')+
	;

fragment OPCODE
	:	'OPCODE' /* not actually used, see below */;

SYMBOL_OR_NUM
	:	(NUM NSYMBOL)=>		NUM	{ $type = NUM; }
	|	SYMBOL				{ $type = IsOpcode($text) ? OPCODE : SYMBOL; }
	;

fragment SPACE
	:	' ' | '\t' | '\f'
	;

WS	:	SPACE+				{ $channel = Hidden; }
	;

CRLF
	:	('\r' | '\n')+
	;

STRING	:	'"'
		(~'"' | '""')*
		'"'				{ $text = UnquoteString($text); }
	;

COMMENT	:	';' ~('\r' | '\n')*		{ $channel = Hidden; }
	;
