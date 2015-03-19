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

// TODO: add a .CUSTOM directive to define new opcodes?

grammar Zap;

options {
	language = CSharp3;
	output = AST;
}

tokens { LLABEL; GLABEL; QUEST; TILDE; ARROW; }

@lexer::namespace { Zapf.Lexing }
@parser::namespace { Zapf.Parsing }

@lexer::header {
	using StringBuilder = System.Text.StringBuilder;
}

@lexer::members {
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

@parser::members {
	internal bool InformMode;
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
SLASH	:	'/';
BACKSLASH
	:	'\\';
RANGLE	:	'>';
PLUS	:	'+';
APOSTROPHE
	:	'\'';
COLON	:	':';
DCOLON	:	'::';

fragment NUM
	:	'-'? '0'..'9'+
	;

fragment NSYMBOL
	:	~('A'..'Z' | 'a'..'z' | '0'..'9' | '-' | '?' | '$' | '#' | '&' | '.')
	;

fragment SYMBOL
	:	('A'..'Z' | 'a'..'z' | '0'..'9' | '-' | '?' | '$' | '#' | '&' | '.')
		('A'..'Z' | 'a'..'z' | '0'..'9' | '-' | '?' | '$' | '#' | '&' | '.' | '\'')*
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

public
file
	:	CRLF!* (label | label? line) (CRLF!+ (label | label? line))* CRLF!* EOF!;

label	:	s=SYMBOL
		( DCOLON	-> ^(GLABEL[$s])
		| COLON		-> ^(LLABEL[$s]) )
	;

symbol	:	SYMBOL
	|	OPCODE		-> SYMBOL[$OPCODE]
	;

line
	:	(OPCODE)=>
			( OPCODE^ operands
			| OPCODE^ STRING)
	|	(SYMBOL)=>
			( SYMBOL
				(					-> ^(WORD SYMBOL)
				| (expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW)=>
					( operands			-> ^(SYMBOL operands)
					| STRING			-> ^(SYMBOL STRING)
					)
				| EQUALS expr				-> ^(EQUALS SYMBOL expr)
				)
			)
	|	meta_directive
	|	data_directive
	|	funct_directive
	|	table_directive
	|	debug_directive
	;

operands:	(	{InformMode}?	inf_operands
		|	{!InformMode}?	zap_operands)
	;

inf_operands
	:	expr* inf_spec_operand*
	;

inf_spec_operand
scope { bool neg; }
	:	QUEST {$inf_spec_operand::neg = false;}
		(TILDE {$inf_spec_operand::neg = true;})?
		(	SYMBOL					-> SYMBOL
		|	OPCODE
			(	{$OPCODE.text == "rtrue"}?	-> TRUE[$OPCODE]
			|	{$OPCODE.text == "rfalse"}?	-> FALSE[$OPCODE]))
		(	{$inf_spec_operand::neg}?		-> ^(BACKSLASH[$QUEST] $inf_spec_operand)
		|	{!$inf_spec_operand::neg}?		-> ^(SLASH[$QUEST] $inf_spec_operand))
	|	ARROW symbol					-> ^(RANGLE[$ARROW] symbol)
	;

zap_operands:	(expr (COMMA! expr)*)? zap_spec_operand*
	;

zap_spec_operand
	:	(s=SLASH | s=BACKSLASH) SYMBOL
		(	{$SYMBOL.text == "TRUE"}?	-> ^($s TRUE)
		|	{$SYMBOL.text == "FALSE"}?	-> ^($s FALSE)
		|					-> ^($s SYMBOL))
	|	RANGLE^ symbol
	;

expr	:	term (PLUS^ term)*
	;

term	:	NUM
	|	symbol
	|	APOSTROPHE^ symbol
	;

meta_directive
	:	NEW^ expr?
	|	TIME
	|	INSERT^ STRING
	|	END
	|	ENDI
	;

data_directive
	:	(BYTE | WORD)^ expr (COMMA! expr)*
	|	expr (COMMA expr)*		-> ^(WORD expr+)
	|	(TRUE | FALSE)
	|	PROP^ expr COMMA! expr
	|	(LEN | STR | STRL | ZWORD)^ STRING
	|	(FSTR | GSTR)^ SYMBOL COMMA! STRING
	|	GVAR^ SYMBOL (EQUALS! expr (COMMA! SYMBOL)?)?
	|	OBJECT^ expr
		COMMA! expr COMMA! expr (COMMA! expr)?
		COMMA! expr COMMA! expr COMMA! expr
		COMMA! expr
	;

funct_directive
	:	FUNCT^ SYMBOL (COMMA! funct_param)*
	;

funct_param
	:	symbol (EQUALS^ expr)?
	;

table_directive
	:	TABLE^ NUM?
	|	ENDT
	|	VOCBEG^ NUM COMMA! NUM
	|	VOCEND
	;

debug_directive
	:	DEBUG_ACTION^ expr COMMA! STRING
	|	DEBUG_ARRAY^ expr COMMA! STRING
	|	DEBUG_ATTR^ expr COMMA! STRING
	|	DEBUG_CLASS^ STRING COMMA! debug_lineref COMMA! debug_lineref
	|	DEBUG_FAKE_ACTION^ expr COMMA! STRING
	|	DEBUG_FILE^ expr COMMA! STRING COMMA! STRING
	|	DEBUG_GLOBAL^ expr COMMA! STRING
	|	DEBUG_LINE^ debug_lineref
	|	DEBUG_MAP^ STRING EQUALS! expr
	|	DEBUG_OBJECT^ expr COMMA! STRING COMMA! debug_lineref COMMA! debug_lineref
	|	DEBUG_PROP^ expr COMMA! STRING
	|	DEBUG_ROUTINE^ debug_lineref COMMA! STRING (COMMA! STRING)*
	| 	DEBUG_ROUTINE_END^ debug_lineref
	;

debug_lineref
	:	expr COMMA! expr COMMA! expr
	;
