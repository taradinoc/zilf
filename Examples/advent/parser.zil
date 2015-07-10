"Library header"

<SETG ZILLIB-VERSION "J2+">

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
<GLOBAL MODE 1>
<GLOBAL HERE-LIT T>
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
        <DEFMAC PUT/B ('T 'O 'V) <FORM PUTB .T .O .V>>
        <DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PBTBL? .O .P .V>>)
    (ELSE
        <DEFMAC GET/B ('T 'O) <FORM GET .T .O>>
        <DEFMAC PUT/B ('T 'O 'V) <FORM PUT .T .O .V>>
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

<DEFMAC PRSO? ("ARGS" A)
    <FORM EQUAL? ',PRSO !.A>>

<DEFMAC PRSI? ("ARGS" A)
    <FORM EQUAL? ',PRSI !.A>>

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
<GLOBAL P-V-WORD <>>
<GLOBAL P-V-WORDN 0>
<GLOBAL P-NOBJ 0>
<GLOBAL P-P1 <>>
<GLOBAL P-P2 <>>
<GLOBAL P-SYNTAX <>>


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
    <SETG HERE <LOC ,WINNER>>
    <SETG HERE-LIT <SEARCH-FOR-LIGHT>>
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
               <SETG P-V-WORD .W>
               <SETG P-V-WORDN .I>
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
    ;"Save command for AGAIN"
    <COND (<NOT <VERB? AGAIN>>
           <COPY-READBUF>
           <COPY-LEXBUF>)>
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
    ;"T? forces the OR to be evaluated as a condition, since we don't
      care about the exact return value from CHKWORD? or WORD?."
    <T? <OR <EQUAL? .W ,W?A ,W?AN ,W?THE>
            <CHKWORD? .W ,PS?ADJECTIVE>
            <WORD? .W OBJECT>>>>

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

<CONSTANT SF-HAVE 2>
<CONSTANT SF-MANY 4>
<CONSTANT SF-TAKE 8>
<CONSTANT SF-ON-GROUND 16>
<CONSTANT SF-IN-ROOM 32>
<CONSTANT SF-CARRIED 64>
<CONSTANT SF-HELD 128>

;"Attempts to match a syntax line for the current verb.

Uses:
  P-V
  P-NOBJ
  P-P1
  P-P2

Sets:
  PRSA
  P-SYNTAX

Returns:
  True if a syntax line was matched."
<ROUTINE MATCH-SYNTAX ("AUX" PTR CNT NOBJ PREP1 PREP2)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <IF-DEBUG <TELL "[MATCH-SYNTAX: " N .CNT " syntaxes at " N .PTR "]" CR>>
    <REPEAT ()
        ;<TELL "CNT is currently " N .CNT CR>
        ;<BIND ((TEST-PTR <GET .PTR ,SYN-NOBJ>) (TEST-P-NOBJ ,P-NOBJ))
            <TELL "<GETB .PTR ,SYN-NOBJ> is " N .TEST-PTR "and P-NOBJ is " N .TEST-P-NOBJ CR>>
        <COND (<DLESS? CNT 0>
               ;"Out of syntax lines"
               <RETURN>)>
        <SET NOBJ <GETB .PTR ,SYN-NOBJ>>
        <SET PREP1 <GETB .PTR ,SYN-PREP1>>
        <SET PREP2 <GETB .PTR ,SYN-PREP2>>
        <COND (<AND <==? .NOBJ ,P-NOBJ>
                    <OR <L? ,P-NOBJ 1>
                        <==? .PREP1 ,P-P1>>
                    <OR <L? ,P-NOBJ 2>
                        <==? .PREP2 ,P-P2>>>
               ;"Complete match"
               <SETG PRSA <GETB .PTR ,SYN-ACTION>>
               <SETG P-SYNTAX .PTR>
               <RTRUE>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>
    ;"No complete match, look for something close"
    <SET PTR <GUESS-SYNTAX>>
    <COND (.PTR
           <SETG PRSA <GETB .PTR ,SYN-ACTION>>
           <SETG P-SYNTAX .PTR>
           <RTRUE>)
          (ELSE
           <TELL "I don't understand that sentence." CR>
           <RFALSE>)>>

;"Find the best incompletely-matching syntax line, given that no line matches
  exactly."
<ROUTINE GUESS-SYNTAX ("AUX" PTR CNT BEST BEST-SCORE S NOBJ PREP1 PREP2)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <REPEAT ()
        <COND (<DLESS? CNT 0>
               ;"Out of syntax lines"
               <RETURN>)>
        <SET NOBJ <GETB .PTR ,SYN-NOBJ>>
        <SET PREP1 <GETB .PTR ,SYN-PREP1>>
        <SET PREP2 <GETB .PTR ,SYN-PREP2>>
        <PROG ()
            ;"The syntax line has to have more objects than we've parsed"
            <COND (<L=? .NOBJ ,P-NOBJ> <RETURN>)>
            ;"See how much we can match..."
            <SET S 1>
            <COND (,P-P1
                   <COND (<N==? .PREP1 ,P-P1>
                          ;"Wrong preposition 1"
                          <RETURN>)
                         (ELSE <SET S <+ .S 1>>)>)
                  (<AND <G=? ,P-NOBJ 1> .PREP1>
                   ;"Missing preposition 1"
                   <RETURN>)
                  (<0? .PREP1> <SET S <+ .S 1>>)>
            <COND (,P-P2
                   <COND (<N==? .PREP2 ,P-P2>
                          ;"Wrong preposition 2"
                          <RETURN>)
                         (ELSE <SET S <+ .S 1>>)>)
                  (<AND <G=? ,P-NOBJ 2> .PREP2>
                   ;"Missing preposition 2 - shouldn't get here?"
                   <RETURN>)
                  (<0? .PREP2> <SET S <+ .S 1>>)>
            <COND (<G? .S .BEST-SCORE>
                   <SET BEST-SCORE .S>
                   <SET BEST .PTR>)>>
        ;"Advance"
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>
    .BEST>

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

;"Attempts to match PRSO and PRSI, if necessary, after parsing a command.
  Prints a message if it fails.

Uses:
  P-NOBJ
  P-DOBJS
  P-SYNTAX

Sets:
  PRSO
  PRSI
  IT-USE
  THEM-USE
  HIM-USE
  HER-USE

Returns:
  True if all required objects were found, or false if not."
<ROUTINE FIND-OBJECTS ("AUX" X F O (SNOBJ <GETB ,P-SYNTAX ,SYN-NOBJ>))
    <COND (<L? .SNOBJ 1>
           <SETG PRSO <>>)
          (ELSE
           <SET F <GETB ,P-SYNTAX ,SYN-FIND1>>
           <SET O <GETB ,P-SYNTAX ,SYN-OPTS1>>
           <COND (<L? ,P-NOBJ 1>
                  <SETG PRSO
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP1>>>
                  <COND (<0? ,PRSO>
                         <WHAT-DO-YOU-WANT>
                         <RFALSE>)>)
                 (ELSE
                  <SET X <GET ,P-DOBJS 2>>
                  ;<TELL "Find objects PRSO test - X is " N .X CR>
                  <FIND-OBJECTS-CHECK-PRONOUN .X IT DOBJ DOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X THEM TOBJ DOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X HIM MOBJ DOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X HER FOBJ DOBJ>
                  <SETG PRSO <FIND-ONE-OBJ ,P-DOBJS ,P-DOBJEX
                                           <ENCODE-NOUN-BITS .F .O>>>)>
           <COND (<OR <NOT ,PRSO>
                      <AND <NOT ,PRSO-DIR> <NOT <HAVE-TAKE-CHECK ,PRSO .O>>>>
                  <RFALSE>)>)>
    <COND (<L? .SNOBJ 2>
           <SETG PRSI <>>)
          (ELSE
           <SET F <GETB ,P-SYNTAX ,SYN-FIND2>>
           <SET O <GETB ,P-SYNTAX ,SYN-OPTS2>>
           <COND (<L? ,P-NOBJ 2>
                  <SETG PRSI
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP2>>>
                  <COND (<0? ,PRSI>
                         <WHAT-DO-YOU-WANT>
                         <RFALSE>)>)
                 (ELSE
                  <SET X <GET ,P-IOBJS 2>>
                  ;<TELL "Find objects PRSI test - X is " N .X CR>
                  <FIND-OBJECTS-CHECK-PRONOUN .X IT DOBJ IOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X THEM TOBJ IOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X HIM MOBJ IOBJ>
                  <FIND-OBJECTS-CHECK-PRONOUN .X HER FOBJ IOBJ>
                  <SETG PRSI <FIND-ONE-OBJ ,P-IOBJS ,P-IOBJEX
                                           <ENCODE-NOUN-BITS .F .O>>>)>
           <COND (<OR <NOT ,PRSI> <NOT <HAVE-TAKE-CHECK ,PRSI .O>>>
                  <RFALSE>)>)>
    <RTRUE>>

<ROUTINE WHAT-DO-YOU-WANT ("AUX" SN SP1 SP2)
    <SET SN <GETB ,P-SYNTAX ,SYN-NOBJ>>
    <SET SP1 <GETB ,P-SYNTAX ,SYN-PREP1>>
    <SET SP2 <GETB ,P-SYNTAX ,SYN-PREP2>>
    ;"TODO: implement orphaning so we can handle the response to this question"
    ;"TODO: use LONG-WORDS table for preposition words"
    <TELL "What do you want to ">
    <PRINT-WORD ,P-V-WORDN>
    <COND (.SP1
           <TELL " " B <GET-PREP-WORD .SP1>>)>
    <COND (<AND ,PRSO <NOT ,PRSO-DIR>>
           <TELL " " T ,PRSO>
           <COND (.SP2
                  <TELL " " B <GET-PREP-WORD .SP2>>)>)>
    <TELL "?" CR>>

;"Applies the rules for the HAVE and TAKE syntax flags to a parsed object,
printing a failure message if appropriate.

Args:
  OBJ: An object matched as one of the nouns in a command.
  OPTS: The corresponding search options, including the HAVE and TAKE flags.

Returns:
  True if the checks passed, i.e. either the object doesn't have to be held
  by the WINNER, or it is held, possibly as the result of an implicit TAKE.
  False if the object has to be held, the WINNER is not holding it, and
  it couldn't be taken implicitly."
<ROUTINE HAVE-TAKE-CHECK (OBJ OPTS)
    ;"Attempt implicit take if WINNER isn't directly holding the object"
    <COND (<BTST .OPTS ,SF-TAKE>
           ;"TODO: Don't implicit take out of a closed container? Or should
             the container handle this by setting TRYTAKEBIT on its contents?"
           ;"TODO: Enforce inventory limit; split relevant logic out of V-TAKE."
           <COND (<AND <NOT <IN? .OBJ ,WINNER>>
                       <FSET? .OBJ ,TAKEBIT>
                       <NOT <FSET? .OBJ ,TRYTAKEBIT>>>
                  <TELL "[taking " T .OBJ "]" CR>
                  <MOVE .OBJ ,WINNER>)>)>
    ;"WINNER must (indirectly) hold the object if SF-HAVE is set"
    <COND (<AND <BTST .OPTS ,SF-HAVE> <NOT <HELD? .OBJ>>>
           <TELL "You aren't holding " T .OBJ "." CR>
           <RFALSE>)>
    <RTRUE>>

;"Searches scope for a single object with the given flag set, and prints an
inference message before returning it.

The flag KLUDGEBIT is a special case that always finds ROOMS.

Args:
  BIT: The flag to search for.
  OPTS: The search options to use. (Currently ignored.)
  PREP: The preposition to use in the message.

Returns:
  The single object that matches, or false if zero or multiple objects match."
<ROUTINE GWIM (BIT OPTS PREP "AUX" O PW)
    ;"Special case"
    <COND (<==? .BIT ,KLUDGEBIT>
           <RETURN ,ROOMS>)>
    ;"Look for exactly one matching object"
    <MAP-SCOPE (I)
        <COND (<FSET? .I .BIT>
               <COND (.O <RFALSE>)
                     (ELSE <SET O .I>)>)>>
    ;"Print inference message"
    <COND (.O
           <TELL "[">
           ;"TODO: use LONG-WORDS table for preposition word"
           <COND (<SET PW <GET-PREP-WORD .PREP>>
                  <TELL B .PW " ">)>
           <TELL T .O "]" CR>
           <RETURN .O>)
          (ELSE <RFALSE>)>>

<ROUTINE GET-PREP-WORD GPW (PREP "AUX" MAX)
    <SET MAX <- <* <GET ,PREPOSITIONS 0> 2> 1>>
    <DO (I 1 .MAX 2)
        <COND (<==? <GET ,PREPOSITIONS <+ .I 1>> .PREP>
               <RETURN <GET ,PREPOSITIONS .I> .GPW>)>>
    <RFALSE>>

<DEFMAC ENCODE-NOUN-BITS ('F 'O)
    <FORM BOR .F <FORM * .O 256>>>

<DEFMAC DECODE-FINDBIT ('E)
    <FORM BAND <FORM / .E 256> 255>>

;"Searches scope for a usable light source.

Returns:
  An object providing light, or false if no light source was found."
<ROUTINE SEARCH-FOR-LIGHT SFL ()
    <COND (<FSET? ,HERE ,LIGHTBIT> <RTRUE>)>
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
    ;"The T? should be unnecessary, but ZILF generates ugly code without it"
    <T? <OR ;"We can always see the contents of surfaces"
            <FSET? .OBJ ,SURFACEBIT>
            ;"We can see inside containers if they're open, transparent, or
              unopenable (= always-open)"
            <AND <FSET? .OBJ ,CONTBIT>
                 <OR <FSET? .OBJ ,OPENBIT>
                     <FSET? .OBJ ,TRANSBIT>
                     <NOT <FSET? .OBJ ,OPENABLEBIT>>>>>>>

;"Attempts to find an object in scope, given the adjectives and nouns that
describe it.

Args:
  YTBL: A table of adjective/noun pairs that the object must have ('yes').
  NTBL: A table of adjective/noun pairs that the object must not have ('no').
  BITS: A word with the scope flags in the lower byte and the number of the
    FIND flag in the upper byte.

Returns:
  The located object, or false if no matching object was found."
<ROUTINE FIND-ONE-OBJ FOO (YTBL NTBL BITS "AUX" A N)
    ;"TODO: Disambiguate and/or match multiple objects."
    <SET A <GET .YTBL 1>>
    <SET N <GET .YTBL 2>>
    <MAP-SCOPE (I)
        <COND (<AND <NOT <FSET? .I ,INVISIBLE>> <REFERS? .A .N .I>>
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
    <AND <NOT <FSET? .O ,INVISIBLE>> <IN-PB/WTBL? .R ,P?GLOBAL .O>>>

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
If ,AGAINCALL is true, no new command is read and the buffers are reused.

Uses:
  AGAINCALL

Sets (contents):
  READBUF
  LEXBUF"
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

;"Invokes the handlers for a given action (and objects).

Uses:
  WINNER

Sets (temporarily):
  PRSA
  PRSO
  PRSO-DIR
  PRSI"
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

;"Helper function to call action handlers, respecting a search order.

The routine searches for handlers in a set order, calling each one it finds
until one returns true to indicate that it has handled the action.

The search order is as follows:
  ACTION property of WINNER
  ACTION property of WINNER's location (with M-BEG parameter)
  Verb preaction
  CONTFCN property of PRSI's location
  ACTION property of PRSI
  CONTFCN property of PRSO's location
  ACTION property of PRSO
  Verb action

Uses:
  WINNER
  PRSO
  PRSO-DIR
  PRSI

Args:
  PRTN: The verb preaction routine, or false for no preaction.
  RTN: The verb action routine.

Returns:
  True if the action was handled."
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

;"Moves the player to a new location, notifies the location that the player
has entered, and prints an appropriate room introduction.

If the old and/or new location is dark, DARKNESS-F will be given a chance to
print the room introduction before DESCRIBE-ROOM and DESCRIBE-OBJECTS.

Uses:
  WINNER

Sets:
  HERE

Args:
  RM: The room to move into."
<ROUTINE GOTO (RM "AUX" WAS-LIT F)
    <SET WAS-LIT ,HERE-LIT>
    <SETG HERE .RM>
    <MOVE ,WINNER ,HERE>
    <APPLY <GETP .RM ,P?ACTION> ,M-ENTER>
    ;"Call SEARCH-FOR-LIGHT down here in case M-ENTER adjusts the light."
    <SETG HERE-LIT <SEARCH-FOR-LIGHT>>
    ;"moved descriptors into GOTO so they'll be called when you call GOTO from a PER routine, etc"
    <COND (<NOT .WAS-LIT>
           <COND (,HERE-LIT
                  <SET F <DARKNESS-F ,M-DARK-TO-LIT>>)
                 (<OR <DARKNESS-F ,M-DARK-TO-DARK>
                      <DARKNESS-F ,M-LOOK>>
                  <SET F T>)>)
          (,HERE-LIT)
          (<OR <DARKNESS-F ,M-LIT-TO-DARK>
               <DARKNESS-F ,M-LOOK>>
           <SET F T>)>
    <COND (<AND <NOT .F> <DESCRIBE-ROOM ,HERE>>
           <DESCRIBE-OBJECTS ,HERE>)>
    <FSET ,HERE ,TOUCHBIT>>

"Misc Routines"

;"Searches an object to find exactly one child with a given flag set, and
optionally prints a message about it.

Args:
  C: The container or location to search.
  BIT: The flag to look for.
  WORD: A string to print in a message describing the found object,
    e.g. 'with' to print '[with the purple key]'. If omitted, no message
    will be shown.

Returns:
  If exactly one object was found, returns the found object.
  If zero or multiple objects were found, returns false."
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

;"Prints a string with italics for emphasis (if supported).

Args:
  STR: The string to emphasize."
<ROUTINE ITALICIZE (STR "AUX" A)
    <VERSION? (ZIP)
              (T <HLIGHT ,H-ITALIC>)>
    <TELL .STR>
    <VERSION? (ZIP)
              (T <HLIGHT ,H-NORMAL>)>>

;"Returns a random element from a table, not repeating until every element
has been returned once.

Args:
  TABL: The table, which should be an LTABLE with word elements. The first
    element of the table (after the length word) is used as a counter and
    must be 2 initially.

Returns:
  A random element from the table."
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

;"Returns a random element from a table, possibly repeating.

Args:
  TABL: The table, which should be an LTABLE with word elements.

Returns:
  A random element from the table."
<ROUTINE PICK-ONE-R (TABL "AUX" MSG RND)
    <SET RND <RANDOM <GET .TABL 0>>>
    <SET MSG <GET .TABL .RND>>
    <RETURN .MSG>>

<VERSION?
    (ZIP
        <DEFMAC INIT-STATUS-LINE () <>>
        <DEFMAC UPDATE-STATUS-LINE () '<USL>> )
    (T
        ;"Splits the screen and clears a 1-line status line."
        <ROUTINE INIT-STATUS-LINE ()
            <SPLIT 1>
            <CLEAR 1>>

        ;"Writes the location name, score, and turn count in the status line.
        
        Uses:
          HERE
          SCORE
          TURNS"
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

        ;"Fills the top row with spaces."
        <ROUTINE FAKE-ERASE ("AUX" CNT WIDTH)
            <CURSET 1 1>
            <DO (I <LOWCORE SCRH> 1 -1) <PRINTC !\ >>
            <CURSET 1 1>>)>

;"Prints a message and ends the game, prompting the player to restart,
(possibly) undo, restore, or quit.

Args:
  TEXT: The message to print before the 'game is over' banner.

Returns:
  True if RESURRECT? indicated that the game should resume.
  Otherwise, never returns."
<ROUTINE JIGS-UP (TEXT "AUX" RESP W)
    <TELL .TEXT CR CR>
    <PRINT-GAME-OVER>
    <CRLF>
    <COND (<RESURRECT?> <RTRUE>)>
    <REPEAT PROMPT ()
        <IFFLAG (UNDO
                 <PRINTI "Would you like to RESTART, UNDO, RESTORE, or QUIT? > ">)
                (ELSE
                 <PRINTI "Would you like to RESTART, RESTORE or QUIT? > ">)>
        <REPEAT ()
            <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
            <VERSION? (ZIP <>) (ELSE <PUTB ,READBUF 1 0>)>
            <READ ,READBUF ,LEXBUF>
            <SET W <AND <GETB ,LEXBUF 1> <GET ,LEXBUF 1>>>
            <COND (<EQUAL? .W ,W?RESTART>
                   <RESTART>)
                  (<EQUAL? .W ,W?RESTORE>
                   <RESTORE>  ;"only returns on failure"
                   <TELL "Restore failed." CR>
                   <AGAIN .PROMPT>)
                  (<EQUAL? .W ,W?QUIT>
                   <TELL CR "Thanks for playing." CR>
                   <QUIT>)
                  (<EQUAL? .W ,W?UNDO>
                   <V-UNDO>   ;"only returns on failure"
                   <TELL "Undo failed." CR>
                   <AGAIN .PROMPT>)
                  (T
                   <IFFLAG (UNDO
                            <TELL CR "(Please type RESTART, UNDO, RESTORE or QUIT) >">)
                           (ELSE
                            <TELL CR "(Please type RESTART, RESTORE or QUIT) > ">)>)>>>>

<DEFAULT-DEFINITION PRINT-GAME-OVER
    ;"Prints a message explaining that the game is over or the player has died.
      This is called after JIGS-UP has already printed the message passed in to
      describe the specific circumstances, so usually this should print a generic
      message appropriate for the game's theme."
    <ROUTINE PRINT-GAME-OVER ()
        <TELL "    ****  The game is over  ****" CR>>
>

<DEFAULT-DEFINITION RESURRECT?
    ;"Optionally gives the player a chance to resume the game after JIGS-UP.
    
    Returns:
      True if JIGS-UP should return to its caller; the function should change
      the game state as needed for this to make sense. False if JIGS-UP should
      prompt the player to RESTART/UNDO/RESTORE/QUIT and never return."
    <DEFMAC RESURRECT? () <>>
>

;"Empties the contents of one object into another, or removes them from play.

The WORNBIT flag will also be cleared on the objects, unless the destination
is a person.

Args:
  VICTIM: The object that will be emptied.
  DEST: The object where the contents will be placed. If omitted or false,
    the contents will be removed from play instead."
<ROUTINE ROB (VICTIM "OPT" DEST "AUX" DEST-IS-PERSON)
    <COND (<AND .DEST <FSET? .DEST ,PERSONBIT>>
           <SET DEST-IS-PERSON T>)>
    <MAP-CONTENTS (I N .VICTIM)
        <COND (<NOT .DEST-IS-PERSON> <FCLEAR .I ,WORNBIT>)>
        <COND (<NOT .DEST> <REMOVE .I>)
              (ELSE <MOVE .I .DEST>)>>>

;"Prompts the player to answer a yes/no question by pressing 'y' or 'n',
repeating the prompt if they press any other key.

The question should be printed before calling this routine.

Returns:
  True if the user pressed 'y', false if they pressed 'n'."
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
        ;"Reads one character from the user.
        
        In V3, this uses line input since there is no character input. Only
        the first character entered is used.

        Sets (contents):
          READBUF
          LEXBUF
        
        Returns:
          The ZSCII code of the character entered."
        <ROUTINE GETONECHAR ()
            <PUTB ,READBUF 0 %<- ,READBUF-SIZE 2>>
            <READ ,READBUF ,LEXBUF>
            <GETB ,READBUF 1>>)
    (ELSE
        <ROUTINE GETONECHAR ()
            <INPUT 1>>)>

;"Determines whether an object can be seen by the player.

Uses:
  HERE
  WINNER

Args:
  OBJ: The object to check.

Returns:
  True if the object is visible, otherwise false."
<ROUTINE VISIBLE? (OBJ "AUX" P M)
    ;"TODO: should this check GENERIC-OBJECTS too?"
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

;"Determines whether an object can be touched by the player.

Uses:
  HERE
  WINNER

Args:
  OBJ: The object to check.

Returns:
  True if the object is accessible, otherwise false."
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

;"Determines whether an object is contained by another object (or the player).

Uses:
  WINNER

Args:
  OBJ: The object to check.
  HLDR: The container to check. If omitted or false, defaults to WINNER.

Returns:
  True if OBJ is contained by HLDR, otherwise false."
<ROUTINE HELD? (OBJ "OPT" (HLDR <>))
    <OR .HLDR <SET HLDR ,WINNER>>
    <REPEAT ()
        <COND (<NOT .OBJ>
               <RFALSE>)
              (<=? <LOC .OBJ> .HLDR>
               <RTRUE>)
              (ELSE
               <SET OBJ <LOC .OBJ>>)>>>

;"Finds the room that ultimately contains a given object.

Args:
  OBJ: The object.

Returns:
  The room that encloses the object, or false if it isn't in a room."
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

;"Checks whether the player has entered darkness, printing a message if so.

This should be called when the player has done something that might cause
a light source to go away."
<ROUTINE NOW-DARK? ()
    <COND (<AND ,HERE-LIT
                <NOT <SEARCH-FOR-LIGHT>>>
           <SETG HERE-LIT <>>
           <DARKNESS-F ,M-NOW-DARK>)>>

;"Checks whether the player is no longer in darkness, printing a message if so.

This should be called when the player has done something that might activate
or reveal a light source."
<ROUTINE NOW-LIT? ()
    <COND (<AND <NOT ,HERE-LIT>
                <SEARCH-FOR-LIGHT>>
           <SETG HERE-LIT T>
           <OR <DARKNESS-F ,M-NOW-LIT> <V-LOOK>>)>>

<INSERT-FILE "events">

<INSERT-FILE "verbs">

"Objects"

;"This has all the flags, just in case other objects don't define them."
<OBJECT ROOMS
    (FLAGS PERSONBIT TOUCHBIT TAKEBIT WEARBIT INVISIBLE WORNBIT LIGHTBIT
           LOCKEDBIT SURFACEBIT CONTBIT NDESCBIT VOWELBIT NARTICLEBIT OPENBIT
           OPENABLEBIT READBIT DEVICEBIT ONBIT EDIBLEBIT TRANSBIT FEMALEBIT
           PLURALBIT KLUDGEBIT DOORBIT)>

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
<PROPDEF CAPACITY -1>

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
    (FLAGS NARTICLEBIT PLURALBIT PERSONBIT TOUCHBIT)
    (CAPACITY -1)
    (ACTION PLAYER-R)>

;"Action handler for the player."
<ROUTINE PLAYER-R ()
    <COND (<N==? ,PLAYER ,PRSO>
           <RFALSE>)
          (<VERB? EXAMINE>
           <PRINTR "You look like you're up for an adventure.">)>>
