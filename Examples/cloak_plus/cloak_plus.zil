"Cloak of Darkness main file"

<VERSION XZIP>
<CONSTANT RELEASEID 1>

"Main loop"

<CONSTANT GAME-BANNER
"Cloak of Darkness|
A basic IF demonstration.|
Original game by Roger Firth|
ZIL conversion by Jesse McGrew with bits and pieces by Jayson Smith|
Additional material added for testing ZILF library by Josh Lawrence">


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

<INSERT-FILE "parser">

"Objects"

<OBJECT CEILING
    (DESC "ceiling")
    (SYNONYM CEILING)
    (IN GLOBAL-OBJECTS)
    (ACTION CEILING-R)>

<ROUTINE CEILING-R ()
    <COND (<VERB? EXAMINE> <TELL "Nothing really noticeable about the ceiling." CR>)>>

<OBJECT CLOAK
    (DESC "cloak")
    (SYNONYM CLOAK)
    (IN PLAYER)
    (FLAGS TAKEBIT WEARBIT WORNBIT)
    (ACTION CLOAK-R)>

<ROUTINE CLOAK-R ()
    <COND (<VERB? EXAMINE> <TELL "The cloak is unnaturally dark." CR>)>>

<OBJECT APPLE
    (DESC "apple")
    (SYNONYM APPLE)
    (IN FOYER)
    (FLAGS TAKEBIT VOWELBIT EDIBLEBIT)
    (ACTION APPLE-R)>

<ROUTINE APPLE-R ()
    <COND (<VERB? EXAMINE>
           <TELL "The apple is green and tasty-looking." CR>
           <QUEUE I-APPLE-FUN 3>)
          (<VERB? EAT>
           <JIGS-UP "Oh no! It was actually a poison apple (mostly so we could test JIGS-UP).">)>>

<ROUTINE I-APPLE-FUN ()
    <TELL "You looked at an apple 2 turns ago!" CR>>

<OBJECT GRAPES
    (DESC "grapes")
    (SYNONYM GRAPES)
    (LOC FOYERTABLE)
    (SIZE 1)
    (FLAGS TAKEBIT EDIBLEBIT PLURALBIT NARTICLEBIT)>

<OBJECT GRIME
    (DESC "grime")
    (SYNONYM GRIME)
    (IN FOYER)
    (FLAGS TAKEBIT NARTICLEBIT)
    (ACTION GRIME-R)>

<ROUTINE GRIME-R ()
    <COND (<VERB? EXAMINE> <TELL "A small but disgusting collection of crud." CR>
        ;<COND (<INTBL? "Phil Collins." SIGN-READS 3> <TELL "By the way, Phil Collins is one of the elements of the SIGN-READS table." CR>)>
        ;<COND (<INTBL? "Deerhoof." SIGN-READS 3> <TELL "By the way, Deerhoof is one of the elements of the SIGN-READS table." CR>)>
    <QUEUE I-GRIME-FUN 2>)>>

<ROUTINE I-GRIME-FUN ()
    <TELL "You looked at grime 1 turn ago!" CR>
    <RTRUE>>

<OBJECT CUBE
    (DESC "cube")
    (SYNONYM cube)
    (IN FOYER)
    (FLAGS TAKEBIT)
    (ACTION CUBE-R)>

<ROUTINE CUBE-R ()
    <COND (<VERB? EXAMINE> <TELL "As you inspect the cube you realize time around you speeds by." CR>
    <WAIT-TURNS 10>)>>

<OBJECT FOYERTABLE
    (DESC "table")
    (SYNONYM TABLE)
    (IN FOYER)
    (FLAGS CONTBIT SURFACEBIT)
    (ACTION TABLE-R)>

<ROUTINE TABLE-R ()
    <COND (<VERB? EXAMINE>
           <TELL "Tatty but functional." CR>
           <COND (<AND <FIRST? ,PRSO>> <DESCRIBE-CONTENTS ,PRSO>)>
           <QUEUE I-TABLE-FUN -1>)>>

<ROUTINE I-TABLE-FUN ()
    <TELL "You examined a table and now this event will run every turn, until you examine the brass hook, which will dequeue it." CR>>

<OBJECT CHANGING-PAINTING
    (DESC "painting")
    (SYNONYM PAINTING)
    (IN FOYER)
    (ACTION C-SIGN-R)>

<ROUTINE C-SIGN-R ()
    <COND (<VERB? EXAMINE>
           <TELL <PICK-ONE ,SIGN-DESCRIPS> CR>)
          (<VERB? READ>
           <TELL "The signature at the bottom rearranges itself to read "
                 <PICK-ONE-R ,SIGN-READS> CR>)>>

<GLOBAL SIGN-DESCRIPS
    <LTABLE
        2
        "It shows a dancing bear."
        "It displays a clown walking on its hands."
        "It shows a horse eating a shoe."
        "It shows a man hunting for a copy of Zork."
        "It displays a cat that is laughing."
        "It displays a machine marked with a Z.">>

<GLOBAL SIGN-READS
    <LTABLE
        "Micheangelo."
        "Phil Collins."
        "The Dude.">>

<OBJECT PLAYING-CARD
    (DESC "card")
    (SYNONYM CARD)
    (ADJECTIVE PLAYING)
    (LOC FOYERTABLE)
    (FLAGS TAKEBIT)
    (ACTION C-CARD-R)>

<ROUTINE C-CARD-R ()
    <COND (<VERB? EXAMINE> <TELL <PICK-ONE ,CARD-DESCRIPS> CR>)>>

<GLOBAL CARD-DESCRIPS
    <LTABLE
        2
        "Ace of Spades."
        "The Hermit."
        "The Weeping Joker.">>


<OBJECT DARKNESS
    (DESC "darkness")
    (SYNONYM DARKNESS DARK)
    (IN GENERIC-OBJECTS)
    (FLAGS NARTICLEBIT)
    (ACTION DARKNESS-R)>

<ROUTINE DARKNESS-R ()
    <COND (<VERB? THINK-ABOUT>
           <TELL "Light, darkness. Your favorite cloak has something to do with them, yes?" CR>)>>

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
    (GLOBAL RUG)
    (FLAGS LIGHTBIT)
    (ACTION FOYER-R)>

<ROUTINE FOYER-R (RARG)
    <COND
        (<==? .RARG ,M-END>
         ;<TELL "In the FOYER-R routine" CR>
         ;<TELL "PRSO is currently " D ,PRSO CR>
         <COND (<RUNNING? I-APPLE-FUN>
                <TELL "The Foyer routine detects that the Apple event will run this turn!" CR>)>
         <COND (<RUNNING? I-TABLE-FUN>
                <TELL "The Foyer routine detects that the Table event will run this turn!" CR>)>
         ;<COND (<NOT <FSET? ,PRSO ,CONTBIT>> <TELL "Yup, the " D ,PRSO " does not have contbit." CR>)>
        )>>

<ROOM BAR
    (DESC "Foyer Bar")
    (IN ROOMS)
    (LDESC "The bar, much rougher than you'd have guessed after the opulence
of the foyer to the north, is completely empty.")
    (NORTH TO FOYER)
    (GLOBAL RUG)
    (ACTION BAR-R)>

<GLOBAL DISTURBED 0>

<ROUTINE BAR-R (RARG)
    <COND
        (<==? .RARG ,M-ENTER>
         <COND (<FSET? ,CLOAK ,WORNBIT> <FCLEAR ,BAR ,LIGHTBIT>)
               (ELSE <FSET ,BAR ,LIGHTBIT>)>)
        (<==? .RARG ,M-BEG>
         <COND (<AND <NOT <FSET? ,BAR ,LIGHTBIT>>
                     <NOT <GAME-VERB?>>
                     <NOT <VERB? THINK-ABOUT>>
                     <NOT <VERB? LOOK>>
                     <NOT <AND <VERB? WALK> <==? ,PRSO ,P?NORTH>>>>
                <TELL "You grope around clumsily in the dark. Better be careful." CR>
                <SETG DISTURBED <+ ,DISTURBED 1>>)>)>>

<OBJECT MESSAGE
    (DESC "message")
    (SYNONYM MESSAGE FLOOR SAWDUST DUST)
    (ADJECTIVE SCRAWLED)
    (IN BAR)
    (FDESC "There seems to be some sort of message scrawled in the sawdust on the floor.")
    (ACTION MESSAGE-R)>

<ROUTINE MESSAGE-R ()
    <COND (<VERB? EXAMINE>
           <TELL "The message simply reads: \"You ">
           <COND (<G? ,DISTURBED 1> <TELL "lose.">)
                 (ELSE <TELL "win.">)>
           <TELL "\"" CR>)>
           <V-QUIT>>

<ROOM CLOAKROOM
    (DESC "Cloakroom")
    (IN ROOMS)
    (LDESC "The walls of this small room were clearly once lined with hooks,
though now only one remains. The exit is a door to the east, but there is also a cramped opening to the west.")
    (EAST TO FOYER)
    (WEST PER CLOAK-CHECK)
    (FLAGS LIGHTBIT)
    (ACTION CLOAKROOM-R)>

<ROUTINE CLOAKROOM-R (RARG)
    <COND
        (<==? .RARG ,M-ENTER>
         ;<TELL "In the CLOARKROOM-R routine" CR>
         <COND (<GLOBAL-IN? RUG FOYER>
                <TELL "Did you know that the rug is a local-global object in the Foyer and the Bar?" CR>)>
         <COND (<GLOBAL-IN? RUG CLOAKROOM>
                <TELL "We should not see this because the rug is not a local-global in the Cloakroom." CR>)>)>>

<ROUTINE CLOAK-CHECK ()
     <COND
         (<FSET? ,CLOAK ,WORNBIT>
          <TELL "You cannot enter the opening to the west while in possession of your cloak." CR>
          <RFALSE>)
         (ELSE <GOTO ,HALL-TO-STUDY> <RFALSE>)>>

<OBJECT BENTLEY
    (DESC "Bentley")
    (SYNONYM BENTLEY CAT)
    (ADJECTIVE GRAY STRIPED)
    (LDESC "Bentley is a gray striped cat. He is in a deep sleep.")
    (IN FOYER)
    (FLAGS PERSONBIT NARTICLEBIT)>

<OBJECT STELLA
    (DESC "Stella")
    (SYNONYM STELLA DOG CORGI)
    (ADJECTIVE BROWN)
    (LDESC "Stella is a brown corgi. She is in a deep sleep.")
    (IN FOYER)
    (FLAGS PERSONBIT NARTICLEBIT FEMALEBIT)>

<OBJECT HOOK
    (DESC "small brass hook")
    (IN CLOAKROOM)
    (SYNONYM HOOK PEG)
    (ADJECTIVE SMALL BRASS)
    (FDESC "A small brass hook is on the wall.")
    (FLAGS CONTBIT SURFACEBIT)
    (ACTION HOOK-R)>

<ROUTINE HOOK-R ()
    <COND (<VERB? EXAMINE>
           <TELL "Test: Normal examine replaced by a dequeue of the Table event." CR>
           <DEQUEUE I-TABLE-FUN>)>>

<ROOM HALL-TO-STUDY
    (DESC "Hallway to Study")
    (IN ROOMS)
    (LDESC "The hallway leads to a Study to the west, and back to the Cloakroom to the east.")
    (EAST TO CLOAKROOM)
    (WEST TO STUDY)
    (FLAGS LIGHTBIT)
    (ACTION HALL-TO-STUDY-R)>

<ROUTINE HALL-TO-STUDY-R (RARG)
    <COND
        (<==? .RARG ,M-ENTER>
         <TELL "Oof - it's cramped in here." CR>)
        (<==? .RARG ,M-END>
         <TELL "A spider scuttles across your feet and then disappears into a crack." CR>)>>

<ROOM STUDY
    (DESC "Study")
    (IN ROOMS)
    (LDESC "A small room with a worn stand in the middle.  A hallway lies east of here, a closet off to the west.")
    (EAST TO HALL-TO-STUDY)
    (WEST TO CLOSET)
    (FLAGS LIGHTBIT)
    (ACTION STUDY-R)>

<ROOM CLOSET
    (DESC "Closet")
    (IN ROOMS)
    (LDESC "A cramped excuse of a closet.")
    (EAST TO STUDY)
    (ACTION CLOSET-R)>

<ROUTINE CLOSET-R (RARG)
    <COND
        (<==? .RARG ,M-ENTER>
         <COND (<FSET? ,LSWITCH ,ONBIT>
                <FSET ,CLOSET ,LIGHTBIT>)
               (ELSE
                <FCLEAR ,CLOSET ,LIGHTBIT>)>)>>

<OBJECT LSWITCH
    (DESC "light switch")
    (IN STUDY)
    (SYNONYM SWITCH)
    (ADJECTIVE LIGHT)
    (LDESC "An ordinary light switch set in the wall beside the entrance to the closet.")
    (FLAGS DEVICEBIT)
    (ACTION LSWITCH-R)>

<ROUTINE LSWITCH-R ()
    <COND (<VERB? EXAMINE>
           <TELL "An ordinary light switch set in the wall to the left of the entrance to the
closet.  It is currently ">
           <COND (<FSET? ,LSWITCH ,ONBIT>
                  <TELL "on." CR>)
                 (ELSE
                  <TELL "off." CR>)>)>>

<OBJECT FLASHLIGHT
    (DESC "flashlight")
    (IN STUDY)
    (SYNONYM FLASHLIGHT)
    (ADJECTIVE CHEAP PLASTIC)
    (LDESC "A cheap plastic flashlight.")
    (FLAGS DEVICEBIT TAKEBIT)
    (ACTION FLASHLIGHT-R)>

<ROUTINE FLASHLIGHT-R ()
    <COND (<VERB? EXAMINE>
           <TELL "A cheap plastic flashlight.  It is currently ">
           <COND (<FSET? ,PRSO ,ONBIT>
                  <TELL "on." CR>)
                 (ELSE
                  <TELL "off." CR>)>)
           (<VERB? TURN-ON>
            <COND (<FSET? ,PRSO ,ONBIT>
                   <TELL "It's already on." CR>)
                  (ELSE
                   <FSET ,PRSO ,ONBIT>
                   <FSET ,PRSO ,LIGHTBIT>
                   <TELL "You switch on the " D ,PRSO "." CR>
                   <NOW-LIT?>)>
            <RTRUE>)
           (<VERB? TURN-OFF>
            <COND (<NOT <FSET? ,PRSO ,ONBIT>>
                   <TELL "It's already off." CR>)
                  (ELSE
                   <FCLEAR ,PRSO ,ONBIT>
                   <FCLEAR ,PRSO ,LIGHTBIT>
                   <TELL "You switch off the " D ,PRSO "." CR>
                   <NOW-DARK?>)>
            <RTRUE>)
           (<VERB? FLIP>
            <COND (<FSET? ,PRSO ,ONBIT>
                   <FCLEAR ,PRSO ,ONBIT>
                   <FCLEAR ,PRSO ,LIGHTBIT>
                   <TELL "You switch off the " D ,PRSO "." CR>
                   <NOW-DARK?>
                   <RTRUE>)
                  (ELSE
                   <FSET ,PRSO ,ONBIT>
                   <FSET ,PRSO ,LIGHTBIT>
                   <TELL "You switch on the " D ,PRSO "." CR>
                   <NOW-LIT?>)>
            <RTRUE>)>>

<OBJECT SIGN
    (DESC "sign")
    (SYNONYM SIGN)
    (ADJECTIVE CRUDE WOODEN)
    (IN HALL-TO-STUDY)
    (FDESC "A crude wooden sign hangs above the western exit.")
    (LDESC "It's a block of grey wood bearing hastily-painted words.")
    (TEXT "It reads, 'Welcome to the Study'")
    (FLAGS READBIT)>

<ROUTINE STUDY-R (RARG "AUX" R)
    <COND
        (<==? .RARG ,M-END>
         ;"The following should not interrupt a wait, since it has a RFALSE"
         <SET R <RANDOM 4>>
         <COND (<==? .R 1> <TELL "A mouse zips across the floor and into a hole." CR>)
               (<==? .R 2> <TELL "A faint scratching sound can be heard from the ceiling." CR>)>
         <RFALSE>)>>

<OBJECT STAND
    (DESC "stand")
    (SYNONYM STAND)
    (ADJECTIVE WORN)
    (IN STUDY)
    (CAPACITY 15)
    (FLAGS CONTBIT SURFACEBIT)>

<OBJECT BOOK
    (DESC "book")
    (SYNONYM BOOK)
    (ADJECTIVE TATTERED RED)
    (IN STAND)
    (LDESC "A tattered hard-cover book with a red binding.")
    (TEXT-HELD "It tells of an adventurer who was tasked with testing out a library that was old and new at the same time.")
    (FLAGS TAKEBIT READBIT)>

<OBJECT SAFE
    (DESC "safe")
    (SYNONYM SAFE)
    (ADJECTIVE SMALL)
    (IN STUDY)
    (FLAGS CONTBIT OPENABLEBIT)>

<OBJECT CASE
    (DESC "case")
    (SYNONYM CASE)
    (ADJECTIVE LARGE GLASS)
    (IN STUDY)
    (LDESC "A large glass case.")
    (FLAGS CONTBIT TRANSBIT)>

<OBJECT MUFFIN
    (DESC "muffin")
    (SYNONYM muffin)
    (ADJECTIVE TASTY)
    (IN CASE)
    (LDESC "A tasty-looking muffin.")
    (FLAGS TAKEBIT EDIBLEBIT)>

<OBJECT SPHERE
    (DESC "sphere")
    (SYNONYM SPHERE)
    (ADJECTIVE GLASS)
    (IN STUDY)
    (SIZE 1)
    (LDESC "A glass sphere.")
    (FLAGS TAKEBIT TRANSBIT CONTBIT)>

<OBJECT FIREFLY
    (DESC "firefly")
    (SYNONYM FIREFLY)
    (ADJECTIVE TINY BRIGHT GLOWING)
    (IN SPHERE)
    (LDESC "A tiny but brightly glowing firefly.")
    (FLAGS TAKEBIT LIGHTBIT)>

<OBJECT WALLET
    (DESC "wallet")
    (SYNONYM WALLET)
    (IN STUDY)
    (CAPACITY 2)
    (FLAGS CONTBIT TAKEBIT OPENABLEBIT)>

<OBJECT JAR
    (DESC "jar")
    (SYNONYM JAR)
    (IN STAND)
    (CAPACITY 6)
    (FLAGS CONTBIT OPENBIT TAKEBIT)>

<OBJECT CRATE
    (DESC "crate")
    (SYNONYM CRATE)
    (IN STUDY)
    (CAPACITY 15)
    (FLAGS CONTBIT)>

<OBJECT TRAY
    (DESC "tray")
    (SYNONYM TRAY)
    (ADJECTIVE SERVING)
    (IN STAND)
    (CAPACITY 11)
    (FLAGS CONTBIT TAKEBIT SURFACEBIT)>

<OBJECT PLUM
    (DESC "plum")
    (SYNONYM PLUM)
    (IN JAR)
    (SIZE 1)
    (FLAGS TAKEBIT EDIBLEBIT)>

 <OBJECT BILL
    (DESC "dollar")
    (SYNONYM DOLLAR BILL)
    (IN SAFE)
    (SIZE 1)
    (FLAGS TAKEBIT)>

<OBJECT RUG
    (DESC "rug")
    (IN LOCAL-GLOBALS)
    (SYNONYM RUG)
    (ACTION RUG-R)>

<ROUTINE RUG-R ()
    <COND (<VERB? PUT-ON>
           <TELL "You don't want to place anything on that tatty rug." CR>)>>
