"Testing framework"

<GLOBAL READBUF-TO-BE <>>
<GLOBAL LEXBUF-TO-BE <>>

<REPLACE-DEFINITION READLINE
    <ROUTINE READLINE ("OPT" PROMPT?)
        <SETG READBUF ,READBUF-TO-BE>
        <SETG LEXBUF ,LEXBUF-TO-BE>>>

<INSERT-FILE "parser">

<SETG TEST-SETUP-CODE '(() <RTRUE>)>
<SETG TEST-CASES ()>		;"reverse definition order"

<ADD-TELL-TOKENS
    BUF * *    <PRINT-BUF .X .Y>>

;"Causes some setup code to run before each test."
<DEFINE TEST-SETUP ("ARGS" BODY)
    <SETG TEST-SETUP-CODE .BODY>>

;"Defines a new test case."
<DEFINE TEST-CASE ("ARGS" BODY "AUX" NAME BINDINGS DESC STMTS RTN-NAME RTN-DEF)
    ;"Optional test case name before bindings"
    <COND (<TYPE? <1 .BODY> ATOM>
           <SET NAME <1 .BODY>>
           <SET BODY <REST .BODY>>)
          (ELSE
           <SET NAME <PARSE <STRING "UNNAMED-" <UNPARSE <LENGTH ,TEST-CASES>>>>>)>
    ;"Bindings"
    <SET BINDINGS <1 .BODY>>
    <SET BODY <REST .BODY>>
    ;"Optional description as first binding"
    <COND (<TYPE? <1 .BINDINGS> STRING>
           <SET DESC <1 .BINDINGS>>
           <SET BINDINGS <REST .BINDINGS>>)
          (ELSE
           <SET DESC <SPNAME .NAME>>)>
    ;"Rest of bindings are ignored for now!"
    ;"Translate rest of body to Z-code"
    <SET STMTS
        <MAPF ,LIST
              <FUNCTION (I "AUX" F)
                  <COND (<NOT <TYPE? .I FORM>>
                         <ERROR TEST-STEP-MUST-BE-FORM .I>)>
                  <SET F <1 .I>>
                  <COND (<==? .F COMMAND>
                         <MAPRET
                             <FORM TEST-LOAD-COMMAND-TABLES
                                   !<TEST-MAKE-COMMAND-TABLES <2 .I>>>
                             <FORM OR <FORM TEST-PERFORM-COMMAND>
                                      '<RFALSE>>>)
                        (<==? .F EXPECT>
                         <FORM OR <FORM TEST-CHECK-OUTPUT !<REST .I>>
                                  '<RFALSE>>)
                        (<==? .F CHECK>
                         <FORM OR <FORM TEST-CHECK-CONDITION <UNPARSE <2 .I>> !<REST .I>>
                                  '<RFALSE>>)
                        (<MEMQ .F '(MOVE REMOVE FSET FCLEAR SETG TELL)> .I)
                        (ELSE <ERROR ILLEGAL-TEST-STEP .F>)>>
              .BODY>>
    ;"Create routine"
    <SET RTN-NAME <PARSE <STRING <SPNAME .NAME> "-TEST-R">>>
    <SET RTN-DEF
        <FORM ROUTINE .RTN-NAME '()
              !.STMTS
              '<RTRUE>>>
    <EVAL .RTN-DEF>
    ;"Add to list"
    <SETG TEST-CASES <CONS (.RTN-NAME .DESC) ,TEST-CASES>>>

;"Defines READBUF and LEXBUF for a given game command.

We have to do this at compile time, because V3 has no LEX opcode, so it can't
lex a command string at runtime.

Args:
  VEC: The game command, given as a vector of atoms.

Returns:
  A two-element list, containing the READBUF table followed by the LEXBUF."
<DEFINE TEST-MAKE-COMMAND-TABLES (VEC "AUX" WORDS LEX-TABLE CMD-STR READ-TABLE)
    ;"Build lex table"
    <SET WORDS
        <BIND ((L <VERSION? (ZIP 1) (EZIP 1) (ELSE 2)>))
            <MAPF ,LIST
                  <FUNCTION (WD "AUX" ITEMS WLEN WADDR)
                      <COND (<TYPE? .WD ATOM>
                             <SET WLEN <LENGTH <SPNAME .WD>>>
                             <SET WADDR <VOC <SPNAME .WD>>>)
                            (<TYPE? .WD STRING>
                             <SET WLEN <LENGTH .WD>>
                             <SET WADDR <>>)
                            (ELSE
                             <ERROR ILLEGAL-WORD-TYPE .WD>)>
                      <SET ITEMS (;"2 bytes: word address"
                                  .WADDR
                                  ;"1 byte: word length"
                                  <CHTYPE .WLEN BYTE>
                                  ;"Offset into READBUF"
                                  <CHTYPE .L BYTE>)>
                      <SET L <+ .L .WLEN 1>>
                      <MAPRET !.ITEMS>>
                  .VEC>>>
    <SET LEX-TABLE
        <PTABLE <CHTYPE <LENGTH .VEC> BYTE>    ;"Max word count"
                <CHTYPE <LENGTH .VEC> BYTE>    ;"Actual word count"
                !.WORDS>>
    ;"Build read table"
    <SET CMD-STR
        <LOWERCASE
            <MAPR ,STRING
                  <FUNCTION (R "AUX" (W <1 .R>))
                      <COND (<TYPE? .W ATOM> <SET W <SPNAME .W>>)>
                      <COND (<LENGTH? .R 1> .W)
                            (ELSE <MAPRET .W " ">)>>
                  .VEC>>>
    <SET READ-TABLE
        <BIND ((L <LENGTH .CMD-STR>))
            <VERSION?
                ;"ZIP/EZIP: 1 byte length, string, 1 byte null terminator"
                (ZIP
                 <PTABLE (STRING) .L .CMD-STR 0>)
                (EZIP
                 <PTABLE (STRING) .L .CMD-STR 0>)
                ;"XZIP+: 1 byte max, 1 byte length, string"
                (ELSE
                 <PTABLE (STRING) .L .L .CMD-STR>)>>>
    ;"Return both tables"
    (.READ-TABLE .LEX-TABLE)>

;"Converts a string to lowercase."
<DEFINE LOWERCASE (S "AUX" (A <ASCII !\A>) (Z <ASCII !\Z>))
    <MAPF ,STRING
          <FUNCTION (C "AUX" (AC <ASCII .C>))
              <COND (<AND <G=? .AC .A> <L=? .AC .Z>>
                     <ASCII <+ .AC 32>>)
                    (ELSE .C)>>
          .S>>

;"Finalizes test definitions and emits the GO routine.

Args:
  ROOM: The room where the player should start."
<DEFINE TEST-GO ('ROOM "AUX" RTNS DESCS)
    ;"Validate"
    <COND (<LENGTH? ,TEST-CASES 0> <ERROR NO-TEST-CASES>)>
    ;"Define necessary constants"
    <CONSTANT GAME-BANNER "Automated Tests">
    <CONSTANT RELEASEID 0>
    ;"Define tables of test case routines and names"
    <SET RTNS <MAPF ,LIST 1 ,TEST-CASES>>
    <SET DESCS <MAPF ,LIST 2 ,TEST-CASES>>
    <CONSTANT TEST-CASE-ROUTINES <LTABLE !.RTNS>>
    <CONSTANT TEST-CASE-DESCS <LTABLE !.DESCS>>
    ;"Define SETUP-BEFORE-TEST-CASE routine"
    <EVAL <FORM ROUTINE SETUP-BEFORE-TEST-CASE !,TEST-SETUP-CODE>>
    ;"Define GO routine"
    <CONSTANT TEST-START-ROOM .ROOM>
    <ROUTINE GO ()
        <SETG HERE ,TEST-START-ROOM>
        <MOVE ,PLAYER ,HERE>
        <RUN-TEST-CASES>>>

;"Runs the test cases.

Returns:
  True if all cases passed. False if any case failed."
<ROUTINE RUN-TEST-CASES ("AUX" (MAX <GET ,TEST-CASE-ROUTINES 0>) RTN (OK T) NTEST NPASS)
    <DO (I 1 .MAX)
        <SETUP-BEFORE-TEST-CASE>
        <SET RTN <GET ,TEST-CASE-ROUTINES .I>>
        <TELL "=== Testing: " <GET ,TEST-CASE-DESCS .I> " ===" CR>
        <SET NTEST <+ .NTEST 1>>
        <COND (<APPLY .RTN> <SET NPASS <+ .NPASS 1>>)
              (ELSE <SET OK <>>)>>
    <TELL "=== Summary ===" CR>
    <TELL N .NTEST " tested, " N .NPASS " passed" CR>
    <COND (.OK <TELL "PASS" CR> <RTRUE>)
          (ELSE <TELL "FAIL" CR> <RFALSE>)>>

;"Implementation of the <COMMAND ...> test step."
<ROUTINE TEST-LOAD-COMMAND-TABLES (RDTBL LXTBL)
    ;"Print command"
    ;"Note that TEST-MAKE-COMMAND-TABLES sets RDTBL's length prefix to the
      actual command length."
    <BIND (START)
        <VERSION? (ZIP <SET START 1>)
                  (EZIP <SET START 1>)
                  (ELSE <SET START 2>)>
        <TELL "> " BUF <REST .RDTBL .START> <GETB .RDTBL 0> CR>>
    ;"Store buffer pointers to be set by READLINE"
    <SETG READBUF-TO-BE .RDTBL>
    <SETG LEXBUF-TO-BE .LXTBL>>

<ROUTINE PRINT-BUF (BUF LEN "AUX" (MAX <- .LEN 1>))
    <DO (I 0 .MAX)
        <PRINTC <GETB .BUF .I>>>>

<ROUTINE BUFS-DIFFER? (BUF1 BUF2 LEN "AUX" MAX)
    <SET MAX <- .LEN 1>>
    <DO (I 0 .MAX)
        <COND (<N=? <GETB .BUF1 .I> <GETB .BUF2 .I>>
               <RTRUE>)>>
    <RFALSE>>

<CONSTANT OUTBUF-SIZE 2048>
<CONSTANT OUTBUF <ITABLE NONE ,OUTBUF-SIZE>>

<ROUTINE TEST-PERFORM-COMMAND ()
    ;"Parse and perform command"
    <DIROUT 3 ,OUTBUF>
    <REPEAT ()
        <MAIN-LOOP-ITERATION>
        <COND (<L=? ,P-CONT 0> <RETURN>)>>
    <DIROUT -3>>

<CONSTANT EXPECTBUF <ITABLE NONE ,OUTBUF-SIZE>>

;"Implementation of the <EXPECT ...> test step."
<ROUTINE TEST-CHECK-OUTPUT (EXPECTED "AUX" ELEN ACTUAL ALEN)
    ;"Copy expected output to buffer"
    <DIROUT 3 ,EXPECTBUF>
    <PRINT .EXPECTED>
    <DIROUT -3>
    <SET ELEN <GET ,EXPECTBUF 0>>
    <SET EXPECTED <REST ,EXPECTBUF 2>>
    <SET ALEN <GET ,OUTBUF 0>>
    <SET ACTUAL <REST ,OUTBUF 2>>
    ;"Compare"
    <COND (<OR <N=? .ALEN .ELEN> <BUFS-DIFFER? .ACTUAL .EXPECTED .ALEN>>
           <TELL "Output differs." CR CR
                 "EXPECTED:" CR
                 BUF .EXPECTED .ELEN CR
                 "ACTUAL:" CR
                 BUF .ACTUAL .ALEN CR>
           <RFALSE>)>
    <RTRUE>>

;"Implementation of the <CHECK ...> test step."
<ROUTINE TEST-CHECK-CONDITION (TEXT VALUE)
    <COND (<NOT .VALUE>
           <TELL "Condition {" .TEXT "} was false. Expected true." CR>
           <RFALSE>)
          (ELSE
           <RTRUE>)>>
