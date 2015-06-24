"Library header"

<SETG ZILLIB-VERSION "J1">

<VERSION?
    (ZIP)
    (EZIP)
    (ELSE <ZIP-OPTIONS UNDO COLOR>)>

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
<GLOBAL AGAINCALL <>>
<GLOBAL USAVE 0>
<GLOBAL NLITSET <>>
<GLOBAL MODE 1>
<CONSTANT SUPERBRIEF 0>
<CONSTANT BRIEF 1>
<CONSTANT VERBOSE 2>

<IF-DEBUG
    <GLOBAL DBCONT <>>
    <GLOBAL DTURNS <>>>

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
<CONSTANT READBUF <ITABLE NONE ,READBUF-SIZE (BYTE)>>
<CONSTANT BACKREADBUF <ITABLE NONE ,READBUF-SIZE (BYTE)>>

<CONSTANT LEXBUF-SIZE 59>
<CONSTANT LEXBUF <ITABLE ,LEXBUF-SIZE (LEXV) 0 #BYTE 0 #BYTE 0>>
<CONSTANT BACKLEXBUF <ITABLE ,LEXBUF-SIZE (LEXV) 0 #BYTE 0 #BYTE 0>>

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

;"Determines whether a word has the given part of speech, and returns
its part of speech value if so.

Args:
  W: The word.
  PS: The part of speech's PS? constant, e.g. PS?VERB.
  P1: The part of speech's P1? constant, e.g. P1?VERB. If omitted,
      the value will not be returned, only a boolean indicating whether
      the word has the given part of speech.

Returns:
  If the word does not have the given part of speech, returns 0. Otherwise,
  returns the word's value for the given part of speech if P1 was supplied,
  or 1 if not."
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

;"Gets the word at the given index in LEXBUF.

Args:
  N: The index, starting at 1.

Returns:
  A pointer to the vocab word, or 0 if the word at the given index was
  unrecognized."
<ROUTINE GETWORD? (N "AUX" R)
    <SET R <GET ,LEXBUF <- <* .N 2> 1>>>
    <IF-DEBUG
        <TELL "[GETWORD " N .N " = ">
        <COND (.R <TELL B .R>) (ELSE <TELL "?">)>
        <TELL "]" CR>>
    .R>

;"Prints the word at the given index in LEXBUF.

Args:
  N: The index, starting at 1."
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

;"Reads and parses a command.

Sets:
  P-LEN
  P-V
  P-NOBJ
  P-DOBJS
  P-IOBJS
  P-P1
  P-P2
  HERE
  PRSA
  PRSO
  PRSO-DIR
  PRSI
  USAVE
  P-DOBJS
  P-DOBJEX
  P-DOBJS-BACK
  P=DOBJEX-BACK
  IT-ONCE
  P-TOBJS-BACK
  P-TOBJEX-BACK
  THEM-ONCE
  P-MOBJS-BACK
  P-MOBJEX-BACK
  HIM-ONCE
  P-FOBJS-BACK
  P-FOBJEX-BACK
  HER-ONCE
"
<ROUTINE PARSER ("AUX" NOBJ VAL DIR)
    ;"Need to (re)initialize locals here since we use AGAIN"
    <SET NOBJ <>>
    <SET VAL <>>
    <SET DIR <>>
    <SETG HERE <LOC ,PLAYER>>
    ;"Fill READBUF and LEXBUF"
    <READLINE>
    <IF-DEBUG <DUMPLINE>>
    <SETG P-LEN <GETB ,LEXBUF 1>>
    <SETG P-V <>>
    <SETG P-NOBJ 0>
    <PUT ,P-DOBJS 0 0>
    <PUT ,P-IOBJS 0 0>
    <SETG P-P1 <>>
    <SETG P-P2 <>>
    <SETG HERE <LOC ,WINNER>>	;"TODO: why WINNER here vs. PLAYER above?"
    ;"Identify the verb, prepositions, and noun clauses"
    <REPEAT ((I 1) W)
        <COND (<G? .I ,P-LEN>
               ;"Reached the end of the command"
               <RETURN>)
              (<NOT <SET W <GETWORD? .I>>>
               ;"Word not in vocabulary"
               <TELL "I don't know the word \"">
               <PRINT-WORD .I>
               <TELL "\"." CR>
               <RFALSE>)
              (<AND <CHKWORD? .W ,PS?VERB> <NOT ,P-V>>
               ;"Found the verb"
               <SETG P-V <WORD? .W VERB>>)
              (<AND <EQUAL? ,P-V <> ,ACT?WALK>
                    <SET VAL <WORD? .W DIRECTION>>>
               ;"Found a direction"
               <SET DIR .VAL>)
              (<SET VAL <CHKWORD? .W ,PS?PREPOSITION 0>>
               ;"Found a preposition"
               ;"Only keep the first preposition for each object"
               <COND (<AND <==? .NOBJ 0> <NOT ,P-P1>>
                      <SETG P-P1 .VAL>)
                     (<AND <==? .NOBJ 1> <NOT ,P-P2>>
                      <SETG P-P2 .VAL>)>)
              (<STARTS-CLAUSE? .W>
               ;"Found a noun clause"
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
               ;"Unexpected word type"
               <TELL "I didn't expect the word \"">
               <PRINT-WORD .I>
               <TELL "\" there." CR>
               <RFALSE>)>
        <SET I <+ .I 1>>>
    <SETG P-NOBJ .NOBJ>
    <IF-DEBUG
        <TELL "[PARSER: V=" N ,P-V " NOBJ=" N ,P-NOBJ
              " P1=" N ,P-P1 " DOBJS=" N <GETB ,P-DOBJS 0>
              " P2=" N ,P-P2 " IOBJS=" N <GETB ,P-IOBJS 0> "]" CR>>
    ;"If we have a direction, it's a walk action, and no verb is needed"
    <COND (.DIR
           <SETG PRSO-DIR T>
           <SETG PRSA ,V?WALK>
           <SETG PRSO .DIR>
           <SETG PRSI <>>
           <IF-UNDO
               ;"save state for undo after moving from room to room"
               <COND (<NOT <OR <VERB? UNDO> ,AGAINCALL>>
                      <SETG USAVE <ISAVE>>
                      <COND (<EQUAL? ,USAVE 2>
                             <TELL "Previous turn undone." CR>
                             <AGAIN>)>)>>
           <RTRUE>)>
    ;"Otherwise, a verb is required"
    <COND (<NOT ,P-V>
           <TELL "That sentence has no verb." CR>
           <RFALSE>)>
    <SETG PRSO-DIR <>>
    ;"Match syntax lines and objects"
    <COND (<NOT <AND <MATCH-SYNTAX> <FIND-OBJECTS>>>
           <RFALSE>)>
    ;"Save UNDO state"
    <IF-UNDO
        <COND (<AND <NOT <VERB? UNDO>>
                    <NOT ,AGAINCALL>>
               <SETG USAVE <ISAVE>>
               ;<TELL "ISAVE returned " N ,USAVE CR>
               <COND (<EQUAL? ,USAVE 2>
                      <TELL "Previous turn undone." CR>
                      ;<SETG USAVE 0>  ;"prevent undoing twice in a row"
                      <AGAIN>
                      ;<SETG NOUAGAIN 0> )>)>>
    ;"if successful PRSO and not after an IT use, back up PRSO for IT"
    <COND (<AND <EQUAL? ,IT-USE 0>
                ,PRSO
                <NOT <FSET? ,PRSO ,PERSONBIT>>
                <NOT <FSET? ,PRSO ,PLURALBIT>>>
           ;<TELL "Copying P-DOBJS into P-DOBJS-BACK" CR>
           <COPY-TABLE ,P-DOBJS ,P-DOBJS-BACK 21>
           <COPY-TABLE ,P-DOBJEX ,P-DOBJEX-BACK 21>
           <COND (<EQUAL? ,IT-ONCE 0> <SET IT-ONCE 1>)>)
          ;"if PRSO has PLURALBIT, back up to THEM instead"
          (<AND <EQUAL? ,THEM-USE 0>
                ,PRSO
                <NOT <FSET? ,PRSO ,PERSONBIT>>
                <FSET? ,PRSO ,PLURALBIT>>
           ;<TELL "Copying P-DOBJS into P-TOBJS-BACK" CR>
           <COPY-TABLE ,P-DOBJS ,P-TOBJS-BACK 21>
           <COPY-TABLE ,P-DOBJEX ,P-TOBJEX-BACK 21>
           <COND (<EQUAL? ,THEM-ONCE 0> <SET THEM-ONCE 1>)>)
          ;"if successful PRSO who is male, back up PRSO for HIM"
          (<AND <EQUAL? ,HIM-USE 0>
                ,PRSO
                <FSET? ,PRSO ,PERSONBIT>
                <NOT <FSET? ,PRSO ,FEMALEBIT>>>
                              ;<TELL "Copying P-DOBJS into P-MOBJS-BACK" CR>
                              <COPY-TABLE ,P-DOBJS ,P-MOBJS-BACK 21>
                              <COPY-TABLE ,P-DOBJEX ,P-MOBJEX-BACK 21>
                              <COND (<0? ,HIM-ONCE> <SET HIM-ONCE 1>)>)
          ;"if successful PRSO who is female, back up PRSO for HER"
          (<AND <EQUAL? ,HER-USE 0>
                ,PRSO
                <FSET? ,PRSO ,PERSONBIT>
                <FSET? ,PRSO ,FEMALEBIT>>
           ;<TELL "Copying P-DOBJS into P-FOBJS-BACK" CR>
           <COPY-TABLE ,P-DOBJS ,P-FOBJS-BACK 21>
           <COPY-TABLE ,P-DOBJEX ,P-FOBJEX-BACK 21>
           <COND (<0? ,HER-ONCE> <SET HER-ONCE 1>)>)>
    <RTRUE>>


<VERSION?
    (ZIP
        ;"Copies a number of words from one table to another.
        
        If the tables overlap, the result is undefined.
        
        Args:
          SRC: A pointer to the source table.
          DEST: A poitner to the destination table.
          LEN: The number of words to copy."
        <ROUTINE COPY-TABLE (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUT .DEST .I <GET .SRC .I>>>>
        
        ;"Copies a number of bytes from one table to another.
        
        If the tables overlap, the result is undefined.
        
        Args:
          SRC: A pointer to the source table.
          DEST: A poitner to the destination table.
          LEN: The number of bytes to copy."
        <ROUTINE COPY-TABLE-B (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUTB .DEST .I <GETB .SRC .I>>>>)
    (EZIP
        <ROUTINE COPY-TABLE (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUT .DEST .I <GET .SRC .I>>>>

        <ROUTINE COPY-TABLE-B (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUTB .DEST .I <GETB .SRC .I>>>>)
    (ELSE
        <DEFMAC COPY-TABLE ('SRC 'DEST 'LEN "AUX" BYTES)
            ;"someday the compiler should do this optimization on its own..."
            <COND (<TYPE? .LEN FIX> <SET BYTES <* .LEN 2>>)
                  (ELSE <SET BYTES <FORM * .LEN 2>>)>
            <FORM COPYT .SRC .DEST .BYTES>>
        
        <DEFMAC COPY-TABLE-B ('SRC 'DEST 'LEN)
            <FORM COPYT .SRC .DEST .LEN>>)>

;"Determines whether a given word can start a noun clause.

For a word to pass this test, it must be an article, adjective, or noun.

Args:
  W: The word to test.

Returns:
  True if the word can start a noun clause."
<ROUTINE STARTS-CLAUSE? (W)
    ;"The COND should be unnecessary, but ZILF generates ugly code without it"
    <COND (<OR <EQUAL? .W ,W?A ,W?AN ,W?THE>
               <CHKWORD? .W ,PS?ADJECTIVE>
               <WORD? .W OBJECT>>
           <RTRUE>)>
    <RFALSE>>

<CONSTANT MCM-ALL 1>
<CONSTANT MCM-ANY 2>


;"Attempts to match a noun clause.

If the match fails, an error message may be printed.

Args:
  WN: The 1-based word number where the noun clause starts.
  YTBL: The address of a table in which to return the adjective/noun pairs that
      should be included ('yes').
  NTBL: The address of a table in which to return the adjective/noun pairs that
      should be excluded ('no').

Returns:
  True if the noun clause was matched. YTBL and NTBL may be modified even if
  this routine returns false.
"
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
                 (<AND <0? .NOUN>                       ;"no noun"
                       <WORD? .W OBJECT>                ;"word can be a noun"
                       <OR ;"word is at end of line"
                           <==? .WN ,P-LEN>
                           ;"next word is not adj/noun"
                           <BIND ((NW <GETWORD? <+ .WN 1>>))
                               <NOT <OR <CHKWORD? .NW ,PS?ADJECTIVE>
                                        <CHKWORD? .NW ,PS?OBJECT>>>>>>
                  <SET NOUN .W>)
                 (<==? .TI ,P-MAXOBJS>
                  <TELL "That clause mentions too many objects." CR>
                  <RFALSE>)
                 (<NOT .ADJ> <SET ADJ .VAL>)>)
            ;"match nouns, exiting the loop if we already found one"
            (<WORD? .W OBJECT>
             <COND (.NOUN <RETURN>)
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

;"Attempts to match a syntax line for the current verb.

Uses:
  P-V
  P-NOBJ
  P-P1
  P-P2

Sets:
  PRSA

Returns:
  True if a syntax line was matched."
<ROUTINE MATCH-SYNTAX ("AUX" PTR CNT)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <IF-DEBUG <TELL "[MATCH-SYNTAX: " N .CNT " syntaxes at " N .PTR "]" CR>>
    <REPEAT ()
        ;<TELL "CNT is currently " N .CNT CR>
        ;<BIND ((TEST-PTR <GET .PTR ,SYN-NOBJ>) (TEST-P-NOBJ ,P-NOBJ))
            <TELL "<GETB .PTR ,SYN-NOBJ> is " N .TEST-PTR "and P-NOBJ is " N .TEST-P-NOBJ CR>>
        <COND (<DLESS? CNT 0>
               <TELL "I don't understand that sentence." CR>
               <RFALSE>)
              (<AND <==? <GETB .PTR ,SYN-NOBJ> ,P-NOBJ>
                    <OR <L? ,P-NOBJ 1>
                        <==? <GETB .PTR ,SYN-PREP1> ,P-P1>>
                    <OR <L? ,P-NOBJ 2>
                        <==? <GETB .PTR ,SYN-PREP2> ,P-P2>>>
               <SETG PRSA <GETB .PTR ,SYN-ACTION>>
               <RTRUE>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>>

<INSERT-FILE "scope">

<CONSTANT S-PRONOUN-UNKNOWN-PERSON "I'm unsure to whom you are referring.">
<CONSTANT S-PRONOUN-UNKNOWN-THING "I'm unsure what you're referring to.">

;"<FIND-OBJECTS-CHECK-PRONOUN .X IT DOBJ IOBJ>
  Expands to:
  <COND (<EQUAL? .X ,W?IT>
         <COND (<0? ,IT-ONCE>
                <TELL ,S-PRONOUN-UNKNOWN-THING CR>
                <RFALSE>)>
         <COPY-TABLE ,P-DOBJS-BACK ,P-IOBJS 21>
         <COPY-TABLE ,P-DOBJEX-BACK ,P-IOBJEX 21>
         <SETG IT-USE T>)
        (ELSE <SETG IT-USE <>>)>"
<DEFMAC FIND-OBJECTS-CHECK-PRONOUN ('X 'PRONOUN 'PRONOUN-TBL-STEM 'DEST-TBL-STEM
                                    "AUX" MSG VOCAB-WORD ONCE-VAR USE-VAR
                                    OBJS-BACK-TBL OBJEX-BACK-TBL
                                    OBJS-DEST-TBL OBJEX-DEST-TBL)
    <COND (<OR <=? .PRONOUN IT> <=? .PRONOUN THEM>>
           <SET MSG ',S-PRONOUN-UNKNOWN-THING>)
          (ELSE
           <SET MSG ',S-PRONOUN-UNKNOWN-PERSON>)>
    <SET WORD <PARSE <STRING "W?" <SPNAME .PRONOUN>>>>
    <SET ONCE-VAR <PARSE <STRING <SPNAME .PRONOUN> "-ONCE">>>
    <SET USE-VAR <PARSE <STRING <SPNAME .PRONOUN> "-USE">>>
    <SET OBJS-BACK-TBL <PARSE <STRING "P-" <SPNAME .PRONOUN-TBL-STEM> "S-BACK">>>
    <SET OBJEX-BACK-TBL <PARSE <STRING "P-" <SPNAME .PRONOUN-TBL-STEM> "EX-BACK">>>
    <SET OBJS-DEST-TBL <PARSE <STRING "P-" <SPNAME .DEST-TBL-STEM> "S">>>
    <SET OBJEX-DEST-TBL <PARSE <STRING "P-" <SPNAME .DEST-TBL-STEM> "EX">>>
    <FORM COND <LIST <FORM EQUAL? .X <FORM GVAL .WORD>>
                     <FORM COND <LIST <FORM 0? <FORM GVAL .ONCE-VAR>>
                                      <FORM TELL .MSG CR>
                                      <FORM RFALSE>>>
                     <FORM COPY-TABLE <FORM GVAL .OBJS-BACK-TBL> <FORM GVAL .OBJS-DEST-TBL> 21>
                     <FORM COPY-TABLE <FORM GVAL .OBJEX-BACK-TBL> <FORM GVAL .OBJEX-DEST-TBL> 21>
                     <FORM SETG .USE-VAR T>>
               <LIST ELSE <FORM SETG .USE-VAR <>>>>>

;"Attempts to match PRSO and PRSO, if necessary, after parsing a command.

Uses:
  P-NOBJ
  P-DOBJS

Sets:
  PRSO
  PRSI
  IT-USE
  THEM-USE
  HIM-USE
  HER-USE

Returns:
  True if all required objects were found, or false if not."
<ROUTINE FIND-OBJECTS ("AUX" X)
    <COND (<G=? ,P-NOBJ 1>
           <SET X <GET ,P-DOBJS 2>>
           ;<TELL "Find objects PRSO test - X is " N .X CR>
           <FIND-OBJECTS-CHECK-PRONOUN .X IT DOBJ DOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X THEM TOBJ DOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X HIM MOBJ DOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X HER FOBJ DOBJ>
           <COND (<NOT <SETG PRSO <FIND-ONE-OBJ ,P-DOBJS ,P-DOBJEX>>>
                  <RFALSE>)>)
          (ELSE <SETG PRSO <>>)>
    <COND (<G=? ,P-NOBJ 2>
           <SET X <GET ,P-IOBJS 2>>
           ;<TELL "Find objects PRSI test - X is " N .X CR>
           <FIND-OBJECTS-CHECK-PRONOUN .X IT DOBJ IOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X THEM TOBJ IOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X HIM MOBJ IOBJ>
           <FIND-OBJECTS-CHECK-PRONOUN .X HER FOBJ IOBJ>
           <COND (<NOT <SETG PRSI <FIND-ONE-OBJ ,P-IOBJS ,P-IOBJEX>>>
                  <RFALSE>)>)
          (ELSE <SETG PRSI <>>)>
    <RTRUE>>

;"Searches scope for a usable light source.

Returns:
  An object providing light, or false if no light source was found."
<ROUTINE SEARCH-FOR-LIGHT SFL ()
    <MAP-SCOPE (I) (LOCATION INVENTORY GLOBALS LOCAL-GLOBALS)
        <COND (<FSET? .I ,LIGHTBIT> <RETURN .I .SFL>)>>
    <RFALSE>>

;"Determines whether an object's contents are in scope (and provide light)
when the object is in scope.

Args:
  OBJ: The object to test.

Returns:
  True if the object's contents are in scope, otherwise false."
<ROUTINE SEE-INSIDE? (OBJ)
    ;"The COND should be unnecessary, but ZILF generates ugly code without it"
    <COND (<OR ;"We can always see the contents of surfaces"
               <FSET? .OBJ ,SURFACEBIT>
               ;"We can see inside containers if they're open, transparent, or
                 unopenable (= always-open)"
               <AND <FSET? .OBJ ,CONTBIT>
                    <OR <FSET? .OBJ ,OPENBIT>
                        <FSET? .OBJ ,TRANSBIT>
                        <NOT <FSET? .OBJ ,OPENABLEBIT>>>>>
           <RTRUE>)>
    <RFALSE>>

;"Attempts to find an object in scope, given the adjectives and nouns that
describe it.

Args:
  YTBL: A table of adjective/noun pairs that the object must have ('yes').
  NTBL: A table of adjective/noun pairs that the object must not have ('no').

Returns:
  The located object, or false if no matching object was found. "
<ROUTINE FIND-ONE-OBJ FOO (YTBL NTBL "AUX" A N)
    <SET A <GET .YTBL 1>>
    <SET N <GET .YTBL 2>>
    <MAP-SCOPE (I)
        <COND (<REFERS? .A .N .I>
               <RETURN .I .FOO>)>>
    ;"Not found"
    <COND (<==? ,MAP-SCOPE-STATUS ,MS-NO-LIGHT>
           <TELL "It's too dark to see anything here." CR>)
          (ELSE
           <TELL "You don't see that here." CR>)>
    <RFALSE>>
        
;"Determines whether a local-global object is present in a given room.

Args:
  O: The local-global object.
  R: The room.

Returns:
  True if the object is present in the room's GLOBAL property.
  Otherwise, false."
<ROUTINE GLOBAL-IN? (O R)
    <IN-PB/WTBL? .R ,P?GLOBAL .O>>

;"Making this a macro is tempting, but it'd evaluate the parameters in the wrong order"
;<DEFMAC GLOBAL-IN? ('O 'R)
    <FORM IN-PB/WTBL? .R ',P?GLOBAL .O>>

;"Determines whether an adjective/noun pair refer to a given object.

Args:
  A: The adjective word. May be 0.
  N: The noun word.
  O: The object."
<ROUTINE REFERS? (A N O)
    <AND
        <OR <0? .A> <IN-PB/WTBL? .O ,P?ADJECTIVE .A>>
        <IN-PWTBL? .O ,P?SYNONYM .N>>>

<VERSION?
    (ZIP
        ;"V3 has no INTBL? opcode"
        
        ;"Attempts to locate a word in a property table.
        
        Args:
          O: The object containing the property.
          P: The property number.
          V: The word to locate.
        
        Returns:
          True if the word is located, otherwise false."
        <ROUTINE IN-PWTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- </ <PTSIZE .PT> 2> 1>>
            <DO (I 0 .MAX)
                <COND (<==? <GET .PT .I> .V> <RTRUE>)>>
            <RFALSE>>

        ;"Attempts to locate a byte in a property table.
        
        Args:
          O: The object containing the property.
          P: The property number.
          V: The byte to locate. Must be 255 or lower.
        
        Returns:
          True if the byte is located, otherwise false."
        <ROUTINE IN-PBTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- <PTSIZE .PT> 1>>
            <DO (I 0 .MAX)
                <COND (<==? <GETB .PT .I> .V> <RTRUE>)>>
            <RFALSE>>)
    (EZIP
        ;"V4 only has the 3-argument (word) form of INTBL?"
        <ROUTINE IN-PWTBL? (O P V "AUX" PT LEN)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET LEN </ <PTSIZE .PT> 2>>
            <AND <INTBL? .V .PT .LEN> <RTRUE>>>

        <ROUTINE IN-PBTBL? (O P V "AUX" PT MAX)
            <OR <SET PT <GETPT .O .P>> <RFALSE>>
            <SET MAX <- <PTSIZE .PT> 1>>
            <DO (I 0 .MAX)
                <COND (<==? <GETB .PT .I> .V> <RTRUE>)>>
            <RFALSE>>)
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

<IF-DEBUG
    ;"Prints the contents of LEXBUF, calling DUMPWORD for each word."
    <ROUTINE DUMPLINE ("AUX" (WDS <GETB ,LEXBUF 1>) (P <+ ,LEXBUF 2>))
        <TELL N .WDS " words:">
        <DO (I 1 .WDS)
            <TELL " ">
            <DUMPWORD <GET .P 0>>
            <SET P <+ .P 4>>>
        <CRLF>>

    ;"Prints the raw contents of LEXBUF."
    <ROUTINE DUMPLEX ("AUX" (WDS <GETB ,LEXBUF 1>))
        ;<TELL N .WDS " words:">
        <DO (C 1 .WDS)
            <TELL N .C " of LEXBUF is " N <GET ,LEXBUF .C> CR>>>

    ;"Prints the raw contents of READBUF."
    <ROUTINE DUMPBUF ("AUX" (WDS <GETB ,READBUF 1>))
        ;<TELL N .WDS " words:">
        <DO (C 1 ,READBUF-SIZE)
            <TELL N .C " of READBUF is " N <GET ,READBUF .C> CR>>>

    ;"Prints a vocabulary word and its parts of speech."
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
              (ELSE <TELL "---">)>>>

;"Saves a copy of LEXBUF in BACKLEXBUF."
<ROUTINE COPY-LEXBUF ("AUX" C W (WDS <GETB ,LEXBUF 1>))
    <PUTB ,BACKLEXBUF 1 .WDS>
    <COPY-TABLE <REST ,LEXBUF 2> <REST ,BACKLEXBUF 2> <* 2 .WDS>>>

;"Restores LEXBUF from BACKLEXBUF."
<ROUTINE RESTORE-LEX ("AUX" C W (WDS <GETB ,BACKLEXBUF 1>))
    <PUTB ,LEXBUF 1 .WDS>
    <COPY-TABLE <REST ,BACKLEXBUF 2> <REST ,LEXBUF 2> <* 2 .WDS>>>

;"Saves a copy of READBUF in BACKREADBUF."
<ROUTINE COPY-READBUF ("AUX" C W)
    <COPY-TABLE ,READBUF ,BACKREADBUF %</ ,READBUF-SIZE 2>>>

;"Restores READBUF from BACKREADBUF."
<ROUTINE RESTORE-READBUF ("AUX" C W)
    <COPY-TABLE ,BACKREADBUF ,READBUF %</ ,READBUF-SIZE 2>>>

;"Fills READBUF and LEXBUF by reading a command from the player.
If ,AGAINCALL is true, no new command is read and the buffers are reused."
<ROUTINE READLINE ()
    ;"skip input if doing an AGAIN"
    <COND (,AGAINCALL <RETURN>)>
    <TELL CR "> ">
    <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
    ;"The read buffer has a slightly different format on V3."
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
    <COND (<DESCRIBE-ROOM ,HERE>
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
               <PUT ,TEMPTABLE .S <GET .TABL <+ .CNT .S>>>
               <SET S <+ .S 1>>
               ;<PROG () <TELL "IN LOOP: S IS NOW: "> <PRINTN .S> <TELL CR>>
               <COND (<G? <+ .S .CNT> .LENGTH> <RETURN>)>>
       ;<PROG () <TELL "S IS CURRENTLY: "> <PRINTN .S> <TELL CR>>
       <SET RND <- <RANDOM .S> 1>>
       ;<PROG () <TELL "RND IS CURRENTLY: "> <PRINTN .RND> <TELL CR>>
       <SET MSG <GET ,TEMPTABLE .RND>>
       <PUT .TABL <+ .CNT .RND> <GET .TABL .CNT>>
       <PUT .TABL .CNT .MSG>
       <SET CNT <+ 1 .CNT>>
       <COND (<G? .CNT .LENGTH> <SET CNT 2>)>
       <PUT .TABL 1 .CNT>
       <RETURN .MSG>>

<ROUTINE PICK-ONE-R (TABL "AUX" MSG RND)
      <SET RND <RANDOM <GET .TABL 0>>>
      <SET MSG <GET .TABL .RND>>
      <RETURN .MSG>>

<VERSION?
    (ZIP
        <DEFMAC INIT-STATUS-LINE () <>>
        <DEFMAC UPDATE-STATUS-LINE () '<USL>> )
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
            <CURSET 1 1>>)>

<ROUTINE JIGS-UP (TEXT "AUX" RESP (Y 0) R)
    <TELL .TEXT CR CR>
    <TELL "    ****  The game is over  ****" CR CR>
    ;"<TELL "    ****  You have died  ****" CR CR>"
    <IFFLAG (UNDO
             <PRINTI "Would you like to RESTART, UNDO, RESTORE, or QUIT? > ">)
            (ELSE
             <PRINTI "Would you like to RESTART, RESTORE or QUIT? > ">)>
    <REPEAT ()
        <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
        <VERSION? (ZIP <>) (ELSE <PUTB ,READBUF 1 0>)>
        <READ ,READBUF ,LEXBUF>
        <COND (<EQUAL? <GET ,LEXBUF 1> ,W?RESTART>
               <SET Y 1>
               <RETURN>)
              (<EQUAL? <GET ,LEXBUF 1> ,W?RESTORE>
               <SET Y 2>
               <RETURN>)
              (<EQUAL? <GET ,LEXBUF 1> ,W?QUIT>
               <SET Y 3>
               <RETURN>)
              (<EQUAL? <GET ,LEXBUF 1> ,W?UNDO>
               <SET Y 4>
               <RETURN>)
              (T
               <IFFLAG (UNDO
                        <TELL CR "(Please type RESTART, UNDO, RESTORE or QUIT) >">)
                       (ELSE
                        <TELL CR "(Please type RESTART, RESTORE or QUIT) > ">)>)>>
    ;"TODO: combine this with the REPEAT above"
    <COND (<=? .Y 1>
           <RESTART>)
          (<=? .Y 2>
           <SET R <RESTORE>>
           <COND (<NOT .R>
                  <TELL "Restore failed." CR>
                  <AGAIN>)>)
          (<=? .Y 3>
           <TELL CR "Thanks for playing." CR>
           <QUIT>)
          (<=? .Y 4>
           <SET R <V-UNDO>>
           <COND (<NOT .R>
                  <TELL "Undo failed." CR>
                  <AGAIN>)>)>>

<ROUTINE ROB (VICTIM "OPT" DEST "AUX" DEST-IS-PERSON)
    ;"TODO: use MAP-CONTENTS"
    <COND (<AND .DEST <FSET? .DEST ,PERSONBIT>>
           <SET DEST-IS-PERSON T>)>
    <MAP-CONTENTS (I N .VICTIM)
        <COND (<NOT .DEST-IS-PERSON> <FCLEAR .I ,WORNBIT>)>
        <COND (<NOT .DEST> <REMOVE .I>)
              (ELSE <MOVE .I .DEST>)>>>

<ROUTINE YES? ("AUX" RESP)
     <PRINTI " (y/n) >">
     <REPEAT ()
         <SET RESP <GETONECHAR>>
         <COND (<EQUAL? .RESP !\Y !\y>
                <RTRUE>)
               (<EQUAL? .RESP !\N !\n>
                <RFALSE>)
               (T
                ;<CRLF>
                <TELL "(Please type y or n) >" >)>>>

<VERSION?
    (ZIP
        <ROUTINE GETONECHAR ()
            <PUTB ,READBUF 0 %<- ,READBUF-SIZE 2>>
            <READ ,READBUF ,LEXBUF>
            <GETB ,READBUF 1>>)
    (ELSE
        <ROUTINE GETONECHAR ()
            ;"TODO: is BUFOUT doing anything useful here?"
            <BUFOUT <>>
            <BUFOUT T>
            <INPUT 1>>)>

;"TODO: should this check GENERIC-OBJECTS too?"
<ROUTINE VISIBLE? (OBJ "AUX" P M)
    <SET P <LOC .OBJ>>
    <SET M <META-LOC .OBJ>>
    <COND (<NOT <=? .M ,HERE>>
           <COND (<OR <AND <=? .P ,LOCAL-GLOBALS>
                           <GLOBAL-IN? .OBJ ,HERE>>
                      <=? .P ,GLOBAL-OBJECTS>>
                  <RTRUE>)
                 (ELSE <RFALSE>)>)>
    ;<TELL "The meta-loc = HERE and the LOC is " D .P CR>
    <REPEAT ()
        <COND (<EQUAL? .P ,HERE ,WINNER>
               ;<TELL D .P " is either = HERE or the player." CR>
               <RTRUE>)
              (<NOT <SEE-INSIDE? .P>>
               ;<TELL D .P " is a non-transparent container that is closed." CR>
               <RFALSE>)
              (ELSE <SET P <LOC .P>>)>>>

<ROUTINE ACCESSIBLE? (OBJ "AUX" L)
    ;"currently GLOBALs and LOCAL-GLOBALS return false since they are non-interactive scenery."
    <SET L <LOC .OBJ>>
    <COND (<NOT <=? <META-LOC .OBJ> ,HERE>>
           ;<TELL "Object not in room" CR>
           <RFALSE>)>
    <REPEAT ()
        ;<TELL "In accessible repeat loop, L is " D .L CR>
        <COND (<AND <FSET? .L ,CONTBIT>
                    <NOT <FSET? .L ,OPENBIT>>
                    <NOT <FSET? .L ,SURFACEBIT>>>
               ;<TELL D .L " is a closed container." CR>
               <RFALSE>)
              (<EQUAL? .L ,HERE ,WINNER>
               ;<TELL D .L " is either = HERE or the player." CR>
               <RTRUE>)
              (ELSE
               <SET L <LOC .L>>)>>>

<ROUTINE HELD? (OBJ "OPT" (HLDR <>))
    <OR .HLDR <SET HLDR ,WINNER>>
    <REPEAT ()
        <COND (<NOT .OBJ>
               <RFALSE>)
              (<=? <LOC .OBJ> .HLDR>
               <RTRUE>)
              (ELSE
               <SET OBJ <LOC .OBJ>>)>>>

<ROUTINE META-LOC ML (OBJ "AUX" P)
    <SET P <LOC .OBJ>>
    <COND (<IN? .P ,ROOMS>
           <RETURN .P>)>
    <REPEAT ()
        ;<TELL "In META-LOC repeat -- P is " D .P CR>
        ;"TODO: infinite loop if P is not a person/container/special object?"
        <COND (<OR <FSET? .P ,PERSONBIT>
                   <FSET? .P ,CONTBIT>
                   <EQUAL? .P ,LOCAL-GLOBALS ,GLOBAL-OBJECTS ,GENERIC-OBJECTS>>
               <SET P <LOC .P>>)>
        <COND (<IN? .P ,ROOMS>
               <RETURN .P .ML>)
              (<NOT .P>
               <RFALSE>)>>>

<ROUTINE NOW-DARK ()
    <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
                <NOT <SEARCH-FOR-LIGHT>>>
           <TELL "You are plunged into darkness." CR>)>>

<ROUTINE NOW-LIT ()
    <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
                <NOT <SEARCH-FOR-LIGHT>>>
           <TELL "You can see your surroundings now." CR CR>
           <SETG NLITSET T>
           <V-LOOK>)>>

<INSERT-FILE "events">

<INSERT-FILE "verbs">

"Objects"

;"This has all the flags, just in case other objects don't define them."
<OBJECT ROOMS
    (FLAGS PERSONBIT TOUCHBIT TAKEBIT WEARBIT INVISIBLE WORNBIT LIGHTBIT
           LOCKEDBIT SURFACEBIT CONTBIT NDESCBIT VOWELBIT NARTICLEBIT OPENBIT
           OPENABLEBIT READBIT DEVICEBIT ONBIT EDIBLEBIT TRANSBIT FEMALEBIT
           PLURALBIT)>

;"This has any special properties, just in case other objects don't define them."
;"I guess all properties should go on this dummy object, just to be safe?"
<OBJECT NULLTHANG
    (SIZE 5)
    (ADJECTIVE NULLTHANG)
    (LDESC <>)
    (FDESC <>)
    (GLOBAL NULLTHANG)
    (TEXT <>)
    (CONTFCN <>)
    (DESCFCN <>)
    (TEXT-HELD <>)
    (CAPACITY 10)>

<PROPDEF SIZE 5>

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
    <COND (<N==? ,PLAYER ,PRSO>
           <RFALSE>)
          (<VERB? EXAMINE>
           <PRINTR "You look like you're up for an adventure.">)
          (<VERB? TAKE>
           <PRINTR "That couldn't possibly work.">)>>
