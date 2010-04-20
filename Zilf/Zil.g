/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

grammar Zil;

options {
	language = CSharp2;
	output = AST;
}

tokens { COMMENT; FORM; LIST; VECTOR; UVECTOR; SEGMENT; MACRO; VMACRO; HASH; GVAL; LVAL; ADECL; }

@lexer::namespace { Zilf.Lexing }
@parser::namespace { Zilf.Parsing }

fragment DIGIT
	:	'0'..'9'
	;

STRING	:	'"'
		(~('\\' | '"') | '\\' .)*
		'"'
	;

CHAR	:	'!' '\\' .
	;

fragment SPACE
	:	' ' | '\t' | '\r' | '\n' | '\f'
	;

WS	:	SPACE+				{ $channel = HIDDEN; }
	;

fragment ATOM_HEAD
	:	~(NATOM | '.' | '!')
	|	'\\' .
	;

fragment ATOM_TAIL
	:	~(NATOM)
	|	'\\' .
	;

fragment NATOM
	:	SPACE | '<' | '>' | '(' | ')' | '{' | '}' | '[' | ']' | ':' | ';' | '"' | '\'' | '\\' | ',' | '%' | '#'
	;

fragment ATOM
	:	ATOM_HEAD ATOM_TAIL*
	;

fragment NUM
	:	('-' | '+')? DIGIT+		// decimal
	|	'*' '0'..'7'+ '*'		// octal
	|	'#2' SPACE+ '0'..'1'+		// binary
	;

ATOM_OR_NUM
	:	(NUM (EOF | NATOM))=>	NUM	{ $type = NUM; }
	|	ATOM				{ $type = ATOM; }
	;

SEMI	:	';';
COLON	:	':';
HASH	:	'#';
PERCENT	:	'%';
LANGLE	:	'<';
RANGLE	:	'>';
LPAREN	:	'(';
RPAREN	:	')';
LSQUARE	:	'[';
RSQUARE	:	']';
BANG	:	'!';
DOT	:	'.';
COMMA	:	',';
APOS	:	'\'';


file	:	(comment | noncomment_expr)*
	;

expr	:	comment* noncomment_expr
	;

comment	:	SEMI noncomment_expr	-> ^(COMMENT noncomment_expr)
	;

noncomment_expr
	:	ATOM
	|	ATOM COLON ATOM		-> ^(ADECL ATOM ATOM)
	|	NUM
	|	CHAR
	|	STRING
	|	form
	|	segment
	|	vector
	|	list
	|	macro
	|	HASH ATOM expr		-> ^(HASH ATOM expr)
	;

macro
	:	PERCENT
		( (PERCENT)=> PERCENT expr	-> ^(VMACRO expr)
		| expr				-> ^(MACRO expr) )
	;

comment_or_expr
	:	comment
	|	noncomment_expr
	;

form	:	h=LANGLE comment_or_expr* RANGLE
					-> ^(FORM[$h] comment_or_expr*)
	|	h=DOT expr		-> ^(FORM[$h] ATOM["LVAL"] expr)
	|	h=COMMA expr		-> ^(FORM[$h] ATOM["GVAL"] expr)
	|	h=APOS expr		-> ^(FORM[$h] ATOM["QUOTE"] expr)
	;

segment	:	BANG form		-> ^(SEGMENT form)
	;

list	:	LPAREN comment_or_expr* RPAREN
					-> ^(LIST comment_or_expr*)
	;

vector	:	LSQUARE comment_or_expr* RSQUARE
					-> ^(VECTOR comment_or_expr*)
	|	BANG LSQUARE comment_or_expr* BANG? RSQUARE
					-> ^(UVECTOR comment_or_expr*)
	;
