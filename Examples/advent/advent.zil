;"TODO: Replace scenery objects with PSEUDO / THINGS once implemented."
;"TODO: DESCRIBE-OBJECTS should mention special LOCAL-GLOBALS?"
;"TODO: Add CANT-GO property?"
;"TODO: Signs, magazines, etc. should respond to READ as well as EXAMINE."

<VERSION ZIP>
<CONSTANT RELEASEID 1>
<CONSTANT IFID-ARRAY <PTABLE (STRING) "UUID://0E123F50-20A2-4F5B-8F01-264678ED419D//">>

;"OIL and WATER appear as verbs, nouns, and prepositions (as part of POUR).
  This isn't allowed in the original vocabulary system, but NEW-VOC? allows it
  by discarding the noun value (which was always set to 1)."
;"TODO: Enable NEW-VOC? once the parser supports it."
;<SETG NEW-VOC? T>

"Main loop"

<CONSTANT GAME-BANNER
"ADVENTURE|
A Modern Classic|
Based on Adventure by Willie Crowther and Don Woods (1977)|
And prior adaptations by David M. Baggett (1993), Graham Nelson (1994), and Kent Tessman (1995)|
Adapted once more by Jesse McGrew (2015)">

;"The main entry point."
<ROUTINE GO ()
    <CRLF> <CRLF>
    <TELL "IT WAS THE SEVENTIES AND THERE WAS TIME FOR..." CR CR>   ;XXX
    <V-VERSION> <CRLF>
    <SETG HERE ,AT-END-OF-ROAD>
    <SETG SCORE 36>
    <QUEUE I-DWARF -1>
    <QUEUE I-PIRATE -1>
    <QUEUE I-CAVE-CLOSER -1>
    <MOVE ,PLAYER ,HERE>
    <V-LOOK>
    <REPEAT ()
        <COND (<PARSER>
               <PERFORM ,PRSA ,PRSO ,PRSI>
               <COND (<NOT <GAME-VERB?>>
                      <APPLY <GETP ,HERE ,P?ACTION> ,M-END>
                      <CLOCKER>)>)>
        <SETG HERE <LOC ,WINNER>>>>

<INSERT-FILE "parser">

;"Utility macro for randomness"
<DEFMAC PROB ('N)
    <FORM L=? '<RANDOM 100> .N>>

;"Properties"
<PROPDEF DEPOSIT-POINTS 10>

;"Treasures"
<GLOBAL TREASURES-FOUND 0>
<CONSTANT MAX-TREASURES 15>
;"TODO: <CONSTANT MAX-CARRIED 7>"
;"TODO: <CONSTANT MAX-SCORE 350>"

;----------------------------------------------------------------------
"The outside world"
;----------------------------------------------------------------------

<ROOM AT-END-OF-ROAD
    (DESC "At End Of Road")
    (IN ROOMS)
    (GLOBAL WELL-HOUSE STREAM ROAD FOREST)
    (LDESC "You are standing at the end of a road before a small brick building.
Around you is a forest.
A small stream flows out of the building and down a gully.")
    (WEST TO AT-HILL-IN-ROAD)
    (UP TO AT-HILL-IN-ROAD)
    (EAST TO INSIDE-BUILDING)
    (DOWN TO IN-A-VALLEY)
    (SOUTH TO IN-A-VALLEY)
    (NORTH PER RANDOM-FOREST)
    (IN TO INSIDE-BUILDING)
    (FLAGS LIGHTBIT SACREDBIT)>

<OBJECT WELL-HOUSE
    (DESC "well house")
    (IN LOCAL-GLOBALS)
    (SYNONYM WELL HOUSE BUILDING WELLHOUSE)
    (ADJECTIVE WELL BRICK SMALL)
    (TEXT "It's a small brick building. It seems to be a well house.")
    (ACTION WELL-HOUSE-F)>

<ROUTINE WELL-HOUSE-F ()
    <COND (<VERB? ENTER>
           <COND (<AND <IN? ,WINNER ,AT-HILL-IN-ROAD>
                       <NOT <FSET? ,INSIDE-BUILDING ,TOUCHBIT>>>
                  <TELL "It's too far away." CR>)
                 (ELSE <GOTO ,INSIDE-BUILDING>)>)>>

<OBJECT STREAM
    (DESC "stream")
    (IN LOCAL-GLOBALS)
    (SYNONYM STREAM WATER ;BROOK RIVER LAKE ;RESERVOIR)
    (ADJECTIVE SMALL TUMBLING SPLASHING BABBLING RUSHING)
    (ACTION STREAM-F)>

<ROUTINE STREAM-F ()
    <COND (<VERB? DRINK>
           <TELL "You have taken a drink from the stream.
The water tastes strongly of animals, but is not unpleasant.
It is extremely cold." CR>)
          (<VERB? TAKE>
           <COND (<HELD? ,BOTTLE>
                  <PERFORM ,V?FILL ,BOTTLE>
                  <RTRUE>)
                 (ELSE <TELL "You have nothing in which to carry the water." CR>)>)
          (<AND <VERB? PUT-IN> <PRSI? ,STREAM>>
           <COND (<PRSO? ,MING-VASE>
                  <REMOVE ,PRSO>
                  <MOVE ,SHARDS ,HERE>
                  <SETG SCORE <- ,SCORE 5>>
                  <TELL "The sudden change in temperature has delicately shattered the vase." CR>)
                 (<PRSO? ,BOTTLE>
                  <PERFORM ,V?FILL ,BOTTLE>
                  <RTRUE>)
                 (ELSE
                  <REMOVE ,PRSO>
                  <COND (<FSET? ,PRSO ,TREASUREBIT>
                         <SETG SCORE <- ,SCORE 5>>)>
                  <TELL CT ,PRSO " wash">
                  <COND (<NOT <FSET? ,PRSO ,PLURALBIT>> <TELL "es">)>
                  <TELL " away with the stream." CR>)>)>>

<OBJECT ROAD
    (DESC "road")
    (IN LOCAL-GLOBALS)
    (SYNONYM ROAD STREET PATH DIRT)
    (TEXT "The road is dirt, not yellow brick.")>

<OBJECT FOREST
    (DESC "forest")
    (IN LOCAL-GLOBALS)
    (SYNONYM FOREST TREE TREES ;OAK ;MAPLE ;GROVE ;PINE ;SPRUCE ;BIRCH ;ASH
             ;SAPLINGS ;BUSHES ;LEAVES ;BERRY ;BERRIES ;HARDWOOD)
    (TEXT "The trees of the forest are large hardwood oak and maple, with an
occasional grove of pine or spruce.
There is quite a bit of undergrowth, largely birch and ash saplings plus
nondescript bushes of various sorts.
This time of year visibility is quite restricted by all the leaves, but travel
is quite easy if you detour around all the spruce and berry bushes.")
    (FLAGS MULTITUDEBIT)>

;----------------------------------------------------------------------

<ROOM AT-HILL-IN-ROAD
    (DESC "At Hill In Road")
    (IN ROOMS)
    (GLOBAL WELL-HOUSE ROAD FOREST)
    (LDESC "You have walked up a hill, still in the forest.
The road slopes back down the other side of the hill.
There is a building in the distance.")
    (EAST TO AT-END-OF-ROAD)
    (NORTH TO AT-END-OF-ROAD)
    (DOWN TO AT-END-OF-ROAD)
    (SOUTH PER RANDOM-FOREST)
    (FLAGS LIGHTBIT SACREDBIT)>

<OBJECT HILL
    (DESC "hill")
    (IN AT-HILL-IN-ROAD)
    (SYNONYM HILL BUMP INCLINE)
    (TEXT "It's just a typical hill.")
    (FLAGS NDESCBIT)>

<OBJECT OTHER-SIDE-OF-HILL
    (DESC "other side of hill")
    (IN AT-HILL-IN-ROAD)
    (SYNONYM SIDE)
    (ADJECTIVE OTHER)
    (TEXT "Why not explore it yourself?")
    (FLAGS NDESCBIT VOWELBIT)>

;----------------------------------------------------------------------

<ROOM INSIDE-BUILDING
    (DESC "Inside Building")
    (IN ROOMS)
    (GLOBAL WELL-HOUSE STREAM)
    (LDESC "You are inside a building, a well house for a large spring.")
    (ACTION INSIDE-BUILDING-F)
    (WEST TO AT-END-OF-ROAD)
    (OUT TO AT-END-OF-ROAD)
    (IN SORRY "The pipes are too small.")
    (FLAGS LIGHTBIT SACREDBIT)>

<CONSTANT STREAM-FLOWS-OUT
    "The stream flows out through a pair of 1 foot diameter sewer pipes.">

<ROUTINE INSIDE-BUILDING-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<AND <VERB? WALK> <0? <GETPT ,HERE ,PRSO>>>
                  <TELL ,STREAM-FLOWS-OUT CR "The only exit is to the west." CR>)
                 (<AND <VERB? ENTER> <PRSO? ,SPRING ,SEWER-PIPES>>
                  <TELL ,STREAM-FLOWS-OUT CR "It would be advisable to use the exit." CR>)
                 (<VERB? XYZZY>
                  <OR <FSET? ,IN-DEBRIS-ROOM ,TOUCHBIT> <RFALSE>>
                  <GOTO ,IN-DEBRIS-ROOM>
                  <RTRUE>)
                 (<VERB? PLUGH>
                  <OR <FSET? ,AT-Y2 ,TOUCHBIT> <RFALSE>>
                  <GOTO ,AT-Y2>
                  <RTRUE>)>)>>

<OBJECT SPRING
    (DESC "spring")
    (IN INSIDE-BUILDING)
    (SYNONYM SPRING)
    (ADJECTIVE LARGE)
    (TEXT ,STREAM-FLOWS-OUT)
    (FLAGS NDESCBIT)>

<OBJECT SEWER-PIPES
    (DESC "pair of 1 foot diameter sewer pipes")
    (IN INSIDE-BUILDING)
    (SYNONYM PIPES PIPE)
    (ADJECTIVE PAIR FOOT DIAMETER SEWER)
    (TEXT "Too small. The only exit is to the west.")
    (FLAGS NDESCBIT)>

<OBJECT SET-OF-KEYS
    (DESC "set of keys")
    (SYNONYM KEY KEYS KEYRING SET)
    (ADJECTIVE SET)
    (IN INSIDE-BUILDING)
    (FDESC "There are some keys on the ground here.")
    (TEXT "It's just a normal-looking set of keys.")
    (ACTION SET-OF-KEYS-F)
    (FLAGS TAKEBIT)>

<ROUTINE SET-OF-KEYS-F ()
    <COND (<VERB? COUNT> <TELL "A dozen or so keys." CR>)>>

<OBJECT TASTY-FOOD
    (DESC "tasty food")
    (IN INSIDE-BUILDING)
    (SYNONYM FOOD RATION TRIPE
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP #SPLICE ()) (ELSE RATIONS)>)
    (ADJECTIVE YUMMY TASTY DELICIOUS SCRUMPTIOUS)
    (ARTICLE "some")
    (FDESC "There is tasty food here.")
    (TEXT "Sure looks yummy!")
    (ACTION TASTY-FOOD-F)
    (FLAGS TAKEBIT EDIBLEBIT)>

<ROUTINE TASTY-FOOD-F ()
    <COND (<VERB? EAT>
           <TELL "Delicious!" CR>
           <REMOVE ,TASTY-FOOD>)>>

<OBJECT BRASS-LANTERN
    (DESC "brass lantern")
    (IN INSIDE-BUILDING)
    (SYNONYM LAMP HEADLAMP LANTERN LIGHT)
    (ADJECTIVE SHINY BRASS)
    (DESCFCN BRASS-LANTERN-DESCFCN)
    (ACTION BRASS-LANTERN-F)
    (FLAGS TAKEBIT DEVICEBIT)>

<ROUTINE BRASS-LANTERN-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)
          (<FSET? ,BRASS-LANTERN ,LIGHTBIT>
           <TELL "Your lamp is here, gleaming brightly.">)
          (ELSE <TELL "There is a shiny brass lamp nearby.">)>>

<ROUTINE BRASS-LANTERN-F ()
    <COND (<VERB? EXAMINE>
           <TELL "It is a shiny brass lamp">
           <COND (<NOT <FSET ,PRSO ,LIGHTBIT>>
                  <TELL ". It is not currently lit." CR>)
                 (<L? ,LANTERN-POWER 30>
                  <TELL ", glowing dimly." CR>)
                 (ELSE <TELL ", glowing brightly." CR>)>)
          (<VERB? BURN>
           <PERFORM ,V?TURN-ON ,PRSO>
           <RTRUE>)
          (<VERB? RUB>
           <TELL "Rubbing the electric lamp is not particularly rewarding.
Anyway, nothing happens." CR>)
          (<VERB? TURN-ON>
           <COND (<L=? ,LANTERN-POWER 0>
                  <TELL "Unfortunately, the batteries seem to be dead." CR>
                  <RTRUE>)>
           <FSET ,PRSO ,LIGHTBIT>
           <OR <RUNNING? ,I-BRASS-LANTERN> <QUEUE I-BRASS-LANTERN -1>>
           <V-TURN-ON>
           <NOW-LIT?>
           <RTRUE>)
          (<VERB? TURN-OFF>
           <FCLEAR ,PRSO ,LIGHTBIT>
           <V-TURN-OFF>
           <NOW-DARK?>
           <RTRUE>)
          (<AND <VERB? PUT-IN> <PRSI? ,BRASS-LANTERN>>
           <COND (<PRSO? ,OLD-BATTERIES>
                  <TELL "Those batteries are dead; they won't do any good at all." CR>)
                 (<PRSO? ,FRESH-BATTERIES>
                  <REPLACE-LANTERN-BATTERIES>
                  <RTRUE>)
                 (ELSE
                  <TELL "The only thing you might successfully put in the lamp is a fresh pair of batteries." CR>)>)>>

<GLOBAL LANTERN-POWER 330>

<ROUTINE REPLACE-LANTERN-BATTERIES ()
    <COND (<OR <IN? ,FRESH-BATTERIES ,HERE> <HELD? ,FRESH-BATTERIES>>
           <REMOVE ,FRESH-BATTERIES>
           <SETG FRESH-BATTERIES-USED T>
           <MOVE ,OLD-BATTERIES ,HERE>
           <SETG LANTERN-POWER 2500>
           <TELL "I'm taking the liberty of replacing the batteries." CR>)>>

<ROUTINE I-BRASS-LANTERN ()
    ;"Dequeue the event if the lantern is off"
    <COND (<NOT <FSET? ,BRASS-LANTERN ,LIGHTBIT>>
           <DEQUEUE I-BRASS-LANTERN>
           <RFALSE>)>
    ;"Drain power and turn lamp off if dead"
    <COND (<DLESS? LANTERN-POWER 1>
           <FCLEAR ,BRASS-LANTERN ,LIGHTBIT>
           <FCLEAR ,BRASS-LANTERN ,ONBIT>)>
    ;"Report anything interesting"
    <COND (<VISIBLE? ,BRASS-LANTERN>
           <COND (<0? ,LANTERN-POWER>
                  <TELL "Your lamp has run out of power.">
                  <COND (<NOT <OR <HELD? ,FRESH-BATTERIES>
                                  <FSET? ,HERE ,LIGHTBIT>>>
                         <JIGS-UP " You can't explore the cave without a lamp. So let's just call it a day.">)
                        (ELSE <REPLACE-LANTERN-BATTERIES>)>
                  <CRLF>
                  <RTRUE>)
                 (<=? ,LANTERN-POWER 30>
                  <TELL "Your lamp is getting dim.">
                  <COND (,FRESH-BATTERIES-USED
                         <TELL " You're also out of spare batteries. You'd best start wrapping this up." CR>)
                        (<AND <IN? ,FRESH-BATTERIES ,VENDING-MACHINE>
                              <FSET? ,DEAD-END-14 ,TOUCHBIT>>
                         <TELL " You'd best start wrapping this up, unless you can find some fresh batteries.
I seem to recall there's a vending machine in the maze. Bring some coins with you.">)
                        (<NOT <OR <IN? ,FRESH-BATTERIES ,VENDING-MACHINE>
                                  <IN? ,FRESH-BATTERIES ,HERE>
                                  <HELD? ,FRESH-BATTERIES>>>
                         <TELL " You'd best go back for those batteries.">)>
                  <CRLF>
                  <RTRUE>)>)>>

<OBJECT BOTTLE
    (DESC "bottle")
    (IN INSIDE-BUILDING)
    (SYNONYM BOTTLE JAR FLASK)
    (FDESC "There is an empty bottle here.")
    (ACTION BOTTLE-F)
    (CONTFCN BOTTLE-CONTFCN)
    (FLAGS TAKEBIT CONTBIT OPENBIT)>

<ROUTINE BOTTLE-F ("AUX" F)
    <COND (<VERB? FILL>
           <COND (<FIRST? ,PRSO>
                  <TELL CT ,PRSO " is full already." CR>)
                 (<OR <GLOBAL-IN? ,STREAM ,HERE> <IN? ,SPRING ,HERE>>
                  <MOVE ,WATER-IN-BOTTLE ,PRSO>
                  <TELL CT ,PRSO " is now full of water." CR>)
                 (<IN? ,OIL ,HERE>
                  <MOVE ,OIL-IN-BOTTLE ,PRSO>
                  <TELL CT ,PRSO " is now full of oil." CR>)
                 (ELSE <TELL "There is nothing here with which to fill the bottle." CR>)>)
          (<VERB? EMPTY>
           <COND (<NOT <SET F <FIRST? ,PRSO>>>
                  <TELL CT ,PRSO " is already empty!" CR>)
                 (ELSE
                  <REMOVE .F>
                  <TELL "Your " D ,PRSO " is now empty and the ground is now wet." CR>)>)
          (<AND <VERB? PUT-IN> <PRSI? ,BOTTLE>>
           <COND (<PRSO? ,STREAM ,OIL>
                  <PERFORM ,V?FILL ,PRSI>
                  <RTRUE>)
                 (ELSE <TELL CT ,PRSI " is only supposed to hold liquids." CR>)>)>>

<ROUTINE BOTTLE-CONTFCN ()
    <COND (<VERB? TAKE> <TELL "You're holding that already (in " T ,BOTTLE ")." CR>)>>

<OBJECT WATER-IN-BOTTLE
    (DESC "bottled water")
    (ARTICLE "some")
    (SYNONYM WATER H2O)
    (ADJECTIVE
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP BOTTLE) (ELSE BOTTLED)>)
    (TEXT "It looks like ordinary water to me.")
    (ACTION WATER-IN-BOTTLE-F)>

<ROUTINE WATER-IN-BOTTLE-F ()
    <COND (<VERB? DRINK>
           <REMOVE ,PRSO>
           <PERFORM ,V?DRINK ,STREAM>
           <RTRUE>)>>

<OBJECT OIL-IN-BOTTLE
    (DESC "bottled oil")
    (ARTICLE "some")
    (SYNONYM OIL LUBRICANT GREASE)
    (ADJECTIVE
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP BOTTLE) (ELSE BOTTLED)>)
    (TEXT "It looks like ordinary oil to me.")
    (ACTION OIL-IN-BOTTLE-F)>

<ROUTINE OIL-IN-BOTTLE-F ()
    <COND (<VERB? DRINK>
           ;"The response to DRINK OIL is to refuse, so we don't remove the object."
           <PERFORM ,V?DRINK ,OIL>
           <RTRUE>)>>

;----------------------------------------------------------------------

<ROOM IN-FOREST-1
    (DESC "In Forest")
    (IN ROOMS)
    (GLOBAL FOREST)
    (LDESC "You are in open forest, with a deep valley to one side.")
    (EAST TO IN-A-VALLEY)
    (DOWN TO IN-A-VALLEY)
    (NORTH PER RANDOM-FOREST)
    (WEST PER RANDOM-FOREST)
    (SOUTH PER RANDOM-FOREST)
    (FLAGS LIGHTBIT SACREDBIT)>

<ROOM IN-FOREST-2
    (DESC "In Forest")
    (IN ROOMS)
    (GLOBAL ROAD FOREST)
    (LDESC "You are in open forest near both a valley and a road.")
    (NORTH TO AT-END-OF-ROAD)
    (EAST TO IN-A-VALLEY)
    (WEST TO IN-A-VALLEY)
    (DOWN TO IN-A-VALLEY)
    (SOUTH PER RANDOM-FOREST)
    (FLAGS LIGHTBIT SACREDBIT)>

<ROUTINE RANDOM-FOREST ()
    <COND (<=? <RANDOM 2> 1> ,IN-FOREST-1)
          (ELSE ,IN-FOREST-2)>>

<ROOM IN-A-VALLEY
    (DESC "In A Valley")
    (IN ROOMS)
    (GLOBAL STREAM FOREST)
    (LDESC "You are in a valley in the forest beside a stream tumbling along a rocky bed.")
    (NORTH TO AT-END-OF-ROAD)
    (EAST PER RANDOM-FOREST)
    (WEST PER RANDOM-FOREST)
    (UP PER RANDOM-FOREST)
    (SOUTH TO AT-SLIT-IN-STREAMBED)
    (DOWN TO AT-SLIT-IN-STREAMBED)
    (FLAGS LIGHTBIT SACREDBIT)>

<OBJECT STREAMBED
    (DESC "streambed")
    (IN IN-A-VALLEY)
    (SYNONYM BED ROCK
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP STREAM) (ELSE STREAMBED)>)
    (ADJECTIVE SMALL ROCKY BARE DRY)
    (FLAGS NDESCBIT)>

;----------------------------------------------------------------------

<CONSTANT YOU-DONT-FIT "You don't fit through a two-inch slit!">

<ROOM AT-SLIT-IN-STREAMBED
    (DESC "At Slit In Streambed")
    (IN ROOMS)
    (GLOBAL STREAM)
    (LDESC "At your feet all the water of the stream splashes into a 2-inch slit in the rock.
Downstream the streambed is bare rock.")
    (NORTH TO IN-A-VALLEY)
    (EAST PER RANDOM-FOREST)
    (WEST PER RANDOM-FOREST)
    (SOUTH TO OUTSIDE-GRATE)
    (DOWN SORRY ,YOU-DONT-FIT)
    (IN SORRY ,YOU-DONT-FIT)
    (FLAGS LIGHTBIT SACREDBIT)>

<OBJECT 2-INCH-SLIT
    (DESC "2-inch slit")
    (IN AT-SLIT-IN-STREAMBED)
    (SYNONYM SLIT)
    (ADJECTIVE TWO INCH 2-INCH)
    (TEXT "It's just a 2-inch slit in the rock, through which the stream is flowing.")
    (ACTION 2-INCH-SLIT-F)
    (FLAGS NDESCBIT)>

<ROUTINE 2-INCH-SLIT-F ()
    <COND (<VERB? ENTER> <TELL ,YOU-DONT-FIT CR>)>>

;----------------------------------------------------------------------

<ROOM OUTSIDE-GRATE
    (DESC "Outside Grate")
    (IN ROOMS)
    (GLOBAL GRATE)
    (LDESC "You are in a 20-foot depression floored with bare dirt.
Set into the dirt is a strong steel grate mounted in concrete.
A dry streambed leads into the depression.")
    (GLOBAL GRATE)
    (EAST PER RANDOM-FOREST)
    (WEST PER RANDOM-FOREST)
    (SOUTH PER RANDOM-FOREST)
    (NORTH TO AT-SLIT-IN-STREAMBED)
    (DOWN TO BELOW-THE-GRATE IF GRATE IS OPEN)
    (ACTION OUTSIDE-GRATE-F)
    (FLAGS LIGHTBIT SACREDBIT)>

<ROUTINE OUTSIDE-GRATE-F (RARG)
    <COND (<AND <=? .RARG ,M-END> <VERB? LOOK>>
           ;"Since the grate isn't actually in the room, describe it here"
           <COND (<FSET? ,GRATE ,OPENBIT>
                  <TELL CR "The grate stands open." CR>)
                 (<NOT <FSET? ,GRATE ,LOCKEDBIT>>
                  <TELL CR "The grate is unlocked but shut." CR>)>)
          (<AND <=? .RARG ,M-BEG>
                <VERB? WALK>
                <PRSO? ,P?DOWN>
                <NOT <FSET? ,GRATE ,LOCKEDBIT>>
                <NOT <FSET? ,GRATE ,OPENBIT>>>
           <TELL "[first opening " T ,GRATE "]" CR>
           <FSET ,GRATE ,OPENBIT>
           ;"Return false to continue handling WALK"
           <RFALSE>)>>

<OBJECT 20-FOOT-DEPRESSION
    (DESC "20-foot depression")
    (IN OUTSIDE-GRATE)
    (SYNONYM DEPRESSION DIRT)
    (ADJECTIVE TWENTY FOOT BARE 20-FOOT)
    (TEXT "You're standing in it.")
    (FLAGS NDESCBIT)>

<OBJECT GRATE
    (DESC "steel grate")
    (IN LOCAL-GLOBALS)
    (SYNONYM GRATE LOCK GATE GRATING)
    (ADJECTIVE METAL STRONG STEEL)
    (TEXT "It just looks like an ordinary grate mounted in concrete.")
    (ACTION GRATE-F)
    (FLAGS DOORBIT OPENABLEBIT LOCKEDBIT)>

<ROUTINE GRATE-F ()
    <COND (<AND <VERB? LOCK UNLOCK> <PRSO? ,GRATE>>
           <COND (<NOT <PRSI? ,SET-OF-KEYS>>
                  <TELL CT ,PRSI " won't fit the lock." CR>)
                 (<VERB? LOCK>
                  <COND (<FSET? ,PRSO ,LOCKEDBIT>
                         <TELL "It's already locked." CR>)
                        (ELSE
                         <FSET ,PRSO ,LOCKEDBIT>
                         <TELL "Locked." CR>)>)
                 (ELSE
                  <COND (<FSET? ,PRSO ,LOCKEDBIT>
                         <FCLEAR ,PRSO ,LOCKEDBIT>
                         <TELL "Unlocked." CR>)
                        (ELSE <TELL "It's already unlocked." CR>)>)>)>>

;----------------------------------------------------------------------
"Facilis descensus Averno..."
;----------------------------------------------------------------------

<ROOM BELOW-THE-GRATE
    (DESC "Below the Grate")
    (IN ROOMS)
    (LDESC "You are in a small chamber beneath a 3x3 steel grate to the surface.
A low crawl over cobbles leads inward to the west.")
    (GLOBAL GRATE COBBLES)
    (WEST TO IN-COBBLE-CRAWL)
    (UP TO OUTSIDE-GRATE IF GRATE IS OPEN)
    (FLAGS LIGHTBIT)>

<OBJECT COBBLES
    (DESC "cobbles")
    (IN LOCAL-GLOBALS)
    ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
    %<VERSION?
        (ZIP '(SYNONYM COBBLE STONES STONE))
        (ELSE '(SYNONYM COBBLE COBBLES COBBLESTONE STONES STONE))>
    (TEXT "They're just ordinary cobbles.")
    (FLAGS PLURALBIT MULTITUDEBIT)>

;----------------------------------------------------------------------

<ROOM IN-COBBLE-CRAWL
    (DESC "In Cobble Crawl")
    (IN ROOMS)
    (LDESC "You are crawling over cobbles in a low passage.
There is a dim light at the east end of the passage.")
    (GLOBAL COBBLES)
    (EAST TO BELOW-THE-GRATE)
    (WEST TO IN-DEBRIS-ROOM)
    (FLAGS LIGHTBIT)>

<OBJECT WICKER-CAGE
    (DESC "wicker cage")
    (IN IN-COBBLE-CRAWL)
    (FDESC "There is a small wicker cage discarded nearby.")
    (TEXT "It's a small wicker cage.")
    (ACTION WICKER-CAGE-F)
    (FLAGS TAKEBIT CONTBIT OPENBIT OPENABLEBIT TRANSBIT)>

<ROUTINE WICKER-CAGE-F ()
    <COND (<AND <VERB? OPEN> <IN? ,LITTLE-BIRD ,PRSO>>
           <V-OPEN>
           <COND (<FSET? ,PRSO ,OPENBIT>
                  <TELL "(releasing " T ,LITTLE-BIRD ")" CR>
                  <PERFORM ,V?RELEASE ,LITTLE-BIRD>)>
           <RTRUE>)>>

;----------------------------------------------------------------------

<ROOM IN-DEBRIS-ROOM
    (DESC "In Debris Room")
    (IN ROOMS)
    (LDESC "You are in a debris room filled with stuff washed in from the surface.
A low wide passage with cobbles becomes plugged with mud and debris here, but an
awkward canyon leads upward and west.||
A note on the wall says, \"Magic word XYZZY.\"")
    (GLOBAL COBBLES)
    (EAST TO IN-COBBLE-CRAWL)
    (UP TO IN-AWKWARD-SLOPING-E/W-CANYON)
    (WEST TO IN-AWKWARD-SLOPING-E/W-CANYON)
    (ACTION IN-DEBRIS-ROOM-F)
    (FLAGS SACREDBIT)>

<ROUTINE IN-DEBRIS-ROOM-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? XYZZY>>
           <GOTO ,INSIDE-BUILDING>
           <RTRUE>)>>

<OBJECT DEBRIS
    (DESC "debris")
    (IN IN-DEBRIS-ROOM)
    (SYNONYM DEBRIS STUFF MUD)
    (TEXT "Yuck.")
    (FLAGS NDESCBIT)>

<OBJECT XYZZY-NOTE
    (DESC "note")
    (IN IN-DEBRIS-ROOM)
    (SYNONYM NOTE)
    (TEXT "The note says \"Magic word XYZZY\".")
    (FLAGS NDESCBIT)>

<OBJECT BLACK-ROD
    (DESC "black rod with a rusty star on the end")
    (IN IN-DEBRIS-ROOM)
    (SYNONYM ROD STAR)
    (ADJECTIVE BLACK RUSTY THREE FOOT IRON)
    (FDESC "A three foot black rod with a rusty star on one end lies nearby.")
    (TEXT "It's a three foot black rod with a rusty star on an end.")
    (ACTION BLACK-ROD-F)
    (FLAGS TAKEBIT)>

<ROUTINE BLACK-ROD-F ()
    <COND (<VERB? WAVE>
           <COND (<=? ,HERE ,WEST-SIDE-OF-FISSURE ,ON-EAST-BANK-OF-FISSURE>
                  <COND (,CAVES-CLOSED
                         <TELL "Peculiar. Nothing happens." CR>)
                        (<FSET? ,CRYSTAL-BRIDGE ,OPENBIT>
                         <FSET ,CRYSTAL-BRIDGE ,INVISIBLE>
                         <FCLEAR ,CRYSTAL-BRIDGE ,OPENBIT>
                         <TELL "The crystal bridge has vanished!" CR>)
                        (ELSE
                         <FCLEAR ,CRYSTAL-BRIDGE ,INVISIBLE>
                         <FSET ,CRYSTAL-BRIDGE ,OPENBIT>
                         <TELL "A crystal bridge now spans the fissure." CR>)>)
                 (ELSE <TELL "Nothing happens." CR>)>)>>

;----------------------------------------------------------------------

<ROOM IN-AWKWARD-SLOPING-E/W-CANYON
    (DESC "Sloping E/W Canyon")
    (IN ROOMS)
    (LDESC "You are in an awkward sloping east/west canyon.")
    (DOWN TO IN-DEBRIS-ROOM)
    (EAST TO IN-DEBRIS-ROOM)
    (UP TO IN-BIRD-CHAMBER)
    (WEST TO IN-BIRD-CHAMBER)
    (FLAGS SACREDBIT)>

;----------------------------------------------------------------------
"The little bird in its natural habitat"
;----------------------------------------------------------------------

<ROOM IN-BIRD-CHAMBER
    (DESC "Orange River Chamber")
    (IN ROOMS)
    (LDESC "You are in a splendid chamber thirty feet high.
The walls are frozen rivers of orange stone.
An awkward canyon and a good passage exit from east and west sides of the chamber.")
    (EAST TO IN-AWKWARD-SLOPING-E/W-CANYON)
    (WEST TO AT-TOP-OF-SMALL-PIT)
    (FLAGS SACREDBIT)>

<OBJECT LITTLE-BIRD
    (DESC "little bird")
    (IN IN-BIRD-CHAMBER)
    (SYNONYM BIRD)
    (ADJECTIVE CHEERFUL MOURNFUL LITTLE)
    (FDESC "A cheerful little bird is sitting here singing.")
    (ACTION LITTLE-BIRD-F)
    (FLAGS PERSONBIT)>

<ROUTINE LITTLE-BIRD-F ()
    <COND (<VERB? EXAMINE>
           <COND (<IN? ,PRSO ,WICKER-CAGE>
                  <TELL "The little bird looks unhappy in the cage." CR>)
                 (ELSE
                  <TELL "The cheerful little bird is sitting here singing." CR>)>)
          (<VERB? PUT-IN>
           <COND (<PRSI? ,WICKER-CAGE>
                  <PERFORM ,V?CATCH ,PRSO>
                  <RTRUE>)
                 (ELSE <TELL "Don't put the poor bird in " T ,PRSI "!" CR>)>)
          (<AND <VERB? DROP> <IN? ,PRSO ,WICKER-CAGE>>
           <TELL "(The bird is released from the cage.)" CR CR>
           <PERFORM ,V?RELEASE ,PRSO>
           <RTRUE>)
          (<VERB? TAKE CATCH>
           <COND (<IN? ,PRSO ,WICKER-CAGE>
                  <TELL "You already have the little bird.
If you take it out of the cage it will likely fly away from you." CR>)
                 (<NOT <HELD? ,WICKER-CAGE>>
                  <TELL "You can catch the bird, but you cannot carry it." CR>)
                 (<HELD? ,BLACK-ROD>
                  <TELL "The bird was unafraid when you entered, but as you approach
it becomes disturbed and you cannot catch it." CR>)
                 (ELSE
                  <MOVE ,PRSO ,WICKER-CAGE>
                  <FCLEAR ,WICKER-CAGE ,OPENBIT>
                  <TELL "You catch the bird in the wicker cage." CR>)>)
          (<VERB? RELEASE>
           <COND (<NOT <IN? ,PRSO ,WICKER-CAGE>>
                  <TELL "The bird is not caged now." CR>)
                 (ELSE
                  <FSET ,WICKER-CAGE ,OPENBIT>
                  <MOVE ,PRSO ,HERE>
                  <COND (<IN? ,SNAKE ,HERE>
                         <REMOVE ,SNAKE>
                         <TELL CT ,PRSO " attacks the green snake, and in an astounding
flurry drives the snake away." CR>)
                        (<IN? ,DRAGON ,HERE>
                         <REMOVE ,PRSO>
                         <TELL CT ,PRSO " attacks the green dragon, and in an astounding
flurry gets burnt to a cinder. The ashes blow away." CR>)
                        (ELSE <TELL "The little bird flies free." CR>)>)>)
          (<AND <VERB? GIVE> <PRSI? ,LITTLE-BIRD>>
           <TELL "It's not hungry. (It's merely pinin' for the fjords).
Besides, I suspect it would prefer bird seed." CR>)
          ;"TODO: TELL, ASK, ANSWER...?"
          (<VERB? ATTACK>
           <COND (<IN? ,PRSO ,WICKER-CAGE>
                  <TELL "Oh, leave the poor unhappy bird alone." CR>)
                 (ELSE
                  <REMOVE ,PRSO>
                  <TELL "The little bird is now dead. Its body disappears." CR>)>)>>

;----------------------------------------------------------------------

<ROOM AT-TOP-OF-SMALL-PIT
    (DESC "At Top of Small Pit")
    (IN ROOMS)
    (LDESC "At your feet is a small pit breathing traces of white mist.
A west passage ends here except for a small crack leading on.||
Rough stone steps lead down the pit.")
    (GLOBAL MIST)
    (EAST TO IN-BIRD-CHAMBER)
    (WEST SORRY "The crack is far too small for you to follow.")
    (DOWN PER DOWN-INTO-SMALL-PIT)
    (FLAGS SACREDBIT)>

<ROUTINE DOWN-INTO-SMALL-PIT ()
    <COND (<HELD? ,LARGE-GOLD-NUGGET>
           <JIGS-UP "You are at the bottom of the pit with a broken neck.">)
          (ELSE ,IN-HALL-OF-MISTS)>>

<OBJECT SMALL-MISTY-PIT
    (DESC "small pit")
    (IN AT-TOP-OF-SMALL-PIT)
    (SYNONYM PIT)
    (ADJECTIVE SMALL)
    (TEXT "The pit is breathing traces of white mist.")
    (FLAGS NDESCBIT)>

<OBJECT PIT-CRACK
    (DESC "crack")
    (IN AT-TOP-OF-SMALL-PIT)
    (SYNONYM CRACK)
    (ADJECTIVE SMALL)
    (TEXT "The crack is very small -- far too small for you to follow.")
    (ACTION PIT-CRACK-F)
    (FLAGS NDESCBIT)>

<ROUTINE PIT-CRACK-F ()
    <COND (<VERB? ENTER> <TELL "The crack is far too small for you to follow." CR>)>>

<OBJECT MIST
    (DESC "mist")
    (IN LOCAL-GLOBALS)
    (SYNONYM MIST VAPOR WISPS)
    (ADJECTIVE WHITE)
    (TEXT "Mist is a white vapor, usually water, seen from time to time in caves.
It can be found anywhere but is frequently a sign of a deep pit leading down to water.")>

;----------------------------------------------------------------------
"The caves open up: The Hall of Mists"
;----------------------------------------------------------------------

<ROOM IN-HALL-OF-MISTS
    (DESC "In Hall of Mists")
    (IN ROOMS)
    (LDESC "You are at one end of a vast hall stretching forward out of sight to the west.
There are openings to either side.
Nearby, a wide stone staircase leads downward.
The hall is filled with wisps of white mist swaying to and fro almost as if alive.
A cold wind blows up the staircase.
There is a passage at the top of a dome behind you.||
Rough stone steps lead up the dome.")
    (GLOBAL MIST)
    (ACTION IN-HALL-OF-MISTS-F)
    (SOUTH TO IN-NUGGET-OF-GOLD-ROOM)
    (WEST TO ON-EAST-BANK-OF-FISSURE)
    (DOWN TO IN-HALL-OF-MT-KING)
    (NORTH TO IN-HALL-OF-MT-KING)
    (UP PER UP-OUT-OF-SMALL-PIT)>

<ROUTINE IN-HALL-OF-MISTS-F (RARG)
    <COND (<AND <=? .RARG ,M-ENTER>
                <NOT <FSET? ,IN-HALL-OF-MISTS-F ,TOUCHBIT>>>
           <SETG SCORE <+ ,SCORE 25>>)>>

<ROUTINE UP-OUT-OF-SMALL-PIT ()
    <COND (<HELD? ,LARGE-GOLD-NUGGET>
           <TELL "The dome is unclimbable." CR>)
          (ELSE ,AT-TOP-OF-SMALL-PIT)>>

<OBJECT WIDE-STONE-STAIRCASE
    (DESC "wide stone staircase")
    (IN IN-HALL-OF-MISTS)
    (SYNONYM STAIR STAIRS STAIRCASE)
    (ADJECTIVE WIDE STONE)
    (TEXT "The staircase leads down.")
    (FLAGS NDESCBIT)>

<OBJECT ROUGH-STONE-STEPS
    (DESC "rough stone steps")
    (IN IN-HALL-OF-MISTS)
    (SYNONYM ;"STAIR STAIRS STAIRCASE"
             STEP STEPS)
    (ADJECTIVE ROUGH STONE)
    (TEXT "The rough stone steps lead up the dome.")
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

<OBJECT DOME
    (DESC "dome")
    (IN IN-HALL-OF-MISTS)
    (SYNONYM DOME)
    (ACTION DOME-F)
    (FLAGS NDESCBIT)>

<ROUTINE DOME-F ()
    <COND (<VERB? EXAMINE>
           <COND (<HELD? ,LARGE-GOLD-NUGGET>
                  <TELL "I'm not sure you'll be able to get up it with what you're carrying." CR>)
                 (ELSE <TELL "It looks like you might be able to climb up it." CR>)>)
          (<VERB? CLIMB>
           <PERFORM ,V?WALK ,P?UP>
           <RTRUE>)>>

;----------------------------------------------------------------------

<ROOM IN-NUGGET-OF-GOLD-ROOM
    (DESC "Low Room")
    (IN ROOMS)
    (LDESC "This is a low room with a crude note on the wall:||
\"You won't get it up the steps.\"")
    (NORTH TO IN-HALL-OF-MISTS)>

<OBJECT NUGGET-NOTE
    (DESC "note")
    (IN IN-NUGGET-OF-GOLD-ROOM)
    (SYNONYM NOTE)
    (ADJECTIVE CRUDE)
    (TEXT "The note says, \"You won't get it up the steps.\"")
    (FLAGS NDESCBIT)>

<OBJECT LARGE-GOLD-NUGGET
    (DESC "large gold nugget")
    (IN IN-NUGGET-OF-GOLD-ROOM)
    (SYNONYM NUGGET)
    (ADJECTIVE GOLD LARGE HEAVY)
    (FDESC "There is a large sparkling nugget of gold here!")
    (TEXT "It's a large sparkling nugget of gold!")
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------

<CONSTANT USE-THE-BRIDGE
    "I respectfully suggest you go across the bridge instead of jumping.">

<ROUTINE FISSURE-ROOMS-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<VERB? JUMP>
                  <COND (<FSET? ,CRYSTAL-BRIDGE ,OPENBIT> <TELL ,USE-THE-BRIDGE CR>)
                        (ELSE <JIGS-UP "You didn't make it.">)>)
                 (<AND <VERB? WALK> <PRSO? ,P?DOWN>>
                  <TELL "The fissure is too terrifying!" CR>)>)
          (<AND <=? .RARG ,M-END> <VERB? LOOK>>
           ;"Since the bridge isn't actually in the room, describe it here"
           <COND (<FSET? ,CRYSTAL-BRIDGE ,OPENBIT>
                  <TELL CR "A crystal bridge now spans the fissure." CR>)>)>>

<ROOM ON-EAST-BANK-OF-FISSURE
    (DESC "On East Bank of Fissure")
    (IN ROOMS)
    (LDESC "You are on the east bank of a fissure slicing clear across the hall.
The mist is quite thick here, and the fissure is too wide to jump.")
    (GLOBAL CRYSTAL-BRIDGE FISSURE MIST)
    (EAST TO IN-HALL-OF-MISTS)
    (WEST TO WEST-SIDE-OF-FISSURE IF CRYSTAL-BRIDGE IS OPEN ELSE "The fissure is too wide.")
    (ACTION FISSURE-ROOMS-F)>

<ROOM WEST-SIDE-OF-FISSURE
    (DESC "West Side of Fissure")
    (IN ROOMS)
    (LDESC "You are on the west side of the fissure in the hall of mists.")
    (GLOBAL CRYSTAL-BRIDGE FISSURE)
    (WEST TO AT-WEST-END-OF-HALL-OF-MISTS)
    (EAST TO ON-EAST-BANK-OF-FISSURE IF CRYSTAL-BRIDGE IS OPEN ELSE "The fissure is too wide.")
    (NORTH TO AT-WEST-END-OF-HALL-OF-MISTS)
    (ACTION WEST-SIDE-OF-FISSURE-F)>

<ROUTINE ROOMS-WITH-LOW-WIDE-PASSAGE-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG>
                <VERB? WALK>
                <PRSO? ,P?NORTH>>
           <TELL "You have crawled through a very low wide passage
parallel to and north of the hall of mists." CR>
           <RFALSE>)>>

<ROUTINE WEST-SIDE-OF-FISSURE-F (RARG)
    <T? <OR <ROOMS-WITH-LOW-WIDE-PASSAGE-F .RARG>
            <FISSURE-ROOMS-F .RARG>>>>

<OBJECT DIAMONDS
    (DESC "diamonds")
    (IN WEST-SIDE-OF-FISSURE)
    (SYNONYM DIAMOND
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP #SPLICE ()) (ELSE DIAMONDS)>)
    (ADJECTIVE SEVERAL HIGH QUALITY)
    (FDESC "There are diamonds here!")
    (TEXT "They look to be of the highest quality!")
    (FLAGS TAKEBIT TREASUREBIT PLURALBIT MULTITUDEBIT)>

<OBJECT CRYSTAL-BRIDGE
    (DESC "crystal bridge")
    (IN LOCAL-GLOBALS)
    (SYNONYM BRIDGE)
    (ADJECTIVE CRYSTAL)
    (TEXT "It spans the fissure, thereby providing you a way across.")
    (FLAGS DOORBIT OPENBIT INVISIBLE)>

<OBJECT FISSURE
    (DESC "fissure")
    (IN LOCAL-GLOBALS)
    (SYNONYM FISSURE)
    (ADJECTIVE WIDE)
    (TEXT "The fissure looks far too wide to jump.")>

;----------------------------------------------------------------------

<ROOM AT-WEST-END-OF-HALL-OF-MISTS
    (DESC "At West End of Hall of Mists")
    (IN ROOMS)
    (LDESC "You are at the west end of the hall of mists.
A low wide crawl continues west and another goes north.
To the south is a little passage 6 feet off the floor.")
    (SOUTH TO ALIKE-MAZE-1)
    (UP TO ALIKE-MAZE-1)
    (EAST TO WEST-SIDE-OF-FISSURE)
    (WEST TO AT-EAST-END-OF-LONG-HALL)
    (NORTH TO WEST-SIDE-OF-FISSURE)
    (ACTION ROOMS-WITH-LOW-WIDE-PASSAGE-F)>

;----------------------------------------------------------------------
"Long Hall to the west of the Hall of Mists"
;----------------------------------------------------------------------

<ROOM AT-EAST-END-OF-LONG-HALL
    (DESC "At East End of Long Hall")
    (IN ROOMS)
    (LDESC "You are at the east end of a very long hall apparently without side chambers.
To the east a low wide crawl slants up.
To the north a round two foot hole slants down.")
    (EAST TO AT-WEST-END-OF-HALL-OF-MISTS)
    (UP TO AT-WEST-END-OF-HALL-OF-MISTS)
    (WEST TO AT-WEST-END-OF-LONG-HALL)
    (NORTH TO CROSSOVER)
    (DOWN TO CROSSOVER)>

;----------------------------------------------------------------------

<ROOM AT-WEST-END-OF-LONG-HALL
    (DESC "At West End of Long Hall")
    (IN ROOMS)
    (LDESC "You are at the west end of a very long featureless hall.
The hall joins up with a narrow north/south passage.")
    (EAST TO AT-EAST-END-OF-LONG-HALL)
    (SOUTH TO DIFFERENT-MAZE-1)
    (NORTH TO CROSSOVER)>

;----------------------------------------------------------------------

<ROOM CROSSOVER
    (DESC "N/S and E/W Crossover")
    (IN ROOMS)
    (LDESC "You are at a crossover of a high N/S passage and a low E/W one.")
    (WEST TO AT-EAST-END-OF-LONG-HALL)
    (NORTH TO DEAD-END-7)
    (EAST TO IN-WEST-SIDE-CHAMBER)
    (SOUTH TO AT-WEST-END-OF-LONG-HALL)>

<OBJECT CROSSOVER-SCENERY
    (DESC "crossover")
    (IN CROSSOVER)
    (SYNONYM CROSSOVER OVER)
    (ADJECTIVE CROSS)
    (TEXT "You know as much as I do at this point.")
    (FLAGS NDESCBIT)>

;----------------------------------------------------------------------
"Many Dead Ends will be needed for the maze below, so define a helper function"
;----------------------------------------------------------------------

<DEFINE DEAD-END-ROOM (NAME DIR DEST "ARGS" PS "AUX" DEF)
    <SET DEF
         <FORM ROOM .NAME
               '(DESC "Dead End")
               '(IN ROOMS)
               '(LDESC "You have reached a dead end.")
               '(ACTION DEAD-END-ROOMS-F)
               <LIST .DIR TO .DEST>
               <LIST OUT TO .DEST>
               !.PS>>
    <EVAL .DEF>>

<ROUTINE DEAD-END-ROOMS-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG>
                <VERB? WALK>
                <0? <GETPT ,HERE ,PRSO>>>
           <TELL "You'll have to go back the way you came." CR>)>>

<DEAD-END-ROOM DEAD-END-7 SOUTH CROSSOVER>

;----------------------------------------------------------------------
"The Hall of the Mountain King and side chambers"
;----------------------------------------------------------------------

<ROOM IN-HALL-OF-MT-KING
    (DESC "Hall of the Mountain King")
    (IN ROOMS)
    (LDESC "You are in the hall of the mountain king, with passages off in all directions.")
    (UP TO IN-HALL-OF-MISTS)
    (EAST TO IN-HALL-OF-MISTS)
    (NORTH TO LOW-N/S-PASSAGE)
    (SOUTH TO IN-SOUTH-SIDE-CHAMBER)
    (WEST TO IN-WEST-SIDE-CHAMBER)
    (SW TO IN-SECRET-E/W-CANYON)
    (ACTION IN-HALL-OF-MT-KING-F)>

<ROUTINE IN-HALL-OF-MT-KING-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? WALK>>
           <COND (<0? <GETPT ,HERE ,PRSO>>
                  <TELL "Well, perhaps not quite all directions." CR>)
                 (<AND <IN? ,SNAKE ,HERE>
                       <OR <PRSO? ,P?NORTH ,P?SOUTH ,P?WEST>
                           <AND <PRSO? ,P?SW> <PROB 35>>>>
                  <TELL "You can't get by the snake." CR>)>)>>

<OBJECT SNAKE
    (DESC "snake")
    (IN IN-HALL-OF-MT-KING)
    (SYNONYM SNAKE COBRA ASP)
    (ADJECTIVE HUGE FIERCE GREEN FEROCIOUS ;VENEMOUS VENOMOUS LARGE BIG KILLER)
    (FDESC "A huge green fierce snake bars the way!")
    (TEXT "I wouldn't mess with it if I were you.")
    (ACTION SNAKE-F)>

<ROUTINE SNAKE-F ()
    <COND (<AND <VERB? THROW-AT> <PRSI? ,SNAKE>>
           <COND (<PRSO? ,AXE> <PERFORM ,V?ATTACK ,PRSI>)
                 (ELSE <PERFORM ,V?GIVE ,PRSO ,PRSI>)>
           <RTRUE>)
          (<AND <VERB? GIVE> <PRSI? ,SNAKE>>
           <COND (<PRSO? ,LITTLE-BIRD>
                  <REMOVE ,PRSO>
                  <TELL "The snake has now devoured your bird." CR>)
                 (ELSE <TELL "There's nothing here it wants to eat (except perhaps you)." CR>)>)
          (<VERB? ATTACK>
           <TELL "Attacking the snake both doesn't work and is very dangerous." CR>)
          (<VERB? TAKE>
           <JIGS-UP "It takes you instead. Glrp!">)>>

;----------------------------------------------------------------------

<ROOM LOW-N/S-PASSAGE
    (DESC "Low N/S Passage")
    (IN ROOMS)
    (LDESC "You are in a low N/S passage at a hole in the floor.
The hole goes down to an E/W passage.")
    (SOUTH TO IN-HALL-OF-MT-KING)
    (DOWN TO IN-DIRTY-PASSAGE)
    (NORTH TO AT-Y2)>

<OBJECT BARS-OF-SILVER
    (DESC "bars of silver")
    (IN LOW-N/S-PASSAGE)
    (SYNONYM BARS SILVER)
    (FDESC "There are bars of silver here!")
    (TEXT "They're probably worth a fortune!")
    (FLAGS TAKEBIT PLURALBIT TREASUREBIT)>

;----------------------------------------------------------------------

<ROOM IN-SOUTH-SIDE-CHAMBER
    (DESC "In South Side Chamber")
    (IN ROOMS)
    (LDESC "You are in the south side chamber.")
    (NORTH TO IN-HALL-OF-MT-KING)>

<OBJECT PRECIOUS-JEWELRY
    (DESC "precious jewelry")
    (IN IN-SOUTH-SIDE-CHAMBER)
    (SYNONYM JEWEL JEWELS JEWELRY)
    (ADJECTIVE PRECIOUS EXQUISITE)
    (ARTICLE "some")
    (FDESC "There is precious jewelry here!")
    (TEXT "It's all quite exquisite!")
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------

<ROOM IN-WEST-SIDE-CHAMBER
    (DESC "In West Side Chamber")
    (IN ROOMS)
    (LDESC "You are in the west side chamber of the hall of the mountain king.
A passage continues west and up here.")
    (WEST TO CROSSOVER)
    (UP TO CROSSOVER)
    (EAST TO IN-HALL-OF-MT-KING)>

<OBJECT RARE-COINS
    (DESC "rare coins")
    (IN IN-WEST-SIDE-CHAMBER)
    (SYNONYM COINS)
    (ADJECTIVE RARE)
    (ARTICLE "many")
    (FDESC "There are many coins here!")
    (TEXT "They're a numismatist's dream!")
    (FLAGS TAKEBIT PLURALBIT TREASUREBIT MULTITUDEBIT)>


;----------------------------------------------------------------------
"The Y2 Rock Room and environs, slightly below"
;----------------------------------------------------------------------

<ROOM AT-Y2
    (DESC "At \"Y2\"")
    (IN ROOMS)
    (LDESC "You are in a large room, with a passage to the south,
a passage to the west, and a wall of broken rock to the east.
There is a large \"Y2\" on a rock in the rooms center.")
    (SOUTH TO LOW-N/S-PASSAGE)
    (EAST TO JUMBLE-OF-ROCK)
    (WEST TO AT-WINDOW-ON-PIT-1)
    (ACTION AT-Y2-F)>

<ROUTINE AT-Y2-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<VERB? PLUGH>
                  <GOTO ,INSIDE-BUILDING>
                  <RTRUE>)
                 (<VERB? PLOVER>
                  <OR <FSET? ,IN-PLOVER-ROOM ,TOUCHBIT> <RFALSE>>
                  <COND (<HELD? ,EGG-SIZED-EMERALD>
                         <MOVE ,EGG-SIZED-EMERALD ,IN-PLOVER-ROOM>
                         <SETG SCORE <- ,SCORE 5>>)>
                  <GOTO ,IN-PLOVER-ROOM>
                  <RTRUE>)>)
          (<AND <=? .RARG ,M-END>
                <VERB? LOOK>
                <PROB 25>>
           <TELL CR "A hollow voice says, \"Plugh.\"" CR>
           <RFALSE>)>>

<OBJECT Y2-ROCK
    (DESC "\"Y2\" rock")
    (IN AT-Y2)
    (SYNONYM ROCK Y2)
    (ADJECTIVE Y2)
    (TEXT "There is a large \"Y2\" painted on the rock.")
    (FLAGS NDESCBIT SURFACEBIT)>

;----------------------------------------------------------------------

<ROOM JUMBLE-OF-ROCK
    (DESC "Jumble of Rock")
    (IN ROOMS)
    (LDESC "You are in a jumble of rocks, with cracks everywhere.")
    (DOWN TO AT-Y2)
    (UP TO IN-HALL-OF-MISTS)>

;----------------------------------------------------------------------

<ROOM AT-WINDOW-ON-PIT-1
    (DESC "At Window on Pit")
    (IN ROOMS)
    (GLOBAL WINDOW HUGE-PIT MARKS-IN-DUST SHADOWY-FIGURE MIST)
    (ACTION AT-WINDOW-ON-PIT-ROOMS-F)
    (EAST TO AT-Y2)>

;"AT-WINDOW-ON-PIT-1 and AT-WINDOW-ON-PIT-2 have nearly identical descriptions
  and some common action responses, so we handle them with the same routine"
<ROUTINE AT-WINDOW-ON-PIT-ROOMS-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<VERB? WAVE-HANDS>
                  <TELL "The shadowy figure waves back at you!" CR>)
                 (<AND <VERB? WALK> <0? <GETPT ,HERE ,PRSO>>>
                  <TELL "The only passage is back "
                        <COND (<=? ,HERE ,AT-WINDOW-ON-PIT-1> "east to Y2")
                              (ELSE "west to the junction")>
                        "." CR>)
                 (<AND <VERB? JUMP> <=? ,HERE ,AT-WINDOW-ON-PIT-2>>
                  <JIGS-UP "You jump and break your neck!">)>)
          (<=? .RARG ,M-LOOK>
           <TELL "You're at a low window overlooking a huge pit, which extends up out of sight.
A floor is indistinctly visible over 50 feet below.
Traces of white mist cover the floor of the pit, becoming thicker to the "
                 <COND (<=? ,HERE ,AT-WINDOW-ON-PIT-1> "right") (ELSE "left")> ".
Marks in the dust around the window would seem to indicate that someone has been here recently.
Directly across the pit from you and 25 feet away there is a similar window looking into
a lighted room.
A shadowy figure can be seen there peering back at you." CR>)>>

<OBJECT WINDOW
    (DESC "window")
    (IN LOCAL-GLOBALS)
    (SYNONYM WINDOW)
    (ADJECTIVE LOW)
    (LDESC "It looks like a regular window.")
    ;(FLAGS OPENABLEBIT)>

<OBJECT HUGE-PIT
    (DESC "huge pit")
    (IN LOCAL-GLOBALS)
    (SYNONYM PIT)
    (ADJECTIVE DEEP LARGE)
    (LDESC "It's so deep you can barely make out the floor below, and the top isn't visible at all.")>

<OBJECT MARKS-IN-DUST
    (DESC "marks in the dust")
    (IN LOCAL-GLOBALS)
    (SYNONYM MARKS DUST)
    (LDESC "Evidently you're not alone here.")
    (FLAGS PLURALBIT MULTITUDEBIT)>

<OBJECT SHADOWY-FIGURE
    (DESC "shadowy figure")
    (IN LOCAL-GLOBALS)
    (SYNONYM FIGURE PERSON INDIVIDUAL SHADOW)
    (ADJECTIVE MYSTERIOUS
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP SHADOW) (ELSE SHADOWY)>)
    (LDESC "The shadowy figure seems to be trying to attract your attention.")>

;----------------------------------------------------------------------

<ROOM IN-DIRTY-PASSAGE
    (DESC "Dirty Passage")
    (IN ROOMS)
    (LDESC "You are in a dirty broken passage. To the east is a crawl.
To the west is a large passage. Above you is a hole to another passage.")
    (EAST TO ON-BRINK-OF-PIT)
    (UP TO LOW-N/S-PASSAGE)
    (WEST TO IN-DUSTY-ROCK-ROOM)>

;----------------------------------------------------------------------

<ROOM ON-BRINK-OF-PIT
    (DESC "Brink of Pit")
    (IN ROOMS)
    (LDESC "You are on the brink of a small clean climbable pit.
A crawl leads west.")
    (WEST TO IN-DIRTY-PASSAGE)
    (DOWN TO IN-PIT)
    (IN TO IN-PIT)>

<OBJECT SMALL-CLIMBABLE-PIT
    (DESC "small pit")
    (IN ON-BRINK-OF-PIT)
    (SYNONYM PIT)
    (ADJECTIVE SMALL CLEAN CLIMBABLE)
    (TEXT "It looks like you might be able to climb down into it.")
    (ACTION SMALL-CLIMBABLE-PIT-F)
    (FLAGS NDESCBIT)>

<ROUTINE SMALL-CLIMBABLE-PIT-F ()
    <COND (<VERB? CLIMB ENTER>
           <PERFORM ,V?WALK ,P?DOWN>
           <RTRUE>)>>

;----------------------------------------------------------------------

<ROOM IN-PIT
    (DESC "In Pit")
    (IN ROOMS)
    (LDESC "You are in the bottom of a small pit with a little stream,
which enters and exits through tiny slits.")
    (GLOBAL STREAM)
    (UP TO ON-BRINK-OF-PIT)
    (DOWN SORRY "You don't fit through the tiny slits!")
    (FLAGS SACREDBIT)>

<OBJECT TINY-SLITS
    (DESC "tiny slits")
    (IN IN-PIT)
    (SYNONYM SLIT SLITS)
    (ADJECTIVE TINY)
    (TEXT "The slits form a complex pattern in the rock.")
    (FLAGS NDESCBIT MULTITUDEBIT PLURALBIT)>

;----------------------------------------------------------------------

<ROOM IN-DUSTY-ROCK-ROOM
    (DESC "In Dusty Rock Room")
    (IN ROOMS)
    (LDESC "You are in a large room full of dusty rocks.
There is a big hole in the floor.
There are cracks everywhere, and a passage leading east.")
    (EAST TO IN-DIRTY-PASSAGE)
    (DOWN TO AT-COMPLEX-JUNCTION)>

<OBJECT DUSTY-ROCKS
    (DESC "dusty rocks")
    (IN IN-DUSTY-ROCK-ROOM)
    (TEXT "They're just rocks. (Dusty ones, that is.)")
    (ACTION DUSTY-ROCKS-F)
    (FLAGS NDESCBIT MULTITUDEBIT PLURALBIT)>

<ROUTINE DUSTY-ROCKS-F ()
    <COND (<VERB? LOOK-UNDER PUSH PULL>
           <TELL "You'd have to blast your way through." CR>)>>

;----------------------------------------------------------------------
"A maze of twisty little passages, all alike..."
;----------------------------------------------------------------------

;"Define a function to save a bit of tedium. This lets us specify connections
  within the maze by number instead of typing out the room name, and omit TO."
<DEFINE MAZE-ROOM (NAME "ARGS" CS "AUX" DEF PS)
    ;"Convert list of map connections to property definitions.
      (NORTH FOO-ROOM) -> (NORTH TO FOO-ROOM)
      (NORTH 5)        -> (NORTH TO ALIKE-MAZE-5)"
    <SET PS
        <MAPF ,LIST
              <FUNCTION (C "AUX" (DIR <1 .C>) (DEST <2 .C>))
                  <COND (<TYPE? .DEST FIX>
                         <SET DEST <PARSE <STRING "ALIKE-MAZE-" <UNPARSE .DEST>>>>)>
                  <LIST .DIR TO .DEST>>
              .CS>>
    ;"Build room definition and evaluate it"
    <SET DEF
        <FORM ROOM .NAME
            '(DESC "Maze")
            '(IN ROOMS)
            '(LDESC "You are in a maze of twisty little passages, all alike.")
            '(ACTION MAZE-ROOMS-F)
            !.PS>>
    <EVAL .DEF>>

<ROUTINE MAZE-ROOMS-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG>
                <VERB? WALK>
                <PRSO? ,P?OUT>>
           <TELL "Easier said than done." CR>)>>

<MAZE-ROOM ALIKE-MAZE-1
    (UP AT-WEST-END-OF-HALL-OF-MISTS) (NORTH 1) (EAST 2) (SOUTH 4) (WEST 11)>

<MAZE-ROOM ALIKE-MAZE-2
    (EAST 2) (DOWN DEAD-END-3) (SOUTH 6) (NORTH DEAD-END-13)>

<MAZE-ROOM ALIKE-MAZE-3
    (EAST 2) (DOWN DEAD-END-3) (SOUTH 6) (NORTH DEAD-END-13)>

<MAZE-ROOM ALIKE-MAZE-4
    (WEST 1) (NORTH 2) (EAST DEAD-END-1) (SOUTH DEAD-END-2) (UP 14) (DOWN 14)>

<MAZE-ROOM ALIKE-MAZE-5
    (EAST 6) (WEST 7)>

<MAZE-ROOM ALIKE-MAZE-6
    (EAST 3) (WEST 5) (DOWN 7) (SOUTH 8)>

<DEAD-END-ROOM DEAD-END-1 WEST ALIKE-MAZE-4>

<DEAD-END-ROOM DEAD-END-2 WEST ALIKE-MAZE-4>

<DEAD-END-ROOM DEAD-END-3 UP ALIKE-MAZE-3>

<MAZE-ROOM ALIKE-MAZE-7
    (WEST 5) (UP 6) (EAST 8) (SOUTH 9)>

<MAZE-ROOM ALIKE-MAZE-8
    (WEST 6) (EAST 7) (SOUTH 8) (UP 9) (NORTH 10) (DOWN DEAD-END-12)>

<MAZE-ROOM ALIKE-MAZE-9
    (WEST 7) (NORTH 8) (SOUTH DEAD-END-4)>

<DEAD-END-ROOM DEAD-END-4 WEST ALIKE-MAZE-9>

<MAZE-ROOM ALIKE-MAZE-10
    (WEST 8) (NORTH 10) (DOWN DEAD-END-5) (EAST AT-BRINK-OF-PIT)>

<DEAD-END-ROOM DEAD-END-5 UP ALIKE-MAZE-10>

;----------------------------------------------------------------------

<ROOM AT-BRINK-OF-PIT
    (DESC "At Brink of Pit")
    (IN ROOMS)
    (LDESC "You are on the brink of a thirty foot pit with a massive orange column down one wall.
You could climb down here but you could not get back up.
The maze continues at this level.")
    (DOWN TO IN-BIRD-CHAMBER)
    (WEST TO ALIKE-MAZE-10)
    (SOUTH TO DEAD-END-6)
    (NORTH TO ALIKE-MAZE-12)
    (EAST TO ALIKE-MAZE-13)>

<OBJECT MASSIVE-ORANGE-COLUMN
    (DESC "massive orange column")
    (IN AT-BRINK-OF-PIT)
    (TEXT "It looks like you could climb down it.")
    (ACTION MASSIVE-ORANGE-COLUMN-F)
    (FLAGS NDESCBIT)>

<ROUTINE MASSIVE-ORANGE-COLUMN-F ()
    <COND (<VERB? CLIMB> <PERFORM ,V?WALK ,P?DOWN> <RTRUE>)>>

<OBJECT PIT
    (DESC "pit")
    (IN AT-BRINK-OF-PIT)
    (SYNONYM PIT)
    (ADJECTIVE THIRTY FOOT
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP #SPLICE ()) (ELSE THIRTY-FOOT)>)
    (TEXT "You'll have to climb down to find out anything more...")
    (ACTION PIT-F)
    (FLAGS NDESCBIT)>

<ROUTINE PIT-F ()
    <COND (<VERB? CLIMB>
           <PERFORM ,V?WALK ,P?DOWN>
           <RTRUE>)>>

<DEAD-END-ROOM DEAD-END-6 EAST AT-BRINK-OF-PIT>

;----------------------------------------------------------------------
"A line of three vital junctions, east to west"
;----------------------------------------------------------------------

<ROOM AT-COMPLEX-JUNCTION
    (DESC "At Complex Junction")
    (IN ROOMS)
    (LDESC "You are at a complex junction.
A low hands and knees passage from the north joins a higher crawl from the east
to make a walking passage going west. There is also a large room above.
The air is damp here.")
    (UP TO IN-DUSTY-ROCK-ROOM)
    (WEST TO IN-BEDQUILT)
    (NORTH TO IN-SHELL-ROOM)
    (EAST TO IN-ANTEROOM)>

;----------------------------------------------------------------------

<ROOM IN-BEDQUILT
    (DESC "In Bedquilt")
    (IN ROOMS)
    (LDESC "You are in bedquilt, a long east/west passage with holes everywhere.
To explore at random select north, south, up, or down.")
    (EAST TO AT-COMPLEX-JUNCTION)
    (WEST TO IN-SWISS-CHEESE-ROOM)
    (SOUTH TO IN-SLAB-ROOM)
    (UP TO IN-DUSTY-ROCK-ROOM)
    (NORTH TO AT-JUNCTION-OF-THREE)
    (DOWN TO IN-ANTEROOM)
    (ACTION IN-BEDQUILT-F)>

<CONSTANT CRAWLED-AROUND-HOLES "You have crawled around in some little holes and
wound up back in the main passage.">

<ROUTINE IN-BEDQUILT-F (RARG "AUX" DEST)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? WALK>>
           <COND (<AND <PRSO? ,P?SOUTH ,P?DOWN> <PROB 80>>
                  <SET DEST 1>)
                 (<PRSO? ,P?UP>
                  <COND (<PROB 80> <SET DEST 1>)>
                  <COND (<PROB 50> <SET DEST ,IN-SECRET-N/S-CANYON-1>)>)
                 (<PRSO? ,P?NORTH>
                  <COND (<PROB 60> <SET DEST 1>)>
                  <COND (<PROB 75> <SET DEST ,IN-LARGE-LOW-ROOM>)>)>
           <COND (<0? .DEST> <RFALSE>)
                 (<1? .DEST> <TELL ,CRAWLED-AROUND-HOLES CR>)
                 (ELSE <GOTO .DEST> <RTRUE>)>)>>

;----------------------------------------------------------------------

<ROOM IN-SWISS-CHEESE-ROOM
    (DESC "In Swiss Cheese Room")
    (IN ROOMS)
    (LDESC "You are in a room whose walls resemble swiss cheese.
Obvious passages go west, east, ne, and nw.
Part of the room is occupied by a large bedrock block.")
    (WEST TO AT-EAST-END-OF-TWOPIT-ROOM)
    (SOUTH TO IN-TALL-E/W-CANYON)
    (NE TO IN-BEDQUILT)
    (NW TO IN-ORIENTAL-ROOM)
    (EAST TO IN-SOFT-ROOM)
    (ACTION IN-SWISS-CHEESE-ROOM-F)>

<ROUTINE IN-SWISS-CHEESE-ROOM-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG>
                <VERB? WALK>
                <OR <AND <PRSO? ,P?SOUTH> <PROB 80>>
                    <AND <PRSO? ,P?NW> <PROB 50>>>>
           <TELL ,CRAWLED-AROUND-HOLES CR>)>>

<OBJECT BEDROCK-BLOCK
    (DESC "bedrock block")
    (IN IN-SWISS-CHEESE-ROOM)
    (SYNONYM BLOCK)
    (ADJECTIVE BEDROCK LARGE)
    (TEXT "It's just a huge block.")
    (ACTION BEDROCK-BLOCK-F)
    (FLAGS NDESCBIT)>

<ROUTINE BEDROCK-BLOCK-F ()
    <COND (<VERB? LOOK-UNDER PUSH PULL TAKE>
           <TELL "Surely you're joking." CR>)>>

;----------------------------------------------------------------------
"The Twopit Room area"
;----------------------------------------------------------------------

;"Possible heights for the plant"
<CONSTANT TINY-HEIGHT 0>
<CONSTANT TALL-HEIGHT 1>
<CONSTANT HUGE-HEIGHT 2>

<ROOM AT-WEST-END-OF-TWOPIT-ROOM
    (DESC "At West End of Twopit Room")
    (IN ROOMS)
    (LDESC "You are at the west end of the twopit room.
There is a large hole in the wall above the pit at the end of this room.")
    (GLOBAL PLANT-STICKING-UP HOLE-ABOVE-PIT-WEST MIST)
    (EAST TO AT-EAST-END-OF-TWOPIT-ROOM)
    (WEST TO IN-SLAB-ROOM)
    (DOWN TO IN-WEST-PIT)
    (UP SORRY "It is too far up for you to reach.")
    (ACTION POTENTIAL-PLANT-STICKING-UP-ROOMS-F)>

<ROUTINE POTENTIAL-PLANT-STICKING-UP-ROOMS-F (RARG)
    <COND (<AND <=? .RARG ,M-END>
                <VERB? LOOK>
                <NOT <FSET? ,PLANT-STICKING-UP ,INVISIBLE>>>
           <CRLF>
           <DESCRIBE-PLANT-STICKING-UP>
           <RTRUE>)>>

<OBJECT PLANT-STICKING-UP
    (DESC "beanstalk")
    (IN LOCAL-GLOBALS)
    (SYNONYM PLANT BEANSTALK STALK)
    ;"V3 property size is limited to 8 bytes"
    %<VERSION?
        (ZIP '(ADJECTIVE BEAN GIANT TINY LITTLE TWELVE FOOT TALL))
        (ELSE '(ADJECTIVE BEAN GIANT TINY LITTLE TWELVE FOOT TALL MURMURING BELLOWING))>
    (ACTION PLANT-STICKING-UP-F)
    (FLAGS INVISIBLE)>

<ROUTINE DESCRIBE-PLANT-STICKING-UP ()
    <COND (<=? ,PLANT-HEIGHT ,TALL-HEIGHT>
           <TELL "The top of a 12-foot-tall beanstalk is poking out of the west pit." CR>)
          (ELSE
           <TELL "There is a huge beanstalk growing out of the west pit up to the hole." CR>)>>

<ROUTINE PLANT-STICKING-UP-F ()
    <COND (<VERB? EXAMINE> <DESCRIBE-PLANT-STICKING-UP>)
          (<AND <VERB? CLIMB> <=? ,PLANT-HEIGHT ,HUGE-HEIGHT>>
           <PERFORM ,V?CLIMB ,PLANT>
           <RTRUE>)>>

<OBJECT HOLE-ABOVE-PIT-WEST
    (DESC "hole above pit")
    (IN LOCAL-GLOBALS)
    (SYNONYM HOLE PIT)
    (ADJECTIVE HOLE ABOVE)
    (TEXT "The hole is in the wall above the pit at this end of the room.")
    (ACTION HOLE-ABOVE-PIT-WEST-F)>

<ROUTINE HOLE-ABOVE-PIT-WEST-F ()
    <COND (<VERB? ENTER> <TELL "It is too far up for you to reach." CR>)>>

;----------------------------------------------------------------------

<ROOM IN-WEST-PIT
    (DESC "In West Pit")
    (IN ROOMS)
    (LDESC "You are at the bottom of the western pit in the twopit room.
There is a large hole in the wall about 25 feet above you.")
    (GLOBAL HOLE-ABOVE-PIT-WEST)
    (UP TO AT-WEST-END-OF-TWOPIT-ROOM)
    (ACTION IN-WEST-PIT-F)
    (FLAGS SACREDBIT)>

;"TODO: is this CLIMB handler needed?"
<ROUTINE IN-WEST-PIT-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? CLIMB> <NOT <PRSO? ,PLANT>>>
           <COND (<=? ,PLANT-HEIGHT ,TINY-HEIGHT>
                  <TELL "There is nothing here to climb. Use \"up\" or \"out\" to leave the pit." CR>)>)>>

<GLOBAL PLANT-HEIGHT ,TINY-HEIGHT>

<OBJECT PLANT
    (DESC "plant")
    (IN IN-WEST-PIT)
    (SYNONYM PLANT BEANSTALK STALK)
    ;"V3 property size is limited to 8 bytes"
    %<VERSION?
        (ZIP '(ADJECTIVE BEAN GIANT TINY LITTLE TWELVE FOOT TALL))
        (ELSE '(ADJECTIVE BEAN GIANT TINY LITTLE TWELVE FOOT TALL MURMURING BELLOWING))>
    (DESCFCN PLANT-DESCFCN)
    (ACTION PLANT-F)>

<ROUTINE PLANT-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)>
    <COND (<=? ,PLANT-HEIGHT ,TINY-HEIGHT>
           <TELL "There is a tiny little plant in the pit, murmuring \"Water, water, ...\"">)
          (<=? ,PLANT-HEIGHT ,TALL-HEIGHT>
           <TELL "There is a 12-foot-tall beanstalk stretching up out of the pit,
bellowing \"Water!! Water!!\"">)
          (ELSE
           <TELL "There is a giantic beanstalk stretching all the way up to the hole.">)>>

<ROUTINE PLANT-F ("AUX" F)
    <COND (<VERB? CLIMB>
           <COND (<=? ,PLANT-HEIGHT ,TINY-HEIGHT>
                  <TELL "It's just a little plant!" CR>)
                 (<=? ,PLANT-HEIGHT ,TALL-HEIGHT>
                  <TELL "You have climbed up the plant and out of the pit." CR>
                  <GOTO ,AT-WEST-END-OF-TWOPIT-ROOM>
                  <RTRUE>)
                 (ELSE
                  <TELL "You clamber up the plant and scurry through the hole at the top." CR>
                  <GOTO ,IN-NARROW-CORRIDOR>
                  <RTRUE>)>)
          (<VERB? TAKE>
           <TELL CT ,PLANT " has exceptionally deep roots and cannot be pulled free." CR>)
          (<VERB? WATER>
           <COND (<NOT <HELD? ,BOTTLE>>
                  <TELL "You have nothing to water the plant with." CR>)
                 (<NOT <SET F <FIRST? ,BOTTLE>>>
                  <TELL CT ,BOTTLE " is empty." CR>)
                 (<=? .F ,OIL-IN-BOTTLE>
                  <REMOVE .F>
                  <TELL CT ,PLANT " indignantly shakes the oil off its leaves and asks, \"Water?\"" CR>)
                 (ELSE
                  <REMOVE .F>
                  <COND (<=? ,PLANT-HEIGHT ,TINY-HEIGHT>
                         <INC PLANT-HEIGHT>
                         <TELL CT ,PLANT " spurts into furious growth for a few seconds." CR CR>
                         <FCLEAR ,PLANT-STICKING-UP ,INVISIBLE>)
                        (<=? ,PLANT-HEIGHT ,TALL-HEIGHT>
                         <INC PLANT-HEIGHT>
                         <TELL CT ,PLANT " grows explosively, almost filling the bottom of the pit." CR CR>)
                        (ELSE
                         <TELL "You've over-watered the plant! It's shriveling up! It's, it's..." CR CR>
                         <FSET ,PLANT-STICKING-UP ,INVISIBLE>
                         <SETG PLANT-HEIGHT ,TINY-HEIGHT>)>
                  <PERFORM ,V?EXAMINE ,PRSO>
                  <RTRUE>)>)
          (<VERB? OIL>
           <PERFORM ,V?WATER ,PRSO>)
          (<VERB? EXAMINE>
           <PLANT-DESCFCN <>>
           <CRLF>)>>

;----------------------------------------------------------------------

<ROOM AT-EAST-END-OF-TWOPIT-ROOM
    (DESC "At East End of Twopit Room")
    (IN ROOMS)
    (LDESC "You are at the east end of the twopit room.
The floor here is littered with thin rock slabs, which make it easy to descend the pits.
There is a path here bypassing the pits to connect passages from east and west.
There are holes all over, but the only big one is on the wall directly over the west pit
where you can't get to it.")
    (GLOBAL HOLE-ABOVE-PIT-EAST PLANT-STICKING-UP)
    (EAST TO IN-SWISS-CHEESE-ROOM)
    (WEST TO AT-WEST-END-OF-TWOPIT-ROOM)
    (DOWN TO IN-EAST-PIT)
    (ACTION POTENTIAL-PLANT-STICKING-UP-ROOMS-F)>

<OBJECT THIN-ROCK-SLABS
    (DESC "thin rock slabs")
    (IN AT-EAST-END-OF-TWOPIT-ROOM)
    (SYNONYM SLABS SLAB ROCKS STAIRS)
    (ADJECTIVE THIN ROCK)
    (LDESC "They almost form natural stairs down into the pit.")
    (ACTION THIN-ROCK-SLABS-F)
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE THIN-ROCK-SLABS-F ()
    <COND (<VERB? LOOK-UNDER PUSH PULL TAKE>
           <TELL "Surely you're joking. You'd have to blast them aside." CR>)>>

;----------------------------------------------------------------------

<ROOM IN-EAST-PIT
    (DESC "In East Pit")
    (IN ROOMS)
    (LDESC "You are at the bottom of the eastern pit in the twopit room.
There is a small pool of oil in one corner of the pit.")
    (GLOBAL HOLE-ABOVE-PIT-EAST)
    (UP TO AT-EAST-END-OF-TWOPIT-ROOM)
    (FLAGS SACREDBIT)>

<OBJECT OIL
    (DESC "pool of oil")
    (IN IN-EAST-PIT)
    (SYNONYM POOL OIL)
    (TEXT "It looks like ordinary oil.")
    (ACTION OIL-F)
    (FLAGS NDESCBIT)>

<ROUTINE OIL-F ()
    <COND (<VERB? DRINK> <TELL "Absolutely not." CR>)
          (<VERB? TAKE>
           <COND (<HELD? ,BOTTLE> <PERFORM ,V?FILL ,BOTTLE> <RTRUE>)
                 (ELSE <TELL "You have nothing in which to carry the oil." CR>)>)
          (<VERB? PUT-IN>
           <COND (<PRSI? ,BOTTLE> <PERFORM ,V?FILL ,BOTTLE> <RTRUE>)
                 (ELSE <TELL "You have nothing in which to carry the oil." CR>)>)>>

<OBJECT HOLE-ABOVE-PIT-EAST
    (DESC "hole above pit")
    (IN LOCAL-GLOBALS)
    (SYNONYM HOLE PIT)
    (ADJECTIVE HOLE ABOVE)
    (TEXT "The hole is in the wall above you.")
    (FLAGS NDESCBIT)>

;----------------------------------------------------------------------

<ROOM IN-SLAB-ROOM
    (DESC "Slab Room")
    (IN ROOMS)
    (LDESC "You are in a large low circular chamber whose floor is an immense slab
fallen from the ceiling (slab room). East and west there once were large passages,
but they are now filled with boulders. Low small passages go north and south, and
the south one quickly bends west around the boulders.")
    (SOUTH TO AT-WEST-END-OF-TWOPIT-ROOM)
    (UP TO IN-SECRET-N/S-CANYON-0)
    (NORTH TO IN-BEDQUILT)>

<OBJECT SLAB
    (DESC "slab")
    (IN IN-SLAB-ROOM)
    (SYNONYM SLAB)
    (ADJECTIVE IMMENSE)
    (TEXT "It is now the floor here.")
    (ACTION SLAB-F)
    (FLAGS NDESCBIT)>

<ROUTINE SLAB-F ()
    <COND (<VERB? LOOK-UNDER PUSH PULL TAKE>
           <TELL "Surely you're joking." CR>)>>

<OBJECT BOULDERS
    (DESC "boulders")
    (IN IN-SLAB-ROOM)
    (SYNONYM BOULDER ROCKS STONES
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP #SPLICE ()) (ELSE BOULDERS)>)
    (TEXT "They're just ordinary boulders.")
    (FLAGS NDESCBIT MULTITUDEBIT PLURALBIT)>

;----------------------------------------------------------------------
"A small network of Canyons, mostly Secret"
;----------------------------------------------------------------------

<GLOBAL CANYON-FROM <>>

;"Rooms that connect to SECRET-CANYON set CANYON-FROM upon entry so
  SECRET-CANYON can return to the correct room"
<ROUTINE SET-CANYON-FROM-F (RARG)
    <COND (<=? .RARG ,M-ENTER> <SETG CANYON-FROM ,HERE>)>
    <RFALSE>>

<ROOM IN-SECRET-N/S-CANYON-0
    (DESC "Secret N/S Canyon")
    (IN ROOMS)
    (LDESC "You are in a secret N/S canyon above a large room.")
    (DOWN TO IN-SLAB-ROOM)
    (SOUTH TO IN-SECRET-CANYON)
    (NORTH TO IN-MIRROR-CANYON)
    (ACTION SET-CANYON-FROM-F)>

<ROOM IN-SECRET-N/S-CANYON-1
    (DESC "Secret N/S Canyon")
    (IN ROOMS)
    (LDESC "You are in a secret N/S canyon above a sizable passage.")
    (NORTH TO AT-JUNCTION-OF-THREE)
    (DOWN TO IN-BEDQUILT)
    (SOUTH TO ATOP-STALACTITE)>

<ROOM AT-JUNCTION-OF-THREE
    (DESC "Junction of Three Secret Canyons")
    (IN ROOMS)
    (LDESC "You are in a secret canyon at a junction of three canyons,
bearing north, south, and se. The north one is as tall as the other two combined.")
    (SE TO IN-BEDQUILT)
    (SOUTH TO IN-SECRET-N/S-CANYON-1)
    (NORTH TO AT-WINDOW-ON-PIT-2)>

<ROOM IN-LARGE-LOW-ROOM
    (DESC "Large Low Room")
    (IN ROOMS)
    (LDESC "You are in a large low room. Crawls lead north, se, and sw.")
    (SW TO IN-SLOPING-CORRIDOR)
    (SE TO IN-ORIENTAL-ROOM)
    (NORTH TO DEAD-END-CRAWL)>

<ROOM DEAD-END-CRAWL
    (DESC "Dead End Crawl")
    (IN ROOMS)
    (LDESC "This is a dead end crawl.")
    (SOUTH TO IN-LARGE-LOW-ROOM)
    (OUT TO IN-LARGE-LOW-ROOM)>

<ROOM IN-SECRET-E/W-CANYON
    (DESC "Secret E/W Canyon Above Tight Canyon")
    (IN ROOMS)
    (LDESC "You are in a secret canyon which here runs E/W.
It crosses over a very tight canyon 15 feet below.
If you go down you may not be able to get back up.")
    (EAST TO IN-HALL-OF-MT-KING)
    (WEST TO IN-SECRET-CANYON)
    (DOWN TO IN-N/S-CANYON)
    (ACTION SET-CANYON-FROM-F)>

<ROOM IN-N/S-CANYON
    (DESC "N/S Canyon")
    (IN ROOMS)
    (LDESC "You are at a wide place in a very tight N/S canyon.")
    (SOUTH TO CANYON-DEAD-END)
    (NORTH TO IN-TALL-E/W-CANYON)>

<ROOM CANYON-DEAD-END
    (DESC "Canyon Dead End")
    (IN ROOMS)
    (LDESC "The canyon here becomes too tight to go further south.")
    (NORTH TO IN-N/S-CANYON)>

<ROOM IN-TALL-E/W-CANYON
    (DESC "In Tall E/W Canyon")
    (IN ROOMS)
    (LDESC "You are in a tall E/W canyon. A low tight crawl goes 3 feet north
and seems to open up.")
    (EAST TO IN-N/S-CANYON)
    (WEST TO DEAD-END-8)
    (NORTH TO IN-SWISS-CHEESE-ROOM)>

;----------------------------------------------------------------------

<ROOM ATOP-STALACTITE
    (DESC "Atop Stalactite")
    (IN ROOMS)
    (LDESC "A large stalactite extends from the roof and almost reaches the
floor below. You could climb down it, and jump from it to the floor, but
having done so you would be unable to reach it to climb back up.")
    (NORTH TO IN-SECRET-N/S-CANYON-1)
    (DOWN PER DOWN-FROM-ATOP-STALACTITE)
    (ACTION ATOP-STALACTITE-F)>

<ROUTINE DOWN-FROM-ATOP-STALACTITE ()
    <COND (<PROB 40> ,ALIKE-MAZE-6)
          (<PROB 50> ,ALIKE-MAZE-9)
          (ELSE ,ALIKE-MAZE-4)>>

<ROUTINE ATOP-STALACTITE-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? JUMP CLIMB>>
           <PERFORM ,V?WALK ,P?DOWN>
           <RTRUE>)>>

<OBJECT STALACTITE
    (DESC "stalactite")
    (IN ATOP-STALACTITE)
    (SYNONYM STALACTITE STALAGMITE ;STALAGTITE)
    (ADJECTIVE LARGE)
    (TEXT "You could probably climb down it, but you can forget climbing back up.")
    (ACTION STALACTITE-F)
    (FLAGS NDESCBIT)>

<ROUTINE STALACTITE-F ()
    <COND (<VERB? LOOK-UNDER PUSH TAKE>
           <TELL "Do get a grip on yourself." CR>)>>

;----------------------------------------------------------------------
"Here be dragons"
;----------------------------------------------------------------------

<ROOM IN-SECRET-CANYON
    (DESC "Secret Canyon")
    (IN ROOMS)
    (LDESC "You are in a secret canyon which exits to the north and east.")
    (EAST PER EAST-FROM-IN-SECRET-CANYON)
    (NORTH PER NORTH-FROM-IN-SECRET-CANYON)
    (OUT PER OUT-FROM-IN-SECRET-CANYON)
    (ACTION IN-SECRET-CANYON-F)>

<ROUTINE EAST-FROM-IN-SECRET-CANYON ()
    <DIR-EXIT-FROM-IN-SECRET-CANYON ,IN-SECRET-E/W-CANYON>>

<ROUTINE NORTH-FROM-IN-SECRET-CANYON ()
    <DIR-EXIT-FROM-IN-SECRET-CANYON ,IN-SECRET-N/S-CANYON-0>>

<ROUTINE DIR-EXIT-FROM-IN-SECRET-CANYON (DEST)
    <COND (<AND <N=? ,CANYON-FROM .DEST> <IN? ,DRAGON ,HERE>>
           <TELL CT ,DRAGON " looks rather nasty. You'd best not try to get by." CR>)
          (ELSE .DEST)>>

<ROUTINE OUT-FROM-IN-SECRET-CANYON ()
    ,CANYON-FROM>

<ROUTINE IN-SECRET-CANYON-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (,DRAGON-BEING-ATTACKED
                  <COND (<VERB? YES>
                         <REMOVE ,DRAGON>
                         <MOVE ,DRAGON-CORPSE ,HERE>
                         <SETG DRAGON-BEING-ATTACKED <>>
                         <TELL "Congratulations!
You have just vanquished a dragon with your bare hands!
(Unbelievable, isn't it?)" CR>
                         <RTRUE>)
                        (<VERB? NO>
                         <SETG DRAGON-BEING-ATTACKED <>>
                         <TELL "I should think not." CR>
                         <RTRUE>)>)>
           <SETG DRAGON-BEING-ATTACKED <>>)>>

<GLOBAL DRAGON-BEING-ATTACKED <>>

<OBJECT DRAGON
    (DESC "dragon")
    (IN IN-SECRET-CANYON)
    (SYNONYM DRAGON MONSTER BEAST LIZARD)
    (ADJECTIVE HUGE GREEN FIERCE SCALY GIANT FEROCIOUS)
    (FDESC "A huge green fierce dragon bars the way!")
    (TEXT "I wouldn't mess with it if I were you.")
    (ACTION DRAGON-F)>

<ROUTINE DRAGON-F ()
    <COND (<VERB? ATTACK>
           <SETG DRAGON-BEING-ATTACKED T>
           <TELL "With what? Your bare hands?" CR>)
          (<AND <VERB? GIVE> <PRSI? ,DRAGON>>
           <TELL T ,PRSI " is implacable.">)
          (<AND <VERB? THROW-AT> <PRSI? ,DRAGON>>
           <COND (<NOT <PRSO? ,AXE>>
                  <TELL "You'd probably be better off using your bare hands than that thing!" CR>)
                 (ELSE
                  <MOVE ,AXE ,HERE>
                  <TELL "The axe bounces harmlessly off the dragon's thick scales." CR>)>)>>

<OBJECT PERSIAN-RUG
    (DESC "Persian rug")
    (IN IN-SECRET-CANYON)
    (SYNONYM RUG PERSIAN)
    (ADJECTIVE PERSIAN FINE FINEST
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP DRAGON) (ELSE DRAGON\'S)>)
    (DESCFCN PERSIAN-RUG-DESCFCN)
    (ACTION PERSIAN-RUG-F)
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TRYTAKEBIT TREASUREBIT)>

<ROUTINE PERSIAN-RUG-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)
          (<IN? ,DRAGON ,HERE>
           <TELL CT ,DRAGON " is sprawled out on " T ,PERSIAN-RUG "!" CR>)
          (ELSE
           <TELL CT ,PERSIAN-RUG " is spread out on the floor here." CR>)>>

<ROUTINE PERSIAN-RUG-F ()
    <COND (<AND <VERB? TAKE> <IN? ,DRAGON ,HERE>>
           <TELL "You'll need to get the dragon to move first!" CR>)>>

<OBJECT DRAGON-CORPSE
    (DESC "dragon's body")
    (SYNONYM DRAGON CORPSE BODY)
    (ADJECTIVE DEAD
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP DRAGON) (ELSE DRAGON\'S)>)
    (FDESC "The body of a huge green dead dragon is lying off to one side.")
    (ACTION DRAGON-CORPSE-F)>

<ROUTINE DRAGON-CORPSE-F ()
    <COND (<VERB? ATTACK> <TELL "You've already done enough damage!" CR>)>>

;----------------------------------------------------------------------
"And more of the Alike Maze"
;----------------------------------------------------------------------

<ROOM DEAD-END-8
    (DESC "Dead End")
    (IN ROOMS)
    (LDESC "The canyon runs into a mass of boulders -- dead end.")
    (ACTION DEAD-END-ROOMS-F)
    (SOUTH TO IN-TALL-E/W-CANYON)
    (OUT TO IN-TALL-E/W-CANYON)>

<MAZE-ROOM ALIKE-MAZE-11
    (NORTH 1) (WEST 11) (SOUTH 11) (EAST 9) (NE 10)>

<DEAD-END-ROOM DEAD-END-9 WEST ALIKE-MAZE-11>

<DEAD-END-ROOM DEAD-END-10 SOUTH ALIKE-MAZE-3>

<MAZE-ROOM ALIKE-MAZE-12
    (SOUTH AT-BRINK-OF-PIT) (EAST 13) (WEST 11)>

<MAZE-ROOM ALIKE-MAZE-13
    (NORTH AT-BRINK-OF-PIT) (WEST 12) (NW 13)>

<DEAD-END-ROOM DEAD-END-11 EAST ALIKE-MAZE-12>

<DEAD-END-ROOM DEAD-END-12 UP ALIKE-MAZE-8>

<MAZE-ROOM ALIKE-MAZE-14
    (UP 4) (DOWN 4)>

<ROOM DEAD-END-13
    (DESC "Dead End")
    (IN ROOMS)
    (LDESC "This is the pirate's dead end.")
    (SE TO ALIKE-MAZE-13)
    (OUT TO ALIKE-MAZE-13)
    (ACTION DEAD-END-13-F)
    (FLAGS SACREDBIT)>

<ROUTINE DEAD-END-13-F (RARG)
    <COND (<=? .RARG ,M-ENTER>
           <DEQUEUE I-PIRATE>
           <COND (<AND <IN? ,TREASURE-CHEST ,HERE>
                       <NOT <FSET? ,TREASURE-CHEST ,TOUCHBIT>>>
                  <TELL "You've found the pirate's treasure chest!" CR>)>)>>

<OBJECT TREASURE-CHEST
    (DESC "treasure chest")
    (IN DEAD-END-13)
    (SYNONYM CHEST BOX RICHES TREASURE)
    (ADJECTIVE PIRATE TREASURE
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP #SPLICE ()) (ELSE PIRATE\'S)>)
    (FDESC "The pirate's treasure chest is here!")
    (TEXT "It's the pirate's treasure chest, filled with riches of all kinds!")
    (DEPOSIT-POINTS 12)
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------
"Above the beanstalk: the Giant Room and the Waterfall"
;----------------------------------------------------------------------

<ROOM IN-NARROW-CORRIDOR
    (DESC "In Narrow Corridor")
    (IN ROOMS)
    (LDESC "You are in a long, narrow corridor stretching out of sight to the west.
At the eastern end is a hole through which you can see a profusion of leaves.")
    (DOWN TO IN-WEST-PIT)
    (WEST TO IN-GIANT-ROOM)
    (EAST TO IN-WEST-PIT)
    (ACTION IN-NARROW-CORRIDOR-F)>

<ROUTINE IN-NARROW-CORRIDOR-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? JUMP>>
           <JIGS-UP "You fall and break your neck!">)>>

<OBJECT LEAVES
    (DESC "leaves")
    (IN IN-NARROW-CORRIDOR)
    ;"V3 property size is limited to 8 bytes"
    %<VERSION?
        (ZIP '(SYNONYM LEAF LEAVES PROFUSION TREE))
        (ELSE '(SYNONYM LEAF LEAVES PROFUSION TREE STALK BEANSTALK PLANT))>
    (ACTION LEAVES-F)
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE LEAVES-F ()
    <COND (<VERB? COUNT> <TELL "69,105." CR>)>>

;----------------------------------------------------------------------

<ROOM AT-STEEP-INCLINE
    (DESC "Steep Incline Above Large Room")
    (IN ROOMS)
    (LDESC "You are at the top of a steep incline above a large room.
You could climb down here, but you would not be able to climb up.
There is a passage leading back to the north.")
    (NORTH TO IN-CAVERN-WITH-WATERFALL)
    (DOWN TO IN-LARGE-LOW-ROOM)>

;----------------------------------------------------------------------

<ROOM IN-GIANT-ROOM
    (DESC "Giant Room")
    (IN ROOMS)
    (LDESC "You are in the giant room.
The ceiling here is too high up for your lamp to show it.
Cavernous passages lead east, north, and south.
On the west wall is scrawled the inscription, \"Fee fie foe foo\" [sic].")
    (SOUTH TO IN-NARROW-CORRIDOR)
    (EAST TO AT-RECENT-CAVE-IN)
    (NORTH TO IN-IMMENSE-N/S-PASSAGE)>

<OBJECT SCRAWLED-INSCRIPTION
    (DESC "scrawled inscription")
    (IN IN-GIANT-ROOM)
    (SYNONYM INSCRIPTION WRITING SCRAWL)
    (ADJECTIVE
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP SCRAWL) (ELSE SCRAWLED)>)
    (TEXT "It says, \"Fee fie foe foo\" [sic].")>

<OBJECT GOLDEN-EGGS
    (DESC "nest of golden eggs")
    (IN IN-GIANT-ROOM)
    (SYNONYM EGGS EGG NEST)
    (ADJECTIVE GOLDEN BEAUTIFUL)
    (FDESC "There is a large nest here, full of golden eggs!")
    (TEXT "The nest is filled with beautiful golden eggs!")
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT PLURALBIT MULTITUDEBIT)>

;----------------------------------------------------------------------

<ROOM AT-RECENT-CAVE-IN
    (DESC "Recent Cave-in")
    (IN ROOMS)
    (LDESC "The passage here is blocked by a recent cave-in.")
    (SOUTH TO IN-GIANT-ROOM)>

;----------------------------------------------------------------------

<ROOM IN-IMMENSE-N/S-PASSAGE
    (DESC "Immense N/S Passage")
    (IN ROOMS)
    (LDESC "You are at one end of an immense north/south passage.")
    (SOUTH TO IN-GIANT-ROOM)
    (NORTH TO IN-CAVERN-WITH-WATERFALL IF RUSTY-DOOR IS OPEN)
    (ACTION IN-IMMENSE-N/S-PASSAGE-F)>

<ROUTINE IN-IMMENSE-N/S-PASSAGE-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? WALK> <PRSO? ,P?NORTH>>
           <COND (<FSET? ,RUSTY-DOOR ,LOCKEDBIT>
                  <PERFORM ,V?OPEN ,RUSTY-DOOR>
                  <RTRUE>)
                 (<NOT <FSET? ,RUSTY-DOOR ,OPENBIT>>
                  <FSET ,RUSTY-DOOR ,OPENBIT>
                  <TELL "[first wrenching the door open]" CR>
                  <RFALSE>)>)>>

<OBJECT RUSTY-DOOR
    (DESC "rusty door")
    (IN IN-IMMENSE-N/S-PASSAGE)
    (SYNONYM DOOR HINGE HINGES)
    (ADJECTIVE MASSIVE RUSTY IRON)
    (TEXT "It's just a big iron door.")
    (DESCFCN RUSTY-DOOR-DESCFCN)
    (ACTION RUSTY-DOOR-F)
    (FLAGS DOORBIT LOCKEDBIT)>

<ROUTINE RUSTY-DOOR-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)>
    <TELL "The way north "
          <COND (<FSET? ,RUSTY-DOOR ,OPENBIT> "leads through")
                (ELSE "is barred by")>
          "a massive, rusty, iron door.">>

<CONSTANT HINGES-ARE-RUSTED
    "The hinges are quite thoroughly rusted now and won't budge.">

<ROUTINE RUSTY-DOOR-F ()
    <COND (<VERB? OPEN>
           <COND (<FSET? ,PRSO ,LOCKEDBIT>
                  <TELL ,HINGES-ARE-RUSTED CR>)
                 (<NOT <FSET? ,PRSO ,OPENBIT>>
                  <FSET ,PRSO ,OPENBIT>
                  <TELL "The door heaves open with a shower of rust." CR>)>)
          (<VERB? CLOSE>
           <COND (<FSET? ,PRSO ,OPENBIT>
                  <TELL "With all the effort it took to get the door open,
I wouldn't suggest closing it again." CR>)
                 (ELSE <TELL "No problem there -- it already is." CR>)>)
          (<VERB? OIL>
           <COND (<AND <HELD? ,BOTTLE> <IN? ,OIL-IN-BOTTLE ,BOTTLE>>
                  <REMOVE ,OIL-IN-BOTTLE>
                  <FCLEAR ,PRSO ,LOCKEDBIT>
                  <FSET ,PRSO ,OPENABLEBIT>
                  <TELL "The oil has freed up the hinges so that the door will move,
although it requires some effort." CR>)
                 (ELSE <TELL "You have nothing to oil it with." CR>)>)
          (<VERB? WATER>
           <COND (<AND <HELD? ,BOTTLE> <IN? ,WATER-IN-BOTTLE ,BOTTLE>>
                  <REMOVE ,WATER-IN-BOTTLE>
                  <FSET ,PRSO ,LOCKEDBIT>
                  <FCLEAR ,PRSO ,OPENABLEBIT>
                  <TELL ,HINGES-ARE-RUSTED CR>)
                 (ELSE <TELL "You have nothing to water it with." CR>)>)>>

;----------------------------------------------------------------------

<ROOM IN-CAVERN-WITH-WATERFALL
    (DESC "In Cavern With Waterfall")
    (IN ROOMS)
    (LDESC "You are in a magnificent cavern with a rushing stream,
which cascades over a sparkling waterfall into a roaring whirlpool
which disappears through a hole in the floor.
Passages exit to the south and west.")
    (GLOBAL STREAM)
    (SOUTH TO IN-IMMENSE-N/S-PASSAGE)
    (WEST TO AT-STEEP-INCLINE)>

<OBJECT WATERFALL
    (DESC "waterfall")
    (IN IN-CAVERN-WITH-WATERFALL)
    (SYNONYM WATERFALL WHIRLPOOL)
    (ADJECTIVE SPARKLING WHIRLING)
    (TEXT "Wouldn't want to go down in a barrel!")
    (FLAGS NDESCBIT)>

<OBJECT TRIDENT
    (DESC "jeweled trident")
    (IN IN-CAVERN-WITH-WATERFALL)
    (SYNONYM TRIDENT)
    (ADJECTIVE JEWELED JEWEL-ENCRUSTED ENCRUSTED FABULOUS)
    (FDESC "There is a jewel-encrusted trident here!")
    (TEXT "The trident is covered with fabulous jewels!")
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------
"The caves around Bedquilt"
;----------------------------------------------------------------------

<ROOM IN-SOFT-ROOM
    (DESC "In Soft Room")
    (IN ROOMS)
    (LDESC "You are in the soft room.
The walls are covered with heavy curtains, the floor with a thick pile carpet.
Moss covers the ceiling.")
    (WEST TO IN-SWISS-CHEESE-ROOM)>

<OBJECT CARPET
    (DESC "carpet")
    (IN IN-SOFT-ROOM)
    (SYNONYM CARPET)
    (ADJECTIVE SHAG PILE HEAVY THICK)
    (TEXT "The carpet is quite plush.")
    (FLAGS NDESCBIT)>

<OBJECT CURTAINS
    (DESC "curtains")
    (IN IN-SOFT-ROOM)
    ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
    %<VERSION? (ZIP '(SYNONYM CURTAIN))
               (ELSE '(SYNONYM CURTAIN CURTAINS))>
    (ADJECTIVE HEAVY THICK)
    (TEXT "They seem to absorb sound very well.")
    (ACTION CURTAINS-F)
    (FLAGS NDESCBIT)>

<ROUTINE CURTAINS-F ()
    <COND (<VERB? TAKE>
           <TELL "Now don't go ripping up the place!" CR>)
          (<VERB? LOOK-UNDER SEARCH>
           <TELL "You don't find anything exciting behind the curtains." CR>)>>

<OBJECT MOSS
    (DESC "moss")
    (IN IN-SOFT-ROOM)
    (SYNONYM MOSS)
    (ADJECTIVE TYPICAL EVERYDAY)
    (TEXT "It just looks like your typical, everyday moss.")
    (ACTION MOSS-F)
    (FLAGS NDESCBIT)>

<ROUTINE MOSS-F ()
    <COND (<VERB? TAKE> <TELL "It crumbles to nothing in your hands." CR>)>>

<OBJECT VELVET-PILLOW
    (DESC "velvet pillow")
    (IN IN-SOFT-ROOM)
    (SYNONYM PILLOW)
    (ADJECTIVE VELVET SMALL)
    (FDESC "A small velvet pillow lies on the floor.")
    (TEXT "It's just a small velvet pillow.")
    (FLAGS TAKEBIT)>

;----------------------------------------------------------------------

<ROOM IN-ORIENTAL-ROOM
    (DESC "Oriental Room")
    (IN ROOMS)
    (LDESC "This is the oriental room.
Ancient oriental cave drawings cover the walls.
A gently sloping passage leads upward to the north, another passage leads se,
and a hands and knees crawl leads west.")
    (WEST TO IN-LARGE-LOW-ROOM)
    (SE TO IN-SWISS-CHEESE-ROOM)
    (UP TO IN-MISTY-CAVERN)
    (NORTH TO IN-MISTY-CAVERN)>

<OBJECT ANCIENT-ORIENTAL-PAINTINGS
    (DESC "ancient oriental paintings")
    (IN IN-ORIENTAL-ROOM)
    (SYNONYM PAINTINGS DRAWINGS ART)
    (ADJECTIVE CAVE ANCIENT ORIENTAL)
    (TEXT "They seem to depict people and animals.")
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT VOWELBIT)>

<OBJECT MING-VASE
    (DESC "ming vase")
    (IN IN-ORIENTAL-ROOM)
    (SYNONYM VASE)
    (ADJECTIVE MING DELICATE)
    (TEXT "It's a delicate, precious, ming vase!")
    (ACTION MING-VASE-F)
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT)>

<ROUTINE MING-VASE-F ()
    <COND (<VERB? DROP>
           <COND (<IN? ,VELVET-PILLOW ,HERE>
                  <TELL "(coming to rest, delicately, on " T ,VELVET-PILLOW ")" CR>
                  <RFALSE>)>
           <REMOVE ,PRSO>
           <MOVE ,SHARDS ,HERE>
           <TELL CT ,PRSO " drops with a delicate crash." CR>)
          (<VERB? ATTACK>
           <REMOVE ,PRSO>
           <MOVE ,SHARDS ,HERE>
           <TELL "You have taken the vase and hurled it delicately to the ground." CR>)
          (<AND <VERB? PUT-IN> <PRSI? ,MING-VASE>>
           <TELL "The vase is too fragile to use as a container." CR>)>>

<OBJECT SHARDS
    (DESC "worthless shards of pottery")
    (SYNONYM POTTERY SHARDS REMAINS VASE)
    (ADJECTIVE WORTHLESS)
    (FDESC "The floor is littered with worthless shards of pottery.")
    (TEXT "They look to be the remains of what was once a beautiful vase.
I guess some oaf must have dropped it.")
    (FLAGS TAKEBIT PLURALBIT MULTITUDEBIT)>

;----------------------------------------------------------------------

<ROOM IN-MISTY-CAVERN
    (DESC "Misty Cavern")
    (IN ROOMS)
    (LDESC "You are following a wide path around the outer edge of a large cavern.
Far below, through a heavy white mist, strange splashing noises can be heard.
The mist rises up through a fissure in the ceiling.
The path exits to the south and west.")
    (GLOBAL MIST)
    (SOUTH TO IN-ORIENTAL-ROOM)
    (WEST TO IN-ALCOVE)>

<OBJECT MISTY-CAVERN-FISSURE
    (DESC "fissure")
    (IN IN-MISTY-CAVERN)
    (SYNONYM FISSURE CEILING)
    (TEXT "You can't really get close enough to examine it.")
    (FLAGS NDESCBIT)>

;----------------------------------------------------------------------
"Plovers and pyramids"
;----------------------------------------------------------------------

<ROOM IN-ALCOVE
    (DESC "Alcove")
    (IN ROOMS)
    (LDESC "You are in an alcove.
A small northwest path seems to widen after a short distance.
An extremely tight tunnel leads east.
It looks like a very tight squeeze.
An eerie light can be seen at the other end.")
    (NW TO IN-MISTY-CAVERN)
    (EAST PER EAST-FROM-IN-ALCOVE)>

<ROUTINE EAST-FROM-IN-ALCOVE ()
    <ONLY-EMERALD-CAN-PASS ,IN-PLOVER-ROOM>>

<ROUTINE ONLY-EMERALD-CAN-PASS (DEST "AUX" F)
    ;"The player can pass if empty-handed, or if only carrying the emerald"
    <COND (<OR <NOT <SET F <FIRST? ,WINNER>>>
               <AND <=? .F ,EGG-SIZED-EMERALD> <NOT <NEXT? .F>>>>
           .DEST)
          (ELSE
           <TELL "Something you're carrying won't fit through the tunnel with you.
You'd best take inventory and drop something." CR>
           <RFALSE>)>>

;----------------------------------------------------------------------

<ROOM IN-PLOVER-ROOM
    (DESC "Plover Room")
    (IN ROOMS)
    (LDESC "You're in a small chamber lit by an eerie green light.
An extremely narrow tunnel exits to the west.
A dark corridor leads northeast.")
    (NE TO IN-DARK-ROOM)
    (WEST PER WEST-FROM-IN-PLOVER-ROOM)
    (ACTION IN-PLOVER-ROOM-F)
    (FLAGS LIGHTBIT)>

<ROUTINE WEST-FROM-IN-PLOVER-ROOM ()
    <ONLY-EMERALD-CAN-PASS ,IN-ALCOVE>>

<ROUTINE IN-PLOVER-ROOM-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<VERB? PLOVER>
                  <COND (<HELD? ,EGG-SIZED-EMERALD>
                         <MOVE ,EGG-SIZED-EMERALD ,IN-PLOVER-ROOM>
                         <SETG SCORE <- ,SCORE 5>>)>
                  <GOTO ,AT-Y2>
                  <RTRUE>)
                 (<AND <VERB? WALK> <PRSO? ,P?OUT>>
                  <PERFORM ,V?WALK ,P?WEST>
                  <RTRUE>)>)>>

<OBJECT EGG-SIZED-EMERALD
    (DESC "emerald the size of a plover's egg")
    (IN IN-PLOVER-ROOM)
    (SYNONYM EMERALD EGG)
    (ADJECTIVE EGG-SIZED EGG SIZED PLOVER\'S)
    (FDESC "There is an emerald here the size of a plover's egg!")
    (TEXT "Plover's eggs, by the way, are quite large.")
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT VOWELBIT)>

;----------------------------------------------------------------------

<ROOM IN-DARK-ROOM
    (DESC "The Dark Room")
    (IN ROOMS)
    (LDESC "You're in the dark-room. A corridor leading south is the only exit.")
    (SOUTH TO IN-PLOVER-ROOM)
    (FLAGS SACREDBIT)>

<OBJECT STONE-TABLET
    (DESC "stone tablet")
    (IN IN-DARK-ROOM)
    (SYNONYM TABLET)
    (ADJECTIVE MASSIVE STONE)
    (FDESC "A massive stone tablet imbedded in the wall reads:
\"Congratulations on bringing light into the dark-room!\"")>

<OBJECT PLATINUM-PYRAMID
    (DESC "platinum pyramid")
    (IN IN-DARK-ROOM)
    (SYNONYM PYRAMID)
    (ADJECTIVE PLATINUM ;PYRAMIDAL)
    (FDESC "There is a platinum pyramid here, 8 inches on a side!")
    (TEXT "The platinum pyramid is 8 inches on a side!")
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------
"North of the complex junction: a long up-down corridor"
;----------------------------------------------------------------------

<ROOM IN-ARCHED-HALL
    (DESC "Arched Hall")
    (IN ROOMS)
    (LDESC "You are in an arched hall.
A coral passage once continued up and east from here, but is now blocked by debris.
The air smells of sea water.")
    (DOWN TO IN-SHELL-ROOM)>

;----------------------------------------------------------------------

<ROOM IN-SHELL-ROOM
    (DESC "Shell Room")
    (IN ROOMS)
    (LDESC "You're in a large room carved out of sedimentary rock.
The floor and walls are littered with bits of shells imbedded in the stone.
A shallow passage proceeds downward, and a somewhat steeper one leads up.
A low hands and knees passage enters from the south.")
    (UP TO IN-ARCHED-HALL)
    (DOWN TO IN-RAGGED-CORRIDOR)
    (SOUTH PER SOUTH-FROM-IN-SHELL-ROOM)>

<ROUTINE SOUTH-FROM-IN-SHELL-ROOM ()
    <COND (<HELD? ,GIANT-BIVALVE>
           <TELL "You can't fit this five-foot " <OYSTER-OR-CLAM> " through that little passage!" CR>
           <RFALSE>)
          (ELSE ,AT-COMPLEX-JUNCTION)>>

<GLOBAL OYSTER-REVEALED <>>

<ROUTINE OYSTER-OR-CLAM ()
    <COND (,OYSTER-REVEALED "oyster") (ELSE "clam")>>

<OBJECT GIANT-BIVALVE
    (DESC "giant clam")
    (IN IN-SHELL-ROOM)
    (SYNONYM CLAM OYSTER BIVALVE)
    (ADJECTIVE GIANT)
    (DESCFCN GIANT-BIVALVE-DESCFCN)
    (ACTION GIANT-BIVALVE-F)
    (FLAGS TAKEBIT)>

<ROUTINE GIANT-BIVALVE-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)>
    <TELL "There is an enormous " <OYSTER-OR-CLAM> " here with its shell tightly closed.">>

<ROUTINE ISARE (O)
    <COND (<FSET? .O ,PLURALBIT> "are") (ELSE "is")>>

<ROUTINE GIANT-BIVALVE-F ()
    <COND (<VERB? EXAMINE>
           <COND (<=? ,HERE ,AT-NE-END ,AT-SW-END>
                  <TELL "Interesting. There seems to be something written
on the underside of the " <OYSTER-OR-CLAM> ":||
\"There is something strange about this place,
such that one of the curses I've always known now has a new effect.\"" CR>)
                 (ELSE <TELL "A giant bivalve of some kind." CR>)>)
          (<VERB? OPEN>
           <TELL "You aren't strong enough to open the " <OYSTER-OR-CLAM> " with your bare hands." CR>)
          (<AND <VERB? UNLOCK> <PRSO? ,GIANT-BIVALVE>>
           <COND (<NOT <PRSI? ,TRIDENT>>
                  <TELL CT ,PRSI " " <ISARE ,PRSI> "n't strong enough to open the " <OYSTER-OR-CLAM> "." CR>)
                 (,OYSTER-REVEALED
                  <TELL "The oyster creaks open, revealing nothing but oyster inside.
It promptly snaps shut again." CR>)
                 (ELSE
                  <SETG OYSTER-REVEALED T>
                  <MOVE ,PEARL ,IN-A-CUL-DE-SAC>
                  <TELL "A glistening pearl falls out of the clam and rolls away.
Goodness, this must really be an oyster.
(I never was very good at identifying bivalves.)
Whatever it is, it has now snapped shut again." CR>)>)
          (<VERB? ATTACK> <TELL "The shell is very strong and impervious to attack." CR>)>>

<OBJECT PEARL
    (DESC "glistening pearl")
    (SYNONYM PEARL)
    (ADJECTIVE GLISTENING ;INCREDIBLE INCREDIBLY LARGE)
    (FDESC "Off to one side lies a glistening pearl!")
    (TEXT "It's incredibly large!")
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT)>

;----------------------------------------------------------------------

<ROOM IN-RAGGED-CORRIDOR
    (DESC "Ragged Corridor")
    (IN ROOMS)
    (LDESC "You are in a long sloping corridor with ragged sharp walls.")
    (UP TO IN-SHELL-ROOM)
    (DOWN TO IN-A-CUL-DE-SAC)>

<ROOM IN-A-CUL-DE-SAC
    (DESC "Cul-de-Sac")
    (IN ROOMS)
    (LDESC "You are in a cul-de-sac about eight feet across.")
    (UP TO IN-RAGGED-CORRIDOR)
    (OUT TO IN-RAGGED-CORRIDOR)>

;----------------------------------------------------------------------
"Witt's End: Cave under construction"
;----------------------------------------------------------------------

<ROOM IN-ANTEROOM
    (DESC "In Anteroom")
    (IN ROOMS)
    (LDESC "You are in an anteroom leading to a large passage to the east.
Small passages go west and up.
The remnants of recent digging are evident.")
    (UP TO AT-COMPLEX-JUNCTION)
    (WEST TO IN-BEDQUILT)
    (EAST TO AT-WITTS-END)>

<OBJECT CONSTRUCTION-SIGN
    (DESC "sign")
    (IN IN-ANTEROOM)
    (FDESC "A sign in midair here says \"Cave under construction beyond this point.
Proceed at own risk. [Witt Construction Company]\"")
    (ACTION CONSTRUCTION-SIGN-F)>

<ROUTINE CONSTRUCTION-SIGN-F ()
    <COND (<VERB? TAKE> <TELL "It's hanging way above your head." CR>)>>

<OBJECT MAGAZINES
    (DESC "recent issues of \"Spelunker Today\"")
    (IN IN-ANTEROOM)
    (SYNONYM MAGAZINE ISSUE ISSUES TODAY)
    (ADJECTIVE RECENT SPELUNKER)
    (ARTICLE "a few")
    (FDESC "There are a few recent issues of \"Spelunker Today\" magazine here.")
    (TEXT "I'm afraid the magazines are written in Dwarvish.")
    (ACTION MAGAZINES-F)
    (FLAGS TAKEBIT TRYTAKEBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE MAGAZINES-F ()
    <COND (<AND <VERB? TAKE> <IN? ,PRSO ,AT-WITTS-END>>
           <V-TAKE>
           <COND (<NOT <IN? ,PRSO ,AT-WITTS-END>> <DEC SCORE>)>
           <RTRUE>)
          (<AND <VERB? DROP> <=? ,HERE ,AT-WITTS-END> <HELD? ,PRSO>>
           <MOVE ,PRSO ,HERE>
           <INC SCORE>
           <TELL "You really are at wit's end." CR>)>>

;----------------------------------------------------------------------

<ROOM AT-WITTS-END
    (DESC "At Witt's End")
    (IN ROOMS)
    (LDESC "You are at Witt's End. Passages lead off in *all* directions.")
    (WEST SORRY "You have crawled around in some little holes
and found your way blocked by a recent cave-in.
You are now back in the main passage.")
    (ACTION AT-WITTS-END-F)>

<ROUTINE AT-WITTS-END-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? WALK> <NOT <PRSO? ,P?WEST>>>
           <COND (<PROB 95> <TELL ,CRAWLED-AROUND-HOLES CR>)
                 (ELSE <GOTO ,IN-ANTEROOM> <RTRUE>)>)>>

;----------------------------------------------------------------------
"North of the secret canyons, on the other side of the pit"
;----------------------------------------------------------------------

<ROOM IN-MIRROR-CANYON
    (DESC "In Mirror Canyon")
    (IN ROOMS)
    (LDESC "You are in a north/south canyon about 25 feet across.
The floor is covered by white mist seeping in from the north.
The walls extend upward for well over 100 feet.
Suspended from some unseen point far above you,
an enormous two-sided mirror is hanging parallel to and midway between the canyon walls.||
A small window can be seen in either wall, some fifty feet up.")
    (GLOBAL MIST)
    (SOUTH TO IN-SECRET-N/S-CANYON-0)
    (NORTH TO AT-RESERVOIR)>

<CONSTANT MIRROR-FOR-DWARVES "The mirror is obviously provided for the use of the dwarves
who, as you know, are extremely vain.">

<OBJECT SUSPENDED-MIRROR
    (DESC "suspended mirror")
    (IN IN-MIRROR-CANYON)
    (SYNONYM MIRROR)
    (ADJECTIVE MASSIVE ENORMOUS HANGING SUSPENDED TWO-SIDED TWO SIDED
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP DWARVES) (ELSE DWARVES\')>)
    (FDESC ,MIRROR-FOR-DWARVES)
    (TEXT ,MIRROR-FOR-DWARVES)
    (ACTION SUSPENDED-MIRROR-F)>

<ROUTINE SUSPENDED-MIRROR-F ()
    ;"TODO: is TAKE the right verb here?"
    <COND (<VERB? ATTACK TAKE> <TELL "You can't reach it from here." CR>)>>

;----------------------------------------------------------------------

<ROOM AT-WINDOW-ON-PIT-2
    (DESC "At Window on Pit")
    (IN ROOMS)
    (GLOBAL WINDOW HUGE-PIT MARKS-IN-DUST SHADOWY-FIGURE MIST)
    (ACTION AT-WINDOW-ON-PIT-ROOMS-F)
    (WEST TO AT-JUNCTION-OF-THREE)>

;----------------------------------------------------------------------

<ROOM AT-RESERVOIR
    (DESC "At Reservoir")
    (IN ROOMS)
    (LDESC "You are at the edge of a large underground reservoir.
An opaque cloud of white mist fills the room and rises rapidly upward.
The lake is fed by a stream, which tumbles out of a hole in the wall about 10 feet overhead
and splashes noisily into the water somewhere within the mist.
The only passage goes back toward the south.")
    (GLOBAL STREAM MIST)
    (SOUTH TO IN-MIRROR-CANYON)
    (OUT TO IN-MIRROR-CANYON)
    (ACTION AT-RESERVOIR-F)>

<ROUTINE AT-RESERVOIR-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? SWIM>>
           <TELL "The water is icy cold, and you would soon freeze to death." CR>)>>

;----------------------------------------------------------------------
"The Chasm and the Troll Bridge"
;----------------------------------------------------------------------

<ROOM IN-SLOPING-CORRIDOR
    (DESC "Sloping Corridor")
    (IN ROOMS)
    (LDESC "You are in a long winding corridor sloping out of sight in both directions.")
    (DOWN TO IN-LARGE-LOW-ROOM)
    (UP TO ON-SW-SIDE-OF-CHASM)
    (ACTION IN-SLOPING-CORRIDOR-F)>

<ROUTINE IN-SLOPING-CORRIDOR-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? WALK> <0? <GETPT ,HERE ,PRSO>>>
           <TELL "The corridor slopes steeply up and down." CR>)>>

<ROOM ON-SW-SIDE-OF-CHASM
    (DESC "On SW Side of Chasm")
    (IN ROOMS)
    (LDESC "You are on one side of a large, deep chasm.
A heavy white mist rising up from below obscures all view of the far side.
A southwest path leads away from the chasm into a winding corridor.")
    (GLOBAL RICKETY-BRIDGE MIST)
    (NE PER CROSS-RICKETY-BRIDGE)
    (SW TO IN-SLOPING-CORRIDOR)
    (DOWN TO IN-SLOPING-CORRIDOR)
    (ACTION ON-SW-SIDE-OF-CHASM-F)>

<ROUTINE ON-SW-SIDE-OF-CHASM-F (RARG)
    <COND (<=? .RARG ,M-BEG>
           <COND (<AND <VERB? WALK> <0? <GETPT ,HERE ,PRSO>>>
                  <TELL "The path winds southwest." CR>)
                 (<VERB? JUMP>
                  <COND (<NOT <FSET? ,RICKETY-BRIDGE ,INVISIBLE>>
                         <TELL ,USE-THE-BRIDGE CR>)
                        (ELSE <JIGS-UP "You didn't make it.">)>)>)
          (<AND <=? .RARG ,M-END> <VERB? LOOK>>
           ;"Since the bridge isn't actually in the room, describe it here"
           <DESCRIBE-RICKETY-BRIDGE>)>>

<ROUTINE CROSS-RICKETY-BRIDGE ()
    <COND (<OR ,TROLL-CAUGHT-TREASURE <IN? ,TROLL <>>>
           <SETG TROLL-CAUGHT-TREASURE <>>
           <COND (,BEAR-FOLLOWING
                  <REMOVE ,BEAR>
                  <FCLEAR ,WRECKAGE ,INVISIBLE>
                  <FSET ,RICKETY-BRIDGE ,INVISIBLE>
                  <DEQUEUE I-BEAR>
                  <JIGS-UP "Just as you reach the other side, the bridge buckles
beneath the weight of the bear, which was still following you around.
You scrabble desperately for support, but as the bridge collapses you stumble back
and fall into the chasm.">)>
           <COND (<=? ,HERE ,ON-SW-SIDE-OF-CHASM> ,ON-NE-SIDE-OF-CHASM)
                 (ELSE ,ON-SW-SIDE-OF-CHASM)>)
          (<IN? ,TROLL ,HERE> <TELL "The troll refuses to let you cross." CR>)
          (ELSE
           <MOVE ,TROLL ,HERE>
           <TELL "The troll steps out from beneath the bridge and blocks your way." CR>)>>

<OBJECT RICKETY-BRIDGE
    (DESC "rickety bridge")
    (IN LOCAL-GLOBALS)
    (SYNONYM BRIDGE)
    (ADJECTIVE RICKETY UNSTABLE WOBBLY ROPE)
    (ACTION RICKETY-BRIDGE-F)
    (FLAGS DOORBIT OPENBIT)>

<ROUTINE DESCRIBE-RICKETY-BRIDGE ()
    <COND (<NOT <FSET? ,RICKETY-BRIDGE ,INVISIBLE>>
           <TELL CR "A rickety bridge extends across the chasm, vanishing into the mist.||
A sign posted on the bridge reads, \"Stop! Pay troll!\"" CR>
           <COND (<NOT <IN? ,TROLL ,HERE>>
                  <TELL "The troll is nowhere to be seen." CR>)>)
          (ELSE
           <TELL "The wreckage of the troll bridge (and a dead bear)
can be seen at the bottom of the chasm." CR>)>>
 
<ROUTINE RICKETY-BRIDGE-F ()
    <COND (<VERB? ENTER>
           <PERFORM
               ,V?WALK
               <COND (<=? ,HERE ,ON-SW-SIDE-OF-CHASM> ,P?NE) (ELSE ,P?SW)>>
           <RTRUE>)>>

<OBJECT TROLL
    (DESC "burly troll")
    (IN RICKETY-BRIDGE)
    (FDESC "A burly troll stands by the bridge and insists you throw him
a treasure before you may cross.")
    (TEXT "Trolls are close relatives with rocks and have skin as tough
as that of a rhinoceros.")
    (ACTION TROLL-F)
    (FLAGS PERSONBIT)>

<GLOBAL TROLL-CAUGHT-TREASURE <>>

<ROUTINE TROLL-F ()
    <COND (<VERB? ATTACK>
           <TELL "The troll laughs aloud at your pitiful attempt to injure him." CR>)
          (<VERB? THROW-AT GIVE>
           <COND (<FSET? ,PRSO ,TREASUREBIT>
                  <REMOVE ,PRSO>
                  <MOVE ,TROLL ,RICKETY-BRIDGE>
                  <SETG TROLL-CAUGHT-TREASURE T>
                  <SETG SCORE <- ,SCORE 5>>
                  <TELL "The troll catches your treasure and scurries away out of sight." CR>)
                 (<PRSO? ,TASTY-FOOD>
                  <TELL "Gluttony is not one of the troll's vices. Avarice, however, is." CR>)
                 (ELSE
                  <TELL "The troll deftly catches " T ,PRSO ", examines it carefully, and
tosses it back, declaring, \"Good workmanship, but it's not valuable enough.\"" CR>)>)
          ;"TODO: TELL, ASK, ANSWER">>

<OBJECT WRECKAGE
    (DESC "wreckage of bridge")
    (IN LOCAL-GLOBALS)
    (ACTION WRECKAGE-F)>

<ROUTINE WRECKAGE-F ()
    <TELL "The wreckage is too far below." CR>>

;----------------------------------------------------------------------

<ROOM ON-NE-SIDE-OF-CHASM
    (DESC "On NE Side of Chasm")
    (IN ROOMS)
    (LDESC "You are on the far side of the chasm.
A northeast path leads away from the chasm on this side.")
    (GLOBAL RICKETY-BRIDGE)
    (SW PER CROSS-RICKETY-BRIDGE)
    (NE TO IN-CORRIDOR)
    (ACTION ON-NE-SIDE-OF-CHASM-F)
    (FLAGS SACREDBIT)>

<ROUTINE ON-NE-SIDE-OF-CHASM-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? JUMP>>
           <COND (<NOT <FSET? ,RICKETY-BRIDGE ,INVISIBLE>>
                  <TELL ,USE-THE-BRIDGE CR>)
                 (ELSE <JIGS-UP "You didn't make it.">)>)
          (<AND <=? .RARG ,M-END> <VERB? LOOK>>
           ;"Since the bridge isn't actually in the room, describe it here"
           <DESCRIBE-RICKETY-BRIDGE>)>>

<ROOM IN-CORRIDOR
    (DESC "In Corridor")
    (IN ROOMS)
    (LDESC "You're in a long east/west corridor.
A faint rumbling noise can be heard in the distance.")
    (WEST TO ON-NE-SIDE-OF-CHASM)
    (EAST TO AT-FORK-IN-PATH)
    (FLAGS SACREDBIT)>

;----------------------------------------------------------------------
"The Volcano"
;----------------------------------------------------------------------

<ROOM AT-FORK-IN-PATH
    (DESC "At Fork in Path")
    (IN ROOMS)
    (LDESC "The path forks here. The left fork leads northeast.
A dull rumbling seems to get louder in that direction.
The right fork leads southeast down a gentle slope.
The main corridor enters from the west.")
    (WEST TO IN-CORRIDOR)
    (NE TO AT-JUNCTION-WITH-WARM-WALLS)
    (SE TO IN-LIMESTONE-PASSAGE)
    (DOWN TO IN-LIMESTONE-PASSAGE)
    (FLAGS SACREDBIT)>

;----------------------------------------------------------------------

<ROOM AT-JUNCTION-WITH-WARM-WALLS
    (DESC "At Junction With Warm Walls")
    (IN ROOMS)
    (LDESC "The walls are quite warm here.
From the north can be heard a steady roar,
so loud that the entire cave seems to be trembling.
Another passage leads south, and a low crawl goes east.")
    (SOUTH TO AT-FORK-IN-PATH)
    (NORTH TO AT-BREATH-TAKING-VIEW)
    (EAST TO IN-CHAMBER-OF-BOULDERS)
    (FLAGS SACREDBIT)>

;----------------------------------------------------------------------

<ROOM AT-BREATH-TAKING-VIEW
    (DESC "At Breath-Taking View")
    (IN ROOMS)
    (LDESC "You are on the edge of a breath-taking view.
Far below you is an active volcano, from which great gouts of molten lava come surging out,
cascading back down into the depths.
The glowing rock fills the farthest reaches of the cavern with a blood-red glare,
giving everything an eerie, macabre appearance.
The air is filled with flickering sparks of ash and a heavy smell of brimstone.
The walls are hot to the touch,
and the thundering of the volcano drowns out all other sounds.
Embedded in the jagged roof far overhead
are myriad twisted formations composed of pure white alabaster,
which scatter the murky light into sinister apparitions upon the walls.
To one side is a deep gorge, filled with a bizarre chaos of tortured rock
which seems to have been crafted by the devil himself.
An immense river of fire crashes out from the depths of the volcano,
burns its way through the gorge, and plummets into a bottomless pit far off to your left.
To the right, an immense geyser of blistering steam erupts continuously
from a barren island in the center of a sulfurous lake, which bubbles ominously.
The far right wall is aflame with an incandescence of its own,
which lends an additional infernal splendor to the already hellish scene.
A dark, forboding passage exits to the south.")
    (SOUTH TO AT-JUNCTION-WITH-WARM-WALLS)
    (OUT TO AT-JUNCTION-WITH-WARM-WALLS)
    (DOWN SORRY "Don't be ridiculous!")
    (ACTION AT-BREATH-TAKING-VIEW-F)
    (FLAGS LIGHTBIT)>

<ROUTINE AT-BREATH-TAKING-VIEW-F (RARG)
    <COND (<AND <=? .RARG ,M-BEG> <VERB? JUMP>>
           <PERFORM ,V?WALK ,P?DOWN>
           <RTRUE>)>>

<OBJECT ACTIVE-VOLCANO
    (DESC "active volcano")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM VOLCANO ROCK)
    (ADJECTIVE ACTIVE GLOWING BLOOD BLOOD-RED RED EERIE MACABRE)
    (TEXT "Great gouts of molten lava come surging out of the volcano
and go cascading back down into the depths.
The glowing rock fills the farthest reaches of the cavern with a blood-red glare,
giving everything an eerie, macabre appearance.")
    (FLAGS NDESCBIT VOWELBIT)>

<OBJECT SPARKS-OF-ASH
    (DESC "sparks of ash")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM SPARK SPARKS ASH AIR)
    (ADJECTIVE FLICKERING)
    (TEXT "The sparks are too far away for you to get a good look at them.")
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

<OBJECT JAGGED-ROOF
    (DESC "jagged roof")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM ROOF FORMATIONS LIGHT APPARITIONS)
    (ADJECTIVE JAGGED TWISTED MURKY SINISTER)
    (TEXT "Embedded in the jagged roof far overhead are myriad twisted formations
composed of pure white alabaster,
which scatter the murky light into sinister apparitions upon the walls.")
    (FLAGS NDESCBIT)>

<OBJECT DEEP-GORGE
    (DESC "deep gorge")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM GORGE CHAOS ROCK)
    (ADJECTIVE DEEP BIZARRE TORTURED)
    (TEXT "The gorge is filled with a bizarre chaos of tortured rock
which seems to have been crafted by the devil himself.")
    (FLAGS NDESCBIT)>

<OBJECT RIVER-OF-FIRE
    (DESC "river of fire")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM RIVER FIRE DEPTH PIT)
    (ADJECTIVE FIERY BOTTOMLESS)
    (TEXT "The river of fire crashes out from the depths of the volcano,
burns its way through the gorge, and plummets into a bottomless pit far off to your left.")
    (FLAGS NDESCBIT)>

<OBJECT IMMENSE-GEYSER
    (DESC "immense geyser")
    (IN AT-BREATH-TAKING-VIEW)
    (SYNONYM GEYSER STEAM ISLAND LAKE)
    (ADJECTIVE IMMENSE BLISTERING BARREN SULFUROUS BUBBLING)
    (TEXT "The geyser of blistering steam erupts continuously from a barren island
in the center of a sulfurous lake, which bubbles ominously.")
    (FLAGS NDESCBIT VOWELBIT)>

;----------------------------------------------------------------------

<ROOM IN-CHAMBER-OF-BOULDERS
    (DESC "In Chamber of Boulders")
    (IN ROOMS)
    (LDESC "You are in a small chamber filled with large boulders.
The walls are very warm, causing the air in the room to be almost stifling from the heat.
The only exit is a crawl heading west, through which is coming a low rumbling.")
    (WEST TO AT-JUNCTION-WITH-WARM-WALLS)
    (OUT TO AT-JUNCTION-WITH-WARM-WALLS)
    (FLAGS SACREDBIT)>

<OBJECT WARM-BOULDERS
    (DESC "boulders")
    (IN IN-CHAMBER-OF-BOULDERS)
    (TEXT "They're just ordinary boulders. They're warm.")
    (ACTION WARM-BOULDERS-F)
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE WARM-BOULDERS-F ()
    <COND (<VERB? LOOK-UNDER PUSH PULL>
           <TELL "You'd have to blast them aside." CR>)>>

<OBJECT RARE-SPICES
    (DESC "rare spices")
    (SYNONYM SPICES SPICE)
    (ADJECTIVE RARE EXOTIC)
    (ARTICLE "a selection of")
    (ACTION RARE-SPICES-F)
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TREASUREBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE RARE-SPICES-F ()
    <COND (<VERB? SMELL EXAMINE>
           <TELL "They smell wonderfully exotic!" CR>)>>

;----------------------------------------------------------------------

<ROOM IN-LIMESTONE-PASSAGE
    (DESC "In Limestone Passage")
    (IN ROOMS)
    (LDESC "You are walking along a gently sloping north/south passage
lined with oddly shaped limestone formations.")
    (NORTH TO AT-FORK-IN-PATH)
    (UP TO AT-FORK-IN-PATH)
    (SOUTH TO IN-FRONT-OF-BARREN-ROOM)
    (DOWN TO IN-FRONT-OF-BARREN-ROOM)
    (FLAGS SACREDBIT)>

<OBJECT LIMESTONE-FORMATIONS
    (DESC "limestone formations")
    (SYNONYM FORMATIONS SHAPE SHAPES LIMESTONE)
    (ADJECTIVE LIME LIMESTONE STONE ODDLY SHAPED ODDLY-SHAPED)
    (TEXT "Every now and then a particularly strange shape catches your eye.")
    (FLAGS NDESCBIT PLURALBIT MULTITUDEBIT)>

;----------------------------------------------------------------------
"If you go down to the woods today..."
;----------------------------------------------------------------------

<ROOM IN-FRONT-OF-BARREN-ROOM
    (DESC "In Front of Barren Room")
    (IN ROOMS)
    (LDESC "You are standing at the entrance to a large, barren room.
A sign posted above the entrance reads: \"Caution! Bear in room!\"")
    (WEST TO IN-LIMESTONE-PASSAGE)
    (UP TO IN-LIMESTONE-PASSAGE)
    (EAST TO IN-BARREN-ROOM)
    (IN TO IN-BARREN-ROOM)
    (FLAGS SACREDBIT)>

<OBJECT CAUTION-SIGN
    (DESC "caution sign")
    (SYNONYM SIGN)
    (ADJECTIVE BARREN ROOM CAUTION)
    (TEXT "The sign reads, \"Caution! Bear in room!\"")
    (FLAGS NDESCBIT)>

;----------------------------------------------------------------------

<ROOM IN-BARREN-ROOM
    (DESC "In Barren Room")
    (IN ROOMS)
    (LDESC "You are inside a barren room.
The center of the room is completely empty except for some dust.
Marks in the dust lead away toward the far end of the room.
The only exit is the way you came in.")
    (WEST TO IN-FRONT-OF-BARREN-ROOM)
    (OUT TO IN-FRONT-OF-BARREN-ROOM)
    (FLAGS SACREDBIT)>

<OBJECT DUST
    (DESC "dust")
    (IN IN-BARREN-ROOM)
    (SYNONYM DUST MARKS)
    (TEXT "It just looks like ordinary dust.")
    (FLAGS NDESCBIT)>

<GLOBAL BEAR-FOLLOWING <>>
<GLOBAL BEAR-FRIENDLY <>>
<GLOBAL AXE-NEAR-BEAR <>>

<OBJECT BEAR
    (DESC "large cave bear")
    (IN IN-BARREN-ROOM)
    (SYNONYM BEAR)
    (ADJECTIVE LARGE TAME FEROCIOUS CAVE)
    (DESCFCN BEAR-DESCFCN)
    (ACTION BEAR-F)
    (FLAGS PERSONBIT)>

<ROUTINE BEAR-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)
          (,BEAR-FOLLOWING
           <TELL "You are being followed by a very large, tame bear." CR>)
          (<NOT ,BEAR-FRIENDLY>
           <TELL "There is a ferocious cave bear eyeing you from the far end of the room!" CR>)
          (<=? ,HERE ,IN-BARREN-ROOM>
           <TELL "There is a gentle cave bear sitting placidly in one corner." CR>)
          (ELSE
           <TELL "There is a contented-looking bear wandering about nearby." CR>)>>

<CONSTANT BEAR-IS-YOUR-FRIEND "The bear is confused; he only wants to be your friend.">

<ROUTINE BEAR-F ()
    <COND (<VERB? ATTACK>
           <COND (<HELD? ,AXE> <PERFORM ,V?THROW-AT ,AXE ,PRSO> <RTRUE>)
                 (,BEAR-FRIENDLY <TELL ,BEAR-IS-YOUR-FRIEND CR>)
                 (ELSE <TELL "With what? Your bare hands? Against *his* bear hands??" CR>)>)
          (<AND <VERB? THROW-AT> <PRSI? ,BEAR>>
           <COND (<NOT <PRSO? ,AXE>> <PERFORM ,V?GIVE ,PRSO ,PRSI> <RTRUE>)
                 (,BEAR-FRIENDLY <TELL ,BEAR-IS-YOUR-FRIEND CR>)
                 (ELSE
                  <MOVE ,AXE ,HERE>
                  <SETG AXE-NEAR-BEAR T>
                  <TELL "The axe misses and lands near the bear where you can't get at it." CR>)>)
          (<AND <VERB? GIVE> <PRSI? ,BEAR>>
           <COND (<PRSO? ,TASTY-FOOD>
                  <SETG AXE-NEAR-BEAR <>>
                  <REMOVE ,PRSO>
                  <SETG BEAR-FRIENDLY T>
                  <TELL "The bear eagerly wolfs down your food, after which he seems
to calm down considerably and even becomes rather friendly." CR>)
                 (,BEAR-FRIENDLY <TELL "The bear doesn't seem very interested in your offer." CR>)
                 (ELSE <TELL "Uh-oh -- your offer only makes the bear angrier!" CR>)>)
          ;"TODO: TELL, ASK, ANSWER"
          (<VERB? EXAMINE>
           <TELL "The bear is extremely large, ">
           <COND (,BEAR-FRIENDLY <TELL "but appears to be friendly." CR>)
                 (ELSE <TELL "and seems quite ferocious!" CR>)>)
          (<VERB? TAKE CATCH>
           <COND (<NOT ,BEAR-FRIENDLY> <TELL "Surely you're joking!" CR>)
                 (<FSET? ,GOLDEN-CHAIN ,LOCKEDBIT>
                  <TELL "The bear is still chained to the wall." CR>)
                 (ELSE
                  <SETG BEAR-FOLLOWING T>
                  <QUEUE I-BEAR -1>
                  <TELL "Ok, the bear's now following you around." CR>)>)
          (<VERB? DROP RELEASE>
           <COND (<NOT ,BEAR-FOLLOWING> <TELL "What?" CR>)
                 (ELSE
                  <SETG BEAR-FOLLOWING <>>
                  <DEQUEUE I-BEAR>
                  <COND (<IN? ,TROLL ,HERE>
                         <REMOVE ,TROLL>
                         <TELL "The bear lumbers toward the troll,
who lets out a startled shriek and scurries away.
The bear soon gives up the pursuit and wanders back." CR>)
                        (ELSE <TELL "The bear wanders away from you." CR>)>)>)>>

<ROUTINE I-BEAR ()
    <COND (<NOT ,HERE-LIT> <RFALSE>)>
    <COND (<IN? ,BEAR ,HERE>
           <COND (<=? ,HERE ,AT-BREATH-TAKING-VIEW>
                  <TELL CR "The bear roars with delight." CR>)>)
          (ELSE
           <MOVE ,BEAR ,HERE>
           <TELL CR "The bear lumbers along behind you." CR>)>>

<OBJECT GOLDEN-CHAIN
    (DESC "golden chain")
    (IN IN-BARREN-ROOM)
    (SYNONYM CHAIN LINKS SHACKLES CHAINS)
    (ADJECTIVE SOLID GOLD GOLDEN THICK)
    (TEXT "The chain has thick links of solid gold!")
    (DESCFCN GOLDEN-CHAIN-DESCFCN)
    (ACTION GOLDEN-CHAIN-F)
    (DEPOSIT-POINTS 14)
    (FLAGS TAKEBIT TRYTAKEBIT LOCKEDBIT)>

<ROUTINE GOLDEN-CHAIN-DESCFCN (ARG)
    <COND (<=? .ARG ,M-OBJDESC?> <RTRUE>)
          (<FSET? ,GOLDEN-CHAIN ,LOCKEDBIT>
           <TELL "The bear is held back by a solid gold chain.">)
          (ELSE <TELL "A solid golden chain lies in coils on the ground!">)>>

<ROUTINE GOLDEN-CHAIN-F ()
    <COND (<VERB? TAKE>
           <COND (<FSET? ,GOLDEN-CHAIN ,LOCKEDBIT>
                  <TELL "It's locked to the ">
                  <COND (,BEAR-FRIENDLY
                         <TELL "friendly bear." CR>)
                        (<TELL "ferocious bear!" CR>)>)>)
          (<AND <VERB? UNLOCK> <PRSO? ,GOLDEN-CHAIN>>
           <COND (<NOT ,BEAR-FRIENDLY>
                  <TELL "There is no way to get past the bear to unlock the chain,
which is probably just as well." CR>)
                 (<NOT <PRSI? ,SET-OF-KEYS>>
                  <TELL CT ,PRSI " won't fit the lock." CR>)
                 (<NOT <FSET? ,PRSO ,LOCKEDBIT>>
                  <TELL "It's already unlocked." CR>)
                 (ELSE
                  <FCLEAR ,PRSO ,LOCKEDBIT>
                  <TELL "Unlocked." CR>)>)
          (<AND <VERB? LOCK> <PRSO? ,GOLDEN-CHAIN>>
           <COND (<FSET? ,PRSO ,LOCKEDBIT>
                  <TELL "It's already locked." CR>)
                 (ELSE <TELL "The mechanism won't lock again." CR>)>)>>

;----------------------------------------------------------------------
"The Different Maze"
;----------------------------------------------------------------------

;"Define a function to save a bit of tedium. Like MAZE-ROOM, this lets us
  specify connections within the maze by number instead of typing out
  the room name, and omit TO. It also builds the room description."
<DEFINE DIFFMAZE-ROOM (NAME DESC "ARGS" CS "AUX" DEF PS)
    ;"Convert list of map connections to property definitions.
      (NORTH FOO-ROOM) -> (NORTH TO FOO-ROOM)
      (NORTH 5)        -> (NORTH TO DIFFERENT-MAZE-5)"
    <SET PS
        <MAPF ,LIST
              <FUNCTION (C "AUX" (DIR <1 .C>) (DEST <2 .C>))
                  <COND (<TYPE? .DEST FIX>
                         <SET DEST <PARSE <STRING "DIFFERENT-MAZE-" <UNPARSE .DEST>>>>)>
                  <LIST .DIR TO .DEST>>
              .CS>>
    ;"Build room definition and evaluate it"
    <SET DEF
        <FORM ROOM .NAME
            '(DESC "Maze")
            '(IN ROOMS)
            <LIST LDESC <STRING "You are in a " .DESC ", all different.">>
            !.PS>>
    <EVAL .DEF>>

<DIFFMAZE-ROOM DIFFERENT-MAZE-1 "maze of twisty little passages"
    (SOUTH 3) (SW 4) (NE 5) (SE 6) (UP 7)
    (NW 8) (EAST 9) (WEST 10) (NORTH 11) (DOWN AT-WEST-END-OF-LONG-HALL)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-2 "little maze of twisting passages"
    (SW 3) (NORTH 4) (EAST 5) (NW 6) (SE 7)
    (NE 8) (WEST 9) (DOWN 10) (UP 11) (SOUTH DEAD-END-14)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-3 "maze of twisting little passages"
    (WEST 1) (SE 4) (NW 5) (SW 6) (NE 7)
    (UP 8) (DOWN 9) (NORTH 10) (SOUTH 11) (EAST 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-4 "little maze of twisty passages"
    (NW 1) (UP 3) (NORTH 5) (SOUTH 6) (WEST 7)
    (SW 8) (NE 9) (EAST 10) (DOWN 11) (SE 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-5 "twisting maze of little passages"
    (UP 1) (DOWN 3) (WEST 4) (NE 6) (SW 7)
    (EAST 8) (NORTH 9) (NW 10) (SE 11) (SOUTH 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-6 "twisting little maze of passages"
    (NE 1) (NORTH 3) (NW 4) (SE 5) (EAST 7)
    (DOWN 8) (SOUTH 9) (UP 10) (WEST 11) (SW 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-7 "twisty little maze of passages"
    (NORTH 1) (SE 3) (DOWN 4) (SOUTH 5) (EAST 6)
    (WEST 8) (SW 9) (NE 10) (NW 11) (UP 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-8 "twisty maze of little passages"
    (EAST 1) (WEST 3) (UP 4) (SW 5) (DOWN 6)
    (SOUTH 7) (NW 9) (SE 10) (NE 11) (NORTH 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-9 "little twisty maze of passages"
    (SE 1) (NE 3) (SOUTH 4) (DOWN 5) (UP 6)
    (NW 7) (NORTH 8) (SW 10) (EAST 11) (WEST 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-10 "maze of little twisting passages"
    (DOWN 1) (EAST 3) (NE 4) (UP 5) (WEST 6)
    (NORTH 7) (SOUTH 8) (SE 9) (SW 11) (NW 2)>

<DIFFMAZE-ROOM DIFFERENT-MAZE-11 "maze of little twisty passages"
    (SW 1) (NW 3) (EAST 4) (WEST 5) (NORTH 6)
    (DOWN 7) (SE 8) (UP 9) (SOUTH 10) (NE 2)>

;----------------------------------------------------------------------

<ROOM DEAD-END-14
    (DESC "Dead End, near Vending Machine")
    (IN ROOMS)
    (LDESC "You have reached a dead end. There is a massive vending machine here.||
Hmmm... There is a message here scrawled in the dust in a flowery script.")
    (NORTH TO DIFFERENT-MAZE-2)
    (OUT TO DIFFERENT-MAZE-3)
    (FLAGS SACREDBIT)>

<OBJECT MESSAGE-IN-DUST
    (DESC "message in the dust")
    (IN DEAD-END-14)
    (SYNONYM MESSAGE SCRAWL WRITING SCRIPT)
    (ADJECTIVE FLOWERY
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP SCRAWL) (ELSE SCRAWLED)>)
    (TEXT "The message reads, \"This is not the maze where the pirate leaves
his treasure chest.\"")
    (FLAGS NDESCBIT)>

<OBJECT VENDING-MACHINE
    (DESC "vending machine")
    (IN DEAD-END-14)
    (SYNONYM MACHINE SLOT)
    (ADJECTIVE VENDING MASSIVE COIN
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP BATTERIES) (ELSE BATTERY)>)
    (TEXT "The instructions on the vending machine read,
\"Insert coins to receive fresh batteries.\"")
    (ACTION VENDING-MACHINE-F)
    (FLAGS NDESCBIT)>

<ROUTINE VENDING-MACHINE-F ()
    <COND (<AND <VERB? PUT-IN> <PRSI? ,VENDING-MACHINE>>
           <COND (<PRSO? ,RARE-COINS>
                  <MOVE ,FRESH-BATTERIES ,HERE>
                  <REMOVE ,RARE-COINS>
                  <TELL "Soon after you insert the coins in the coin slot, "
T ,VENDING-MACHINE " makes a grinding sound, and a set of fresh batteries
falls at your feet." CR>)
                 (ELSE <TELL "The machine seems to be designed to take coins." CR>)>)
          (<VERB? ATTACK>
           <TELL "The machine is quite sturdy and survives your attack
without getting so much as a scratch." CR>)
          (<VERB? LOOK-UNDER>
           <TELL "You don't find anything under the machine." CR>)
          (<VERB? SEARCH>
           <TELL "You can't get inside the machine." CR>)
          (<VERB? TAKE>
           <TELL CT ,VENDING-MACHINE " is far too heavy to move." CR>)>>

<GLOBAL FRESH-BATTERIES-USED <>>

<OBJECT FRESH-BATTERIES
    (DESC "fresh batteries")
    (IN VENDING-MACHINE)
    ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
    %<VERSION? (ZIP '(SYNONYM BATTERIES))
               (ELSE '(SYNONYM BATTERIES BATTERY))>
    (ADJECTIVE FRESH)
    (FDESC "There are fresh batteries here.")
    (TEXT "They look like ordinary batteries. (A sepulchral voice says, \"Still going!\")")
    (ACTION COUNTABLE-BATTERIES-F)
    (FLAGS TAKEBIT)>

<ROUTINE COUNTABLE-BATTERIES-F ()
    <COND (<VERB? COUNT> <TELL "A pair." CR>)>>

<OBJECT OLD-BATTERIES
    (DESC "worn-out batteries")
    ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
    %<VERSION? (ZIP '(SYNONYM BATTERIES))
               (ELSE '(SYNONYM BATTERIES BATTERY))>
    (ADJECTIVE WORN OUT WORN-OUT)
    (FDESC "Some worn-out batteries have been discarded nearby.")
    (TEXT "They look like ordinary batteries.")
    (ACTION COUNTABLE-BATTERIES-F)
    (FLAGS TAKEBIT)>

;----------------------------------------------------------------------
"Dwarves!"
;----------------------------------------------------------------------

<GLOBAL DWARVES-REMAINING 5>

<OBJECT DWARF
    (DESC "threatening little dwarf")
    (SYNONYM DWARF)
    (ADJECTIVE THREATENING NASTY LITTLE MEAN)
    (TEXT "It's probably not a good idea to get too close.
Suffice it to say the little guy's pretty aggressive.")
    (FDESC "A threatening little dwarf hides in the shadows.")
    (ACTION DWARF-F)
    (FLAGS PERSONBIT)>

<ROUTINE DWARF-F ()
    <COND (<VERB? KICK>
           <TELL "You boot the dwarf across the room. He curses, the gets up and
brushes himself off. Now he's madder than ever!" CR>)
          (<AND <VERB? THROW-AT> <PRSI? ,DWARF>>
           <COND (<PRSO? ,AXE>
                  <COND (<N=? <RANDOM 3> 1>
                         <REMOVE ,PRSI>
                         <MOVE ,AXE ,HERE>
                         <DEC DWARVES-REMAINING>
                         <TELL "You killed a little dwarf! The body vanishes in a cloud of greasy black smoke." CR>)
                        (ELSE
                         <MOVE ,AXE ,HERE>
                         <TELL "Missed! The little dwarf dodges out of the way of the axe." CR>)>)
                 (ELSE <PERFORM ,V?GIVE ,PRSO ,PRSI> <RTRUE>)>)
          (<AND <VERB? GIVE> <PRSI? ,DWARF>>
           <COND (<PRSO? ,TASTY-FOOD>
                  <TELL "You fool, dwarves eat only coal! Now you've made him *really* mad!" CR>)
                 (ELSE
                  <TELL "The dwarf is not all interested in your offer.
(The reason being, perhaps, that if he kills you he gets everything you have anyway.)" CR>)>)
          (<VERB? ATTACK> <TELL "Not with your bare hands. No way." CR>)>>

<GLOBAL DWARF-THREW-AXE <>>

<ROUTINE I-DWARF ()
    <COND (<NOT ,HERE-LIT> <RFALSE>)
          (<0? ,DWARVES-REMAINING> <DEQUEUE I-DWARF> <RFALSE>)
          (<IN? ,DWARF <>>
           <COND (<OR <FSET? ,HERE ,SACREDBIT> <FSET? ,HERE ,LIGHTBIT>> <RFALSE>)
                 (<PROB ,DWARVES-REMAINING>
                  <COND (<OR <IN? ,BEAR ,HERE> <IN? ,TROLL ,HERE>> <RFALSE>)>
                  <CRLF>
                  <COND (<IN? ,DRAGON ,HERE>
                         <DEC DWARVES-REMAINING>
                         <TELL "A dwarf appears, but with one casual blast the dragon vaporizes him!" CR>)
                        (ELSE
                         <MOVE ,DWARF ,HERE>
                         <TELL CA ,DWARF " comes out of the shadows!" CR>)>)>)
          (<NOT <IN? ,DWARF ,HERE>>
           <COND (<NOT ,HERE-LIT> <RFALSE>)
                 (<OR <FSET? ,HERE ,SACREDBIT> <FSET? ,HERE ,LIGHTBIT>> <RFALSE>)
                 (<AND <PROB 96> <NOT <IN? ,DWARF ,IN-MIRROR-CANYON>>>
                  <MOVE ,DWARF ,HERE>
                  <TELL CR "The dwarf stalks after you..." CR>)
                 (ELSE <REMOVE ,DWARF> <RFALSE>)>)
          (<PROB 75>
           <CRLF>
           <COND (<NOT ,DWARF-THREW-AXE>
                  <MOVE ,AXE ,HERE>
                  <SETG DWARF-THREW-AXE T>
                  <REMOVE ,DWARF>
                  <TELL "The dwarf throws a nasty little axe at you, misses, curses, and runs away." CR>)
                 (<=? ,HERE ,IN-MIRROR-CANYON>
                  <TELL "The dwarf admires himself in the mirror." CR>)
                 (ELSE
                  <TELL "The dwarf throws a nasty little knife at you, ">
                  <COND (<L=? <RANDOM 1000> 95> <JIGS-UP "and hits!">)
                        (ELSE <TELL "but misses!" CR>)>)>)
          (<=? <RANDOM 3> 1>
           <REMOVE ,DWARF>
           <TELL CR "Tiring of this, the dwarf slips away." CR>)>>

<OBJECT AXE
    (DESC "dwarvish axe")
    (SYNONYM AXE)
    (ADJECTIVE LITTLE DWARVISH
        ;"BUGFIX: Work around dictionary collision in ZILF 0.6"
        %<VERSION? (ZIP DWARVES) (ELSE DWARVEN)>)
    (FDESC "There is a little axe here.")
    (TEXT "It's just a little axe.")
    (ACTION AXE-F)
    (FLAGS TAKEBIT TRYTAKEBIT)>

<ROUTINE AXE-F ()
    <COND (,AXE-NEAR-BEAR
           <COND (<VERB? EXAMINE>
                  <TELL "It's lying beside the bear." CR>)
                 (<VERB? TAKE>
                  <TELL "No chance. It's lying beside the ferocious bear, quite within harm's way." CR>)>)>>

;----------------------------------------------------------------------
"Two brushes with piracy"
;----------------------------------------------------------------------

<GLOBAL PIRATE-STOLE <>>
<GLOBAL PIRATE-SPOTTED <>>

;"Filled in by the compiler"
<CONSTANT LAST-OBJECT <>>

<ROUTINE I-PIRATE ("AUX" BOOTY)
    ;"Pirate has a 2% chance of appearing, but won't appear in sacred rooms or
      near the dwarf"
    <COND (<OR <PROB 98>
               <NOT ,HERE-LIT>
               <=? ,HERE ,IN-SECRET-CANYON>
               <FSET? ,HERE ,LIGHTBIT>
               <FSET? ,HERE ,SACREDBIT>>
           <RFALSE>)
          (<IN? ,DWARF ,HERE>
           <TELL CR "A bearded pirate appears, catches sight of the dwarf and runs away." CR>
           <RTRUE>)>
    ;"Look for treasure nearby"
    <MAP-SCOPE (I) (LOCATION INVENTORY)
        <COND (<FSET? .I ,TREASUREBIT>
               <SET BOOTY .I>
               <RETURN>)>>
    <COND (<NOT .BOOTY>
           <COND (,PIRATE-SPOTTED <RFALSE>)>
           <SETG PIRATE-SPOTTED T>
           <COND (,PIRATE-STOLE <DEQUEUE I-PIRATE>)>
           <TELL CR "There are faint rustling noises from the darkness behind you.
As you turn toward them, you spot a bearded pirate.
He is carrying a large chest.||
\"Shiver me timbers!\" he cries, \"I've been spotted!
I'd best hie meself off to the maze to hide me chest!\"||
With that, he vanishes into the gloom." CR>
           <RTRUE>)>
    ;"Steal treasure"
    <COND (,PIRATE-STOLE <RFALSE>)>
    <SETG PIRATE-STOLE T>
    <COND (,PIRATE-SPOTTED <DEQUEUE I-PIRATE>)>
    ;"We can't move objects in a MAP-SCOPE, so use recursion in a separate routine"
    <NESTED-ROB-TREASURE ,HERE ,DEAD-END-13>
    <NESTED-ROB-TREASURE ,WINNER ,DEAD-END-13 -5>
    <TELL CR "Out from the shadows behind you pounces a bearded pirate!
\"Har, har,\" he chortles. \"I'll just take all this booty and hide it away
with me chest deep in the maze!\"
He snatches your treasure and vanishes into the gloom." CR>>

<ROUTINE NESTED-ROB-TREASURE (SRC DEST "OPT" (DSCORE 0))
    <MAP-CONTENTS (I N .SRC)
        <COND (<FIRST? .I> <NESTED-ROB-TREASURE .I .DEST .DSCORE>)>
        <COND (<FSET? .I ,TREASUREBIT>
               <MOVE .I .DEST>
               <SETG SCORE <+ ,SCORE .DSCORE>>)>>>

;----------------------------------------------------------------------
"The cave is closing now..."
;----------------------------------------------------------------------

<GLOBAL CAVES-CLOSED <>>

<ROUTINE I-CAVE-CLOSER ()
    <COND (<L? ,TREASURES-FOUND ,MAX-TREASURES> <RFALSE>)>
    <DEQUEUE I-CAVE-CLOSER>
    <SETG CAVES-CLOSED T>
    <SETG SCORE <+ ,SCORE 25>>
    <FSET CRYSTAL-BRIDGE ,INVISIBLE>
    <REMOVE ,SET-OF-KEYS>
    <DEQUEUE I-DWARF>
    <DEQUEUE I-PIRATE>
    <REMOVE ,TROLL>
    <REMOVE ,BEAR>
    <REMOVE ,DRAGON>
    <QUEUE I-ENDGAME 26>    ;"26 = 25 turns after this one"
    <TELL CR "A sepulchral voice reverberating through the cave says,
\"Cave closing soon. All adventurers exit immediately through main office.\"" CR>>

<ROUTINE I-ENDGAME ("AUX" F)
    <DEQUEUE I-ENDGAME>
    <SETG SCORE <+ ,SCORE 10>>
    <MAP-CONTENTS (I N ,WINNER) <REMOVE .I>>
    <MOVE ,BOTTLE ,AT-NE-END>
    <COND (<SET F <FIRST? ,BOTTLE>> <REMOVE .F>)>
    <MOVE ,GIANT-BIVALVE ,AT-NE-END>
    <MOVE ,BRASS-LANTERN ,AT-NE-END>
    <MOVE ,BLACK-ROD ,AT-NE-END>
    <MOVE ,LITTLE-BIRD ,AT-SW-END>
    <MOVE ,VELVET-PILLOW ,AT-SW-END>
    <TELL CR "The sepulchral voice intones, \"The cave is now closed.\"
As the echoes fade, there is a blinding flash of light (and a small
puff of orange smoke). . .||
As your eyes refocus, you look around..." CR>
    <GOTO ,AT-NE-END>
    <RTRUE>>

;----------------------------------------------------------------------
"The End Game"
;----------------------------------------------------------------------

<ROOM AT-NE-END
    (DESC "NE End of Repository")
    (IN ROOMS)
    (LDESC "You are at the northeast end of an immense room, even larger than the giant room.
It appears to be a repository for the \"Adventure\" program.
Massive torches far overhead bathe the room with smoky yellow light.
Scattered about you can be seen a pile of bottles (all of them empty),
a nursery of young beanstalks murmuring quietly, a bed of oysters,
a bundle of black rods with rusty stars on their ends, and a collection of brass lanterns.
Off to one side a great many dwarves are sleeping on the floor, snoring loudly.
A sign nearby reads: \"Do not disturb the dwarves!\"")
    (GLOBAL ENORMOUS-MIRROR)
    (SW TO AT-SW-END)
    (FLAGS LIGHTBIT)>

<OBJECT ENORMOUS-MIRROR
    (DESC "enormous mirror")
    (IN LOCAL-GLOBALS)
    (SYNONYM MIRROR VANITY)
    (ADJECTIVE ENORMOUS HUGE BIG LARGE SUSPENDED HANGING DWARVISH)
    (ACTION ENORMOUS-MIRROR-F)
    (FLAGS VOWELBIT)>

<ROUTINE ENORMOUS-MIRROR-F ()
    <COND (<VERB? ATTACK>
           <TELL "You strike the mirror a resounding blow,
whereupon it shatters into a myriad tiny fragments." CR CR>
           <DWARVES-WAKE-UP>
           <RTRUE>)>>

<OBJECT NE-GAME-MATERIALS
    (DESC "collection of adventure game materials")
    (IN AT-NE-END)
    (SYNONYM MATERIALS TORCHES REPOSITORY OBJECTS
        ;"V3 property size is limited to 8 bytes"
        %<VERSION? (ZIP #SPLICE ())
                   (ELSE #SPLICE (STUFF JUNK))>)
    (ADJECTIVE ADVENTURE MASSIVE SUNDRY)
    (TEXT "You've seen everything in here already, albeit in somewhat different contexts.")
    (ACTION GAME-MATERIALS-F)
    (FLAGS NDESCBIT MULTITUDEBIT)>

<ROUTINE GAME-MATERIALS-F ()
    <COND (<VERB? TAKE>
           <TELL "Realizing that by removing the loot here you'd be ruining the game for future players,
you leave the \"Adventure\" materials where they are." CR>)>>

<OBJECT SLEEPING-DWARVES
    (DESC "sleeping dwarves")
    (IN AT-NE-END)
    (SYNONYM DWARF DWARVES)
    (ADJECTIVE SLEEPING SNORING DOZING SNOOZING)
    (ARTICLE "hundreds of angry")
    (TEXT "I wouldn't bother the dwarves if I were you.")
    (ACTION SLEEPING-DWARVES-F)
    (FLAGS PERSONBIT PLURALBIT MULTITUDEBIT)>

<ROUTINE SLEEPING-DWARVES-F ()
    <COND (<VERB? TAKE> <TELL "What, all of them?" CR>)
          (<VERB? WAKE>
           <TELL "You prod the nearest dwarf, who wakes up grumpily,
takes one look at you, curses, and grabs for his axe." CR CR>
           <DWARVES-WAKE-UP>
           <RTRUE>)
          (<VERB? ATTACK>
           <DWARVES-WAKE-UP>
           <RTRUE>)>>

<ROUTINE DWARVES-WAKE-UP ()
    <JIGS-UP "The resulting ruckus has awakened the dwarves.
There are now dozens of threatening little dwarves in the room with you!
Most of them throw knives at you! All of them get you!">>

;----------------------------------------------------------------------

<ROOM AT-SW-END
    (DESC "SW End of Repository")
    (IN ROOMS)
    (LDESC "You are at the southwest end of the repository.
To one side is a pit full of fierce green snakes.
On the other side is a row of small wicker cages, each of which contains a little sulking bird.
In one corner is a bundle of black rods with rusty marks on their ends.
A large number of velvet pillows are scattered about on the floor.
A vast mirror stretches off to the northeast.
At your feet is a large steel grate, next to which is a sign which reads,
\"TREASURE VAULT. Keys in main office.\"")
    (DOWN TO OUTSIDE-GRATE IF REPOSITORY-GRATE IS OPEN)
    (NE TO AT-NE-END)
    (FLAGS LIGHTBIT)>

<OBJECT REPOSITORY-GRATE
    (DESC "steel grate")
    (IN AT-SW-END)
    (SYNONYM GRATE GRATING)
    (ADJECTIVE ORDINARY STEEL)
    (FDESC "The grate is closed.")
    (TEXT "It just looks like an ordinary steel grate.")
    (ACTION REPOSITORY-GRATE-F)
    (FLAGS DOORBIT LOCKEDBIT OPENABLEBIT)>

;"It can't actually be unlocked."
<ROUTINE REPOSITORY-GRATE-F ()
    <COND (<AND <VERB? LOCK> <PRSO? ,REPOSITORY-GRATE>>
           <TELL "It's already locked." CR>)
          (<AND <VERB? UNLOCK> <PRSO? ,REPOSITORY-GRATE>>
           <TELL CT ,PRSI " won't fit the lock." CR>)>>

<OBJECT SW-GAME-MATERIALS
    (DESC "collection of adventure game materials")
    (IN AT-SW-END)
    (SYNONYM MATERIALS SNAKES REPOSITORY OBJECTS
        ;"V3 property size is limited to 8 bytes"
        %<VERSION? (ZIP #SPLICE ())
                   (ELSE #SPLICE (STUFF JUNK))>)
    (ADJECTIVE ADVENTURE MASSIVE SUNDRY FIERCE GREEN)
    (TEXT "You've seen everything in here already, albeit in somewhat different contexts.")
    (ACTION GAME-MATERIALS-F)
    (FLAGS NDESCBIT MULTITUDEBIT)>

<OBJECT BLACK-MARK-ROD
    (DESC "black rod with a rusty mark on the end")
    (IN AT-SW-END)
    (SYNONYM ROD MARK DYNAMITE EXPLOSIVE ;BLAST)
    (ADJECTIVE BLACK RUSTY THREE FOOT IRON)
    (FDESC "A three foot rod with a rusty mark on one end lies nearby.")
    (TEXT "It's a three foot rod with a rusty mark on an end.")
    (ACTION BLACK-MARK-ROD-F)
    (FLAGS TAKEBIT)>

<ROUTINE BLACK-MARK-ROD-F ()
    <COND (<VERB? WAVE> <TELL "Nothing happens." CR>)>>

;----------------------------------------------------------------------
"Some entry points"
;----------------------------------------------------------------------

;"TODO: print player ranking in V-SCORE"
<ROUTINE PRINT-PLAYER-RANKING ()
    <TELL
        ", earning you the rank of "
        <COND (<G=? ,SCORE 348> "Grandmaster Adventurer!")
              (<G=? ,SCORE 330> "Master, first class.")
              (<G=? ,SCORE 300> "Master, second class.")
              (<G=? ,SCORE 200> "Junior Master.")
              (<G=? ,SCORE 130> "Seasoned Adventurer.")
              (<G=? ,SCORE 100> "Experienced Adventurer.")
              (<G=? ,SCORE 35> "Adventurer.")
              (<G=? ,SCORE 10> "Novice.")
              (ELSE "Amateur.")>
        CR>>

;"TODO: call DARK-TO-DARK when stumbling around"
<GLOBAL DARK-WARNING <>>

<ROUTINE DARK-TO-DARK ()
    <COND (<NOT ,DARK-WARNING>
           <SETG DARK-WARNING T>
           <TELL "It is now pitch dark. If you proceed you will likely fall into a pit." CR>)
          (<PROB 25>
           <JIGS-UP "You fell into a pit and broke every bone in your body!">)>>

;"Unknown verb handler is unnecessary; we don't support teleporting."

;----------------------------------------------------------------------
"Resurrection"
;----------------------------------------------------------------------

;"TODO: replace JIGS-UP for resurrection"

;XXX
<ROUTINE FINISH ()
    <TELL "CONGRATULATION! A WINNER IS YOU" CR>
    <QUIT>>

;----------------------------------------------------------------------
"Grammar extensions"
;----------------------------------------------------------------------

<SYNTAX OFF = V-OFF>
<SYNTAX ON = V-ON>

<ROUTINE V-OFF ()
    <COND (<NOT <HELD? ,BRASS-LANTERN>>
           <TELL "You have no lamp." CR>)
          (ELSE <PERFORM ,V?TURN-OFF ,BRASS-LANTERN>)>>

<ROUTINE V-ON ()
    <COND (<NOT <HELD? ,BRASS-LANTERN>>
           <TELL "You have no lamp." CR>)
          (ELSE <PERFORM ,V?TURN-ON ,BRASS-LANTERN>)>>

;----------------------------------------------------------------------

<SYNTAX CATCH OBJECT (FIND PERSONBIT) = V-CATCH>
<VERB-SYNONYM CATCH CAPTURE>
<SYNTAX RELEASE OBJECT (FIND PERSONBIT) = V-RELEASE>
<VERB-SYNONYM RELEASE FREE>

<ROUTINE V-CATCH ()
    <TELL "You can't catch " T ,PRSO "." CR>>

<ROUTINE V-RELEASE ()
    <TELL "You can't release " T ,PRSO "." CR>>

;----------------------------------------------------------------------

<SYNTAX WATER OBJECT = V-WATER>
<SYNTAX OIL OBJECT = V-OIL>
<VERB-SYNONYM OIL GREASE LUBRICATE>
;"TODO: Enable POUR WATER/OIL syntaxes once NEW-VOC? is working."
;<SYNTAX POUR WATER OBJECT = V-WATER>
;<SYNTAX POUR OIL OBJECT = V-OIL>
<SYNTAX POUR OBJECT = V-EMPTY>
<VERB-SYNONYM POUR DOUSE>

<ROUTINE V-WATER ()
    <COND (<HELD? ,BOTTLE> <PERFORM ,V?EMPTY ,BOTTLE>)
          (ELSE <TELL "Water? What water?" CR>)>>

<ROUTINE V-OIL ()
    <COND (<HELD? ,BOTTLE> <PERFORM ,V?EMPTY ,BOTTLE>)
          (ELSE <TELL "Oil? What oil?" CR>)>>

;----------------------------------------------------------------------

<SYNTAX BLAST = V-BLAST>
<SYNTAX BLAST OBJECT WITH OBJECT (HAVE HELD CARRIED) = V-BLAST-WITH>

<ROUTINE V-BLAST ()
    <COND (<N=? ,HERE ,AT-SW-END ,AT-NE-END>
           <TELL "Frustrating, isn't it?" CR>)
          (<AND <=? ,HERE ,AT-SW-END> <IN? ,BLACK-MARK-ROD ,AT-NE-END>>
           <SETG SCORE <+ ,SCORE 35>>
           <TELL "There is a loud explosion, and a twenty-foot hole appears in the far wall,
burying the dwarves in the rubble.
You march through the hole and find yourself in the main office,
where a cheering band of friendly elves carry the conquering adventurer off into the sunset." CR>
           <FINISH>)
          (<AND <=? ,HERE ,AT-NE-END> <IN? ,BLACK-MARK-ROD ,AT-SW-END>>
           <SETG SCORE <+ ,SCORE 20>>
           <JIGS-UP "There is a loud explosion, and a twenty-foot hole appears in the far wall,
burying the snakes in the rubble.
A river of molten lava pours in through the hole, destroying everything in its path, including you!">)
          (ELSE
           <JIGS-UP "There is a loud explosion, and you are suddenly splashed
across the walls of the room.">)>>

<ROUTINE V-BLAST-WITH ()
    <COND (<NOT <PRSI? ,BLACK-MARK-ROD>>
           <TELL "Blasting requires dynamite." CR>)
          (ELSE
           <TELL "Been eating those funny brownies again?" CR>)>>
          
;----------------------------------------------------------------------

<SYNTAX XYZZY = V-XYZZY>
<SYNTAX PLUGH = V-PLUGH>
<SYNTAX PLOVER = V-PLOVER>
<SYNTAX FEE = V-FEE>
<SYNTAX FIE = V-FOE>
<SYNTAX FOE = V-FOE>
<SYNTAX FOO = V-FOO>
<SYNTAX SESAME (SHAZAM HOCUS ABRACADABRA FOOBAR OPEN-SESAME FROTZ) = V-OLD-MAGIC>

<SYNTAX SAY BLAST OBJECT (FIND KLUDGEBIT) = V-BLAST>
<SYNTAX SAY XYZZY OBJECT (FIND KLUDGEBIT) = V-XYZZY>
<SYNTAX SAY PLUGH OBJECT (FIND KLUDGEBIT) = V-PLUGH>
<SYNTAX SAY PLOVER OBJECT (FIND KLUDGEBIT) = V-PLOVER>
<SYNTAX SAY FEE OBJECT (FIND KLUDGEBIT) = V-FEE>
<SYNTAX SAY FIE OBJECT (FIND KLUDGEBIT) = V-FIE>
<SYNTAX SAY FOE OBJECT (FIND KLUDGEBIT) = V-FOE>
<SYNTAX SAY FOO OBJECT (FIND KLUDGEBIT) = V-FOO>
<SYNTAX SAY SESAME OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY SHAZAM OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY HOCUS OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY ABRACADABRA OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY FOOBAR OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY OPEN-SESAME OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>
<SYNTAX SAY FROTZ OBJECT (FIND KLUDGEBIT) = V-OLD-MAGIC>

<ROUTINE V-XYZZY () <TELL "Nothing happens." CR>>

<ROUTINE V-PLUGH () <TELL "Nothing happens." CR>>

<ROUTINE V-PLOVER () <TELL "Nothing happens." CR>>

<ROUTINE V-FEE () <FTHING 0>>
<ROUTINE V-FIE () <FTHING 1>>
<ROUTINE V-FOE () <FTHING 2>>
<ROUTINE V-FOO () <FTHING 3>>

<GLOBAL FEEFIE-COUNT 0>

<ROUTINE FTHING (N)
    <COND (<N=? ,FEEFIE-COUNT .N>
           <SETG FEEFIE-COUNT 0>
           <TELL "Get it right, dummy!" CR>)
          (<G? <SETG FEEFIE-COUNT <+ ,FEEFIE-COUNT 1>> 3>
           <SETG FEEFIE-COUNT 0>
           <COND (<IN? ,GOLDEN-EGGS ,IN-GIANT-ROOM>
                  <TELL "Nothing happens." CR>)
                 (ELSE
                  <COND (<OR <HELD? ,GOLDEN-EGGS> <IN? ,GOLDEN-EGGS ,HERE>>
                         <TELL CT ,GOLDEN-EGGS " has vanished!" CR>)
                        (ELSE <TELL "Done!" CR>)>
                  <COND (<HELD? ,GOLDEN-EGGS> <SETG SCORE <- ,SCORE 5>>)
                        (<IN? ,GOLDEN-EGGS ,INSIDE-BUILDING>
                         <SETG SCORE <- ,SCORE <GETP ,GOLDEN-EGGS ,P?DEPOSIT-POINTS>>>)>
                  <MOVE ,GOLDEN-EGGS ,IN-GIANT-ROOM>
                  <COND (<=? ,HERE ,IN-GIANT-ROOM>
                         <TELL CR CR "A large nest full of golden eggs suddenly
appears out of nowhere!" CR>)>)>)
          (ELSE <TELL "Ok." CR>)>>

<ROUTINE V-OLD-MAGIC ()
    <TELL "Good try, but that is an old worn-out magic word." CR>>

;----------------------------------------------------------------------

<SYNTAX COUNT OBJECT = V-COUNT>

<SYNTAX KICK OBJECT = V-KICK>

<SYNTAX USE OBJECT = V-USE>

<ROUTINE V-COUNT ()
    <COND (<FSET? ,PRSO ,MULTITUDEBIT>
           <TELL "There are a multitude." CR>)
          (ELSE
           <TELL "I see one (1) " D ,PRSO "." CR>)>>

<ROUTINE V-KICK ()
    <PERFORM ,V?ATTACK ,PRSO>
    <RTRUE>>

<ROUTINE V-USE ()
    <TELL "You'll have to be a bit more explicit than that." CR>>

;----------------------------------------------------------------------

"XXX To be implemented"

<SYNTAX DRINK OBJECT = V-DRINK>
<SYNTAX FILL OBJECT = V-FILL>
<SYNTAX EMPTY OBJECT = V-EMPTY>
<SYNTAX BURN OBJECT = V-BURN>
<SYNTAX RUB OBJECT = V-RUB>
<SYNTAX WAVE OBJECT (HAVE HELD CARRIED) = V-WAVE>
<SYNTAX GIVE OBJECT (HAVE HELD CARRIED) TO OBJECT (FIND PERSONBIT) = V-GIVE>
<SYNTAX GIVE OBJECT (FIND PERSONBIT) OBJECT (HAVE HELD CARRIED) = V-RGIVE>
<SYNTAX ATTACK OBJECT (FIND PERSONBIT) = V-ATTACK>
<SYNTAX CLIMB OBJECT = V-CLIMB>
<SYNTAX CLIMB = V-CLIMB>
<SYNTAX JUMP = V-JUMP>
<SYNTAX THROW OBJECT (TAKE HAVE HELD CARRIED) AT OBJECT = V-THROW-AT>
<SYNTAX WAVE = V-WAVE-HANDS>
<SYNTAX LOOK UNDER OBJECT = V-LOOK-UNDER>
<SYNTAX PULL OBJECT = V-PULL>
<SYNTAX YES = V-YES>
<SYNTAX NO = V-NO>
<SYNTAX SEARCH OBJECT = V-SEARCH>
<SYNTAX SWIM = V-SWIM>
<SYNTAX SMELL OBJECT = V-SMELL>
<SYNTAX WAKE OBJECT (FIND PERSONBIT) = V-WAKE>

<ROUTINE V-RGIVE () <PERFORM ,V?GIVE ,PRSI ,PRSO> <RTRUE>>

<MAPF <>
      <FUNCTION (RTN)
          <EVAL <FORM ROUTINE .RTN '() '<TELL "Not implemented." CR>>>>
      (V-DRINK
       V-FILL
       V-EMPTY
       V-BURN
       V-RUB
       V-WAVE
       V-GIVE
       V-ATTACK
       V-CLIMB
       V-JUMP
       V-THROW-AT
       V-WAVE-HANDS
       V-LOOK-UNDER
       V-PULL
       V-YES
       V-NO
       V-SEARCH
       V-SWIM
       V-SMELL
       V-WAKE)>
