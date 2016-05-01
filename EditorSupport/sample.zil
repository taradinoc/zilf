"Syntax Highlighting Sample"

"=== Comments ==="

;"This is a commented string."

;(This is a commented list)

;[This is a commented vector]

;![This is a commented uvector]
;![This one ends with a bang bracket !]

;<THIS IS A <COMMENTED <FORM> (WITH LIST) AND #OTHER STUFF>>

;.COMMENTED-LVAL
;,COMMENTED-GVAL
;'COMMENTED-QUOTE
;!.COMMENTED-SEG-LVAL
;!,COMMENTED-SEG-GVAL
;!<COMMENTED-SEGMENT>
;%<COMMENTED-MACRO>
;%%<COMMENTED-VMACRO>

; "Comment with space after semicolon"
; <WORKS FOR THESE TOO>

"=== Quotations ==="

'"This is a quoted string."
'<THIS IS A <QUOTED <FORM (ET CETERA)>>>
'.QUOTED-LVAL
',QUOTED-GVAL
''QUOTED-QUOTATION
'!.QUOTED-SEGMENT
'%<QUOTED-MACRO>
'%%<QUOTED-VMACRO>

"=== Structures ==="

<THIS IS A <FORM (WITH LIST) AND #OTHER STUFF 123>>

(THIS IS A (NESTED LIST (WITH MORE (NESTING))))

[THIS IS A VECTOR [WITH INNER VECTOR]]
![THIS IS A UVECTOR ![INNER ONE ENDS WITH BANG BRACKET !] ]

<TELL "This string has \"nested\" quotes.">
<TELL "This string|
spans multiple|
lines">

"=== Local and global variable references ==="
.LVAL
. LVAL-WITH-SPACE
,GVAL
, GVAL-WITH-SPACE
..LVAL-LVAL
.,LVAL-GVAL
,,GVAL-GVAL
,.GVAL-LVAL
.<LVAL-FORM>
,<GVAL-FORM>

"=== Segments ==="
!.SEG-LVAL
!,SEG-GVAL
!<SEGMENT>

! .SEG-WITH-SPACE
! ,SEG-WITH-SPACE
! <SEG-WITH-SPACE>

"=== READ Macros==="
%<MACRO>
%%<VMACRO>
%.FOO
%%.FOO
%,FOO
%%,FOO

<FOO %<MACRO>>		;"call FOO with macro result arg"

"=== Character literals ==="
!\C		;"literal C"
!\ 		;"literal space"
!\CDEF		;"literal C followed by atom DEF"

"=== Decimal numbers ==="
1234567890
-1234567890

"=== Octal numbers ==="
*12345670*
*123*\A		;"octal 123 followed by atom A"
*123*,A		;"octal 123 followed by gval ,A"

"=== Binary numbers ==="
#2 11001010

"=== Hashed (chtyped) expressions ==="
#FALSE (HEY NOW)
#BYTE 255
#HASH ATOM

"=== False ==="
<>

"=== Atoms ==="
FOO
FOO-BAR
-BAR
FOO\<BAR
ATOM-WITH-TRAILING-SPACE\ 
ATOM\ WITH\ INNER\ SPACES
\123
\-123
12\3
-12\3

"=== Atoms that start out looking like numbers ==="
1234567890?
-1234567890?
123-
-123-
--123
--
-

"=== Atoms that start out looking like octal numbers ==="
**
***
*12345670
*12345670**
*12345678*
*1234567A*
*123*.A		;"dot is allowed in an atom"
\*123*
*1\23*
*123\*
