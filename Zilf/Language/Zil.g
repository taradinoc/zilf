/* Copyright 2010-2017 Jesse McGrew
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
	language = CSharp3;
	output = AST;
}

tokens { COMMENT; FORM; LIST; VECTOR; UVECTOR; SEGMENT; MACRO; VMACRO; HASH; GVAL; LVAL; ADECL; TEMPLATE; }

@lexer::namespace { Zilf.Language.Lexing }
@parser::namespace { Zilf.Language.Parsing }

@members {
	private List<SyntaxError> syntaxErrors = new List<SyntaxError>();

	internal IEnumerable<SyntaxError> SyntaxErrors { get { return syntaxErrors; } }

	public override void DisplayRecognitionError(string[] tokenNames, RecognitionException ex) {
		syntaxErrors.Add(new SyntaxError($file::filename, ex.Line, GetErrorMessage(ex, tokenNames)));
	}
}

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

WS	:	SPACE+				{ $channel = Hidden; }
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
LCURLY	:	'{';
RCURLY	:	'}';
BANG	:	'!';
DOT	:	'.';
COMMA	:	',';
APOS	:	'\'';


public
file[string theFilename]
scope { string filename }
@init { $file::filename = $theFilename; }
	:	(comment | noncomment_expr)* EOF!
	;

expr	:	comment* noncomment_expr
	;

comment	:	SEMI noncomment_expr	-> ^(COMMENT noncomment_expr)
	;

noncomment_expr
	:	a=non_adecl_expr
		( (COLON)=> COLON b=non_adecl_expr	-> ^(ADECL $a $b)
		|									-> $a
		)
	;

non_adecl_expr
	:	ATOM
	|	NUM
	|	CHAR
	|	STRING
	|	form
	|	segment
	|	vector
	|	list
	|	macro
	|	HASH ATOM expr		-> ^(HASH ATOM expr)
	|	template
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
	|	h=DOT non_adecl_expr		-> ^(FORM[$h] ATOM["LVAL"] non_adecl_expr)
	|	h=COMMA non_adecl_expr		-> ^(FORM[$h] ATOM["GVAL"] non_adecl_expr)
	|	h=APOS non_adecl_expr		-> ^(FORM[$h] ATOM["QUOTE"] non_adecl_expr)
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

template
	:	LCURLY comment_or_expr* RCURLY -> ^(TEMPLATE comment_or_expr*)
	;
