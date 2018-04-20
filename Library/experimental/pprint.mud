<PACKAGE "PPRINT">

"Pretty PRINTer for ZILF"

"This package is based on the Guile pretty printer (ice-9 pretty-print),
 released under the GPLv3:
 https://github.com/skangas/guile/blob/master/module/ice-9/pretty-print.scm"

"The main entry point is PPRINT, which takes a single argument. If given an
 atom, it will try to look up a definition by that name, and print it as
 a call to DEFINE, DEFMAC, ROUTINE, OBJECT, SETG, etc. Otherwise it will
 simply print the object."

"An alternate entry point is PRETTY-PRINT, which prints the argument exactly
 as given even if it's an atom, and also takes some optional parameters to
 control the formatting."

"By default, a few types such as LIST and FUNCTION will have their PRINTTYPEs
 set to PPRINT when this package is loaded. To change that, call PPRINT-TYPES
 after loading the package:

     <PPRINT-TYPES FUNCTION LIST>  ;only use PPRINT for these types
     <PPRINT-TYPES <>>             ;don't use PPRINT for any types"

<ENTRY PPRINT PRETTY-PRINT PPRINT-TYPES>

<DEFINE DEBUG ('X) ;<FORM PRINC .X> #SPLICE ()>

<SETG CRLF-STRING <STRING <ASCII 13> <ASCII 10>>>

<DEFINE LENGTH1? (OBJ)
    <AND <BRACKETED? .OBJ> <1? <LENGTH? .OBJ 1>>>>

<DEFINE READ-MACRO? (F)
    %<DEBUG "[READ-MACRO? ">
    %<DEBUG <UNPARSE .F>>
    %<DEBUG "]">
    <AND <TYPE? .F FORM SEGMENT>
         <NOT <LENGTH? .F 0>>
         <MEMQ <1 .F> '(QUOTE GVAL LVAL)>
         <LENGTH1? <REST .F>>>>

<DEFINE READ-MACRO-BODY (F)
    %<DEBUG "[READ-MACRO-BODY ">
    %<DEBUG <UNPARSE .F>>
    %<DEBUG "]">
    <2 .F>>

<DEFINE READ-MACRO-PREFIX (F "AUX" HEAD)
    %<DEBUG "[READ-MACRO-PREFIX ">
    %<DEBUG <UNPARSE .F>>
    %<DEBUG "]">
    <SET HEAD <1 .F>>
    <STRING <COND (<TYPE? .F SEGMENT> "!") (ELSE "")>
            <COND (<==? .HEAD QUOTE> "'")
                  (<==? .HEAD GVAL> ",")
                  (<==? .HEAD LVAL> ".")>>>

<DEFINE OUT (STR COL)
    <AND .COL <OUTPUT .STR> <+ .COL <LENGTH .STR>>>>

<DEFINE WR-STRUC (OBJ COL BRA KET)
    <SET COL <WR <1 .OBJ> <OUT .BRA .COL>>>
    <MAPF <>
        <FUNCTION (I)
            <SET COL <WR .I <OUT " " .COL>>>>
        <REST .OBJ>>
    <OUT .KET .COL>>

<DEFINE OBJ-TO-STRING (OBJ "AUX" RES (T <TYPE .OBJ>) (PT <PRINTTYPE .T>))
    ;"XXX use .PRINC?"
    <PRINTTYPE .T ,PRINT>
    <SET RES <UNPARSE .OBJ>>
    <AND .PT <PRINTTYPE .T .PT>>
    .RES>

<SETG STD-BRACKETS '(LIST ["(" ")"]
                     FORM ["<" ">"]
                     VECTOR ["[" "]"]
                     SEGMENT ["!<" ">"]
                     ;"TABLE is its own PRIMTYPE"
                     TABLE ["#TABLE (" ")"])>

<DEFINE BRA&KET (L "AUX" R)
    <COND (<SET R <MEMQ <TYPE .L> ,STD-BRACKETS>>
           <2 .R>)
          (<SET R <MEMQ <PRIMTYPE .L> ,STD-BRACKETS>>
           [<STRING !\# <SPNAME <TYPE .L>> " " <1 <2 .R>>>
            <2 <2 .R>>])
          (ELSE <ERROR UNRECOGNIZED-STRUC-TYPE <TYPE .L>>)>>

<DEFINE BRACKETED? (OBJ)
    <AND <STRUCTURED? .OBJ>
         <NOT <TYPE? .OBJ STRING>>
         <NOT <EMPTY? .OBJ>>>>

<DEFINE WR (OBJ COL)
    %<DEBUG "[WR ">
    %<DEBUG .OBJ>
    %<DEBUG " ">
    %<DEBUG .COL>
    %<DEBUG "]">
    <COND (<READ-MACRO? .OBJ>
           %<DEBUG "[WR1]">
           <WR <READ-MACRO-BODY .OBJ> <OUT <READ-MACRO-PREFIX .OBJ> .COL>>)
          (<=? .OBJ <>>
           <OUT "<>" .COL>)
          (<BRACKETED? .OBJ>
           %<DEBUG "[WR2]">
           ;"A proper list: do our own list printing so as to catch read
             macros that appear in the middle of the list."
           <WR-STRUC .OBJ .COL !<BRA&KET .OBJ>>)
          (ELSE
           %<DEBUG "[WR3]">
           <OUT <OBJ-TO-STRING .OBJ> .COL>)>>

<DEFINE SPACES (N COL)
    <COND (<G? .N 0>
           <COND (<G? .N 7>
                  <SPACES <- .N 8> <OUT "        " .COL>>)
                 (ELSE
                  <OUT <SUBSTRUC "        " 0 .N> .COL>)>)
          (ELSE .COL)>>

<DEFINE INDENT (TO COL)
    %<DEBUG "[INDENT ">
    %<DEBUG .TO>
    %<DEBUG " ">
    %<DEBUG .COL>
    %<DEBUG "]">
    <AND .COL
         <COND (<L? .TO .COL>
                <AND <OUT ,CRLF-STRING .COL>
                     <OUT .PER-LINE-PREFIX 0>
                     <SPACES .TO 0>>)
               (ELSE
                <SPACES <- .TO .COL> .COL>)>>>

;"Prints some shit.

  OBJ: The object to print.
  COL: The column where we are now.
  EXTRA: The number of extra columns to reserve at the right margin.
  PP-STRUC: The PP function to use for printing the object if it needs to be
    split across multiple lines.

  Returns: The column where we are after printing."
<DEFINE PR (OBJ COL EXTRA PP-STRUC "AUX" (RESULT '()) LEFT)
    %<DEBUG "[PR ">
    %<DEBUG <TYPE .OBJ>>
    %<DEBUG " ">
    %<DEBUG .COL>
    %<DEBUG " ">
    %<DEBUG .EXTRA>
    %<DEBUG "]">
    <COND (<BRACKETED? .OBJ>	;"may have to split on multiple lines"
           <SET LEFT <MIN <+ <- .WIDTH .COL .EXTRA> 1> .MAX-EXPR-WIDTH>>
           <GENERIC-WRITE .OBJ .PRINC? <> .MAX-EXPR-WIDTH ""
               <FUNCTION (STR)
                   <SET RESULT <CONS .STR .RESULT>>
                   <SET LEFT <- .LEFT <LENGTH .STR>>>
                   <G? .LEFT 0>>>
           <COND (<G? .LEFT 0>  ;"all can be printed on one line"
                  <OUT <REVERSE-STRING-APPEND .RESULT> .COL>)
                 (ELSE
                  <APPLY .PP-STRUC .OBJ .COL .EXTRA>)>)
          (ELSE
           <WR .OBJ .COL>)>>

<DEFINE PP-EXPR (EXPR COL EXTRA "AUX" HEAD PROC)
    <COND (<READ-MACRO? .EXPR>
           <PR <READ-MACRO-BODY .EXPR>
               <OUT <READ-MACRO-PREFIX .EXPR> .COL>
               .EXTRA
               ,PP-EXPR>)
          (<=? .EXPR <>>
           <OUT "<>" .COL>)
          (<TYPE? .EXPR FORM SEGMENT>
           <SET HEAD <1 .EXPR>>
           <COND (<TYPE? .HEAD ATOM>
                  <SET PROC <STYLE .HEAD>>
                  <COND (.PROC
                         <APPLY .PROC .EXPR .COL .EXTRA>)
                        (<G? <LENGTH <SPNAME .HEAD>> ,MAX-CALL-HEAD-WIDTH>
                         <PP-GENERAL .EXPR .COL .EXTRA <> <> <> ,PP-EXPR>)
                        (ELSE
                         <PP-CALL .EXPR .COL .EXTRA ,PP-EXPR>)>)
                 (ELSE
                  <PP-STRUC .EXPR .COL .EXTRA ,PP-EXPR>)>)
          (ELSE
           <PP-STRUC .EXPR .COL .EXTRA ,PP-EXPR>)>>

;"(head item1
        item2
        item3)"
<DEFINE PP-CALL (EXPR COL EXTRA PP-ITEM "AUX" COL* (BK <BRA&KET .EXPR>))
    <SET COL* <WR <1 .EXPR> <OUT <1 .BK> .COL>>>
    <AND .COL
         <PP-DOWN <REST .EXPR> .COL* <+ .COL* 1> .EXTRA .PP-ITEM <2 .BK>>>>

;"(item1
   item2
   item3)"
<DEFINE PP-STRUC (L COL EXTRA PP-ITEM "AUX" BK)
    <SET BK <BRA&KET .L>>
    <SET COL <OUT <1 .BK> .COL>>
    <PP-DOWN .L .COL .COL .EXTRA .PP-ITEM <2 .BK>>>

;"Prints the items of L downward, one per line, then ends with KET.

  L: The list of items to print.
  COL1: The column where we are now.
  COL2: The column where the items should be lined up.
  EXTRA: The number of extra columns to reserve at the right margin.
  PP-ITEM: The PP function to use for printing each item.
  KET: The closing bracket to print on the last line.
  
  Returns: The column where we are after printing."
<DEFINE PP-DOWN (L COL1 COL2 EXTRA PP-ITEM KET)
    %<DEBUG "[PP-DOWN ">
    %<DEBUG .COL1>
    %<DEBUG " ">
    %<DEBUG .COL2>
    %<DEBUG "]">
    <PROG ()
        <AND .COL1
             <COND (<EMPTY? .L>
                    %<DEBUG "[PPD1]">
                    <OUT .KET .COL1>)
                   (ELSE
                    %<DEBUG "[PPD2]">
                    <BIND ((REST <REST .L>)
                           (EXTRA <COND (<EMPTY? .REST> <+ .EXTRA <LENGTH .KET>>)
                                        (ELSE 0)>))
                        <SET COL1 <PR <1 .L>
                                      <INDENT .COL2 .COL1>
                                      .EXTRA
                                      .PP-ITEM>>
                        <SET L .REST>
                        <AGAIN>>)>>>>

;"Helper for PP-GENERAL.

  REST: The items left to print.
  COL1: The column where the final items (handled by the last PP function)
    should be lined up.
  COL2: The column where we are now.
  COL3: The column where the initial items (handled by PP functions before the last)
    should be lined up.

  Returns: The column where we are after printing."
<DEFINE TAIL1 (REST COL1 COL2 COL3)
    <COND (<AND .PP-1 <TYPE? .REST LIST> <NOT <EMPTY? .REST>>>
           <BIND ((VAL1 <1 .REST>)
                  (REST <REST .REST>)
                  (EXTRA* <COND (<EMPTY? .REST> <+ .EXTRA 1>) (ELSE 0)>))
               <TAIL2 .REST .COL1 <PR .VAL1 <INDENT .COL3 .COL2> .EXTRA* .PP-1> .COL3>>)
          (ELSE
           <TAIL2 .REST .COL1 .COL2 .COL3>)>>

<DEFINE TAIL2 (REST COL1 COL2 COL3)
    <COND (<AND .PP-2 <TYPE? .REST LIST> <NOT <EMPTY? .REST>>>
           <BIND ((VAL1 <1 .REST>)
                  (REST <REST .REST>)
                  (EXTRA* <COND (<EMPTY? .REST> <+ .EXTRA 1>) (ELSE 0)>))
               <TAIL3 .REST .COL1 <PR .VAL1 <INDENT .COL3 .COL2> .EXTRA* .PP-2>>>)
          (ELSE
           <TAIL3 .REST .COL1 .COL2>)>>

<DEFINE TAIL3 (REST COL1 COL2)
    <PP-DOWN .REST .COL2 .COL1 .EXTRA .PP-3 .KET>>

;"Prints a structured expression using the supplied PP functions for various parts.

  EXPR: The object to print.
  COL: The column where we are now.
  EXTRA: The number of extra columns to reserve at the right margin.
  NAMED?: If true, the 2nd element of the structure will be printed on the same line
    as the 1st, and the PP functions shift down by 1 (e.g. PP-1 is used for the 3rd).
  PP-1: The PP function to use for the 2nd element. If omitted, the remaining PP
    functions shift up by 1.
  PP-2: The PP function to use for the 3rd element. If omitted, the remaining PP
    functions shift up by 1.
  PP-3: The PP function to use for the 4th and following elements.

  Returns: The column where we are after printing."
<DEFINE PP-GENERAL (EXPR COL EXTRA NAMED? PP-1 PP-2 PP-3)
    <BIND ((HEAD <1 .EXPR>)
           (REST <REST .EXPR>)
           (BK <BRA&KET .EXPR>)
           (BRA <1 .BK>)
           (KET <2 .BK>)
           (COL* <WR .HEAD <OUT .BRA .COL>>))
        <REPEAT ()
            <COND (<AND .NAMED?
                        <COND (<TYPE? .NAMED? FIX> <G? .NAMED? 0>)
                              (ELSE <SET .NAMED? 1>)>
                        <STRUCTURED? .REST>
                        <NOT <EMPTY? .REST>>>
                   <SET COL* <WR <1 .REST> <OUT " " .COL*>>>
                   <SET REST <REST .REST>>
                   <SET NAMED? <- .NAMED? 1>>)
                  (ELSE <RETURN>)>>
        <TAIL1 .REST <+ .COL ,INDENT-GENERAL> .COL* <+ .COL* 1>>>>

<DEFINE PP-EXPR-LIST (L COL EXTRA)
    <PP-STRUC .L .COL .EXTRA ,PP-EXPR>>

<DEFINE PP-FUNCTION (EXPR COL EXTRA "AUX" NAMED?)
    <SET NAMED? <AND <NOT <LENGTH? .EXPR 1>>
                     <TYPE? <2 .EXPR> ATOM>>>
    <PP-GENERAL .EXPR .COL .EXTRA .NAMED? ,PP-EXPR-LIST <> ,PP-EXPR>>

<DEFINE PP-COND (EXPR COL EXTRA)
    <PP-CALL .EXPR .COL .EXTRA ,PP-EXPR-LIST>>

<DEFINE PP-AND (EXPR COL EXTRA)
    <PP-CALL .EXPR .COL .EXTRA ,PP-EXPR>>

<DEFINE PP-DEFINE (EXPR COL EXTRA "AUX" (NAMED? 1))
    <AND <NOT <LENGTH? .EXPR 2>>
         <TYPE? <3 .EXPR> ATOM>
         <SET NAMED? <+ .NAMED 1>>>
    <PP-GENERAL .EXPR .COL .EXTRA .NAMED? ,PP-EXPR-LIST <> ,PP-EXPR>>

<DEFINE PP-DO (EXPR COL EXTRA)
    <PP-GENERAL .EXPR .COL .EXTRA <> ,PP-EXPR-LIST <> ,PP-EXPR>>

<DEFINE PP-OBJECT (EXPR COL EXTRA)
    <PP-GENERAL .EXPR .COL .EXTRA 1 <> <> ,PP-AND>>

;"Define formatting style (change these to suit your style)"

<SETG INDENT-GENERAL 4>
<SETG MAX-CALL-HEAD-WIDTH 5>

<DEFINE STYLE (HEAD)
    <COND (<MEMQ .HEAD '(FUNCTION BIND PROG REPEAT)>		,PP-FUNCTION)
          (<MEMQ .HEAD '(COND VERSION? IFFLAG)>			,PP-COND)
          (<MEMQ .HEAD '(AND OR)>				,PP-AND)
          (<MEMQ .HEAD '(DEFINE DEFMAC ROUTINE DEFSTRUCT
                         PRONOUN)>				,PP-DEFINE)
          (<MEMQ .HEAD '(DO MAP-CONTENTS MAP-DIRECTIONS)>	,PP-DO)
          (<MEMQ .HEAD '(OBJECT ROOM)>                          ,PP-OBJECT)
          (ELSE <>)>>

;"TODO?
  * FORM style mirroring the style of the form being constructed:
      <FORM BIND '()
          <FORM ...>>
  * MAKE-* style:
      <MAKE-MYSTRUCT
          'FOO <BAR>
          'BAZ 123>
  * IF-* style:
      <IF-DEBUG
          <BLAH>
          <BLAH>>
  * TELL style fitting multiple exprs on one line and keeping
    atoms together with the next expr:
      <TELL \"long text long text long text\"
            N <LONG-EXPRESSION-LONG-EXPRESSION>
            CR CR
            D <LONG-EXPRESSION-LONG-EXPRESSION>
            CR>
  * DEFAULT-DEFINITION style:
      <DEFAULT-DEFINITION FOO
          <BLAH>
          <BLAH>>
  * SYNTAX style:
      <SYNTAX LOOK
              IN OBJECT (FIND CONTBIT) (ON-GROUND IN-ROOM)
              WITH OBJECT (FIND TOOLBIT) (TAKE HAVE HELD CARRIED)
              = V-LOOK-IN
                PRE-LOOK-IN
                LOOK-IN>
  * SCOPE-STAGE style:
      <SCOPE-STAGE FOO 1
          (<BLAH>)
          (<BLAH>)>
  * [ADD-]TELL-TOKENS style:
      <ADD-TELL-TOKENS
          FOO *           <BLAH>
          BAR * *         <BLAH>
          BLAH-BLAH-BLAH  <BLAH>>
  * styles for individual object properties (e.g. THINGS)
"       

<DEFINE PP (OBJ COL)
    <PR .OBJ .COL 0 ,PP-EXPR>>

<DEFINE GENERIC-WRITE (OBJ PRINC? WIDTH MAX-EXPR-WIDTH PER-LINE-PREFIX OUTPUT)
    <OUT .PER-LINE-PREFIX 0>
    ;"TODO: don't print CRLF when PPRINT is being used as PRINTTYPE inside a larger object"
    <COND (.WIDTH <OUT ,CRLF-STRING <PP .OBJ 0>>)
          (ELSE <WR .OBJ 0>)>
    T>

<DEFINE REVERSE-STRING-APPEND (L "AUX" (RESULT '()))
    <MAPF <>
          <FUNCTION (I) <SET RESULT <CONS .I .RESULT>>>
          .L>
    <STRING !.RESULT>>

;"Pretty-prints an object, with optional formatting controls."
<DEFINE PRETTY-PRINT (OBJ "OPT" (OUTCHAN .OUTCHAN) (WIDTH 79)
                                (MAX-EXPR-WIDTH 50) (PRINC? <>)
                                (PER-LINE-PREFIX ""))
    <GENERIC-WRITE .OBJ
                   .PRINC?
                   <- .WIDTH <LENGTH .PER-LINE-PREFIX>>
                   .MAX-EXPR-WIDTH
                   .PER-LINE-PREFIX
                   <FUNCTION (S) <PRINC .S .OUTCHAN> T>>>

<DEFINE WRAP-OBJ-FOR-PPRINT (OBJ ENV "AUX" F)
    <COND (<NOT <TYPE? .OBJ ATOM>> .OBJ)
          (<SET F <COND (<GASSIGNED? .OBJ> <GVAL .OBJ>)
                        (<ASSIGNED? .OBJ .ENV> <LVAL .OBJ .ENV>)
                        (ELSE <GETPROP .OBJ ZVAL>)>>
           <COND (<TYPE? .F FUNCTION>
                  <FORM DEFINE .OBJ !<CHTYPE .F LIST>>)
                 (<TYPE? .F MACRO>
                  <FORM DEFMAC .OBJ !<CHTYPE <1 <CHTYPE .F LIST>> LIST>>)
                 (<TYPE? .F ROUTINE>
                  <FORM ROUTINE .OBJ !<CHTYPE .F LIST>>)
                 (<TYPE? .F OBJECT>
                  <FORM !<CHTYPE .F LIST>>)
                 (<ASSIGNED? .OBJ .ENV>
                  <FORM SET .OBJ <LVAL .OBJ .ENV>>)
                 (ELSE
                  <FORM SETG .OBJ <GVAL .OBJ>>)>)
          (ELSE .OBJ)>>

;"Pretty-prints a definition or object."
<DEFINE PPRINT ("BIND" ENV OBJ)
    <PRETTY-PRINT <WRAP-OBJ-FOR-PPRINT .OBJ .ENV>>>

;"Gets or sets the list of types whose PRINTTYPEs are ,PPRINT.
  Get: <PPRINT-TYPES>
  Set: <PPRINT-TYPES A B C>  ;clears other types
  Clear: <PPRINT-TYPES <>>
  Will not alter the PRINTTYPE of a type whose PRINTTYPE is already
  set to something other than ,PPRINT."
<DEFINE PPRINT-TYPES ("TUPLE" TYPES "AUX" L)
    #DECL ((TYPES) <OR <LIST [REST ATOM]> !<LIST FALSE>>)
    <SET L <MAPF ,LIST
                 <FUNCTION (T)
                     <COND (<==? <PRINTTYPE .T> ,PPRINT> .T)
                           (ELSE <MAPRET>)>>
                 <ALLTYPES>>>
    <COND (<NOT <EMPTY? .TYPES>>
           <MAPF <>
                 <FUNCTION (T)
                     <PRINTTYPE .T ,PRINT>>
                 .L>
           <AND <1 .TYPES>
                <MAPF <>
                      <FUNCTION (T "AUX" (PT <PRINTTYPE .T>))
                          <OR .PT
                              <PRINTTYPE .T ,PPRINT>>>
                      .TYPES>>
           <SET L <PPRINT-TYPES>>)>
    .L>

<PPRINT-TYPES LIST VECTOR FUNCTION MACRO ROUTINE>

<ENDPACKAGE>
