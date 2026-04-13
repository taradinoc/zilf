"Infrastructure for interiors (traditional parser areas)"

;"Each entrance lives on a specific floor at a specific coordinate and maps to an
interior ID."

<CONSTANT MAX-INTERIOR-ENTRANCES 32>

;"Interior IDs (resolved by LAUNCH-INTERIOR-ID in interiors.zil)."

<CONSTANT INTERIOR-INFO-BOOTH 1>
<CONSTANT INTERIOR-CARROT-FARM 2>
<CONSTANT INTERIOR-BEES 3>
<CONSTANT INTERIOR-BLACKSMITH 4>
<CONSTANT INTERIOR-BUSKER 5>
<CONSTANT INTERIOR-ORACLE-GROTTO 6>

<GLOBAL INTERIOR-ENTRANCE-COUNT 0>
<GLOBAL INTERIOR-ENTRANCE-FLOOR <ITABLE ,MAX-INTERIOR-ENTRANCES (BYTE) 0>>
<GLOBAL INTERIOR-ENTRANCE-X <ITABLE ,MAX-INTERIOR-ENTRANCES (BYTE) 0>>
<GLOBAL INTERIOR-ENTRANCE-Y <ITABLE ,MAX-INTERIOR-ENTRANCES (BYTE) 0>>
<GLOBAL INTERIOR-ENTRANCE-ID <ITABLE ,MAX-INTERIOR-ENTRANCES (BYTE) 0>>

;"Chosen at game init: a floor (2..5) that always has a carrot farm interior entrance."

<GLOBAL GUARANTEED-CARROT-FARM-FLOOR 0>

;"Chosen at game init: a floor (2..MAX-1) that always has a busker interior entrance.
    Excludes the carrot farm floor and floors with other guaranteed interiors."

<GLOBAL GUARANTEED-BUSKER-FLOOR 0>

;"Bee swarm: implemented as an enemy that spawns one turn after leaving the hive.
    BEE-SWARM-FLOOR/X/Y store the hive entrance ('home') for despawn distance checks."

<GLOBAL BEE-SWARM-ON? <>>
<GLOBAL BEE-SWARM-FLOOR 0>
<GLOBAL BEE-SWARM-X 0>
<GLOBAL BEE-SWARM-Y 0>

;"When set, the player has just exited the hive and the swarm will spawn on the next turn."

<GLOBAL BEE-SWARM-PENDING? <>>
<GLOBAL BEE-SWARM-PEND-FLOOR 0>
<GLOBAL BEE-SWARM-PEND-X 0>
<GLOBAL BEE-SWARM-PEND-Y 0>
<GLOBAL BEE-SWARM-PEND-DELAY 0>

;"Set by AFTER-PLAYER-RELOCATE when stepping onto an interior entrance."

<GLOBAL INTERIOR-LAUNCHED? <>>

"Interior status line"

<STATUS-LINE-SECTION GOLD
                     (JUSTIFY LEFT)
                     (WIDTH 11)
                     (CONTENT <TELL "Gold: " N ,PLAYER-GOLD>)>

<STATUS-LINE-SECTION HP
                     (JUSTIFY LEFT)
                     (WIDTH 11)
                     (CONTENT <TELL "HP: " N ,PLAYER-HP "/" N ,PLAYER-MAX-HP>)>

<STATUS-LINE-SECTION FLOOR
                     (JUSTIFY LEFT)
                     (WIDTH 14)
                     (CONTENT
                     <TELL "Floor: " N ,CURRENT-FLOOR "/" N ,MAX-FLOORS>)>

<STATUS-LINE RASCAL
             (SECTION LOCATION)
             (SECTION FLOOR)
             (SECTION HP)
             (SECTION GOLD)>

<ROUTINE LAUNCH-INTERIOR (ROOM "AUX" ENTRX ENTRY LEFT-BEES? MONKEY)
    <SET ENTRX ,PLAYER-X>
    <SET ENTRY ,PLAYER-Y>
    <SET LEFT-BEES? <==? .ROOM ,BEE-HIVE>>
    <UI-RESET>
    <CLEAR -1>
    <SETG HERE .ROOM>
    <MOVE ,PLAYER ,HERE>
    <SETG WINNER ,PLAYER>
    <SETG SCORE ,PLAYER-GOLD>
    <INTERIOR-ENTER-SYNC>
    <INIT-STATUS-LINE RASCAL>
    <SCREEN 0>
    <CRLF>
    <CRLF>
    <TELL "You approach the " <GETP .ROOM ,P?INTERIOR-NAME> "." CR CR>

    <SET MONKEY <ENEMY-OBJ-OF-TYPE ,CURRENT-FLOOR-OBJ ,ETYPE-MONKEY>>
    <COND (<AND .MONKEY <FSET? .MONKEY ,TAMEBIT>>
           <TELL "The monkey follows you." CR CR>
           <SET-TAMED-MONKEY-VOCAB .MONKEY>
           <MOVE .MONKEY ,HERE>)>

    <APPLY <GETP .ROOM ,P?ACTION> ,M-ENTER>
    <V-LOOK>
    <WRAP-PARSER-MAIN-LOOP>
    <INTERIOR-EXIT-SYNC>
    <UI-RESET>
    <CLEAR -1>
    <SPLIT ,UPPER-HEIGHT>
    <UI-LOG-COLOR>
    <CLEAR 0>
    <SETG FULL-REDRAW? T>
    ;"If the player died inside the interior, log the death and don't print leave text or spawn hazards."
    <COND (<L=? ,PLAYER-HP 0> <CHECK-END> <RETURN>)>
    <COND (.LEFT-BEES? <START-BEE-SWARM ,CURRENT-FLOOR .ENTRX .ENTRY>)>
    <COND (,PENDING-INTERIOR-TELEPORT?
           <SETG PENDING-INTERIOR-TELEPORT? <>>
           <INTERIOR-TELEPORT-PLAYER>)>

    <COND (<AND .MONKEY
                <FSET? .MONKEY ,TAMEBIT>
                <OR <IN? .MONKEY ,HERE> <IN? .MONKEY ,WINNER>>>
           <MOVE .MONKEY ,CURRENT-FLOOR-OBJ>
           <DO (D 1 8)
               <COND (<TRY-ENEMY-MOVE .MONKEY
                                      <+ ,PLAYER-X <DIR8-DX .D>>
                                      <+ ,PLAYER-Y <DIR8-DY .D>>>
                      <RETURN>)>>)>

    <LOG "You leave the " <GETP .ROOM ,P?INTERIOR-NAME> "." CR>>

<ROUTINE SET-TAMED-MONKEY-VOCAB (OBJ "AUX" PT A1)
    ;"Make a tamed monkey addressable and visible in the parser world."
    <COND (<NOT .OBJ> <RFALSE>)>
    <FCLEAR .OBJ ,NDESCBIT>
    <SET A1 <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>
    <COND (<SET PT <GETPT .OBJ ,P?SYNONYM>>
           <PUT .PT 0 ,WORD-MONKEY>
           <PUT .PT 1 ,WORD-MONKEYS>)>
    <COND (<SET PT <GETPT .OBJ ,P?ADJECTIVE>>
           <PUT/B .PT 0 .A1>)>
    <PUTP .OBJ ,P?DESCFCN MONKEY-DESCFCN>
    <PUTP .OBJ ,P?ACTION ENEMY-ACTION-MONKEY>
    <PUTP .OBJ ,P?GENERIC MONKEY-GENERIC-FCN>
    <RTRUE>>

;"Let the player GIVE MONKEY TO BUSKER without holding the monkey."
<REPLACE-DEFINITION FAILS-HAVE-CHECK?
    <ROUTINE FAILS-HAVE-CHECK? (OBJ)
        <NOT <OR <ORDERING?> <HELD? .OBJ> <MONKEY? .OBJ>>>>>

<ROUTINE INTERIOR-TELEPORT-PLAYER ("AUX" TRIES X Y)
    ;"Teleport the roguelike position after leaving an interior, without calling
      AFTER-PLAYER-RELOCATE (which could immediately re-launch another interior)."
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES ,TELEPORT-TRY-LIMIT> <RETURN T>)>
        <SET X <+ 1 <RNG ,MAP-W>>>
        <SET Y <+ 1 <RNG ,MAP-H>>>
        <COND (<NOT <IN-BOUNDS? .X .Y>> <AGAIN>)>
        <COND (<NOT <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
                        <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
                        <==? <TILE-AT .X .Y> ,TILE-DOOR>>>
               <AGAIN>)>
        <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>
                   <==? <TILE-AT .X .Y> ,TILE-INTERIOR>>
               <AGAIN>)>
        <COND (<TRADER-AT? .X .Y> <AGAIN>)>
        <COND (<G? <ENEMY-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <GOLD-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <FOOD-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <WEAPON-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <TREASURE-AT? .X .Y> 0> <AGAIN>)>
        <COND (<POTION-AT? .X .Y> <AGAIN>)>
        <SETG PLAYER-X .X>
        <SETG PLAYER-Y .Y>
        <RETURN T>>>

"Interior inventory"

<REPLACE-LIBRARY-MESSAGES INVENTORY
    (HEADER "Your pack contains:")
    (EMPTY-HANDED "Your pack is empty.")>

<USE "TEMPLATE">

<BIND ((REDEFINE T))
    <DEFMAC GAME-VERB? ()
        `<VERB? QUIT VERSION WAIT ;SAVE ;RESTORE RESTART UNDO SUPERBRIEF BRIEF VERBOSE AGAIN SCRIPT UNSCRIPT PRONOUNS TELL>>>

<ROUTINE RASCAL-ITEM? (OBJ "AUX" K)
    <COND (<NOT .OBJ> <RFALSE>)>
    <SET K <GETP .OBJ ,P?R-ITKIND>>
    <AND <G? .K 0> <L=? .K ,ITEMKIND-COUNT>>>

<ROUTINE RASCAL-ENEMY? (OBJ "AUX" K)
    <COND (<NOT .OBJ> <RFALSE>)>
    <SET K <GETP .OBJ ,P?R-ETYPE>>
    <AND <G? .K 0> <L=? .K ,ETYPE-COUNT>>>

<REPLACE-DEFINITION INDISTINGUISHABLE?
    <ROUTINE INDISTINGUISHABLE? (A B)
        <OR <AND <RASCAL-ITEM? .A>
                 <RASCAL-ITEM? .B>
                 <==? <GETP .A ,P?R-ITKIND> <GETP .B ,P?R-ITKIND>>
                 <==? <GETP .A ,P?R-ITID> <GETP .B ,P?R-ITID>>
                 <==? <GETP .A ,P?R-ITLVL> <GETP .B ,P?R-ITLVL>>
                 <==? <GETP .A ,P?R-ITENCH> <GETP .B ,P?R-ITENCH>>>
            <AND <CARROT? .A> <CARROT? .B>>
            <AND <MONKEY? .A> <MONKEY? .B> <FSET? .A ,SOLDBIT> <FSET? .B ,SOLDBIT>>>>>

<REPLACE-DEFINITION INV-EXTRA-DETAILS
    <ROUTINE INV-PRINT-EXTRA-DETAILS (OBJ)
        <COND (<==? .OBJ ,EQUIPPED-WEAPON> <TELL " (equipped)">)>>

    <ROUTINE INV-SAME-EXTRA-DETAILS? (A B)
        ;"Only one item can be equipped at a time"
        <N==? ,EQUIPPED-WEAPON .A .B>>>

<IF-DEBUG <CONSTANT DEBUG-PACK-SYNC? <>>>

<ROUTINE INTERIOR-PACK-COUNT ("AUX" O C)
    ;"Count carried real item objects in the parser interior (WINNER contents)."
    <COND (<NOT <SET O <FIRST? ,WINNER>>> <RFALSE>)>
    <SET C 0>
    <REPEAT ()
        <COND (<RASCAL-ITEM? .O> <SET C <+ .C 1>>)>
        <COND (<NOT <SET O <NEXT? .O>>> <RETURN .C>)>>>

<ROUTINE INTERIOR-PACK-FULL? ()
    <G=? <INTERIOR-PACK-COUNT> ,INV-SIZE>>

<ROUTINE SANITIZE-DUNGEON-INVENTORY ("AUX" I K O)
    ;"Remove any invalid inventory entries (kind outside ITEMKIND-* range)."
    <SET I ,INV-SIZE>
    <REPEAT ()
        <COND (<L? .I 1> <RETURN>)>
        <SET O <INV-NTH-OBJ .I>>
        <COND (<NOT .O>
               <SET I <- .I 1>>
               <AGAIN>)>
        <SET K <GETP .O ,P?R-ITKIND>>
         <COND (<OR <L? .K ,ITEMKIND-FOOD> <G? .K ,ITEMKIND-KEY>>
               <IF-DEBUG
                   <COND (,DEBUG-PACK-SYNC?
                          <TELL "[PACKSYNC] SANITIZE removing slot " N .I
                                " kind=" N .K CR>)>>
               <INV-REMOVE .I>)>
        <SET I <- .I 1>>>
    <RTRUE>>

<ROUTINE ASSIGN-MISSING-DUNGEON-SLOTS ("AUX" O N SLOT)
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN T>)>
        <SET N <NEXT? .O>>
        <SET SLOT <INV-SLOT-OF-OBJ .O>>
        <COND (<L=? .SLOT 0>
               <SET SLOT <INV-FIRST-FREE-SLOT>>
               <COND (<G? .SLOT 0>
                      <INV-SET-OBJ .SLOT .O>)>)>
        <SET O .N>>>

<ROUTINE CLEAR-NONHELD-DUNGEON-SLOTS ("AUX" O)
    <DO (I 1 ,INV-SIZE)
        <SET O <INV-NTH-OBJ .I>>
        <COND (<AND .O <NOT <IN? .O ,PLAYER-INVENTORY>>>
               <INV-CLEAR-SLOT .I>)>>
    <RTRUE>>

<ROUTINE RESYNC-DUNGEON-INVENTORY-SLOTS ()
    ;"Preserve existing held slots where possible, then reclaim stale ones and fill again."
    <ASSIGN-MISSING-DUNGEON-SLOTS>
    <CLEAR-NONHELD-DUNGEON-SLOTS>
    <ASSIGN-MISSING-DUNGEON-SLOTS>
    <RTRUE>>

;"Compile-time vocab words for pack items (so we can write them into SYNONYM/ADJECTIVE)."

<CONSTANT WORD-DAGGER <VOC "DAGGER" OBJECT>>
<CONSTANT WORD-KATANA <VOC "KATANA" OBJECT>>
<CONSTANT WORD-WARAXE <VOC "WARAXE" OBJECT>>
<CONSTANT WORD-SCYTHE <VOC "SCYTHE" OBJECT>>
<CONSTANT WORD-CUDGEL <VOC "CUDGEL" OBJECT>>
<CONSTANT WORD-HAMMER <VOC "HAMMER" OBJECT>>
<CONSTANT WORD-AXE <VOC "AXE" OBJECT>>
<CONSTANT WORD-WEAPON <VOC "WEAPON" OBJECT>>
<CONSTANT WORD-DAGGERS <VOC "DAGGERS" OBJECT>>
<CONSTANT WORD-KATANAS <VOC "KATANAS" OBJECT>>
<CONSTANT WORD-WARAXES <VOC "WARAXES" OBJECT>>
<CONSTANT WORD-SCYTHES <VOC "SCYTHES" OBJECT>>
<CONSTANT WORD-CUDGELS <VOC "CUDGELS" OBJECT>>
<CONSTANT WORD-HAMMERS <VOC "HAMMERS" OBJECT>>
<CONSTANT WORD-AXES <VOC "AXES" OBJECT>>
<CONSTANT WORD-WEAPONS <VOC "WEAPONS" OBJECT>>

<CONSTANT WORD-BANANA <VOC "BANANA" OBJECT>>
<CONSTANT WORD-CHEESE <VOC "CHEESE" OBJECT>>
<CONSTANT WORD-GRAPES <VOC "GRAPES" OBJECT>>
<CONSTANT WORD-MUFFIN <VOC "MUFFIN" OBJECT>>
<CONSTANT WORD-TURKEY <VOC "TURKEY" OBJECT>>
<CONSTANT WORD-CARROT <VOC "CARROT" OBJECT>>
<CONSTANT WORD-CAVIAR <VOC "CAVIAR" OBJECT>>
<CONSTANT WORD-FOOD <VOC "FOOD" OBJECT>>
<CONSTANT WORD-BANANAS <VOC "BANANAS" OBJECT>>
<CONSTANT WORD-CHEESES <VOC "CHEESES" OBJECT>>
<CONSTANT WORD-GRAPESES <VOC "GRAPESES" OBJECT>>
<CONSTANT WORD-MUFFINS <VOC "MUFFINS" OBJECT>>
<CONSTANT WORD-TURKEYS <VOC "TURKEYS" OBJECT>>
<CONSTANT WORD-CARROTS <VOC "CARROTS" OBJECT>>
<CONSTANT WORD-CAVIARS <VOC "CAVIARS" OBJECT>>
<CONSTANT WORD-FOODS <VOC "FOODS" OBJECT>>

<CONSTANT WORD-MONKEY <VOC "MONKEY" OBJECT>>
<CONSTANT WORD-MONKEYS <VOC "MONKEYS" OBJECT>>

<CONSTANT WORD-AMULET <VOC "AMULET" OBJECT>>
<CONSTANT WORD-SCARAB <VOC "SCARAB" OBJECT>>
<CONSTANT WORD-GOBLET <VOC "GOBLET" OBJECT>>
<CONSTANT WORD-IOLITE <VOC "IOLITE" OBJECT>>
<CONSTANT WORD-GARNET <VOC "GARNET" OBJECT>>
<CONSTANT WORD-JASPER <VOC "JASPER" OBJECT>>
<CONSTANT WORD-ZIRCON <VOC "ZIRCON" OBJECT>>
<CONSTANT WORD-POSTER <VOC "POSTER" OBJECT>>
<CONSTANT WORD-TROPHY <VOC "TROPHY" OBJECT>>
<CONSTANT WORD-SCRYRA <VOC "SCRYRA" OBJECT>>
<CONSTANT WORD-TREASURE <VOC "TREASURE" OBJECT>>
<CONSTANT WORD-TREASURES <VOC "TREASURES" OBJECT>>

<CONSTANT WORD-POTION <VOC "POTION" OBJECT>>
<CONSTANT WORD-POTIONS <VOC "POTIONS" OBJECT>>
<CONSTANT WORD-HEALTH <VOC "HEALTH" OBJECT>>
<CONSTANT WORD-MUSCLE <VOC "MUSCLE" OBJECT>>
<CONSTANT WORD-VISION <VOC "VISION" OBJECT>>
<CONSTANT WORD-HIDING <VOC "HIDING" OBJECT>>
<CONSTANT WORD-POISON <VOC "POISON" OBJECT>>
<CONSTANT WORD-MOTION <VOC "MOTION" OBJECT>>
<CONSTANT WORD-SHADOW <VOC "SHADOW" OBJECT>>
<CONSTANT WORD-METTLE <VOC "METTLE" OBJECT>>
<CONSTANT WORD-TORPOR <VOC "TORPOR" OBJECT>>
<CONSTANT WORD-HUSTLE <VOC "HUSTLE" OBJECT>>
<CONSTANT ADJ-ARGENT <VOC "ARGENT" ADJ>>
<CONSTANT ADJ-BLUISH <VOC "BLUISH" ADJ>>
<CONSTANT ADJ-MAROON <VOC "MAROON" ADJ>>
<CONSTANT ADJ-VIOLET <VOC "VIOLET" ADJ>>
<CONSTANT ADJ-ORANGE <VOC "ORANGE" ADJ>>
<CONSTANT ADJ-YELLOW <VOC "YELLOW" ADJ>>
<CONSTANT ADJ-INDIGO <VOC "INDIGO" ADJ>>
<CONSTANT ADJ-SALMON <VOC "SALMON" ADJ>>
<CONSTANT ADJ-AUBURN <VOC "AUBURN" ADJ>>
<CONSTANT ADJ-SIENNA <VOC "SIENNA" ADJ>>
<CONSTANT ADJ-GOLDEN <VOC "GOLDEN" ADJ>>
<CONSTANT ADJ-GOLD <VOC "GOLD" ADJ>>
<CONSTANT ADJ-SILVER <VOC "SILVER" ADJ>>
<CONSTANT ADJ-BRONZE <VOC "BRONZE" ADJ>>
<CONSTANT ADJ-COPPER <VOC "COPPER" ADJ>>
<CONSTANT ADJ-NICKEL <VOC "NICKEL" ADJ>>
<CONSTANT WORD-KEY <VOC "KEY" OBJECT>>
<CONSTANT WORD-KEYS <VOC "KEYS" OBJECT>>

<ROUTINE WEAPON-NOUN1 (ID)
    <COND (<==? .ID ,WEAPON-DAGGER> ,WORD-DAGGER)
          (<==? .ID ,WEAPON-KATANA> ,WORD-KATANA)
          (<==? .ID ,WEAPON-WARAXE> ,WORD-WARAXE)
          (<==? .ID ,WEAPON-SCYTHE> ,WORD-SCYTHE)
          (<==? .ID ,WEAPON-CUDGEL> ,WORD-CUDGEL)
          (<==? .ID ,WEAPON-HAMMER> ,WORD-HAMMER)
          (ELSE ,W?\,DUMMY-NOUN)>>

<ROUTINE WEAPON-NOUN2 (ID)
    <COND (<==? .ID ,WEAPON-WARAXE> ,WORD-AXE) (ELSE ,WORD-WEAPON)>>

<ROUTINE WEAPON-PLURAL1 (ID)
    <COND (<==? .ID ,WEAPON-DAGGER> ,WORD-DAGGERS)
          (<==? .ID ,WEAPON-KATANA> ,WORD-KATANAS)
          (<==? .ID ,WEAPON-WARAXE> ,WORD-WARAXES)
          (<==? .ID ,WEAPON-SCYTHE> ,WORD-SCYTHES)
          (<==? .ID ,WEAPON-CUDGEL> ,WORD-CUDGELS)
          (<==? .ID ,WEAPON-HAMMER> ,WORD-HAMMERS)
          (ELSE ,WORD-WEAPONS)>>

<ROUTINE WEAPON-PLURAL2 (ID)
    <COND (<==? .ID ,WEAPON-WARAXE> ,WORD-AXES) (ELSE ,WORD-WEAPONS)>>

<ROUTINE FOOD-NOUN1 (ID)
    <COND (<==? .ID ,FOOD-BANANA> ,WORD-BANANA)
          (<==? .ID ,FOOD-CHEESE> ,WORD-CHEESE)
          (<==? .ID ,FOOD-GRAPES> ,WORD-GRAPES)
          (<==? .ID ,FOOD-MUFFIN> ,WORD-MUFFIN)
          (<==? .ID ,FOOD-TURKEY> ,WORD-TURKEY)
          (<==? .ID ,FOOD-CARROT> ,WORD-CARROT)
          (<==? .ID ,FOOD-CAVIAR> ,WORD-CAVIAR)
          (ELSE ,W?\,DUMMY-NOUN)>>

<ROUTINE FOOD-PLURAL (ID)
    <COND (<==? .ID ,FOOD-BANANA> ,WORD-BANANAS)
          (<==? .ID ,FOOD-CHEESE> ,WORD-CHEESES)
          (<==? .ID ,FOOD-GRAPES> ,WORD-GRAPESES)
          (<==? .ID ,FOOD-MUFFIN> ,WORD-MUFFINS)
          (<==? .ID ,FOOD-TURKEY> ,WORD-TURKEYS)
          (<==? .ID ,FOOD-CARROT> ,WORD-CARROTS)
          (<==? .ID ,FOOD-CAVIAR> ,WORD-CAVIARS)
          (ELSE ,WORD-FOODS)>>

<ROUTINE TREASURE-NOUN1 (ID)
    <COND (<==? .ID ,TREASURE-AMULET> ,WORD-AMULET)
          (<==? .ID ,TREASURE-SCARAB> ,WORD-SCARAB)
          (<==? .ID ,TREASURE-GOBLET> ,WORD-GOBLET)
          (<==? .ID ,TREASURE-IOLITE> ,WORD-IOLITE)
          (<==? .ID ,TREASURE-GARNET> ,WORD-GARNET)
          (<==? .ID ,TREASURE-JASPER> ,WORD-JASPER)
          (<==? .ID ,TREASURE-ZIRCON> ,WORD-ZIRCON)
          (<==? .ID ,TREASURE-POSTER> ,WORD-POSTER)
          (<==? .ID ,TREASURE-TROPHY> ,WORD-TROPHY)
          (ELSE ,W?\,DUMMY-NOUN)>>

<ROUTINE TREASURE-NOUN2 (ID)
    <COND (<==? .ID ,TREASURE-TROPHY> ,WORD-SCRYRA) (ELSE ,WORD-TREASURE)>>

<ROUTINE POTION-ADJ1 (ID)
    <COND (<==? .ID ,POTCOLOR-ARGENT> ,ADJ-ARGENT)
          (<==? .ID ,POTCOLOR-BLUISH> ,ADJ-BLUISH)
          (<==? .ID ,POTCOLOR-MAROON> ,ADJ-MAROON)
          (<==? .ID ,POTCOLOR-VIOLET> ,ADJ-VIOLET)
          (<==? .ID ,POTCOLOR-ORANGE> ,ADJ-ORANGE)
          (<==? .ID ,POTCOLOR-YELLOW> ,ADJ-YELLOW)
          (<==? .ID ,POTCOLOR-INDIGO> ,ADJ-INDIGO)
          (<==? .ID ,POTCOLOR-SALMON> ,ADJ-SALMON)
          (<==? .ID ,POTCOLOR-AUBURN> ,ADJ-AUBURN)
          (<==? .ID ,POTCOLOR-SIENNA> ,ADJ-SIENNA)
          (ELSE <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>)>>

<ROUTINE POTION-EFFECT-WORD (TYPE)
    <COND (<==? .TYPE ,POTION-HEALTH> ,WORD-HEALTH)
          (<==? .TYPE ,POTION-MUSCLE> ,WORD-MUSCLE)
          (<==? .TYPE ,POTION-VISION> ,WORD-VISION)
          (<==? .TYPE ,POTION-HIDING> ,WORD-HIDING)
          (<==? .TYPE ,POTION-POISON> ,WORD-POISON)
          (<==? .TYPE ,POTION-MOTION> ,WORD-MOTION)
          (<==? .TYPE ,POTION-SHADOW> ,WORD-SHADOW)
          (<==? .TYPE ,POTION-METTLE> ,WORD-METTLE)
          (<==? .TYPE ,POTION-TORPOR> ,WORD-TORPOR)
          (<==? .TYPE ,POTION-HUSTLE> ,WORD-HUSTLE)
          (ELSE ,W?\,DUMMY-NOUN)>>

<ROUTINE KEY-ADJ1 (TYPE)
    <COND (<==? .TYPE ,LOCK-GOLDEN> ,ADJ-GOLDEN)
          (<==? .TYPE ,LOCK-SILVER> ,ADJ-SILVER)
          (<==? .TYPE ,LOCK-BRONZE> ,ADJ-BRONZE)
          (<==? .TYPE ,LOCK-COPPER> ,ADJ-COPPER)
          (<==? .TYPE ,LOCK-NICKEL> ,ADJ-NICKEL)
          (ELSE ,W?\,DUMMY-ADJ)>>

<ROUTINE KEY-ADJ2 (TYPE)
    <COND (<==? .TYPE ,LOCK-GOLDEN> ,ADJ-GOLD)
          (ELSE ,W?\,DUMMY-ADJ)>>

<ROUTINE UPDATE-POTION-ITEMS-FOR-COLOR (COLOR "AUX" O N)
    <SET O <FIRST? ,WINNER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <COND (<AND <RASCAL-ITEM? .O>
                    <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-POTION>
                    <==? <GETP .O ,P?R-ITID> .COLOR>>
               <SET-ITEM-VOCAB .O>)>
        <SET O .N>>

    <SET O <FIRST? ,HERE>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <COND (<AND <RASCAL-ITEM? .O>
                <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-POTION>
                <==? <GETP .O ,P?R-ITID> .COLOR>>
            <SET-ITEM-VOCAB .O>)>
        <SET O .N>>>

<ROUTINE ITEM-ACTION-WEAPON ("AUX" E TYPE L)
    <COND (<VERB? EXAMINE>
           <SET TYPE <GETP ,PRSO ,P?R-ITID>>
           <COND (<==? .TYPE ,WEAPON-DAGGER>
                  <TELL "It's called a dagger, but come to think of it, you've
never actually seen it dag. This short blade has low base damage, above-average
variance, and an outstanding crit chance." CR>)
                 (<==? .TYPE ,WEAPON-WARAXE>
                  <TELL "It's autographed by all the members of War. This
double-bladed axe has above-average base damage, wild variance, and an
impressive crit chance." CR>)
                 (<==? .TYPE ,WEAPON-CUDGEL>
                  <TELL "Metaphorically, a cudgel is an issue used to
aggressively beat down an opponent in public discourse. Physically,
this cudgel is a stout stick used to aggressively beat down an opponent in
a dungeon, with average base damage, minimal variance, and an average crit
chance." CR>)
                 (<==? .TYPE ,WEAPON-KATANA>
                  <TELL "This curved, single-edged blade is traditionally
associated with samurai and ninja turtles. It has average base damage,
high variance, and a below-average crit chance." CR>)
                 (<==? .TYPE ,WEAPON-SCYTHE>
                  <TELL "This curved blade on the end of a long pole
demonstrates why it was so hard to convince people not to fear the reaper. It
has above-average base damage, wild variance, and a low crit chance." CR>)
                 (<==? .TYPE ,WEAPON-HAMMER>
                  <TELL "When they sang about hammering out danger, this must've
been what they had in mind. It has powerful base damage, average variance, and a
minimal crit chance." CR>)
                 (ELSE <TELL "It's a typical " WEAPON-NAME .TYPE "." CR>)>
           <COND (<SET E <GETP ,PRSO ,P?R-ITENCH>>
                  <TELL CR "Because of its +" N .E
                           " enchantment, it hits like a level "
                           N <+ <SET L <GETP ,PRSO ,P?R-ITLVL>> .E> " "
                           WEAPON-NAME .TYPE " and only requires "
                           N <MAX 1 <- .L .E>> " strength to wield." CR>)>
           <RTRUE>)>>

<ROUTINE ITEM-ACTION-TREASURE ()
    <COND (<VERB? EXAMINE READ>
           <COND (<POSTER? ,PRSO> <POSTER-F>)
                 (<==? <GETP ,PRSO ,P?R-ITID> ,TREASURE-TROPHY>
                  <TELL "It's a foot-tall ornate cup made of yellowish metal.
Engraved on one side, you see the outline of an oval with a zigzag line running
lengthwise across it and an inverted V above it." CR>)
                 (ELSE <TELL "It looks valuable." CR>)>)>>

<ROUTINE ITEM-ACTION-POTION ("AUX" COLOR)
    <COND (<VERB? DRINK>
           <SETG P-CONT 0>
           <COND (<NOT <==? <GETP ,PRSO ,P?R-ITKIND> ,ITEMKIND-POTION>>
                  <RFALSE>)>
           <SET COLOR <GETP ,PRSO ,P?R-ITID>>
           <DRINK-POTION-COLOR .COLOR ,POTIONFX-FL-INTERIOR>
           <UPDATE-POTION-ITEMS-FOR-COLOR .COLOR>
           ;"Consume: remove the item object from the live inventory."
           <REMOVE ,PRSO>
           <FREE-RASCAL-ITEM ,PRSO>
           <RTRUE>)
          (<VERB? EXAMINE>
           <TELL "It's a glass flask with a cork stopper, filled with "
                 POTION-BARE-COLOR-NAME <GETP ,PRSO ,P?R-ITID> " liquid." CR>)
          (<VERB? OPEN>
           <TELL "No sense in opening it unless you're going to drink it." CR>)>>

<ROUTINE ITEM-ACTION-FOOD ("AUX" ID HEAL NEWHP)
    <COND (<VERB? EAT>
           <SETG P-CONT 0>
           <COND (<NOT <==? <GETP ,PRSO ,P?R-ITKIND> ,ITEMKIND-FOOD>> <RFALSE>)>
           <SET ID <GETP ,PRSO ,P?R-ITID>>
           <SET HEAL <FOOD-HEAL-AMT .ID>>
           <SET NEWHP <+ ,PLAYER-HP .HEAL>>
           <COND (<G? .NEWHP ,PLAYER-MAX-HP> <SET NEWHP ,PLAYER-MAX-HP>)>
           <SETG PLAYER-HP .NEWHP>
           <HONORS-NOTE-PLAYER-HP>
           <COND (<AND <G? .ID 0> <L=? .ID ,FOOD-TYPE-COUNT>>
                  <STATS-INC-WORD-TABLE ,STATS-FOODS-EATEN <- .ID 1>>)>
           <TELL "You eat the " FOOD-NAME .ID " and recover " N .HEAL " HP."
                 CR>
           ;"Consume: remove the item object from the live inventory."
           <REMOVE ,PRSO>
           <FREE-RASCAL-ITEM ,PRSO>
           <RTRUE>)
          (<VERB? EXAMINE>
           <SET ID <GETP ,PRSO ,P?R-ITID>>
           <SET HEAL <FOOD-HEAL-AMT .ID>>
           <COND (<==? .ID ,FOOD-GRAPES> <TELL "They look">)
                 (ELSE <TELL "It looks">)>
           <TELL " mighty delicious, and about " N .HEAL " HP's worth of nutritious." CR>)
          (<VERB? TAKE>
           <COND (<AND <CARROT? ,PRSO> <ACCESSIBLE? ,CARROT-PATCH>>
                  <PERFORM ,V?TAKE ,CARROT-PATCH>)>)
          (ELSE <RFALSE>)>>

<ROUTINE ITEM-ACTION-KEY ("AUX" ID)
    <COND (<VERB? EXAMINE>
           <SET ID <GETP ,PRSO ,P?R-ITID>>
           <TELL "The " KEY-NAME .ID " probably opens a "
                 <KEY-BARE-METAL-DESC .ID> " lock somewhere." CR>)>>

<ROUTINE SET-ITEM-VOCAB (OBJ "AUX" K ID PT N1 N2 A1 A2 P1 P2)
    <SET K <GETP .OBJ ,P?R-ITKIND>>
    <SET ID <GETP .OBJ ,P?R-ITID>>
    <COND (<==? .K ,ITEMKIND-WEAPON>
           <PUTP .OBJ ,P?ACTION ,ITEM-ACTION-WEAPON>
           <SET N1 <WEAPON-NOUN1 .ID>>
           <SET N2 <WEAPON-NOUN2 .ID>>
           <SET A1 <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>
           <SET A2 .A1>
           <SET P1 <WEAPON-PLURAL1 .ID>>
           <SET P2 <WEAPON-PLURAL2 .ID>>)
          (<==? .K ,ITEMKIND-FOOD>
           <PUTP .OBJ ,P?ACTION ,ITEM-ACTION-FOOD>
           <SET N1 <FOOD-NOUN1 .ID>>
           <SET N2 ,WORD-FOOD>
           <SET A1 <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>
           <SET A2 .A1>
           <SET P1 <FOOD-PLURAL .ID>>
           <SET P2 ,WORD-FOODS>)
          (<==? .K ,ITEMKIND-TREASURE>
           <PUTP .OBJ ,P?ACTION ,ITEM-ACTION-TREASURE>
           <SET N1 <TREASURE-NOUN1 .ID>>
           <SET N2 <TREASURE-NOUN2 .ID>>
           <SET A1 <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>
           <SET A2 .A1>
           <SET P1 ,WORD-TREASURES>
           <SET P2 <>>)
          (<==? .K ,ITEMKIND-POTION>
           <PUTP .OBJ ,P?ACTION ,ITEM-ACTION-POTION>
           <SET N1 ,WORD-POTION>
           <COND (<AND <G? .ID 0>
                       <L=? .ID ,POTION-COLOR-COUNT>
                       <G? <GETB ,POTION-DISCOVERED <- .ID 1>> 0>>
                  ;"Identified: parse as 'POTION' + effect, both nouns, and let
                    the parser handle the 'OF' in between."
                  <SET N2
                       <POTION-EFFECT-WORD <GETB ,POTION-TYPE-FOR-COLOR
                                                 <- .ID 1>>>>
                  <SET A1
                       <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>)
                 (ELSE
                  ;"Unidentified: parse as color adjective + POTION."
                  <SET N2 ,W?\,DUMMY-NOUN>
                  <SET A1 <POTION-ADJ1 .ID>>)>
           <SET P1 ,WORD-POTIONS>
           <SET P2 <>>)
          (<==? .K ,ITEMKIND-KEY>
           <PUTP .OBJ ,P?ACTION ,ITEM-ACTION-KEY>
           <SET N1 ,WORD-KEY>
           <SET A1 <KEY-ADJ1 .ID>>
           <SET A2 <KEY-ADJ2 .ID>>
           <SET P1 ,WORD-KEYS>
           <SET P2 <>>)
          (ELSE
           <PUTP .OBJ ,P?ACTION 0>
           <SET N1 ,W?\,DUMMY-NOUN>
           <SET N2 ,W?\,DUMMY-NOUN>
           <SET A1 <VERSION? (ZIP ,A?\,DUMMY-ADJ) (ELSE ,W?\,DUMMY-ADJ)>>
           <SET A2 .A1>
           <SET P1 <>>
           <SET P2 <>>)>
    <COND (<SET PT <GETPT .OBJ ,P?SYNONYM>> <PUT .PT 0 .N1> <PUT .PT 1 .N2>)>
    <COND (<SET PT <GETPT .OBJ ,P?ADJECTIVE>>
           <PUT/B .PT 0 .A1>
           <PUT/B .PT 1 .A2>)>
    <COND (<SET PT <GETPT .OBJ ,P?PLURAL>> <PUT .PT 0 .P1> <PUT .PT 1 .P2>)>
    <COND (<==? .K ,ITEMKIND-FOOD>
           <FSET .OBJ ,EDIBLEBIT>
           <COND (<==? .ID ,FOOD-GRAPES> <FSET .OBJ ,PLURALBIT>)
                 (ELSE <FCLEAR .OBJ ,PLURALBIT>)>
           <COND (<OR <==? .ID ,FOOD-CHEESE>
                      <==? .ID ,FOOD-CAVIAR>
                      <==? .ID ,FOOD-GRAPES>>
                  <PUTP .OBJ ,P?ARTICLE "some">)
                 (ELSE <PUTP .OBJ ,P?ARTICLE <>>)>)
          (ELSE
           <FCLEAR .OBJ ,EDIBLEBIT>
           <FCLEAR .OBJ ,PLURALBIT>
           <PUTP .OBJ ,P?ARTICLE <>>)>>

<ADD-TELL-TOKENS
    ITEM-NAME * <PRINT-ITEM-NAME .X>>

<ROUTINE PRINT-ITEM-NAME (OBJ "AUX" K ID LVL ENCH)
    <COND (<NOT <RASCAL-ITEM? .OBJ>> <TELL D .OBJ> <RTRUE>)>
    <SET K <GETP .OBJ ,P?R-ITKIND>>
    <SET ID <GETP .OBJ ,P?R-ITID>>
    <SET LVL <GETP .OBJ ,P?R-ITLVL>>
    <SET ENCH <GETP .OBJ ,P?R-ITENCH>>
    <COND (<==? .K ,ITEMKIND-WEAPON>
           <TELL "level " N .LVL>
           <COND (<G? .ENCH 0> <TELL "+" N .ENCH>)>
           <TELL " " WEAPON-NAME .ID>)
          (<==? .K ,ITEMKIND-POTION> <TELL POTION-DISPLAY-NAME .ID>)
          (<==? .K ,ITEMKIND-FOOD> <TELL FOOD-NAME .ID>)
          (<==? .K ,ITEMKIND-TREASURE> <TELL TREASURE-NAME .ID>)
          (<==? .K ,ITEMKIND-KEY> <TELL KEY-NAME .ID>)
          (ELSE <TELL "item">)>>

<ROUTINE INTERIOR-EXIT-SYNC ("AUX" O N)
    ;"Move real inventory item objects back out of the parser world."

    ;"Pass 1: move real item objects back to PLAYER-INVENTORY."
    <SET O <FIRST? ,WINNER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <COND (<RASCAL-ITEM? .O> <MOVE .O ,PLAYER-INVENTORY>)>
        <SET O .N>>

    <RESYNC-DUNGEON-INVENTORY-SLOTS>

    ;"Defensive cleanup: if anything still managed to add an invalid entry, drop it now."
    <SANITIZE-DUNGEON-INVENTORY>

    ;"If the equipped weapon was dropped in the interior, clear the stale reference."
    <COND (<AND ,EQUIPPED-WEAPON <NOT <IN? ,EQUIPPED-WEAPON ,PLAYER-INVENTORY>>>
           <SETG EQUIPPED-WEAPON <>>
           <HONORS-NOTE-EQUIP-CHANGE>)>

    ;"If unarmed but now carrying a weapon, auto-wield the best one."
    <COND (<AND <NOT ,EQUIPPED-WEAPON> <G? <INV-COUNT> 0>>
           <AUTO-EQUIP-WEAPON>)>>

<ROUTINE INTERIOR-ENTER-SYNC ("AUX" O N)
    ;"Move real inventory item objects into WINNER so the parser treats them as carried."
    <SANITIZE-DUNGEON-INVENTORY>
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <MOVE .O ,WINNER>
        <SET-ITEM-VOCAB .O>
        <SET O .N>>>

"Interior printing overrides (item names)"

<BIND ((REDEFINE T))
    <ROUTINE PRINT-INDEF (OBJ "AUX" A K ID)
        <COND (<RASCAL-ITEM? .OBJ>
               <SET A <GETP .OBJ ,P?ARTICLE>>
               <COND (.A
                      <TELL .A>
                      <PRINTC !\ >)
                     (ELSE
                      <SET K <GETP .OBJ ,P?R-ITKIND>>
                      <SET ID <GETP .OBJ ,P?R-ITID>>
                      <COND (<==? .K ,ITEMKIND-POTION>
                             <TELL <POTION-ARTICLE .ID>>
                             <PRINTC !\ >)
                            (ELSE <TELL "a ">)>)>
               <PRINT-ITEM-NAME .OBJ>
               <RTRUE>)
              (<RASCAL-ENEMY? .OBJ>
               <TELL "a " <ENEMY-NAME <GETP .OBJ ,P?R-ETYPE>>>
               <RTRUE>)>
        <COND (<FSET? .OBJ ,NARTICLEBIT>)
              (<SET A <GETP .OBJ ,P?ARTICLE>>
               <TELL .A>
               <PRINTC !\ >)
              (<FSET? .OBJ ,PLURALBIT> <TELL "some ">)
              (<FSET? .OBJ ,VOWELBIT> <TELL "an ">)
              (ELSE <TELL "a ">)>
        <PRINTD .OBJ>>

    <ROUTINE PRINT-DEF (OBJ)
        <COND (<RASCAL-ITEM? .OBJ>
               <TELL "the ">
               <PRINT-ITEM-NAME .OBJ>
               <RTRUE>)
              (<RASCAL-ENEMY? .OBJ>
               <TELL "the " <ENEMY-NAME <GETP .OBJ ,P?R-ETYPE>>>
               <RTRUE>)>
        <COND (<NOT <FSET? .OBJ ,NARTICLEBIT>> <TELL "the ">)>
        <PRINTD .OBJ>>

    <ROUTINE PRINT-CINDEF (OBJ "AUX" A K ID)
        <COND (<RASCAL-ITEM? .OBJ>
               <SET A <GETP .OBJ ,P?ARTICLE>>
               <COND (.A
                      <PRINT-CAP-STR .A>
                      <PRINTC !\ >)
                     (ELSE
                      <SET K <GETP .OBJ ,P?R-ITKIND>>
                      <SET ID <GETP .OBJ ,P?R-ITID>>
                      <COND (<==? .K ,ITEMKIND-POTION>
                             <PRINT-CAP-STR <POTION-ARTICLE .ID>>
                             <PRINTC !\ >)
                            (ELSE <TELL "A ">)>)>
               <PRINT-ITEM-NAME .OBJ>
               <RTRUE>)
              (<RASCAL-ENEMY? .OBJ>
               <TELL "A " <ENEMY-NAME <GETP .OBJ ,P?R-ETYPE>>>
               <RTRUE>)>
        <COND (<FSET? .OBJ ,NARTICLEBIT> <PRINT-CAP-OBJ .OBJ> <RTRUE>)>
        <COND (<SET A <GETP .OBJ ,P?ARTICLE>>
               <PRINT-CAP-STR .A>
               <PRINTC !\ >)
              (<FSET? .OBJ ,PLURALBIT> <TELL "Some ">)
              (<FSET? .OBJ ,VOWELBIT> <TELL "An ">)
              (ELSE <TELL "A ">)>
        <PRINTD .OBJ>>

    <ROUTINE PRINT-CDEF (OBJ)
        <COND (<RASCAL-ITEM? .OBJ>
               <TELL "The ">
               <PRINT-ITEM-NAME .OBJ>
               <RTRUE>)
              (<RASCAL-ENEMY? .OBJ>
               <TELL "The " <ENEMY-NAME <GETP .OBJ ,P?R-ETYPE>>>
               <RTRUE>)>
        <COND (<FSET? .OBJ ,NARTICLEBIT>
               <PRINT-CAP-OBJ .OBJ>
               <RTRUE>)
              (ELSE <TELL "The " D .OBJ>)>>

    <ROUTINE PRINT-PLURAL (OBJ "AUX" K PT W E COLOR TYPE)
        <COND (<RASCAL-ITEM? .OBJ>
               <COND (<==? <SET K <GETP .OBJ ,P?R-ITKIND>> ,ITEMKIND-POTION>
                      <SET COLOR <GETP .OBJ ,P?R-ITID>>
                      <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>>
                             <TELL "potions">)
                            (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>
                             <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
                                 <TELL POTION-PLURAL-TYPE-NAME .TYPE>)
                                (ELSE <TELL POTION-COLOR-NAME .COLOR !\s>)>
                      <RTRUE>)
                     (<==? .K ,ITEMKIND-WEAPON>
                      <TELL "level " N <GETP .OBJ ,P?R-ITLVL>>
                      <COND (<G? <SET E <GETP .OBJ ,P?R-ITENCH>> 0> <TELL "+" N .E>)>
                      <TELL !\ >)>
               <COND (<AND <SET PT <GETPT .OBJ ,P?PLURAL>>
                           <SET W <GET .PT 0>>>
                      <TELL B .W>)
                     (ELSE <TELL "items">)>)
              (<SET W <GETP .OBJ ,P?PDESC>>
               <TELL .W>)
              (ELSE <TELL D .OBJ !\s>)>>>

<REPLACE-LIBRARY-MESSAGES PARSER
    (MANY-HEADER ITEM-NAME .OBJ ": ")>

"Interior parsing overrides"

;"We hook into the parser to replace sequences like 'LEVEL 1' or 'LEVEL 1 + 2'
  with an untypeable adjective, then assign the adjective to the weapon objects
  in scope with matching levels."

<CONSTANT WORD-LEVEL-ADJ1 <VOC ",LVL1" ADJ>>
<CONSTANT VAL-LEVEL-ADJ1 <VERSION? (ZIP A?\,LVL1) (ELSE W?\,LVL1)>>
<CONSTANT WORD-LEVEL-ADJ2 <VOC ",LVL2" ADJ>>
<CONSTANT VAL-LEVEL-ADJ2 <VERSION? (ZIP A?\,LVL2) (ELSE W?\,LVL2)>>
<CONSTANT VAL-DUMMY-ADJ <VERSION? (ZIP A?\,DUMMY-ADJ) (ELSE W?\,DUMMY-ADJ)>>
<GLOBAL USED-LEVEL-ADJ1? <>>

<REPLACE-DEFINITION HOOK-BEFORE-PARSER
    <ROUTINE HOOK-BEFORE-PARSER ("AUX" PT O N)
        ;"Reset magic adjective usage"
        <SETG USED-LEVEL-ADJ1? <>>

        <SET O <FIRST? ,WINNER>>
        <REPEAT ()
            <COND (<NOT .O> <RETURN>)>
            <SET N <NEXT? .O>>
            <COND (<RASCAL-ITEM? .O>
                   <SET PT <GETPT .O ,P?ADJECTIVE>>
                   ;"We only use the third adjective slot"
                   <COND (<==? <GET/B .PT 2> ,VAL-LEVEL-ADJ1 ,VAL-LEVEL-ADJ2>
                          <PUT/B .PT 2 ,VAL-DUMMY-ADJ>)>)>
            <SET O .N>>

        <SET O <FIRST? ,HERE>>
        <REPEAT ()
            <COND (<NOT .O> <RETURN>)>
            <SET N <NEXT? .O>>
            <COND (<RASCAL-ITEM? .O>
                   <SET PT <GETPT .O ,P?ADJECTIVE>>
                   ;"We only use the third adjective slot"
                   <COND (<==? <GET/B .PT 2> ,VAL-LEVEL-ADJ1 ,VAL-LEVEL-ADJ2>
                          <PUT/B .PT 2 ,VAL-DUMMY-ADJ>)>)>
            <SET O .N>>>>

<REPLACE-DEFINITION HOOK-MID-PARSE-CONSUME
    <ROUTINE HOOK-MID-PARSE-CONSUME (WN "AUX" LVL ENCH WDS A PT)
        ;"If the word at WN starts a level clause, then parse it, assign the
          magic adjective to the matching objects in scope, and consume it"
        <COND (<AND <L? .WN ,P-LEN>
                    <==? <GETWORD? .WN> %<VOC "LEVEL" ADJ>>
                    <PARSE-NUMBER? <+ .WN 1>>>
               <SET WDS 1>
               <SET LVL ,P-NUMBER>
               <COND (<AND <L=? .WN <- ,P-LEN 2>>
                           <==? <GETWORD? <+ .WN 2>> %<VOC "+" BUZZ>>
                           <PARSE-NUMBER? <+ .WN 3>>>
                      <SET WDS 3>
                      <SET ENCH ,P-NUMBER>)>
               <COND (<NOT ,USED-LEVEL-ADJ1?>
                      <SETG USED-LEVEL-ADJ1? T>
                      <SET A ,VAL-LEVEL-ADJ1>)
                     (ELSE <SET A ,VAL-LEVEL-ADJ2>)>
               ;"Assign it to matching objects"
               <MAP-CONTENTS (O ,WINNER)
                   <TRACE 3 "[hook: considering " T .O "]" CR>
                   <COND (<AND <RASCAL-ITEM? .O>
                               <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-WEAPON>
                               <==? <GETP .O ,P?R-ITLVL> .LVL>
                               <==? <GETP .O ,P?R-ITENCH> .ENCH>>
                          <TRACE 3 "[hook: setting adj on " T .O "]" CR>
                          <SET PT <GETPT .O ,P?ADJECTIVE>>
                          <PUT/B .PT 2 .A>)>>
               <MAP-CONTENTS (O ,HERE)
                   <TRACE 3 "[hook: considering " T .O "]" CR>
                   <COND (<AND <RASCAL-ITEM? .O>
                               <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-WEAPON>
                               <==? <GETP .O ,P?R-ITLVL> .LVL>
                               <==? <GETP .O ,P?R-ITENCH> .ENCH>>
                          <TRACE 3 "[hook: setting adj on " T .O "]" CR>
                          <SET PT <GETPT .O ,P?ADJECTIVE>>
                          <PUT/B .PT 2 .A>)>>
               ;"Replace it in LEXBUF, skipping .WDS words"
               <PUTWORD <+ .WN .WDS> .A>
               .WDS)>>>

"Interior action overrides"

<REMOVE-SYNTAX * = V-SAVE>
<REMOVE-SYNTAX * = V-RESTORE>

<BIND ((REDEFINE T))
    <ROUTINE V-READ ()
        <PERFORM ,V?EXAMINE ,PRSO>>>

<VERB-SYNONYM GIVE SHOW>

<SYNTAX ASK OBJECT (FIND PERSONBIT) ABOUT OBJECT = V-ASK-ABOUT>

<ROUTINE V-ASK-ABOUT ()
    <TELL CT ,PRSO " doesn't answer." CR>>

<SYNTAX HELLO OBJECT (FIND KLUDGEBIT) = V-HELLO>

<SYNTAX HELLO = V-HELLO>
<VERB-SYNONYM HELLO HI>

<SYNTAX SAY HELLO OBJECT (FIND PERSONBIT) = V-HELLO>

<ROUTINE V-HELLO ("AUX" (WHOM ,PRSO))
    <COND (<NOT .WHOM>
           <MAP-CONTENTS (I ,HERE)
                         <COND (<AND <FSET? .I ,PERSONBIT> <N==? .I ,WINNER>>
                                <SET WHOM .I>
                                <RETURN>)>>)>
    <COND (<AND .WHOM <FSET? .WHOM ,PERSONBIT> <N==? .WHOM ,WINNER>>
           <WITH-GLOBAL ((WINNER .WHOM)) <PERFORM ,V?HELLO>>)
          (ELSE <TELL "Pleased to meet you." CR>)>>

<SYNTAX GOODBYE OBJECT (FIND PERSONBIT) = V-GOODBYE>
<VERB-SYNONYM GOODBYE BYE>

<SYNTAX SAY GOODBYE OBJECT (FIND PERSONBIT) = V-GOODBYE>

<ROUTINE V-GOODBYE () <PERFORM ,V?EXIT>>

"Interior/dungeon interface"

<GLOBAL INTERIOR-CATCH-TOKEN <>>

;"Launches an interior by numeric ID.

This is the interface the roguelike uses so it doesn't need to reference parser
ROOM objects directly.

Args:
    ID: Interior ID (see INTERIOR-* constants).

Returns:
    T if launched, FALSE otherwise."

<ROUTINE LAUNCH-INTERIOR-ID (ID)
    <COND (<==? .ID ,INTERIOR-INFO-BOOTH>
           <LAUNCH-INTERIOR ,INFO-BOOTH>
           <RTRUE>)
          (<==? .ID ,INTERIOR-CARROT-FARM>
           <LAUNCH-INTERIOR ,CARROT-FARM>
           <RTRUE>)
          (<==? .ID ,INTERIOR-BEES>
           <LAUNCH-INTERIOR ,BEE-HIVE>
           <RTRUE>)
          (<==? .ID ,INTERIOR-BLACKSMITH>
           <LAUNCH-INTERIOR ,BLACKSMITH-SHOP>
           <RTRUE>)
          (<==? .ID ,INTERIOR-ORACLE-GROTTO>
           <LAUNCH-INTERIOR ,ORACLE-GROTTO>
           <RTRUE>)
          (<==? .ID ,INTERIOR-BUSKER>
           <LAUNCH-INTERIOR ,BUSKERS-CORNER>)
          (ELSE <RFALSE>)>>

<ROUTINE WRAP-PARSER-MAIN-LOOP ()
    <SETG INTERIOR-CATCH-TOKEN <CATCH>>
    <MAIN-LOOP>>

<ROUTINE CARROT? (OBJ)
    <OR <==? .OBJ ,CARROT-PATCH>
        <AND <FOOD? .OBJ> <==? <GETP .OBJ ,P?R-ITID> ,FOOD-CARROT>>>>

<ROUTINE FOOD? (OBJ)
    <AND <RASCAL-ITEM? .OBJ> <==? <GETP .OBJ ,P?R-ITKIND> ,ITEMKIND-FOOD>>>

<ROUTINE POTION? (OBJ)
    <AND <RASCAL-ITEM? .OBJ> <==? <GETP .OBJ ,P?R-ITKIND> ,ITEMKIND-POTION>>>

<ROUTINE WEAPON? (OBJ)
    <AND <RASCAL-ITEM? .OBJ> <==? <GETP .OBJ ,P?R-ITKIND> ,ITEMKIND-WEAPON>>>

<ROUTINE POSTER? (OBJ)
    <OR <==? .OBJ ,STACK-OF-POSTERS>
        <AND <TREASURE? .OBJ>
             <==? <GETP .OBJ ,P?R-ITID> ,TREASURE-POSTER>>>>

<ROUTINE TREASURE? (OBJ)
    <AND <RASCAL-ITEM? .OBJ> <==? <GETP .OBJ ,P?R-ITKIND> ,ITEMKIND-TREASURE>>>
