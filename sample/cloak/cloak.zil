"Cloak of Darkness main file"

<VERSION ZIP>
<CONSTANT RELEASEID 2>

"Main loop"

<CONSTANT GAME-BANNER
"Cloak of Darkness|
A basic IF demonstration.|
Original game by Roger Firth|
ZIL conversion by Tara McGrew, Jayson Smith, and Josh Lawrence">

<SETG USE-SCORING? T>
<CONSTANT MAX-SCORE 2>

<ROUTINE GO ()
    <CRLF> <CRLF>
    <TELL "Hurrying through the rainswept November night, you're glad to see the
bright lights of the Opera House. It's surprising that there aren't more
people about but, hey, what do you expect in a cheap demo game...?" CR CR>
    <INIT-STATUS-LINE>
    <V-VERSION> <CRLF>
    <SETG HERE ,FOYER>
    <MOVE ,PLAYER ,HERE>
    <V-LOOK>
    <MAIN-LOOP>>

<DELAY-DEFINITION PRINT-GAME-OVER>

<INSERT-FILE "parser">

<SCORING-ACHIEVEMENTS
    (PUTTING-ON-HOOK "for putting the cloak away")
    (WINNING "for not trampling the message")>

"Objects"

<OBJECT CLOAK
    (DESC "cloak")
    (SYNONYM CLOAK)
    (ADJECTIVE HANDSOME VELVET SATIN BLACK)
    (IN PLAYER)
    (LDESC "Your cloak is in a rumpled pile on the floor.")
    (FLAGS TAKEBIT WEARBIT WORNBIT)
    (ACTION CLOAK-R)>

<ROUTINE CLOAK-R ()
    <COND (<VERB? EXAMINE>
           <TELL "A handsome cloak, of velvet trimmed with satin, and slightly
spattered with raindrops. Its blackness is so deep that it almost seems to suck
light from the room." CR>)
          (<VERB? DROP PUT-ON>
           <COND (<==? ,HERE ,CLOAKROOM>
                  <FSET ,BAR ,LIGHTBIT>
                  <RFALSE>)
                 (ELSE <TELL "This isn't the best place to leave a smart cloak
lying around." CR>)>)
          (<VERB? TAKE>
           <FCLEAR ,BAR ,LIGHTBIT>
           <RFALSE>)>>

<ROOM FOYER
    (DESC "Foyer of the Opera House")
    (IN ROOMS)
    (LDESC "You are standing in a spacious hall, splendidly decorated in red
and gold, with glittering chandeliers overhead. The entrance from
the street is to the north, and there are doorways south and west.")
    (SOUTH TO BAR)
    (WEST TO CLOAKROOM)
    (NORTH SORRY "You've only just arrived, and besides, the weather outside
seems to be getting worse.")
    (FLAGS LIGHTBIT)>

<ROOM BAR
    (DESC "Foyer Bar")
    (IN ROOMS)
    (LDESC "The bar, much rougher than you'd have guessed after the opulence
of the foyer to the north, is completely empty.")
    (NORTH TO FOYER)
    (ACTION BAR-R)>

<GLOBAL DISTURBED 0>

<ROUTINE BAR-R (RARG)
    <COND (<==? .RARG ,M-BEG>
           <COND (<OR <FSET? ,BAR ,LIGHTBIT>
                      <GAME-VERB?>
                      <VERB? LOOK>>
                  ;"Let the action proceed"
                  <RFALSE>)
                 (<VERB? WALK>
                  <COND (<N==? ,PRSO ,P?NORTH>
                         <SETG DISTURBED <+ ,DISTURBED 2>>
                         <TELL "Blundering around in the dark isn't a good idea!" CR>)>)
                 (ELSE
                  <SETG DISTURBED <+ ,DISTURBED 1>>
                  <TELL "In the dark? You could easily disturb something!" CR>)>)>>

<OBJECT MESSAGE
    (DESC "message")
    (SYNONYM MESSAGE FLOOR SAWDUST DUST)
    (ADJECTIVE SCRAWLED)
    (IN BAR)
    (FDESC "There seems to be some sort of message scrawled in the sawdust on the floor.")
    (ACTION MESSAGE-R)>

<ROUTINE MESSAGE-R ()
    <COND (<VERB? EXAMINE READ>
           <COND (<G? ,DISTURBED 1>
                  <JIGS-UP "The message has been carelessly trampled, making it
difficult to read. You can just distinguish the words...">)
                 (ELSE
                  <AWARD-POINTS 1 ,ACH?WINNING>
                  <JIGS-UP "The message, neatly marked in the sawdust, reads...">)>)>>

<REPLACE-DEFINITION PRINT-GAME-OVER
    <ROUTINE PRINT-GAME-OVER ()
        <TELL "    ****  You have ">
        <COND (<G? ,DISTURBED 1> <TELL "lost">)
              (ELSE <TELL "won">)>
        <TELL "  ****" CR>>>

<ROOM CLOAKROOM
    (DESC "Cloakroom")
    (IN ROOMS)
    (LDESC "The walls of this small room were clearly once lined with hooks,
though now only one remains. The exit is a door to the east.")
    (EAST TO FOYER)
    (FLAGS LIGHTBIT)
    (ACTION CLOAKROOM-R)>

<ROUTINE CLOAKROOM-R (RARG)
    <COND (<==? .RARG ,M-FLASH>
           <COND (<IN? ,CLOAK ,HOOK> <TELL CR "Your cloak is hanging on the hook." CR>)>)>>

<OBJECT HOOK
    (DESC "small brass hook")
    (IN CLOAKROOM)
    (SYNONYM HOOK PEG)
    (ADJECTIVE SMALL BRASS)
    (FLAGS NDESCBIT CONTBIT SURFACEBIT)
    (ACTION HOOK-R)>

<ROUTINE HOOK-R ()
    <COND (<VERB? EXAMINE>
           <TELL "It's just a small brass hook, ">
           <COND (<IN? ,CLOAK ,HOOK> <TELL "with a cloak hanging on it." CR>)
                 (ELSE <TELL "screwed to the wall." CR>)>)
          (<AND <VERB? PUT-ON> <PRSO? ,CLOAK>>
           <AWARD-POINTS 1 ,ACH?PUTTING-ON-HOOK>
           <RFALSE>)>>
