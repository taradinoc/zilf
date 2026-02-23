"Combat & Enemy Management"


<CONSTANT ETYPE-GOBLIN 1>
<CONSTANT ETYPE-SPHINX 2>
<CONSTANT ETYPE-KRAKEN 3>
<CONSTANT ETYPE-DRAGON 4>
<CONSTANT ETYPE-WRAITH 5>
<CONSTANT ETYPE-LEGION 6> ;"bees"
<CONSTANT ETYPE-MONKEY 7>
<CONSTANT ETYPE-SPIRIT 8>
<CONSTANT ETYPE-COUNT 8>

;"Set by PLAYER-DAMAGE: T if the last player attack roll crit."

<GLOBAL LAST-HIT-CRIT? <>>

;"One-frame combat feedback: when an enemy damages the player, the next draw
    inverts the player sprite and draws the attacker in red."

<GLOBAL HIT-FLASH? <>>
<GLOBAL HIT-FLASH-EX 0>
<GLOBAL HIT-FLASH-EY 0>

<GLOBAL PLAYER-MAX-HP ,INITIAL-PLAYER-MAX-HP>
<GLOBAL PLAYER-HP ,INITIAL-PLAYER-MAX-HP>

<GLOBAL PLAYER-STR ,INITIAL-PLAYER-STR>

<GLOBAL PLAYER-DEF ,INITIAL-PLAYER-DEF>

;"Enemy objects live as children under each FLOOR-OBJ. Active enemies are
  RASCAL-ENEMY objects whose R-ETYPE property is > 0."

<ROUTINE ENEMY-OBJ-OF-TYPE (CONTAINER TYPE "AUX" O T)
    <SET O <FIRST? .CONTAINER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET T <GETP .O ,P?R-ETYPE>>
        <COND (<AND <G? .T 0> <==? .T .TYPE>> <RETURN .O>)>
        <SET O <NEXT? .O>>>>

<ROUTINE DESPAWN-ENEMY-OBJ (O "AUX" X Y)
    <COND (<NOT .O> <RTRUE>)>
    <SET X <GETP .O ,P?R-X>>
    <SET Y <GETP .O ,P?R-Y>>
    <FREE-RASCAL-ENEMY .O>
    <COND (<AND <G? .X 0> <G? .Y 0> <ENEMY-VISIBLE? .X .Y>> <MARK-DIRTY .X .Y>)>
    <RTRUE>>

;"Returns a per-floor scaling bonus for an enemy type.

This is the shared scaling used for both enemy HP and gold drops.

Args:
    TYPE: Enemy type code (ETYPE-*).
    F: Floor number (1-based).

Returns:
    Scaling bonus value (non-negative integer)."

<ROUTINE ENEMY-FLOOR-BONUS (TYPE F)
    <COND (<==? .TYPE ,ETYPE-GOBLIN> </ .F ,ENEMY-SCALING-DIVISOR-EASY>)
          (<==? .TYPE ,ETYPE-SPHINX ,ETYPE-WRAITH> </ .F ENEMY-SCALING-DIVISOR-MEDIUM>)
          (<==? .TYPE ,ETYPE-KRAKEN ,ETYPE-DRAGON ,ETYPE-SPIRIT> </ .F ,ENEMY-SCALING-DIVISOR-HARD>)
          (ELSE </ .F ,ENEMY-SCALING-DIVISOR-EASY>)>>

;"Maps an enemy type code to its display name.

Args:
  TYPE: Enemy type code (ETYPE-*).

Returns:
  Lowercase name string."

<ROUTINE ENEMY-NAME (TYPE)
    <COND (<==? .TYPE ,ETYPE-SPHINX> "sphinx")
          (<==? .TYPE ,ETYPE-KRAKEN> "kraken")
          (<==? .TYPE ,ETYPE-DRAGON> "dragon")
          (<==? .TYPE ,ETYPE-WRAITH> "wraith")
          (<==? .TYPE ,ETYPE-LEGION> "legion")
          (<==? .TYPE ,ETYPE-MONKEY> "monkey")
          (<==? .TYPE ,ETYPE-SPIRIT> "spirit")
          (ELSE "goblin")>>

;"Maps an enemy type code to its map sprite.

Args:
  TYPE: Enemy type code (ETYPE-*).

Returns:
  ZSCII tile constant (TILE-*)."

<ROUTINE ENEMY-TILE-FOR-TYPE (TYPE)
    <COND (<==? .TYPE ,ETYPE-SPHINX> ,TILE-SPHINX)
          (<==? .TYPE ,ETYPE-KRAKEN> ,TILE-KRAKEN)
          (<==? .TYPE ,ETYPE-DRAGON> ,TILE-DRAGON)
          (<==? .TYPE ,ETYPE-WRAITH> ,TILE-WRAITH)
          (<==? .TYPE ,ETYPE-LEGION> ,TILE-BEES)
          (<==? .TYPE ,ETYPE-MONKEY> ,TILE-MONKEY)
          (<==? .TYPE ,ETYPE-SPIRIT> ,TILE-SPIRIT)
          (ELSE ,TILE-GOBLIN)>>

<IF-DEBUG
    <ROUTINE APPLY-IMMORTAL ()
        <COND (,IMMORTAL? <SETG PLAYER-HP ,PLAYER-MAX-HP>)>
        <RTRUE>>>

"Monkey enemy"

<CONSTANT MONKEY-MIN-FLOOR 2>
<CONSTANT MONKEY-MAX-FLOOR 24>

<ROUTINE MONKEY-ON-FLOOR? (F)
    <ENEMY-OBJ-OF-TYPE <FLOOR-OBJ .F> ,ETYPE-MONKEY>>

<DEFMAC ENEMY-CARRIED-ITEM ('ENEMY)
    `<FIRST? ~.ENEMY>>

<ROUTINE ENEMY-CARRIES-BANANA? (ENEMY "AUX" IT)
    <AND <SET IT <ENEMY-CARRIED-ITEM .ENEMY>>
         <==? <GETP .IT ,P?R-ITKIND> ,ITEMKIND-FOOD>
         <==? <GETP .IT ,P?R-ITID> ,FOOD-BANANA>>>

<ROUTINE BANANA-OBJ-AT (X Y "AUX" O)
    <SET O <FOOD-OBJ-AT .X .Y>>
    <COND (<AND <G? .O 0> <==? <GETP .O ,P?R-ITID> ,FOOD-BANANA>> .O) (ELSE 0)>>

<ROUTINE BANANA-OBJ-IN-RANGE (X Y R "AUX" O K ID PX PY DIST)
    <SET O <FIRST? <FLOOR-OBJ ,CURRENT-FLOOR>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-FOOD>
               <SET ID <GETP .O ,P?R-ITID>>
               <COND (<==? .ID ,FOOD-BANANA>
                      <SET PX <GETP .O ,P?R-X>>
                      <SET PY <GETP .O ,P?R-Y>>
                      <SET DIST <+ <ABS <- .PX .X>> <ABS <- .PY .Y>>>>
                      <COND (<L=? .DIST .R> <RETURN .O>)>)>)>
        <SET O <NEXT? .O>>>>

;"Approximates checking whether a monkey can see the player.

Args:
  O: The monkey.

Returns:
  T if the monkey is on a revealed tile and it has the same ROOMID as the player's tile."
<ROUTINE MONKEY-CAN-SEE-PLAYER? (O "AUX" EX EY RID)
    <SET EX <GETP .O ,P?R-X>>
    <SET EY <GETP .O ,P?R-Y>>
    <SET RID <ROOMID-AT .EX .EY>>
    <AND <G? .RID 0>
         <==? .RID <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
         <REVEALED? .EX .EY>>>

<ROUTINE MONKEY-STEAL-ONE (ENEMY "AUX" K PC OC PICK AMT)
    <SET PC 0>
    <SET OC 0>
    <SET PICK 0>
    <MAP-CONTENTS (O ,PLAYER-INVENTORY)
    <COND (<AND ,EQUIPPED-WEAPON <==? .O ,EQUIPPED-WEAPON>>)
        (ELSE
         <SET K <GETP .O ,P?R-ITKIND>>
         <COND (<OR <==? .K ,ITEMKIND-FOOD> <==? .K ,ITEMKIND-TREASURE>>
            <SET PC <+ .PC 1>>
            <COND (<==? <RNG .PC> 1> <SET PICK .O>)>)
           (ELSE
            <SET OC <+ .OC 1>>
            <COND (<AND <L=? .PC 0> <==? <RNG .OC> 1>> <SET PICK .O>)>)>)>>

    <COND (<L=? .PICK 0>
           <COND (<G? ,PLAYER-GOLD 0>
                  <SET AMT <+ 4 <RNG 21>>>
                  <COND (<G? .AMT ,PLAYER-GOLD> <SET AMT ,PLAYER-GOLD>)>
                  <COND (<NOT <SET PICK <ALLOC-RASCAL-ITEM>>> <RETURN 0>)>
                  <PUTP .PICK ,P?R-ITKIND ,ITEMKIND-GOLD>
                  <PUTP .PICK ,P?R-ITAMT .AMT>
                  <PUTP .PICK ,P?R-X 0>
                  <PUTP .PICK ,P?R-Y 0>
                  <SETG PLAYER-GOLD <- ,PLAYER-GOLD .AMT>>
                  <MOVE .PICK .ENEMY>
                  <LOG "The monkey steals " N .AMT " gold!" CR>
                  <RETURN .PICK>)
                 (ELSE <RETURN 0>)>)>

    <REMOVE .PICK>
    <MOVE .PICK .ENEMY>

    <LOG "The monkey steals your " ITEM-NAME .PICK "!" CR>

    .PICK>

<ROUTINE STEP-ENEMY-AWAY-PREFERRED (I TX TY "AUX" EX EY DX DY SX SY)
    <SET EX <GETP .I ,P?R-X>>
    <SET EY <GETP .I ,P?R-Y>>
    <SET DX <- .TX .EX>>
    <SET DY <- .TY .EY>>
    <SET SX <COND (<L? .DX 0> 1) (<G? .DX 0> -1) (ELSE 0)>>
    <SET SY <COND (<L? .DY 0> 1) (<G? .DY 0> -1) (ELSE 0)>>

    ;"Directly away"
    <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> <+ .EY .SY>> <RTRUE>)>

    ;"Diagonally away"
    <COND (<AND <N==? .SX 0> <N==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> .EY> <RTRUE>)>
           <COND (<TRY-ENEMY-MOVE .I .EX <+ .EY .SY>> <RTRUE>)>)>
    <COND (<AND <N==? .SX 0> <==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> <+ .EY 1>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <+ .EX .SX> <- .EY 1>> <RTRUE>)>)>
    <COND (<AND <==? .SX 0> <N==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX 1> <+ .EY .SY>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <- .EX 1> <+ .EY .SY>> <RTRUE>)>)>

    ;"Sideways (not directly toward)"
    <COND (<AND <N==? .SX 0> <==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I .EX <+ .EY 1>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I .EX <- .EY 1>> <RTRUE>)>)>
    <COND (<AND <==? .SX 0> <N==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX 1> .EY> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <- .EX 1> .EY> <RTRUE>)>)>

    ;"Diagonally toward (but not directly toward)"
    <COND (<AND <N==? .SX 0> <N==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> <- .EY .SY>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <- .EX .SX> <+ .EY .SY>> <RTRUE>)>)>
    <COND (<AND <N==? .SX 0> <==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <- .EX .SX> <+ .EY 1>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <- .EX .SX> <- .EY 1>> <RTRUE>)>)>
    <COND (<AND <==? .SX 0> <N==? .SY 0>>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX 1> <- .EY .SY>> <RTRUE>)
                 (<TRY-ENEMY-MOVE .I <- .EX 1> <- .EY .SY>> <RTRUE>)>)>

    <RFALSE>>

"Bee swarm (enemy)"

<ROUTINE BEE-ENEMY-OBJ-FOR-FLOOR (F)
    <ENEMY-OBJ-OF-TYPE <FLOOR-OBJ .F> ,ETYPE-LEGION>>

<ROUTINE BEE-SPAWN-CANDIDATE? (X Y)
    <AND <IN-BOUNDS? .X .Y>
         <FLOOR? .X .Y>
         <NOT <AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>>
         <L=? <ENEMY-AT .X .Y> 0>
         <NOT <TRADER-AT? .X .Y>>>>

<ROUTINE SPAWN-BEE-ENEMY (X Y "AUX" O)
    <COND (<NOT <SET O <ALLOC-RASCAL-ENEMY>>> <RFALSE>)>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <PUTP .O ,P?R-ETYPE ,ETYPE-LEGION>
    <PUTP .O ,P?R-EHP 50>
    <MOVE .O ,CURRENT-FLOOR-OBJ>
    <RTRUE>>

<ROUTINE START-BEE-SWARM (F X Y "AUX" OLD O)
    ;"Schedule spawn on the following turn, so the swarm visibly lags behind the player."
    <SET OLD ,BEE-SWARM-FLOOR>
    <COND (<G? .OLD 0>
           <SET O <BEE-ENEMY-OBJ-FOR-FLOOR .OLD>>
           <COND (<G? .O 0> <DESPAWN-ENEMY-OBJ .O>)>)>
    <SET O <BEE-ENEMY-OBJ-FOR-FLOOR .F>>
    <COND (<G? .O 0> <DESPAWN-ENEMY-OBJ .O>)>
    <SETG BEE-SWARM-ON? <>>
    <SETG BEE-SWARM-FLOOR .F>
    <SETG BEE-SWARM-X .X>
    <SETG BEE-SWARM-Y .Y>
    <SETG BEE-SWARM-PENDING? T>
    <SETG BEE-SWARM-PEND-FLOOR .F>
    <SETG BEE-SWARM-PEND-X .X>
    <SETG BEE-SWARM-PEND-Y .Y>
    <SETG BEE-SWARM-PEND-DELAY 1>
    <RTRUE>>

<ROUTINE UPDATE-BEE-SWARM ("AUX" O DIST SX SY)
    <COND (,BEE-SWARM-PENDING?
           <COND (<N==? ,CURRENT-FLOOR ,BEE-SWARM-PEND-FLOOR>
                  ;"Changed floors before the swarm could emerge."
                  <SETG BEE-SWARM-PENDING? <>>
                  <SETG BEE-SWARM-PEND-DELAY 0>
                  <RTRUE>)>
           <COND (<G? ,BEE-SWARM-PEND-DELAY 0>
                  <SETG BEE-SWARM-PEND-DELAY <- ,BEE-SWARM-PEND-DELAY 1>>
                  <RTRUE>)>
           ;"Try to spawn at the hive entrance tile; if blocked, try adjacent tiles."
           <SET SX 0>
           <SET SY 0>
           <COND (<BEE-SPAWN-CANDIDATE? ,BEE-SWARM-PEND-X ,BEE-SWARM-PEND-Y>
                  <SET SX ,BEE-SWARM-PEND-X>
                  <SET SY ,BEE-SWARM-PEND-Y>)
                 (<BEE-SPAWN-CANDIDATE? <- ,BEE-SWARM-PEND-X 1>
                                        ,BEE-SWARM-PEND-Y>
                  <SET SX <- ,BEE-SWARM-PEND-X 1>>
                  <SET SY ,BEE-SWARM-PEND-Y>)
                 (<BEE-SPAWN-CANDIDATE? <+ ,BEE-SWARM-PEND-X 1>
                                        ,BEE-SWARM-PEND-Y>
                  <SET SX <+ ,BEE-SWARM-PEND-X 1>>
                  <SET SY ,BEE-SWARM-PEND-Y>)
                 (<BEE-SPAWN-CANDIDATE? ,BEE-SWARM-PEND-X
                                        <- ,BEE-SWARM-PEND-Y 1>>
                  <SET SX ,BEE-SWARM-PEND-X>
                  <SET SY <- ,BEE-SWARM-PEND-Y 1>>)
                 (<BEE-SPAWN-CANDIDATE? ,BEE-SWARM-PEND-X
                                        <+ ,BEE-SWARM-PEND-Y 1>>
                  <SET SX ,BEE-SWARM-PEND-X>
                  <SET SY <+ ,BEE-SWARM-PEND-Y 1>>)>
           <COND (<AND <G? .SX 0> <G? .SY 0> <SPAWN-BEE-ENEMY .SX .SY>>
                  <SETG BEE-SWARM-PENDING? <>>
                  <SETG BEE-SWARM-ON? T>
                  <LOG "A legion of bees pours out after you!" CR>)
                 (ELSE
                  ;"No room to spawn yet (or no enemy slots). Try again next turn."
                  <RTRUE>)>
           <RTRUE>)>

    <COND (<N==? ,CURRENT-FLOOR ,BEE-SWARM-FLOOR>
           <SET O <BEE-ENEMY-OBJ-FOR-FLOOR ,BEE-SWARM-FLOOR>>
           <COND (<G? .O 0> <DESPAWN-ENEMY-OBJ .O>)>
           <SETG BEE-SWARM-ON? <>>
           <RTRUE>)>

    <SET O <BEE-ENEMY-OBJ-FOR-FLOOR ,CURRENT-FLOOR>>
    <COND (<L=? .O 0> <SETG BEE-SWARM-ON? <>> <RTRUE>)>
    <SET DIST
         <+ <ABS <- ,PLAYER-X ,BEE-SWARM-X>> <ABS <- ,PLAYER-Y ,BEE-SWARM-Y>>>>
    <COND (<G=? .DIST ,BEE-SWARM-LOSE-DIST>
           <DESPAWN-ENEMY-OBJ .O>
           <SETG BEE-SWARM-ON? <>>
           <LOG "You finally lose the legion of bees." CR>
           <RTRUE>)>
    <RTRUE>>

;"Consumes a poison potion when the legion reaches it.

    This removes the potion from the floor and discovers its color (identifying
    it) if it wasn't already discovered."

<ROUTINE BEE-CONSUME-POISON-POTION (ENEMY-OBJ POTION-OBJ "AUX" COLOR DISC TYPE)
    <SET COLOR <GETP .POTION-OBJ ,P?R-ITID>>
    <SET DISC <G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>>
    <LOG "The legion of bees swarms the potion and dies." CR>
    <COND (<NOT .DISC>
           <PUTB ,POTION-DISCOVERED <- .COLOR 1> 1>
           <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
           <LOG "You discover it was a " <POTION-TYPE-NAME .TYPE> "." CR>)>
    <REMOVE-POTION-OBJ .POTION-OBJ>
    <DESPAWN-ENEMY-OBJ .ENEMY-OBJ>
    <RTRUE>>

"Combat and items (gold + food)"

;"Computes the player's per-point miss chance from defense.

Formula:
       miss% = (75*DEF)/(10+DEF)

Returns:
       Integer percent (0..75)."

<ROUTINE PLAYER-DEF-MISS-PCT (DEF "AUX" DEN NUM)
    <COND (<L=? .DEF 0> <RETURN 0>)>
    <SET DEN <+ ,DEFENSE-FORMULA-DENOMINATOR .DEF>>
    ;"Round to nearest percent: add half the denominator before dividing."
    <SET NUM <+ <* ,DEFENSE-FORMULA-NUMERATOR .DEF> </ .DEN 2>>>
    </ .NUM .DEN>>

;"Applies defense to incoming damage.

The miss chance is evaluated independently for each point of damage.

Args:
       DMG: Incoming damage (>=0)

Returns:
       Damage after defense (>=0)."

<ROUTINE APPLY-PLAYER-DEFENSE (DMG "AUX" PCT BLOCKED)
    <COND (<L=? .DMG 0> <RETURN 0>)>
    <SET PCT <PLAYER-DEF-MISS-PCT ,PLAYER-DEF>>
    <COND (<L=? .PCT 0> <RETURN .DMG>)>
    <SET BLOCKED 0>
    <DO (I 1 .DMG) <COND (<L=? <RNG 100> .PCT> <SET BLOCKED <+ .BLOCKED 1>>)>>
    <- .DMG .BLOCKED>>

<ROUTINE PLAYER-DAMAGE ("AUX" S TYPE LVL EXCESS POWER DIV RANGE MIN PCT DMG)
    <SETG LAST-HIT-CRIT? <>>
    <SET S ,PLAYER-STR>
    <COND (<L=? .S 1> <SET S 1>)>
    <COND (<AND ,EQUIPPED-WEAPON
                <==? <GETP ,EQUIPPED-WEAPON ,P?R-ITKIND> ,ITEMKIND-WEAPON>
                <G=? <+ .S <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH>>
                     <GETP ,EQUIPPED-WEAPON ,P?R-ITLVL>>>
           ;"Weapon roll: a predictable-vs-swingy distribution per weapon type,
             with a chance to crit for 2x damage (dagger/waraxe highest).
             If STR exceeds the weapon's level requirement, the excess boosts damage."
           <SET TYPE <GETP ,EQUIPPED-WEAPON ,P?R-ITID>>
           <SET LVL <GETP ,EQUIPPED-WEAPON ,P?R-ITLVL>>
           <SET EXCESS <- <+ .S <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH>> .LVL>>
           <COND (<L? .EXCESS 0> <SET EXCESS 0>)>
           <SET POWER <+ <WEAPON-BASE-DMG .TYPE> .LVL </ .EXCESS 2>>>
           <SET DIV <WEAPON-VARIANCE-DIV .TYPE>>
           <COND (<L=? .DIV 1> <SET RANGE .POWER>)
                 (ELSE <SET RANGE </ .POWER .DIV>>)>
           <COND (<L=? .RANGE 1> <SET RANGE 2>)>
           <SET MIN <+ <- .POWER .RANGE> 1>>
           <COND (<L=? .MIN 1> <SET MIN 1>)>
           <SET DMG <+ <- .MIN 1> <RNG .RANGE>>>
           <SET PCT <WEAPON-CRIT-PCT .TYPE>>
           <COND (<AND <G? .PCT 0> <L=? <RNG 100> .PCT>>
                  <SETG LAST-HIT-CRIT? T>
                  <SET DMG <* .DMG 2>>)>
           .DMG)
          (ELSE
           ;"Fists: 1..STR (slightly weaker than the old STR+1)."
           <RNG .S>)>>

<ROUTINE PLAYER-CRIT-MSG ("AUX" TYPE)
    <COND (<AND ,EQUIPPED-WEAPON
                <==? <GETP ,EQUIPPED-WEAPON ,P?R-ITKIND> ,ITEMKIND-WEAPON>>
           <SET TYPE <GETP ,EQUIPPED-WEAPON ,P?R-ITID>>
           <COND (<==? .TYPE ,WEAPON-DAGGER> "(A ROGUEISH BLOW!!)")
                 (<==? .TYPE ,WEAPON-KATANA> "(A SAMURAI-LIKE BLOW!!)")
                 (<==? .TYPE ,WEAPON-WARAXE> "(A BYZANTINE BLOW!!)")
                 (<==? .TYPE ,WEAPON-SCYTHE> "(A REAPER-LIKE BLOW!!)")
                 (<==? .TYPE ,WEAPON-CUDGEL> "(A STOUT BLOW!!)")
                 (<==? .TYPE ,WEAPON-HAMMER> "(A THOR-LIKE BLOW!!)")
                 (ELSE "(A CRITICAL BLOW!!)")>)
          (ELSE "(A CRITICAL BLOW!!)")>>

<ROUTINE ENEMY-DAMAGE-RAND (TYPE "AUX" BASE J DMG)
    <SET BASE <ENEMY-DAMAGE .TYPE>>
    ;"J is -1, 0, or 1."
    <SET J <- <RNG 3> 2>>
    <SET DMG <+ .BASE .J>>
    <COND (<L=? .DMG 1> 1) (ELSE .DMG)>>

<ROUTINE ENEMY-DAMAGE (TYPE)
    <COND (<==? .TYPE ,ETYPE-GOBLIN> ,ENEMY-BASE-DMG-GOBLIN)
          (<==? .TYPE ,ETYPE-SPHINX> ,ENEMY-BASE-DMG-SPHINX)
          (<==? .TYPE ,ETYPE-WRAITH> ,ENEMY-BASE-DMG-WRAITH)
          (<==? .TYPE ,ETYPE-KRAKEN> ,ENEMY-BASE-DMG-KRAKEN)
          (<==? .TYPE ,ETYPE-DRAGON> ,ENEMY-BASE-DMG-DRAGON)
          (<==? .TYPE ,ETYPE-SPIRIT> ,ENEMY-BASE-DMG-SPIRIT)
          (<==? .TYPE ,ETYPE-LEGION> ,ENEMY-BASE-DMG-LEGION)
          (ELSE 1)>>

;"Applies damage to the player and enqueues a message.

Args:
  DMG: Damage amount (positive integer).

Returns:
  T."

<ROUTINE ENEMY-ATTACK-PLAYER (O "AUX" TYPE RAW DMG)
    <COND (<NOT .O> <RTRUE>)>
    <SET TYPE <GETP .O ,P?R-ETYPE>>
    <SETG STATS-ENEMY-ATTACKS <+ ,STATS-ENEMY-ATTACKS 1>>
    <SET RAW <ENEMY-DAMAGE-RAND .TYPE>>
    <SET DMG <APPLY-PLAYER-DEFENSE .RAW>>
    <COND (<G? .DMG ,STATS-BIGGEST-HIT-TAKEN>
           <SETG STATS-BIGGEST-HIT-TAKEN .DMG>)>
    <SETG STATS-DMG-TAKEN <+ ,STATS-DMG-TAKEN .DMG>>
    <COND (<G? .RAW .DMG>
           <SETG STATS-DMG-BLOCKED-BY-DEF
               <+ ,STATS-DMG-BLOCKED-BY-DEF <- .RAW .DMG>>>)>
    <SETG PLAYER-HP <- ,PLAYER-HP .DMG>>
    <COND (<G? .DMG 0>
           <SETG HIT-FLASH? T>
           <SETG HIT-FLASH-EX <GETP .O ,P?R-X>>
           <SETG HIT-FLASH-EY <GETP .O ,P?R-Y>>
           <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <MARK-DIRTY ,HIT-FLASH-EX ,HIT-FLASH-EY>
           <LOG "The "
                <ENEMY-NAME .TYPE>
                " hits you, dealing "
                N
                .DMG
                " damage."
                CR>)
          (ELSE <LOG "The " <ENEMY-NAME .TYPE> " attacks you and misses." CR>)>
    <COND (<L=? ,PLAYER-HP 0> <SETG PLAYER-HP 0> <SETG GAME-OVER? T>)>
    <RTRUE>>

;"Attacks a target enemy. If it dies, drops gold and enqueues a
    kill message; otherwise enqueues a hit message.

Args:
    (none)

Returns:
    I: Enemy slot number (1..MAX-ENEMIES).

Returns:
    T if an attack was performed; FALSE if no enemy exists in that slot."

<ROUTINE PLAYER-ATTACK (O "AUX" HP EX EY AMT ETYPE DMG LOOT)
    <COND (<NOT .O> <RFALSE>)>
    <SET HP <GETP .O ,P?R-EHP>>
    <COND (<L=? .HP 0> <RFALSE>)>
    <SETG STATS-PLAYER-ATTACKS <+ ,STATS-PLAYER-ATTACKS 1>>
    <SET ETYPE <GETP .O ,P?R-ETYPE>>
    <COND (<==? .ETYPE ,ETYPE-LEGION>
           <LOG "You swing at the legion of bees, but it won't disperse." CR>
           <RTRUE>)>
    <SET EX <GETP .O ,P?R-X>>
    <SET EY <GETP .O ,P?R-Y>>
    <SET DMG <PLAYER-DAMAGE>>
    <COND (<G? .DMG ,STATS-BIGGEST-HIT-DEALT>
           <SETG STATS-BIGGEST-HIT-DEALT .DMG>)>
    <COND (,LAST-HIT-CRIT? <SETG STATS-PLAYER-CRITS <+ ,STATS-PLAYER-CRITS 1>>)>
    <SETG STATS-DMG-DEALT <+ ,STATS-DMG-DEALT .DMG>>
    <SET HP <- .HP .DMG>>
    <PUTP .O ,P?R-EHP .HP>
    <COND (<L=? .HP 0>
           <COND (<AND <G? .ETYPE 0> <L=? .ETYPE ,ETYPE-COUNT>>
                  <STATS-INC-WORD-TABLE ,STATS-ENEMIES-KILLED <- .ETYPE 1>>)>
           <COND (<==? .ETYPE ,ETYPE-MONKEY>
                  <COND (<G? <SET LOOT <ENEMY-CARRIED-ITEM .O>> 0>
                         <DROP-EXISTING-ITEM-NEAR .LOOT .EX .EY>)>
                  <COND (<L? ,CURRENT-FLOOR ,MAX-FLOORS>
                         <PUTB ,PENDING-SPIRIT-SPAWNS
                               ,CURRENT-FLOOR
                               <+ 1
                                  <GETB ,PENDING-SPIRIT-SPAWNS
                                        ,CURRENT-FLOOR>>>)>)>
           <SET AMT <+ 4 <RNG 16> <ENEMY-FLOOR-BONUS .ETYPE ,CURRENT-FLOOR>>>
           <COND (<G? .AMT 255> <SET AMT 255>)>
           <DROP-GOLD-NEAR .EX .EY .AMT>
           <COND (<AND <==? .ETYPE ,ETYPE-DRAGON> <==? <RNG 4> 1>>
                  <DROP-KEY-NEAR .EX .EY <RNG ,LOCK-TYPE-COUNT>>)>
           ;"Occasional item drop as loot. If an item drops: 50% weapon, 25% potion, 25% food."
           <COND (<L=? <RNG 100> ,ITEM-ON-KILL-DROP-PCT>
                  <SET LOOT <RNG 4>>
                  <COND (<==? .LOOT 1>
                         <COND (<DROP-POTION-NEAR .EX
                                                  .EY
                                                  <ROLL-LOOT-POTION-COLOR>>)
                               (ELSE
                                <DROP-WEAPON-NEAR .EX
                                                  .EY
                                                  <RNG ,WEAPON-COUNT>
                                                  <ROLL-LOOT-WEAPON-LEVEL ,CURRENT-FLOOR>
                                                  <ROLL-LOOT-WEAPON-ENCH>>)>)
                        (<==? .LOOT 2>
                         <COND (<DROP-FOOD-NEAR .EX
                                                .EY
                                                <PICK-FOOD-TYPE ,CURRENT-FLOOR>>)
                               (ELSE
                                <DROP-WEAPON-NEAR .EX
                                                  .EY
                                                  <RNG ,WEAPON-COUNT>
                                                  <ROLL-LOOT-WEAPON-LEVEL ,CURRENT-FLOOR>
                                                  <ROLL-LOOT-WEAPON-ENCH>>)>)
                        (ELSE
                         <DROP-WEAPON-NEAR .EX
                                           .EY
                                           <RNG ,WEAPON-COUNT>
                                           <ROLL-LOOT-WEAPON-LEVEL ,CURRENT-FLOOR>
                                           <ROLL-LOOT-WEAPON-ENCH>>)>)>
           <DESPAWN-ENEMY-OBJ .O>
           <COND (,LAST-HIT-CRIT?
                  <LOG "You hit the " <ENEMY-NAME .ETYPE> " for " N .DMG " damage and kill it. "
                       <PLAYER-CRIT-MSG> CR>)
                 (ELSE
                  <LOG "You hit the " <ENEMY-NAME .ETYPE> " for " N .DMG " damage and kill it." CR>)>)
          (ELSE
           <COND (,LAST-HIT-CRIT?
                  <LOG "You hit the " <ENEMY-NAME .ETYPE> " for " N .DMG " damage. "
                       <PLAYER-CRIT-MSG> CR>)
                 (ELSE
                  <LOG "You hit the " <ENEMY-NAME .ETYPE> " for " N .DMG " damage." CR>)>)>
    <RTRUE>>

"Enemy movement + AI"

;"Attempts to move enemy slot I to (NX, NY) if the tile is walkable and not
  occupied by another enemy or the player.

Args:
  I: Enemy slot number (1..MAX-ENEMIES).
  NX, NY: Destination coordinates.

Returns:
  T if moved; FALSE otherwise."

<ROUTINE TRY-ENEMY-MOVE (I NX NY "AUX" EX EY)
    <COND (<NOT <IN-BOUNDS? .NX .NY>> <RFALSE>)>
    <COND (<NOT <FLOOR? .NX .NY>> <RFALSE>)>
    <COND (<TRADER-AT? .NX .NY> <RFALSE>)>
    <COND (<G? <ENEMY-AT .NX .NY> 0> <RFALSE>)>
    <COND (<AND <==? .NX ,PLAYER-X> <==? .NY ,PLAYER-Y>> <RFALSE>)>
    <SET EX <GETP .I ,P?R-X>>
    <SET EY <GETP .I ,P?R-Y>>
    <PUTP .I ,P?R-X .NX>
    <PUTP .I ,P?R-Y .NY>
    <COND (<AND <G? .EX 0> <G? .EY 0> <ENEMY-VISIBLE? .EX .EY>>
           <MARK-DIRTY .EX .EY>)>
    <COND (<ENEMY-VISIBLE? .NX .NY> <MARK-DIRTY .NX .NY>)>
    <RTRUE>>

;"Attempts to step enemy slot I one tile toward (TX, TY), using the same
chase movement logic as STEP-ENEMIES.

Args:
  I: Enemy slot number (1..MAX-ENEMIES).
  TX, TY: Target coordinates.

Returns:
  T if moved; FALSE otherwise."

<ROUTINE STEP-ENEMY-TOWARD (I TX TY "AUX" EX EY DX DY ADX ADY SX SY NX NY)
    <SET EX <GETP .I ,P?R-X>>
    <SET EY <GETP .I ,P?R-Y>>
    <SET DX <- .TX .EX>>
    <SET DY <- .TY .EY>>
    <SET ADX <ABS .DX>>
    <SET ADY <ABS .DY>>
    <SET SX <COND (<L? .DX 0> -1) (<G? .DX 0> 1) (ELSE 0)>>
    <SET SY <COND (<L? .DY 0> -1) (<G? .DY 0> 1) (ELSE 0)>>
    <SET NX <+ .EX .SX>>
    <SET NY <+ .EY .SY>>
    <COND (<TRY-ENEMY-MOVE .I .NX .NY> <RTRUE>)>
    <COND (<G? .ADX .ADY>
           <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> .EY> <RTRUE>)>
           <COND (<TRY-ENEMY-MOVE .I .EX <+ .EY .SY>> <RTRUE>)>
           <RFALSE>)>
    <COND (<TRY-ENEMY-MOVE .I .EX <+ .EY .SY>> <RTRUE>)>
    <COND (<TRY-ENEMY-MOVE .I <+ .EX .SX> .EY> <RTRUE>)>
    <RFALSE>>

<ROUTINE ENEMY-RANDOM-MOVE (O EX EY "AUX" CHOICE DX DY)
    <SET CHOICE <RNG 9>>
    <SET DX <DIR9-DX .CHOICE>>
    <SET DY <DIR9-DY .CHOICE>>
    <TRY-ENEMY-MOVE .O <+ .EX .DX> <+ .EY .DY>>>

<ROUTINE ENEMY-RANDOM-MOVE-OR-ATTACK (O EX EY "AUX" CHOICE DX DY NX NY)
    <SET CHOICE <RNG 9>>
    <SET DX <DIR9-DX .CHOICE>>
    <SET DY <DIR9-DY .CHOICE>>
    <SET NX <+ .EX .DX>>
    <SET NY <+ .EY .DY>>
    <COND (<AND <==? .NX ,PLAYER-X> <==? .NY ,PLAYER-Y>>
           <ENEMY-ATTACK-PLAYER .O>)
          (ELSE <TRY-ENEMY-MOVE .O .NX .NY>)>>

<ROUTINE STEP-BEE-ENEMY (O "AUX" EX EY CHOICE NX NY)
    <SET EX <GETP .O ,P?R-X>>
    <SET EY <GETP .O ,P?R-Y>>

    <COND (<G? <SET CHOICE <POISON-POTION-OBJ-AT .EX .EY>> 0>
           <BEE-CONSUME-POISON-POTION .O .CHOICE>
           <RTRUE>)>

    <SET CHOICE <POISON-POTION-OBJ-IN-RANGE .EX .EY ,CHASE-RADIUS>>
    <COND (<G? .CHOICE 0>
           <SET NX <GETP .CHOICE ,P?R-X>>
           <SET NY <GETP .CHOICE ,P?R-Y>>
           <STEP-ENEMY-TOWARD .O .NX .NY>
           <SET EX <GETP .O ,P?R-X>>
           <SET EY <GETP .O ,P?R-Y>>
           <COND (<G? <SET CHOICE <POISON-POTION-OBJ-AT .EX .EY>> 0>
                  <BEE-CONSUME-POISON-POTION .O .CHOICE>)>
           <RTRUE>)>

    <RFALSE>>

<ROUTINE STEP-MONKEY-ENEMY (O "AUX" EX EY ADX ADY NX NY CHOICE)
    <SET EX <GETP .O ,P?R-X>>
    <SET EY <GETP .O ,P?R-Y>>
    <SET ADX <ABS <- ,PLAYER-X .EX>>>
    <SET ADY <ABS <- ,PLAYER-Y .EY>>>

    <COND (<G? ,PLAYER-INVIS-TURNS 0> <ENEMY-RANDOM-MOVE .O .EX .EY>)
          (<FSET? .O ,TAMEBIT>
           <COND (<G? <+ .ADX .ADY> 1>
                  <STEP-ENEMY-TOWARD .O ,PLAYER-X ,PLAYER-Y>)>)
          (<AND <NOT <ENEMY-CARRIES-BANANA? .O>>
                <G? <SET CHOICE <BANANA-OBJ-IN-RANGE .EX .EY ,CHASE-RADIUS>> 0>>
           <SET NX <GETP .CHOICE ,P?R-X>>
           <SET NY <GETP .CHOICE ,P?R-Y>>
           <STEP-ENEMY-TOWARD .O .NX .NY>
           <SET EX <GETP .O ,P?R-X>>
           <SET EY <GETP .O ,P?R-Y>>
           <COND (<G? <SET CHOICE <BANANA-OBJ-AT .EX .EY>> 0>
                  <COND (<G? <SET NX <ENEMY-CARRIED-ITEM .O>> 0>
                         <DROP-EXISTING-ITEM-NEAR .NX .EX .EY>)>
                  <REMOVE .CHOICE>
                  <PUTP .CHOICE ,P?R-X 0>
                  <PUTP .CHOICE ,P?R-Y 0>
                  <MOVE .CHOICE .O>)>)
          (<AND <ENEMY-CARRIES-BANANA? .O>
                <L=? <+ .ADX .ADY> ,CHASE-RADIUS>
                <MONKEY-CAN-SEE-PLAYER? .O>>
           <FREE-RASCAL-ITEM <ENEMY-CARRIED-ITEM .O>>
           <FSET .O ,TAMEBIT>
           <LOG "The monkey eats the banana and seems to like you." CR>)
          (<AND <G? <ENEMY-CARRIED-ITEM .O> 0>
                <L=? .ADX 1>
                <L=? .ADY 1>
                <OR <G? .ADX 0> <G? .ADY 0>>>
           <STEP-ENEMY-AWAY-PREFERRED .O ,PLAYER-X ,PLAYER-Y>)
          (<AND <L=? <ENEMY-CARRIED-ITEM .O> 0>
                <L=? <+ .ADX .ADY> ,CHASE-RADIUS>>
           <STEP-ENEMY-TOWARD .O ,PLAYER-X ,PLAYER-Y>
           <SET EX <GETP .O ,P?R-X>>
           <SET EY <GETP .O ,P?R-Y>>
           <SET ADX <ABS <- ,PLAYER-X .EX>>>
           <SET ADY <ABS <- ,PLAYER-Y .EY>>>
           <COND (<AND <L=? .ADX 1> <L=? .ADY 1> <OR <G? .ADX 0> <G? .ADY 0>>>
                  <MONKEY-STEAL-ONE .O>)>)
          (ELSE <ENEMY-RANDOM-MOVE .O .EX .EY>)>

    <RTRUE>>

<ROUTINE STEP-STANDARD-ENEMY (O "AUX" EX EY ADX ADY)
    <SET EX <GETP .O ,P?R-X>>
    <SET EY <GETP .O ,P?R-Y>>
    <SET ADX <ABS <- ,PLAYER-X .EX>>>
    <SET ADY <ABS <- ,PLAYER-Y .EY>>>

    <COND (<G? ,PLAYER-INVIS-TURNS 0>
           ;"Invisible: enemies wander randomly as if the player weren't there."
           <ENEMY-RANDOM-MOVE-OR-ATTACK .O .EX .EY>)
          (<AND <L=? .ADX 1> <L=? .ADY 1> <OR <G? .ADX 0> <G? .ADY 0>>>
           <ENEMY-ATTACK-PLAYER .O>)
          (<L=? <+ .ADX .ADY> ,CHASE-RADIUS>
           <STEP-ENEMY-TOWARD .O ,PLAYER-X ,PLAYER-Y>)
          (ELSE <ENEMY-RANDOM-MOVE .O .EX .EY>)>

    <RTRUE>>

<ROUTINE STEP-ENEMY-AI (O TYPE)
    <COND (<==? .TYPE ,ETYPE-MONKEY> <STEP-MONKEY-ENEMY .O> <RTRUE>)>
    <COND (<==? .TYPE ,ETYPE-LEGION> <COND (<STEP-BEE-ENEMY .O> <RTRUE>)>)>
    <STEP-STANDARD-ENEMY .O>
    <RTRUE>>

;"Advances enemy AI one step each. Enemies chase when near the player and
  attack when adjacent.

Args:
  (none)

Returns:
  (none)"

<ROUTINE STEP-ENEMIES ("AUX" O NXT TYPE HP)
    <SET O <FIRST? ,CURRENT-FLOOR-OBJ>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET NXT <NEXT? .O>>
        <SET TYPE <GETP .O ,P?R-ETYPE>>
        <SET HP <GETP .O ,P?R-EHP>>

        <COND (<AND <G? .TYPE 0> <G? .HP 0>> <STEP-ENEMY-AI .O .TYPE>)>

        <SET O .NXT>>>

"Enemy type selection"

<ROUTINE PICK-ENEMY-TYPE (F "AUX" R TYPE)
    <COND (<AND <G=? .F ,MONKEY-MIN-FLOOR>
                <L=? .F ,MONKEY-MAX-FLOOR>
                <L=? <RNG 100> ,MONKEY-SPAWN-PCT>
                <L=? <MONKEY-ON-FLOOR? .F> 0>>
           <RETURN ,ETYPE-MONKEY>)>

    <SET R <RNG 100>>
    <SET TYPE
      <COND (<L=? .F 3> <COND (<L=? .R 90> ,ETYPE-GOBLIN) (ELSE ,ETYPE-WRAITH)>)
            (<L=? .F 7>
             <COND (<L=? .R 60> ,ETYPE-GOBLIN)
                   (<L=? .R 80> ,ETYPE-SPHINX)
                   (ELSE ,ETYPE-WRAITH)>)
            (<L=? .F 12>
             <COND (<L=? .R 40> ,ETYPE-GOBLIN)
                   (<L=? .R 65> ,ETYPE-SPHINX)
                   (<L=? .R 85> ,ETYPE-WRAITH)
                   (ELSE ,ETYPE-KRAKEN)>)
            (<L=? .F 18>
             <COND (<L=? .R 25> ,ETYPE-GOBLIN)
                   (<L=? .R 50> ,ETYPE-SPHINX)
                   (<L=? .R 70> ,ETYPE-WRAITH)
                   (<L=? .R 90> ,ETYPE-KRAKEN)
                   (ELSE ,ETYPE-DRAGON)>)
            (ELSE
             <COND (<L=? .R 15> ,ETYPE-GOBLIN)
                   (<L=? .R 35> ,ETYPE-SPHINX)
                   (<L=? .R 55> ,ETYPE-WRAITH)
                   (<L=? .R 80> ,ETYPE-KRAKEN)
                   (ELSE ,ETYPE-DRAGON)>)>>

    <COND (<AND <==? .TYPE ,ETYPE-MONKEY> <G? <MONKEY-ON-FLOOR? .F> 0>> ,ETYPE-GOBLIN)
          (ELSE .TYPE)>>

<ROUTINE ENEMY-START-HP (TYPE F)
    <COND (<==? .TYPE ,ETYPE-GOBLIN> <+ ,ENEMY-BASE-HP-GOBLIN <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (<==? .TYPE ,ETYPE-SPHINX> <+ ,ENEMY-BASE-HP-SPHINX <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (<==? .TYPE ,ETYPE-WRAITH> <+ ,ENEMY-BASE-HP-WRAITH <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (<==? .TYPE ,ETYPE-KRAKEN> <+ ,ENEMY-BASE-HP-KRAKEN <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (<==? .TYPE ,ETYPE-DRAGON> <+ ,ENEMY-BASE-HP-DRAGON <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (<==? .TYPE ,ETYPE-SPIRIT> <+ ,ENEMY-BASE-HP-SPIRIT <ENEMY-FLOOR-BONUS .TYPE .F>>)
          (ELSE <+ 3 <ENEMY-FLOOR-BONUS .TYPE .F>>)>>

;"Attempts to spawn a single enemy object on floor F.

Returns:
  T if spawned; FALSE otherwise."

<ROUTINE SPAWN-ENEMY-OBJ (F "AUX" TRIES R PR X Y HP O TYPE)
    <SET PR <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 120> <RFALSE>)>
        <SET R <RNG ,ROOM-COUNT>>
        <COND (<AND <G? .PR 0> <==? .R .PR>> <AGAIN>)>
        <COND (<NOT <RANDOM-POINT-IN-ROOM .R>> <AGAIN>)>
        <SET X ,ENTRY-X>
        <SET Y ,ENTRY-Y>
        <COND (<NOT <IN-BOUNDS? .X .Y>> <AGAIN>)>
        <COND (<NOT <FLOOR? .X .Y>> <AGAIN>)>
        <COND (<AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>> <AGAIN>)>
        <COND (<G? <GOLD-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <FOOD-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <WEAPON-OBJ-AT .X .Y> 0> <AGAIN>)>
        <COND (<POTION-AT? .X .Y> <AGAIN>)>
        <COND (<G? <TREASURE-AT? .X .Y> 0> <AGAIN>)>
        <COND (<TRADER-AT? .X .Y> <AGAIN>)>
        <COND (<G? <ENEMY-AT .X .Y> 0> <AGAIN>)>
        <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>
               <AGAIN>)>
        <SET TYPE <PICK-ENEMY-TYPE .F>>
        <SET HP <ENEMY-START-HP .TYPE .F>>
        <COND (<NOT <SET O <ALLOC-RASCAL-ENEMY>>> <RFALSE>)>
        <PUTP .O ,P?R-ETYPE .TYPE>
        <PUTP .O ,P?R-EHP .HP>
        <PUTP .O ,P?R-X .X>
        <PUTP .O ,P?R-Y .Y>
        <MOVE .O ,CURRENT-FLOOR-OBJ>
        <COND (<==? .TYPE ,ETYPE-MONKEY> <FSET .O ,PERSONBIT>)>
        <RTRUE>>>

;"Spawns the runtime enemy list for a floor, scaling frequency by depth.

Args:
  F: Floor number (1-based).

Returns:
  T."

<ROUTINE SPAWN-ENEMIES (F "AUX" THRESH SPAWNED)
    <SET THRESH <+ 20 <* .F 3>>>
    <COND (<G? .THRESH 85> <SET THRESH 85>)>
    <SET SPAWNED 0>
    <FREE-RASCAL-ENEMY-CHILDREN ,CURRENT-FLOOR-OBJ>
    <DO (I 1 ,MAX-ENEMIES)
        <COND (<AND <L=? <RNG 100> .THRESH>
                    <SPAWN-ENEMY-OBJ .F>>
               <SET SPAWNED <+ .SPAWNED 1>>)>>
    <COND (<==? .SPAWNED 0> <SPAWN-ENEMY-OBJ .F>)>
    <RTRUE>>

;"Finds an enemy occupying (X, Y).

Args:
  X, Y: Map coordinates.

Returns:
  Enemy object, or 0 if none."

<ROUTINE ENEMY-AT (X Y "AUX" O T)
    <SET O <FIRST? ,CURRENT-FLOOR-OBJ>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET T <GETP .O ,P?R-ETYPE>>
        <COND (<AND <G? .T 0>
                    <==? <GETP .O ,P?R-X> .X>
                    <==? <GETP .O ,P?R-Y> .Y>>
               <RETURN .O>)>
        <SET O <NEXT? .O>>>>
