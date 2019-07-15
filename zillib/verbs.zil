"Verbs"

<DIRECTIONS NORTH SOUTH EAST WEST NE NW SE SW IN OUT UP DOWN>

<SYNONYM NORTH N>
<SYNONYM SOUTH S>
<SYNONYM EAST E>
<SYNONYM WEST W>
<SYNONYM NE NORTHEAST>
<SYNONYM NW NORTHWEST>
<SYNONYM SE SOUTHEAST>
<SYNONYM SW SOUTHWEST>
<SYNONYM IN INTO INSIDE>
<SYNONYM OUT OUTSIDE>
<SYNONYM UP U>
<SYNONYM DOWN D>

<SYNONYM THROUGH THRU>
<SYNONYM ON ONTO>

;"Syntaxes for regular verbs"

<SYNTAX LOOK = V-LOOK>
<VERB-SYNONYM LOOK L>

<SYNTAX WALK OBJECT = V-WALK>
<SYNTAX WALK IN OBJECT (FIND DOORBIT) (IN-ROOM) = V-ENTER>
<SYNTAX WALK THROUGH OBJECT (FIND DOORBIT) (IN-ROOM) = V-ENTER>
<VERB-SYNONYM WALK GO>

<SYNTAX ENTER OBJECT (FIND DOORBIT) (IN-ROOM) = V-ENTER>
<SYNTAX GET IN OBJECT (FIND DOORBIT) (IN-ROOM) = V-ENTER>

<SYNTAX QUIT = V-QUIT>

<SYNTAX TAKE OBJECT (FIND TAKEBIT) (MANY ON-GROUND IN-ROOM) = V-TAKE>
<VERB-SYNONYM TAKE GRAB>
<SYNTAX PICK UP OBJECT (FIND TAKEBIT) (MANY ON-GROUND IN-ROOM) = V-TAKE>
<SYNTAX PICK OBJECT (FIND TAKEBIT) (MANY ON-GROUND IN-ROOM) UP OBJECT (FIND KLUDGEBIT) = V-TAKE>
<SYNTAX GET OBJECT (FIND TAKEBIT) (MANY ON-GROUND IN-ROOM) = V-TAKE>

<SYNTAX DROP OBJECT (MANY HAVE HELD CARRIED) = V-DROP PRE-DROP>
<SYNTAX PUT DOWN OBJECT (MANY HAVE HELD CARRIED) = V-DROP PRE-DROP>
<SYNTAX PUT OBJECT (MANY HAVE HELD CARRIED) DOWN OBJECT (FIND KLUDGEBIT) = V-DROP PRE-DROP>

<SYNTAX EXAMINE OBJECT (MANY HELD CARRIED ON-GROUND IN-ROOM) = V-EXAMINE PRE-REQUIRES-LIGHT>
<SYNTAX LOOK AT OBJECT (MANY HELD CARRIED ON-GROUND IN-ROOM) = V-EXAMINE PRE-REQUIRES-LIGHT>
<VERB-SYNONYM EXAMINE X>

<SYNTAX WEAR OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>
<VERB-SYNONYM WEAR DON>
<SYNTAX PUT ON OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>

<SYNTAX UNWEAR OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>
<VERB-SYNONYM UNWEAR DOFF>
<SYNTAX TAKE OFF OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>

<SYNTAX PUT OBJECT (MANY TAKE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON PRE-PUT-ON>
<SYNTAX PUT UP OBJECT (MANY TAKE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON PRE-PUT-ON>
<VERB-SYNONYM PUT HANG PLACE>

<SYNTAX PUT OBJECT (MANY TAKE HELD CARRIED) IN OBJECT (FIND CONTBIT) = V-PUT-IN PRE-PUT-IN>
<VERB-SYNONYM PUT PLACE INSERT>

<SYNTAX INVENTORY = V-INVENTORY>
<SYNTAX TAKE INVENTORY OBJECT (FIND KLUDGEBIT) = V-INVENTORY>
<VERB-SYNONYM INVENTORY I>

<SYNTAX CONTEMPLATE OBJECT = V-THINK-ABOUT>
<VERB-SYNONYM CONTEMPLATE CONSIDER>
<SYNTAX THINK ABOUT OBJECT = V-THINK-ABOUT>

<SYNTAX OPEN OBJECT (FIND OPENABLEBIT) = V-OPEN>

<SYNTAX CLOSE OBJECT (FIND OPENBIT) = V-CLOSE>
<VERB-SYNONYM CLOSE SHUT>

<SYNTAX LOCK OBJECT (FIND OPENABLEBIT) WITH OBJECT (FIND TOOLBIT) (HAVE HELD CARRIED) = V-LOCK>

<SYNTAX UNLOCK OBJECT (FIND LOCKEDBIT) WITH OBJECT (FIND TOOLBIT) (HAVE HELD CARRIED) = V-UNLOCK>

<SYNTAX TURN ON OBJECT (FIND DEVICEBIT) = V-TURN-ON>
<SYNTAX TURN OBJECT (FIND DEVICEBIT) ON OBJECT (FIND KLUDGEBIT) = V-TURN-ON>

<SYNTAX TURN OFF OBJECT (FIND DEVICEBIT) = V-TURN-OFF>
<SYNTAX TURN OBJECT (FIND DEVICEBIT) OFF OBJECT (FIND KLUDGEBIT) = V-TURN-OFF>

<SYNTAX FLIP OBJECT (FIND DEVICEBIT) = V-FLIP>
<VERB-SYNONYM FLIP SWITCH TOGGLE>

<SYNTAX WAIT = V-WAIT>
<VERB-SYNONYM WAIT Z>

<SYNTAX AGAIN = V-AGAIN>
<VERB-SYNONYM AGAIN G>

<SYNTAX READ OBJECT (FIND READBIT) (TAKE HELD CARRIED ON-GROUND IN-ROOM) = V-READ PRE-REQUIRES-LIGHT>
<VERB-SYNONYM READ PERUSE>

<SYNTAX EAT OBJECT (FIND EDIBLEBIT) (TAKE HAVE HELD CARRIED ON-GROUND IN-ROOM) = V-EAT>
<VERB-SYNONYM EAT SCARF DEVOUR GULP CHEW>

<SYNTAX DRINK OBJECT = V-DRINK>

<SYNTAX SMELL OBJECT = V-SMELL>

<SYNTAX PUSH OBJECT = V-PUSH>
<VERB-SYNONYM PUSH SHOVE>

<SYNTAX PULL OBJECT = V-PULL>
<VERB-SYNONYM PULL YANK DRAG CARRY>

<SYNTAX FILL OBJECT (FIND CONTBIT) = V-FILL>
<SYNTAX EMPTY OBJECT (FIND CONTBIT) = V-EMPTY>

<SYNTAX ATTACK OBJECT (FIND ATTACKBIT) = V-ATTACK>
<VERB-SYNONYM ATTACK HIT SMASH BREAK KILL DESTROY>

<SYNTAX GIVE OBJECT (HAVE HELD CARRIED) TO OBJECT (FIND PERSONBIT) = V-GIVE>
<SYNTAX GIVE OBJECT (FIND PERSONBIT) OBJECT (HAVE HELD CARRIED) = V-SGIVE>

<SYNTAX TELL OBJECT (FIND PERSONBIT) ABOUT OBJECT = V-TELL-ABOUT PRE-TELL>

<SYNTAX WAVE = V-WAVE-HANDS>
<SYNTAX WAVE OBJECT (TAKE HAVE HELD CARRIED) = V-WAVE>

<SYNTAX THROW OBJECT (TAKE HAVE HELD CARRIED) TO OBJECT (FIND PERSONBIT) (ON-GROUND IN-ROOM) = V-GIVE>
<SYNTAX THROW OBJECT (TAKE HAVE HELD CARRIED) AT OBJECT (FIND ATTACKBIT) (ON-GROUND IN-ROOM) = V-THROW-AT>
<VERB-SYNONYM THROW TOSS>

<SYNTAX BURN OBJECT ;(FIND BURNBIT) = V-BURN>
<VERB-SYNONYM BURN ROAST TORCH>

<SYNTAX RUB OBJECT = V-RUB>

<SYNTAX LOOK UNDER OBJECT (FIND SURFACEBIT) = V-LOOK-UNDER PRE-REQUIRES-LIGHT>

<SYNTAX SEARCH OBJECT (FIND CONTBIT) = V-SEARCH PRE-REQUIRES-LIGHT>
<SYNTAX LOOK IN OBJECT (FIND CONTBIT) = V-SEARCH PRE-REQUIRES-LIGHT>

<SYNTAX WAKE OBJECT (FIND PERSONBIT) = V-WAKE>
<SYNTAX WAKE UP OBJECT (FIND PERSONBIT) = V-WAKE>
<SYNTAX WAKE OBJECT (FIND PERSONBIT) UP OBJECT (FIND KLUDGEBIT) = V-WAKE>

<SYNTAX JUMP = V-JUMP>
<SYNTAX SWIM = V-SWIM>
<SYNTAX CLIMB = V-CLIMB>
<SYNTAX CLIMB OBJECT = V-CLIMB>
<SYNTAX SING = V-SING>
<SYNTAX DANCE = V-DANCE>

<SYNTAX YES = V-YES>
<VERB-SYNONYM YES Y>
<SYNTAX NO = V-NO>

;"Syntaxes for game verbs"

<SYNTAX VERSION = V-VERSION>

<SYNTAX UNDO = V-UNDO>
<SYNTAX SAVE = V-SAVE>
<SYNTAX RESTORE = V-RESTORE>
<SYNTAX RESTART = V-RESTART>

<SYNTAX BRIEF = V-BRIEF>
<SYNTAX SUPERBRIEF = V-SUPERBRIEF>
<SYNTAX VERBOSE = V-VERBOSE>

<SYNTAX SCRIPT = V-SCRIPT>
<SYNTAX SCRIPT ON OBJECT (FIND KLUDGEBIT) = V-SCRIPT>
<SYNTAX SCRIPT OFF OBJECT (FIND KLUDGEBIT) = V-UNSCRIPT>
<VERB-SYNONYM SCRIPT TRANSCRIPT>
<SYNTAX UNSCRIPT = V-UNSCRIPT>
<VERB-SYNONYM UNSCRIPT NOSCRIPT>

<SYNTAX PRONOUNS = V-PRONOUNS>

<SYNTAX \,TELL OBJECT (FIND PERSONBIT) = V-TELL PRE-TELL>

;"Debugging verbs"
<IF-DEBUG
    <SYNTAX XTRACE OBJECT = V-XTRACE>>

<IF-DEBUGGING-VERBS
    <SYNTAX XTREE = V-XTREE>
    <SYNTAX XTREE OBJECT = V-XTREE>
    
    <SYNTAX XGOTO OBJECT = V-XGOTO>
    
    <SYNTAX XMOVE OBJECT TO OBJECT (FIND PERSONBIT) = V-XMOVE>
    <SYNTAX XMOVE OBJECT = V-XMOVE>
    <SYNTAX XREMOVE OBJECT = V-XREMOVE>
    
    <SYNTAX XLIGHT = V-XLIGHT>
    
    <SYNTAX XEXITS = V-XEXITS>
    <SYNTAX XEXITS OBJECT = V-XEXITS>
    
    <SYNTAX XOBJ OBJECT = V-XOBJ>
    
    <SYNTAX XIT OBJECT = V-XIT>
>

;"Constants"

;"TODO: these belong in parser.zil?"
;"Object action handlers may get: M-WINNER, or no arg"
;"Room action handlers may get: M-BEG, M-END, M-ENTER, M-LOOK, M-FLASH"
;"Object DESCFCNs may get: M-OBJDESC?, M-OBJDESC"
;"DARKNESS-F may get: M-LOOK, M-SCOPE?, M-LIT-TO-DARK, M-DARK-TO-LIT,
    M-DARK-TO-DARK, M-DARK-CANT-GO"
<CONSTANT M-BEG 1>                ;"Intercept action at beginning of turn"
<CONSTANT M-END 2>                ;"React to action at end of turn"
<CONSTANT M-ENTER 3>              ;"Player is entering room"
<CONSTANT M-LOOK 4>               ;"Show room description"
<CONSTANT M-FLASH 5>              ;"Show important descriptions even in BRIEF mode"
<CONSTANT M-OBJDESC? 6>           ;"Choose whether to self-describe"
<CONSTANT M-OBJDESC 7>            ;"Write a self-description"
<CONSTANT M-SCOPE? 8>             ;"Decide which scope stages run in darkness"
<CONSTANT M-LIT-TO-DARK 9>        ;"Player moved from light to darkness"
<CONSTANT M-DARK-TO-LIT 10>       ;"Player moved from darkness to light"
<CONSTANT M-DARK-TO-DARK 11>      ;"Player moved from one dark room to another"
<CONSTANT M-DARK-CANT-GO 12>      ;"Player stumbled around in a dark room"
<CONSTANT M-NOW-DARK 13>          ;"Light source is gone"
<CONSTANT M-NOW-LIT 14>           ;"Light source is back"
<CONSTANT M-WINNER 15>            ;"Object is the one performing this action"

;"Helper routines for action handlers"

<ROUTINE YOU-MASHER ("OPT" WHOM)
    <TELL "I don't think " T <OR .WHOM ,PRSO> " would appreciate that." CR>>

<ROUTINE POINTLESS (VING "OPT" PREP REV? "AUX" F S)
    <COND (.REV? <SET F ,PRSI> <SET S ,PRSO>)
          (ELSE <SET F ,PRSO> <SET S ,PRSI>)>
    <TELL .VING>
    <COND (.F
           <TELL !\  T .F>
           <COND (.PREP
                  <TELL !\  .PREP>
                  <COND (.S <TELL !\  T .S>)>)>)>
    <TELL " doesn't seem like it will help." CR>>

<ROUTINE NOT-POSSIBLE (V)
    <SETG P-CONT 0>
    <TELL "That's not something you can " .V "." CR>>

<ROUTINE RHETORICAL ()
    <TELL "That was a rhetorical question." CR>>

<ROUTINE BE-SPECIFIC ()
    <SETG P-CONT 0>
    <TELL "You'll have to be more specific." CR>>

<ROUTINE SILLY ()
    <SETG P-CONT 0>
    <TELL "You must be joking." CR>>

<ROUTINE TSD ()
    <SETG P-CONT 0>
    <TELL "Not here, not now." CR>>

<DEFMAC IF-PLURAL ('O 'IF-PL 'IF-SG)
    <FORM COND <LIST <FORM FSET? .O ',PLURALBIT> .IF-PL> <LIST ELSE .IF-SG>>>

<ROUTINE PRE-REQUIRES-LIGHT ()
    <COND (<NOT ,HERE-LIT>
           <SETG P-CONT 0>
           <TELL "It's too dark to see anything here." CR>)>>

;"Action handler routines"

<ROUTINE V-LOOK ()
    <COND (<DESCRIBE-ROOM ,HERE T>
           <DESCRIBE-OBJECTS ,HERE>)>>

;"Prints a room description, handling darkness and briefness.

If the room is HERE and the player doesn't have a light source, this prints
a generic room name and description.

Otherwise, the real name is printed, optionally followed by a description
(if MODE is VERBOSE, or if it's BRIEF and this is the first time describing
the room, or if LONG is true).

Uses:
  MODE

Args:
  RM: The room to describe.
  LONG: If true, print the room description even in BRIEF mode.

Returns:
  True if the objects in the room should also be described, otherwise
  false."
<ROUTINE DESCRIBE-ROOM (RM "OPT" LONG "AUX" P)
    <COND (<AND <==? .RM ,HERE> <NOT ,HERE-LIT>>
           <DARKNESS-F ,M-LOOK>
           <RFALSE>)>
    ;"Print the room's real name."
    <VERSION? (ZIP) (ELSE <HLIGHT ,H-BOLD>)>
    <TELL D .RM CR>
    <VERSION? (ZIP) (ELSE <HLIGHT ,H-NORMAL>)>
    ;"If this is an implicit LOOK, check briefness."
    <COND (<NOT .LONG>
           <COND (<EQUAL? ,MODE ,SUPERBRIEF>
                  <RFALSE>)
                 (<AND <FSET? .RM ,TOUCHBIT>
                       <NOT <EQUAL? ,MODE ,VERBOSE>>>
                  ;"Call the room's ACTION with M-FLASH even in brief mode."
                  <APPLY <GETP .RM ,P?ACTION> ,M-FLASH>
                  <RTRUE>)>)>
    ;"The room's ACTION can print a description with M-LOOK.
      Otherwise, print the LDESC if present."
    <COND (<APPLY <GETP .RM ,P?ACTION> ,M-LOOK>)
          (<SET P <GETP .RM ,P?LDESC>>
           <TELL .P CR>)>
    ;"Call the room's ACTION again with M-FLASH for important descriptions."
    <APPLY <GETP .RM ,P?ACTION> ,M-FLASH>
    ;"Mark the room visited."
    <FSET .RM ,TOUCHBIT>
    <RTRUE>>

<DEFAULT-DEFINITION DARKNESS-F

    <ROUTINE DARKNESS-F (ARG)
        <COND (<=? .ARG ,M-LOOK>
               <TELL "It is pitch black. You can't see a thing." CR>)
              (<=? .ARG ,M-SCOPE?>
               <T? <SCOPE-STAGE? GENERIC INVENTORY GLOBALS>>)
              (<=? .ARG ,M-NOW-DARK>
               <TELL "You are plunged into darkness." CR>)
              (<=? .ARG ,M-NOW-LIT>
               <TELL "You can see your surroundings now." CR CR>
               <RFALSE>)
              (ELSE <RFALSE>)>>
>

<DEFAULT-DEFINITION DESCRIBE-OBJECTS

;"Describes the objects in a room.

Objects are described in four passes:
1. All non-person objects with DESCFCNs, FDESCS, and LDESCs.
2. All non-person objects not covered by #1.
3. The visible contents of containers and surfaces.
4. All objects with PERSONBIT other than WINNER.

Uses:
  WINNER

Args:
  RM: The room."

    <ROUTINE DESCRIBE-OBJECTS (RM "AUX" P N)
        <MAP-CONTENTS (I .RM)
            <COND
                (<FSET? .I ,NDESCBIT>)
                ;"objects with DESCFCNs"
                (<SET P <GETP .I ,P?DESCFCN>>
                 <CRLF>
                 ;"The DESCFCN is responsible for listing the object's contents"
                 <APPLY .P ,M-OBJDESC>
                 <THIS-IS-IT .I>)
                ;"objects with applicable FDESCs or LDESCs"
                (<OR <AND <NOT <FSET? .I ,TOUCHBIT>>
                          <SET P <GETP .I ,P?FDESC>>>
                     <SET P <GETP .I ,P?LDESC>>>
                 <TELL CR .P CR>
                 <THIS-IS-IT .I>
                 ;"Describe contents if applicable"
                 <COND (<AND <SEE-INSIDE? .I> <FIRST? .I>>
                        <DESCRIBE-CONTENTS .I>)>)>>
        ;"See if there are any non fdesc, ndescbit, personbit objects in room"
        <MAP-CONTENTS (I .RM)
            <COND (<GENERIC-DESC? .I>
                   <SET N T>
                   <RETURN>)>>
        ;"go through the N objects"
        <COND (.N
               <TELL CR "There ">
               <LIST-OBJECTS .RM GENERIC-DESC? ,L-ISMANY>
               <TELL " here." CR>
               <CONTENTS-ARE-IT .RM GENERIC-DESC?>)>
        ;"describe visible contents of generic-desc containers and surfaces"
        <MAP-CONTENTS (I .RM)
            <COND (<AND <SEE-INSIDE? .I>
                        <GENERIC-DESC? .I>
                        <FIRST? .I>>
                   <DESCRIBE-CONTENTS .I>)>>
        ;"See if there are any NPCs"
        <SET N <>>
        <MAP-CONTENTS (I .RM)
            <COND (<NPC-DESC? .I>
                   <SET N T>
                   <RETURN>)>>
        ;"go through the N NPCs"
        <COND (.N
               <CRLF>
               <LIST-OBJECTS .RM NPC-DESC? <+ ,L-SUFFIX ,L-CAP>>
               <TELL " here." CR>
               <CONTENTS-ARE-IT .RM NPC-DESC?>)>>

    <ROUTINE GENERIC-DESC? (OBJ "AUX" P)
        <T? <NOT <OR <==? .OBJ ,WINNER>
                     <FSET? .OBJ ,NDESCBIT>
                     <FSET? .OBJ ,PERSONBIT>
                     <AND <NOT <FSET? .OBJ ,TOUCHBIT>>
                          <GETP .OBJ ,P?FDESC>>
                     <GETP .OBJ ,P?LDESC>
                     <AND <SET P <GETP .OBJ ,P?DESCFCN>> <APPLY .P ,M-OBJDESC?>>>>>>

    <ROUTINE NPC-DESC? (OBJ "AUX" P)
        <T? <AND <FSET? .OBJ ,PERSONBIT>
                 <NOT <OR <==? .OBJ ,WINNER>
                          <FSET? .OBJ ,NDESCBIT>
                          <AND <NOT <FSET? .OBJ ,TOUCHBIT>>
                               <GETP .OBJ ,P?FDESC>>
                          <GETP .OBJ ,P?LDESC>
                          <AND <SET P <GETP .OBJ ,P?DESCFCN>> <APPLY .P ,M-OBJDESC?>>>>>>>

>

<DEFMAC UPPERCASE-CHAR ('C)
    <FORM BIND <LIST <LIST ?TMP .C>>
        '<COND (<AND <G=? .?TMP !\a> <L=? .?TMP !\z>>
                <- .?TMP 32>)
               (ELSE .?TMP)>>>

;"Prints a (short) string with the first letter capitalized."
<ROUTINE PRINT-CAP-STR (S "AUX" MAX C)
    <DIROUT 3 ,TEMPTABLE>
    <PRINT .S>
    <DIROUT -3>
    <SET MAX <GET ,TEMPTABLE 0>>
    <COND (.MAX
           <INC MAX>
           <DO (I 2 .MAX)
               <SET C <GETB ,TEMPTABLE .I>>
               <AND <=? .I 2> <SET C <UPPERCASE-CHAR .C>>>
               <PRINTC .C>>)>>

;"Prints an object name with the first letter capitalized."
<ROUTINE PRINT-CAP-OBJ (OBJ "AUX" MAX C)
    <DIROUT 3 ,TEMPTABLE>
    <PRINTD .OBJ>
    <DIROUT -3>
    <SET MAX <GET ,TEMPTABLE 0>>
    <COND (.MAX
           <INC MAX>
           <DO (I 2 .MAX)
               <SET C <GETB ,TEMPTABLE .I>>
               <AND <=? .I 2> <SET C <UPPERCASE-CHAR .C>>>
               <PRINTC .C>>)>>

;"Implements <TELL A .OBJ>."
<ROUTINE PRINT-INDEF (OBJ "AUX" A)
    <COND (<FSET? .OBJ ,NARTICLEBIT>)
          (<SET A <GETP .OBJ ,P?ARTICLE>> <TELL .A> <PRINTC !\ >)
          (<FSET? .OBJ ,PLURALBIT> <TELL "some ">)
          (<FSET? .OBJ ,VOWELBIT> <TELL "an ">)
          (ELSE <TELL "a ">)>
    <PRINTD .OBJ>>

;"Implements <TELL T .OBJ>."
<ROUTINE PRINT-DEF (OBJ)
    <COND (<NOT <FSET? .OBJ ,NARTICLEBIT>> <TELL "the ">)>
    <PRINTD .OBJ>>

;"Implements <TELL CA .OBJ>."
<ROUTINE PRINT-CINDEF (OBJ "AUX" A)
    <COND (<FSET? .OBJ ,NARTICLEBIT>
           <PRINT-CAP-OBJ .OBJ>
           <RTRUE>)>
    <COND (<SET A <GETP .OBJ ,P?ARTICLE>> <PRINT-CAP-STR .A> <PRINTC !\ >)
          (<FSET? .OBJ ,PLURALBIT> <TELL "Some ">)
          (<FSET? .OBJ ,VOWELBIT> <TELL "An ">)
          (ELSE <TELL "A ">)>
    <PRINTD .OBJ>>

;"Implements <TELL CT .OBJ>."
<ROUTINE PRINT-CDEF (OBJ)
    <COND (<FSET? .OBJ ,NARTICLEBIT>
           <PRINT-CAP-OBJ .OBJ>
           <RTRUE>)
          (ELSE <TELL "The " D .OBJ>)>>

;"Prints a sentence describing the contents of a surface or container."
<ROUTINE DESCRIBE-CONTENTS (OBJ)
    <COND (<FSET? .OBJ ,SURFACEBIT> <TELL "On">)
          (ELSE <TELL "In">)>
    <TELL " " T .OBJ " ">
    <LIST-OBJECTS .OBJ <> ,L-ISARE>
    <TELL "." CR>
    <CONTENTS-ARE-IT .OBJ>>

;"Prints a space followed by a parenthetical describing the contents of a
surface or container, for use in inventory listings."
<ROUTINE INV-DESCRIBE-CONTENTS (OBJ "AUX" N F)
    <COND (<FSET? .OBJ ,SURFACEBIT> <TELL " (holding ">)
          (ELSE <TELL " (containing ">)>
    <SET F <FIRST? .OBJ>>
    <COND (<NOT .F>
           <TELL "nothing)">
           <RETURN>)>
    <MAP-CONTENTS (I .OBJ)
        <SET N <+ .N 1>>>
    <COND (<==? .N 1>
           <TELL A .F>)
          (<==? .N 2>
           <TELL A .F " and " A <NEXT? .F>>)
          (ELSE
           <MAP-CONTENTS (I .OBJ)
               <TELL A .I>
               <SET N <- .N 1>>
               <COND (<0? .N>)
                     (<==? .N 1> <TELL ", and ">)
                     (ELSE <TELL ", ">)>>)>
    <TELL ")">>

;"Prints a list describing a set of objects, usually the contents of a
surface or container.

No trailing punctuation is printed.

If the L-ISARE flag is passed, the list begins with 'is' or 'are' depending
on the count and plurality of the child objects. For example:

  If the container is empty:
    is nothing

  If the container has one singular object:
    is a shirt

  If the container has one plural object:
    are some pants

  If the container has two objects:
    are a shirt and a hat

  If the container has three objects:
    are a shirt, a hat, and a watch

Uses:
  HERE
  PSEUDO-LOC

Args:
  O: The object whose contents are to be listed, or if L-PRSTABLE is given,
    the address of a table containing the objects.
  FILTER: An optional routine to select children to list.
    If provided, the list will only include objects for which the filter
    returns true; otherwise it'll list all contents.
  FLAGS: A combination of option flags:
    L-ISARE: Print 'is' or 'are'.
    L-SUFFIX: Print the verb after the list instead of before. (Implies L-ISARE.)
    L-ISMANY: Use 'is' before a list of objects unless the first one has
      PLURALBIT. (Implies L-ISARE. Ignored if L-SUFFIX is given.)
    L-PRSTABLE: List objects from a table instead of the contents of another
      object. O is the address of the table in P-PRSOS/P-PRSIS format.
    L-THE: Print the definite article instead of indefinite.
    L-OR: Print 'or' instead of 'and'.
    L-CAP: Capitalize the first article printed. (Implies L-SUFFIX.)
    L-SCENERY: Refer to PSEUDO-OBJECT as 'some scenery [in PSEUDO-LOC]'.

Returns:
  The number of objects listed."
<ROUTINE LIST-OBJECTS (O "OPT" FILTER FLAGS "AUX" N F S MAX J)
    <COND (<BTST .FLAGS ,L-CAP>
           <SET FLAGS <BOR .FLAGS ,L-SUFFIX>>)>
    <COND (<OR <BTST .FLAGS ,L-SUFFIX> <BTST .FLAGS ,L-ISMANY>>
           <SET FLAGS <BOR .FLAGS ,L-ISARE>>)>
    <COND (<BTST .FLAGS ,L-PRSTABLE>
           <COND (<SET MAX <GETB .O 0>>
                  <DO (I 1 .MAX)
                      <SET J <GET/B .O .I>>
                      <COND (<OR <NOT .FILTER> <APPLY .FILTER .J>>
                             <COND (<0? .F> <SET F .J>)
                                   (<0? .S> <SET S .J>)>
                             <SET N <+ .N 1>>)>>)>)
          (ELSE
           <MAP-CONTENTS (I .O)
               <COND (<OR <NOT .FILTER> <APPLY .FILTER .I>>
                      <COND (<0? .F> <SET F .I>)
                            (<0? .S> <SET S .I>)>
                      <SET N <+ .N 1>>)>>)>
    <COND (<==? .N 0>
           <COND (<BTST .FLAGS ,L-CAP>
                  <TELL "Nothing is">)
                 (<BTST .FLAGS ,L-SUFFIX>
                  <TELL "nothing is">)
                 (<BTST .FLAGS ,L-ISARE>
                  <TELL "is nothing">)
                 (ELSE <TELL "nothing">)>)
          (<==? .N 1>
           <COND (<BTST .FLAGS ,L-CAP>
                  <LIST-OBJECTS-PRINT .F .FLAGS T>
                  <IF-PLURAL .F <TELL " are"> <TELL " is">>)
                 (<BTST .FLAGS ,L-SUFFIX>
                  <LIST-OBJECTS-PRINT .F .FLAGS <>>
                  <IF-PLURAL .F <TELL " are"> <TELL " is">>)
                 (ELSE
                  <AND <BTST .FLAGS ,L-ISARE>
                       <IF-PLURAL .F <TELL "are "> <TELL "is ">>>
                  <LIST-OBJECTS-PRINT .F .FLAGS <>>)>)
          (<==? .N 2>
           <COND (<AND <BTST .FLAGS ,L-ISARE>
                       <NOT <BTST .FLAGS ,L-SUFFIX>>>
                       <COND (<OR <NOT <BTST .FLAGS ,L-ISMANY>>
                                  <FSET? .F ,PLURALBIT>>
                              <TELL "are ">)
                             (ELSE <TELL "is ">)>)>
           <LIST-OBJECTS-PRINT .F .FLAGS <BAND .FLAGS ,L-CAP>>
           <COND (<BTST .FLAGS ,L-OR> <TELL " or ">) (ELSE <TELL " and ">)>
           <LIST-OBJECTS-PRINT .S .FLAGS <>>
           <AND <BTST .FLAGS ,L-SUFFIX> <TELL " are">>)
          (ELSE
           <COND (<AND <BTST .FLAGS ,L-ISARE>
                       <NOT <BTST .FLAGS ,L-SUFFIX>>>
                  <COND (<OR <NOT <BTST .FLAGS ,L-ISMANY>>
                             <FSET? .F ,PLURALBIT>>
                         <TELL "are ">)
                        (ELSE <TELL "is ">)>)>
           <COND (<BTST .FLAGS ,L-PRSTABLE>
                  <DO (I 1 .MAX)
                      <SET J <GET/B .O .I>>
                      <COND (<OR <NOT .FILTER> <APPLY .FILTER .J>>
                             <COND (<AND <BTST .FLAGS ,L-CAP> <=? .I 1>>
                                    <LIST-OBJECTS-PRINT .J .FLAGS T>)
                                   (ELSE
                                    <LIST-OBJECTS-PRINT .J .FLAGS <>>)>
                             <SET N <- .N 1>>
                             <COND (<0? .N>)
                                   (<==? .N 1>
                                    <COND (<BTST .FLAGS ,L-OR> <TELL ", or ">)
                                          (ELSE <TELL ", and ">)>)
                                   (ELSE <TELL ", ">)>)>>)
                 (ELSE
                  <MAP-CONTENTS (I .O)
                      <COND (<OR <NOT .FILTER> <APPLY .FILTER .I>>
                             <COND (<AND <BTST .FLAGS ,L-CAP> <=? .I .F>>
                                    <LIST-OBJECTS-PRINT .I .FLAGS T>)
                                   (ELSE
                                    <LIST-OBJECTS-PRINT .I .FLAGS <>>)>
                             <SET N <- .N 1>>
                             <COND (<0? .N>)
                                   (<==? .N 1>
                                    <COND (<BTST .FLAGS ,L-OR> <TELL ", or ">)
                                          (ELSE <TELL ", and ">)>)
                                   (ELSE <TELL ", ">)>)>>)>
           <AND <BTST .FLAGS ,L-SUFFIX> <TELL " are">>)>
    <RETURN .N>>

<ROUTINE LIST-OBJECTS-PRINT (O FLAGS CAP?)
    <COND (<AND <=? .O ,PSEUDO-OBJECT>
                <BTST .FLAGS ,L-SCENERY>>
           <COND (.CAP? <TELL !\S>) (ELSE <TELL !\s>)>
           <TELL "ome scenery">
           <COND (<N=? ,PSEUDO-LOC ,HERE>
                  <TELL " in " D ,PSEUDO-LOC>)>
           <RTRUE>)
          (.CAP?
           <COND (<BTST .FLAGS ,L-THE> <TELL CT .O>)
                 (ELSE <TELL CA .O>)>)
          (ELSE
           <COND (<BTST .FLAGS ,L-THE> <TELL T .O>)
                 (ELSE <TELL A .O>)>)>>

;"Direction properties have a different format on V4+, where object numbers are words."
<VERSION?
    (ZIP
        <CONSTANT UEXIT 1>          ;"size of unconditional exit"
        <CONSTANT NEXIT 2>          ;"size of non-exit"
        <CONSTANT FEXIT 3>          ;"size of function exit"
        <CONSTANT CEXIT 4>          ;"size of conditional exit"
        <CONSTANT DEXIT 5>          ;"size of door exit"

        <CONSTANT EXIT-RM 0>        ;GET/B
        <CONSTANT NEXIT-MSG 0>      ;GET
        <CONSTANT FEXIT-RTN 0>      ;GET
        <CONSTANT CEXIT-VAR 1>      ;GETB
        <CONSTANT CEXIT-MSG 1>      ;GET
        <CONSTANT DEXIT-OBJ 1>      ;GET/B
        <CONSTANT DEXIT-MSG 1>      ;GET)
    (T
        <CONSTANT UEXIT 2>
        <CONSTANT NEXIT 3>
        <CONSTANT FEXIT 4>
        <CONSTANT CEXIT 5>
        <CONSTANT DEXIT 6>

        <CONSTANT EXIT-RM 0>
        <CONSTANT NEXIT-MSG 0>
        <CONSTANT FEXIT-RTN 0>
        <CONSTANT CEXIT-VAR 4>
        <CONSTANT CEXIT-MSG 1>
        <CONSTANT DEXIT-OBJ 1>
        <CONSTANT DEXIT-MSG 2>)>

;"Checks whether PRSA is a meta-verb that does not cause time to pass."
<DEFMAC GAME-VERB? ()
    <FORM VERB? QUIT VERSION WAIT SAVE RESTORE RESTART INVENTORY UNDO
                SUPERBRIEF BRIEF VERBOSE AGAIN SCRIPT UNSCRIPT
                PRONOUNS TELL
                !<IFFLAG (DEBUG '(XTRACE)) (ELSE '())>
                !<IFFLAG
                    (DEBUGGING-VERBS
                     '(XTREE XGOTO XMOVE XREMOVE XLIGHT XEXITS XOBJ XIT))
                    (ELSE '())>
                !,EXTRA-GAME-VERBS>>

<COND (<NOT <GASSIGNED? EXTRA-GAME-VERBS>> <SETG EXTRA-GAME-VERBS '()>)>

<CONSTANT CANT-GO-THAT-WAY "You can't go that way.">

<ROUTINE V-WALK ("AUX" PT PTS RM D)
    <COND (<NOT ,PRSO-DIR>
           <PRINTR "You must give a direction to walk in.">)
          (<0? <SET PT <GETPT ,HERE ,PRSO>>>
           <COND (<OR ,HERE-LIT <NOT <DARKNESS-F ,M-DARK-CANT-GO>>>
                  <TELL ,CANT-GO-THAT-WAY CR>)>
           <SETG P-CONT 0>
           <RTRUE>)
          (<==? <SET PTS <PTSIZE .PT>> ,UEXIT>
           <SET RM <GET/B .PT ,EXIT-RM>>)
          (<==? .PTS ,NEXIT>
           <TELL <GET .PT ,NEXIT-MSG> CR>
           <SETG P-CONT 0>
           <RTRUE>)
          (<==? .PTS ,FEXIT>
           <COND (<0? <SET RM <APPLY <GET .PT ,FEXIT-RTN>>>>
                  <SETG P-CONT 0>
                  <RTRUE>)>)
          (<==? .PTS ,CEXIT>
           <COND (<VALUE <GETB .PT ,CEXIT-VAR>>
                  <SET RM <GET/B .PT ,EXIT-RM>>)
                 (ELSE
                  <COND (<SET RM <GET .PT ,CEXIT-MSG>>
                         <TELL .RM CR>)
                        (<AND <NOT ,HERE-LIT> <DARKNESS-F ,M-DARK-CANT-GO>>
                         ;"DARKNESS-F printed a message")
                        (ELSE
                         <TELL ,CANT-GO-THAT-WAY CR>)>
                  <SETG P-CONT 0>
                  <RTRUE>)>)
          (<==? .PTS ,DEXIT>
           <COND (<FSET? <SET D <GET/B .PT ,DEXIT-OBJ>> ,OPENBIT>
                  <SET RM <GET/B .PT ,EXIT-RM>>)
                 (<SET RM <GET .PT ,DEXIT-MSG>>
                  <TELL .RM CR>
                  <SETG P-CONT 0>
                  <RTRUE>)
                 (ELSE
                  <THIS-IS-IT .D>
                  <TELL "You'll have to open " T .D
                        " first." CR>
                  <SETG P-CONT 0>
                  <RTRUE>)>)
          (ELSE
           <TELL "Broken exit (" N .PTS ")." CR>
           <SETG P-CONT 0>
           <RTRUE>)>
    <GOTO .RM>>

<ROUTINE V-ENTER ()
    <COND (<FSET? ,PRSO ,DOORBIT>
           <DO-WALK <DOOR-DIR ,PRSO>>
           <RTRUE>)
          (ELSE
           <NOT-POSSIBLE "get inside"> <RTRUE>)>>

;"Performs the WALK action with a direction."
<ROUTINE DO-WALK (DIR)
    <WITH-GLOBAL ((PRSO-DIR T)) <PERFORM ,V?WALK .DIR>>>

;"Finds a direction from HERE that leads through the given door.

Returns:
  A direction property, or false if no direction leads through the door."
<ROUTINE DOOR-DIR DD (DOOR)
    <MAP-DIRECTIONS (D PT ,HERE)
        <COND (<AND <==? <PTSIZE .PT> ,DEXIT>
                    <==? <GET/B .PT ,DEXIT-OBJ> .DOOR>>
               <RETURN .D .DD>)>>
    <RFALSE>>

;"Finds the room on the other side of a given door from HERE.

Returns:
  A room, or false if no direction leads through the door."
<ROUTINE OTHER-SIDE (DOOR "AUX" D)
    <COND (<SET D <DOOR-DIR .DOOR>>
           <GET/B <GETPT ,HERE .D> ,EXIT-RM>)
          (ELSE <>)>>

<ROUTINE V-QUIT ()
    <TELL "Are you sure you want to quit?">
    <COND (<YES?>
           <TELL CR "Thanks for playing." CR>
           <QUIT>)
          (ELSE
           <TELL CR "OK - not quitting." CR>)>>

<ROUTINE V-EXAMINE ("AUX" P (N <>))
    <COND (<OR <SET P <GETP ,PRSO ,P?TEXT>>
               <SET P <GETP ,PRSO ,P?LDESC>>>
           <TELL .P CR>
           <SET N T>)>
    <COND (<FSET? ,PRSO ,OPENABLEBIT>
           <TELL CT ,PRSO " is ">
           <COND (<FSET? ,PRSO ,OPENBIT> <TELL "open.">)
                 (ELSE <TELL "closed.">)>
           <CRLF>
           <SET N T>)>
    <COND (<AND <FIRST? ,PRSO> <SEE-INSIDE? ,PRSO>>
           <DESCRIBE-CONTENTS ,PRSO>
           <SET N T>)>
    <COND (<NOT .N>
           <TELL "You see nothing special about " T ,PRSO "." CR>)>>

<ROUTINE V-LOOK-UNDER ()
    <COND (<AND <N=? ,PRSO ,WINNER> <FSET? ,PRSO ,PERSONBIT>>
           <YOU-MASHER>
           <RTRUE>)
          (<NOT ,HERE-LIT> <TELL "It's too dark." CR>)
          (ELSE <TELL "You can't see anything of interest." CR>)>>

<ROUTINE V-SEARCH ()
    <COND (<PRSO? ,WINNER> <PERFORM ,V?INVENTORY>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (<NOT <FSET? ,PRSO ,CONTBIT>> <NOT-POSSIBLE "look inside">)
          (<AND <FSET? ,PRSO ,OPENABLEBIT> <NOT <SEE-INSIDE? ,PRSO>>>
           <TELL CT ,PRSO <IF-PLURAL ,PRSO " are" " is"> " closed." CR>)
          (<NOT <FIRST? ,PRSO>>
           <TELL CT ,PRSO <IF-PLURAL ,PRSO " are" " is"> " empty." CR>)
          (ELSE <DESCRIBE-CONTENTS ,PRSO>)>>

<ROUTINE V-INVENTORY ()
    ;"check for light first"
    <COND (,HERE-LIT
           <COND (<FIRST? ,WINNER>
                  <TELL "You are carrying:" CR>
                  <MAP-CONTENTS (I ,WINNER)
                      <TELL "   " A .I>
                      <AND <FSET? .I ,WORNBIT> <TELL " (worn)">>
                      <AND <FSET? .I ,LIGHTBIT> <TELL " (providing light)">>
                      <COND (<FSET? .I ,CONTBIT>
                             <COND (<FSET? .I ,OPENABLEBIT>
                                    <COND (<FSET? .I ,OPENBIT> <TELL " (open)">)
                                          (ELSE <TELL " (closed)">)>)>
                             <COND (<SEE-INSIDE? .I> <INV-DESCRIBE-CONTENTS .I>)>)>
                      <CRLF>>)
                 (ELSE
                  <TELL "You are empty-handed." CR>)>)
          (ELSE
           <TELL "It's too dark to see what you're carrying." CR>)>>

<ROUTINE V-TAKE ()
    <TRY-TAKE ,PRSO>
    <RTRUE>>

;"Attempts to take an object, implementing all of the default checks, and
  possibly printing a success or failure message.

Args:
  OBJ: The object to take.
  SILENT: If true, suppresses any success or failure message.

Returns:
  True if the object was taken."
<ROUTINE TRY-TAKE (OBJ "OPT" SILENT "AUX" HOLDER)
    <COND (<=? .OBJ ,WINNER>
           <COND (.SILENT)
                 (<=? ,P-V-WORD ,W?GET> <TELL "Not quite." CR>)
                 (<=? ,P-V-WORD ,W?TAKE ,W?GRAB> <TSD>)
                 (<=? ,P-V-WORD ,W?PICK> <TELL "You aren't my type." CR>)
                 (ELSE <SILLY>)>
           <RFALSE>)
          (<FSET? .OBJ ,PERSONBIT>
           <OR .SILENT <YOU-MASHER>>
           <RFALSE>)
          (<NOT <FSET? .OBJ ,TAKEBIT>>
           <OR .SILENT <NOT-POSSIBLE "pick up">>
           <RFALSE>)
          (<IN? .OBJ ,WINNER>
           <OR .SILENT <TELL "You already have that." CR>>
           <RFALSE>)>
    ;"See if picked up object is being taken from a container"
    <COND (<SET HOLDER <TAKE-HOLDER .OBJ ,WINNER>>
           <COND (<FSET? .HOLDER ,PERSONBIT>
                  <OR .SILENT <TELL "That seems to belong to " T .HOLDER "." CR>>
                  <RFALSE>)
                 (<BLOCKS-TAKE? .HOLDER>
                  <THIS-IS-IT .HOLDER>
                  <OR .SILENT <TELL CT .HOLDER " is in the way." CR>>
                  <RFALSE>)
                 (<NOT <TAKE-CAPACITY-CHECK .OBJ .SILENT>>)
                 (<AND <FSET? .HOLDER ,CONTBIT>
                       <HELD? .OBJ .HOLDER>
                       <NOT <HELD? ,WINNER .HOLDER>>>
                  <FSET .OBJ ,TOUCHBIT>
                  <MOVE .OBJ ,WINNER>
                  <COND (.SILENT)
                        (<SHORT-REPORT?> <TELL "Taken." CR>)
                        (ELSE
                         <TELL "You reach ">
                         <COND (<HELD? ,WINNER .HOLDER>
                                <TELL "out of ">)
                               (ELSE <TELL "in ">)>
                         <TELL T .HOLDER " and ">
                         <COND (<FSET? .OBJ ,WEARBIT>
                                <TELL "wear ">
                                <FSET .OBJ ,WORNBIT>)
                               (ELSE <TELL "take ">)>
                         <TELL T .OBJ "." CR>)>
                  <RTRUE>)>)>
    <COND (<NOT <TAKE-CAPACITY-CHECK .OBJ .SILENT>>
           <RFALSE>)
          (<FSET? .OBJ ,WEARBIT>
           <FSET .OBJ ,WORNBIT>
           <MOVE .OBJ ,WINNER>
           <FSET .OBJ ,TOUCHBIT>
           <COND (.SILENT)
                 (<SHORT-REPORT?> <TELL "Taken (and worn)." CR>)
                 (ELSE <TELL "You wear " T .OBJ "." CR>)>
           <RTRUE>)
          (ELSE
           <FSET .OBJ ,TOUCHBIT>
           <MOVE .OBJ ,WINNER>
           <COND (.SILENT)
                 (<SHORT-REPORT?> <TELL "Taken." CR>)
                 (ELSE <TELL "You pick up " T .OBJ "." CR>)>
           <RTRUE>)>>

;"Locates the container, person, or room that restricts the ability to take a
given object.

If at least one thing between the taker and the object is a closed container
or a person, the closest one to the taker will be returned. Otherwise, the
innermost non-surface container or room that encloses the object will be
returned.

Args:
  OBJ: The object being taken.
  TAKER: The object doing the taking.

Returns:
  The closed container or person blocking the take, or the open container
  or room allowing the take, or ROOMS if the objects have no common parent."
<ROUTINE TAKE-HOLDER (OBJ TAKER "AUX" CEIL BLOCKER ALLOWER HAD-ALLOWER?)
    <SET CEIL <COMMON-PARENT? .OBJ .TAKER>>
    <COND (<0? .CEIL> <RETURN ,ROOMS>)>
    ;"Walk up the tree from OBJ to CEIL"
    <COND (<N=? .OBJ .CEIL>
           <DO (L <LOC .OBJ> <0? .L> <SET L <LOC .L>>)
               <COND (<==? .L .CEIL>
                      <RETURN>)
                     (<BLOCKS-TAKE? .L>
                      ;"Keep the furthest blocker from OBJ"
                      <SET BLOCKER .L>)
                     (<OR <AND <FSET? .L ,CONTBIT>
                               <NOT <FSET? .L ,SURFACEBIT>>>
                          <IN? .L ,ROOMS>>
                      ;"Keep the closest allower to OBJ"
                      <OR .ALLOWER <SET ALLOWER .L>>)>>)>
    ;"Walk up the tree from TAKER to CEIL, setting variables in reverse"
    <SET HAD-ALLOWER? .ALLOWER>
    <COND (<N=? .TAKER .CEIL>
           <DO (L <LOC .TAKER> <0? .L> <SET L <LOC .L>>)
               <COND (<==? .L .CEIL>
                      <RETURN>)
                     (<BLOCKS-TAKE? .L>
                      ;"Keep the closest blocker to TAKER"
                      <OR .BLOCKER <SET BLOCKER .L>>)
                     (<OR <AND <FSET? .L ,CONTBIT>
                               <NOT <FSET? .L ,SURFACEBIT>>>
                          <IN? .L ,ROOMS>>
                      ;"Keep the furthest blocker from TAKER unless we already found
                        one on the first walk."
                      <OR .HAD-ALLOWER? <SET ALLOWER .L>>)>>)>
    <OR .BLOCKER .ALLOWER>>

<ROUTINE BLOCKS-TAKE? (OBJ)
    <T? <OR <FSET? .OBJ ,PERSONBIT>
            <AND <FSET? .OBJ ,CONTBIT>
                 <FSET? .OBJ ,OPENABLEBIT>
                 <NOT <FSET? .OBJ ,OPENBIT>>>>>>

<DEFMAC COMMON-PARENT? ('A 'B)
    <FORM COMMON-PARENT-R .A .B ',HERE>>

<ROUTINE COMMON-PARENT-R CPR (A B ROOT "AUX" N F R)
    <OR .ROOT <RFALSE>>
    ;"If ROOT is equal to A or B, it's the common parent"
    <COND (<EQUAL? .ROOT .A .B> <RETURN .ROOT>)>
    ;"Look for common parent in each subtree, keeping any matching
      tree and counting the number found."
    <MAP-CONTENTS (I .ROOT)
        <COND (<SET R <COMMON-PARENT-R .A .B .I>>
               <SET F .R>
               <SET N <+ .N 1>>
               ;"If we found matching parents in two children,
                ROOT is the common parent."
               <COND (<G? .N 1> <RETURN .ROOT .CPR>)>)>>
    ;"One child contained both objects, so the common parent is whatever
      COMMON-PARENT-R returned for it."
    .F>

<DEFAULT-DEFINITION TAKE-CAPACITY-CHECK

    <ROUTINE TAKE-CAPACITY-CHECK (O "OPT" SILENT "AUX" (CAP <GETP ,WINNER ,P?CAPACITY>) CWT NWT)
        <COND (<L? .CAP 0> <RTRUE>)>
        <SET CWT <- <WEIGHT ,WINNER> <GETP ,WINNER ,P?SIZE>>>
        <SET NWT <WEIGHT .O>>
        <COND (<G? <+ .CWT .NWT> .CAP>
               <COND (.SILENT)
                     (<SHORT-REPORT?>
                      <TELL "You're carrying too much." CR>)
                     (ELSE
                      <TELL "You're carrying too much to pick up " T .O "." CR>)>
               <RFALSE>)>
        <RTRUE>>
>

<ROUTINE PRE-DROP ()
    <COND (<NOT <IN? ,PRSO ,WINNER>>
           <SETG P-CONT 0>
           <PRINTR "You don't have that.">)>>

<ROUTINE V-DROP ()
    <MOVE ,PRSO ,HERE>
    <FSET ,PRSO ,TOUCHBIT>
    <FCLEAR ,PRSO ,WORNBIT>
    <COND (<SHORT-REPORT?> <TELL "Dropped." CR>)
          (ELSE <TELL "You drop " T ,PRSO "." CR>)>>

<ROUTINE PRE-PUT-ON ()
    <COND (<PRSI? ,WINNER> <PERFORM ,V?WEAR ,PRSO> <RTRUE>)
          (<PRSO? ,WINNER> <PERFORM ,V?ENTER ,PRSI> <RTRUE>)
          (<NOT <HAVE-TAKE-CHECK ,PRSO ,SF-HAVE>> <RTRUE>)>>

<ROUTINE V-PUT-ON ("AUX" S CCAP CSIZE X W B)
    <COND (<FSET? ,PRSI ,PERSONBIT> <YOU-MASHER ,PRSI> <RTRUE>)
          (<NOT <AND <FSET? ,PRSI ,CONTBIT>
                     <FSET? ,PRSI ,SURFACEBIT>>>
           <NOT-POSSIBLE "put things on">
           <RTRUE>)
          (<NOT <IN? ,PRSO ,WINNER>>
           <PRINTR "You don't have that.">)
          (<OR <EQUAL? ,PRSO ,PRSI> <HELD? ,PRSI ,PRSO>>
           <PRINTR "You can't put something on itself.">)
          (ELSE
           <SET S <GETP ,PRSO ,P?SIZE>>
           <COND (<G=? <SET CCAP <GETP ,PRSI ,P?CAPACITY>> 0>)
                 (ELSE
                  <SET CCAP 5>
                  ;"set bottomless flag"
                  <SET B 1>)>
           <SET CSIZE <GETP ,PRSI ,P?SIZE>>
           <COND (<OR <G? .S .CCAP> <G? .S .CSIZE>>
                  <TELL "That won't fit on " T ,PRSI "." CR>
                  <RETURN>)>
           <COND (<0? .B>
                  ;"Determine weight of contents of IO"
                  <SET W <CONTENTS-WEIGHT ,PRSI>>
                  <SET X <+ .W .S>>
                  <COND (<G? .X .CCAP>
                         <TELL "There's not enough room on " T ,PRSI "." CR>
                         <RETURN>)>
                  )>
           <MOVE ,PRSO ,PRSI>
           <FSET ,PRSO ,TOUCHBIT>
           <FCLEAR ,PRSO ,WORNBIT>
           <COND (<SHORT-REPORT?> <TELL "Done." CR>)
                 (ELSE <TELL "You put " T ,PRSO " on " T ,PRSI "." CR>)>)>>

<ROUTINE PRE-PUT-IN ()
    <COND (<PRSI? ,WINNER> <TSD> <RTRUE>)
          (<PRSO? ,WINNER> <PERFORM ,V?ENTER ,PRSI> <RTRUE>)
          (<NOT <HAVE-TAKE-CHECK ,PRSO ,SF-HAVE>> <RTRUE>)>>

<ROUTINE V-PUT-IN ("AUX" S CCAP CSIZE X W B)
    <COND (<FSET? ,PRSI ,PERSONBIT> <YOU-MASHER ,PRSI> <RTRUE>)
          (<OR <NOT <FSET? ,PRSI ,CONTBIT>>
               <FSET? ,PRSI ,SURFACEBIT>>
           <NOT-POSSIBLE "put things in">
           <RTRUE>)
          (<AND <NOT <FSET? ,PRSI ,OPENBIT>>
                <FSET? ,PRSI ,OPENABLEBIT>>
           <TELL CT ,PRSI " is closed." CR>)
          ;"always closed case"
          (<AND <NOT <FSET? ,PRSI ,OPENBIT>>
                <FSET? ,PRSI ,CONTBIT>>
           <TELL "You see no way to put things into " T ,PRSI  "." CR>)
          (<NOT <IN? ,PRSO ,WINNER>>
           <PRINTR "You aren't holding that.">)
          (<OR <EQUAL? ,PRSO ,PRSI> <HELD? ,PRSI ,PRSO>>
           <PRINTR "You can't put something in itself.">)
          (ELSE
           <SET S <GETP ,PRSO ,P?SIZE>>
           <COND (<G=? <SET CCAP <GETP ,PRSI ,P?CAPACITY>> 0>)
                 (ELSE
                  <SET CCAP 5>
                  ;"set bottomless flag"
                  <SET B 1>)>
           <SET CSIZE <GETP ,PRSI ,P?SIZE>>
        <COND (<OR <G? .S .CCAP>
                   <G? .S .CSIZE>>
               <TELL "That won't fit in " T ,PRSI "." CR>
               <RETURN>)>
        <COND (<0? .B>
               ;"Determine weight of contents of IO"
               <SET W <CONTENTS-WEIGHT ,PRSI>>
               ;<TELL "Back from Contents-weight loop" CR>
               <SET X <+ .W .S>>
               <COND (<G? .X .CCAP>
                      <TELL "There's not enough room in " T ,PRSI "." CR>
                      <RETURN>)>)>
        <MOVE ,PRSO ,PRSI>
        <FSET ,PRSO ,TOUCHBIT>
        <FCLEAR ,PRSO ,WORNBIT>
        <COND (<SHORT-REPORT?> <TELL "Done." CR>)
              (ELSE <TELL "You put " T ,PRSO " in " T ,PRSI "." CR>)>)>>

;"Calculates the weight of all objects in a container, non-recursively."
<ROUTINE CONTENTS-WEIGHT (O "AUX" W)
    ;"add size of objects inside container - does not recurse through containers
      within this container"
    <MAP-CONTENTS (I .O)
        <SET W <+ .W <GETP .I ,P?SIZE>>>>
    .W>

;"Calculates the weight of an object, including its contents recursively."
<ROUTINE WEIGHT (O "AUX" X W)
    ;"Unlike CONTENTS-WEIGHT - drills down through all contents, adding sizes of all objects + contents"
    ;"start with size of container itself"
    <SET W <GETP .O ,P?SIZE>>
    ;"add size of objects inside container"
    <MAP-CONTENTS (I .O)
         <COND (<OR <FSET? .I ,CONTBIT>
                    <FSET? .I ,PERSONBIT>>
                <SET X <WEIGHT .I>>
                <SET W <+ .W .X>>)
               (ELSE
                <SET W <+ .W <GETP .I ,P?SIZE>>>)>>
    .W>

<ROUTINE V-WEAR ()
    <COND (<FSET? ,PRSO ,WEARBIT>
           <PERFORM ,V?TAKE ,PRSO>)
          (ELSE <NOT-POSSIBLE "wear">)>
    <RTRUE>>

<ROUTINE V-UNWEAR ()
    <COND (<AND <FSET? ,PRSO ,WORNBIT>
                <IN? ,PRSO ,WINNER>>
           <PERFORM ,V?DROP ,PRSO>)
          (ELSE <TELL "You aren't wearing that." CR>)>>

<ROUTINE V-EAT ()
    <COND (<PRSO? ,WINNER> <TSD> <RTRUE>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER> <RTRUE>)
          (<FSET? ,PRSO ,EDIBLEBIT>
           <REMOVE ,PRSO>
           <COND (<SHORT-REPORT?> <TELL "Eaten." CR>)
                 (ELSE <TELL "You devour " T ,PRSO "." CR>)>)
          (ELSE <TELL "That's hardly edible." CR>)>>

<DEFMAC PRINT-GAME-BANNER ()
    <COND (<GASSIGNED? GAME-TITLE>
           #SPLICE (<VERSION? (ZIP) (ELSE <HLIGHT ,H-BOLD>)>
                    <TELL ,GAME-TITLE CR>
                    <VERSION? (ZIP) (ELSE <HLIGHT ,H-NORMAL>)>
                    <TELL ,GAME-DESCRIPTION CR>))
          (ELSE '<TELL ,GAME-BANNER CR>)>>

<ROUTINE V-VERSION ()
    <PRINT-GAME-BANNER>
    <TELL "Release ">
    <PRINTN <BAND <LOWCORE RELEASEID> *3777*>>
    <TELL " / Serial number ">
    <LOWCORE-TABLE SERIAL 6 PRINTC>
    <TELL %<STRING " / " ,ZIL-VERSION " lib " ,ZILLIB-VERSION>>
    <CRLF>>

<ROUTINE V-THINK-ABOUT ()
    <COND (<PRSO? ,WINNER>
           <TELL "Yes, yes, you're very important." CR>)
          (ELSE
           <TELL "You contemplate " T ,PRSO " for a bit, but nothing fruitful comes to mind." CR>)>>

<ROUTINE V-OPEN ()
    <COND (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER> <RTRUE>)
          (<NOT <FSET? ,PRSO ,OPENABLEBIT>> <NOT-POSSIBLE "open"> <RTRUE>)
          (<FSET? ,PRSO ,OPENBIT>
           <PRINTR "It's already open.">)
          (<FSET? ,PRSO ,LOCKEDBIT>
           <TELL "You'll have to unlock it first." CR>)
          (ELSE
           <FSET ,PRSO ,TOUCHBIT>
           <FSET ,PRSO ,OPENBIT>
           <COND (<SHORT-REPORT?> <TELL "Opened." CR>)
                 (ELSE
                  <TELL "You open " T ,PRSO "." CR>
                  <COND (<AND ,HERE-LIT
                              <FSET? ,PRSO ,CONTBIT>
                              <NOT <FSET? ,PRSO ,TRANSBIT>>>
                         <DESCRIBE-CONTENTS ,PRSO>)>)>
           <NOW-LIT?>)>>

<ROUTINE V-CLOSE ()
    <COND (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER> <RTRUE>)
          (<NOT <FSET? ,PRSO ,OPENABLEBIT>> <NOT-POSSIBLE "close"> <RTRUE>)
          ;(<FSET? ,PRSO ,SURFACEBIT> <NOT-POSSIBLE "close"> <RTRUE>)
          (<NOT <FSET? ,PRSO ,OPENBIT>>
           <PRINTR "It's already closed.">)
          (ELSE
           <FSET ,PRSO ,TOUCHBIT>
           <FCLEAR ,PRSO ,OPENBIT>
           <COND (<SHORT-REPORT?> <TELL "Closed." CR>)
                 (ELSE <TELL "You close " T ,PRSO "." CR>)>
           <NOW-DARK?>)>>

<ROUTINE V-LOCK ()
    <NOT-POSSIBLE "lock">
    <RTRUE>>

<ROUTINE V-UNLOCK ()
    <NOT-POSSIBLE "unlock">
    <RTRUE>>

<ROUTINE V-WAIT ("AUX" T INTERRUPT ENDACT)
    <SET T 1>
    <TELL "Time passes." CR>
    <REPEAT ()
        <HOOK-BEFORE-M-END>
        <SET ENDACT <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
        <HOOK-AFTER-M-END ENDACT>
        <HOOK-BEFORE-CLOCKER>
        <SET INTERRUPT <CLOCKER>>
        <HOOK-AFTER-CLOCKER INTERRUPT>
        <SET T <+ .T 1>>
        <COND (<OR <G? .T ,STANDARD-WAIT>
                   .ENDACT
                   .INTERRUPT>
               <RETURN>)>>>

<ROUTINE V-AGAIN ()
    <COND (<NOT <PST-PRSA ,AGAIN-STORAGE>>
           <TELL "Nothing to repeat." CR>
           <RTRUE>)>
    <SAVE-PARSER-RESULT ,TEMP-PARSER-RESULT>
    <RESTORE-PARSER-RESULT ,AGAIN-STORAGE>
    <PROG ()
        ;"if PRSO and/or PRSI are set, make sure the object(s) still pass the checks"
        <COND (<AND ,PRSO
                    <NOT ,PRSO-DIR>
                    <NOT <AND <STILL-VISIBLE-CHECK ,P-PRSOS>
                              <HAVE-TAKE-CHECK-TBL ,P-PRSOS <GETB ,P-SYNTAX ,SYN-OPTS1>>>>>
               <RETURN>)
              (<AND ,PRSI
                    <NOT <AND <STILL-VISIBLE-CHECK ,P-PRSIS>
                              <HAVE-TAKE-CHECK-TBL ,P-PRSIS <GETB ,P-SYNTAX ,SYN-OPTS2>>>>>
               <RETURN>)>
        ;"if we get here, they were unset or they passed"
        <MAIN-LOOP-HANDLE-COMMAND>>
    <RESTORE-PARSER-RESULT ,TEMP-PARSER-RESULT>
    <RTRUE>>

<ROUTINE V-READ ("AUX" T)
    <COND (<NOT <FSET? ,PRSO ,READBIT>> <NOT-POSSIBLE "read"> <RTRUE>)
          (<SET T <GETP ,PRSO ,P?TEXT>>
           <TELL .T CR>)
          (<SET T <GETP ,PRSO ,P?TEXT-HELD>>
           <COND (<IN? ,PRSO ,WINNER>
                  <TELL .T CR>)
                 (ELSE
                  <TELL "You must be holding that to be able to read it." CR>)>)
          (ELSE
           <PERFORM ,V?EXAMINE ,PRSO>)>>

<ROUTINE V-TURN-ON ()
    <COND (<PRSO? ,WINNER> <TSD> <RTRUE>)
          (<NOT <FSET? ,PRSO ,DEVICEBIT>> <NOT-POSSIBLE "switch on and off"> <RTRUE>)
          (<FSET? ,PRSO ,ONBIT>
           <TELL "It's already on." CR>)
          (ELSE
           <FSET ,PRSO ,ONBIT>
           <COND (<SHORT-REPORT?> <TELL "Switched on." CR>)
                 (ELSE <TELL "You switch on " T ,PRSO "." CR>)>)>>

<ROUTINE V-TURN-OFF ()
    <COND (<PRSO? ,WINNER>
           <TELL <PICK-ONE-R <PLTABLE "Baseball." "Cold showers.">> CR>)
          (<NOT <FSET? ,PRSO ,DEVICEBIT>>
           <NOT-POSSIBLE "switch on and off"> <RTRUE>)
          (<NOT <FSET? ,PRSO ,ONBIT>>
           <TELL "It's already off." CR>)
          (ELSE
           <FCLEAR ,PRSO ,ONBIT>
           <COND (<SHORT-REPORT?> <TELL "Switched off." CR>)
                 (ELSE <TELL "You switch off " T ,PRSO "." CR>)>)>>

<ROUTINE V-FLIP ()
    <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
           <COND (<FSET? ,PRSO ,SURFACEBIT>
                  <POINTLESS "Taking your frustration out on">)
                 (ELSE <NOT-POSSIBLE "switch on and off">)>)
          (<FSET? ,PRSO ,ONBIT>
           <PERFORM ,V?TURN-OFF ,PRSO>)
          (ELSE
           <PERFORM ,V?TURN-ON ,PRSO>)>
    <RTRUE>>

<ROUTINE V-PUSH ()
    <COND (<PRSO? ,WINNER> <TELL "No, you seem close to the edge." CR>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <POINTLESS "Pushing">)>
    <RTRUE>>

<ROUTINE V-PULL ()
    <COND (<PRSO? ,WINNER> <TELL "That would demean both of us." CR>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <POINTLESS "Pulling">)>
    <RTRUE>>

<ROUTINE V-YES ()
    <RHETORICAL>
    <RTRUE>>

<ROUTINE V-NO ()
    <RHETORICAL>
    <RTRUE>>

<ROUTINE V-DRINK ()
    <TELL "You aren't ">
    <ITALICIZE "that">
    <TELL " thirsty." CR>>

<ROUTINE V-FILL ()
    <BE-SPECIFIC>
    <RTRUE>>

<ROUTINE V-EMPTY ()
    <BE-SPECIFIC>
    <RTRUE>>

<ROUTINE V-SMELL ()
    <TELL "You smell nothing unexpected." CR>>

<ROUTINE V-ATTACK ()
    <COND (<PRSO? ,WINNER> <TELL "Let's hope it doesn't come to that." CR>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <POINTLESS "Taking your frustration out on">)>
    <RTRUE>>

<ROUTINE V-THROW-AT ()
    <COND (<PRSO? ,WINNER> <TELL "Get " <IF-PLURAL ,PRSO "them" "it"> " yourself." CR>)
          (<FSET? ,PRSI ,PERSONBIT> <YOU-MASHER ,PRSI>)
          (ELSE <POINTLESS "Taking your frustration out on" <> T>)>
    <RTRUE>>

<ROUTINE V-GIVE ()
    <COND (<PRSI? ,WINNER>
           <COND (<HELD? ,PRSO> <TELL "You already have that." CR>)
                 (ELSE <TELL "Get " <IF-PLURAL ,PRSO "them" "it"> " yourself." CR>)>)
          (<PRSO? ,WINNER> <SILLY>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (<NOT <FSET? ,PRSI ,PERSONBIT>> <NOT-POSSIBLE "give things to">)
          (ELSE <TELL CT ,PRSI <IF-PLURAL ,PRSI " don't" " doesn't">
                      " take " T ,PRSO "." CR>)>>

<ROUTINE V-SGIVE ()
    <PERFORM ,V?GIVE ,PRSI ,PRSO>
    <RTRUE>>

<ROUTINE PRE-TELL ()
    <COND (<OR <PRSO? ,WINNER> <NOT <FSET? ,PRSO ,PERSONBIT>>>
           <SETG P-CONT 0>
           <TELL "Talking to ">
           <COND (<PRSO? ,WINNER> <TELL "yourself">)
                 (ELSE <TELL A ,PRSO>)>
           <TELL ", huh?" CR>)>>

<ROUTINE V-TELL-ABOUT ()
    <TELL CT ,PRSO " doesn't seem interested." CR>>

;"TELL is a game verb, but it's defined here because it shares PRE-TELL"
<ROUTINE V-TELL ()
    <IF-DEBUG <COND (<0? ,P-CONT> <PRINTR "[P-CONT=0 in V-TELL]">)>>
    <SETG WINNER ,PRSO>
    <RTRUE>>

<ROUTINE V-WAVE-HANDS ()
    <POINTLESS "Waving your hands">
    <RTRUE>>

<ROUTINE V-WAVE ()
    <SILLY>
    <RTRUE>>

<ROUTINE V-CLIMB ()
    <COND (,PRSO <NOT-POSSIBLE "climb">) (ELSE <SILLY>)>
    <RTRUE>>

<ROUTINE V-SWIM ()
    <SILLY>
    <RTRUE>>

<ROUTINE V-JUMP ()
    <POINTLESS "Jumping in place">
    <RTRUE>>

<ROUTINE V-SING ()
    <TELL "You give a stirring performance of \"MacArthur Park\". Bravo!" CR>>

<ROUTINE V-DANCE ()
    <TELL "Dancing is forbidden." CR>>

<ROUTINE V-WAKE ()
    <COND (<PRSO? ,WINNER> <TELL "If only this were a dream." CR>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <NOT-POSSIBLE "wake">)>>

<ROUTINE V-RUB ()
    <COND (<PRSO? ,WINNER> <TSD>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <POINTLESS "Rubbing">)>>

<ROUTINE V-BURN ()
    <COND (<PRSO? ,WINNER> <TELL "What is this, the Friars Club?" CR>)
          (<FSET? ,PRSO ,PERSONBIT> <YOU-MASHER>)
          (ELSE <POINTLESS "Recklessly incinerating">)>>

;"Action handlers for game verbs"

<ROUTINE V-UNDO ()
    <IFFLAG
        (UNDO
         <COND (<NOT ,USAVE>
                <TELL "Cannot undo any further." CR>
                <RETURN>)
               (<NOT <IRESTORE>>
                <TELL "Undo failed." CR>)>)
        (ELSE <TELL "Undo is not available in this version." CR>)>>

<ROUTINE V-SAVE ()
    <TELL "Saving..." CR CR>
    <COND (<SAVE> <V-LOOK>)
          (ELSE <TELL "Save failed." CR>)>>

<ROUTINE V-RESTORE ()
    <COND (<NOT <RESTORE>>
           <TELL "Restore failed." CR>)>>

<ROUTINE V-RESTART ()
    <TELL "Are you sure you want to restart?">
    <COND (<YES?>
           <RESTART>)
          (ELSE
           <TELL "Restart aborted." CR>)>>

<ROUTINE V-BRIEF ()
    <TELL "Brief descriptions." CR>
    <SETG MODE ,BRIEF>>

<ROUTINE V-VERBOSE ()
    <TELL "Verbose descriptions." CR CR>
    <SETG MODE ,VERBOSE>
    <V-LOOK>>

<ROUTINE V-SUPERBRIEF ()
    <TELL "Superbrief descriptions." CR>
    <SETG MODE ,SUPERBRIEF>>

<ROUTINE V-SCRIPT ()
    <COND (<BTST <LOWCORE FLAGS> 1>
           <TELL "Transcript already on." CR>)
          (<AND <DIROUT 2>
                <BTST <LOWCORE FLAGS> 1>>
           <TELL "This begins a transcript of ">
           <V-VERSION>
           <RTRUE>)
          (ELSE <TELL "Failed." CR>)>>

<ROUTINE V-UNSCRIPT ()
    <COND (<NOT <BTST <LOWCORE FLAGS> 1>>
           <TELL "Transcript already off." CR>)
          (<AND <TELL CR "End of transcript." CR>
                <DIROUT -2>
                <BTST <LOWCORE FLAGS> 1>>
           <TELL "Failed." CR>)>>

;"Debugging verbs"
<IF-DEBUG

    <ROUTINE V-XTRACE ()
        <COND (<NOT <PRSO? ,NUMBER>>
               <TELL "Expected a number." CR>)
              (ELSE
               <SETG TRACE-LEVEL ,P-NUMBER>
               <TELL "Tracing level " N ,TRACE-LEVEL "." CR>)>>
>

<IF-DEBUGGING-VERBS

    <ROUTINE OBJREF? (O)
        <COND (<=? .O ,NUMBER>
               <COND (<AND <G=? ,P-NUMBER 1>
                           <L=? ,P-NUMBER ,LAST-OBJECT>>
                      ,P-NUMBER)
                     (ELSE
                      <TELL "[Bad objref.]" CR>
                      <>)>)
              (ELSE .O)>>

    <CONSTANT TREE-INDENT <ITABLE BYTE 80 <BYTE !\ >>>

    <ROUTINE V-XTREE ("AUX" OFL ROOT L)
        <SET OFL <LOWCORE FLAGS>>
        <LOWCORE FLAGS <ORB .OFL 2>>
        <PUTB ,TREE-INDENT 0 0>
        <COND (<SET ROOT <OBJREF? ,PRSO>>
               <COND (<SET L <LOC .ROOT>>
                      <PRINT-OBJREF .L>
                      <CRLF>)>
               <TREE-FROM .ROOT>)
              (ELSE
               <DO (I ,LAST-OBJECT 1 -1)
                   <COND (<IN? .I <>> <TREE-FROM .I>)>>)>
        <LOWCORE FLAGS .OFL>>

    <ROUTINE TREE-FROM (O "AUX" I)
        <PRINT-TREE-INDENT>
        <TELL "- ">
        <PRINT-OBJREF .O>
        <CRLF>
        <SET I <GETB ,TREE-INDENT 0>>
        <COND (<AND .I <=? <GETB ,TREE-INDENT .I> !\`>>
               <PUTB ,TREE-INDENT .I !\ >)>
        <INC I>
        <PUTB ,TREE-INDENT .I !\ >
        <INC I>
        <PUTB ,TREE-INDENT .I !\ >
        <INC I>
        <PUTB ,TREE-INDENT .I !\|>
        <PUTB ,TREE-INDENT 0 .I>
        <MAP-CONTENTS (C N .O)
            <COND (<NOT .N> <PUTB ,TREE-INDENT .I !\`>)>
            <TREE-FROM .C>>
        <PUTB ,TREE-INDENT 0 <- .I 3>>>

    <ROUTINE PRINT-TREE-INDENT ("AUX" MAX)
        <SET MAX <GETB ,TREE-INDENT 0>>
        <OR .MAX <RETURN>>
        <DO (I 1 .MAX)
            <PRINTC <GETB ,TREE-INDENT .I>>>>

    <ROUTINE PRINT-OBJREF (O)
        ;"Object name"
        <COND (<0? .O> <TELL "<>">)
              (<=? .O ,ROOMS> <TELL "ROOMS">)
              (<=? .O ,GLOBAL-OBJECTS> <TELL "GLOBAL-OBJECTS">)
              (<=? .O ,LOCAL-GLOBALS> <TELL "LOCAL-GLOBALS">)
              (<=? .O ,GENERIC-OBJECTS> <TELL "GENERIC-OBJECTS">)
              (ELSE <TELL D .O>)>
        ;"Object number"
        <TELL " (" N .O ")">>

    <ROUTINE PRINT-VARREF (N)
        <TELL !\[>
        <COND (<=? .N 0> <TELL "stack">)
              (<L=? .N 15> <TELL "local " N .N>)
              (<L=? .N 255> <TELL "global " N .N>)
              (ELSE <TELL "bad var " N .N>)>
        <TELL " = " <VALUE .N> !\]>>

    <CONSTANT FLAG-NAMES
        <PLTABLE
            !<MAPF ,LIST
                   <FUNCTION (FLAG) <MAPRET .FLAG <SPNAME .FLAG>>>
                   ,KNOWN-FLAGS>>>

    <ROUTINE PRINT-FLAGREF (N "AUX" MAX)
        <TELL N .N>
        <SET MAX <GET ,FLAG-NAMES 0>>
        <DO (I 1 .MAX 2)
            <COND (<=? <GET ,FLAG-NAMES .I> .N>
                   <TELL " (" <GET ,FLAG-NAMES <+ .I 1>> ")">
                   <RETURN>)>>>

    <ROUTINE V-XGOTO ("AUX" D)
        <COND (<SET D <OBJREF? ,PRSO>>
               <GOTO .D>)>>

    <ROUTINE V-XMOVE ("AUX" V D)
        <OR ,PRSI <SETG PRSI ,WINNER>>
        <COND (<AND <SET V <OBJREF? ,PRSO>>
                    <SET D <OBJREF? ,PRSI>>>
               <MOVE .V .D>
               <TELL "Moved ">
               <PRINT-OBJREF .V>
               <TELL " to ">
               <PRINT-OBJREF .D>
               <TELL "." CR>)>>

    <ROUTINE V-XREMOVE (V)
        <COND (<SET V <OBJREF? ,PRSO>>
               <REMOVE .V>
               <TELL "Removed ">
               <PRINT-OBJREF .V>
               <TELL "." CR>)>>

    <ROUTINE V-XLIGHT ()
        <COND (<FSET? ,WINNER ,LIGHTBIT>
               <FCLEAR ,WINNER ,LIGHTBIT>
               <TELL "You stop glowing." CR>
               <NOW-DARK?>)
              (ELSE
               <FSET ,WINNER ,LIGHTBIT>
               <TELL "You're now glowing." CR>
               <NOW-LIT?>)>>

    <ROUTINE V-XEXITS ("AUX" R S M)
        <COND (,PRSO <SET R <OBJREF? ,PRSO>>)
              (ELSE <SET R ,HERE>)>
        <OR .R <RTRUE>>
        <PRINT-OBJREF .R>
        <CRLF>
        <MAP-DIRECTIONS (D PT .R)
            <PRINT-MATCHING-WORD .D ,PS?DIRECTION ,P1?DIRECTION>
            <TELL " -> ">
            <SET S <PTSIZE .PT>>
            <COND (<=? .S ,UEXIT>
                   <TELL "TO ">
                   <PRINT-OBJREF <GET/B .PT ,EXIT-RM>>)
                  (<=? .S ,NEXIT>
                   <TELL "SORRY \"" <GET .PT ,NEXIT-MSG> "\"">)
                  (<=? .S ,FEXIT>
                   <TELL "PER " N <GET .PT ,FEXIT-RTN>>)
                  (<=? .S ,CEXIT>
                   <TELL "TO ">
                   <PRINT-OBJREF <GET/B .PT ,EXIT-RM>>
                   <TELL " IF ">
                   <PRINT-VARREF <GETB .PT ,CEXIT-VAR>>
                   <COND (<SET M <GET .PT ,CEXIT-MSG>>
                          <TELL " ELSE \"" .M "\"">)>)
                  (<=? .S ,DEXIT>
                   <TELL "TO ">
                   <PRINT-OBJREF <GET/B .PT ,EXIT-RM>>
                   <TELL " IF ">
                   <PRINT-OBJREF <GET/B .PT ,DEXIT-OBJ>>
                   <TELL " IS OPEN">
                   <COND (<SET M <GET .PT ,DEXIT-MSG>>
                          <TELL " ELSE \"" .M "\"">)>)
                  (ELSE
                   <TELL "??? S=" N .S>)>
            <CRLF>>>

    <ROUTINE V-XOBJ ("AUX" O F PT MAX)
        <COND (<NOT <SET O <OBJREF? ,PRSO>>> <RETURN>)>
        <PRINT-OBJREF .O>
        <CRLF>
        <TELL "Adjectives: ">
        <COND (<AND <SET PT <GETPT .O ,P?ADJECTIVE>>
                    <SET MAX <PTSIZE .PT>>>
               <VERSION? (ZIP) (ELSE <SET MAX </ .MAX 2>>)>
               <SET MAX <- .MAX 1>>
               <DO (I 0 .MAX)
                   <COND (.I <TELL ", ">)>
                   <VERSION?
                       (ZIP <PRINT-MATCHING-WORD <GETB .PT .I> ,PS?ADJECTIVE ,P1?ADJECTIVE>)
                       (ELSE <PRINTB <GET .PT .I>>)>>)>
        <CRLF>
        <TELL "Nouns: ">
        <COND (<AND <SET PT <GETPT .O ,P?SYNONYM>>
                    <SET MAX </ <PTSIZE .PT> 2>>>
               <SET MAX <- .MAX 1>>
               <DO (I 0 .MAX)
                   <COND (.I <TELL ", ">)>
                   <PRINTB <GET .PT .I>>>)>
        <CRLF>
        <TELL "Location: ">
        <PRINT-OBJREF <LOC .O>>
        <COND (<AND <SET PT <GETPT .O ,P?GLOBAL>>
                    <SET MAX <PTSIZE .PT>>>
               <VERSION? (ZIP) (ELSE <SET MAX </ .MAX 2>>)>
               <SET MAX <- .MAX 1>>
               <CRLF>
               <TELL "Local globals: ">
               <DO (I 0 .MAX)
                   <COND (.I <TELL ", ">)>
                   <PRINT-OBJREF <GET/B .PT .I>>>)>
        <COND (<SET PT <GETP .O ,P?THINGS>>
               <CRLF>
               <TELL "Pseudos: ">
               <PRINT-PSEUDOS .PT>)>
        <CRLF>
        <TELL "Flags: ">
        <DO (I 0 %<VERSION? (ZIP 31) (ELSE 47)>)
            <COND (<FSET? .O .I>
                   <COND (.F <TELL ", ">)>
                   <SET F T>
                   <PRINT-FLAGREF .I>)>>
        <CRLF>>

    <ROUTINE V-XIT ("AUX" O)
        <COND (<NOT <SET O <OBJREF? ,PRSO>>> <RETURN>)>
        <PUTB ,P-PRO-IT-OBJS 0 1>
        <PUT/B ,P-PRO-IT-OBJS 1 .O>
        <TELL "IT now refers to ">
        <PRINT-OBJREF .O>
        <TELL "." CR>>
>
