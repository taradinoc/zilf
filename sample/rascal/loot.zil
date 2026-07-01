"Loot spawning"

<CONSTANT DROP-GOLD-NEAR-TRY-LIMIT 24>
<CONSTANT DROP-NEAR-TRY-LIMIT 40>
<CONSTANT TELEPORT-TRY-LIMIT 500>

<CONSTANT DROPMODE-GOLD 1>
<CONSTANT DROPMODE-WEAPON 2>
<CONSTANT DROPMODE-INVENTORY 3>

<GLOBAL DROP-CAND-X 0>
<GLOBAL DROP-CAND-Y 0>

;"Temporary pool used by INIT-POTIONS to randomize assignments."

<GLOBAL POTION-COLOR-POOL <ITABLE ,POTION-COLOR-COUNT (BYTE) 0>>

;"Applies Excess/Gold Scarcity modifiers to a gold amount.

Args:
  AMT: Raw gold amount (positive integer).

Returns:
  Adjusted gold amount (positive integer)."

<ROUTINE APPLY-GOLD-MODIFIER (AMT "AUX" RESULT)
    <SET RESULT .AMT>
    ;"Excess: double gold gains."
    <COND (<G? ,PLAYER-EXCESS-TURNS 0> <SET RESULT <* .RESULT 2>>)>
    ;"Gold Scarcity: halve gold gains (round up)."
    <COND (<G? ,PLAYER-GOLD-SCARCITY-TURNS 0>
           <SET RESULT </ <+ .RESULT 1> 2>>)>
    <COND (<L=? .RESULT 0> 1) (ELSE .RESULT)>>

;"Allowed initial spawn floor ranges per treasure (1-based floors)."
<GLOBAL TREASURE-MIN-FLOOR
    <PTABLE (BYTE)
        2   ;"amulet"
        3   ;"scarab"
        6   ;"goblet"
        9   ;"iolite"
        12  ;"garnet"
        15  ;"jasper"
        18  ;"zircon"
        0   ;"poster (does not spawn randomly)"
        25  ;"trophy of scryra"
    >>

<GLOBAL TREASURE-MAX-FLOOR
    <PTABLE (BYTE)
        7   ;"amulet"
        10  ;"scarab"
        13  ;"goblet"
        16  ;"iolite"
        19  ;"garnet"
        22  ;"jasper"
        24  ;"zircon"
        0   ;"poster (does not spawn randomly)"
        25  ;"trophy of scryra"
    >>

"Initialization"

;"Randomizes the per-game mapping between potion colors and potion types.

Potion items store a color ID (POTCOLOR-*). Each game, each potion type gets a
unique color assigned (1:1), selected randomly from the available colors.
Discovery is tracked per color.

Args:
  (none)

Returns:
  T."

<ROUTINE INIT-POTIONS ("AUX" N PICK COLOR TEMP)
    ;"Reset mapping + discovery and initialize the color pool."
    <DO (C 1 ,POTION-COLOR-COUNT)
        <PUTB ,POTION-TYPE-FOR-COLOR <- .C 1> 0>
        <PUTB ,POTION-DISCOVERED <- .C 1> 0>
        <PUTB ,POTION-COLOR-POOL <- .C 1> .C>>
    <DO (T 1 ,POTION-TYPE-COUNT) <PUTB ,POTION-COLOR-FOR-TYPE <- .T 1> 0>>
    <SET N ,POTION-COLOR-COUNT>
    <DO (T 1 ,POTION-TYPE-COUNT)
        <COND (<L? .N 1> <RETURN>)>
        <SET PICK <RNG .N>>
        <SET COLOR <GETB ,POTION-COLOR-POOL <- .PICK 1>>>
        <SET TEMP <GETB ,POTION-COLOR-POOL <- .N 1>>>
        <PUTB ,POTION-COLOR-POOL <- .PICK 1> .TEMP>
        <PUTB ,POTION-COLOR-POOL <- .N 1> .COLOR>
        <SET N <- .N 1>>
        <PUTB ,POTION-COLOR-FOR-TYPE <- .T 1> .COLOR>
        <PUTB ,POTION-TYPE-FOR-COLOR <- .COLOR 1> .T>>
    <RTRUE>>

;"Gives the player a random level-1 starting weapon and equips it.

Args:
  (none)

Returns:
  T."

<ROUTINE INIT-START-WEAPON ("AUX" T)
    <SET T <RNG ,WEAPON-COUNT>>
    <INV-ADD-WEAPON .T 1 0>
    <SETG EQUIPPED-WEAPON <INV-NTH-OBJ 1>>
    <HONORS-NOTE-STARTER-WEAPON ,EQUIPPED-WEAPON>
    <HONORS-NOTE-EQUIP-CHANGE>
    <LOG "You start with a level 1 " WEAPON-NAME .T "." CR>
    <RTRUE>>

;"Chooses the initial floor assignment for each treasure (unique), and clears
  runtime treasure state.

This is run once per new game after MAKE-FLOOR-SEEDS so floor generation is
deterministic and not perturbed by these rolls."

<ROUTINE INIT-TREASURES ("AUX" MIN MAX RANGE FLOOR OK TRY)
    ;"Clear per-game treasure spawn assignments and remove any leftover treasure objects."
    <DO (I 1 ,TREASURE-COUNT) <PUTB ,TREASURE-SPAWN-FLOOR <- .I 1> 0>>

    <DO (F 1 ,MAX-FLOORS)
        <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-TREASURE>>
    ;"Clear trader locations and per-floor trader inventory."
    <DO (F 1 ,MAX-FLOORS)
        <PUTP <TRADER-OBJ .F> ,P?R-X 0>
        <PUTP <TRADER-OBJ .F> ,P?R-Y 0>
        <PUTP <TRADER-OBJ .F> ,P?R-TON 0>
        <FREE-RASCAL-ITEM-CHILDREN <TRADER-OBJ .F> 0>>

    ;"Assign unique floors within per-treasure ranges."
    <DO (I 1 ,TREASURE-COUNT)
        <SET MIN <GETB ,TREASURE-MIN-FLOOR <- .I 1>>>
        <SET MAX <GETB ,TREASURE-MAX-FLOOR <- .I 1>>>
        <SET RANGE <+ 1 <- .MAX .MIN>>>
        <SET TRY 0>
        <SET FLOOR 0>
        <REPEAT ()
            <SET TRY <+ .TRY 1>>
            <COND (<G? .TRY 200> <RETURN>)>
            <SET FLOOR <+ .MIN <- <RNG .RANGE> 1>>>
            <SET OK T>
            <DO (J 1 <G=? .J .I>)
                <COND (<==? <GETB ,TREASURE-SPAWN-FLOOR <- .J 1>> .FLOOR>
                       <SET OK <>>)>>
            <COND (.OK <RETURN>)>
            <AGAIN>>
        ;"Fallback: first unused in range."
        <COND (<L=? .FLOOR 0>
               <DO (F .MIN .MAX)
                   <SET OK T>
                   <DO (J 1 <G=? .J .I>)
                       <COND (<==? <GETB ,TREASURE-SPAWN-FLOOR <- .J 1>> .F>
                              <SET OK <>>)>>
                   <COND (.OK <SET FLOOR .F> <RETURN>)>>)>
        <PUTB ,TREASURE-SPAWN-FLOOR <- .I 1> .FLOOR>>
    <RTRUE>>

"Gold operations"

;"Adds a gold pile at (X, Y), merging with an existing pile if present.

Args:
  X, Y: Map coordinates.
  AMT: Gold amount.

Returns:
  T."

<ROUTINE ADD-GOLD-PILE (X Y AMT "AUX" SLOT CUR)
    <COND (<L=? .AMT 0> <RTRUE>)>
    <COND (<G? .AMT 255> <SET AMT 255>)>
    <SET SLOT <GOLD-OBJ-AT .X .Y>>
    <COND (<G? .SLOT 0>
           <SET CUR <GETP .SLOT ,P?R-ITAMT>>
           <SET CUR <+ .CUR .AMT>>
           <COND (<G? .CUR 255> <SET CUR 255>)>
           <PUTP .SLOT ,P?R-ITAMT .CUR>
           <RTRUE>)>
    <SET SLOT <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .SLOT>
           ;"Out of item objects. Best-effort: merge into any gold pile on this floor."
           <SET SLOT <FIRST? <FLOOR-OBJ ,CURRENT-FLOOR>>>
           <REPEAT ()
               <COND (<NOT .SLOT> <RETURN T>)>
               <COND (<==? <GETP .SLOT ,P?R-ITKIND> ,ITEMKIND-GOLD>
                      <SET CUR <GETP .SLOT ,P?R-ITAMT>>
                      <PUTP .SLOT ,P?R-ITAMT <+ .CUR .AMT>>
                      <RETURN T>)>
               <SET SLOT <NEXT? .SLOT>>>
           <RTRUE>)>
    <PUTP .SLOT ,P?R-ITKIND ,ITEMKIND-GOLD>
    <PUTP .SLOT ,P?R-ITAMT .AMT>
    <PUTP .SLOT ,P?R-X .X>
    <PUTP .SLOT ,P?R-Y .Y>
    <MOVE .SLOT <FLOOR-OBJ ,CURRENT-FLOOR>>
    <MARK-DIRTY .X .Y>
    <RTRUE>>

;"Drops a gold pile of AMT on a random adjacent passable tile near (X, Y).
  This avoids dropping on the enemy's own tile, since the player will move
  onto that tile immediately after killing it.

Args:
  X, Y: Center coordinate (typically the enemy location).
  AMT: Gold amount.

Returns:
  T."

<ROUTINE DROP-GOLD-NEAR (X Y AMT)
    <COND (<FIND-ADJACENT-DROP-TILE .X .Y ,DROPMODE-GOLD>
           <ADD-GOLD-PILE ,DROP-CAND-X ,DROP-CAND-Y .AMT>
           <RTRUE>)>
    ;"Fallback: drop on the tile you stepped from (always adjacent)."
    <ADD-GOLD-PILE ,PLAYER-X ,PLAYER-Y .AMT>
    <RTRUE>>

<ROUTINE DROP-EXISTING-ITEM-NEAR (ITEM X Y "AUX" OX OY)
    <COND (<NOT .ITEM> <RTRUE>)>
    <SET OX <GETP .ITEM ,P?R-X>>
    <SET OY <GETP .ITEM ,P?R-Y>>
    <REMOVE .ITEM>
    <COND (<FIND-ADJACENT-DROP-TILE .X .Y ,DROPMODE-INVENTORY>
           <PUTP .ITEM ,P?R-X ,DROP-CAND-X>
           <PUTP .ITEM ,P?R-Y ,DROP-CAND-Y>
           <MOVE .ITEM <FLOOR-OBJ ,CURRENT-FLOOR>>
           <MARK-DIRTY .OX .OY>
           <MARK-DIRTY ,DROP-CAND-X ,DROP-CAND-Y>
           <RTRUE>)>
    <PUTP .ITEM ,P?R-X ,PLAYER-X>
    <PUTP .ITEM ,P?R-Y ,PLAYER-Y>
    <MOVE .ITEM <FLOOR-OBJ ,CURRENT-FLOOR>>
    <MARK-DIRTY .OX .OY>
    <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
    <RTRUE>>

  <ROUTINE ADD-BASIC-ITEM-PILE (X Y KIND ID "AUX" O)
    <COND (<G? <ITEM-OBJ-AT .X .Y 0> 0> <RETURN 0>)>
    <COND (<NOT <SET O <ALLOC-RASCAL-ITEM>>> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND .KIND>
    <PUTP .O ,P?R-ITID .ID>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <MOVE .O <FLOOR-OBJ ,CURRENT-FLOOR>>
    <MARK-DIRTY .X .Y>
    .O>

"Weapon operations"

;"Adds a weapon drop at (X, Y) as a floor item object.

Args:
  X, Y: Map coordinates.
  TYPE: Weapon type code (WEAPON-*).
  LVL: Weapon level.

Returns:
  T if added; FALSE if no item object is available."

<ROUTINE ADD-WEAPON-PILE (X Y TYPE LVL ENCH "AUX" O)
    <COND (<L=? <SET O <ADD-BASIC-ITEM-PILE .X .Y ,ITEMKIND-WEAPON .TYPE>> 0>
           <RFALSE>)>
    <PUTP .O ,P?R-ITLVL .LVL>
    <PUTP .O ,P?R-ITENCH .ENCH>
    <RTRUE>>

;"Rolls a loot weapon level for the current floor.

Args:
  F: Floor number (1-based).

Returns:
  Weapon level (1..15)."

<ROUTINE ROLL-LOOT-WEAPON-LEVEL (F "AUX" BASE LVL)
    <SET BASE <+ 1 </ .F 4>>>
    <COND (<L? .BASE 1> <SET BASE 1>)>
    <SET LVL .BASE>
    <REPEAT ()
        <COND (<G? .LVL 15> <RETURN>)>
        <COND (<==? <RNG 4> 1> <SET LVL <+ .LVL 1>> <AGAIN>)>
        <RETURN>>
    .LVL>

;"Rolls a loot weapon enchantment.

1 in 10 chance of being enchanted.
If enchanted: 9 in 10 chance of +1, 1 in 10 chance of +2.

Returns:
  Enchantment level (0, 1, or 2)."

<ROUTINE ROLL-LOOT-WEAPON-ENCH ()
    <COND (<==? <RNG 10> 1> <COND (<==? <RNG 10> 1> 2) (ELSE 1)>) (ELSE 0)>>

;"Drops a weapon near (X, Y) on a random adjacent passable tile.

Args:
  X, Y: Center coordinate.
  TYPE: Weapon type code (WEAPON-*).
  LVL: Weapon level.

Returns:
  T if dropped; FALSE if no spot or no slot."

<ROUTINE DROP-PILE-NEAR (X Y MODE KIND ID LVL ENCH)
    <COND (<NOT <FIND-ADJACENT-DROP-TILE .X .Y .MODE>> <RFALSE>)>
    <COND (<==? .KIND ,ITEMKIND-WEAPON>
           <COND (<ADD-WEAPON-PILE ,DROP-CAND-X ,DROP-CAND-Y .ID .LVL .ENCH>
                  <RTRUE>)>)
          (<==? .KIND ,ITEMKIND-POTION>
           <COND (<ADD-POTION-PILE ,DROP-CAND-X ,DROP-CAND-Y .ID> <RTRUE>)>)
          (<==? .KIND ,ITEMKIND-FOOD>
           <COND (<ADD-FOOD-PILE ,DROP-CAND-X ,DROP-CAND-Y .ID> <RTRUE>)>)>
    <RFALSE>>

<ROUTINE DROP-WEAPON-NEAR (X Y TYPE LVL ENCH)
    <DROP-PILE-NEAR .X .Y ,DROPMODE-WEAPON ,ITEMKIND-WEAPON .TYPE .LVL .ENCH>>

<ROUTINE PICK-WEAPON-TYPE (F)
    <RNG ,WEAPON-COUNT>>

<ROUTINE DROP-RANDOM-WEAPON-NEAR (X Y F)
    <DROP-WEAPON-NEAR .X
                      .Y
                      <PICK-WEAPON-TYPE .F>
                      <ROLL-LOOT-WEAPON-LEVEL .F>
                      <ROLL-LOOT-WEAPON-ENCH>>>

<ROUTINE SPAWN-WEAPONS (F "AUX" WANT TRIES X Y LVL ENCH)
    <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-WEAPON>
    <SET WANT 0>
    <COND (<L=? <RNG 100> 20> <SET WANT 1>)>
    <COND (<L=? <RNG 100> 5> <SET WANT <+ .WANT 1>>)>
    <DO (I 1 .WANT)
        <SET LVL <ROLL-LOOT-WEAPON-LEVEL .F>>
        <SET ENCH <ROLL-LOOT-WEAPON-ENCH>>
        <SET TRIES 0>
        <REPEAT ()
            <SET TRIES <+ .TRIES 1>>
            <COND (<G? .TRIES 200> <RETURN>)>
            <SET X <+ 1 <RNG ,MAP-W>>>
            <SET Y <+ 1 <RNG ,MAP-H>>>
            <COND (<NOT <IN-BOUNDS? .X .Y>> <AGAIN>)>
            <COND (<NOT <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
                            <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
                            <==? <TILE-AT .X .Y> ,TILE-DOOR>>>
                   <AGAIN>)>
            <COND (<AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>> <AGAIN>)>
            <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                       <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>
                   <AGAIN>)>
            <COND (<G? <ENEMY-AT .X .Y> 0> <AGAIN>)>
            <COND (<G? <FLOOR-ITEM-OBJ-AT .F .X .Y 0> 0> <AGAIN>)>
            <COND (<TRADER-AT? .X .Y> <AGAIN>)>
            <COND (<G? <TREASURE-AT? .X .Y> 0> <AGAIN>)>
            <COND (<NOT <ADD-WEAPON-PILE .X
                                         .Y
                                         <PICK-WEAPON-TYPE .F>
                                         .LVL
                                         .ENCH>>
                   <RETURN>)>
            <RETURN>>>
    <RTRUE>>

"Potion operations"

;"Rolls a loot potion color.

Uses the per-game type->color mapping, but falls back to a random color if
something is missing (should be rare).

Returns:
  Potion color code (1..POTION-COLOR-COUNT)."

<ROUTINE ROLL-LOOT-POTION-COLOR ("AUX" TYPE COLOR)
    <SET TYPE <RNG ,POTION-TYPE-COUNT>>
    <SET COLOR <GETB ,POTION-COLOR-FOR-TYPE <- .TYPE 1>>>
    <COND (<L? .COLOR 1> <SET COLOR <RNG ,POTION-COLOR-COUNT>>)>
    .COLOR>

;"Adds a potion drop at (X, Y) as a floor item object.

Args:
  X, Y: Map coordinates.
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  T if added; FALSE if no item object is available (or tile already has an item)."

<ROUTINE ADD-POTION-PILE (X Y COLOR "AUX" O)
    <COND (<L=? <SET O <ADD-BASIC-ITEM-PILE .X .Y ,ITEMKIND-POTION .COLOR>> 0>
           <RFALSE>)>
    <FCLEAR .O ,SEENBIT>
    <SETG STATS-POTIONS-PLACED <+ ,STATS-POTIONS-PLACED 1>>
    <RTRUE>>

;"Drops a potion near (X, Y) on a random adjacent passable tile.

Args:
  X, Y: Center coordinate.
  COLOR: Potion color code.

Returns:
  T if dropped; FALSE if no spot or no slot."

<ROUTINE DROP-POTION-NEAR (X Y COLOR)
    <DROP-PILE-NEAR .X .Y ,DROPMODE-WEAPON ,ITEMKIND-POTION .COLOR 0 0>>

"Key operations"

<ROUTINE ADD-KEY-PILE (X Y LOCKTYPE "AUX" O)
    <COND (<G? <ITEM-OBJ-AT .X .Y 0> 0> <RFALSE>)>
    <COND (<SET O <ALLOC-RASCAL-ITEM>>
           <PUTP .O ,P?R-ITKIND ,ITEMKIND-KEY>
           <PUTP .O ,P?R-ITID .LOCKTYPE>
           <PUTP .O ,P?R-X .X>
           <PUTP .O ,P?R-Y .Y>
           <MOVE .O <FLOOR-OBJ ,CURRENT-FLOOR>>
              <MARK-DIRTY .X .Y>
           <RTRUE>)>
    <RFALSE>>

<ROUTINE DROP-KEY-NEAR (X Y LOCKTYPE)
    <COND (<NOT <FIND-ADJACENT-DROP-TILE .X .Y ,DROPMODE-INVENTORY>> <RFALSE>)>
    <COND (<ADD-KEY-PILE ,DROP-CAND-X ,DROP-CAND-Y .LOCKTYPE> <RTRUE>)>
    <RFALSE>>

<ROUTINE FIND-RANDOM-GROUND-ITEM-SPAWN-POINT (LIMIT "AUX" TRIES X Y)
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES .LIMIT>
               <SETG ENTRY-X 0>
               <SETG ENTRY-Y 0>
               <COND (<FIND-GROUND-ITEM-SPOT-ANYWHERE> <RTRUE>)
                     (ELSE <RFALSE>)>)>
        <SET X <+ 1 <RNG ,MAP-W>>>
        <SET Y <+ 1 <RNG ,MAP-H>>>
        <COND (<NOT <VALID-GROUND-ITEM-TILE? .X .Y>> <AGAIN>)>
        <SETG ENTRY-X .X>
        <SETG ENTRY-Y .Y>
        <RTRUE>>>

<ROUTINE SPAWN-POTION (F "AUX" WANT X Y TYPE COLOR)
    <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-POTION>
    ;"On floor 25, guarantee at least one of each potion color."
    <COND (<==? .F ,MAX-FLOORS> <SET WANT ,POTION-COLOR-COUNT>)
          (ELSE
           ;"4/5 chance per floor of having any potions at all."
           <COND (<==? <RNG 5> 1> <RTRUE>)>
           ;"1/3 of the levels that have potions get two."
           <SET WANT 1>
           <COND (<==? <RNG 3> 1> <SET WANT 2>)>)>
    <DO (I 1 .WANT)
        <COND (<NOT <FIND-RANDOM-GROUND-ITEM-SPAWN-POINT 400>>
               <RETURN>)>
        <SET X ,ENTRY-X>
        <SET Y ,ENTRY-Y>

      <COND (<==? .F ,MAX-FLOORS> <SET COLOR .I>)
          (ELSE
           <SET TYPE <RNG ,POTION-TYPE-COUNT>>
           <SET COLOR <GETB ,POTION-COLOR-FOR-TYPE <- .TYPE 1>>>
           <COND (<L? .COLOR 1>
              <SET COLOR <RNG ,POTION-COLOR-COUNT>>)>)>
      <COND (<ADD-POTION-PILE .X .Y .COLOR> <RETURN>)>
      <RETURN>>
    <RTRUE>>

"Food operations"

;"Adds a food pile at (X, Y) as a floor item object.

Args:
  X, Y: Map coordinates.
  TYPE: Food type code (FOOD-*).

Returns:
  T if added; FALSE if no item object is available."

<ROUTINE ADD-FOOD-PILE (X Y TYPE)
    <COND (<G? <ADD-BASIC-ITEM-PILE .X .Y ,ITEMKIND-FOOD .TYPE> 0> <RTRUE>)>
    <RFALSE>>

;"Drops food near (X, Y) on a random adjacent passable tile.

Args:
  X, Y: Center coordinate.
  TYPE: Food type code (FOOD-*).

Returns:
  T if dropped; FALSE if no spot or no slot."

<ROUTINE DROP-FOOD-NEAR (X Y TYPE)
    <DROP-PILE-NEAR .X .Y ,DROPMODE-WEAPON ,ITEMKIND-FOOD .TYPE 0 0>>

<ROUTINE PICK-FOOD-TYPE (F "AUX" R)
    <SET R <RNG 100>>
    <COND (<==? .F ,MAX-FLOORS>
           <COND (<L=? .R 10> ,FOOD-CAVIAR)
                 (ELSE
                  <SET R <RNG 100>>
                  <COND (<L=? .R 10> ,FOOD-BANANA)
                        (<L=? .R 25> ,FOOD-GRAPES)
                        (<L=? .R 50> ,FOOD-CHEESE)
                        (<L=? .R 75> ,FOOD-MUFFIN)
                        (ELSE ,FOOD-TURKEY)>)>)
          (<L=? .F 5>
           <COND (<L=? .R 25> ,FOOD-BANANA)
                 (<L=? .R 50> ,FOOD-GRAPES)
                 (<L=? .R 75> ,FOOD-CHEESE)
                 (ELSE ,FOOD-MUFFIN)>)
          (<L=? .F 12>
           <COND (<L=? .R 20> ,FOOD-BANANA)
                 (<L=? .R 40> ,FOOD-GRAPES)
                 (<L=? .R 65> ,FOOD-CHEESE)
                 (<L=? .R 85> ,FOOD-MUFFIN)
                 (ELSE ,FOOD-TURKEY)>)
          (ELSE
           <COND (<L=? .R 10> ,FOOD-BANANA)
                 (<L=? .R 25> ,FOOD-GRAPES)
                 (<L=? .R 50> ,FOOD-CHEESE)
                 (<L=? .R 75> ,FOOD-MUFFIN)
                 (ELSE ,FOOD-TURKEY)>)>>

<ROUTINE SPAWN-FOOD (F "AUX" WANT X Y)
    <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-FOOD>
    <SET X <RNG 100>>
    <COND (<==? .F ,MAX-FLOORS> <SET WANT 3>)
          (<L=? .X 60> <SET WANT 1>)
          (<L=? .X 90> <SET WANT 2>)
          (ELSE <SET WANT 3>)>
    <DO (I 1 .WANT)
       <COND (<NOT <FIND-RANDOM-GROUND-ITEM-SPAWN-POINT 320>>
         <RETURN>)>
       <SET X ,ENTRY-X>
       <SET Y ,ENTRY-Y>
       <COND (<==? .F ,MAX-FLOORS>
         <COND (<ADD-FOOD-PILE .X .Y ,FOOD-CAVIAR> <RETURN>)
          (ELSE <RETURN>)>)
        (ELSE
         <COND (<ADD-FOOD-PILE .X .Y <PICK-FOOD-TYPE .F>> <RETURN>)
          (ELSE <RETURN>)>)>
       <RETURN>>
    <RTRUE>>

"Drop tile and spot finding helpers"

;"Returns true if a tile is an ordinary passable tile suitable for drops.

Args:
  X, Y: Map coordinates.

Returns:
  T if passable for drops; FALSE otherwise."

<ROUTINE PASSABLE-FOR-DROP? (X Y)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <COND (<OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
               <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
         <AND <==? <TILE-AT .X .Y> ,TILE-DOOR>
          <NOT <LOCKED-DOOR-CLOSED-AT? .X .Y>>>>
           <RTRUE>)>
    <RFALSE>>

;"Fallback: scans the entire map for any valid ground-item tile.
Returns coordinates via ENTRY-X/ENTRY-Y globals, or 0/0 if not found."

<ROUTINE FIND-GROUND-ITEM-SPOT-ANYWHERE ()
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    <DO (YY 1 ,MAP-H)
        <DO (XX 1 ,MAP-W)
            <COND (<VALID-GROUND-ITEM-TILE? .XX .YY>
                   <SETG ENTRY-X .XX>
                   <SETG ENTRY-Y .YY>
                   <RTRUE>)>>>
    <RFALSE>>

;"Find a random adjacent tile suitable for dropping something.

MODE controls collision checks:
  DROPMODE-GOLD: disallow food, potion, treasure, trader.
  DROPMODE-WEAPON: disallow stairs, trader, potion, food, gold, treasure, weapon, enemy.
  DROPMODE-INVENTORY: disallow stairs, trader, potion, food, gold, treasure, enemy.

On success sets DROP-CAND-X/Y and returns T. On failure returns FALSE."

<ROUTINE FIND-ADJACENT-DROP-TILE (X Y MODE "AUX" LIMIT TRIES D DX DY NX NY)
    <COND (<==? .MODE ,DROPMODE-GOLD> <SET LIMIT ,DROP-GOLD-NEAR-TRY-LIMIT>)
          (ELSE <SET LIMIT ,DROP-NEAR-TRY-LIMIT>)>
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES .LIMIT> <RFALSE>)>
        <SET D <RNG 8>>
        <SET DX <DIR8-DX .D>>
        <SET DY <DIR8-DY .D>>
        <SET NX <+ .X .DX>>
        <SET NY <+ .Y .DY>>
        <COND (<NOT <PASSABLE-FOR-DROP? .NX .NY>> <AGAIN>)>
        <COND (<AND <==? .NX ,PLAYER-X> <==? .NY ,PLAYER-Y>> <AGAIN>)>
        <COND (<==? .MODE ,DROPMODE-GOLD>
           <COND (<G? <FOOD-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<POTION-AT? .NX .NY> <AGAIN>)>
               <COND (<G? <TREASURE-AT? .NX .NY> 0> <AGAIN>)>
               <COND (<TRADER-AT? .NX .NY> <AGAIN>)>)
              (<==? .MODE ,DROPMODE-WEAPON>
               <COND (<OR <==? <TILE-AT .NX .NY> ,TILE-STAIR-UP>
                          <==? <TILE-AT .NX .NY> ,TILE-STAIR-DOWN>>
                      <AGAIN>)>
               <COND (<TRADER-AT? .NX .NY> <AGAIN>)>
               <COND (<POTION-AT? .NX .NY> <AGAIN>)>
               <COND (<G? <FOOD-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<G? <GOLD-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<G? <TREASURE-AT? .NX .NY> 0> <AGAIN>)>
               <COND (<G? <WEAPON-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<G? <ENEMY-AT .NX .NY> 0> <AGAIN>)>)
              (<==? .MODE ,DROPMODE-INVENTORY>
               <COND (<OR <==? <TILE-AT .NX .NY> ,TILE-STAIR-UP>
                          <==? <TILE-AT .NX .NY> ,TILE-STAIR-DOWN>>
                      <AGAIN>)>
               <COND (<TRADER-AT? .NX .NY> <AGAIN>)>
               <COND (<POTION-AT? .NX .NY> <AGAIN>)>
               <COND (<G? <FOOD-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<G? <GOLD-OBJ-AT .NX .NY> 0> <AGAIN>)>
               <COND (<G? <TREASURE-AT? .NX .NY> 0> <AGAIN>)>
               <COND (<G? <ENEMY-AT .NX .NY> 0> <AGAIN>)>)>
        <SETG DROP-CAND-X .NX>
        <SETG DROP-CAND-Y .NY>
        <RTRUE>>>
