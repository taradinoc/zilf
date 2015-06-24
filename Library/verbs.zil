"Verbs"

<DIRECTIONS NORTH SOUTH EAST WEST NORTHEAST NORTHWEST SOUTHEAST SOUTHWEST
            IN OUT UP DOWN>

<SYNONYM NORTH N>
<SYNONYM SOUTH S>
<SYNONYM EAST E>
<SYNONYM WEST W>
<SYNONYM NORTHEAST NE>
<SYNONYM NORTHWEST NW>
<SYNONYM SOUTHEAST SE>
<SYNONYM SOUTHWEST SW>
<SYNONYM IN ENTER>
<SYNONYM OUT EXIT>
<SYNONYM UP U>
<SYNONYM DOWN D>

<SYNTAX LOOK = V-LOOK>
<VERB-SYNONYM LOOK L>

<SYNTAX WALK OBJECT = V-WALK>
<VERB-SYNONYM WALK GO>

<SYNTAX QUIT = V-QUIT>

<SYNTAX TAKE OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>
<VERB-SYNONYM TAKE GRAB>
<SYNTAX PICK UP OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>
<SYNTAX GET OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>

<SYNTAX DROP OBJECT (HAVE HELD CARRIED) = V-DROP>
<SYNTAX PUT DOWN OBJECT (HAVE HELD CARRIED) = V-DROP>	;"too many parts of speech for DOWN"

<SYNTAX EXAMINE OBJECT = V-EXAMINE>
<SYNTAX LOOK AT OBJECT = V-EXAMINE>
<VERB-SYNONYM EXAMINE X>

<SYNTAX WEAR OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>
<VERB-SYNONYM WEAR DON>
<SYNTAX PUT ON OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>

<SYNTAX UNWEAR OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>
<VERB-SYNONYM UNWEAR DOFF>
<SYNTAX TAKE OFF OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>

<SYNTAX PUT OBJECT (HAVE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON>
<SYNTAX PUT UP OBJECT (HAVE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON>
<VERB-SYNONYM PUT HANG PLACE>

<SYNTAX PUT OBJECT (HAVE HELD CARRIED) IN OBJECT (FIND CONTBIT) = V-PUT-IN>
<VERB-SYNONYM PUT PLACE INSERT>

<SYNTAX INVENTORY = V-INVENTORY>
<VERB-SYNONYM INVENTORY I>

<SYNTAX CONTEMPLATE OBJECT = V-THINK-ABOUT>
<VERB-SYNONYM CONTEMPLATE CONSIDER>
<SYNTAX THINK ABOUT OBJECT = V-THINK-ABOUT>

<SYNTAX OPEN OBJECT = V-OPEN>

<SYNTAX CLOSE OBJECT = V-CLOSE>
<VERB-SYNONYM SHUT>

<SYNTAX TURN ON OBJECT = V-TURN-ON>
<VERB-SYNONYM FLIP SWITCH>

<SYNTAX TURN OFF OBJECT = V-TURN-OFF>
<VERB-SYNONYM FLIP SWITCH>

<SYNTAX FLIP OBJECT = V-FLIP>
<VERB-SYNONYM TOGGLE>

<SYNTAX WAIT = V-WAIT>
<VERB-SYNONYM WAIT Z>

<SYNTAX AGAIN = V-AGAIN>
<VERB-SYNONYM AGAIN G>

<SYNTAX READ OBJECT = V-READ>
<VERB-SYNONYM READ PERUSE>

<SYNTAX EAT OBJECT = V-EAT>
<VERB-SYNONYM EAT SCARF DEVOUR GULP CHEW>

<SYNTAX PUSH OBJECT = V-PUSH>
<VERB-SYNONYM PUSH SHOVE>

<SYNTAX VERSION = V-VERSION>

<SYNTAX UNDO = V-UNDO>
<SYNTAX SAVE = V-SAVE>
<SYNTAX RESTORE = V-RESTORE>
<SYNTAX RESTART = V-RESTART>

<SYNTAX BRIEF = V-BRIEF>
<SYNTAX SUPERBRIEF = V-SUPERBRIEF>
<SYNTAX VERBOSE = V-VERBOSE>

;"debugging verbs - remove"
<IF-DEBUG
    <SYNTAX DROB OBJECT = V-DROB>
    <SYNTAX DSEND OBJECT TO OBJECT = V-DSEND>
    <SYNTAX DOBJL OBJECT = V-DOBJL>
    ;<SYNTAX DVIS = V-DVIS>
    ;<SYNTAX DMETALOC OBJECT = V-DMETALOC>
    ;<SYNTAX DACCESS OBJECT = V-DACCESS>
    ;<SYNTAX DHELD OBJECT IN OBJECT = V-DHELD>
    ;<SYNTAX DHELDP OBJECT = V-DHELDP>
    ;<SYNTAX DLIGHT = V-DLIGHT>
    <SYNTAX DCONT = V-DCONT>
    <SYNTAX DTURN = V-DTURN>
>

<CONSTANT M-BEG 1>
<CONSTANT M-END 2>
<CONSTANT M-ENTER 3>
<CONSTANT M-LOOK 4>

<ROUTINE V-BRIEF ()
    <TELL "Brief descriptions." CR>
    <SETG MODE ,BRIEF>>

<ROUTINE V-VERBOSE ()
    <TELL "Verbose descriptions." CR>
    <SETG MODE ,VERBOSE>
    <V-LOOK>>

<ROUTINE V-SUPERBRIEF ()
    <TELL "Superbrief descriptions." CR>
    <SETG MODE ,SUPERBRIEF>>

<ROUTINE V-LOOK ()
    <COND (<DESCRIBE-ROOM ,HERE T>
           <DESCRIBE-OBJECTS ,HERE>)>>

<ROUTINE DESCRIBE-ROOM (RM "OPT" LONG "AUX" P)
    <COND
        ;"check for light, unless running LOOK from NOW-LIT (which sets NLITSET to 1)"
        (<NOT <OR <FSET? .RM ,LIGHTBIT>
                  <SEARCH-FOR-LIGHT>
                  ,NLITSET>>
         <TELL "Darkness" CR "It is pitch black. You are likely to be eaten by a grue." CR>
         <RFALSE>)
        (ELSE
         ;"print the room's real name"
         <TELL D .RM CR>
         <COND (<EQUAL? ,MODE ,SUPERBRIEF>
                <RFALSE>)
               (<AND <NOT .LONG>
                     <FSET? .RM ,TOUCHBIT>
                     <NOT <EQUAL? ,MODE ,VERBOSE>>>
                <RTRUE>)>
         <FSET .RM ,TOUCHBIT>
         ;"either print the room's LDESC or call its ACTION with M-LOOK"
         <COND (<SET P <GETP .RM ,P?LDESC>>
                <TELL .P CR>)
               (ELSE
                <APPLY <GETP .RM ,P?ACTION> ,M-LOOK>)>)>>

<ROUTINE DESCRIBE-OBJECTS (RM "AUX" P F N S)
    <MAP-CONTENTS (I .RM)
        <COND
            ;"objects with DESCFNs"
            (<SET P <GETP .I ,P?DESCFCN>>
             <CRLF>
             <APPLY .P>
             <CRLF>)
            ;"un-moved objects with FDESCs"
            (<AND <NOT <FSET? .I ,TOUCHBIT>>
                  <SET P <GETP .I ,P?FDESC>>>
             <TELL CR .P CR>)
            ;"objects with LDESCs"
            (<SET P <GETP .I ,P?LDESC>>
             <TELL CR .P CR>)>>
    ;"use N add up all non fdesc, ndescbit, personbit objects in room"
    <MAP-CONTENTS (I .RM)
        <COND (<NOT <OR <FSET? .I ,NDESCBIT>
                        <SET P <GETP .I ,P?DESCFCN>>
                        <==? .I ,WINNER>
                        <FSET? .I ,PERSONBIT>
                        <AND <NOT <FSET? .I ,TOUCHBIT>>
                             <SET P <GETP .I ,P?FDESC>>>
                        <SET P <GETP .I ,P?LDESC>>>>
               <SET N <+ .N 1>>
               <COND (<==? .N 1> <SET F .I>)
                     (<==? .N 2> <SET S .I>)>)>>
    ;"go through the N objects"
    <COND (<G? .N 0>
           <TELL CR "There ">
           <COND (<FSET? .F ,PLURALBIT>
                  <TELL "are ">)
                 (ELSE
                  <TELL "is ">)>
           <COND (<==? .N 1> <TELL A .F>)
                 (<==? .N 2> <TELL A .F " and " A .S>)
                 (ELSE
                  <MAP-CONTENTS (I .RM)
                      <COND (<NOT <OR <FSET? .I ,NDESCBIT>
                                      <==? .I ,WINNER>
                                      <SET P <GETP .I ,P?LDESC>>
                                      <FSET? .I ,PERSONBIT>
                                      <SET P <GETP .I ,P?DESCFCN>>
                                      <AND <NOT <FSET? .I ,TOUCHBIT>>
                                           <SET P <GETP .I ,P?FDESC>>>>>
                             <TELL A .I>
                             <SET N <- .N 1>>
                             <COND (<0? .N>)
                                   (<==? .N 1> <TELL ", and ">)
                                   (ELSE <TELL ", ">)>)>>)>
           <TELL " here." CR>)>
    ;"describe visible contents of containers and surfaces"
    <MAP-CONTENTS (I .RM)
        <COND (<AND <NOT <FSET? .I ,INVISIBLE>>
                    <FIRST? .I>
                    <OR <FSET? .I ,SURFACEBIT>
                        <AND <FSET? .I ,CONTBIT>
                             <FSET? .I ,OPENBIT>>>>
               <DESCRIBE-CONTENTS .I>)>>
    ;"Re-use N to add up NPCs"
    <SET N 0>
    <MAP-CONTENTS (I .RM)
        <COND (<AND <FSET? .I ,PERSONBIT>
                    <NOT <OR <==? .I ,WINNER>
                             <FSET? .I ,NDESCBIT>
                             <SET P <GETP .I ,P?DESCFCN>>
                             <SET P <GETP .I ,P?LDESC>>
                             <AND <NOT <FSET? .I ,TOUCHBIT>>
                                  <SET P <GETP .I ,P?FDESC>>>>>>
               <SET N <+ .N 1>>
               <COND (<==? .N 1> <SET F .I>)
                     (<==? .N 2> <SET S .I>)>)>>
    ;"go through the N NPCs"
    <COND (<G? .N 0>
           ;<TELL CR>
           <COND (<==? .N 1> <TELL CT .F " is">)
                 (<==? .N 2> <TELL CT .F " and " T .S " are">)
                 (ELSE
                  <MAP-CONTENTS (I .RM)
                      <COND (<AND <FSET? .I ,PERSONBIT>
                                  <NOT <OR <==? .I ,WINNER>
                                           <FSET? .I ,NDESCBIT>
                                           <SET P <GETP .I ,P?DESCFCN>>
                                           <SET P <GETP .I ,P?LDESC>>
                                           <AND <NOT <FSET? .I ,TOUCHBIT>>
                                                <SET P <GETP .I ,P?FDESC>>>>>>
                             <COND (<==? .I .F> <TELL CT .I>) (ELSE <TELL T .I>)>
                             <SET N <- .N 1>>
                             <COND (<0? .N>)
                                   (<==? .N 1> <TELL ", and ">)
                                   (ELSE <TELL ",">)>)>>
                  <TELL " are">)>
           <TELL " here." CR>)>
    <SETG NLITSET <>>>

<ROUTINE INDEF-ARTICLE (OBJ)
    <COND (<FSET? .OBJ ,NARTICLEBIT>)
          (<FSET? .OBJ ,VOWELBIT> <TELL "an ">)
          (ELSE <TELL "a ">)>>

<ROUTINE DEF-ARTICLE (OBJ)
    <COND (<FSET? .OBJ ,NARTICLEBIT>)
          (ELSE <TELL "the ">)>>

<ROUTINE CINDEF-ARTICLE (OBJ)
    <COND (<FSET? .OBJ ,NARTICLEBIT>)
          (<FSET? .OBJ ,VOWELBIT> <TELL "An ">)
          (ELSE <TELL "A ">)>>

<ROUTINE CDEF-ARTICLE (OBJ)
    <COND (<FSET? .OBJ ,NARTICLEBIT>)
          (ELSE <TELL "The ">)>>

<ROUTINE PRINT-INDEF (OBJ)
    <INDEF-ARTICLE .OBJ> <PRINTD .OBJ>>

<ROUTINE PRINT-DEF (OBJ)
    <DEF-ARTICLE .OBJ> <PRINTD .OBJ>>

<ROUTINE PRINT-CINDEF (OBJ)
    <CINDEF-ARTICLE .OBJ> <PRINTD .OBJ>>

<ROUTINE PRINT-CDEF (OBJ)
    <CDEF-ARTICLE .OBJ> <PRINTD .OBJ>>

<ROUTINE DESCRIBE-CONTENTS (OBJ)
    <COND (<FSET? .OBJ ,SURFACEBIT> <TELL "On">)
          (ELSE <TELL "In">)>
    <TELL " " T .OBJ " ">
    <ISARE-LIST .OBJ>
    <TELL "." CR>>

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

<ROUTINE ISARE-LIST (O "AUX" N F)
    <SET F <FIRST? .O>>
    <COND (<NOT .F>
           <TELL "is nothing">
           <RETURN>)>
    <MAP-CONTENTS (I .O)
        <SET N <+ .N 1>>>
    <COND (<==? .N 1>
           <COND (<FSET? .F ,PLURALBIT>
                  <TELL "are ">)
                 (ELSE <TELL "is ">)>
           <TELL A .F>)
          (<==? .N 2>
           <TELL "are " A .F " and " A <NEXT? .F>>)
          (ELSE
           <TELL "are ">
           <MAP-CONTENTS (I .O)
               <TELL A .I>
               <SET N <- .N 1>>
               <COND (<0? .N>)
                     (<==? .N 1> <TELL ", and ">)
                     (ELSE <TELL ", ">)>>)>>

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

<DEFMAC META-VERB? ()
    '<VERB? QUIT VERSION WAIT SAVE RESTORE INVENTORY ;DLIGHT UNDO ;DSEND
            SUPERBRIEF BRIEF VERBOSE>>

<ROUTINE V-WALK ("AUX" PT PTS RM)
    <COND (<NOT ,PRSO-DIR>
           <PRINTR "You must give a direction to walk in.">)
          (<0? <SET PT <GETPT ,HERE ,PRSO>>>
           <PRINTR "You can't go that way.">)
          (<==? <SET PTS <PTSIZE .PT>> ,UEXIT>
           <SET RM <GET/B .PT ,EXIT-RM>>)
          (<==? .PTS ,NEXIT>
           <PRINT <GET .PT ,NEXIT-MSG>>
           <CRLF>
           <RTRUE>)
          (<==? .PTS ,FEXIT>
           <OR <SET RM <APPLY <GET .PT ,FEXIT-RTN>>>
               <RTRUE>>)
          (<==? .PTS ,CEXIT>
           <COND (<VALUE <GETB .PT ,CEXIT-VAR>>
                  <SET RM <GET/B .PT ,EXIT-RM>>)
                 (<SET RM <GET .PT ,CEXIT-MSG>>
                  <PRINT .RM>
                  <CRLF>
                  <RTRUE>)
                 (ELSE
                  <PRINTR "You can't go that way.">)>)
          (<==? .PTS ,DEXIT>
           ;"TODO: implement DEXIT"
           <PRINTR "Not implemented.">)
          (ELSE
           <TELL "Broken exit (" N .PTS ")." CR>
           <RTRUE>)>
    <GOTO .RM>>

<ROUTINE V-QUIT ()
    <TELL "Are you sure you want to quit?">
    <COND (<YES?>
           <TELL CR "Thanks for playing." CR>
           <QUIT>)
          (ELSE
           <TELL CR "OK - not quitting." CR>)>>

<ROUTINE V-EXAMINE ("AUX" P (N <>))
    <COND (<SET P <GETP ,PRSO P?LDESC>>
           <TELL .P CR>
           <SET N T>)>
    <COND (<SET P <GETP ,PRSO P?TEXT>>
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

<ROUTINE V-INVENTORY ()
    ;"check for light first"
    <COND (<OR <FSET? ,HERE ,LIGHTBIT>
               <SEARCH-FOR-LIGHT>>
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
           <TELL "It is too dark to see what you're carrying." CR>)>>

<ROUTINE V-TAKE ("AUX" HOLDER S X)
    <COND (<FSET? ,PRSO ,PERSONBIT>
           <TELL "I don't think " T ,PRSO " would appreciate that." CR>)
          (<NOT <FSET? ,PRSO ,TAKEBIT>>
           <PRINTR "That's not something you can pick up.">)
          (<IN? ,PRSO ,WINNER>
           <PRINTR "You already have that.">)
          (<FSET? ,PRSO ,WEARBIT>
           <TELL "You wear " T ,PRSO "." CR>
           <FSET ,PRSO ,WORNBIT>
           <MOVE ,PRSO ,WINNER>
           <FSET ,PRSO ,TOUCHBIT>)
          ;"See if picked up object is being taken from a container"
          (<SET HOLDER <TAKE-CONT-SEARCH ,HERE>>
           ;<TELL "HOLDER is currently " D .HOLDER CR>
           <COND (<FSET? .HOLDER ,SURFACEBIT>
                  <TELL "You pick up " T ,PRSO "." CR>
                  <FSET ,PRSO ,TOUCHBIT>
                  <MOVE ,PRSO ,WINNER>)
                 (<FSET? .HOLDER ,OPENBIT>
                  <TELL "You reach in " T .HOLDER " and take " T ,PRSO "." CR>
                  <FSET ,PRSO ,TOUCHBIT>
                  <MOVE ,PRSO ,WINNER>)
                 (ELSE
                  <TELL "The enclosing " D .HOLDER " prevents you from taking " T ,PRSO "." CR>)>)
          (ELSE
           <TELL "You pick up " T ,PRSO "." CR>
           <FSET ,PRSO ,TOUCHBIT>
           <MOVE ,PRSO ,WINNER>)>>

<ROUTINE TAKE-CONT-SEARCH TCS (A "AUX" H)
    <MAP-CONTENTS (I .A)
        ;<TELL "Looping to check containers.  I is currently " D .I CR>
        <COND (<OR <FSET? .I ,CONTBIT>
                   <==? .I ,WINNER>>
               ;<TELL "Found container " D .I CR>
               <COND (<IN? ,PRSO .I>
                      ;<TELL "PRSO is in I, setting HOLDER" CR>
                      <RETURN .I .TCS>)
                     (ELSE
                      <SET H <TAKE-CONT-SEARCH .I>>
                      <AND .H <RETURN .H .TCS>>)>)>>
    <RFALSE>>


<ROUTINE V-DROP ()
    <COND
        (<NOT <IN? ,PRSO ,WINNER>>
         <PRINTR "You don't have that.">)
        (ELSE
         <MOVE ,PRSO ,HERE>
         <FSET ,PRSO ,TOUCHBIT>
         <FCLEAR ,PRSO ,WORNBIT>
         <TELL "You drop " T ,PRSO "." CR>)>>

<ROUTINE INDIRECTLY-IN? (OBJ CONT)
    <REPEAT ()
        <COND (<0? .OBJ>
               <RFALSE>)
              (<EQUAL? <SET OBJ <LOC .OBJ>> .CONT>
               <RTRUE>)>>>

<ROUTINE V-PUT-ON ("AUX" S CCAP CSIZE X W B)
    <COND (<FSET? ,PRSI ,PERSONBIT>
           <TELL "I don't think " T ,PRSI " would appreciate that." CR>)
          (<NOT <AND <FSET? ,PRSI ,CONTBIT>
                     <FSET? ,PRSI ,SURFACEBIT>>>
           <TELL CT ,PRSI>
           <COND (<FSET? ,PRSI ,PLURALBIT> <TELL " aren't">)
                 (ELSE <TELL " isn't">)>
           <TELL " something you can put things on." CR>)
          (<NOT <IN? ,PRSO ,WINNER>>
           <PRINTR "You don't have that.">)
          (<OR <EQUAL? ,PRSO ,PRSI> <INDIRECTLY-IN? ,PRSI ,PRSO>>
           <PRINTR "You can't put something on itself.">)
          (ELSE
           <SET S <GETP ,PRSO ,P?SIZE>>
           <COND (<SET X <GETPT ,PRSI ,P?CAPACITY>>
                  <SET CCAP <GETP ,PRSI ,P?CAPACITY>>
                  ;<TELL D ,PRSI " has a capacity prop of " N .CCAP CR>)
                 (ELSE
                  ;<TELL D ,PRSI " has no capacity prop.  Will take endless amount of objects as long as each object is size 5 or under" CR>
                  <SET CCAP 5>
                  ;"set bottomless flag"
                  <SET B 1>)>
           <SET CSIZE <GETP ,PRSI ,P?SIZE>>
           ;<TELL D ,PRSO "size is " N .S ", " D ,PRSI " size is " N .CSIZE ", capacity ">
                 ;<COND (<0? .B>
                            ;<TELL N .CCAP CR>)
                        ;(ELSE <TELL "infinite" CR>)>
           <COND (<OR <G? .S .CCAP> <G? .S .CSIZE>>
                  <TELL "That won't fit on " T ,PRSI "." CR>
                  <RETURN>)>
           <COND (<0? .B>
                  ;"Determine weight of contents of IO"
                  <SET W <CONTENTS-WEIGHT ,PRSI>>
                  ;<TELL "Back from Contents-weight loop" CR>
                  <SET X <+ .W .S>>
                  <COND (<G? .X .CCAP>
                         <TELL "There's not enough room on " T ,PRSI "." CR>
                         ;<TELL D ,PRSO " of size " N .S " can't fit, since current weight of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>
                         <RETURN>)>
                  ; <TELL D ,PRSO " of size " N .S " can fit, since current weight of of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>
                  )>
           <MOVE ,PRSO ,PRSI>
           <FSET ,PRSO ,TOUCHBIT>
           <FCLEAR ,PRSO ,WORNBIT>
           <TELL "You put " T ,PRSO " on " T ,PRSI "." CR>)>>

<ROUTINE V-PUT-IN ("AUX" S CCAP CSIZE X W B)
    ;<TELL "In the PUT-IN routine" CR>
    <COND (<FSET? ,PRSI ,PERSONBIT>
           <TELL "I don't think " T ,PRSI " would appreciate that." CR>)
          (<OR <NOT <FSET? ,PRSI ,CONTBIT>>
               <FSET? ,PRSI ,SURFACEBIT>>
           <TELL CT ,PRSI>
           <COND (<FSET? ,PRSI ,PLURALBIT> <TELL " aren't">)
                 (ELSE <TELL " isn't">)>
           <TELL " something you can put things in." CR>)
          (<AND <NOT <FSET? ,PRSI ,OPENBIT>>
                <FSET? ,PRSI ,OPENABLEBIT>>
           <TELL CT ,PRSI " is closed." CR>)
          ;"always closed case"
          (<AND <NOT <FSET? ,PRSI ,OPENBIT>>
                <FSET? ,PRSI ,CONTBIT>>
           <TELL "You see no way to put things into " T ,PRSI  "." CR>)
          (<NOT <IN? ,PRSO ,WINNER>>
           <PRINTR "You aren't holding that.">)
          (<OR <EQUAL? ,PRSO ,PRSI> <INDIRECTLY-IN? ,PRSI ,PRSO>>
           <PRINTR "You can't put something in itself.">)
          (ELSE
           <SET S <GETP ,PRSO ,P?SIZE>>
           <COND (<SET X <GETPT ,PRSI ,P?CAPACITY>>
                  <SET CCAP <GETP ,PRSI ,P?CAPACITY>>
                  <IF-DEBUG <COND (,DBCONT
                                   <TELL D ,PRSI " has a capacity prop of " N .CCAP CR>)>>)
                 (ELSE
                  <IF-DEBUG <COND (,DBCONT
                                   <TELL D ,PRSI " has no capacity prop.  Will take endless amount of objects as long as each object is size 5 or under" CR>)>>
                  <SET CCAP 5>
                  ;"set bottomless flag"
                  <SET B 1>)>
           <SET CSIZE <GETP ,PRSI ,P?SIZE>>
           ;<COND (<AND ,DBCONT> <TELL D ,PRSI " has a size of " N .CSIZE CR>)>
        <IF-DEBUG <COND (,DBCONT
               <TELL D ,PRSO "size is " N .S ", " D ,PRSI " size is " N .CSIZE CR>)>>
        ;<COND (<0? .B>
        ;<TELL N .CCAP CR>)
        ;(ELSE <TELL "infinite" CR>)>
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
                      <IF-DEBUG <COND (,DBCONT
                             <TELL D ,PRSO " can't fit, since current bulk of "
                                   D ,PRSI "'s contents is " N .W " and "
                                   D ,PRSI "'s capacity is " N .CCAP CR>)>>
                      <RETURN>)>
               <IF-DEBUG <COND (,DBCONT
                      <TELL D ,PRSO " can fit, since current bulk of " D ,PRSI
                            "'s contents is " N .W " and " D ,PRSI "'s capacity is "
                            N .CCAP CR>)>>)>
        <MOVE ,PRSO ,PRSI>
        <FSET ,PRSO ,TOUCHBIT>
        <FCLEAR ,PRSO ,WORNBIT>
        <TELL "You put " T ,PRSO " in " T ,PRSI "." CR>)>>

<ROUTINE CONTENTS-WEIGHT (O "AUX" X W)
    ;"add size of objects inside container - does not recurse through containers
      within this container"
    <MAP-CONTENTS (I .O)
        ;<TELL "Content-weight loop for " D .O ", which contains " D .I CR>
        <SET W <+ .W <GETP .I ,P?SIZE>>>
        ;<TELL "Content weight of " D .O " is now " N .W CR>>
    ;<TELL "Total weight of contents of " D .O " is " N .W CR>
    .W>

<ROUTINE WEIGHT (O "AUX" X W)
    ;"Unlike CONTENTS-WEIGHT - drills down through all contents, adding sizes of all objects + contents"
    ;"start with size of container itself"
    <SET W <GETP .O ,P?SIZE>>
    ;"add size of objects inside container"
    <MAP-CONTENTS (I .O)
         ;<TELL "Looping to set weight.  I is currently " D .I CR>
         ;<SET W <+ .W <GETP .I ,P?SIZE>>>
         ;<TELL "Weight of " D .O " is now " N .W CR>
         <COND (<OR <FSET? .I ,CONTBIT>
                    <==? .I ,WINNER>>  ;"TODO: should check PERSONBIT"
                ;<TELL "Weightloop: found container " D .I CR>
                <SET X <WEIGHT .I>>
                <SET W <+ .W .X>>
                ;<TELL "Weightloop-containerloop: Weight of " D .O " is now " N .W CR>)
               (ELSE
                <SET W <+ .W <GETP .I ,P?SIZE>>>)>>
    ;<TELL "Total weight (its size + contents' size) of " D .O " is " N .W CR>
    .W>

<ROUTINE V-WEAR ()
    <COND (<FSET? ,PRSO ,WEARBIT>
           <PERFORM ,V?TAKE ,PRSO>)
          (ELSE
           <TELL "You can't wear that." CR>)>>

<ROUTINE V-UNWEAR ()
    <COND (<AND <FSET? ,PRSO ,WORNBIT>
                <IN? ,PRSO ,WINNER>>
           <PERFORM ,V?DROP ,PRSO>)
          (ELSE <TELL "You aren't wearing that." CR>)>>

<ROUTINE V-EAT ()
    <COND (<FSET? ,PRSO ,EDIBLEBIT>
           ;"TODO: improve this check will a real, drilling-down HELD? routine"
           <COND (<IN? ,PRSO ,WINNER>
                  <TELL "You devour " T ,PRSO CR>
                  <REMOVE ,PRSO>)
                 (ELSE <TELL "You're not holding that." CR>)>)
          (ELSE <TELL "That's hardly edible." CR>)>>

<ROUTINE V-VERSION ()
     <TELL ,GAME-BANNER "|Release ">
     <PRINTN <BAND <LOWCORE RELEASEID> *3777*>>
     <TELL " / Serial number ">
     <LOWCORE-TABLE SERIAL 6 PRINTC>
     <TELL %<STRING " / " ,ZIL-VERSION " lib " ,ZILLIB-VERSION>>
     <CRLF>>

<ROUTINE V-THINK-ABOUT ()
    <TELL "You contemplate " T ,PRSO " for a bit, but nothing fruitful comes to mind." CR>>

<ROUTINE V-OPEN ()
    <COND (<FSET? ,PRSO ,PERSONBIT>
           <TELL "I don't think " T ,PRSO " would appreciate that." CR>)
          (<NOT <FSET? ,PRSO ,OPENABLEBIT>>
           <PRINTR "That's not something you can open.">)
          (<FSET? ,PRSO ,OPENBIT>
           <PRINTR "It's already open.">)
          (<FSET? ,PRSO ,LOCKEDBIT>
           <TELL "You'll have to unlock it first." CR>)
          (ELSE
           <FSET ,PRSO ,TOUCHBIT>
           <FSET ,PRSO ,OPENBIT>
           <TELL "You open " T ,PRSO "." CR>
           <DESCRIBE-CONTENTS ,PRSO>)>>

<ROUTINE V-CLOSE ()
    <COND (<FSET? ,PRSO ,PERSONBIT>
           <TELL "I don't think " T ,PRSO " would appreciate that." CR>)
          (<NOT <FSET? ,PRSO ,OPENABLEBIT>>
           <PRINTR "That's not something you can close.">)
          ;(<FSET? ,PRSO ,SURFACEBIT>
           <PRINTR "That's not something you can close.">)
          (<NOT <FSET? ,PRSO ,OPENBIT>>
           <PRINTR "It's already closed.">)
          (ELSE
           <FSET ,PRSO ,TOUCHBIT>
           <FCLEAR ,PRSO ,OPENBIT>
           <TELL "You close " T ,PRSO "." CR>)>>

<ROUTINE V-WAIT ("AUX" T INTERRUPT ENDACT)
    <SET T 1>
    <TELL "Time passes." CR>
    <REPEAT ()
        ;<TELL "THE WAIT TURN IS " N .T CR>
        <SET ENDACT <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
        ;<TELL "ENDACT IS NOW " D .ENDACT CR>
        <SET INTERRUPT <CLOCKER>>
        ;<TELL "INTERRUPT IS NOW " D .INTERRUPT CR>
        <SET T <+ .T 1>>
        <COND (<OR <G? .T ,STANDARD-WAIT>
                   .ENDACT
                   .INTERRUPT>
               <RETURN>)>>>

<ROUTINE V-AGAIN ()
    <SETG AGAINCALL T>
    <RESTORE-READBUF>
    <RESTORE-LEX>
    ;<TELL "In V-AGAIN - Previous readbuf and lexbuf restored." CR>
    ;<DUMPBUF>
    ;<DUMPLEX>
    <COND (<NOT <EQUAL? <GET ,READBUF 1> 0>>
           <COND (<PARSER>
                  ;<TELL "Doing PERFORM within V-AGAIN" CR>
                  <PERFORM ,PRSA ,PRSO ,PRSI>
                  <COND (<NOT <META-VERB?>>
                         <APPLY <GETP ,HERE ,P?ACTION> ,M-END>
                         <CLOCKER>)>
                  <SETG HERE <LOC ,WINNER>>)>)
          (ELSE <TELL "Nothing to repeat." CR>)>>

<ROUTINE V-READ ("AUX" T)
    <COND (<NOT <FSET? ,PRSO ,READBIT>>
           <TELL "That's not something you can read." CR>)
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
    ;<TELL "CURRENTLY IN TURN-ON" CR>
    <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
           <TELL "That's not something you can switch on and off." CR>)
          (<FSET? ,PRSO ,ONBIT>
           <TELL "It's already on." CR>)
          (ELSE
           <FSET ,PRSO ,ONBIT>
           <TELL "You switch on " T ,PRSO "." CR>)>>

<ROUTINE V-TURN-OFF ()
    ;<TELL "CURRENTLY IN TURN-OFF" CR>
    <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
           <TELL "That's not something you can switch on and off." CR>)
          (<NOT <FSET? ,PRSO ,ONBIT>>
           <TELL "It's already off." CR>)
          (ELSE
           <FCLEAR ,PRSO ,ONBIT>
           <TELL "You switch off " T ,PRSO "." CR>)>>

<ROUTINE V-FLIP ()
    ;<TELL "CURRENTLY IN FLIP" CR>
    <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
           <TELL "That's not something you can switch on and off." CR>)
          (<FSET? ,PRSO ,ONBIT>
           <FCLEAR ,PRSO ,ONBIT>
           <TELL "You switch off " T ,PRSO "." CR>)
          (<NOT <FSET? ,PRSO ,ONBIT>>
           <FSET ,PRSO ,ONBIT>
           <TELL "You switch on " T ,PRSO "." CR>)>>

<ROUTINE V-PUSH ()
    <COND (<FSET? ,PRSO ,PERSONBIT>
           <TELL "I don't think " T ,PRSO " would appreciate that." CR>)
          (ELSE
           <TELL "Pushing " T ,PRSO " doesn't seem to accomplish much." CR>)>>

<ROUTINE V-UNDO ("AUX" R)
    <IFFLAG
        (UNDO
         <COND (<NOT ,USAVE>
                <TELL "Cannot undo any further." CR>
                <RETURN>)>
         <SET R <IRESTORE>>
         <COND (<EQUAL? .R 0>
                <TELL "Undo failed." CR>)>)
        (ELSE <TELL "Undo is not available in this version." CR>)>>

<ROUTINE V-SAVE ("AUX" S)
     ;<TELL "Now in save routine" CR>
    <TELL "Saving..." CR>
    <COND (<SAVE> <V-LOOK>)
          (ELSE <TELL "Save failed." CR>)>>

<ROUTINE V-RESTORE ("AUX" R)
    ; <TELL "Now in restore routine" CR>
    <COND (<NOT <RESTORE>>
           <TELL "Restore failed." CR>)>>

<ROUTINE V-RESTART ()
    <TELL "Are you sure you want to restart?" CR>
    <COND (<YES?>
           <RESTART>)
          (ELSE
           <TELL "Restart aborted." CR>)>>

;"Debugging verbs"
<IF-DEBUG
    <ROUTINE V-DROB ()
        <TELL "REMOVING CONTENTS OF " D ,PRSO " FROM THE GAME." CR>
        <ROB ,PRSO>>

    <ROUTINE V-DSEND ()
        <TELL "SENDING CONTENTS OF " D ,PRSO " TO " D ,PRSI "." CR>
        <ROB ,PRSO ,PRSI>>

    <ROUTINE V-DOBJL ()
        <MAP-CONTENTS (I ,PRSO)
            <TELL "The objects in " T ,PRSO " include " D .I CR>>>

    ;<ROUTINE V-DVIS ("AUX" P)
        <SET P <VISIBLE? ,BILL>>
        <COND (<NOT .P>
                    <TELL "The bill is not visible." CR>)
                        (ELSE
                    <TELL "The bill is visible." CR>)>
        <SET P <VISIBLE? ,GRIME>>
        <COND (<NOT .P>
                    <TELL "The grime is not visible." CR>)
                        (ELSE
                    <TELL "The grime is visible." CR>)>
        >

    ;<ROUTINE V-DMETALOC ("AUX" P)
        <SET P <META-LOC ,PRSO>>
        <COND (<NOT .P>
                <TELL "META-LOC returned false." CR>)
                  (ELSE
                <TELL "Meta-Loc of " D ,PRSO " is " D .P CR>)>
        <SET P <META-LOC ,GRIME>>
        <COND (<NOT .P>
                <TELL "META-LOC of grime returned false." CR>)
                  (ELSE
                <TELL "Meta-Loc of grime is " D .P CR>)
                >
        >

    ;<ROUTINE V-DACCESS ("AUX" P)
        <SET P <ACCESSIBLE? ,PRSO>>
        <COND (<NOT .P>
                <TELL D ,PRSO " is not accessible" CR>)
                  (ELSE
                <TELL D ,PRSO " is accessible" CR>)>
        <SET P <ACCESSIBLE? ,BILL>>
        <COND (<NOT .P>
                <TELL "Bill is not accessible" CR>)
                  (ELSE
                <TELL "Bill is accessible" CR>)>
        >

    ;<ROUTINE V-DHELD ("AUX" P)
        <SET P <HELD? ,PRSO ,PRSI>>
        <COND (<NOT .P>
                <TELL D ,PRSO " is not held by " D ,PRSI CR>)
                  (ELSE
                <TELL D ,PRSO " is held by " D ,PRSI CR>)>
        <SET P <HELD? ,PRSO ,FOYER>>
        <COND (<NOT .P>
                <TELL D ,PRSO " is not held by the Foyer" CR>)
                  (ELSE
                <TELL D ,PRSO " is held by the Foyer" CR>)>
        >

    ;<ROUTINE V-DHELDP ("AUX" P)
        <SET P <HELD? ,PRSO>>
        <COND (<NOT .P>
                <TELL D ,PRSO " is not held by the player" CR>)
                  (ELSE
                <TELL D ,PRSO " is held by the player" CR>)>
        <SET P <HELD? ,BILL>>
        <COND (<NOT .P>
                <TELL "Bill is not held by the player" CR>)
                  (ELSE
                <TELL "Bill is held by the player" CR>)>
        >

    ;<ROUTINE V-DLIGHT ()
        <COND (<FSET? ,FLASHLIGHT ,LIGHTBIT>
                    <FCLEAR ,FLASHLIGHT ,LIGHTBIT>
                    <FCLEAR ,FLASHLIGHT ,ONBIT>
                    <TELL "Flashlight is turned off." CR>
                    <NOW-DARK>)
                  (ELSE
                    <TELL "Flashlight is turned on." CR>
                    <NOW-LIT>
                    ;"always set LIGHTBIT after calling NOW-LIT"
                    <FSET ,FLASHLIGHT ,LIGHTBIT>
                    <FSET ,FLASHLIGHT ,ONBIT>
                    )>
        >

    <ROUTINE V-DCONT ()
        <COND (,DBCONT
               <SET DBCONT <>>
               <TELL "Reporting of PUT-IN process with containers turned off." CR>)
              (ELSE
               <SET DBCONT T>
               <TELL "Reporting of PUT-IN process with containers turned on." CR>)>>

    <ROUTINE V-DTURN ()
        <COND (,DTURNS
               <SET DTURNS <>>
               <TELL "Reporting of TURN # turned off." CR>)
              (ELSE
               <SET DTURNS T>
               <TELL "Reporting of TURN # turned on." CR>)>>>
