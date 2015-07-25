"Library header"

<SETG ZILLIB-VERSION "J2+">

<VERSION?
    (ZIP)
    (EZIP)
    (ELSE <ZIP-OPTIONS UNDO COLOR>)>

;"Filled in by compiler"
<CONSTANT LAST-OBJECT <>>

"Debugging"

;"Enables trace messages"
<COMPILATION-FLAG-DEFAULT DEBUG <>>
;"Enables XTREE, XGOTO, etc."
<COMPILATION-FLAG-DEFAULT DEBUGGING-VERBS <>>

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

"Property and flag defaults"

<COND (<NOT <GASSIGNED? EXTRA-FLAGS>> <SETG EXTRA-FLAGS '()>)>

;"These are all set on ROOMS in case no game objects define them."
;"TODO: Eliminate some standard flags or make them optional.
  27 flags in the library only leaves 5 for V3 games."
<SETG KNOWN-FLAGS
    (ATTACKBIT CONTBIT DEVICEBIT DOORBIT EDIBLEBIT FEMALEBIT INVISIBLE KLUDGEBIT
     LIGHTBIT LOCKEDBIT NARTICLEBIT NDESCBIT ONBIT OPENABLEBIT OPENBIT PERSONBIT
     PLURALBIT READBIT SURFACEBIT TAKEBIT TOOLBIT TOUCHBIT TRANSBIT TRYTAKEBIT
     VOWELBIT WEARBIT WORNBIT
     !,EXTRA-FLAGS)>

;"Default property values. Even properties with uninteresting defaults are
  listed here in case no game objects define them."
<PROPDEF SIZE 5>
<PROPDEF ADJECTIVE <>>
<PROPDEF LDESC <>>
<PROPDEF FDESC <>>
<PROPDEF GLOBAL <>>
<PROPDEF TEXT <>>
<PROPDEF CONTFCN <>>
<PROPDEF DESCFCN <>>
<PROPDEF TEXT-HELD <>>
<PROPDEF CAPACITY -1>
<PROPDEF ARTICLE <>>

"Parser"

<CONSTANT READBUF-SIZE 100>
<CONSTANT READBUF <ITABLE NONE ,READBUF-SIZE (BYTE)>>

<CONSTANT LEXBUF-SIZE 59>
<CONSTANT LEXBUF <ITABLE ,LEXBUF-SIZE (LEXV) 0 #BYTE 0 #BYTE 0>>

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

"Constants"

<CONSTANT L-ISARE 1>
<CONSTANT L-SUFFIX 2>
<CONSTANT L-ISMANY 4>
<CONSTANT L-PRSTABLE 8>
<CONSTANT L-THE 16>
<CONSTANT L-OR 32>
<CONSTANT L-CAP 64>

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
    ;<IF-DEBUG <TELL "[CHKWORD: " B .W " PS=" N .PS " P1=" N .P1>>
    <SET F <GETB .W ,VOCAB-FL>>
    ;<IF-DEBUG <TELL " F=" N .F>>
    <SET F <COND (<BTST .F .PS>
                  <COND (<L? .P1 0>
                         ;<IF-DEBUG <TELL " OK!]" CR>>
                         <RTRUE>)
                        (<==? <BAND .F ,P1MASK> .P1>
                         <GETB .W ,VOCAB-V1>)
                        (ELSE <GETB .W ,VOCAB-V2>)>)>>
    ;<IF-DEBUG <TELL " = " N .F "]" CR>>
    .F>

;"Gets the word at the given index in LEXBUF.

Args:
  N: The index, starting at 1.

Returns:
  A pointer to the vocab word, or 0 if the word at the given index was
  unrecognized."
<ROUTINE GETWORD? (N "AUX" R)
    <SET R <GET ,LEXBUF <- <* .N 2> 1>>>
    ;<IF-DEBUG
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
<GLOBAL P-V-WORDN 0>    ;"0 = invalid, e.g. after orphaning; use P-V-WORD instead"
<GLOBAL P-NOBJ 0>
<GLOBAL P-P1 <>>
<GLOBAL P-P2 <>>
<GLOBAL P-SYNTAX <>>

"Structured types for storing noun phrases.

 NOUN-PHRASE:
   NP-YTBL and NP-NTBL are tables of OBJSPECs, with the corresponding counts
   in NP-YCNT and NP-NCNT.
   NP-MODE is a mode byte: either 0, MCM-ALL, or MCM-ANY.
   The helper macros NP-YSPEC and NP-NSPEC return an OBJSPEC by 1-based index.

 OBJSPEC:
   OBJSPEC-ADJ contains an adjective (number or voc word, depending on version)
   or special flag (TBD).
   OBJSPEC-NOUN contains a noun (voc word).
   Either field may be 0, but not both."
<DEFSTRUCT NOUN-PHRASE (TABLE ('NTH GETB) ('PUT PUTB) ('START-OFFSET 0))
    (NP-YCNT FIX)
    (NP-NCNT FIX)
    (NP-YTBL TABLE 'OFFSET 1 'NTH ZGET 'PUT ZPUT)
    (NP-NTBL TABLE 'OFFSET 2 'NTH ZGET 'PUT ZPUT)
    (NP-MODE FIX 'OFFSET 6)>

<CONSTANT P-MAX-OBJSPECS 10>
<DEFINE NOUN-PHRASE ()
    <MAKE-NOUN-PHRASE
        'NOUN-PHRASE <TABLE <BYTE 0> <BYTE 0> 0 0 <BYTE 0>>
        'NP-YTBL <ITABLE <* 2 ,P-MAX-OBJSPECS>>
        'NP-NTBL <ITABLE <* 2 ,P-MAX-OBJSPECS>>>>

<CONSTANT P-OBJSPEC-SIZE 4>

<DEFMAC NP-YSPEC ('NP 'I)
    <COND (<==? .I 1>
           <FORM NP-YTBL .NP>)
          (<TYPE? .I FIX>
           <FORM REST <FORM NP-YTBL .NP> <* ,P-OBJSPEC-SIZE <- .I 1>>>)
          (ELSE
           <FORM REST <FORM NP-YTBL .NP> <FORM * ,P-OBJSPEC-SIZE <FORM - .I 1>>>)>>

<DEFMAC NP-NSPEC ('NP 'I)
    <COND (<==? .I 1>
           <FORM NP-NTBL .NP>)
          (<TYPE? .I FIX>
           <FORM REST <FORM NP-NTBL .NP> <* ,P-OBJSPEC-SIZE <- .I 1>>>)
          (ELSE
           <FORM REST <FORM NP-NTBL .NP> <FORM * ,P-OBJSPEC-SIZE <FORM - .I 1>>>)>>

<DEFSTRUCT OBJSPEC (TABLE ('NTH ZGET) ('PUT ZPUT) ('START-OFFSET 0))
    (OBJSPEC-ADJ VOC)
    (OBJSPEC-NOUN VOC)>

;"Resets a noun phrase to be empty with no mode."
<ROUTINE CLEAR-NOUN-PHRASE (NP)
    <NP-YCNT .NP 0>
    <NP-NCNT .NP 0>
    <NP-MODE .NP 0>>

;"Copies the contents of one noun phrase into another."
<ROUTINE COPY-NOUN-PHRASE (SRC DEST "AUX" C)
    <NP-YCNT .DEST <SET C <NP-YCNT .SRC>>>
    <COPY-TABLE-B <NP-YTBL .SRC> <NP-YTBL .DEST> <* ,P-OBJSPEC-SIZE .C>>
    <NP-NCNT .DEST <SET C <NP-NCNT .SRC>>>
    <COPY-TABLE-B <NP-NTBL .SRC> <NP-NTBL .DEST> <* ,P-OBJSPEC-SIZE .C>>
    <NP-MODE .DEST <NP-MODE .SRC>>>

<IF-DEBUG
    ;"Prints the meaning of a noun phrase."
    <ROUTINE PRINT-NOUN-PHRASE (NP "AUX" CNT F S A N)
        ;"Mode"
        <SET F <NP-MODE .NP>>
        <COND (<=? .F ,MCM-ALL> <TELL "all ">)
              (<=? .F ,MCM-ANY> <TELL "any ">)>
        ;"YSPECs"
        <SET CNT <NP-YCNT .NP>>
        <COND (<0? .CNT>
               <COND (<=? .F ,MCM-ALL> <TELL "objects">)
                     (<=? .F ,MCM-ANY> <TELL "object">)>)
              (ELSE
               <SET S <NP-YSPEC .NP 1>>
               <DO (I 1 .CNT)
                   <COND (<G? .I 1> <TELL " and ">)>
                   <PRINT-OBJSPEC .S>
                   <SET S <REST .S ,P-OBJSPEC-SIZE>>>)>
        ;"NSPECs"
        <SET CNT <NP-NCNT .NP>>
        <COND (<0? .CNT> <RTRUE>)>
        <TELL " except ">
        <SET S <NP-NSPEC .NP 1>>
        <DO (I 1 .CNT)
            <COND (<G? .I 1> <TELL ", ">)>
            <PRINT-OBJSPEC .S>
            <SET S <REST .S ,P-OBJSPEC-SIZE>>>>

    <ROUTINE PRINT-OBJSPEC (SPEC "AUX" A N)
        <SET A <OBJSPEC-ADJ .SPEC>>
        <SET N <OBJSPEC-NOUN .SPEC>>
        <COND (<AND .A .N>
               <PRINT-ADJ .A>
               <TELL " " B .N>)
              (.A <PRINT-ADJ .A>)
              (.N <TELL B .N>)
              (ELSE <TELL "???">)>>

    <VERSION?
        (ZIP
            <DEFMAC PRINT-ADJ ('A)
                <FORM PRINT-MATCHING-WORD .A ,PS?ADJECTIVE ,P1?ADJECTIVE>>)
        (ELSE
            <DEFMAC PRINT-ADJ ('A)
                <FORM TELL B .A>>)>
>

<IFFLAG (<OR DEBUG DEBUGGING-VERBS>
         <ROUTINE PRINT-MATCHING-WORD (V PS P1 "AUX" W CNT SIZE)
             <SET W ,VOCAB>
             <SET W <+ .W 1 <GETB .W 0>>>
             <SET SIZE <GETB .W 0>>
             <SET W <+ .W 1>>
             <SET CNT <GET .W 0>>
             <SET W <+ .W 2>>
             <DO (I 1 .CNT)
                 <COND (<=? <CHKWORD? .W .PS .P1> .V>
                        <TELL B .W>
                        <RTRUE>)>
                 <SET W <+ .W .SIZE>>>
             <TELL "???">>)>

"Noun phrase storage for direct/indirect objects"
<CONSTANT P-NP-DOBJ <NOUN-PHRASE>>
<CONSTANT P-NP-IOBJ <NOUN-PHRASE>>

"Extra noun phrase for orphaning"
<CONSTANT P-NP-XOBJ <NOUN-PHRASE>>

"Tables for objects recognized from object specs.
 These each have one length byte, a dummy byte (V4+ only),
 then P-MAX-OBJECTS bytes/words for the objects."
<CONSTANT P-MAX-OBJECTS 50>

<DEFINE PRSTBL ()
    <ITABLE <+ 1 ,P-MAX-OBJECTS> <VERSION? (ZIP '(BYTE)) (ELSE '(WORD))>>>

"Matched direct objects"
<GLOBAL P-PRSOS <PRSTBL>>
"Matched indirect objects"
<GLOBAL P-PRSIS <PRSTBL>>
"Extra objects for orphaning>"
<GLOBAL P-XOBJS <PRSTBL>>

<DEFMAC COPY-PRSTBL ('SRC 'DEST)
    <FORM <VERSION? (ZIP COPY-TABLE-B) (ELSE COPY-TABLE)> .SRC .DEST <+ 1 ,P-MAX-OBJECTS>>>

"Structured type for backing up a complete parsed command"
<DEFSTRUCT PARSER-RESULT (TABLE ('NTH ZGET) ('PUT ZPUT) ('START-OFFSET 0))
    (PST-V-WORD FIX)
    (PST-P1 FIX)
    (PST-P2 FIX)
    (PST-SYNTAX FIX)
    (PST-PRSOS TABLE)
    (PST-PRSIS TABLE)
    (PST-NUMBER FIX)
    (PST-READBUF TABLE)
    (PST-LEXBUF TABLE)
    (PST-LEN BYTE 'OFFSET 18 'NTH GETB 'PUT PUTB)
    (PST-V BYTE 'OFFSET 19 'NTH GETB 'PUT PUTB)
    (PST-V-WORDN BYTE 'OFFSET 20 'NTH GETB 'PUT PUTB)
    (PST-NOBJ BYTE 'OFFSET 21 'NTH GETB 'PUT PUTB)
    (PST-PRSO-DIR BYTE 'OFFSET 22 'NTH GETB 'PUT PUTB)
    (PST-PRSA BYTE 'OFFSET 23 'NTH GETB 'PUT PUTB)>

<DEFINE PARSER-RESULT ()
    <MAKE-PARSER-RESULT
        'PARSER-RESULT <ITABLE 24 (BYTE)>
        'PST-PRSOS <PRSTBL>
        'PST-PRSIS <PRSTBL>
        'PST-READBUF <ITABLE NONE ,READBUF-SIZE (BYTE)>
        'PST-LEXBUF <ITABLE ,LEXBUF-SIZE (LEXV) 0 #BYTE 0 #BYTE 0>>>

<CONSTANT AGAIN-STORAGE <PARSER-RESULT>>

<ROUTINE SAVE-PARSER-RESULT (DEST)
    <PST-V-WORD .DEST ,P-V-WORD>
    <PST-P1 .DEST ,P-P1>
    <PST-P2 .DEST ,P-P2>
    <PST-SYNTAX .DEST ,P-SYNTAX>
    <COPY-PRSTBL ,P-PRSOS <PST-PRSOS .DEST>>
    <COPY-PRSTBL ,P-PRSIS <PST-PRSIS .DEST>>
    <PST-NUMBER .DEST ,P-NUMBER>
    <COPY-LEXBUF ,LEXBUF <PST-LEXBUF .DEST>>
    <COPY-READBUF ,READBUF <PST-READBUF .DEST>>
    <PST-LEN .DEST ,P-LEN>
    <PST-V .DEST ,P-V>
    <PST-V-WORDN .DEST ,P-V-WORDN>
    <PST-NOBJ .DEST ,P-NOBJ>
    <PST-PRSO-DIR .DEST ,PRSO-DIR>
    <PST-PRSA .DEST ,PRSA>>

<ROUTINE RESTORE-PARSER-RESULT (SRC "AUX" L)
    <SETG P-V-WORD <PST-V-WORD .SRC>>
    <SETG P-P1 <PST-P1 .SRC>>
    <SETG P-P2 <PST-P2 .SRC>>
    <SETG P-SYNTAX <PST-SYNTAX .SRC>>
    <COPY-PRSTBL <PST-PRSOS .SRC> ,P-PRSOS>
    <COPY-PRSTBL <PST-PRSIS .SRC> ,P-PRSIS>
    <SETG P-NUMBER <PST-NUMBER .SRC>>
    <COPY-LEXBUF <PST-LEXBUF .SRC> ,LEXBUF>
    <COPY-READBUF <PST-READBUF .SRC> ,READBUF>
    <SETG P-LEN <PST-LEN .SRC>>
    <SETG P-V <PST-V .SRC>>
    <SETG P-V-WORDN <PST-V-WORDN .SRC>>
    <SETG P-NOBJ <PST-NOBJ .SRC>>
    <SETG PRSO-DIR <PST-PRSO-DIR .SRC>>
    <SETG PRSA <PST-PRSA .SRC>>
    ;"Set variables calculated from these"
    <COND (<0? <SET L <GETB ,P-PRSOS 0>>>
           <SETG PRSO <>>)
          (<1? .L>
           <SETG PRSO <GET/B ,P-PRSOS 1>>)
          (ELSE <SETG PRSO ,MANY-OBJECTS>)>
    <COND (<0? <SET L <GETB ,P-PRSIS 0>>>
           <SETG PRSI <>>)
          (<1? .L>
           <SETG PRSI <GET/B ,P-PRSIS 1>>)
          (ELSE <SETG PRSI ,MANY-OBJECTS>)>>

"Orphaning"
<INSERT-FILE "orphan">

"Pronouns"
<INSERT-FILE "pronouns">

<PRONOUN IT (X)
    <NOT <OR <=? .X ,WINNER>
             <=? .X ,MANY-OBJECTS>
             <FSET? .X PERSONBIT>
             <FSET? .X PLURALBIT>>>>

<PRONOUN THEM (X)
    <AND <N=? .X ,WINNER>
         <OR <=? .X ,MANY-OBJECTS>
             <FSET? .X PLURALBIT>>>>

<PRONOUN HIM (X)
    <AND <N=? .X ,WINNER>
         <FSET? .X PERSONBIT>
         <NOT <FSET? .X FEMALEBIT>>>>

<PRONOUN HER (X)
    <AND <N=? .X ,WINNER>
         <FSET? .X PERSONBIT>
         <FSET? .X FEMALEBIT>>>

<FINISH-PRONOUNS>

"Buzzwords"
<BUZZ A AN AND ANY ALL BUT EXCEPT OF ONE THE \. \, \">

;"Reads and parses a command.

The primary outputs are PRSA, PRSO (+ PRSO-DIR), and PRSI, suitable
for passing to PERFORM.

If multiple objects are used for PRSO or PRSI, they will be set
to MANY-OBJECTS and PERFORM will have to read P-PRSOS or P-PRSIS.

Sets:
  P-LEN
  P-V
  P-NOBJ
  P-P1
  P-P2
  HERE
  PRSA
  PRSO
  PRSO-DIR
  P-PRSOS
  PRSI
  P-PRSIS
  P-BUTS
  P-EXTRA
  USAVE
  P-NP-DOBJ
  P-NP-IOBJ
"
<ROUTINE PARSER ("AUX" NOBJ VAL DIR O-R KEEP)
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
    <SET KEEP 0>
    ;"Handle an orphan response, which may abort parsing or ask us to skip steps"
    <COND (<ORPHANING?>
           <SET O-R <HANDLE-ORPHAN-RESPONSE>>
           <COND (<=? .O-R ,O-RES-REORPHANED> <RFALSE>)>
           <COND (<=? .O-R ,O-RES-FAILED>
                  <SETG P-O-REASON <>>
                  <RFALSE>)
                 (<=? .O-R ,O-RES-SET-NP>
                  ;"TODO: Set the P-variables somewhere else? Shouldn't we fill in what
                    we know about the command-to-be when we ask the orphaning question, not
                    when we get the response?"
                  <SETG P-P1 <GETB ,P-SYNTAX ,SYN-PREP1>>
                  <COND (<ORPHANING-PRSI?>
                         <SETG P-P2 <GETB ,P-SYNTAX ,SYN-PREP2>>
                         <SETG P-NOBJ 2>
                         ;"Don't re-match P-NP-DOBJ when we've just orphaned PRSI. Use the saved
                           match results. There won't be a NP to match if we GWIMmed PRSO."
                         <SET KEEP 1>)
                        (ELSE <SETG P-NOBJ 1>)>)
                 (<=? .O-R ,O-RES-SET-PRSTBL>
                  <COND (<ORPHANING-PRSI?> <SET KEEP 2>)
                        (ELSE <SET KEEP 1>)>)>
           <SETG P-O-REASON <>>)>
    ;"Identify parts of speech, parse noun phrases"
    <COND (<N=? .O-R ,O-RES-SET-NP ,O-RES-SET-PRSTBL>
           <SETG P-V <>>
           <SETG P-NOBJ 0>
           <CLEAR-NOUN-PHRASE ,P-NP-DOBJ>
           <CLEAR-NOUN-PHRASE ,P-NP-IOBJ>
           <SETG P-P1 <>>
           <SETG P-P2 <>>
           ;"Identify the verb, prepositions, and noun phrases"
           <REPEAT ((I 1) W)
               <COND (<G? .I ,P-LEN>
                      ;"Reached the end of the command"
                      <RETURN>)
                     (<NOT <OR <SET W <GETWORD? .I>>
                               <AND <PARSE-NUMBER? .I> <SET W ,W?\,NUMBER>>>>
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
                     (<STARTS-NOUN-PHRASE? .W>
                      ;"Found a noun phrase"
                      <SET NOBJ <+ .NOBJ 1>>
                      <COND (<==? .NOBJ 1>
                             <SET VAL <PARSE-NOUN-PHRASE .I ,P-NP-DOBJ>>)
                            (<==? .NOBJ 2>
                             <SET VAL <PARSE-NOUN-PHRASE .I ,P-NP-IOBJ>>)
                            (ELSE
                             <TELL "That sentence has too many objects." CR>
                             <RFALSE>)>
                      <COND (.VAL
                             <SET I .VAL>
                             <AGAIN>)
                            (ELSE
                             <TELL "That noun phrase didn't make sense." CR>
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
                     " P1=" N ,P-P1 " DOBJS=+" N <NP-YCNT ,P-NP-DOBJ> "-" N <NP-NCNT ,P-NP-DOBJ>
                     " P2=" N ,P-P2 " IOBJS=+" N <NP-YCNT ,P-NP-IOBJ> "-" N <NP-NCNT ,P-NP-IOBJ> "]" CR>>
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
           <SETG PRSO-DIR <>>)>
    ;"Match syntax lines and objects"
    <COND (<NOT .O-R>
           <COND (<NOT <AND <MATCH-SYNTAX> <FIND-OBJECTS .KEEP>>>
                  <RFALSE>)>)
          (<L? .KEEP 2>
           ;"We already found a syntax line last time, but we need FIND-OBJECTS to
             match at least one noun phrase."
           <COND (<NOT <FIND-OBJECTS .KEEP>>
                  <RFALSE>)>)>
    ;"Save command for AGAIN"
    <COND (<NOT <VERB? AGAIN>>
           <SAVE-PARSER-RESULT ,AGAIN-STORAGE>)>
    ;"Save UNDO state"
    <IF-UNDO
        <COND (<AND <NOT <VERB? UNDO>>
                    <NOT ,AGAINCALL>>
               <SETG USAVE <ISAVE>>
               <COND (<EQUAL? ,USAVE 2>
                      <TELL "Previous turn undone." CR>
                      ;<SETG USAVE 0>  ;"prevent undoing twice in a row"
                      <AGAIN>)>)>>
    ;"if successful PRSO and not after an IT use, back up PRSO for IT"
    <SET-PRONOUNS ,PRSO ,P-PRSOS>
    <RTRUE>>

;"PRSO or PRSI are set to this when multiple objects are used."
<OBJECT MANY-OBJECTS
    (DESC "those things")
    (FLAGS NDESCBIT NARTICLEBIT INVISIBLE PLURALBIT)>

;"PRSO or PRSI are set to this when a number is used as a noun.
  The actual parsed number is in the P-NUMBER global.
  The synonym is a word that can't be typed normally; we substitute
  it in the input buffer when we detect a number."
<OBJECT NUMBER
    (DESC "number")
    (IN GENERIC-OBJECTS)
    (SYNONYM \,NUMBER)>

<GLOBAL P-NUMBER 0>

;"Tries to parse the given word as a number.

If successful, the value is left in P-NUMBER, and the buffer is updated to
point the word to W?\,NUMBER.

Sets:
  P-NUMBER
  LEXBUF

Returns:
  True if the number was parsed and the buffer updated; otherwise false."
<ROUTINE PARSE-NUMBER? (WN "AUX" I MAX V C DIG NEG F)
    <SET I <GETB ,LEXBUF <+ <* .WN 4> 1>>>
    <SET MAX <- <+ .I <GETB ,LEXBUF <* .WN 4>>> 1>>
    <COND (<0? .MAX> <RFALSE>)>
    <SET F T>
    <REPEAT ()
        <SET C <GETB ,READBUF .I>>
        <COND (<AND .F <=? .C !\->>
               <SET NEG T>)
              (<AND <G=? .C !\0> <L=? .C !\9>>
               <SET DIG T>
               <SET V <+ <* .V 10> <- .C !\0>>>
               <COND (<L? .V 0> <RFALSE>)>)
              (ELSE <RFALSE>)>
        <SET F <>>
        <AND <IGRTR? I .MAX> <RETURN>>>
    <COND (<NOT .DIG> <RFALSE>)>
    <COND (.NEG <SET V <- .V>>)>
    <SETG P-NUMBER .V>
    <PUT ,LEXBUF <- <* .I 2> 1> ,W?\,NUMBER>
    <RETURN ,NUMBER>>

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

;"Determines whether a given word can start a noun phrase.

For a word to pass this test, it must be an article, adjective, or noun.

Args:
  W: The word to test.

Returns:
  True if the word can start a noun phrase."
<ROUTINE STARTS-NOUN-PHRASE? (W)
    ;"T? forces the OR to be evaluated as a condition, since we don't
      care about the exact return value from CHKWORD? or WORD?."
    <T? <OR <EQUAL? .W ,W?A ,W?AN ,W?THE ,W?ALL ,W?ANY ,W?ONE>
            <CHKWORD? .W ,PS?ADJECTIVE>
            <CHKWORD? .W ,PS?OBJECT>>>>

<CONSTANT MCM-ALL 1>
<CONSTANT MCM-ANY 2>

;"Attempts to parse a noun phrase.

If the match fails, an error message may be printed.

Args:
  WN: The 1-based word number where the noun clause starts.
  NP: A NOUN-PHRASE in which to return the parsed result.

Returns:
  The number of the first word that was not part of the noun phrase,
  if parsing was successful, or 0 if parsing failed.
  NP may be left in an invalid state if parsing fails."
<ROUTINE PARSE-NOUN-PHRASE (WN NP "AUX" SPEC CNT W VAL MODE ADJ NOUN BUT)
    <SET SPEC <NP-YSPEC .NP 1>>
    <NP-NCNT .NP 0>
    <REPEAT ()
        <COND
            ;"exit loop if we reached the end of the command"
            (<G? .WN ,P-LEN> <RETURN>)
            ;"fail if we found an unrecognized word"
            (<NOT <OR <SET W <GETWORD? .WN>>
                      <AND <PARSE-NUMBER? .WN> <SET W ,W?\,NUMBER>>>>
             <RFALSE>)
            ;"recognize BUT/EXCEPT"
            (<AND <NOT .BUT> <EQUAL? .W ,W?BUT ,W?EXCEPT>>
             <COND (<OR .ADJ .NOUN>
                    <OBJSPEC-ADJ .SPEC .ADJ>
                    <OBJSPEC-NOUN .SPEC .NOUN>
                    <SET ADJ <SET NOUN <>>>
                    <SET CNT <+ .CNT 1>>)>
             <NP-YCNT .NP .CNT>
             <SET BUT T>
             <SET SPEC <NP-NSPEC .NP 1>>
             <SET CNT 0>)
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
                       <CHKWORD? .W ,PS?OBJECT>         ;"word can be a noun"
                       <OR ;"word is at end of line"
                           <==? .WN ,P-LEN>
                           ;"next word is not adj/noun"
                           <BIND ((NW <GETWORD? <+ .WN 1>>))
                               <NOT <OR <CHKWORD? .NW ,PS?ADJECTIVE>
                                        <CHKWORD? .NW ,PS?OBJECT>>>>>>
                  <SET NOUN .W>)
                 (<==? .CNT ,P-MAX-OBJSPECS>
                  <TELL "That phrase mentions too many objects." CR>
                  <RFALSE>)
                 (<NOT .ADJ> <SET ADJ .VAL>)>)
            ;"match nouns, exiting the loop if we already found one"
            (<CHKWORD? .W ,PS?OBJECT>
             <COND (.NOUN <RETURN>)
                   (<==? .CNT ,P-MAX-OBJSPECS>
                    <TELL "That phrase mentions too many objects." CR>
                    <RFALSE>)
                   (ELSE <SET NOUN .W>)>)
            ;"recognize AND/comma"
            (<EQUAL? .W ,W?AND ,W?COMMA>
             <COND (<OR .ADJ .NOUN>
                    <OBJSPEC-ADJ .SPEC .ADJ>
                    <OBJSPEC-NOUN .SPEC .NOUN>
                    <SET ADJ <SET NOUN <>>>
                    <SET SPEC <REST .SPEC ,P-OBJSPEC-SIZE>>
                    <SET CNT <+ .CNT 1>>)>)
            ;"skip buzzwords"
            (<CHKWORD? .W ,PS?BUZZ-WORD>)
            ;"exit loop if we found any other word type"
            (ELSE <RETURN>)>
        <SET WN <+ .WN 1>>>
    ;"store final adj/noun pair"
    <COND (<OR .ADJ .NOUN>
           <OBJSPEC-ADJ .SPEC .ADJ>
           <OBJSPEC-NOUN .SPEC .NOUN>
           <SET CNT <+ .CNT 1>>)>
    ;"store phrase count and mode"
    <COND (.BUT <NP-NCNT .NP .CNT>) (ELSE <NP-YCNT .NP .CNT>)>
    <NP-MODE .NP .MODE>
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
<ROUTINE MATCH-SYNTAX ("AUX" PTR CNT S BEST BEST-SCORE)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <IF-DEBUG <TELL "[MATCH-SYNTAX: " N .CNT " syntaxes at " N .PTR "]" CR>>
    <SET BEST-SCORE -999>
    <REPEAT ()
        <COND (<DLESS? CNT 0>
               ;"Out of syntax lines"
               <RETURN>)>
        <IF-DEBUG
            <TELL "[trying line: ">
            <PRINT-SYNTAX-LINE .PTR>
            <TELL " ... ">>
        <SET S <MATCH-SYNTAX-LINE? .PTR>>
        <IF-DEBUG <TELL N .S "]" CR>>
        <COND (<AND .S <G? .S .BEST-SCORE>>
               <SET BEST-SCORE .S>
               <SET BEST .PTR>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>
    <COND (.BEST
           <SETG PRSA <GETB .BEST ,SYN-ACTION>>
           <SETG P-SYNTAX .BEST>
           <RTRUE>)
          (ELSE
           <TELL "I don't understand that sentence." CR>
           <RFALSE>)>>

<IF-DEBUG
    <ROUTINE PRINT-SYNTAX-LINE (PTR)
        <TELL "NOBJ=" N <GETB .PTR ,SYN-NOBJ>
              " P1=" N <GETB .PTR ,SYN-PREP1>
              " P2=" N <GETB .PTR ,SYN-PREP2>
              " A=" N <GETB .PTR ,SYN-ACTION>>>>

;"Scores how well the parsed command matches a syntax line.

Args:
  PTR: The syntax line.

Returns:
  1 if it matches exactly, 0 if it cannot match, or a negative number
  if it partially matches (i.e. if it could match after inference).
  Negative numbers further below 0 indicate worse matches needing more inference."
<ROUTINE MATCH-SYNTAX-LINE? (PTR "AUX" NOBJ PREP1 PREP2 R)
    <SET NOBJ <GETB .PTR ,SYN-NOBJ>>
    <SET PREP1 <GETB .PTR ,SYN-PREP1>>
    <SET PREP2 <GETB .PTR ,SYN-PREP2>>
    <COND ;"If the object count and prepositions are all as expected,
            this is an exact match."
          (<AND <=? ,P-NOBJ .NOBJ> <=? ,P-P1 .PREP1> <=? ,P-P2 .PREP2>>
           <RTRUE>)
          ;"If object count >= expected count, this can't match."
          (<G=? ,P-NOBJ .NOBJ> <RFALSE>)
          ;"If either preposition is nonzero yet different from expected,
            this can't match."
          (<OR <N=? ,P-P1 .PREP1 0> <N=? ,P-P2 .PREP2 0>>
           <RFALSE>)
          ;"If we have one object, and we expected a first preposition but
            didn't get it, this can't match. (If we have two objects, we
            already failed an earlier test.)"
          (<AND <1? ,P-NOBJ> .PREP1 <NOT ,P-P1>> <RFALSE>)
          ;"If we'd end up using FIND KLUDGEBIT for a missing noun and having
            to infer the preposition, don't match this line."
          (<OR <AND <G=? .NOBJ 1>
                    <0? ,P-NOBJ>
                    <0? ,P-P1>
                    .PREP1
                    <=? <GETB .PTR ,SYN-FIND1> ,KLUDGEBIT>>
               <AND <=? .NOBJ 2>
                    <0? ,P-P2>
                    .PREP2
                    <=? <GETB .PTR ,SYN-FIND2> ,KLUDGEBIT>>>
           <RFALSE>)>
    ;"We have a possible match; now score how well it matches.
      Dock three points for each object we have to infer."
    <SET R <* 3 <- ,P-NOBJ .NOBJ>>>
    ;"Dock an extra point for PRSO if we have to infer a preposition also."
    <COND (<AND <NOT ,P-P1> .PREP1> <SET R <- .R 1>>)>
    ;"Dock an extra point for PRSI if we have to infer *no* preposition.
      This makes us prefer syntaxes with the direct object first
      (GIVE OBJECT TO OBJECT instead of GIVE OBJECT OBJECT)."
    <COND (<AND <=? .NOBJ 2> <NOT <OR ,P-P2 .PREP2>>> <SET R <- .R 1>>)>
    .R>

<INSERT-FILE "scope">

<CONSTANT S-PRONOUN-UNKNOWN-PERSON "I'm unsure to whom you are referring.">
<CONSTANT S-PRONOUN-UNKNOWN-THING "I'm unsure what you're referring to.">

;"Attempts to match PRSO and PRSI, if necessary, after parsing a command.
  Prints a message if it fails.

  If multiple objects are used, sets PRSO or PRSI to MANY-OBJECTS.
  The objects are left in P-PRSOS and P-PRSIS.

Args:
  KEEP: The number of already matched noun phrases to leave as-is (0, 1, or 2).
    Pass 1 when P-PRSOS was previously matched and should not be modified, e.g.
    when PRSO was set by GWIM and FIND-OBJECTS is being called to set PRSI from
    a new noun phrase after orphaning.

Uses:
  P-NOBJ
  P-DOBJS
  P-SYNTAX

Sets:
  PRSO
  PRSI
  P-PRSOS
  P-PRSIS

Returns:
  True if all required objects were found, or false if not."
<ROUTINE FIND-OBJECTS (KEEP "AUX" F O (SNOBJ <GETB ,P-SYNTAX ,SYN-NOBJ>))
    <COND (<G=? .KEEP 1>)
          (<L? .SNOBJ 1>
           <SETG PRSO <>>)
          (ELSE
           <SET F <GETB ,P-SYNTAX ,SYN-FIND1>>
           <SET O <GETB ,P-SYNTAX ,SYN-OPTS1>>
           <COND (<L? ,P-NOBJ 1>
                  <SETG PRSO
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP1>>>
                  <COND (<0? ,PRSO>
                         <WHAT-DO-YOU-WANT>
                         <ORPHAN T MISSING PRSO>
                         <RFALSE>)
                        (ELSE
                         <PUT/B ,P-PRSOS 1 ,PRSO>
                         <PUTB ,P-PRSOS 0 1>)>)
                 (ELSE
                  <SETG PRSO
                      <OR <AND <1? <NP-YCNT ,P-NP-DOBJ>>
                               <EXPAND-PRONOUN <OBJSPEC-NOUN <NP-YSPEC ,P-NP-DOBJ 1>> ,P-PRSOS>>
                          <MATCH-NOUN-PHRASE ,P-NP-DOBJ ,P-PRSOS <ENCODE-NOUN-BITS .F .O>>>>
                  <COND (<=? ,PRSO ,EXPAND-PRONOUN-FAILED> <RFALSE>)>)>
           <COND (<NOT <AND ,PRSO
                            <OR ,PRSO-DIR
                                <AND <MANY-CHECK ,PRSO .O <>>
                                     <HAVE-TAKE-CHECK-TBL ,P-PRSOS .O>>>>>
                  <RFALSE>)>)>
    <COND (<G=? .KEEP 2>)
          (<L? .SNOBJ 2>
           <SETG PRSI <>>)
          (ELSE
           <SET F <GETB ,P-SYNTAX ,SYN-FIND2>>
           <SET O <GETB ,P-SYNTAX ,SYN-OPTS2>>
           <COND (<L? ,P-NOBJ 2>
                  <SETG PRSI
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP2>>>
                  <COND (<0? ,PRSI>
                         <WHAT-DO-YOU-WANT>
                         <ORPHAN T MISSING PRSI>
                         <RFALSE>)
                        (ELSE
                         <PUT/B ,P-PRSIS 1 ,PRSI>
                         <PUTB ,P-PRSIS 0 1>)>)
                 (ELSE
                  <SETG PRSI
                      <OR <AND <1? <NP-YCNT ,P-NP-IOBJ>>
                               <EXPAND-PRONOUN <OBJSPEC-NOUN <NP-YSPEC ,P-NP-IOBJ 1>> ,P-PRSIS>>
                          <MATCH-NOUN-PHRASE ,P-NP-IOBJ ,P-PRSIS <ENCODE-NOUN-BITS .F .O>>>>
                  <COND (<=? ,PRSI ,EXPAND-PRONOUN-FAILED> <RFALSE>)>)>
           <COND (<NOT <AND ,PRSI
                            <MANY-CHECK ,PRSI .O T>
                            <HAVE-TAKE-CHECK-TBL ,P-PRSIS .O>>>
                  <RFALSE>)>)>
    <RTRUE>>

<ROUTINE WHAT-DO-YOU-WANT ("AUX" SN SP1 SP2 F)
    <SET SN <GETB ,P-SYNTAX ,SYN-NOBJ>>
    <SET SP1 <GETB ,P-SYNTAX ,SYN-PREP1>>
    <SET SP2 <GETB ,P-SYNTAX ,SYN-PREP2>>
    ;"TODO: use LONG-WORDS table for preposition words"
    <COND (<AND ,PRSO <NOT ,PRSO-DIR>>
           <SET F <GETB ,P-SYNTAX ,SYN-FIND2>>)
          (ELSE <SET F <GETB ,P-SYNTAX ,SYN-FIND1>>)>
    <COND (<=? .F ,PERSONBIT> <TELL "Whom">)
          (ELSE <TELL "What">)>
    <TELL " do you want to ">
    <PRINT-VERB>
    <COND (.SP1
           <TELL " " B <GET-PREP-WORD .SP1>>)>
    <COND (<AND ,PRSO <NOT ,PRSO-DIR>>
           <TELL " " T ,PRSO>
           <COND (.SP2
                  <TELL " " B <GET-PREP-WORD .SP2>>)>)>
    <TELL "?" CR>>

<ROUTINE PRINT-VERB ()
    <COND (,P-V-WORDN <PRINT-WORD ,P-V-WORDN>)
          (ELSE <PRINTB ,P-V-WORD>)>>

;"Applies the rules for the MANY syntax flag to PRSO or PRSI, printing a
failure message if appropriate.

Args:
  OBJ: PRSO or PRSI, which should equal MANY-OBJECTS if multiple objects were
    matched or a single object otherwise.
  OPTS: The corresponding search options, including the MANY flag if set.
  INDIRECT?: True if the failure message should say 'indirect objects' instead
    of 'direct objects'.

Returns:
  True if the check passed, i.e. either a single object was matched or the MANY
  flag allowed multiple objects. False if multiple objects were matched but the
  MANY flag was not set."
<ROUTINE MANY-CHECK (OBJ OPTS INDIRECT?)
    <COND (<AND <=? .OBJ ,MANY-OBJECTS>
                <NOT <BTST .OPTS ,SF-MANY>>>
           <TELL "You can't use multiple ">
           <COND (.INDIRECT? <TELL "in">)>
           <TELL "direct objects with \"">
           <PRINT-VERB>
           <TELL "\"." CR>
           <RFALSE>)>
    <RTRUE>>

;"Applies the rules for the HAVE and TAKE syntax flags to a set of parsed objects,
printing a failure message if appropriate.

Args:
  TBL: Either P-PRSOS or P-PRSIS.
  OPTS: The corresponding search options, including the HAVE and TAKE flags.

Returns:
  True if the checks passed, i.e. either the objects don't have to be held
  by the WINNER, or they are held, possibly as the result of an implicit TAKE.
  False if the objects have to be held, the WINNER is not holding them, and
  they couldn't be taken implicitly."
<ROUTINE HAVE-TAKE-CHECK-TBL (TBL OPTS "AUX" MAX DO-TAKE? O)
    <SET MAX <GETB .TBL 0>>
    ;"Attempt implicit take if WINNER isn't directly holding the objects"
    <COND (<BTST .OPTS ,SF-TAKE>
           ;"TODO: Don't implicit take out of a closed container? Or should
             the container handle this by setting TRYTAKEBIT on its contents?"
           ;"TODO: Enforce inventory limit; split relevant logic out of V-TAKE."
           <DO (I 1 .MAX)
               <COND (<NEEDS-IMPLICIT-TAKE? <GET/B .TBL .I>>
                      <TELL "[taking ">
                      <LIST-OBJECTS .TBL ,NEEDS-IMPLICIT-TAKE? %<+ ,L-PRSTABLE ,L-THE>>
                      <TELL "]" CR>
                      <REPEAT ()
                          <COND (<NEEDS-IMPLICIT-TAKE? <SET O <GET/B .TBL .I>>>
                                 <MOVE .O ,WINNER>)>
                          <COND (<IGRTR? I .MAX> <RETURN>)>>
                      <RETURN>)>>)>
    ;"WINNER must (indirectly) hold the objects if SF-HAVE is set"
    <COND (<BTST .OPTS ,SF-HAVE>
           <DO (I 1 .MAX)
               <COND (<NOT <HELD? <GET/B .TBL .I>>>
                      <TELL "You aren't holding ">
                      <LIST-OBJECTS .TBL ,NOT-HELD? %<+ ,L-PRSTABLE ,L-THE ,L-OR>>
                      <TELL "." CR>
                      <RFALSE>)>>)>
    <RTRUE>>

;"Like HAVE-TAKE-CHECK-TBL but for a single object.

Args:
  OBJ: An object.
  OPTS: The corresponding search options, including the HAVE and TAKE flags.

Returns:
  True if the checks passed, i.e. either the objects don't have to be held
  by the WINNER, or they are held, possibly as the result of an implicit TAKE.
  False if the objects have to be held, the WINNER is not holding them, and
  they couldn't be taken implicitly."
<ROUTINE HAVE-TAKE-CHECK (OBJ OPTS)
    ;"Attempt implicit take if WINNER isn't directly holding the object"
    <COND (<BTST .OPTS ,SF-TAKE>
           <COND (<NEEDS-IMPLICIT-TAKE? .OBJ>
                  <TELL "[taking " T .OBJ "]" CR>
                  <MOVE .OBJ ,WINNER>)>)>
    ;"WINNER must (indirectly) hold the object if SF-HAVE is set"
    <COND (<BTST .OPTS ,SF-HAVE>
           <COND (<NOT <HELD? .OBJ>>
                  <TELL "You aren't holding " T .OBJ "." CR>
                  <RFALSE>)>)>
    <RTRUE>>

<ROUTINE NEEDS-IMPLICIT-TAKE? (OBJ)
    <T? <AND <NOT <IN? .OBJ ,WINNER>>
             <FSET? .OBJ ,TAKEBIT>
             <NOT <FSET? .OBJ ,TRYTAKEBIT>>>>>

<ROUTINE NOT-HELD? (OBJ)
    <NOT <HELD? .OBJ>>>

;"Checks whether the objects listed in a table, which were part of a previous
  command, are still available to the player, and prints an error message if not.

Args:
  TBL: A PRS table.

Returns:
  True if the check passed, or false if at least one object has become unavailable
  and a message was printed."
<ROUTINE STILL-VISIBLE-CHECK (TBL "AUX" (CNT <GETB .TBL 0>))
    <OR .CNT <RTRUE>>
    <DO (I 1 .CNT)
        <COND (<NOT <VISIBLE? <GET/B .TBL .I>>>
               <LIST-OBJECTS .TBL ,NOT-VISIBLE? %<+ ,L-PRSTABLE ,L-THE ,L-CAP ,L-SUFFIX>>
               <TELL " no longer here." CR>
               <RFALSE>)>>
    <RTRUE>>

<ROUTINE NOT-VISIBLE? (O)
    <NOT <VISIBLE? .O>>>

;"Searches scope for a single object with the given flag set, and prints an
inference message before returning it.

The flag KLUDGEBIT is a special case that always finds ROOMS.

Args:
  BIT: The flag to search for.
  OPTS: The search options to use.
  PREP: The preposition to use in the message.

Returns:
  The single object that matches, or false if zero or multiple objects match."
<ROUTINE GWIM (BIT OPTS PREP "AUX" O PW)
    ;"Special case"
    <COND (<==? .BIT ,KLUDGEBIT>
           <RETURN ,ROOMS>)>
    ;"Look for exactly one matching object, or two if one is WINNER"
    <MAP-SCOPE (I [BITS .OPTS])
        <COND (<FSET? .I .BIT>
               <COND (<AND .O <N=? .O ,WINNER> <N=? .I ,WINNER>>
                      <RFALSE>)
                     (<OR <NOT .O> <N=? .I ,WINNER>>
                      <SET O .I>)>)>>
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
    <FORM BOR <FORM * .F 256> .O>>

<DEFMAC DECODE-FINDBIT ('E)
    <FORM BAND <FORM / .E 256> 255>>

;"Searches scope for a usable light source.

Returns:
  An object providing light, or false if no light source was found."
<ROUTINE SEARCH-FOR-LIGHT SFL ()
    <COND (<FSET? ,HERE ,LIGHTBIT> <RTRUE>)>
    <MAP-SCOPE (I [STAGES (LOCATION INVENTORY GLOBALS LOCAL-GLOBALS)] [NO-LIGHT])
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

;"Attempts to find one or more objects in scope, given a noun phrase that
describes them and a set of search options.

The search options are used to guide the scope search toward the right objects,
but are not hard requirements.

Specifically, when ALL/ANY is used, we first look for a match in only the preferred
scope stages, and then expand to all stages if no matches are found.
When ALL/ANY is not used, we first use all scope stages, and then narrow to only the
preferred stages if more than one match is found.

Args:
  NP: The NOUN-PHRASE describing the objects.
  OUT: A table (in the form of P-PRSOS or P-PRSIS) in which to return the matched objects.
  BITS: A FIND flag and search options, as returned by ENCODE-NOUN-BITS.

Uses:
  PRSA

Returns:
  The matched object, or MANY-OBJECTS if multiple objects were matched,
  or false if no objects were matched."
<ROUTINE MATCH-NOUN-PHRASE (NP OUT BITS "AUX" F NY NN SPEC MODE NOUT OBITS ONOUT BEST Q)
    <SET NY <NP-YCNT .NP>>
    <SET NN <NP-NCNT .NP>>
    <SET MODE <NP-MODE .NP>>
    <SET OBITS .BITS>
    <COND (<0? .MODE>
           <SET .BITS <ORB .BITS %<ORB ,SF-HELD ,SF-CARRIED ,SF-ON-GROUND ,SF-IN-ROOM>>>)>
    <PROG BITS-SET ()
        ;"Look for matching objects"
        <SET NOUT 0>
        <COND (<0? .NY>
               ;"ALL with no YSPECs matches all objects, or if the action is TAKE/DROP,
                 all objects with TAKEBIT/TRYTAKEBIT, skipping generic/global objects."
               <MAP-SCOPE (I [BITS .BITS])
                   <COND (<SCOPE-STAGE? GENERIC GLOBALS>)
                         (<NOT <ALL-INCLUDES? .I>>)
                         (<AND .NN <NP-EXCLUDES? .NP .I>>)
                         (<G=? .NOUT ,P-MAX-OBJECTS>
                          <TELL "[too many objects!]" CR>
                          <RETURN>)
                         (ELSE
                          <SET NOUT <+ .NOUT 1>>
                          <PUT/B .OUT .NOUT .I>)>>)
              (ELSE
               ;"Go through all YSPECs and find matching objects for each one.
                 Give an error if any YSPEC has no matches, but it's OK if all
                 the matches for some YSPEC are excluded by NSPECs. Keep track of
                 the match quality and only select the best matches."
               <DO (J 1 .NY)
                   <SET SPEC <NP-YSPEC .NP .J>>
                   <SET F <>>
                   <SET ONOUT .NOUT>
                   <SET BEST 1>
                   <MAP-SCOPE (I [BITS .BITS])
                       <COND (<AND <NOT <FSET? .I ,INVISIBLE>>
                                   <SET Q <REFERS? .SPEC .I>>
                                   <G=? .Q .BEST>>
                              <SET F T>
                              ;"Erase previous matches if this is better"
                              <COND (<G? .Q .BEST>
                                     <SET NOUT .ONOUT>
                                     <SET .BEST .Q>)>
                              <COND (<G=? .NOUT ,P-MAX-OBJECTS>
                                     <TELL "[too many objects!]" CR>
                                     <RETURN>)
                                    (<NOT <AND .NN <NP-EXCLUDES? .NP .I>>>
                                     <SET NOUT <+ .NOUT 1>>
                                     <PUT/B .OUT .NOUT .I>)>)>>
                   <COND (<NOT .F>
                          ;"Try expanding the search if we can."
                          <SET F <ORB .BITS %<ORB ,SF-HELD ,SF-CARRIED ,SF-ON-GROUND ,SF-IN-ROOM>>>
                          <COND (<N=? .BITS .F>
                                 <SET BITS .F>
                                 <SET OBITS .F>    ;"Avoid bouncing between <1 and >1 matches"
                                 <AGAIN .BITS-SET>)>
                          <COND (<=? ,MAP-SCOPE-STATUS ,MS-NO-LIGHT>
                                 <TELL "It's too dark to see anything here." CR>)
                                (ELSE
                                 <TELL "You don't see that here." CR>)>
                          <RFALSE>)
                         (<G=? .NOUT ,P-MAX-OBJECTS>
                          <RETURN>)>>)>
        ;"Check the number of objects"
        <PUTB .OUT 0 .NOUT>
        <COND (<0? .NOUT>
               ;"This means ALL matched nothing, or BUT excluded everything.
                 Try expanding the search if we can."
               <SET F <ORB .BITS %<ORB ,SF-HELD ,SF-CARRIED ,SF-ON-GROUND ,SF-IN-ROOM>>>
               <COND (<=? .BITS .F>
                      <TELL "There are none at all available!" CR>
                      <RFALSE>)>
               <SET BITS .F>
               <SET OBITS .F>    ;"Avoid bouncing between <1 and >1 matches"
               <AGAIN .BITS-SET>)
              (<1? .NOUT>
               <RETURN <GET/B .OUT 1>>)
              (<OR <=? .MODE ,MCM-ALL> <G? .NY 1>>
               <RETURN ,MANY-OBJECTS>)
              (<=? .MODE ,MCM-ANY>
               ;"Pick a random object"
               <PUT/B .OUT 1 <SET F <GET/B .OUT <RANDOM .NOUT>>>>
               <PUTB .OUT 0 1>
               <TELL "[" T .F "]" CR>
               <RETURN .F>)
              (ELSE
               ;"TODO: Do this check when we're matching YSPECs, so each YSPEC can be
                 disambiguated individually."
               ;"Try narrowing the search if we can."
               <COND (<N=? .BITS .OBITS>
                      <SET BITS .OBITS>
                      <AGAIN .BITS-SET>)>
               <WHICH-DO-YOU-MEAN .OUT>
               <COND (<=? .NP ,P-NP-DOBJ> <ORPHAN T AMBIGUOUS PRSO>)
                     (ELSE <ORPHAN T AMBIGUOUS PRSI>)>
               <RFALSE>)>>>

<ROUTINE ALL-INCLUDES? (OBJ)
    <NOT <OR <FSET? .OBJ ,INVISIBLE>
             <=? .OBJ ,WINNER>
             <AND <VERB? TAKE DROP>
                  <NOT <OR <FSET? .OBJ ,TAKEBIT>
                       <FSET? .OBJ ,TRYTAKEBIT>>>>>>>

<ROUTINE WHICH-DO-YOU-MEAN (TBL)
    <TELL "Which do you mean, ">
    <LIST-OBJECTS .TBL <> %<+ ,L-PRSTABLE ,L-THE ,L-OR>>
    <TELL "?" CR>>

;"Determines whether an object is excluded by a NOUN-PHRASE's NTBL.
  Note: NP may be evaluated twice."
<DEFMAC NP-INCLUDES? ('NP 'O)
    <FORM ANY-SPEC-REFERS? <FORM NP-YTBL .NP> <FORM NP-YCNT .NP> .O>>

<ROUTINE ANY-SPEC-REFERS? (TBL N O)
    <COND (<0? .N> <RFALSE>)>
    <DO (I 1 .N)
        <COND (<REFERS? .TBL .O> <RTRUE>)>
        <SET TBL <+ .TBL ,P-OBJSPEC-SIZE>>>
    <RFALSE>>

;"Determines whether an object is excluded by a NOUN-PHRASE's NTBL.
  Note: NP may be evaluated twice."
<DEFMAC NP-EXCLUDES? ('NP 'O)
    <FORM ANY-SPEC-REFERS? <FORM NP-NTBL .NP> <FORM NP-NCNT .NP> .O>>

;"Determines whether a local-global object is present in a given room.

Args:
  O: The local-global object.
  R: The room.

Returns:
  True if the object is present in the room's GLOBAL property.
  Otherwise, false."
<ROUTINE GLOBAL-IN? (O R)
    <AND <NOT <FSET? .O ,INVISIBLE>> <IN-PB/WTBL? .R ,P?GLOBAL .O>>>

;"Determines whether an OBJSPEC refers to a given object.

The OBJSPEC may have an adjective, a noun, or both. It may also have a word in its
noun slot that's actually an adjective.

Args:
  SPEC: The OBJSPEC.
  O: The object.

Returns:
  A quality score. 0 means the spec didn't match at all, 1 means it matched as
  adjective-only, 2 means it matched as noun-only, 3 means it was a two-word match."
<ROUTINE REFERS? (SPEC O "AUX" (A <OBJSPEC-ADJ .SPEC>) (N <OBJSPEC-NOUN .SPEC>))
    <COND (<AND .A .N>
           <COND (<AND <IN-PB/WTBL? .O ,P?ADJECTIVE .A>
                       <IN-PWTBL? .O ,P?SYNONYM .N>>
                  <RETURN 3>)>)
          (.N
           <COND (<IN-PWTBL? .O ,P?SYNONYM .N> <RETURN 2>)
                 (<VERSION?
                      (ZIP <SET A <CHKWORD? .N ,PS?ADJECTIVE ,P1?ADJECTIVE>>)
                      (ELSE <AND <CHKWORD? .N ,PS?ADJECTIVE> <SET A .N>>)>
                  <COND (<IN-PB/WTBL? .O ,P?ADJECTIVE .A> <RETURN 1>)>)>)
          (.A
           <COND (<IN-PB/WTBL? .O ,P?ADJECTIVE .A> <RETURN 1>)>)>
    <RETURN 0>>

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

;"Copies a LEXBUF-like table."
<ROUTINE COPY-LEXBUF (SRC DEST "AUX" (WDS <GETB .SRC 1>))
    <PUTB .DEST 1 .WDS>
    <COPY-TABLE <REST .SRC 2> <REST .DEST 2> <* 2 .WDS>>>

;"Copies a READBUF-like table."
<ROUTINE COPY-READBUF (SRC DEST)
    <COPY-TABLE .SRC .DEST %</ ,READBUF-SIZE 2>>>

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
<ROUTINE PERFORM (ACT "OPT" DOBJ IOBJ "AUX" PRTN RTN OA OD ODD OI WON CNT)
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
    ;"Warn about improper number use, and handle multiple objects"
    <COND (<G? <COUNT-PRS-APPEARANCES ,NUMBER> 1>
           <TELL "You can't use more than one number in a command." CR>
           <SET WON <>>)
          (<AND <NOT ,PRSO-DIR> <PRSO? ,MANY-OBJECTS>>
           <COND (<PRSI? ,MANY-OBJECTS>
                  <TELL "You can't use multiple direct and indirect objects together." CR>
                  <SET WON <>>)
                 (ELSE
                  <SET CNT <GETB ,P-PRSOS 0>>
                  <DO (I 1 .CNT)
                      <SETG PRSO <GET/B ,P-PRSOS .I>>
                      <TELL D ,PRSO ": ">
                      <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>>)>)
          (<PRSI? ,MANY-OBJECTS>
           <SET CNT <GETB ,P-PRSIS 0>>
           <DO (I 1 .CNT)
               <SETG PRSI <GET/B ,P-PRSIS .I>>
               <TELL D ,PRSI ": ">
               <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>>)
          (ELSE <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>)>
    <SETG PRSA .OA>
    <SETG PRSO .OD>
    <SETG PRSO-DIR .ODD>
    <SETG PRSI .OI>
    .WON>

<ROUTINE COUNT-PRS-APPEARANCES (O "AUX" R MAX)
    <COND (<PRSO? .O> <INC R>)
          (<PRSO? ,MANY-OBJECTS>
           <SET MAX <GETB ,P-PRSOS 0>>
           <DO (I 1 .MAX)
               <COND (<=? <GET/B ,P-PRSOS .I> .O> <INC R>)>>)>
    <COND (<PRSI? .O> <INC R>)
          (<PRSI? ,MANY-OBJECTS>
           <SET MAX <GETB ,P-PRSIS 0>>
           <DO (I 1 .MAX)
               <COND (<=? <GET/B ,P-PRSIS .I> .O> <INC R>)>>)>
    .R>

;"Helper function to call action handlers, respecting a search order.

The routine searches for handlers in a set order, calling each one it finds
until one returns true to indicate that it has handled the action.

The search order is as follows:
  ACTION property of WINNER (with M-WINNER parameter)
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
                <APPLY .AC ,M-WINNER>>
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
    <COND (,HERE-LIT <FSET ,HERE ,TOUCHBIT>)>>

"Misc Routines"

;"Searches an object to find exactly one child with a given flag set, and
optionally prints a message about it.

If no matching child is found, the search expands to any LOCAL-GLOBALS present.

Args:
  C: The container or location to search.
  BIT: The flag to look for.
  WORD: A string to print in a message describing the found object,
    e.g. 'with' to print '[with the purple key]'. If omitted, no message
    will be shown.

Returns:
  If exactly one object was found, returns the found object.
  If zero or multiple objects were found, returns false."
<ROUTINE FIND-IN (C BIT "OPT" WORD "AUX" N W PT MAX)
    <IF-DEBUG <TELL "[FIND-IN: looking in " D .C " for " N .BIT "]" CR>>
    <MAP-CONTENTS (I .C)
        <IF-DEBUG <TELL "[considering " D .I "...">>
        <COND (<FSET? .I .BIT>
               <IF-DEBUG <TELL " OK">>
               <SET N <+ .N 1>>
               <SET W .I>)>
        <IF-DEBUG <TELL "]" CR>>>
    <COND (<AND <0? .N> <SET PT <GETPT .C ,P?GLOBAL>>>
           <IF-DEBUG <TELL "[falling back to LOCAL-GLOBALS]" CR>>
           <SET MAX <PTSIZE .PT>>
           <VERSION? (ZIP) (ELSE <SET MAX </ .MAX 2>>)>
           <SET MAX <- .MAX 1>>
           <DO (J 0 .MAX)
               <BIND ((I <GET/B .PT .J>))
                   <IF-DEBUG <TELL "[FIND-IN: considering " D .I "...">>
                   <COND (<FSET? .I .BIT>
                          <IF-DEBUG <TELL " OK">>
                          <SET N <+ .N 1>>
                          <SET W .I>)>
                   <IF-DEBUG <TELL "]" CR>>>>)>
    <IF-DEBUG <TELL "[FIND-IN: found " N .N "]" CR>>
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
         <VERSION? (ZIP) (ELSE <PRINTC .RESP> <CRLF>)>
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
           <FSET ,HERE ,TOUCHBIT>
           <OR <DARKNESS-F ,M-NOW-LIT> <V-LOOK>>)>>

<INSERT-FILE "events">

<INSERT-FILE "verbs">

"Objects"


;"This has all the flags, just in case other objects don't define them."
<OBJECT ROOMS
    (FLAGS %<CHTYPE ,KNOWN-FLAGS SPLICE>)>

<OBJECT GLOBAL-OBJECTS>

<OBJECT GENERIC-OBJECTS>

<OBJECT LOCAL-GLOBALS>

<OBJECT PLAYER
    (DESC "you")
    (SYNONYM ME MYSELF)
    (FLAGS NARTICLEBIT PLURALBIT PERSONBIT TOUCHBIT)
    (CAPACITY -1)
    (ACTION PLAYER-F)>

;"Action handler for the player."
<ROUTINE PLAYER-F ()
    <COND (<N==? ,PLAYER ,PRSO>
           <RFALSE>)
          (<VERB? EXAMINE>
           <PRINTR "You look like you're up for an adventure.">)>>
