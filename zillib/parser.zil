"Library header"

<SETG ZILLIB-VERSION "J5">

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

<IFFLAG
    (DEBUG
     <GLOBAL TRACE-LEVEL 0>
     <GLOBAL TRACE-INDENT 0>

     <DEFMAC TRACE ('N "ARGS" A)
         <FORM COND <LIST <FORM G=? ',TRACE-LEVEL .N>
                          '<PRINT-TRACE-INDENT>
                          <FORM TELL !.A>>
                    '(ELSE T)>>

     <DEFMAC TRACE-DO ('N "ARGS" A)
         <FORM COND <LIST <FORM G=? ',TRACE-LEVEL .N> !.A>
                    '(ELSE T)>>

     <ROUTINE PRINT-TRACE-INDENT ()
         <OR ,TRACE-INDENT <RETURN>>
         <DO (I 1 ,TRACE-INDENT)
             <TELL "  ">>>

     <DEFMAC TRACE-IN () '<INC TRACE-INDENT>>
     <DEFMAC TRACE-OUT () '<DEC TRACE-INDENT>>
    )
    (ELSE
     <DEFMAC TRACE ("ARGS" A) T>
     <DEFMAC TRACE-DO ("ARGS" A) T>
     <DEFMAC TRACE-IN () T>
     <DEFMAC TRACE-OUT () T>)>

"Global variables"

<GLOBAL HERE <>>                   ;"Player's location"
<GLOBAL SCORE 0>                   ;"Score, or hours in V3 time games"
<GLOBAL MOVES 0>                   ;"Turn count, or minutes in V3 time games"

<CONSTANT IQUEUE <ITABLE 20>>      ;"Interrupt queue"
<GLOBAL IQ-LENGTH 0>               ;"Number of slots used in IQUEUE"
<CONSTANT TEMPTABLE <ITABLE 50>>   ;"Temporary table for miscellaneous use"
<GLOBAL STANDARD-WAIT 4>           ;"TODO: Make STANDARD-WAIT a constant"
<GLOBAL USAVE 0>                   ;"TODO: Fix USAVE"
<GLOBAL HERE-LIT T>                ;"Whether the player's location is lit"

;"Verbosity modes (controlled by player)"
<CONSTANT SUPERBRIEF 0>
<CONSTANT BRIEF 1>
<CONSTANT VERBOSE 2>
<GLOBAL MODE ,BRIEF>

;"Report modes (automatically set)"
<CONSTANT SHORT-REPORT 1>
<CONSTANT LONG-REPORT 2>
<GLOBAL REPORT-MODE ,LONG-REPORT>

<DEFMAC SHORT-REPORT? ()
    '<=? ,REPORT-MODE ,SHORT-REPORT>>

"Extensions for TELL"

<ADD-TELL-TOKENS
    T *                  <PRINT-DEF .X>
    A *                  <PRINT-INDEF .X>
    CT *                 <PRINT-CDEF .X>
    CA *                 <PRINT-CINDEF .X>
    NOUN-PHRASE *        <PRINT-NOUN-PHRASE .X>
    OBJSPEC *            <PRINT-OBJSPEC .X>
    SYNTAX-LINE *        <PRINT-SYNTAX-LINE .X>
    WORD *               <PRINT-WORD .X>
    MATCHING-WORD * * *  <PRINT-MATCHING-WORD .X .Y .Z>>

"Version considerations: certain values are bytes on V3 but words on all
other versions. These macros let us write the same code for all versions."

<VERSION?
    (ZIP
        <DEFMAC GET/B ('T 'O) <FORM GETB .T .O>>
        <DEFMAC PUT/B ('T 'O 'V) <FORM PUTB .T .O .V>>
        <DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PBTBL? .O .P .V>>
        <DEFMAC IN-B/WTBL? ('T 'C 'V) <FORM IN-BTBL? .T .C .V>>)
    (ELSE
        <DEFMAC GET/B ('T 'O) <FORM GET .T .O>>
        <DEFMAC PUT/B ('T 'O 'V) <FORM PUT .T .O .V>>
        <DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PWTBL? .O .P .V>>
        <DEFMAC IN-B/WTBL? ('T 'C 'V) <FORM IN-WTBL? .T .C .V>>)>

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
<PROPDEF PRONOUN <>>
<PROPDEF THINGS <>>
<PROPDEF GENERIC <>>

"Parser"

<CONSTANT READBUF-SIZE 100>
<DEFINE MAKE-READBUF () <ITABLE NONE ,READBUF-SIZE (BYTE)>>

<CONSTANT LEXBUF-SIZE 59>
<DEFINE MAKE-LEXBUF () <ITABLE ,LEXBUF-SIZE (LEXV) 0 #BYTE 0 #BYTE 0>>

;"We need to use up to three sets of input buffers to parse an action before
  performing it. These sets are prefixed with KBD, EDIT, and CONT.

    >MAN, OPEN DOOR

  The command is read into KBD, then copied into EDIT and expanded, then
  copied into CONT.

  KBD:  MAN, OPEN DOOR
  EDIT: ,TELL MAN . OPEN DOOR
  CONT: ,TELL MAN . OPEN DOOR

    Which do you mean, the tall man or the short man?
    >TLL

  The answer to the question is read into KBD, then copied into EDIT.

  KBD:  TLL
  EDIT: TLL
  CONT: ,TELL MAN . OPEN DOOR

    I don't know the word 'tll'.
    >OOPS TALL

  The correction command is read into KBD, and the word in EDIT is corrected.

  KBD:  OOPS TALL
  EDIT: TALL
  CONT: ,TELL MAN . OPEN DOOR

    The tall man opens the door."

<CONSTANT KBD-READBUF <MAKE-READBUF>>
<CONSTANT KBD-LEXBUF <MAKE-LEXBUF>>
<CONSTANT EDIT-READBUF <MAKE-READBUF>>
<CONSTANT EDIT-LEXBUF <MAKE-LEXBUF>>
<CONSTANT CONT-READBUF <MAKE-READBUF>>
<CONSTANT CONT-LEXBUF <MAKE-LEXBUF>>

<GLOBAL READBUF KBD-READBUF>
<GLOBAL LEXBUF KBD-LEXBUF>

<DEFMAC ACTIVATE-BUFS (PREFIX)
    <FORM BIND '()
        <FORM SETG READBUF <FORM GVAL <PARSE <STRING .PREFIX "-READBUF">>>>
        <FORM SETG LEXBUF <FORM GVAL <PARSE <STRING .PREFIX "-LEXBUF">>>>>>

<DEFMAC COPY-TO-BUFS (PREFIX)
    <FORM BIND '()
        <FORM COPY-READBUF ',READBUF <FORM GVAL <PARSE <STRING .PREFIX "-READBUF">>>>
        <FORM COPY-LEXBUF ',LEXBUF <FORM GVAL <PARSE <STRING .PREFIX "-LEXBUF">>>>>>

<CONSTANT P1MASK 3>

<GLOBAL WINNER PLAYER>

<GLOBAL PRSA <>>
<GLOBAL PRSO <>>
<GLOBAL PRSI <>>
<GLOBAL PRSO-DIR <>>

<DEFMAC VERB? ("ARGS" A "AUX" O)
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
<CONSTANT L-SCENERY 128>

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
    <SET F <GETB .W ,VOCAB-FL>>
    <SET F <COND (<BTST .F .PS>
                  <COND (<L? .P1 0>
                         <RTRUE>)
                        (<==? <BAND .F ,P1MASK> .P1>
                         <GETB .W ,VOCAB-V1>)
                        (ELSE <GETB .W ,VOCAB-V2>)>)>>
    .F>

;"Gets the word at the given index in LEXBUF.

Args:
  N: The index, starting at 1.

Returns:
  A pointer to the vocab word, or 0 if the word at the given index was
  unrecognized."
<ROUTINE GETWORD? (N "AUX" R)
    <SET R <GET ,LEXBUF <- <* .N 2> 1>>>
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

<GLOBAL P-LEN 0>            ;"Number of words in the command"
<GLOBAL P-V <>>             ;"Verb number"
<GLOBAL P-V-WORD <>>        ;"Verb word pointer"
<GLOBAL P-V-WORDN 0>        ;"Verb word number; 0 = invalid, use P-V-WORD instead"
<GLOBAL P-NOBJ 0>           ;"Number of noun phrases"
<GLOBAL P-P1 <>>            ;"Prep number before first NP"
<GLOBAL P-P2 <>>            ;"Prep number before second NP"
<GLOBAL P-SYNTAX <>>        ;"Matched syntax line pointer"
<GLOBAL P-CONT 0>           ;"Word number in held buffer where next parse should resume;
                              0 or less = get fresh input"

"Structure type for OOPS"

<DEFSTRUCT OOPS-RECORD (TABLE ('NTH ZGET) ('PUT ZPUT) ('START-OFFSET 0))
    (OOPS-O-REASON FIX)     ;"Value of P-O-REASON before parsing the failed command"
    (OOPS-WINNER <OR OBJECT FALSE>)
                            ;"Value of WINNER before parsing the failed command"
    (OOPS-WN FIX 'OFFSET 4 'NTH GETB 'PUT PUTB)
                            ;"Word number in EDIT buffer to be corrected by OOPS"
    (OOPS-CONT FIX 'OFFSET 5 'NTH GETB 'PUT PUTB)
                            ;"Value of P-CONT before parsing the failed command">

<CONSTANT P-OOPS-DATA
    <MAKE-OOPS-RECORD 'OOPS-RECORD <TABLE 0 0 <BYTE 0> <BYTE 0>>>>

<MAPF <>
    <FUNCTION (FIELD)
        <EVAL <FORM DEFMAC <PARSE <STRING "P-" <SPNAME .FIELD>>> '("ARGS" A)
                    <FORM FORM .FIELD '',P-OOPS-DATA '!.A>>>>
    '(OOPS-WN OOPS-CONT OOPS-O-REASON OOPS-WINNER)>

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
    <ROUTINE PRINT-NOUN-PHRASE (NP "AUX" CNT F S)
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
>

<IFFLAG (<OR DEBUG DEBUGGING-VERBS>
         <VERSION?
             (ZIP
                 <DEFMAC PRINT-ADJ ('A)
                     <FORM PRINT-MATCHING-WORD .A ,PS?ADJECTIVE ,P1?ADJECTIVE>>)
             (ELSE
                 <DEFMAC PRINT-ADJ ('A)
                     <FORM TELL B .A>>)>
)>

<IFFLAG (<OR DEBUG DEBUGGING-VERBS>
         <ROUTINE PRINT-MATCHING-WORD (V PS P1 "AUX" W CNT SIZE)
             <COND (<0? .V> <TELL "---"> <RTRUE>)>
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

"Extra noun phrase for temporary use"
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
"Extra objects for temporary use"
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
    (PST-WINNER OBJECT)
    (PST-LEN BYTE 'OFFSET 20 'NTH GETB 'PUT PUTB)
    (PST-V BYTE 'OFFSET 21 'NTH GETB 'PUT PUTB)
    (PST-V-WORDN BYTE 'OFFSET 22 'NTH GETB 'PUT PUTB)
    (PST-NOBJ BYTE 'OFFSET 23 'NTH GETB 'PUT PUTB)
    (PST-PRSO-DIR BYTE 'OFFSET 24 'NTH GETB 'PUT PUTB)
    (PST-PRSA BYTE 'OFFSET 25 'NTH GETB 'PUT PUTB)>

<DEFINE PARSER-RESULT ()
    <MAKE-PARSER-RESULT
        'PARSER-RESULT <ITABLE 26 (BYTE)>
        'PST-PRSOS <PRSTBL>
        'PST-PRSIS <PRSTBL>
        'PST-READBUF <MAKE-READBUF>
        'PST-LEXBUF <MAKE-LEXBUF>>>

<CONSTANT AGAIN-STORAGE <PARSER-RESULT>>
<CONSTANT TEMP-PARSER-RESULT <PARSER-RESULT>>

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
    <PST-WINNER .DEST ,WINNER>
    <PST-LEN .DEST ,P-LEN>
    <PST-V .DEST ,P-V>
    <PST-V-WORDN .DEST ,P-V-WORDN>
    <PST-NOBJ .DEST ,P-NOBJ>
    <PST-PRSO-DIR .DEST <AND ,PRSO-DIR ,PRSO>>
    <PST-PRSA .DEST ,PRSA>>

<ROUTINE RESTORE-PARSER-RESULT (SRC "AUX" L (OW ,WINNER))
    <SETG P-V-WORD <PST-V-WORD .SRC>>
    <SETG P-P1 <PST-P1 .SRC>>
    <SETG P-P2 <PST-P2 .SRC>>
    <SETG P-SYNTAX <PST-SYNTAX .SRC>>
    <COPY-PRSTBL <PST-PRSOS .SRC> ,P-PRSOS>
    <COPY-PRSTBL <PST-PRSIS .SRC> ,P-PRSIS>
    <SETG P-NUMBER <PST-NUMBER .SRC>>
    <COPY-LEXBUF <PST-LEXBUF .SRC> ,LEXBUF>
    <COPY-READBUF <PST-READBUF .SRC> ,READBUF>
    <SETG WINNER <PST-WINNER .SRC>>
    <SETG P-LEN <PST-LEN .SRC>>
    <SETG P-V <PST-V .SRC>>
    <SETG P-V-WORDN <PST-V-WORDN .SRC>>
    <SETG P-NOBJ <PST-NOBJ .SRC>>
    <SETG PRSO-DIR <PST-PRSO-DIR .SRC>>
    <SETG PRSA <PST-PRSA .SRC>>
    ;"Set variables calculated from these"
    <COND (,PRSO-DIR
           <SETG PRSO ,PRSO-DIR>
           <SETG PRSO-DIR T>)
          (<OR <L? ,P-NOBJ 1>
               <0? <SET L <GETB ,P-PRSOS 0>>>>
           <SETG PRSO <>>)
          (<1? .L>
           <SETG PRSO <GET/B ,P-PRSOS 1>>)
          (ELSE <SETG PRSO ,MANY-OBJECTS>)>
    <COND (<OR <L? ,P-NOBJ 2>
               <0? <SET L <GETB ,P-PRSIS 0>>>>
           <SETG PRSI <>>)
          (<1? .L>
           <SETG PRSI <GET/B ,P-PRSIS 1>>)
          (ELSE <SETG PRSI ,MANY-OBJECTS>)>
    <COND (<N=? ,WINNER .OW <>>
           <SETG HERE <LOC ,WINNER>>
           <SETG HERE-LIT <SEARCH-FOR-LIGHT>>)>>

"Orphaning"
<INSERT-FILE "orphan">

"Pseudo-Objects"
<INSERT-FILE "pseudo">

"Pronouns"
<INSERT-FILE "pronouns">

<PRONOUN IT (X)
    <NOT <OR <=? .X ,WINNER>
             <=? .X ,MANY-OBJECTS>
             <FSET? .X ,PERSONBIT>
             <FSET? .X ,PLURALBIT>>>>

<PRONOUN THEM (X)
    <AND <N=? .X ,WINNER>
         <OR <=? .X ,MANY-OBJECTS>
             <FSET? .X ,PLURALBIT>>>>

<PRONOUN HIM (X)
    <AND <N=? .X ,WINNER>
         <FSET? .X ,PERSONBIT>
         <NOT <FSET? .X ,FEMALEBIT>>
         <NOT <FSET? .X ,PLURALBIT>>>>

<PRONOUN HER (X)
    <AND <N=? .X ,WINNER>
         <FSET? .X ,PERSONBIT>
         <FSET? .X ,FEMALEBIT>
         <NOT <FSET? .X ,PLURALBIT>>>>

<FINISH-PRONOUNS>

"Buzzwords"
<BUZZ A AN AND ANY ALL EVERY EVERYTHING BUT EXCEPT OF ONE THE THEN UNDO OOPS \. \, \">

"Parser entry points"

;"Reads a command and processes it, repeating forever.

The behavior of MAIN-LOOP can be extended by overriding macros such as HOOK-BEFORE-PARSER.
These extensions will be respected by other code that simulates the main loop, e.g. V-WAIT."
<ROUTINE MAIN-LOOP ()
    <REPEAT () <MAIN-LOOP-ITERATION>>>

<DEFMAC MAIN-LOOP-ITERATION ()
    '<BIND ()
        <COND (<MAIN-LOOP-PARSER>
               <MAIN-LOOP-HANDLE-COMMAND>)>
        <MAIN-LOOP-END-OF-ITERATION>>>

<DEFMAC WITH-HOOK (NAME:ATOM 'EXPR "AUX" (RA ?RESULT))
    <FORM BIND (.RA)
        <FORM <PARSE <STRING "HOOK-BEFORE-" <SPNAME .NAME>>>>
        <FORM SET .RA .EXPR>
        <FORM <PARSE <STRING "HOOK-AFTER-" <SPNAME .NAME>>> .RA>
        <FORM LVAL .RA>>>

<DEFAULT-DEFINITION MAIN-LOOP-PARSER
    <DEFMAC MAIN-LOOP-PARSER ()
        '<WITH-HOOK PARSER <PARSER>>>>

<DEFAULT-DEFINITION MAIN-LOOP-HANDLE-COMMAND
    <DEFMAC MAIN-LOOP-HANDLE-COMMAND ()
        '<BIND ()
           <MAIN-LOOP-PERFORM>
           <MAIN-LOOP-ADVANCE-TIME>
           <MAIN-LOOP-END-OF-COMMAND>>>>

<DEFAULT-DEFINITION MAIN-LOOP-PERFORM
    <DEFMAC MAIN-LOOP-PERFORM ()
        '<WITH-HOOK PERFORM <PERFORM ,PRSA ,PRSO ,PRSI>>>>

<DEFAULT-DEFINITION MAIN-LOOP-ADVANCE-TIME
    <DEFMAC MAIN-LOOP-ADVANCE-TIME ()
        '<COND (<NOT <GAME-VERB?>>
                <WITH-HOOK M-END <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
                <WITH-HOOK CLOCKER <CLOCKER>>)>>>

<DEFAULT-DEFINITION MAIN-LOOP-END-OF-COMMAND
    <DEFMAC MAIN-LOOP-END-OF-COMMAND ()
        '<BIND ()
            <SETG HERE <LOC ,WINNER>>
            <HOOK-END-OF-COMMAND>>>>

<DEFAULT-DEFINITION MAIN-LOOP-END-OF-ITERATION
    <DEFMAC MAIN-LOOP-END-OF-ITERATION ()
        '<HOOK-END-OF-ITERATION>>>

;"Dummy implementations of MAIN-LOOP's BEFORE hooks."
<DEFAULT-DEFINITION HOOK-BEFORE-PARSER
    <DEFMAC HOOK-BEFORE-PARSER () <>>>
<DEFAULT-DEFINITION HOOK-BEFORE-PERFORM
    <DEFMAC HOOK-BEFORE-PERFORM () <>>>
<DEFAULT-DEFINITION HOOK-BEFORE-M-END
    <DEFMAC HOOK-BEFORE-M-END () <>>>
<DEFAULT-DEFINITION HOOK-BEFORE-CLOCKER
    <DEFMAC HOOK-BEFORE-CLOCKER () <>>>

;"Dummy implementations of MAIN-LOOP's AFTER hooks.
  These take a single parameter, an atom naming the local variable that stores the
  function's result."
<DEFAULT-DEFINITION HOOK-AFTER-PARSER
    <DEFMAC HOOK-AFTER-PARSER (R-ATOM) <>>>
<DEFAULT-DEFINITION HOOK-AFTER-PERFORM
    <DEFMAC HOOK-AFTER-PERFORM (R-ATOM) <>>>
<DEFAULT-DEFINITION HOOK-AFTER-M-END
    <DEFMAC HOOK-AFTER-M-END (R-ATOM) <>>>
<DEFAULT-DEFINITION HOOK-AFTER-CLOCKER
    <DEFMAC HOOK-AFTER-CLOCKER (R-ATOM) <>>>

;"Dummy implementations of miscellaneous MAIN-LOOP hooks."
<DEFAULT-DEFINITION HOOK-END-OF-COMMAND
    <DEFMAC HOOK-END-OF-COMMAND () <>>>
<DEFAULT-DEFINITION HOOK-END-OF-ITERATION
    <DEFMAC HOOK-END-OF-ITERATION () <>>>

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
  P-OOPS-DATA
  P-CONT
"
<ROUTINE PARSER ("AUX" NOBJ VAL DIR DIR-WN O-R KEEP OW OH OHL)
    ;"Need to (re)initialize locals here since we use AGAIN"
    <SET OW ,WINNER>
    <SET OH ,HERE>
    <SET OHL ,HERE-LIT>
    <SET NOBJ <>>
    <SET VAL <>>
    <SET DIR <>>
    <SET DIR-WN <>>
    ;"Fill READBUF and LEXBUF"
    <COND (<L? ,P-CONT 0> <SETG P-CONT 0>)>
    <COND (,P-CONT
           <TRACE 1 "[PARSER: continuing from word " N ,P-CONT "]" CR>
           <ACTIVATE-BUFS "CONT">
           <COND (<1? ,P-CONT> <SETG P-CONT 0>)
                 (<N=? ,MODE ,SUPERBRIEF>
                  ;"Print a blank line between multiple commands"
                  <COND (<NOT <VERB? TELL>> <CRLF>)>)>)
          (ELSE
           <TRACE 1 "[PARSER: fresh input]" CR>
           <RESET-WINNER>
           <SETG HERE <LOC ,WINNER>>
           <SETG HERE-LIT <SEARCH-FOR-LIGHT>>
           <READLINE T>)>

    <IF-DEBUG <SETG TRACE-INDENT 0>>
    <TRACE-DO 1 <DUMPBUFS> ;<DUMPLINE>>
    <TRACE-IN>

    <SETG P-LEN <GETB ,LEXBUF 1>>
    <COND (<0? ,P-LEN>
           <TELL "..." CR>
           <SETG P-CONT 0>
           <RFALSE>)>

    ;"Save undo state unless this looks like an undo command"
    <IF-UNDO
        <COND (<AND <G=? ,P-LEN 1>
                    <=? <GETWORD? 1> ,W?UNDO>
                    <OR <1? ,P-LEN>
                        <=? <GETWORD? 2> ,W?\. ,W?THEN>>>)
              (ELSE
               <TRACE 4 "[saving for UNDO]" CR>
               <BIND ((RES <ISAVE>))
                   <COND (<=? .RES 2>
                          <TELL "Previous turn undone." CR CR>
                          <SETG WINNER .OW>
                          <SETG HERE .OH>
                          <SETG HERE-LIT .OHL>
                          <V-LOOK>
                          <SETG P-CONT 0>
                          <AGAIN>)
                         (ELSE
                          <SETG USAVE .RES>)>>)>>

    <COND (<0? ,P-CONT>
           ;"Handle OOPS"
           <COND (<AND ,P-LEN <=? <GETWORD? 1> ,W?OOPS>>
                  <COND (<=? ,P-LEN 2>
                         <COND (<P-OOPS-WN>
                                <TRACE 2 "[handling OOPS]" CR>
                                <HANDLE-OOPS 2>
                                <SETG P-LEN <GETB ,LEXBUF 1>>
                                <TRACE-DO 1 <DUMPLINE>>)
                               (ELSE
                                <TELL "Nothing to correct." CR>
                                <RFALSE>)>)
                        (<=? ,P-LEN 1>
                         <TELL "It's OK." CR>
                         <RFALSE>)
                        (ELSE
                         <TELL "You can only correct one word at a time." CR>
                         <RFALSE>)>)>)>

    <SET KEEP 0>
    <P-OOPS-WN 0>
    <P-OOPS-CONT 0>
    <P-OOPS-O-REASON ,P-O-REASON>

    <COND (<0? ,P-CONT>
           ;"Save command in edit buffer for OOPS"
           <COND (<N=? ,READBUF ,EDIT-READBUF>
                  <COPY-TO-BUFS "EDIT">
                  <ACTIVATE-BUFS "EDIT">)>
           ;"Handle an orphan response, which may abort parsing or ask us to skip steps"
           <COND (<ORPHANING?>
                  <SET O-R <HANDLE-ORPHAN-RESPONSE>>
                  <COND (<N=? .O-R ,O-RES-NOT-HANDLED>
                         <SETG WINNER .OW>
                         <SETG HERE .OH>
                         <SETG HERE-LIT .OHL>)>
                  <COND (<=? .O-R ,O-RES-REORPHANED>
                         <TRACE-OUT>
                         <RFALSE>)
                        (<=? .O-R ,O-RES-FAILED>
                         <SETG P-O-REASON <>>
                         <TRACE-OUT>
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
           ;"If we aren't handling this command as an orphan response, convert it if needed
             and copy it to CONT bufs"
           <COND (<NOT .O-R>
                  ;"Translate order syntax (HAL, OPEN THE POD BAY DOOR or
                    TELL HAL TO OPEN THE POD BAY DOOR) into multi-command syntax
                    (\,TELL HAL THEN OPEN THE POD BAY DOOR)."
                  <COND (<CONVERT-ORDER-TO-TELL?>
                         <SETG P-LEN <GETB ,LEXBUF 1>>)>)>)>

    ;"Identify parts of speech, parse noun phrases"
    <COND (<N=? .O-R ,O-RES-SET-NP ,O-RES-SET-PRSTBL>
           <SETG P-V <>>
           <SETG P-NOBJ 0>
           <CLEAR-NOUN-PHRASE ,P-NP-DOBJ>
           <CLEAR-NOUN-PHRASE ,P-NP-IOBJ>
           <SETG P-P1 <>>
           <SETG P-P2 <>>
           ;"Identify the verb, prepositions, and noun phrases"
           <REPEAT ((I <OR ,P-CONT 1>) W V)
               <COND (<G? .I ,P-LEN>
                      ;"Reached the end of the command"
                      <SETG P-CONT 0>
                      <RETURN>)
                     (<NOT <OR <SET W <GETWORD? .I>>
                               <AND <PARSE-NUMBER? .I> <SET W ,W?\,NUMBER>>>>
                      ;"Word not in vocabulary"
                      <STORE-OOPS .I>
                      <SETG P-CONT 0>
                      <TELL "I don't know the word \"" WORD .I "\"." CR>
                      <RFALSE>)
                     (<=? .W ,W?THEN ,W?\.>
                      ;"End of command, maybe start of a new one"
                      <TRACE 3 "['then' word " N .I "]" CR>
                      <SETG P-CONT <+ .I 1>>
                      <COND (<G? ,P-CONT ,P-LEN> <SETG P-CONT 0>)
                            (ELSE <COPY-TO-BUFS "CONT">)>
                      <RETURN>)
                     (<AND <NOT ,P-V>
                           <SET V <WORD? .W VERB>>
                           <OR <NOT .DIR> <=? .V ,ACT?WALK>>>
                      ;"Found the verb"
                      <SETG P-V-WORD .W>
                      <SETG P-V-WORDN .I>
                      <SETG P-V .V>
                      <TRACE 3 "[verb word " N ,P-V-WORDN " '" B ,P-V-WORD "' = " N ,P-V "]" CR>)
                     (<AND <NOT .DIR>
                           <EQUAL? ,P-V <> ,ACT?WALK>
                           <SET VAL <WORD? .W DIRECTION>>>
                      ;"Found a direction"
                      <SET DIR .VAL>
                      <SET DIR-WN .I>
                      <TRACE 3 "[got a direction]" CR>)
                     (<SET VAL <CHKWORD? .W ,PS?PREPOSITION 0>>
                      ;"Found a preposition"
                      ;"Only keep the first preposition for each object"
                      <COND (<AND <==? .NOBJ 0> <NOT ,P-P1>>
                             <TRACE 3 "[P1 word " N .I " '" B .W "' = " N .VAL "]" CR>
                             <SETG P-P1 .VAL>)
                            (<AND <==? .NOBJ 1> <NOT ,P-P2>>
                             <TRACE 3 "[P2 word " N .I " '" B .W "' = " N .VAL "]" CR>
                             <SETG P-P2 .VAL>)>)
                     (<STARTS-NOUN-PHRASE? .W>
                      ;"Found a noun phrase"
                      <SET NOBJ <+ .NOBJ 1>>
                      <TRACE 3 "[NP start word " N .I ", now NOBJ=" N .NOBJ "]" CR>
                      <TRACE-IN>
                      <COND (<==? .NOBJ 1>
                             ;"If we found a direction earlier, try it as a preposition instead"
                             ;"This fixes GO IN BUILDING (vs. GO IN)"
                             <COND (<AND .DIR
                                         ,P-V
                                         <NOT ,P-P1>
                                         <SET V <GETWORD? .DIR-WN>>
                                         <SET VAL <CHKWORD? .V ,PS?PREPOSITION 0>>>
                                    <TRACE 3 "[revising direction word " N .DIR-WN
                                             " as P1: '" B .V "' = " N .VAL "]" CR>
                                    <SETG P-P1 .VAL>
                                    <SET DIR <>>
                                    <SET DIR-WN <>>)>
                             <SET VAL <PARSE-NOUN-PHRASE .I ,P-NP-DOBJ>>)
                            (<==? .NOBJ 2>
                             <SET VAL <PARSE-NOUN-PHRASE .I ,P-NP-IOBJ>>)
                            (ELSE
                             <SETG P-CONT 0>
                             <TELL "That sentence has too many objects." CR>
                             <RFALSE>)>
                      <TRACE 3 "[PARSE-NOUN-PHRASE returned " N .VAL "]" CR>
                      <TRACE-OUT>
                      <COND (.VAL
                             <SET I .VAL>
                             <AGAIN>)
                            (ELSE
                             <SETG P-CONT 0>
                             <RFALSE>)>)
                     (ELSE
                      ;"Unexpected word type"
                      <STORE-OOPS .I>
                      <SETG P-CONT 0>
                      <TELL "I didn't expect the word \"" WORD .I "\" there." CR>
                      <TRACE-OUT>
                      <RFALSE>)>
               <SET I <+ .I 1>>>

           <SETG P-NOBJ .NOBJ>

           <TRACE-OUT>
           <TRACE 1 "[sentence: V=" MATCHING-WORD ,P-V ,PS?VERB ,P1?VERB "(" N ,P-V ") NOBJ=" N ,P-NOBJ
                 " P1=" MATCHING-WORD ,P-P1 ,PS?PREPOSITION 0 "(" N ,P-P1
                 ") DOBJS=+" N <NP-YCNT ,P-NP-DOBJ> "-" N <NP-NCNT ,P-NP-DOBJ>
                 " P2=" MATCHING-WORD ,P-P2 ,PS?PREPOSITION 0 "(" N ,P-P2
                 ") IOBJS=+" N <NP-YCNT ,P-NP-IOBJ> "-" N <NP-NCNT ,P-NP-IOBJ> "]" CR>
           <TRACE-IN>

           ;"If we have a direction and nothing else except maybe a WALK verb, it's
             a movement command."
           <COND (<AND .DIR
                       <EQUAL? ,P-V <> ,ACT?WALK>
                       <0? .NOBJ>
                       <NOT ,P-P1>
                       <NOT ,P-P2>>
                  <SETG PRSO-DIR T>
                  <SETG PRSA ,V?WALK>
                  <SETG PRSO .DIR>
                  <SETG PRSI <>>
                  <COND (<NOT <VERB? AGAIN>>
                         <TRACE 4 "[saving for AGAIN]" CR>
                         <SAVE-PARSER-RESULT ,AGAIN-STORAGE>)>
                  <TRACE-OUT>
                  <RTRUE>)>
           ;"Otherwise, a verb is required and a direction is forbidden."
           <COND (<NOT ,P-V>
                  <SETG P-CONT 0>
                  <TELL "That sentence has no verb." CR>
                  <TRACE-OUT>
                  <RFALSE>)
                 (.DIR
                  <STORE-OOPS .DIR-WN>
                  <SETG P-CONT 0>
                  <TELL "I don't understand what \"" WORD .DIR-WN "\" is doing in that sentence." CR>
                  <TRACE-OUT>
                  <RFALSE>)>
           <SETG PRSO-DIR <>>)>
    ;"Match syntax lines and objects"
    <COND (<NOT .O-R>
           <TRACE 2 "[matching syntax and finding objects, KEEP=" N .KEEP "]" CR>
           <COND (<NOT <AND <MATCH-SYNTAX> <FIND-OBJECTS .KEEP>>>
                  <TRACE-OUT>
                  <SETG P-CONT 0>
                  <RFALSE>)>)
          (<L? .KEEP 2>
           ;"We already found a syntax line last time, but we need FIND-OBJECTS to
             match at least one noun phrase."
           <TRACE 2 "[only finding objects, KEEP=" N .KEEP "]" CR>
           <COND (<NOT <FIND-OBJECTS .KEEP>>
                  <TRACE-OUT>
                  <SETG P-CONT 0>
                  <RFALSE>)>)>
    ;"Save command for AGAIN"
    <COND (<NOT <VERB? AGAIN>>
           <TRACE 4 "[saving for AGAIN]" CR>
           <SAVE-PARSER-RESULT ,AGAIN-STORAGE>)>
    ;"If successful PRSO, back up PRSO for IT"
    <SET-PRONOUNS ,PRSO ,P-PRSOS>
    <TRACE-OUT>
    <RTRUE>>

<DEFAULT-DEFINITION RESET-WINNER
    <DEFMAC RESET-WINNER ()
        '<SETG WINNER ,PLAYER>>

    <DEFMAC ORDERING? ()
        '<N=? ,WINNER ,PLAYER>>>

;"Stores WN and P-CONT in P-OOPS-WN/CONT, and copies LEXBUF/READBUF to EDIT-LEXBUF/READBUF if needed."
<ROUTINE STORE-OOPS (WN)
    <COND (<N=? ,LEXBUF ,EDIT-LEXBUF>
           <COPY-TO-BUFS "EDIT">)>
    ;"NOTE: P-OOPS-O-REASON is not set here."
    <P-OOPS-CONT ,P-CONT>
    <P-OOPS-WINNER ,WINNER>
    <P-OOPS-WN .WN>>

;"Replaces word P-OOPS-WN in EDIT-LEXBUF (and -READBUF) with word N from the active buffer, then
  sets the active buffer to the held buffer."
<ROUTINE HANDLE-OOPS (N "AUX" W WN SS SL DS DL BL MAX DELTA
                      (LBUF ,EDIT-LEXBUF) (RBUF ,EDIT-READBUF))
    <SET W <GETWORD? .N>>
    ;"Copy word into LEXBUF"
    <SET WN <P-OOPS-WN>>
    <PUT .LBUF <- <* .WN 2> 1> .W>
    ;"Copy word into READBUF"
    <SET SS <GETB ,LEXBUF <+ <* .N 4> 1>>>
    <SET SL <GETB ,LEXBUF <* .N 4>>>
    <SET DS <GETB .LBUF <+ <* .WN 4> 1>>>
    <SET DL <GETB .LBUF <* .WN 4>>>
    <PUTB .LBUF <* .WN 4> .SL>
    <COND (<L? .SL .DL>
           ;"Copy the new word and overwrite the end of the old one with spaces"
           <COPY-TABLE-B <REST ,READBUF .SS> <REST .RBUF .DS> .SL>
           <SET MAX <- <+ .DS .DL> 1>>
           <DO (I <+ .SL 1> .MAX)
               <PUTB .RBUF .I !\ >>)
          (<G? .SL .DL>
           ;"Shift the rest of the buffer up to make room"
           <SET BL <READBUF-LENGTH .RBUF>>
           <VERSION? (ZIP <SET BL <+ .BL 1>>)>
           <SET DELTA <- .SL .DL>>
           <COND (<G=? <+ .BL .DELTA> ,READBUF-SIZE>
                  <SET BL <- ,READBUF-SIZE .DELTA 1>>)>
           <DO (I .BL .DS -1)
               <PUTB .RBUF <+ .I .DELTA> <GETB .RBUF .I>>>
           <COPY-TABLE-B <REST ,READBUF .SS> <REST .RBUF .DS> .SL>
           ;"Update pointers to subsequent words"
           <SET MAX <GETB .LBUF 1>>
           <COND (<L? .N .MAX>
                  <DO (I <+ .N 1> .MAX)
                      <PUTB .LBUF
                            <+ <* .I 4> 1>
                            <+ <GETB .LBUF <+ <* .I 4> 1>> .DELTA>>>)>)>
    ;"Activate held buffers, restore orphaning state, and clear oops state"
    <SETG READBUF .RBUF>
    <SETG LEXBUF .LBUF>
    <SETG P-O-REASON <P-OOPS-O-REASON>>
    <SETG P-CONT <P-OOPS-CONT>>
    <SETG WINNER <P-OOPS-WINNER>>
    <P-OOPS-WN 0>
    <P-OOPS-CONT 0>
    <P-OOPS-O-REASON <>>>

<ROUTINE REPLACE-HELD-WORD (N NEW-WORD "AUX" S OL NL BL MAX DELTA
                            (LBUF ,EDIT-LEXBUF) (RBUF ,EDIT-READBUF))
    <TRACE 5 "[replace held word " N .N " with '" B .NEW-WORD "']" CR>
    ;"Copy word into LEXBUF"
    <PUT .LBUF <- <* .N 2> 1> .NEW-WORD>
    ;"Copy word into READBUF"
    <DIROUT 3 ,TEMPTABLE>
    <PRINTB .NEW-WORD>
    <DIROUT -3>
    <SET S <GETB .LBUF <+ <* .N 4> 1>>>
    <SET OL <GETB .LBUF <* .N 4>>>
    <SET NL <GET ,TEMPTABLE 0>>
    <PUTB .LBUF <* .N 4> .NL>
    <COND (<L? .NL .OL>
           ;"Overwrite the end of the old word with spaces"
           <SET MAX <- <+ .S .OL> 1>>
           <DO (I <+ .S .NL 1> .MAX)
               <PUTB .RBUF .I !\ >>)
          (<G? .NL .OL>
           ;"Shift the rest of the buffer up to make room"
           <SET BL <READBUF-LENGTH .RBUF>>
           <VERSION? (ZIP <SET BL <+ .BL 1>>)>
           <SET DELTA <- .NL .OL>>
           <COND (<G=? <+ .BL .DELTA> ,READBUF-SIZE>
                  <SET BL <- ,READBUF-SIZE .DELTA 1>>)>
           <DO (I .BL .S -1)
               <PUTB .RBUF <+ .I .DELTA> <GETB .RBUF .I>>>
           ;"Update pointers to subsequent words"
           <SET MAX <GETB .LBUF 1>>
           <COND (<L? .N .MAX>
                  <DO (I <+ .N 1> .MAX)
                      <PUTB .LBUF
                            <+ <* .I 4> 1>
                            <+ <GETB .LBUF <+ <* .I 4> 1>> .DELTA>>>)>)>
    ;"Copy the new word"
    <TRACE 5 "[at char " N .S "]" CR>
    <COPY-TABLE-B <REST ,TEMPTABLE 2> <REST .RBUF .S> .NL>
    <TRACE-DO 5 <DUMPLINE T>>>

<ROUTINE INSERT-HELD-WORD (N NEW-WORD "AUX" (LBUF ,EDIT-LEXBUF) (RBUF ,EDIT-READBUF)
                           (LEN <GETB .LBUF 1>) MIN BL S MAX NL DELTA)
    <TRACE 5 "[insert '" B .NEW-WORD "' as held word " N .N "]" CR>
    <COND (<L? .N 1> <SET N 1>)
          (<G? .N .LEN> <SET N <+ .LEN 1>>)>
    <DIROUT 3 ,TEMPTABLE>
    <PRINTB .NEW-WORD>
    <DIROUT -3>
    <SET NL <GET ,TEMPTABLE 0>>
    ;"Shift LEXBUF up to make room (sacrificing the last word if needed)"
    <COND (<=? .LEN ,LEXBUF-SIZE> <SET LEN <- ,LEXBUF-SIZE 1>>)>
    <COND (<L=? .N .LEN>
           <SET MIN <* .N 2>>
           <DO (I <* .LEN 2> .MIN -2)
               <PUT .LBUF <+ .I 2> <GET .LBUF .I>>
               <PUT .LBUF <+ .I 1> <GET .LBUF <- .I 1>>>>)>
    ;"Write the new entry and set the word count"
    <COND (<G? .N .LEN> <SET S <+ <READBUF-LENGTH .RBUF> 1>>)
          (ELSE <SET S <GETB .LBUF <+ <* .N 4> 1>>>)>
    <PUT .LBUF <- <* .N 2> 1> .NEW-WORD>
    <PUTB .LBUF <* .N 4> .NL>
    <PUTB .LBUF <+ <* .N 4> 1> .S>
    <PUTB .LBUF 1 <+ .LEN 1>>
    <COND (<L=? .N .LEN>
           ;"Shift READBUF up to make room"
           <SET BL <READBUF-LENGTH .RBUF>>
           <SET DELTA <+ .NL 1>>
           <VERSION? (ZIP <SET BL <+ .BL 1>>)>
           <COND (<G=? <+ .BL .DELTA> ,READBUF-SIZE>
                  <SET BL <- ,READBUF-SIZE .DELTA 1>>)>
           <VERSION? (ZIP)
                     (ELSE <PUTB .RBUF 1 <+ .BL .DELTA>>)>
           <DO (I .BL .S -1)
               <PUTB .RBUF <+ .I .DELTA> <GETB .RBUF .I>>>
           ;"Update pointers to subsequent words"
           <SET MAX <+ .LEN 1>>
           <COND (<L? .N .MAX>
                  <DO (I <+ .N 1> .MAX)
                      <PUTB .LBUF
                            <+ <* .I 4> 1>
                            <+ <GETB .LBUF <+ <* .I 4> 1>> .DELTA>>>)>)>
    ;"Write word into READBUF, with space before/after as appropriate"
    <TRACE 5 "[at char " N .S "]" CR>
    <COPY-TABLE-B <REST ,TEMPTABLE 2> <REST .RBUF .S> .NL>
    <PUTB .RBUF
          <COND (<G? .N .LEN> <- .S 1>) (ELSE <+ .S .NL>)>
          !\ >
    <TRACE-DO 5 <DUMPLINE T>>>

;"Checks whether the command is an order, and if so, converts it to multi-command syntax
  (\,TELL <actor> THEN <command>).

Sets:
  HELD-READBUF
  HELD-LEXBUF

Returns:
  True if the command was converted."
<ROUTINE CONVERT-ORDER-TO-TELL? ("AUX" P W)
    <COND (<L? ,P-LEN 2> <RFALSE>)
          (<AND <STARTS-NOUN-PHRASE? <GETWORD? 1>>
                <SET P <PARSE-NOUN-PHRASE 1 ,P-NP-XOBJ T>>
                <L? .P ,P-LEN>
                <=? <GETWORD? .P> ,W?COMMA>
                <OR <CHKWORD? <SET W <GETWORD? <+ .P 1>>> ,PS?VERB>
                    <CHKWORD? .W ,PS?DIRECTION>>>
           <TRACE 2 "[got ACTOR, VERB order syntax]" CR>
           <TRACE-IN>
           <REPLACE-HELD-WORD .P ,W?.>
           <INSERT-HELD-WORD 1 ,W?\,TELL>
           <TRACE-OUT>
           <RTRUE>)
          (<AND <=? <GETWORD? 1> ,W?TELL>
                <SET P <PARSE-NOUN-PHRASE 2 ,P-NP-XOBJ T>>
                <L? .P ,P-LEN>
                <=? <GETWORD? .P> ,W?TO>
                <OR <CHKWORD? <SET W <GETWORD? <+ .P 1>>> ,PS?VERB>
                    <CHKWORD? .W ,PS?DIRECTION>>>
           <TRACE 2 "[got TELL ACTOR TO VERB order syntax]" CR>
           <TRACE-IN>
           <REPLACE-HELD-WORD .P ,W?.>
           <REPLACE-HELD-WORD 1 ,W?\,TELL>
           <TRACE-OUT>
           <RTRUE>)>>

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
    (SYNONYM \,NUMBER)
    (ACTION NUMBER-F)>

<ROUTINE NUMBER-F ()
    <COND (<VERB? EXAMINE>
           <TELL N ,P-NUMBER " is ">
           <COND (<=? ,P-NUMBER 0>
                  <TELL "zilch">)
                 (<=? ,P-NUMBER 1>
                  <TELL "the loneliest number that you'll ever do">)
                 (<=? ,P-NUMBER 2>
                  <TELL "the loneliest number since the number 1">)
                 (<=? ,P-NUMBER 3>
                  <TELL "a magic number">)
                 (<=? ,P-NUMBER 4>
                  <TELL "the only number that has the same number of characters as its value when written out in English">)
                 (<=? ,P-NUMBER 5>
                  <TELL "the only number that's part of more than one pair of twin primes">)
                 (<=? ,P-NUMBER 6>
                  <TELL "the smallest perfect number">)
                 (<=? ,P-NUMBER 7>
                  <TELL "a 1995 film directed by David Fincher">)
                 (<=? ,P-NUMBER 8>
                  <TELL "the first number that's neither prime nor semiprime">)
                 (<=? ,P-NUMBER 9>
                  <TELL "a 2009 animated film written and directed by Shane Acker">)
                 (<=? ,P-NUMBER 10>
                  <TELL "a 1979 film written, produced, and directed by Blake Edwards">)
                 (<=? ,P-NUMBER 42>
                  <TELL "the Answer to the Ultimate Question of Life, The Universe, and Everything">)
                 (<=? ,P-NUMBER 1729>
                  <TELL "a very interesting number; it is the smallest number
expressible as the sum of two cubes in two different ways">)
                 (<=? ,P-NUMBER 12345>
                  <TELL "the combination on my luggage">)
                 (<=? ,P-NUMBER -32768 32767>
                  <TELL "the ">
                  <COND (<L? ,P-NUMBER 0> <TELL "min">)
                        (ELSE <TELL "max">)>
                  <TELL "imum 16-bit signed integer">)
                 (ELSE
                  <TELL "the number between ">
                  <COND (<G? ,P-NUMBER 0>
                         <TELL N <- ,P-NUMBER 1> " and " N <+ ,P-NUMBER 1>>)
                        (ELSE
                         <TELL N <+ ,P-NUMBER 1> " and " N <- ,P-NUMBER 1>>)>)>
           <TELL ", but that's not important right now." CR>)
          (<AND <=? ,P-V-WORD ,W?TAKE> <=? ,P-NUMBER 5 10>>
           <PERFORM ,V?WAIT>)>>

<GLOBAL P-NUMBER 0>

;"Tries to parse the given word as a number.

If successful, the value is left in P-NUMBER, and the buffer is updated to
point the word to W?\,NUMBER.

Sets:
  P-NUMBER
  LEXBUF

Returns:
  True if the number was parsed and the buffer updated; otherwise false."
<ROUTINE PARSE-NUMBER? (WN "AUX" I MAX V C NEG)
    <SET I <GETB ,LEXBUF <+ <* .WN 4> 1>>>
    <SET MAX <- <+ .I <GETB ,LEXBUF <* .WN 4>>> 1>>
    <COND (<0? .MAX> <RFALSE>)>
    <COND (<=? <SET C <GETB ,READBUF .I>> !\->
           <SET NEG T>
           <AND <IGRTR? I .MAX> <RFALSE>>
           <SET C <GETB ,READBUF .I>>)>
    <PROG ()
        <COND (<AND <G=? .C !\0> <L=? .C !\9>>
               ;"Special case for -32768"
               <COND (<AND <=? .V 3276>
                           <=? .C !\8>
                           .NEG
                           <=? .I .MAX>>
                      <SET V -32768>
                      <RETURN>)>
               ;"Detect overflow"
               <COND (<AND <G=? .V 3276>
                           <OR <G? .V 3276>
                               <G? .C !\7>>>
                      <RFALSE>)>
               <SET V <+ <* .V 10> <- .C !\0>>>)
              (ELSE <RFALSE>)>
        <COND (<NOT <IGRTR? I .MAX>>
               <SET C <GETB ,READBUF .I>>
               <AGAIN>)>
        <COND (.NEG <SET V <- .V>>)>>
    <SETG P-NUMBER .V>
    <PUT ,LEXBUF <- <* .I 2> 1> ,W?\,NUMBER>
    <RETURN ,NUMBER>>

<VERSION?
    (ZIP
        ;"Copies a number of words from one table to another.

        If the tables overlap, the result is undefined.

        Args:
          SRC: A pointer to the source table.
          DEST: A pointer to the destination table.
          LEN: The number of words to copy."
        <ROUTINE COPY-TABLE (SRC DEST LEN)
            <SET LEN <- .LEN 1>>
            <DO (I 0 .LEN)
                <PUT .DEST .I <GET .SRC .I>>>>

        ;"Copies a number of bytes from one table to another.

        If the tables overlap, the result is undefined.

        Args:
          SRC: A pointer to the source table.
          DEST: A pointer to the destination table.
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
      care about the exact return value from CHKWORD?."
    <T? <OR <EQUAL? .W ,W?A ,W?AN ,W?THE ,W?ALL ,W?EVERY ,W?EVERYTHING ,W?ANY ,W?ONE>
            <CHKWORD? .W ,PS?ADJECTIVE>
            <CHKWORD? .W ,PS?OBJECT>>>>

<CONSTANT MCM-ALL 1>
<CONSTANT MCM-ANY 2>

;"Attempts to parse a noun phrase.

If the match fails, an error message may be printed.

Sets:
  P-OOPS-DATA

Uses:
  P-LEN

Args:
  WN: The 1-based word number where the noun clause starts.
  NP: A NOUN-PHRASE in which to return the parsed result.
  SILENT?: If true, don't print a message on failure, and don't set any error-related
    parser state (i.e. P-OOPS-DATA). Defaults to false.

Returns:
  If parsing is successful, returns a positive number: the number of the first word that is
  not part of the noun phrase, which will be one greater than P-LEN if the noun phrase consumes
  the rest of the command.

  If parsing fails, returns zero, prints an error message (unless SILENT? is true) and may
  leave NP in an invalid state."
<ROUTINE PARSE-NOUN-PHRASE (WN NP "OPT" (SILENT? <>) "AUX" SPEC CNT W VAL MODE ADJ NOUN BUT SPEC-WN)
    <TRACE 3 "[PARSE-NOUN-PHRASE starting at word " N .WN "]" CR>
    <TRACE-IN>

    <SET SPEC <NP-YSPEC .NP 1>>
    <NP-NCNT .NP 0>
    <REPEAT ()
        <COND
            ;"exit loop if we reached the end of the command"
            (<G? .WN ,P-LEN>
             <TRACE 4 "[end of command]" CR>
             <RETURN>)
            ;"fail if we found an unrecognized word"
            (<NOT <OR <SET W <GETWORD? .WN>>
                      <AND <PARSE-NUMBER? .WN> <SET W ,W?\,NUMBER>>>>
             <TRACE 4 "[stop at unrecognized word: " WORD .WN "]" CR>
             <COND (<NOT .SILENT?>
                    <STORE-OOPS .WN>
                    <TELL "I don't know the word \"" WORD .WN "\"." CR>)>
             <TRACE-OUT>
             <RFALSE>)
            ;"exit loop if THEN or period"
            (<EQUAL? .W ,W?THEN ,W?\.>
             <TRACE 4 "[THEN at word " N .WN "]" CR>
             <RETURN>)
            ;"recognize BUT/EXCEPT"
            (<AND <NOT .BUT> <EQUAL? .W ,W?BUT ,W?EXCEPT>>
             <TRACE 4 "[BUT at word " N .WN "]" CR>
             <COND (<OR .ADJ .NOUN>
                    <OBJSPEC-ADJ .SPEC .ADJ>
                    <OBJSPEC-NOUN .SPEC .NOUN>
                    <SET ADJ <SET NOUN <>>>
                    <SET CNT <+ .CNT 1>>)>
             <TRACE 4 "[saving " N .CNT " YSPEC(s)]" CR>
             <NP-YCNT .NP .CNT>
             <SET BUT T>
             <SET SPEC <NP-NSPEC .NP 1>>
             <SET CNT 0>)
            ;"recognize ALL/ANY/ONE"
            (<EQUAL? .W ,W?ALL ,W?EVERY ,W?EVERYTHING ,W?ANY ,W?ONE>
             <COND (<OR .MODE .ADJ .NOUN>
                    <TRACE 4 "[too late for mode change at word " N .WN "]" CR>
                    <COND (<NOT .SILENT?>
                           <TELL "You can't use \"" B .W "\" there." CR>)>
                    <TRACE-OUT>
                    <RFALSE>)>
             <SET MODE
                  <COND (<EQUAL? .W ,W?ALL ,W?EVERY ,W?EVERYTHING> ,MCM-ALL)
                        (ELSE ,MCM-ANY)>>
             <TRACE 4 "[mode change at word " N .WN ", now mode=" N .MODE "]" CR>
             <SET SPEC-WN .WN>)
            ;"match adjectives, keeping only the first"
            (<VERSION?
                (ZIP <SET VAL <WORD? .W ADJECTIVE>>)
                (ELSE <CHKWORD? <SET VAL .W> ,PS?ADJECTIVE>)>
             <TRACE 4 "[adjective '" B .W "' at word " N .WN "]" CR>
             ;"if we already have a noun, this must start a new noun phrase"
             <COND (.NOUN
                    <TRACE 4 "[terminating]" CR>
                    <RETURN>)>
             <SET SPEC-WN .WN>
             <COND
                 ;"if W can also be a noun, treat it as such if
                   it isn't followed by an adj or noun"
                 (<AND <CHKWORD? .W ,PS?OBJECT>         ;"word can be a noun"
                       <OR ;"word is at end of line"
                           <==? .WN ,P-LEN>
                           ;"next word is not adj/noun"
                           <BIND ((NW <GETWORD? <+ .WN 1>>))
                               <NOT <OR <CHKWORD? .NW ,PS?ADJECTIVE>
                                        <CHKWORD? .NW ,PS?OBJECT>>>>>>
                  <TRACE 4 "[treating it as a noun]" CR>
                  <SET NOUN .W>)
                 (<==? .CNT ,P-MAX-OBJSPECS>
                  <TRACE 4 "[already have " N .CNT " specs]" CR>
                  <COND (<NOT .SILENT?>
                         <TELL "That phrase mentions too many objects." CR>)>
                  <TRACE-OUT>
                  <RFALSE>)
                 (<NOT .ADJ>
                  <SET ADJ .VAL>)
                 (ELSE
                  <TRACE 4 "[ignoring it]" CR>)>)
            ;"match nouns, exiting the loop if we already found one"
            (<CHKWORD? .W ,PS?OBJECT>
             <TRACE 4 "[noun '" B .W "' at word " N .WN "]" CR>
             <COND (.NOUN
                    <TRACE 4 "[terminating]" CR>
                    <RETURN>)
                   (<==? .CNT ,P-MAX-OBJSPECS>
                    <TRACE 4 "[already have " N .CNT " specs]" CR>
                    <COND (<NOT .SILENT?>
                           <TELL "That phrase mentions too many objects." CR>)>
                    <TRACE-OUT>
                    <RFALSE>)
                   (ELSE
                    <SET NOUN .W>
                    <SET SPEC-WN .WN>)>)
            ;"recognize AND/comma"
            (<EQUAL? .W ,W?AND ,W?COMMA>
             <TRACE 4 "[AND at word " N .WN "]" CR>
             <COND (<OR .ADJ .NOUN>
                    <OBJSPEC-ADJ .SPEC .ADJ>
                    <OBJSPEC-NOUN .SPEC .NOUN>
                    <SET ADJ <SET NOUN <>>>
                    <SET SPEC <REST .SPEC ,P-OBJSPEC-SIZE>>
                    <SET CNT <+ .CNT 1>>
                    <TRACE 4 "[now have " N .CNT " spec(s)]" CR>)>)
            ;"recognize OF"
            (<AND <EQUAL? .W ,W?OF>
                  <L? .WN ,P-LEN>
                  <STARTS-NOUN-PHRASE? <GETWORD? <+ .WN 1>>>>
             ;"This is a hack to deal with object names consisting of multiple NPs
               joined by OF. When we see OF before a word that could start a new
               noun phrase, we forget the current noun, so SMALL PIECE OF TASTY PIE
               parses as SMALL TASTY PIE (which in turn parses as SMALL PIE)."
             <TRACE 4 "[OF at word " N .WN ", clearing noun]" CR>
             <SET NOUN <>>)
            ;"skip buzzwords"
            (<CHKWORD? .W ,PS?BUZZ-WORD>
             <TRACE 4 "[skip buzzword at word " N .WN "]" CR>
             <SET SPEC-WN .WN>)
            ;"exit loop if we found any other word type"
            (ELSE
             <TRACE 4 "[bail over type at word " N .WN "]" CR>
             <RETURN>)>
        <SET WN <+ .WN 1>>>
    ;"store final adj/noun pair"
    <COND (<OR .ADJ .NOUN>
           <OBJSPEC-ADJ .SPEC .ADJ>
           <OBJSPEC-NOUN .SPEC .NOUN>
           <SET CNT <+ .CNT 1>>
           <TRACE 4 "[finally have " N .CNT " spec(s)]" CR>)>
    ;"store phrase count and mode"
    <COND (.BUT <NP-NCNT .NP .CNT>) (ELSE <NP-YCNT .NP .CNT>)>
    <NP-MODE .NP .MODE>
    <TRACE 2 "[noun phrase parsed: " NOUN-PHRASE .NP "]" CR>
    <TRACE-OUT>
    <+ .SPEC-WN 1>>

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
    <TRACE 1 "[MATCH-SYNTAX: " N .CNT " syntaxes for verb " N ,P-V "]" CR>
    <TRACE-IN>
    <SET BEST-SCORE -999>
    <REPEAT ()
        <COND (<DLESS? CNT 0>
               ;"Out of syntax lines"
               <RETURN>)>
        <SET S <MATCH-SYNTAX-LINE? .PTR>>
        <COND (<AND .S <G? .S .BEST-SCORE>>
               <SET BEST-SCORE .S>
               <SET BEST .PTR>
               <COND (<G? .S 0> <RETURN>)>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>
    <TRACE-OUT>
    <COND (.BEST
           <SETG PRSA <GETB .BEST ,SYN-ACTION>>
           <SETG P-SYNTAX .BEST>
           <TRACE 1 "[picked line at " N .BEST " with score " N .BEST-SCORE
                    ", PRSA=" N ,PRSA "]" CR>
           <RTRUE>)
          (ELSE
           <TELL "I don't understand that sentence." CR>
           <RFALSE>)>>

<IF-DEBUG
    <ROUTINE PRINT-SYNTAX-LINE (PTR "AUX" NOBJ PREP1 PREP2 ACT)
        <SET NOBJ <GETB .PTR ,SYN-NOBJ>>
        <SET PREP1 <GETB .PTR ,SYN-PREP1>>
        <SET PREP2 <GETB .PTR ,SYN-PREP2>>
        <SET ACT <GETB .PTR ,SYN-ACTION>>
        <TELL "*">
        <COND (<G=? .NOBJ 1>
               <COND (.PREP1 <TELL " " MATCHING-WORD .PREP1 ,PS?PREPOSITION 0>)>
               <TELL " object">)>
        <COND (<G=? .NOBJ 2>
               <COND (.PREP2 <TELL " " MATCHING-WORD .PREP2 ,PS?PREPOSITION 0>)>
               <TELL " object">)>
        <TELL " (" N .NOBJ ", " N .PREP1 ", " N .PREP2 ") = " N .ACT>>>

;"Scores how well the parsed command matches a syntax line.

Args:
  PTR: The syntax line.

Returns:
  1 if it matches exactly, 0 if it cannot match, or a negative number
  if it partially matches (i.e. if it could match after inference).
  Negative numbers further below 0 indicate worse matches needing more inference."
<ROUTINE MATCH-SYNTAX-LINE? (PTR "AUX" NOBJ PREP1 PREP2 R)
    <TRACE 2 "[attempting syntax line at " N .PTR ": " SYNTAX-LINE .PTR "]" CR>
    <TRACE-IN>
    <SET NOBJ <GETB .PTR ,SYN-NOBJ>>
    <SET PREP1 <GETB .PTR ,SYN-PREP1>>
    <SET PREP2 <GETB .PTR ,SYN-PREP2>>
    <COND ;"If the object count and prepositions are all as expected,
            this is an exact match."
          (<AND <=? ,P-NOBJ .NOBJ> <=? ,P-P1 .PREP1> <=? ,P-P2 .PREP2>>
           <TRACE 2 "[exact match]" CR>
           <TRACE-OUT>
           <RTRUE>)
          ;"If object count >= expected count, this can't match."
          (<G=? ,P-NOBJ .NOBJ>
           <TRACE 2 "[DQ, no objects left to infer]" CR>
           <TRACE-OUT>
           <RFALSE>)
          ;"If either preposition is nonzero yet different from expected,
            this can't match."
          (<OR <N=? ,P-P1 .PREP1 0> <N=? ,P-P2 .PREP2 0>>
           <TRACE 2 "[DQ, wrong preposition]" CR>
           <TRACE-OUT>
           <RFALSE>)
          ;"If we have one object, and we expected a first preposition but
            didn't get it, this can't match. (If we have two objects, we
            already failed an earlier test.)"
          (<AND <1? ,P-NOBJ> .PREP1 <NOT ,P-P1>>
           <TRACE 2 "[DQ, skipped over prep1]" CR>
           <TRACE-OUT>
           <RFALSE>)
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
           <TRACE 2 "[DQ, kludge bit]" CR>
           <TRACE-OUT>
           <RFALSE>)>
    ;"We have a possible match; now score how well it matches.
      Dock one point for each object we have to infer."
    <SET R <- ,P-NOBJ .NOBJ>>
    <TRACE 3 "[base score " N .R "]" CR>
    ;"Dock an extra two points for PRSO if we have to infer a preposition also."
    <COND (<AND <NOT ,P-P1> .PREP1>
           <TRACE 3 "[-2, needs preposition on PRSO]" CR>
           <SET R <- .R 2>>)>
    ;"Dock an extra point for PRSI if we have to infer *no* preposition.
      This makes us prefer syntaxes with the direct object first
      (GIVE OBJECT TO OBJECT instead of GIVE OBJECT OBJECT)."
    <COND (<AND <=? .NOBJ 2> <NOT <OR ,P-P2 .PREP2>>>
           <TRACE 3 "[-1, no preposition on PRSI]" CR>
           <SET R <- .R 1>>)>
    <TRACE-OUT>
    .R>

<INSERT-FILE "scope">

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
    <TRACE 2 "[FIND-OBJECTS: KEEP=" N .KEEP ", syntax expects " N .SNOBJ ", we have " N ,P-NOBJ "]" CR>
    <TRACE-IN>

    ;"Direct object (PRSO)"
    <SET O <GETB ,P-SYNTAX ,SYN-OPTS1>>
    <COND (<L? .SNOBJ 1>
           <SETG PRSO <>>)
          (<L? .KEEP 1>
           <SET F <GETB ,P-SYNTAX ,SYN-FIND1>>
           <COND (<L? ,P-NOBJ 1>
                  <TRACE 3 "[gwimming PRSO]" CR>
                  <SETG PRSO
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP1>>>
                  <COND (<0? ,PRSO>
                         <WHAT-DO-YOU-WANT>
                         <ORPHAN T MISSING PRSO>
                         <TRACE-OUT>
                         <RFALSE>)
                        (ELSE
                         <SETG P-NOBJ 1>
                         <PUT/B ,P-PRSOS 1 ,PRSO>
                         <PUTB ,P-PRSOS 0 1>)>)
                 (ELSE
                  <TRACE 3 "[matching PRSO]" CR>
                  <SETG PRSO
                      <OR <AND <1? <NP-YCNT ,P-NP-DOBJ>>
                               <EXPAND-PRONOUN <OBJSPEC-NOUN <NP-YSPEC ,P-NP-DOBJ 1>> ,P-PRSOS>>
                          <MATCH-NOUN-PHRASE ,P-NP-DOBJ ,P-PRSOS <ENCODE-NOUN-BITS .F .O>>>>
                  <COND (<=? ,PRSO ,EXPAND-PRONOUN-FAILED>
                         <TRACE-OUT>
                         <RFALSE>)>)>
           <COND (<NOT ,PRSO>
                  <TRACE-OUT>
                  <RFALSE>)>)>
    <COND (<AND ,PRSO
                <NOT <OR ,PRSO-DIR
                         <AND <MANY-CHECK ,PRSO .O <>>
                              <HAVE-TAKE-CHECK-TBL ,P-PRSOS .O>>>>>
           <TRACE-OUT>
           <RFALSE>)>

    ;"Indirect object (PRSI)"
    <SET O <GETB ,P-SYNTAX ,SYN-OPTS2>>
    <COND (<L? .SNOBJ 2>
           <SETG PRSI <>>)
          (<L? .KEEP 2>
           <SET F <GETB ,P-SYNTAX ,SYN-FIND2>>
           <COND (<L? ,P-NOBJ 2>
                  <TRACE 3 "[gwimming PRSI]" CR>
                  <SETG PRSI
                      <GWIM .F .O <GETB ,P-SYNTAX ,SYN-PREP2>>>
                  <COND (<0? ,PRSI>
                         <WHAT-DO-YOU-WANT>
                         <ORPHAN T MISSING PRSI>
                         <TRACE-OUT>
                         <RFALSE>)
                        (ELSE
                         <SETG P-NOBJ 2>
                         <PUT/B ,P-PRSIS 1 ,PRSI>
                         <PUTB ,P-PRSIS 0 1>)>)
                 (ELSE
                  <TRACE 3 "[matching PRSI]" CR>
                  <SETG PRSI
                      <OR <AND <1? <NP-YCNT ,P-NP-IOBJ>>
                               <EXPAND-PRONOUN <OBJSPEC-NOUN <NP-YSPEC ,P-NP-IOBJ 1>> ,P-PRSIS>>
                          <MATCH-NOUN-PHRASE ,P-NP-IOBJ ,P-PRSIS <ENCODE-NOUN-BITS .F .O>>>>
                  <COND (<=? ,PRSI ,EXPAND-PRONOUN-FAILED>
                         <TRACE-OUT>
                         <RFALSE>)>)>
           <COND (<NOT ,PRSI>
                  <TRACE-OUT>
                  <RFALSE>)>)>
    <COND (<AND ,PRSI
                <NOT <AND <MANY-CHECK ,PRSI .O T>
                          <HAVE-TAKE-CHECK-TBL ,P-PRSIS .O>>>>
           <TRACE-OUT>
           <RFALSE>)>
    <TRACE-OUT>
    <RTRUE>>

<DEFAULT-DEFINITION WHAT-DO-YOU-WANT
    <ROUTINE WHAT-DO-YOU-WANT ("AUX" SN SP1 SP2 F)
        <SET SN <GETB ,P-SYNTAX ,SYN-NOBJ>>
        <SET SP1 <GETB ,P-SYNTAX ,SYN-PREP1>>
        <SET SP2 <GETB ,P-SYNTAX ,SYN-PREP2>>
        ;"TODO: use LONG-WORDS table for preposition words"
        <COND (<AND ,PRSO <NOT ,PRSO-DIR>>
               <SET F <GETB ,P-SYNTAX ,SYN-FIND2>>)
              (ELSE <SET F <GETB ,P-SYNTAX ,SYN-FIND1>>)>
        <COND (<AND <VERB? WALK> <NOT ,PRSO>> <TELL "Which way">)
              (<=? .F ,PERSONBIT> <TELL "Whom">)
              (ELSE <TELL "What">)>
        <TELL " do you want">
        <COND (<ORDERING?> <TELL " " T ,WINNER>)>
        <TELL " to ">
        <PRINT-VERB>
        <COND (.SP1
               <TELL " " B <GET-PREP-WORD .SP1>>)>
        <COND (<AND ,PRSO <NOT ,PRSO-DIR>>
               <TELL " " T ,PRSO>
               <COND (.SP2
                      <TELL " " B <GET-PREP-WORD .SP2>>)>)>
        <TELL "?" CR>>>

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
           <COND (<VERB? TELL>
                  <TELL "You can only address one person at a time.">)
                 (ELSE
                  <TELL "You can't use multiple ">
                  <COND (.INDIRECT? <TELL "in">)>
                  <TELL "direct objects with \"">
                  <PRINT-VERB>
                  <TELL "\".">)>
           <CRLF>
           <SETG P-CONT 0>
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
<ROUTINE HAVE-TAKE-CHECK-TBL (TBL OPTS "AUX" MAX O N ORM)
    <SET MAX <GETB .TBL 0>>
    ;"Attempt implicit take if WINNER isn't directly holding the objects"
    <COND (<BTST .OPTS ,SF-TAKE>
           <DO (I 1 .MAX)
               <COND (<SHOULD-IMPLICIT-TAKE? <GET/B .TBL .I>>
                      <TELL "[taking ">
                      <SET N <LIST-OBJECTS .TBL ,SHOULD-IMPLICIT-TAKE? <+ ,L-PRSTABLE ,L-THE>>>
                      <TELL "]" CR>
                      <REPEAT ()
                          <COND (<SHOULD-IMPLICIT-TAKE? <SET O <GET/B .TBL .I>>>
                                 <COND (<NOT <TRY-TAKE .O T>>
                                        <COND (<G? .N 1>
                                               <SET ORM ,REPORT-MODE>
                                               <SETG REPORT-MODE ,SHORT-REPORT>
                                               <TELL D .O ": ">
                                               <TRY-TAKE .O>
                                               <SETG REPORT-MODE .ORM>)
                                              (ELSE
                                               <TRY-TAKE .O>)>
                                        <RFALSE>)>)>
                          <COND (<IGRTR? I .MAX> <RETURN>)>>
                      <RETURN>)>>)>
    ;"WINNER must (indirectly) hold the objects if SF-HAVE is set"
    <COND (<BTST .OPTS ,SF-HAVE>
           <DO (I 1 .MAX)
               <COND (<FAILS-HAVE-CHECK? <GET/B .TBL .I>>
                      <TELL "You aren't holding ">
                      <LIST-OBJECTS .TBL ,FAILS-HAVE-CHECK? <+ ,L-PRSTABLE ,L-THE ,L-OR>>
                      <TELL "." CR>
                      <SETG P-CONT 0>
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
           <COND (<SHOULD-IMPLICIT-TAKE? .OBJ>
                  <TELL "[taking " T .OBJ "]" CR>
                  <COND (<NOT <TRY-TAKE .OBJ T>>
                         <TRY-TAKE .OBJ>
                         <RFALSE>)>)>)>
    ;"WINNER must (indirectly) hold the object if SF-HAVE is set"
    <COND (<BTST .OPTS ,SF-HAVE>
           <COND (<FAILS-HAVE-CHECK? .OBJ>
                  <TELL "You aren't holding " T .OBJ "." CR>
                  <SETG P-CONT 0>
                  <RFALSE>)>)>
    <RTRUE>>

;"The game can override these to change the precise conditions for TAKE and HAVE."
<DEFAULT-DEFINITION SHOULD-IMPLICIT-TAKE?
    <ROUTINE SHOULD-IMPLICIT-TAKE? (OBJ)
        <T? <AND <NOT <IN? .OBJ ,WINNER>>
                 <FSET? .OBJ ,TAKEBIT>
                 <NOT <FSET? .OBJ ,TRYTAKEBIT>>>>>>

<DEFAULT-DEFINITION FAILS-HAVE-CHECK?
    <ROUTINE FAILS-HAVE-CHECK? (OBJ)
        <NOT <HELD? .OBJ>>>>

;"Checks whether the objects listed in a table, which were part of a previous
  command, are still available to the player, and prints an error message if not.

Args:
  TBL: A PRS table.

Returns:
  True if the check passed, or false if at least one object has become unavailable
  and a message was printed."
<ROUTINE STILL-VISIBLE-CHECK (TBL "AUX" (CNT <GETB .TBL 0>))
    <TRACE 4 "[STILL-VISIBLE-CHECK: CNT=" N .CNT "]" CR>
    <OR .CNT <RTRUE>>
    <TRACE-IN>
    <DO (I 1 .CNT)
        <TRACE 4 "[considering " D <GET/B .TBL .I> "]" CR>
        <COND (<NOT <VISIBLE? <GET/B .TBL .I>>>
               <LIST-OBJECTS .TBL ,NOT-VISIBLE? <+ ,L-PRSTABLE ,L-THE ,L-CAP ,L-SUFFIX>>
               <TELL " no longer here." CR>
               <TRACE-OUT>
               <SETG P-CONT 0>
               <RFALSE>)>>
    <TRACE-OUT>
    <RTRUE>>

<ROUTINE NOT-VISIBLE? (O)
    <NOT <VISIBLE? .O>>>

;"Searches scope for a single object with the given flag set, and prints an
inference message before returning it.

The flag KLUDGEBIT is a special case that always finds ROOMS.

Args:
  BIT: The flag to search for. If zero, all objects in scope will be considered.
  OPTS: The search options to use.
  PREP: The preposition to use in the message.

Returns:
  The single object that matches, or false if zero or multiple objects match."
<ROUTINE GWIM (BIT OPTS PREP "AUX" O PW)
    ;"Special cases"
    <COND (<==? .BIT ,KLUDGEBIT>
           <TRACE 4 "[GWIM: autofilling ROOMS for kludge bit]" CR>
           <RETURN ,ROOMS>)
          (<VERB? WALK>
           <TRACE 4 "[GWIM: refusing, verb is WALK]" CR>
           <RFALSE>)>
    ;"Look for exactly one matching object, excluding WINNER"
    <TRACE 4 "[GWIM: searching scope for flag " N .BIT " opts " N .OPTS "]" CR>
    <TRACE-IN>
    <MAP-SCOPE (I [BITS .OPTS])
        <COND (<AND <N=? .I ,WINNER>
                    <OR <0? .BIT> <FSET? .I .BIT>>>
               <TRACE 4 "[considering " D .I "]" CR>
               <COND (.O
                      <TRACE 4 "[too many, bailing]" CR>
                      <TRACE-OUT>
                      <RFALSE>)
                     (ELSE
                      <TRACE 4 "[updating preference]" CR>
                      <SET O .I>)>)>>
    <TRACE-OUT>
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
  HERE

Returns:
  The matched object, or MANY-OBJECTS if multiple objects were matched,
  or false if no objects were matched."
<ROUTINE MATCH-NOUN-PHRASE (NP OUT BITS "AUX" F NY NN SPEC MODE NOUT OBITS ONOUT BEST Q)
    <SET NY <NP-YCNT .NP>>
    <SET NN <NP-NCNT .NP>>
    <SET MODE <NP-MODE .NP>>
    <SET OBITS .BITS>
    <COND (<0? .MODE>
           <SET .BITS <ORB .BITS ;"<ORB" ,SF-HELD ,SF-CARRIED ,SF-ON-GROUND ,SF-IN-ROOM ;">" >>)>
    <TRACE 3 "[MATCH-NOUN-PHRASE: NY=" N .NY " NN=" N .NN " MODE=" N .MODE
             " BITS=" N .BITS " OBITS=" N .OBITS "]" CR>
    <TRACE-IN>
    <PROG BITS-SET ()
        ;"Look for matching objects"
        <SET NOUT 0>
        <COND (<0? .NY>
               ;"ALL with no YSPECs matches all objects, or if the action is TAKE/DROP,
                 all objects with TAKEBIT/TRYTAKEBIT, skipping generic/global objects."
               <TRACE 4 "[applying ALL rules]" CR>
               <MAP-SCOPE (I [BITS .BITS])
                   <COND (<SCOPE-STAGE? GENERIC GLOBALS>)
                         (<NOT <ALL-INCLUDES? .I>>)
                         (<AND .NN <NP-EXCLUDES? .NP .I>>)
                         (<G=? .NOUT ,P-MAX-OBJECTS>
                          <TELL "[too many objects!]" CR>
                          <TRACE-OUT>
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
                   <TRACE 4 "[SPEC=" OBJSPEC .SPEC "]" CR>
                   <SET F <>>
                   <SET ONOUT .NOUT>
                   <SET BEST 1>
                   <MAP-SCOPE (I [BITS .BITS])
                       <TRACE 5 "[considering " T .I "]" CR>
                       <COND (<AND <NOT <FSET? .I ,INVISIBLE>>
                                   <SET Q <REFERS? .SPEC .I>>
                                   <G=? .Q .BEST>>
                              <TRACE 4 "[matches " T .I "(" N .I "), Q=" N .Q "]" CR>
                              <SET F T>
                              ;"Erase previous matches if this is better"
                              <COND (<G? .Q .BEST>
                                     <TRACE 4 "[clearing match list]" CR>
                                     <SET NOUT .ONOUT>
                                     <SET .BEST .Q>)>
                              <COND (<AND .NN <NP-EXCLUDES? .NP .I>>
                                     <TRACE 4 "[excluded]" CR>)
                                    (<G=? .NOUT ,P-MAX-OBJECTS>
                                     <TELL "[too many objects!]" CR>
                                     <TRACE-OUT>
                                     <RETURN>)
                                    (ELSE
                                     <TRACE 4 "[accepted]" CR>
                                     <SET NOUT <+ .NOUT 1>>
                                     <PUT/B .OUT .NOUT .I>)>)>>
                   ;"Look for a pseudo-object if we didn't find a real one."
                   <COND (<AND <NOT .F>
                               <BTST .BITS ,SF-ON-GROUND>
                               <SET Q <GETP ,HERE ,P?THINGS>>>
                          <TRACE 4 "[looking for pseudo]" CR>
                          <SET F <MATCH-PSEUDO .SPEC .Q>>
                          <COND (.F
                                 <COND (<AND .NN <NP-EXCLUDES-PSEUDO? .NP .F>>)
                                       (<G=? .NOUT ,P-MAX-OBJECTS>
                                        <TELL "[too many objects!]" CR>
                                        <TRACE-OUT>
                                        <RETURN>)
                                       (ELSE
                                        <SET NOUT <+ .NOUT 1>>
                                        <PUT/B .OUT .NOUT <MAKE-PSEUDO .F>>)>)>)>
                   <COND (<NOT .F>
                          ;"Try expanding the search if we can."
                          <COND (<N=? .BITS -1>
                                 <TRACE 4 "[expanding to ludicrous scope]" CR>
                                 <SET BITS -1>
                                 <SET OBITS -1>    ;"Avoid bouncing between <1 and >1 matches"
                                 <AGAIN .BITS-SET>)>
                          <COND (<=? ,MAP-SCOPE-STATUS ,MS-NO-LIGHT>
                                 <TELL "It's too dark to see anything here." CR>)
                                (ELSE
                                 <TELL "You don't see that here." CR>)>
                          <TRACE-OUT>
                          <RFALSE>)
                         (<G=? .NOUT ,P-MAX-OBJECTS>
                          <TRACE-OUT>
                          <RETURN>)>>)>
        ;"Check the number of objects"
        <PUTB .OUT 0 .NOUT>
        <COND (<0? .NOUT>
               ;"This means ALL matched nothing, or BUT excluded everything.
                 Try expanding the search if we can."
               <SET F <ORB .BITS ;"<ORB" ,SF-HELD ,SF-CARRIED ,SF-ON-GROUND ,SF-IN-ROOM ;">" >>
               <COND (<=? .BITS .F>
                      <TELL "There are none at all available!" CR>
                      <TRACE-OUT>
                      <RFALSE>)>
               <TRACE 4 "[expanding to reasonable scope]" CR>
               <SET BITS .F>
               <SET OBITS .F>    ;"Avoid bouncing between <1 and >1 matches"
               <AGAIN .BITS-SET>)
              (<1? .NOUT>
               <TRACE-OUT>
               <RETURN <GET/B .OUT 1>>)
              (<OR <=? .MODE ,MCM-ALL> <G? .NY 1>>
               <TRACE-OUT>
               <RETURN ,MANY-OBJECTS>)
              (<=? .MODE ,MCM-ANY>
               ;"Pick a random object"
               <PUT/B .OUT 1 <SET F <GET/B .OUT <RANDOM .NOUT>>>>
               <PUTB .OUT 0 1>
               <TELL "[" T .F "]" CR>
               <TRACE-OUT>
               <RETURN .F>)
              (ELSE
               ;"TODO: Do this check when we're matching YSPECs, so each YSPEC can be
                 disambiguated individually."
               ;"Try narrowing the search if we can."
               <COND (<N=? .BITS .OBITS>
                      <TRACE 4 "[narrowing scope to BITS=" N .OBITS "]" CR>
                      <SET BITS .OBITS>
                      <AGAIN .BITS-SET>)>
               <COND (<SET F <APPLY-GENERIC-FCN .OUT>>
                      <TRACE 4 "[GENERIC chose " T .F "]" CR>
                      <PUT/B .OUT 1 .F>
                      <PUTB .OUT 0 1>
                      <TRACE-OUT>
                      <RETURN .F>)>
               <WHICH-DO-YOU-MEAN .OUT>
               <COND (<=? .NP ,P-NP-DOBJ> <ORPHAN T AMBIGUOUS PRSO>)
                     (ELSE <ORPHAN T AMBIGUOUS PRSI>)>
               <TRACE-OUT>
               <RFALSE>)>>>

<ROUTINE ALL-INCLUDES? (OBJ)
    <NOT <OR <FSET? .OBJ ,INVISIBLE>
             <=? .OBJ ,WINNER>
             <AND <VERB? TAKE DROP>
                  <NOT <OR <FSET? .OBJ ,TAKEBIT>
                       <FSET? .OBJ ,TRYTAKEBIT>>>>>>>

<ROUTINE APPLY-GENERIC-FCN (TBL "AUX" (MAX <GETB .TBL 0>) F R)
    <DO (I 1 .MAX) (END <RFALSE>)
        <SET F <GETP <GET/B .TBL .I> ,P?GENERIC>>
        <COND (<SET R <APPLY .F .TBL>>
               <RETURN .R>)>>>

<ROUTINE WHICH-DO-YOU-MEAN (TBL)
    <TELL "Which do you mean, ">
    <LIST-OBJECTS .TBL <> <+ ,L-PRSTABLE ,L-THE ,L-OR>>
    <TELL "?" CR>>

;"Determines whether an object is excluded by a NOUN-PHRASE's NTBL.
  Note: NP may be evaluated twice."
<DEFMAC NP-INCLUDES? ('NP 'O)
    <FORM ANY-SPEC-REFERS? <FORM NP-YTBL .NP> <FORM NP-YCNT .NP> .O>>

<DEFMAC NP-INCLUDES-PSEUDO? ('NP 'PDO)
    <FORM ANY-SPEC-REFERS-PSEUDO? <FORM NP-YTBL .NP> <FORM NP-YCNT .NP> .PDO>>

<ROUTINE ANY-SPEC-REFERS? (TBL N O)
    <COND (<0? .N> <RFALSE>)>
    <DO (I 1 .N)
        <COND (<REFERS? .TBL .O> <RTRUE>)>
        <SET TBL <+ .TBL ,P-OBJSPEC-SIZE>>>
    <RFALSE>>

<ROUTINE ANY-SPEC-REFERS-PSEUDO? (TBL N PDO)
    <COND (<0? .N> <RFALSE>)>
    <DO (I 1 .N)
        <COND (<REFERS-PSEUDO? .TBL .PDO> <RTRUE>)>
        <SET TBL <+ .TBL ,P-OBJSPEC-SIZE>>>
    <RFALSE>>

;"Determines whether an object is excluded by a NOUN-PHRASE's NTBL.
  Note: NP may be evaluated twice."
<DEFMAC NP-EXCLUDES? ('NP 'O)
    <FORM ANY-SPEC-REFERS? <FORM NP-NTBL .NP> <FORM NP-NCNT .NP> .O>>

<DEFMAC NP-EXCLUDES-PSEUDO? ('NP 'PDO)
    <FORM ANY-SPEC-REFERS-PSEUDO? <FORM NP-NTBL .NP> <FORM NP-NCNT .NP> .PDO>>

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

;"Attempts to locate a word in a property table.

Args:
  O: The object containing the property.
  P: The property number.
  V: The word to locate.

Returns:
  True if the word is located, otherwise false."
<ROUTINE IN-PWTBL? (O P V "AUX" PT)
    <AND <SET PT <GETPT .O .P>>
         <IN-WTBL? .PT </ <PTSIZE .PT> 2> .V>>>

;"Attempts to locate a byte in a property table.

Args:
  O: The object containing the property.
  P: The property number.
  V: The byte to locate. Must be 255 or lower.

Returns:
  True if the byte is located, otherwise false."
<ROUTINE IN-PBTBL? (O P V "AUX" PT)
    <AND <SET PT <GETPT .O .P>>
         <IN-BTBL? .PT <PTSIZE .PT> .V>>>

<VERSION?
    (ZIP
        ;"V3 has no INTBL? opcode"
        <ROUTINE IN-WTBL? (TBL CNT V)
            <OR .CNT <RFALSE>>
            <SET CNT <- .CNT 1>>
            <DO (I 0 .CNT)
                <COND (<==? <GET .TBL .I> .V> <RTRUE>)>>
            <RFALSE>>

        <ROUTINE IN-BTBL? (TBL CNT V)
            <OR .CNT <RFALSE>>
            <SET CNT <- .CNT 1>>
            <DO (I 0 .CNT)
                <COND (<==? <GETB .TBL .I> .V> <RTRUE>)>>
            <RFALSE>>)
    (EZIP
        ;"V4 only has the 3-argument (word) form of INTBL?"
        <ROUTINE IN-WTBL? (TBL CNT V)
            <T? <INTBL? .V .TBL .CNT>>>

        <ROUTINE IN-BTBL? (TBL CNT V)
            <OR .CNT <RFALSE>>
            <SET CNT <- .CNT 1>>
            <DO (I 0 .CNT)
                <COND (<==? <GETB .TBL .I> .V> <RTRUE>)>>
            <RFALSE>>)
    (T
        ;"use built-in INTBL? in V5+"
        <ROUTINE IN-WTBL? (TBL CNT V)
            <T? <INTBL? .V .TBL .CNT>>>

        <ROUTINE IN-BTBL? (TBL CNT V)
            <T? <INTBL? .V .TBL .CNT 1>>>)>

<IF-DEBUG
    ;"Prints the contents of LEXBUF, calling DUMPWORD for each word."
    <ROUTINE DUMPLINE ("OPT" RAW? "AUX" (WDS <GETB ,LEXBUF 1>) (P <+ ,LEXBUF 2>))
        <TELL N .WDS " words in ">
        <COND (<==? ,LEXBUF ,KBD-LEXBUF> <TELL "KBD">)
              (<==? ,LEXBUF ,EDIT-LEXBUF> <TELL "EDIT">)
              (<==? ,LEXBUF ,CONT-LEXBUF> <TELL "CONT">)
              (ELSE <TELL "?">)>
        <TELL " buf:">
        <DO (I 1 .WDS)
            <TELL " ">
            <DUMPWORD <GET .P 0>>
            <COND (.RAW? <TELL "[\"" WORD .I "\"]">)>
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

    ;"Prints the contents of various LEXBUFS using DUMPLINE."
    <ROUTINE DUMPBUFS ("AUX" (OLB ,LEXBUF) (ORB ,READBUF))
        <ACTIVATE-BUFS "KBD">
        <DUMPLINE>
        <ACTIVATE-BUFS "EDIT">
        <DUMPLINE>
        <ACTIVATE-BUFS "CONT">
        <DUMPLINE>
        <SETG LEXBUF .OLB>
        <SETG READBUF .ORB>>

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
    <COPY-TABLE .SRC .DEST </ ,READBUF-SIZE 2>>>

;"Measures the length of a READBUF-like table (not including the null terminator on V3-4)."
<ROUTINE READBUF-LENGTH (TBL)
    <VERSION? (ZIP
               <REPEAT ((P 1))
                   <SET P <+ .P 1>>
                   <COND (<0? <GETB .TBL .P>> <RETURN <- .P 2>>)>>)
              (EZIP
               <REPEAT ((P 1))
                   <SET P <+ .P 1>>
                   <COND (<0? <GETB .TBL .P>> <RETURN <- .P 2>>)>>)
              (ELSE
               <RETURN <GETB .TBL 1>>)>>

;"Fills READBUF and LEXBUF by reading a command from the player.

Args:
  PROMPT?: Whether to print the prompt first.

Sets (contents):
  READBUF
  LEXBUF"
<DEFAULT-DEFINITION READLINE
    <ROUTINE READLINE ("OPT" PROMPT?)
        <COND (.PROMPT? <TELL CR "> ">)>
        <SETG READBUF ,KBD-READBUF>
        <SETG LEXBUF ,KBD-LEXBUF>
        <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
        ;"The read buffer has a slightly different format on V3."
        <VERSION? (ZIP)
                  (ELSE
                   <PUTB ,READBUF 1 0>
                   <UPDATE-STATUS-LINE>)>
        <DO-READ ,READBUF ,LEXBUF>
        <RTRUE>>>

;"Sets temporary values for one or more global variables, runs some code, then restores them.

Example:
  <WITH-GLOBAL ((WINNER <FOO>)) <BAR>>

  Expands to:

  <BIND ((ORIG?WINNER ,WINNER))
      <SETG WINNER <FOO>>
      <BAR>
      <SETG WINNER .ORIG?WINNER>>"
<DEFMAC WITH-GLOBAL ('TEMPS "ARGS" BODY "AUX" GATOMS TEMPATOMS NEWVALUES BINDINGS SETGS RESTORES)
    <SET GATOMS <MAPF ,LIST
                      <FUNCTION (F)
                          <COND (<OR <NOT <TYPE? .F LIST>>
                                     <N=? <LENGTH? .F 2> 2>
                                     <NOT <TYPE? <1 .F> ATOM>>>
                                 <ERROR BAD-BINDING .F>)
                                (ELSE <1 .F>)>>
                      .TEMPS>>
    <SET TEMPATOMS <MAPF ,LIST
                         <FUNCTION (A) <PARSE <STRING "ORIG?" <SPNAME .A>>>>
                         .GATOMS>>
    <SET NEWVALUES <MAPF ,LIST 2 .TEMPS>>
    <SET BINDINGS <MAPF ,LIST
                        <FUNCTION (T G) <LIST .T <CHTYPE .G GVAL>>>
                        .TEMPATOMS
                        .GATOMS>>
    <SET SETGS <MAPF ,LIST
                     <FUNCTION (G V) <FORM SETG .G .V>>
                     .GATOMS
                     .NEWVALUES>>
    <SET RESTORES <MAPF ,LIST
                        <FUNCTION (G T) <FORM SETG .G <CHTYPE .T LVAL>>>
                        .GATOMS
                        .TEMPATOMS>>
    ;"And finally..."
    <FORM BIND (!.BINDINGS ?RESULT)
          !.SETGS
          <FORM SET ?RESULT <FORM PROG '() !.BODY>>
          !.RESTORES
          '.?RESULT>>

<VERSION?
    (ZIP
     ;"If unlit, change HERE to 'Darkness' temporarily."
     <DEFMAC DO-READ ('RB 'LB)
         <EXPAND <FORM WRAP-FOR-DARK-STATUS <FORM READ .RB .LB>>>>

     <DEFMAC WRAP-FOR-DARK-STATUS ('F)
         <FORM BIND '((OHERE ,HERE))
             '<COND (<NOT ,HERE-LIT> <SETG HERE ,ROOMS>)>
             .F
             '<SETG HERE .OHERE>>>)
    (ELSE
     <DEFMAC DO-READ ('RB 'LB)
         <FORM READ .RB .LB>>)>

"Action framework"

;"Invokes the handlers for a given action (and objects).

Uses:
  WINNER

Sets (temporarily):
  PRSA
  PRSO
  PRSO-DIR
  PRSI"
<ROUTINE PERFORM (ACT "OPT" DOBJ IOBJ "AUX" PRTN RTN OA OD ODD OI WON CNT ORM)
    <TRACE 1 "[PERFORM: ACT=" N .ACT>
    <TRACE-DO 1
        <COND (.DOBJ <TELL " DOBJ=" D .DOBJ "(" N .DOBJ ")">)>
        <COND (.IOBJ <TELL " IOBJ=" D .IOBJ "(" N .IOBJ ")">)>
        <TELL "]" CR>>
    <SET PRTN <GET ,PREACTIONS .ACT>>
    <SET RTN <GET ,ACTIONS .ACT>>
    <SET OA ,PRSA>
    <SET OD ,PRSO>
    <SET ODD ,PRSO-DIR>
    <SET OI ,PRSI>
    <SET ORM ,REPORT-MODE>
    <SETG PRSA .ACT>
    <SETG PRSO .DOBJ>
    <OR <==? .ACT ,V?WALK> <SETG PRSO-DIR <>>>
    <SETG PRSI .IOBJ>
    <TRACE-IN>
    ;"Warn about improper number use, and handle multiple objects"
    <COND (<G? <COUNT-PRS-APPEARANCES ,NUMBER> 1>
           <TELL "You can't use more than one number in a command." CR>
           <SET WON <>>)
          (<AND <NOT ,PRSO-DIR> <PRSO? ,MANY-OBJECTS>>
           <COND (<PRSI? ,MANY-OBJECTS>
                  <TELL "You can't use multiple direct and indirect objects together." CR>
                  <SET WON <>>)
                 (ELSE
                  <SETG REPORT-MODE ,SHORT-REPORT>
                  <SET CNT <GETB ,P-PRSOS 0>>
                  <DO (I 1 .CNT)
                      <SETG PRSO <GET/B ,P-PRSOS .I>>
                      <TELL D ,PRSO ": ">
                      <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>>)>)
          (<PRSI? ,MANY-OBJECTS>
           <SETG REPORT-MODE ,SHORT-REPORT>
           <SET CNT <GETB ,P-PRSIS 0>>
           <DO (I 1 .CNT)
               <SETG PRSI <GET/B ,P-PRSIS .I>>
               <TELL D ,PRSI ": ">
               <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>>)
          (ELSE <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>)>
    <TRACE-OUT>
    <SETG PRSA .OA>
    <SETG PRSO .OD>
    <SETG PRSO-DIR .ODD>
    <SETG PRSI .OI>
    <SETG REPORT-MODE .ORM>
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
                <TRACE 4 "[calling WINNER (" D ,WINNER ") ACTION]" CR>
                <APPLY .AC ,M-WINNER>>
           <RTRUE>)
          (<AND <SET RM <LOC ,WINNER>>
                <SET AC <GETP .RM ,P?ACTION>>
                <TRACE 4 "[calling LOC (" D .RM ") ACTION]" CR>
                <APPLY .AC ,M-BEG>>
           <RTRUE>)
          (<AND .PRTN
                <TRACE 4 "[calling preaction routine]" CR>
                <APPLY .PRTN>>
           <RTRUE>)
          (<AND ,PRSI
                <SET RM <LOC ,PRSI>>
                <SET AC <GETP .RM ,P?CONTFCN>>
                <TRACE 4 "[calling PRSI LOC (" D <LOC ,PRSI> ") CONTFCN]" CR>
                <APPLY .AC>>
           <RTRUE>)
          (<AND ,PRSI
                <SET AC <GETP ,PRSI ,P?ACTION>>
                <TRACE 4 "[calling PRSI (" D ,PRSI ") ACTION]" CR>
                <APPLY .AC>>
           <RTRUE>)
          (<AND ,PRSO
                <NOT ,PRSO-DIR>
                <SET RM <LOC ,PRSO>>
                <SET AC <GETP .RM ,P?CONTFCN>>
                <TRACE 4 "[calling PRSO LOC (" D <LOC ,PRSO> ") CONTFCN]" CR>
                <APPLY .AC>>
           <RTRUE>)
          (<AND ,PRSO
                <NOT ,PRSO-DIR>
                <SET AC <GETP ,PRSO ,P?ACTION>>
                <TRACE 4 "[calling PRSO (" D ,PRSO ") ACTION]" CR>
                <APPLY .AC>>
           <RTRUE>)
          (ELSE
           <TRACE 4 "[calling action routine]" CR>
           <APPLY .RTN>)>>

;"Moves the player to a new location, notifies the location that the player
has entered, and prints an appropriate room introduction.

If the old and/or new location is dark, DARKNESS-F will be given a chance to
print the room introduction before DESCRIBE-ROOM and DESCRIBE-OBJECTS.

Uses:
  RESET-WINNER

Sets:
  HERE

Args:
  RM: The room to move into."
<ROUTINE GOTO (RM "AUX" WAS-LIT F (OWINNER <>))
    <COND (<ORDERING?>
           <SET OWINNER ,WINNER>
           <RESET-WINNER>)>
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
    <COND (,HERE-LIT <FSET ,HERE ,TOUCHBIT>)>
    <COND (.OWINNER <SETG WINNER .OWINNER>)>
    <RTRUE>>

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
    <TRACE 2 "[FIND-IN: looking in " D .C " for " N .BIT "]" CR>
    <TRACE-IN>
    <MAP-CONTENTS (I .C)
        <TRACE 3 "[considering " D .I "...">
        <COND (<FSET? .I .BIT>
               <TRACE 3 " OK">
               <SET N <+ .N 1>>
               <SET W .I>)>
        <TRACE 3 "]" CR>>
    <TRACE-OUT>
    <COND (<AND <0? .N> <SET PT <GETPT .C ,P?GLOBAL>>>

           <TRACE 2 "[falling back to LOCAL-GLOBALS]" CR>
           <TRACE-IN>

           <SET MAX <PTSIZE .PT>>
           <VERSION? (ZIP) (ELSE <SET MAX </ .MAX 2>>)>
           <SET MAX <- .MAX 1>>
           <DO (J 0 .MAX)
               <BIND ((I <GET/B .PT .J>))
                   <TRACE 3 "[considering " D .I "...">
                   <COND (<FSET? .I .BIT>
                          <TRACE 3 " OK">
                          <SET N <+ .N 1>>
                          <SET W .I>)>
                   <TRACE 3 "]" CR>>>
           <TRACE-OUT>)>
    <TRACE 2 "[FIND-IN: found " N .N "]" CR>
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
<ROUTINE ITALICIZE (STR)
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
<ROUTINE PICK-ONE (TABL "AUX" (LENGTH <GET .TABL 0>) (CNT <GET .TABL 1>) OCNT RND MSG)
    ;"Choose a random table element between CNT and LENGTH"
    <SET RND <RANDOM-IN-RANGE .CNT .LENGTH>>
    <SET MSG <GET .TABL .RND>>
    ;"Increase CNT"
    <SET OCNT .CNT>
    <SET CNT <+ 1 .CNT>>
    ;"If that finishes the table, reset and exit"
    <COND (<G? .CNT .LENGTH>
           <PUT .TABL 1 2>
           <RETURN .MSG>)>
    ;"Otherwise, move the item we just picked below CNT"
    <COND (<N=? .RND .OCNT>
           <PUT .TABL .RND <GET .TABL .OCNT>>
           <PUT .TABL .OCNT .MSG>)>
    <PUT .TABL 1 .CNT>
    <RETURN .MSG>>

<DEFAULT-DEFINITION RANDOM-IN-RANGE
  <DEFMAC RANDOM-IN-RANGE ('LO 'HI)
    <COND (<TYPE? .LO LVAL GVAL FIX FALSE CONSTANT>
           <FORM - <FORM + .LO <FORM RANDOM <FORM + <FORM - .HI .LO> 1>>> 1>)
          (ELSE
           <FORM BIND ((?LO .LO))
               <FORM - <FORM + '.?LO <FORM RANDOM <FORM + <FORM - .HI '.?LO> 1>>> 1>>)>>>

;"Returns a random element from a table, possibly repeating.

Args:
  TABL: The table, which should be an LTABLE with word elements.

Returns:
  A random element from the table."
<ROUTINE PICK-ONE-R (TABL "AUX" RND)
    <SET RND <RANDOM-IN-RANGE 1 <GET .TABL 0>>>
    <GET .TABL .RND>>

;"The game can override this with SETG. It doesn't go through DARKNESS-F, since
 it has to be a constant on V3."
<OR <GASSIGNED? DARKNESS-STATUS-TEXT>
    <SETG DARKNESS-STATUS-TEXT "Darkness">>

<DEFAULT-DEFINITION STATUS-LINE

    <VERSION?
        (ZIP
            <DEFMAC INIT-STATUS-LINE () <>>

            <ROUTINE UPDATE-STATUS-LINE ()
                <WRAP-FOR-DARK-STATUS <USL>>>)
        (T
            ;"Splits the screen and clears a 1-line status line."
            <ROUTINE INIT-STATUS-LINE ()
                <SPLIT 1>
                <CLEAR 1>>

            ;"Writes the location name, score, and turn count in the status line.

            Uses:
            HERE
            HERE-LIT
            SCORE
            MOVES"
            <ROUTINE UPDATE-STATUS-LINE ("AUX" WIDTH)
                <SCREEN 1>
                <HLIGHT ,H-INVERSE>
                <FAKE-ERASE>
                <TELL !\ >
                <COND (,HERE-LIT <TELL D ,HERE>)
                      (ELSE <TELL %,DARKNESS-STATUS-TEXT>)>
                <SET WIDTH <LOWCORE SCRH>>
                <CURSET 1 <- .WIDTH 22>>
                <TELL "Score: ">
                <PRINTN ,SCORE>
                <CURSET 1 <- .WIDTH 10>>
                <TELL "Moves: ">
                <PRINTN ,MOVES>
                <SCREEN 0>
                <HLIGHT ,H-NORMAL>>

            ;"Fills the top row with spaces."
            <ROUTINE FAKE-ERASE ()
                <CURSET 1 1>
                <DO (I <LOWCORE SCRH> 1 -1) <PRINTC !\ >>
                <CURSET 1 1>>)>
>

;"Prints a message and ends the game, prompting the player to restart,
(possibly) undo, restore, or quit.

Args:
  TEXT: The message to print before the 'game is over' banner.

Returns:
  True if RESURRECT? indicated that the game should resume.
  Otherwise, never returns."
<ROUTINE JIGS-UP (TEXT "AUX" W)
    <SETG P-CONT 0>
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
            <READLINE>
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
         <READLINE>
         <VERSION?
             (ZIP <SET RESP <GETB ,READBUF 1>>)
             (EZIP <SET RESP <GETB ,READBUF 1>>)
             (ELSE
              <COND (<GETB ,READBUF 1>
                     <SET RESP <GETB ,READBUF 2>>)
                    (ELSE
                     <SET RESP 0>)>)>
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
        <DEFMAC GETONECHAR ()
            '<BIND ()
                <READLINE>
                <GETB ,READBUF 1>>>)
    (ELSE
        <DEFMAC GETONECHAR ()
            '<INPUT 1>>)>

;"Determines whether an object can be seen by the player.

Uses:
  HERE
  WINNER
  PSEUDO-LOC

Args:
  OBJ: The object to check.

Returns:
  True if the object is visible, otherwise false."
<ROUTINE VISIBLE? (OBJ "AUX" P M)
    <COND (<=? .OBJ ,PSEUDO-OBJECT>
           <RETURN <=? ,HERE ,PSEUDO-LOC>>)>
    <SET P <LOC .OBJ>>
    <COND (<0? .P> <RFALSE>)>
    <SET M <META-LOC .OBJ>>
    <COND (<NOT <=? .M ,HERE>>
           <COND (<OR <AND <=? .P ,LOCAL-GLOBALS>
                           <GLOBAL-IN? .OBJ ,HERE>>
                      <=? .P ,GLOBAL-OBJECTS ,GENERIC-OBJECTS>>
                  <RTRUE>)
                 (ELSE <RFALSE>)>)>
    <REPEAT ()
        <COND (<EQUAL? .P ,HERE ,WINNER>
               <RTRUE>)
              (<NOT <SEE-INSIDE? .P>>
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
<ROUTINE META-LOC (OBJ)
    <REPEAT ()
        <SET OBJ <LOC .OBJ>>
        <COND (<0? .OBJ> <RFALSE>)
              (<IN? .OBJ ,ROOMS>
               <RETURN .OBJ>)>>>

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

<OBJECT ROOMS
    ;"For V3, we need an object called 'Darkness' to show in the status line."
    %<VERSION?
       (ZIP <LIST DESC ,DARKNESS-STATUS-TEXT>)
       (ELSE #SPLICE ())>
    ;"This has all the flags, just in case other objects don't define them."
    (FLAGS !,KNOWN-FLAGS)>

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
