"Library header"

<SETG ZILLIB-VERSION "J1">

"Debugging"

<COMPILATION-FLAG-DEFAULT DEBUG <>>

"In V3, we need these globals for the status line. In V5, we could
call them something else, but we'll continue the tradition anyway."

<GLOBAL HERE <>>
<GLOBAL SCORE 0>
<GLOBAL TURNS 0>
"next two for the Interrupt Queue"
<CONSTANT IQUEUE <ITABLE 20>>
<GLOBAL TEMPTABLE <ITABLE 50 <> >>
<GLOBAL IQ-LENGTH 0>
<GLOBAL STANDARD-WAIT 4>
<GLOBAL AGAINCALL 0>
<GLOBAL USAVE 0>
<GLOBAL ONCAGAIN 0>
<GLOBAL NLITSET <>>
<GLOBAL DBCONT <>>
<GLOBAL DTURNS <>>
<GLOBAL MODE 1>
<CONSTANT SUPERBRIEF 0>
<CONSTANT BRIEF 1>
<CONSTANT VERBOSE 2>

<ADD-TELL-TOKENS
    T *     <PRINT-DEF .X>
    A *     <PRINT-INDEF .X>
    CT *    <PRINT-CDEF .X>
    CA *    <PRINT-CINDEF .X>>

"Version considerations: certain values are bytes on V3 but words on all
other versions. These macros let us write the same code for all versions."

<VERSION?
    (ZIP
        <DEFMAC GET/B ('T 'O) <FORM GETB .T .O>>
        <DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PBTBL? .O .P .V>>)
    (ELSE
        <DEFMAC GET/B ('T 'O) <FORM GET .T .O>>
        <DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PWTBL? .O .P .V>>)>

"Parser"

<CONSTANT READBUF-SIZE 100>
<CONSTANT READBUF <ITABLE NONE 100>>
<CONSTANT BACKREADBUF <ITABLE NONE 100>>
<CONSTANT LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>
<CONSTANT BACKLEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>

<CONSTANT P1MASK 3>

<GLOBAL WINNER PLAYER>

<GLOBAL PRSA <>>
<GLOBAL PRSO <>>
<GLOBAL PRSI <>>
<GLOBAL PRSO-DIR <>>

<DEFMAC VERB? ("ARGS" A)
    <SET O <MAPF ,LIST
        <FUNCTION (I)
            <FORM GVAL <PARSE <STRING "V?" <SPNAME .I>>>>>
        .A>>
    <FORM EQUAL? ',PRSA !.O>>

<DEFMAC WORD? ('W 'T)
    <FORM CHKWORD? .W
        <FORM GVAL <PARSE <STRING "PS?" <SPNAME .T>>>>
        <FORM GVAL <PARSE <STRING "P1?" <SPNAME .T>>>>>>

<VERSION?
    (ZIP
        <CONSTANT VOCAB-FL 4>   ;"part of speech flags"
        <CONSTANT VOCAB-V1 5>   ;"value for 1st part of speech"
        <CONSTANT VOCAB-V2 6>   ;"value for 2nd part of speech")
    (T
        <CONSTANT VOCAB-FL 6>
        <CONSTANT VOCAB-V1 7>
        <CONSTANT VOCAB-V2 8>)>

<ROUTINE CHKWORD? (W PS "OPT" (P1 -1) "AUX" F)
    <COND (<0? .W> <RFALSE>)>
    <IF-DEBUG <TELL "[CHKWORD: " B .W " PS=" N .PS " P1=" N .P1>>
    <SET F <GETB .W ,VOCAB-FL>>
    <IF-DEBUG <TELL " F=" N .F>>
    <SET F <COND (<BTST .F .PS>
                    <COND (<L? .P1 0>
                            <RTRUE>)
                        (<==? <BAND .F ,P1MASK> .P1>
                            <GETB .W ,VOCAB-V1>)
                        (ELSE <GETB .W ,VOCAB-V2>)>)>>
    <IF-DEBUG <TELL " = " N .F "]" CR>>
    .F>

<ROUTINE GETWORD? (N "AUX" R)
    <SET R <GET ,LEXBUF <- <* .N 2> 1>>>
    <IF-DEBUG
        <TELL "[GETWORD " N .N " = ">
        <COND (.R <TELL B .R>) (ELSE <TELL "?">)>
        <TELL "]" CR>>
    .R>

<ROUTINE PRINT-WORD (N "AUX" I MAX)
    <SET I <GETB ,LEXBUF <+ <* .N 4> 1>>>
    <SET MAX <- <+ .I <GETB ,LEXBUF <* .N 4>>> 1>>
    <REPEAT ()
        <PRINTC <GETB ,READBUF .I>>
        <AND <IGRTR? I .MAX> <RETURN>>>>

<GLOBAL P-LEN 0>
<GLOBAL P-V <>>
<GLOBAL P-NOBJ 0>
<GLOBAL P-P1 <>>
<GLOBAL P-P2 <>>


<CONSTANT P-MAXOBJS 10>
"These each have one length byte, one mode byte, then P-MAXOBJS pairs of words."
<CONSTANT P-DOBJS <ITABLE 21 <>>>
<CONSTANT P-DOBJEX <ITABLE 21 <>>>
<CONSTANT P-IOBJS <ITABLE 21 <>>>
<CONSTANT P-IOBJEX <ITABLE 21 <>>>
"for recalling last PRSO with IT"
<CONSTANT P-DOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-DOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL IT-USE 0>
<GLOBAL IT-ONCE 0>
"for recalling last PRSO with THEM"
<CONSTANT P-TOBJS-BACK <ITABLE 21<>>>
<CONSTANT P-TOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL THEM-USE 0>
<GLOBAL THEM-ONCE 0>
"for recalling last male PRSO with HIM"
<CONSTANT P-MOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-MOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL HIM-USE 0>
<GLOBAL HIM-ONCE 0>
"for recalling last PRSO with HER"
<CONSTANT P-FOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-FOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL HER-USE 0>
<GLOBAL HER-ONCE 0>

<BUZZ A AN AND ANY ALL BUT EXCEPT OF ONE THE \. \, \">

<ROUTINE PARSER ("AUX" NOBJ VAL DIR)
    <SET NOBJ <>>
    <SET VAL <>>
    <SET DIR <>>
    <SETG HERE <LOC ,PLAYER>>
    <READLINE>
    <IF-DEBUG <DUMPLINE>>
    <SETG P-LEN <GETB ,LEXBUF 1>>
    <SETG P-V <>>
    <SETG P-NOBJ 0>
    <PUT ,P-DOBJS 0 0>
    <PUT ,P-IOBJS 0 0>
    <SETG P-P1 <>>
    <SETG P-P2 <>>
    <SETG HERE <LOC ,WINNER>>
    <REPEAT ((I 1) W)
        ;<TELL "W is currently " N .W CR>
         ;"setting of W separated out so verb recogniton could also be separated"
         <COND
        (<G? .I ,P-LEN>
                <RETURN>)
        (<NOT <SET W <GETWORD? .I>>>
            <TELL "I don't know the word \"">
            <PRINT-WORD .I>
            <TELL "\"." CR>
            <RFALSE>)>
    ;"verb recognition separated from rest so a verb and object can have the same name"
         <COND
        (<AND <CHKWORD? .W ,PS?VERB>
                <NOT ,P-V>>
                      <SETG P-V <WORD? .W VERB>>)
             (ELSE
            <COND
                (<AND <EQUAL? ,P-V <> ,ACT?WALK>
                        <SET VAL <WORD? .W DIRECTION>>>
                    <SET DIR .VAL>)
                (<SET VAL <CHKWORD? .W ,PS?PREPOSITION 0>>
                    <COND (<AND <==? .NOBJ 0> <NOT ,P-P1>>
                            <SETG P-P1 .VAL>)
                        (<AND <==? .NOBJ 1> <NOT ,P-P2>>
                            <SETG P-P2 .VAL>)>)
                (<STARTS-CLAUSE? .W>
                    <SET NOBJ <+ .NOBJ 1>>
                    <COND (<==? .NOBJ 1>
                            <SET VAL <MATCH-CLAUSE .I ,P-DOBJS ,P-DOBJEX>>)
                        (<==? .NOBJ 2>
                            <SET VAL <MATCH-CLAUSE .I ,P-IOBJS ,P-IOBJEX>>)
                        (ELSE
                            <TELL "That sentence has too many objects." CR>
                            <RFALSE>)>
                    <COND (.VAL
                            <SET I .VAL>
                            <AGAIN>)
                        (ELSE
                            <TELL "That noun clause didn't make sense." CR>
                            <RFALSE>)>)
                (ELSE
                    <TELL "I didn't expect the word \"">
                    <PRINT-WORD .I>
                    <TELL "\" there." CR>
                    <RFALSE>)>
        )>
        <SET I <+ .I 1>>>
    <SETG P-NOBJ .NOBJ>
    <IF-DEBUG <TELL "[PARSER: V=" N ,P-V " NOBJ=" N ,P-NOBJ
        " P1=" N ,P-P1 " DOBJS=" N <GETB ,P-DOBJS 0>
        " P2=" N ,P-P2 " IOBJS=" N <GETB ,P-IOBJS 0> "]" CR>>
    <COND
        (.DIR
            <SETG PRSO-DIR T>
            <SETG PRSA ,V?WALK>
            <SETG PRSO .DIR>
            <SETG PRSI <>>
        <VERSION?
        (ZIP <>)
        (EZIP <>)
        (ELSE
         ;"save state for undo after moving from room to room"
          <COND (<AND <NOT <VERB? UNDO >>
                       <NOT <EQUAL? ,AGAINCALL 1>>>
                        <SETG USAVE <ISAVE>>
                        ;<TELL "ISAVE returned " N ,USAVE CR>
                        <COND (<EQUAL? ,USAVE 2>
                            <TELL "Previous turn undone." CR>
                            ;<SETG USAVE 0>  ;"prevent undoing twice in a row"
                        <AGAIN>)>)>
        )>
            <RTRUE>)
        (<NOT ,P-V>
            <TELL "That sentence has no verb." CR>
            <RFALSE>)
        (ELSE
            <SETG PRSO-DIR <>>
            <COND 
                (<AND
                    <MATCH-SYNTAX>
                    <FIND-OBJECTS>>
            <VERSION?
                (ZIP <>)
                (EZIP <>)
                (ELSE
                ;"save state for UNDO, unless we're about to UNDO the previous command"
                        <COND (<AND <NOT <VERB? UNDO >>
                                    <NOT <EQUAL? ,AGAINCALL 1>>>
                    <SETG USAVE <ISAVE>>
                       ; <TELL "ISAVE returned " N ,USAVE CR>
                        <COND (<EQUAL? ,USAVE 2>
                            <TELL "Previous turn undone." CR>
                            ;<SETG USAVE 0>  ;"prevent undoing twice in a row"
                        <AGAIN>
                        ;<SETG NOUAGAIN 0>
                        )>)>
                )>
                        ;"if successful PRSO and not after an IT use, back up PRSO for IT"
                        <COND (<AND <EQUAL? ,IT-USE 0>
                                    <AND ,PRSO>
                                    <NOT <FSET? ,PRSO ,PERSONBIT>>
                                    <NOT <FSET? ,PRSO ,PLURALBIT>>>
                                        ;<TELL "Copying P-DOBJS into P-DOBJS-BACK" CR>
                                        <COPY-TABLE ,P-DOBJS ,P-DOBJS-BACK 21>
                                        <COPY-TABLE ,P-DOBJEX ,P-DOBJEX-BACK 21>
                                        <COND (<EQUAL? ,IT-ONCE 0> <SET IT-ONCE 1>)>)
                        ;"if PRSO has PLURALBIT, back up to THEM instead"
                             (<AND <EQUAL? ,THEM-USE 0>
                                    <AND ,PRSO>
                                    <NOT <FSET? ,PRSO ,PERSONBIT>>
                                    <FSET? ,PRSO ,PLURALBIT>>
                                        ;<TELL "Copying P-DOBJS into P-TOBJS-BACK" CR>
                                        <COPY-TABLE ,P-DOBJS ,P-TOBJS-BACK 21>
                                        <COPY-TABLE ,P-DOBJEX ,P-TOBJEX-BACK 21>
                                        <COND (<EQUAL? ,THEM-ONCE 0> <SET THEM-ONCE 1>)>)
                        ;"if successful PRSO who is male, back up PRSO for HIM"
                               (<AND <EQUAL? ,HIM-USE 0>
                                        <AND ,PRSO>
                                        <FSET? ,PRSO ,PERSONBIT>
                                        <NOT <FSET? ,PRSO ,FEMALEBIT>>>
                                            ;<TELL "Copying P-DOBJS into P-MOBJS-BACK" CR>
                                            <COPY-TABLE ,P-DOBJS ,P-MOBJS-BACK 21>
                                            <COPY-TABLE ,P-DOBJEX ,P-MOBJEX-BACK 21>
                                            <COND (<0? ,HIM-ONCE> <SET HIM-ONCE 1>)>)
                       ;"if successful PRSO who is female, back up PRSO for HER"
                               (<AND <EQUAL? ,HER-USE 0>
                                        <AND ,PRSO>
                                        <FSET? ,PRSO ,PERSONBIT>
                                        <FSET? ,PRSO ,FEMALEBIT>>
                                            ;<TELL "Copying P-DOBJS into P-FOBJS-BACK" CR>
                                            <COPY-TABLE ,P-DOBJS ,P-FOBJS-BACK 21>
                                            <COPY-TABLE ,P-DOBJEX ,P-FOBJEX-BACK 21>
                                            <COND (<0? ,HER-ONCE> <SET HER-ONCE 1>)>
                                            )>
                        <RTRUE>
                )>
         )>
>


<VERSION?
    (ZIP
        <ROUTINE COPY-TABLE (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUT .DEST .I <GET .SRC .I>>>>)
    (EZIP
        <ROUTINE COPY-TABLE (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUT .DEST .I <GET .SRC .I>>>>)
    (ELSE
        <DEFMAC COPY-TABLE ('SRC 'DEST 'LEN "AUX" BYTES)
            ;"someday the compiler should do this optimization on its own..."
            <COND (<TYPE? .LEN FIX> <SET BYTES <* .LEN 2>>)
                  (ELSE <SET BYTES <FORM * .LEN 2>>)>
            <FORM COPYT .SRC .DEST .BYTES>>)>


<ROUTINE STARTS-CLAUSE? (W)
    <OR <EQUAL? .W ,W?A ,W?AN ,W?THE>
        <CHKWORD? .W ,PS?ADJECTIVE>
        <WORD? .W OBJECT>>>

<CONSTANT MCM-ALL 1>
<CONSTANT MCM-ANY 2>

<ROUTINE MATCH-CLAUSE (WN YTBL NTBL "AUX" (TI 1) W VAL (MODE 0) (ADJ <>) (NOUN <>) (BUT <>))
    <REPEAT ()
        <COND
            ;"exit loop if we reached the end of the command"
            (<G? .WN ,P-LEN> <RETURN>)
            ;"fail if we found an unrecognized word"
            (<0? <SET W <GETWORD? .WN>>> <RFALSE>)
            ;"recognize ALL/ANY/ONE"
            (<EQUAL? .W ,W?ALL ,W?ANY ,W?ONE>
                <COND (<OR .MODE .ADJ .NOUN> <RFALSE>)>
                <SET MODE
                    <COND (<==? .W ,W?ALL> ,MCM-ALL)
                        (ELSE ,MCM-ANY)>>)
            ;"match adjectives, keeping only the first"
            (<VERSION?
                (ZIP <SET VAL <WORD? .W ADJECTIVE>>)
                (ELSE <AND <CHKWORD? .W ,PS?ADJECTIVE> <SET VAL .W>>)>
                    <COND
                        ;"if W can also be a noun, treat it as such if we don't
                          already have a noun, and it isn't followed by an adj or noun"
                        (<AND
                            <0? .NOUN>				;"no noun"
                            <WORD? .W OBJECT>		;"word can be a noun"
                            <OR
                                <==? .WN ,P-LEN>	;"word is at end of line"
                                <PROG ((NW <GETWORD? <+ .WN 1>>))
                                    <NOT <OR		;"next word is not adj/noun"
                                        <CHKWORD? .W ,PS?ADJECTIVE>
                                        <CHKWORD? .W ,PS?OBJECT>>>>>>
                            <SET NOUN .W>)
                        (<==? .TI ,P-MAXOBJS>
                            <TELL "That clause mentions too many objects." CR>
                            <RFALSE>)
                        (<NOT .ADJ> <SET ADJ .VAL>)>)
            ;"match nouns, exiting the loop if we already found one"
            (<WORD? .W OBJECT>
                <COND
                    (.NOUN <RETURN>)
                    (<==? .TI ,P-MAXOBJS>
                        <TELL "That clause mentions too many objects." CR>
                        <RFALSE>)
                    (ELSE <SET NOUN .W>)>)
            ;"recognize AND/comma"
            (<EQUAL? .W ,W?AND ,W?COMMA>
                <COND (<OR .ADJ .NOUN>
                        <PUT .YTBL .TI .ADJ>
                        <PUT .YTBL <+ .TI 1> .NOUN>
                        <SET ADJ <SET NOUN <>>>
                        <SET TI <+ .TI 2>>)>)
            ;"skip buzzwords"
            (<CHKWORD? .W ,PS?BUZZ-WORD>)
            ;"exit loop if we found any other word type"
            (ELSE <RETURN>)>
        <SET WN <+ .WN 1>>>
    ;"store final adj/noun pair"
    <COND (<OR .ADJ .NOUN>
            <PUT .YTBL .TI .ADJ>
            <PUT .YTBL <+ .TI 1> .NOUN>
            <SET TI <+ .TI 2>>)>
    ;"store phrase count and mode"
    <PUTB .YTBL 0 </ <- .TI 1> 2>>
    <PUTB .YTBL 1 .MODE>
    .WN>

<CONSTANT SYN-REC-SIZE 8>
<CONSTANT SYN-NOBJ 0>
<CONSTANT SYN-PREP1 1>
<CONSTANT SYN-PREP2 2>
<CONSTANT SYN-FIND1 3>
<CONSTANT SYN-FIND2 4>
<CONSTANT SYN-OPTS1 5>
<CONSTANT SYN-OPTS2 6>
<CONSTANT SYN-ACTION 7>

<ROUTINE MATCH-SYNTAX ("AUX" PTR CNT TEST-PTR TEST-P-NOBJ)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <IF-DEBUG <TELL "[MATCH-SYNTAX: " N .CNT " syntaxes at " N .PTR "]" CR>>
    <REPEAT ()
        ;<TELL "CNT is currently " N .CNT CR>
        <PROG () <SET TEST-PTR <GET .PTR ,SYN-NOBJ>>
                 <SET TEST-P-NOBJ ,P-NOBJ>
                 ;<TELL "<GETB .PTR ,SYN-NOBJ> is " N .TEST-PTR "and P-NOBJ is " N .TEST-P-NOBJ CR>
                 >
        <COND (<DLESS? CNT 0>
                    <TELL "I don't understand that sentence." CR>
                                <RFALSE>)
            (<AND <==? <GETB .PTR ,SYN-NOBJ> ,P-NOBJ>
                  <OR <L? ,P-NOBJ 1> <==? <GETB .PTR ,SYN-PREP1> ,P-P1>>
                  <OR <L? ,P-NOBJ 2> <==? <GETB .PTR ,SYN-PREP2> ,P-P2>>>
                <SETG PRSA <GETB .PTR ,SYN-ACTION>>
                <RTRUE>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>>


<ROUTINE FIND-OBJECTS ("AUX" X)
    <COND (<G=? ,P-NOBJ 1>
            <SET X <GET ,P-DOBJS 2>>
            ;<TELL "Find objects PRSO test - X is " N .X CR>
            <COND (<EQUAL? .X W?IT>
                        <COND (<L? ,IT-ONCE 1> <TELL "I'm unsure what you're referring to." CR> <RFALSE>)>
                        ;<TELL "IT found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
                            <COPY-TABLE ,P-DOBJS-BACK ,P-DOBJS  21>
                            <COPY-TABLE ,P-DOBJEX-BACK ,P-DOBJEX  21>
                        <SETG IT-USE 1>
                        )
                  (ELSE <SETG IT-USE 0>)>
            <COND (<EQUAL? .X W?THEM>
                        <COND (<L? ,THEM-ONCE 1> <TELL "I'm unsure what you're referring to." CR> <RFALSE>)>
                        ;<TELL "IT found (in D.O.).  Replacing with backed-up P-TOBJS" CR>
                            <COPY-TABLE ,P-TOBJS-BACK ,P-DOBJS  21>
                            <COPY-TABLE ,P-TOBJEX-BACK ,P-DOBJEX  21>
                        <SETG THEM-USE 1>
                        )
                  (ELSE <SETG THEM-USE 0>)>
            <COND (<EQUAL? .X W?HIM>
                        <COND (<0? ,HIM-ONCE> <TELL "I'm unsure to whom you are referring." CR> <RFALSE>)>
                        ;<TELL "HIM found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
                            <COPY-TABLE ,P-MOBJS-BACK ,P-DOBJS  21>
                            <COPY-TABLE ,P-MOBJEX-BACK ,P-DOBJEX  21>
                        <SETG HIM-USE 1>
                        )
                  (ELSE <SETG HIM-USE 0>)>
            <COND (<EQUAL? .X W?HER>
                        <COND (<0? ,HER-ONCE> <TELL "I'm unsure to whom you are referring." CR> <RFALSE>)>
                        ;<TELL "HER found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
                            <COPY-TABLE ,P-FOBJS-BACK ,P-DOBJS  21>
                            <COPY-TABLE ,P-FOBJEX-BACK ,P-DOBJEX  21>
                        <SETG HER-USE 1>
                        )
                  (ELSE <SETG HER-USE 0>)>
            <COND (<NOT <SETG PRSO <FIND-ONE-OBJ ,P-DOBJS ,P-DOBJEX>>>
                        <RFALSE>)
            >)
         (ELSE <SETG PRSO <>>)>
    <COND (<G=? ,P-NOBJ 2>
            <SET X <GET ,P-IOBJS 2>>
            ;<TELL "Find objects PRSI test - X is " N .X CR>
            <COND (<EQUAL? .X W?IT>
                        ;<TELL "IT found (in .I.O). Replacing with backed-up P-DOBJS" CR>
                        <COPY-TABLE ,P-DOBJS-BACK ,P-IOBJS 21>
                        <COPY-TABLE ,P-DOBJEX-BACK ,P-IOBJEX 21>
                        <SETG IT-USE 1>
                        )
                  (ELSE <SETG IT-USE 0>)>
            <COND (<EQUAL? .X W?THEM>
                        ;<TELL "THEM found (in .I.O). Replacing with backed-up P-DOBJS" CR>
                        <COPY-TABLE ,P-TOBJS-BACK ,P-IOBJS 21>
                        <COPY-TABLE ,P-TOBJEX-BACK ,P-IOBJEX 21>
                        <SETG THEM-USE 1>
                        )
                  (ELSE <SETG THEM-USE 0>)>
            <COND (<EQUAL? .X W?HIM>
                        ;<TELL "HIM found (in .I.O). Replacing with backed-up P-DOBJS" CR>
                        <COPY-TABLE ,P-MOBJS-BACK ,P-IOBJS 21>
                        <COPY-TABLE ,P-MOBJEX-BACK ,P-IOBJEX 21>
                        <SETG HIM-USE 1>
                        )
                  (ELSE <SETG HIM-USE 0>)>
            <COND (<EQUAL? .X W?HER>
                        ;<TELL "HER found (in .I.O). Replacing with backed-up P-DOBJS" CR>
                            <COPY-TABLE ,P-FOBJS-BACK ,P-IOBJS 21>
                            <COPY-TABLE ,P-FOBJEX-BACK ,P-IOBJEX 21>
                        <SETG HER-USE 1>
                        )
                  (ELSE <SETG HER-USE 0>)>
            <COND (<NOT <SETG PRSI <FIND-ONE-OBJ ,P-IOBJS ,P-IOBJEX>>>
                        <RFALSE>)
            >)
          (ELSE <SETG PRSI <>>)>
    <RTRUE>>

;"This seems really inefficient - mostly repeating FIND-ONE-OBJ & CONTAINER-SEARCH - can't think of better right now"
<ROUTINE SEARCH-FOR-LIGHT ("AUX" F G H GMAX X)
       ;<TELL "search inventory for light source">
        <MAP-CONTENTS (I ,WINNER)
        ;<TELL "I is currently " D . I  CR>
        <COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                <RTRUE>)
              ;"if any items are surfaces or open containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
                          <AND  <FSET? .I ,OPENABLEBIT>
                                <FSET? .I ,OPENBIT>
                          >
                          ;"the always-open case"
                          <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLEBIT>>
                          >
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                      >
                                ;<TELL D .I " is a container." CR>
                                <SET F <CONTAINER-LIGHT-SEARCH .I>>
                                ;<TELL "Back in main SEARCH-FOR-LIGHT loop and F is " D .F CR>
                                <AND .F <RETURN>>
                                ;<RETURN>
                      )>
               )       
        >>
        <AND .F <RTRUE>>
        ;"check location"
        <MAP-CONTENTS (I ,HERE)
        ;<TELL "Room Loop object is currently " D .I CR>
            <COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                ;<TELL "F is now set to " D .F CR>
                <RETURN>)
              ;"if any items are surfaces or open containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
                          <FSET? .I ,OPENBIT>
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLEBIT>>
                          >
                      >
                                ;<TELL D .I " is a container." CR>
                                <SET F <CONTAINER-LIGHT-SEARCH .I>>
                                ;<TELL "Back in main SEARCH-FOR-LIGHT loop and F is " D .F CR>
                                <AND .F <RETURN>>
                                ;<RETURN>
                      )>
               )
            >
        >
        <AND .F <RTRUE>>
        ;"check global objects"
        <MAP-CONTENTS (I ,GLOBAL-OBJECTS)
            <COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                <RETURN>)>>
        <AND .F <RETURN .F>>
        ;"check local-globals"
        <MAP-CONTENTS (I ,LOCAL-GLOBALS)
            <COND (<AND <FSET? .I ,LIGHTBIT> <GLOBAL-IN? .I ,HERE>>
                ;"room has an object that matches this global with LIGHTBIT"
                <SET F .I>
                <RETURN>)>>
        <AND .F <RTRUE>> 
        ;<TELL "no light source found" CR>
        <RFALSE>>      

<ROUTINE SEE-INSIDE? (OBJ)
    <COND
        (<OR <FSET? .OBJ ,SURFACEBIT>
        <AND <FSET? .OBJ ,CONTBIT>
        <OR <FSET? .OBJ ,OPENBIT> <FSET? .OBJ ,TRANSBIT>>>
         >
         <RTRUE>)
    >
         <RFALSE>	 
>

<ROUTINE CONTAINER-LIGHT-SEARCH (I "AUX" J F)
            <MAP-CONTENTS (J .I)
                    ;<TELL "Searching contents of " D .I " for light source" CR>
                    ;<TELL "Current object is " D .J CR>
                    <COND (<FSET? .J ,LIGHTBIT>
                            <SET F .J>
                            ;<TELL "Light source match found in container search, F is now " D .F CR>
                            <RETURN>)
                          (<OR 	<FSET? .J ,SURFACEBIT>
                                <AND  <FSET? .J ,OPENABLEBIT>
                                      <FSET? .J ,OPENBIT>
                                >
                                ;"the always-open case"
                                <AND  <FSET? .J ,CONTBIT> 
                                      <NOT <FSET? .J ,OPENABLEBIT>>
                                >
                           >                      				
                      ;<TELL "Found another container - about to search through " D .J CR>
                                    <SET F <CONTAINER-LIGHT-SEARCH .J>>
                                    <AND .F <RETURN>>)    	
                     >
             >
            <AND .F <RETURN .F>>
 >





<ROUTINE FIND-ONE-OBJ (YTBL NTBL "AUX" A N F G H GMAX X P)
    <SET A <GET .YTBL 1>>
    <SET N <GET .YTBL 2>>
    <IF-DEBUG <TELL "[FIND-ONE-OBJ: adj=" N .A " noun=" N .N "]" CR>>
    ;"check abstract/generic objects"
    <MAP-CONTENTS (I ,GENERIC-OBJECTS)
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)>>
    <AND .F <RETURN .F>>
    ;"check for light"
    <COND (<NOT <FSET? ,HERE ,LIGHTBIT>>
                <COND (<NOT <SEARCH-FOR-LIGHT>>
                            <TELL "It's too dark to see anything here." CR>
                            <RFALSE>)>)>
    ;"check location"
    <MAP-CONTENTS (I ,HERE)
        ;<TELL "Room Loop object is currently " D .I CR>
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                ;<TELL "F is now set to " D .F CR>
                <RETURN>)
              ;"if any items are surfaces, open containers, or transparent containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
                          ;"open container"
                          <AND  <FSET? .I ,OPENABLEBIT>
                                <FSET? .I ,OPENBIT>
                          >
                          ;"always open container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLEBIT>>
                          >
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >
                                ;<TELL D .I " is a container." CR>
                                <SET F <CONTAINER-SEARCH .I .A .N>>
                                ;<TELL "Back in main FIND-ONE-OBJ loop and F is " D .F CR>
                                <AND .F <RETURN>>
                                ;<RETURN>
                      )>
               )
         >
    >
    <AND .F <RETURN .F>>       
    ;"check inventory"
    <MAP-CONTENTS (I ,WINNER)
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)
              ;"if any items are surfaces or open containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
                          ;"open container"
                          <AND  <FSET? .I ,OPENABLEBIT>
                                <FSET? .I ,OPENBIT>
                          >
                          ;"always open container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLEBIT>>
                          >
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >
                                ;<TELL D .I " is a container." CR>
                                <SET F <CONTAINER-SEARCH .I .A .N>>
                                ;<TELL "Back in main FIND-ONE-OBJ loop and F is " D .F CR>
                                <AND .F <RETURN>>
                                ;<RETURN>
                      )>
               )       
        >>
    <AND .F <RETURN .F>>
    ;"check global objects"
     <MAP-CONTENTS (I ,GLOBAL-OBJECTS)
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)>>
    <AND .F <RETURN .F>>
    ;"check local-globals"
    <MAP-CONTENTS (I ,LOCAL-GLOBALS)
        <COND (<AND <REFERS? .A .N .I> <GLOBAL-IN? .I ,HERE>>
                <SET F .I>
                <RETURN>)>>
    <AND .F <RETURN .F>> 
    ;"no match"
    ;"TO DO - Search through containers in rooms to see if I-matching NPC is there for 'does not seem to be here' message"
    <MAP-CONTENTS (I ROOMS)
            <MAP-CONTENTS (J .I)
                    <COND (<REFERS? .A .N .J>
                            <SET P .J>
                            ;<TELL "No match - P is " D .P CR>
                            <COND (<FSET? .P ,PERSONBIT>
                                    <TELL CT .P " does not seem to be here." CR > <RFALSE>)>
                            )>>>

    <TELL "You don't see that here." CR>
    <RFALSE>>
    
<ROUTINE CONTAINER-SEARCH (I A N "AUX" J F)
            <MAP-CONTENTS (J .I)
                    ;<TELL "Searching contents of " D .I CR>
                    ;<TELL "Current object is " D .J CR>
                    <COND (<REFERS? .A .N .J>
                            <SET F .J>
                            ;<TELL "Match found in container search, F is now " D .F CR>
                            <RETURN>)
                          (<OR 	<FSET? .I ,SURFACEBIT>
                                <AND  <FSET? .I ,OPENABLEBIT>
                                      <FSET? .I ,OPENBIT>
                                >
                                ;"the always-open case"
                                <AND  <FSET? .I ,CONTBIT> 
                                      <NOT <FSET? .I ,OPENABLEBIT>>
                                >
                                 ;"transparent container"
                                <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >                      				
                      ;<TELL "Found another container - about to search through " D .J CR>
                                    <SET F <CONTAINER-SEARCH .J .A .N>>
                                    <AND .F <RETURN>>)    	
                     >
             >
            <AND .F <RETURN .F>>
 >

<ROUTINE GLOBAL-IN? (O R)
    <IN-PB/WTBL? .R ,P?GLOBAL .O>>

;"Making this a macro is tempting, but it'd evaluate the parameters in the wrong order"
;<DEFMAC GLOBAL-IN? ('O 'R)
    <FORM IN-PB/WTBL? .R ',P?GLOBAL .O>>

<ROUTINE REFERS? (A N O)
    <AND
        <OR <0? .A> <IN-PB/WTBL? .O ,P?ADJECTIVE .A>>
            <IN-PWTBL? .O ,P?SYNONYM .N>>>

<VERSION?
    (ZIP
        ;"V3 has no INTBL? opcode"
        <ROUTINE IN-PWTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- </ <PTSIZE .PT> 2> 1>>
            <REPEAT ((I 0))
                <COND
                    (<==? <GET .PT .I> .V> <RTRUE>)
                    (<IGRTR? I .MAX> <RFALSE>)>>>

        <ROUTINE IN-PBTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- <PTSIZE .PT> 1>>
            <REPEAT ((I 0))
                <COND
                    (<==? <GETB .PT .I> .V> <RTRUE>)
                    (<IGRTR? I .MAX> <RFALSE>)>>>)
    (EZIP
        ;"V4 only has the 3-argument (word) form of INTBL?"
        <ROUTINE IN-PWTBL? (O P V "AUX" PT LEN)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET LEN </ <PTSIZE .PT> 2>>
            <AND <INTBL? .V .PT .LEN> <RTRUE>>>

        <ROUTINE IN-PBTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- <PTSIZE .PT> 1>>
            <REPEAT ((I 0))
                <COND
                    (<==? <GETB .PT .I> .V> <RTRUE>)
                    (<IGRTR? I .MAX> <RFALSE>)>>>)
    (T
        ;"use built-in INTBL? in V5+"
        <ROUTINE IN-PWTBL? (O P V "AUX" PT LEN)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
        <SET LEN </ <PTSIZE .PT> 2>>
        <AND <INTBL? .V .PT .LEN> <RTRUE>>>
    
        <ROUTINE IN-PBTBL? (O P V "AUX" PT LEN)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET LEN <PTSIZE .PT>>
            <AND <INTBL? .V .PT .LEN 1> <RTRUE>>>)>

<ROUTINE DUMPLINE ("AUX" (P <+ ,LEXBUF 2>) (WDS <GETB ,LEXBUF 1>))
    <TELL N .WDS " words:">
    <REPEAT ()
        <COND (<DLESS? WDS 0> <CRLF> <RTRUE>)
            (ELSE <TELL " "> <DUMPWORD <GET .P 0>>)>
        <SET P <+ .P 4>>>>
        
<ROUTINE DUMPLEX ("AUX" C (P <+ ,LEXBUF 2>) (WDS <GETB ,LEXBUF 1>))
    ;<TELL N .WDS " words:">
    <SET C 1>
        <REPEAT ()
        <TELL N .C " of LEXBUF is " N <GET ,LEXBUF .C> CR >
        <SET C <+ .C 1>>
        <COND (<G? .C .WDS> <RETURN>)>
    >>


<ROUTINE DUMPWORD (W "AUX" FL)
    <COND (.W
            <PRINTB .W>
            <TELL "(">
            <SET FL <GETB .W ,VOCAB-FL>>
            <COND (<BTST .FL ,PS?BUZZ-WORD> <TELL "B">)>
            <COND (<BTST .FL ,PS?PREPOSITION> <TELL "P">)>
            <COND (<BTST .FL ,PS?DIRECTION> <TELL "D">)>
            <COND (<BTST .FL ,PS?ADJECTIVE> <TELL "A">)>
            <COND (<BTST .FL ,PS?VERB> <TELL "V">)>
            <COND (<BTST .FL ,PS?OBJECT> <TELL "O">)>
            <TELL ")">)
        (ELSE <TELL "---">)>>
        

<ROUTINE COPY-LEXBUF ("AUX" C W (WDS <GETB ,LEXBUF 1>))  
        <SET C 1>
        <PUTB ,BACKLEXBUF 1 .WDS>
        <REPEAT ()
              <SET W <GET ,LEXBUF .C>>
              ;<TELL N .C "COPY LEX W is " N .W CR>
              <PUT ,BACKLEXBUF  .C .W> 
              <SET C <+ .C 1>>
              <COND (<G? .C .WDS> <RETURN>)>
         >
>

<ROUTINE RESTORE-LEX ("AUX" C W (WDS <GETB ,BACKLEXBUF 1>))  
        <SET C 1>
        <PUTB ,LEXBUF 1 .WDS>
        <REPEAT ()
              <SET W <GET ,BACKLEXBUF .C>>
              ;<TELL N .C "RESTORE LEX W is " N .W CR>
              <PUT ,LEXBUF .C .W> 
              <SET C <+ .C 1>>
              <COND (<G? .C .WDS> <RETURN>)>
         >
>

<ROUTINE COPY-READBUF ("AUX" C W)  
        <SET C 1>
        <REPEAT ()
              <SET W <GETB ,READBUF .C>>
              <PUTB ,BACKREADBUF .C .W> 
              <SET C <+ .C 1>>
              <COND (<G? .C 100> <RETURN>)>
         >
>


<ROUTINE RESTORE-READBUF ("AUX" C W)  
        <SET C 1>
        <REPEAT ()
              <SET W <GETB ,BACKREADBUF .C>>
              <PUTB ,READBUF .C .W> 
              <SET C <+ .C 1>>
              <COND (<G? .C 100> <RETURN>)>
         >
>

<ROUTINE DUMPBUF ("AUX" C (WDS <GETB ,READBUF 1>))
    ;<TELL N .WDS " words:">
    <SET C 1>
        <REPEAT ()
        <TELL N .C " of READBUF is " N <GET ,READBUF .C> CR >
        <SET C <+ .C 1>>
        <COND (<G? .C 100> <RETURN>)>
    >>
               

;"The read buffer has a slightly different format on V3."
<ROUTINE READLINE ()
    ;"skip input if doing an AGAIN"
    <COND (<AND ,AGAINCALL>
                    <RETURN>)>
    <TELL CR "> ">
    <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
    <VERSION? (ZIP <>)
              (ELSE
               <PUTB ,READBUF 1 0>
               <UPDATE-STATUS-LINE>)>
    <READ ,READBUF ,LEXBUF>>


"Action framework"

<ROUTINE PERFORM (ACT "OPT" DOBJ IOBJ "AUX" PRTN RTN OA OD ODD OI WON)
    <IF-DEBUG <TELL "[PERFORM: ACT=" N .ACT " DOBJ=" N .DOBJ " IOBJ=" N .IOBJ "]" CR>>
    <SET PRTN <GET ,PREACTIONS .ACT>>
    <SET RTN <GET ,ACTIONS .ACT>>
    <SET OA ,PRSA>
    <SET OD ,PRSO>
    <SET ODD ,PRSO-DIR>
    <SET OI ,PRSI>
    <SETG PRSA .ACT>
    <SETG PRSO .DOBJ>
    <SETG PRSO-DIR <==? .ACT ,V?WALK>>
    <SETG PRSI .IOBJ>
    <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>
    <SETG PRSA .OA>
    <SETG PRSO .OD>
    <SETG PRSO-DIR .ODD>
    <SETG PRSI .OI>
    .WON>

;"Handler order:
   player's ACTION,
   location's ACTION (M-BEG),
   verb preaction,
   PRSI's location's CONTFCN,
   PRSI's ACTION,
   PRSO's location's CONTFCN,
   PRSO's ACTION,
   verb action."

<ROUTINE PERFORM-CALL-HANDLERS (PRTN RTN "AUX" AC RM)
    <COND (<AND <SET AC <GETP ,WINNER ,P?ACTION>>
                <APPLY .AC>>
           <RTRUE>)
          (<AND <SET RM <LOC ,WINNER>>
                <SET AC <GETP .RM ,P?ACTION>>
                <APPLY .AC ,M-BEG>>
           <RTRUE>)
          (<AND .PRTN <APPLY .PRTN>>
           <RTRUE>)
          (<AND ,PRSI 
                <SET AC <GETP <LOC ,PRSI> ,P?CONTFCN>>
                <APPLY .AC>>
           <RTRUE>)
          (<AND ,PRSI 
                <SET AC <GETP ,PRSI ,P?ACTION>>
                <APPLY .AC>>
           <RTRUE>)
          (<AND ,PRSO
                <NOT ,PRSO-DIR>
                <SET AC <GETP <LOC ,PRSO> ,P?CONTFCN>>
                <APPLY .AC>>
           <RTRUE>)    
          (<AND ,PRSO
                <NOT ,PRSO-DIR>
                <SET AC <GETP ,PRSO ,P?ACTION>>
                <APPLY .AC>>
           <RTRUE>)
          (ELSE <APPLY .RTN>)>>

<ROUTINE GOTO (RM)
    <SETG HERE .RM>
    <MOVE ,WINNER ,HERE>
    <APPLY <GETP .RM ,P?ACTION> ,M-ENTER>
    ;"moved V-LOOK into GOTO so descriptors will be called when you call GOTO from a PER routine, etc"
    <COND
     (<DESCRIBE-ROOM ,HERE>
        <DESCRIBE-OBJECTS ,HERE>)>
    <FSET ,HERE ,TOUCHBIT>>

"Misc Routines"

<ROUTINE FIND-IN (C BIT "OPT" WORD "AUX" N W)
    <MAP-CONTENTS (I .C)
        <COND (<FSET? .I  .BIT>
               <SET N <+ .N 1>>
               <SET W .I>)>>
    <COND
        ;"If less or more than one match, we return false."
        (<NOT <EQUAL? .N 1>>
         <RFALSE>)
        ;"if the routine was given the optional word, print [<word> the object]"
        (.WORD
         <TELL "[" .WORD " " T .W "]" CR>)>
    .W>

<VERSION? (ZIP)
          (T
           <CONSTANT H-NORMAL 0>
           <CONSTANT H-INVERSE 1>
           <CONSTANT H-BOLD 2>
           <CONSTANT H-ITALIC 4>
           <CONSTANT H-MONO 8>)>

<ROUTINE ITALICIZE (STR "AUX" A)
    <VERSION? (ZIP)
              (T <HLIGHT ,H-ITALIC>)>
    <TELL .STR>
    <VERSION? (ZIP)
              (T <HLIGHT ,H-NORMAL>)>>

<ROUTINE PICK-ONE (TABL "AUX" LENGTH CNT RND S MSG)
       <SET LENGTH <GET .TABL 0>>
       <SET CNT <GET .TABL 1>>
       <REPEAT ()
               <PUT ,TEMPTABLE .S <GET .TABL <+ .CNT .S>>  >
               <SET S <+ .S 1>>
               ;<PROG () <TELL "IN LOOP: S IS NOW: "> <PRINTN .S> <TELL CR>>
               <COND (<G? <+ .S .CNT> .LENGTH> <RETURN>)>
       >
       ;<PROG () <TELL "S IS CURRENTLY: "> <PRINTN .S> <TELL CR>>
       <SET RND <- <RANDOM .S> 1>>
       ;<PROG () <TELL "RND IS CURRENTLY: "> <PRINTN .RND> <TELL CR>>
       <SET MSG <GET ,TEMPTABLE .RND>>
       <PUT .TABL <+ .CNT .RND> <GET .TABL .CNT> >
       <PUT .TABL .CNT .MSG >
       <SET CNT <+ 1 .CNT>>
       <COND (<G? .CNT .LENGTH> <SET CNT 2>)>
       <PUT .TABL 1 .CNT>
       <RETURN .MSG>
>


<ROUTINE PICK-ONE-R (TABL "AUX" MSG RND)
      <SET RND <RANDOM <GET .TABL 0>>>
      <SET MSG <GET .TABL .RND>>
      <RETURN .MSG>>
      
<VERSION?
    (ZIP
        <DEFMAC INIT-STATUS-LINE () <>>
        <DEFMAC UPDATE-STATUS-LINE () '<USL>>
    )
    (T
        <ROUTINE INIT-STATUS-LINE ()
            <SPLIT 1>
            <CLEAR 1>>

        <ROUTINE UPDATE-STATUS-LINE ("AUX" WIDTH)
            <SCREEN 1>
            <HLIGHT 1>   ;"reverses the fg and bg colors"
            <FAKE-ERASE>
            <TELL " " D ,HERE>
            <SET WIDTH <LOWCORE SCRH>>
            <CURSET 1 <- .WIDTH 22>>
            <TELL "Score: ">
            <PRINTN ,SCORE>
            <CURSET 1 <- .WIDTH 10>>
            <TELL "Moves: ">
            <PRINTN ,TURNS>
            <SCREEN 0>
            <HLIGHT 0>>

        <ROUTINE FAKE-ERASE ("AUX" CNT WIDTH)
            <CURSET 1 1>
            <DO (I <LOWCORE SCRH> 1 -1) <PRINTC !\ >>
            <CURSET 1 1>>
    )>

<ROUTINE JIGS-UP (TEXT "AUX" RESP (Y 0) (X <>) R)
    <TELL .TEXT CR CR>
     <TELL "    ****  The game is over  ****" CR CR>
     ;"<TELL "    ****  You have died  ****" CR CR>"
     <VERSION?
            (ZIP <>)
            (EZIP <>)
            (ELSE <SET X T>)>
     <COND (<AND .X>
                <PRINTI "Would you like to RESTART, UNDO, RESTORE, or QUIT? > ">)
            (T
                <PRINTI "Would you like to RESTART, RESTORE or QUIT? > ">)>
     <REPEAT ()
        <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
        <VERSION? (ZIP <>) (ELSE <PUTB ,READBUF 1 0>)>
        <READ ,READBUF ,LEXBUF>
            <COND (<EQUAL? <GET ,LEXBUF 1> ,W?RESTART>
                    <SET Y 1>
                    <RETURN>)
               (<EQUAL? <GET ,LEXBUF 1>  ,W?RESTORE>
                    <SET Y 2>
                    <RETURN>)
              (<EQUAL? <GET ,LEXBUF 1> ,W?QUIT>
                    <SET Y 3>
                    <RETURN>)
              (<EQUAL? <GET ,LEXBUF 1>  ,W?UNDO>
                    <SET Y 4>
                    <RETURN>)
               (T
                    <COND (<AND .X>
                        <TELL CR "(Please type RESTART, UNDO, RESTORE or QUIT)  >" >)
                             (ELSE
                            <TELL CR "(Please type RESTART, RESTORE or QUIT) > " > )>
                     )>>
    <COND (<=? .Y 1>
              <RESTART>)
          (<=? .Y 2>
              <COND (<AND .X>
                        <SET R <RESTORE>>
                        ;"Workaround for restore failing duirng JIGS-UP, otherwise game will continue, even though player is 'dead'"
                        <COND (<NOT .R> <TELL "Restore failed - restarting instead." CR> 
                                        <TELL "Press any key >">
                                        <GETONECHAR>
                                        <RESTART>)>)
                    (T
                        <JIGS-UP "">)>)
          (<=? .Y 3>
              <TELL CR "Thanks for playing." CR>
              <QUIT>)
         (<=? .Y 4>
               <COND (<AND .X>
                        <SET R <V-UNDO>>
                        ;"Workaround for undo failing duirng JIGS-UP, otherwise game will continue, even though player is 'dead'"
                        <COND (<NOT .R> <TELL "Undo failed - restarting instead." CR> 
                                        <TELL "Press any key >">
                                        <GETONECHAR>
                                        <RESTART>)>)
                    (T
                        <JIGS-UP "">)>)
              
              
              
              >
    
>      
    
<ROUTINE ROB (VICTIM "OPT" DEST "AUX" N I)
     <SET I <FIRST? .VICTIM>>
     <REPEAT ()
         <COND (<NOT .I>
            <RETURN>)>
         <SET N <NEXT? .I>>
         <COND (<AND <FSET? .I ,WORNBIT>
                     <NOT <FSET? .DEST ,PERSONBIT>>>
                        <FCLEAR .I ,WORNBIT>)>
         <COND (<NOT .DEST>
                    <REMOVE .I>)
               (ELSE
                    <MOVE .I .DEST>)>
         <SET I .N>>>

<ROUTINE YES? ("AUX" RESP)
     <PRINTI " (y/n) >">
     <REPEAT ()
         <SET RESP <GETONECHAR>>
         <COND (<EQUAL? .RESP !\Y !\y>
                    <RTRUE>)
               (<EQUAL? .RESP !\N !\n>
                    <RFALSE>)
               (T
                        "<CRLF>"
                    <TELL "(Please type y or n) >" >)>
                 >>

<VERSION?
    (ZIP
        <ROUTINE GETONECHAR ()
            <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
            <READ ,READBUF ,LEXBUF>
            <GETB ,READBUF 1>>)
    (ELSE
        <ROUTINE GETONECHAR ()
                 <BUFOUT <>>
                 <BUFOUT T>
                 <INPUT 1>>)>			
         
<ROUTINE VISIBLE? (OBJ "AUX" P M (T 0))
    <SET P <LOC .OBJ>>
    <SET M <META-LOC .OBJ>>
    <COND (<NOT <=? .M ,HERE>>
                <COND (<OR
                    <AND <=? .P ,LOCAL-GLOBALS>
                    <GLOBAL-IN? .OBJ ,HERE>>
                    <=? .P ,GLOBAL-OBJECTS>>
                                    <RTRUE>)
                                            (ELSE <RFALSE>)>)
             (ELSE
                ;<TELL "The meta-loc = HERE and the LOC is " D .P CR>
                <REPEAT ()
                    <COND 
                        (<AND 
                            <FSET? .P ,CONTBIT>
                            <NOT <FSET? .P ,SURFACEBIT>>
                            <NOT <FSET? .P ,TRANSBIT>>
                            <NOT <FSET? .P ,OPENBIT>>>
                                ;<TELL D .P " is a non-transparent container that is closed." CR>
                                <SET T 0>
                                <RETURN>)
                        (<OR
                            <=? .P ,HERE>
                            <=? .P ,WINNER>>
                                ;<TELL D .P " is either = HERE or the player." CR>
                                <SET T 1>
                                <RETURN>)
                        (ELSE
                            <SET P <LOC .P>>)>>
                <COND (<=? .T 1>
                        <RTRUE>)
                      (ELSE
                        <RFALSE>)>							
        )>		
>

<ROUTINE ACCESSIBLE? (OBJ "AUX" L M (T 0))
       ;"currently GLOBALs and LOCAL-GLOBALS return false since they are non-interactive scenery."
       <SET L <LOC .OBJ>>
       <SET M <META-LOC .OBJ>>
       <COND (<NOT <=? .M ,HERE>>
                ;<TELL "Object not in room" CR>
                <RFALSE>)>
       <REPEAT ()
                                      ;<TELL "In accessible repeat loop, L is " D .L CR>
                <COND
                                               (<AND
                                                       <FSET? .L ,CONTBIT>
                                                       <NOT <FSET? .L ,OPENBIT>>
                            <NOT <FSET? .L ,SURFACEBIT>>>
                                                                       ;<TELL D .L " is a closed container." CR>
                                                                       <SET T 0>
                                                                       <RETURN>)
                    (<OR
                                                       <=? .L ,HERE>
                                                       <=? .L ,WINNER>>
                                                               ;<TELL D .L " is either = HERE or the player." CR>
                                                               <SET T 1>
                                                               <RETURN>)
                                               (ELSE
                                                       <SET L <LOC .L>>)>>
                               <COND (<=? .T 1>
                                               <RTRUE>)
                                         (ELSE
                                               <RFALSE>)>
>

<ROUTINE HELD? (OBJ "OPT" (HLDR <>) "AUX" TH (T 0))
       <OR .HLDR <SET HLDR ,WINNER>>
       <REPEAT ()
                                      <COND 
                    (<=? <LOC .OBJ> .HLDR>
                        <SET T 1>
                        <RETURN>)
                    (<NOT <AND .OBJ>>
                        <SET T 0>
                        <RETURN>)
                    (ELSE
                        <SET OBJ <LOC .OBJ>>)>>
                               <COND (<=? .T 1>
                                               <RTRUE>)
                                         (ELSE
                                               <RFALSE>)>
>

<ROUTINE META-LOC (OBJ "AUX" P (T 0))
    <SET P <LOC .OBJ>>
    <COND (<IN? .P ,ROOMS>
                <RETURN .P>)>
    <REPEAT ()
        ;<TELL "In META-LOC repeat -- P is " D .P CR>
        <COND (<OR
                    <FSET? .P ,PERSONBIT>
                     <FSET? .P ,CONTBIT>
                     <=? .P ,LOCAL-GLOBALS>
                     <=? .P ,GLOBAL-OBJECTS >
                     <=? .P ,GENERIC-OBJECTS>>
                            <SET P <LOC .P>>)>
        <COND  (<IN? .P ,ROOMS>
                    <SET T 1>
                    <RETURN>)
                   (<NOT .P>
                    <SET T 0>
                    <RETURN>)>>
        <COND (<=? T 1>
                    <RETURN .P>)
                  (ELSE
                    <RFALSE>)>					
>

<ROUTINE NOW-DARK ()
        <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
                     <NOT <SEARCH-FOR-LIGHT>>>
                        <TELL "You are plunged into darkness." CR>
                        )>>
    
    
<ROUTINE NOW-LIT ()
        <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
                     <NOT <SEARCH-FOR-LIGHT>>>
                        <TELL "You can see your surroundings now." CR CR>
                        <SETG NLITSET 1>
                        <V-LOOK>)>>
      

<INSERT-FILE "events">
            
<INSERT-FILE "verbs">

"Objects"

;"This has all the flags, just in case other objects don't define them."
<OBJECT ROOMS
    (FLAGS PERSONBIT TOUCHBIT TAKEBIT WEARBIT INVISIBLE WORNBIT LIGHTBIT LOCKEDBIT
           SURFACEBIT CONTBIT NDESCBIT VOWELBIT NARTICLEBIT OPENBIT OPENABLEBIT READBIT DEVICEBIT ONBIT EDIBLEBIT TRANSBIT FEMALEBIT PLURALBIT)>

         
;"This has any special properties, just in case other objects don't define them."
;"I guess all properties should go on this dummy object, just to be safe?"
<OBJECT NULLTHANG
    (SIZE 2)
    (ADJECTIVE NULLTHANG)
    (LDESC <>)
    (FDESC <>)
    (GLOBAL NULLTHANG)
    (TEXT <>)
    (CONTFCN <>)
    (DESCFCN <>)
    (TEXT-HELD <>)
    (CAPACITY 10)>
    
<OBJECT GLOBAL-OBJECTS>

<OBJECT GENERIC-OBJECTS>

<OBJECT LOCAL-GLOBALS>

<OBJECT IT
    (SYNONYM IT)>
    
<OBJECT HIM
    (SYNONYM HIM)>
    
<OBJECT HER
    (SYNONYM HER)>
    
<OBJECT THEM
    (SYNONYM THEM)>

<OBJECT PLAYER
    (DESC "you")
    (SYNONYM ME MYSELF)
    (FLAGS NARTICLEBIT PERSONBIT TOUCHBIT)
    (ACTION PLAYER-R)>

<ROUTINE PLAYER-R ()
    <COND (<N==? ,PLAYER ,PRSO> <RFALSE>)>
    <COND
        (<VERB? EXAMINE> <PRINTR "You look like you're up for an adventure.">)
        (<VERB? TAKE> <PRINTR "That couldn't possibly work.">)>>
