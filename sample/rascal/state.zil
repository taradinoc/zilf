"Game state"

;"Trader inventory persistence:

Trader stock is stored directly as child item objects under the per-floor
trader object (see objects.zil). The trader object's R-TON property is used
as a per-floor \"stock initialized\" flag."

<GLOBAL FLOOR-ENEMY-INIT <ITABLE ,MAX-FLOORS (BYTE) 0>>

<GLOBAL PENDING-SPIRIT-SPAWNS <ITABLE ,MAX-FLOORS (BYTE) 0>>

<GLOBAL PLAYER-GOLD 0>

;"Per-game randomized potion identification.

Potion items store a color (POTCOLOR-*). The color->effect mapping is randomized
each new game; once a color has been sampled, that color is considered
discovered and will display by effect name instead of color name."

<GLOBAL POTION-TYPE-FOR-COLOR <ITABLE ,POTION-COLOR-COUNT (BYTE) 0>>

<GLOBAL POTION-COLOR-FOR-TYPE <ITABLE ,POTION-TYPE-COUNT (BYTE) 0>>

<GLOBAL POTION-DISCOVERED <ITABLE ,POTION-COLOR-COUNT (BYTE) 0>>

;"Per-treasure persistence. State: 0=not yet spawned, 1=on ground, 2=carried, 3=sold."

;"Per-game treasure spawning: each treasure ID is assigned a unique initial
  spawn floor at game start (see INIT-TREASURES in loot.zil). Treasures
  themselves are live item objects and persist by being MOVEd between floors,
  inventory, and traders."

<GLOBAL TREASURE-SPAWN-FLOOR <ITABLE ,TREASURE-COUNT (BYTE) 0>>

;"Per-floor treasure-room creation flag. State: 0=not yet created, 1=created."

<GLOBAL TREASURE-ROOM-CREATED <ITABLE ,MAX-FLOORS (BYTE) 0>>

;"Precomputed treasure-room plans (one per floor)."

<CONSTANT TREASURE-LOOT-WEAPON 1>
<CONSTANT TREASURE-LOOT-POTION 2>
<CONSTANT TREASURE-LOOT-GOLD 3>

<GLOBAL TREASURE-ROOM-DOOR-X <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-DOOR-Y <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOCKTYPE <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-X <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-Y <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-KIND <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-ID <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-LVL <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-ENCH <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-LOOT-AMT <ITABLE ,MAX-FLOORS (WORD) 0>>
<GLOBAL TREASURE-ROOM-BONUS-GOLD-X <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-BONUS-GOLD-Y <ITABLE ,MAX-FLOORS (BYTE) 0>>
<GLOBAL TREASURE-ROOM-BONUS-GOLD-AMT <ITABLE ,MAX-FLOORS (WORD) 0>>

<GLOBAL SHRINE-OFFER1-KIND 0>
<GLOBAL SHRINE-OFFER1-ID 0>
<GLOBAL SHRINE-OFFER1-LVL 0>
<GLOBAL SHRINE-OFFER1-ENCH 0>
<GLOBAL SHRINE-OFFER1-AMT 0>

<GLOBAL SHRINE-OFFER2-KIND 0>
<GLOBAL SHRINE-OFFER2-ID 0>
<GLOBAL SHRINE-OFFER2-LVL 0>
<GLOBAL SHRINE-OFFER2-ENCH 0>
<GLOBAL SHRINE-OFFER2-AMT 0>

<GLOBAL SEED-HI 0>
<GLOBAL SEED-LO 0>

<GLOBAL EXPERT-MODE? <>>

<GLOBAL IDENTIFY-COST 50>
<GLOBAL CARROT-COST 5>
<GLOBAL ENCHANT-COST 100>

<GLOBAL CURRENT-FLOOR 1>
<GLOBAL ROOM-COUNT 0>
<GLOBAL CURRENT-ROOM 0>

<GLOBAL PLAYER-X 2>
<GLOBAL PLAYER-Y 2>

<GLOBAL PLAYER-INVIS-TURNS 0>
<GLOBAL PLAYER-VISION-TURNS 0>
<GLOBAL PLAYER-TORPOR-TURNS 0>
<GLOBAL PLAYER-HUSTLE-TURNS 0>

<GLOBAL GAME-OVER? <>>
<GLOBAL YOU-WIN? <>>

;"Set to avoid repeating the death message/score each turn after dying."
<GLOBAL DEATH-LOGGED? <>>

<ROUTINE STARTING-PLAYER-STR ()
    <COND (,EXPERT-MODE? 1)
          (ELSE ,INITIAL-PLAYER-STR)>>

<ROUTINE WIN-SCORE-BONUS ()
    <COND (,EXPERT-MODE? <* 2 ,SCORE-BONUS-WIN>)
          (ELSE ,SCORE-BONUS-WIN)>>

<ROUTINE BEE-SWARM-LOSE-DISTANCE ()
    <COND (,EXPERT-MODE? <* 2 ,BEE-SWARM-LOSE-DIST>)
          (ELSE ,BEE-SWARM-LOSE-DIST)>>

<ROUTINE POISON-POTION-DAMAGE ()
    <COND (,EXPERT-MODE? 9)
          (ELSE 3)>>

<ROUTINE PRICE-PLUS-HALF (PRICE)
    <+ .PRICE </ <+ .PRICE 1> 2>>>

<ROUTINE LOAD-FLOOR-TRINV (F "AUX" T O)
    <SET T <TRADER-OBJ .F>>
    ;"If no trader is present, keep the per-floor trader object empty and uninitialized."
    <COND (<NOT ,TRADER-ON?>
           <FREE-RASCAL-ITEM-CHILDREN .T 0>
           <PUTP .T ,P?R-TON 0>
           <RTRUE>)>
    ;"If stock is already initialized, leave it as-is."
    <COND (<G? <GETP .T ,P?R-TON> 0> <RTRUE>)>
    ;"Compatibility: if items already exist, mark initialized."
    <SET O <FIRST? .T>>
    <COND (.O <PUTP .T ,P?R-TON 1> <RTRUE>)>
    ;"First visit: generate stock and mark initialized."
    <INIT-TRADER-STOCK .F>
    <PUTP .T ,P?R-TON 1>
    <RTRUE>>

;"For floors 2+, maintain stable map generation by forcing entry to the persisted
  up-stair coordinates on every visit.

Args:
  F: Floor number (1..MAX-FLOORS).
  X, Y: Landing coordinates; used only on first descent.
  LANDING?: True when arriving from previous floor.

Returns:
  T."

<ROUTINE ENTER-FLOOR-SET-FORCED-ENTRY (F X Y LANDING? "AUX" UPX UPY)
    <COND (<G? .F 1>
           <COND (<AND .LANDING? <G? .X 0> <G? .Y 0>>
                  ;"First descent: store entry as the up-stair position."
                  <FLOOR-SET-UP-STAIRS .F .X .Y>)>
           <SET UPX <FLOOR-UP-X .F>>
           <SET UPY <FLOOR-UP-Y .F>>
           <COND (<AND <G? .UPX 0> <G? .UPY 0>>
                  ;"Use stored up-stair coords for forced-entry on every visit."
                  <SETG FORCED-ENTRY? T>
                  <SETG ENTRY-X .UPX>
                  <SETG ENTRY-Y .UPY>)
                 (ELSE
                  <SETG FORCED-ENTRY? <>>
                  <SETG ENTRY-X 0>
                  <SETG ENTRY-Y 0>)>)
          (ELSE
           ;"Floor 1: never use forced entry."
           <SETG FORCED-ENTRY? <>>
           <SETG ENTRY-X 0>
           <SETG ENTRY-Y 0>)>
    <RTRUE>>

;"Place the up stairs for a floor, if applicable.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-PLACE-UP-STAIRS (F "AUX" UPX UPY)
    <COND (<G? .F 1>
           <SET UPX <FLOOR-UP-X .F>>
           <SET UPY <FLOOR-UP-Y .F>>
           <COND (<AND <G? .UPX 0> <G? .UPY 0>>
                  <FORCE-PASSABLE .UPX .UPY>
                  <PUTB ,MAP <MAP-INDEX .UPX .UPY> ,TILE-STAIR-UP>)>)>
    <RTRUE>>

;"If the trophy is carried back to floor 1, ensure an exit stair exists at the
  original spawn point.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-ENSURE-TROPHY-EXIT (F)
    <COND (<AND <==? .F 1>
                <PLAYER-HAS-TROPHY?>
                <G? ,ORIG-SPAWN-X 0>
                <G? ,ORIG-SPAWN-Y 0>>
           <FORCE-PASSABLE ,ORIG-SPAWN-X ,ORIG-SPAWN-Y>
           <PUTB ,MAP <MAP-INDEX ,ORIG-SPAWN-X ,ORIG-SPAWN-Y> ,TILE-STAIR-UP>)>
    <RTRUE>>

;"Initialize per-floor reveal/room-discovery tables on first visit.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-INIT-VISIT (F)
    <COND (<NOT <FSET? <FLOOR-OBJ .F> ,TOUCHBIT>>
           <CLEAR-FLOOR-REVEAL .F>
           <CLEAR-FLOOR-ROOMDISC .F>
           <FSET <FLOOR-OBJ .F> ,TOUCHBIT>)>
    <RTRUE>>

;"Position the player and set CURRENT-ROOM.

Args:
  X, Y: Landing coordinates; 0/0 for initial entry.

Returns:
  T."

<ROUTINE ENTER-FLOOR-PLACE-PLAYER (X Y "AUX" PR)
    <COND (<AND <G? .X 0> <G? .Y 0>>
           <SETG PLAYER-X .X>
           <SETG PLAYER-Y .Y>)
          (<AND <G? ,ROOM-COUNT 0> <RANDOM-POINT-IN-ROOM 1>>
           <SETG PLAYER-X ,ENTRY-X>
           <SETG PLAYER-Y ,ENTRY-Y>)
          (ELSE
           <SETG PLAYER-X <ROOM-GET 1 ,ROOM-CX>>
           <SETG PLAYER-Y <ROOM-GET 1 ,ROOM-CY>>)>
    <SET PR <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<AND <L=? .PR 0> <G? ,ROOM-COUNT 0>> <SET PR 1>)>
    <SETG CURRENT-ROOM .PR>
    <RTRUE>>

;"Place (or reuse) the down stairs for a floor.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-TRY-REUSE-DOWN-STAIRS (F)
    <SETG ENTRY-X <FLOOR-DOWN-X .F>>
    <SETG ENTRY-Y <FLOOR-DOWN-Y .F>>
    <COND (<AND <G? ,ENTRY-X 0> <G? ,ENTRY-Y 0>>
           <FORCE-PASSABLE ,ENTRY-X ,ENTRY-Y>
           <PUTB ,MAP <MAP-INDEX ,ENTRY-X ,ENTRY-Y> ,TILE-STAIR-DOWN>
           <RTRUE>)>
    <RFALSE>>

<ROUTINE ENTER-FLOOR-PICK-DOWN-STAIRS-ROOMCENTER ("AUX" R)
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    ;"First try: pick a carved tile in a different room."
    <COND (<G? ,ROOM-COUNT 1>
           <DO (I 1 40)
               <SET R <RNG ,ROOM-COUNT>>
               <COND (<AND <G? ,CURRENT-ROOM 0> <==? .R ,CURRENT-ROOM>>
                      <AGAIN>)>
               <COND (<NOT <RANDOM-POINT-IN-ROOM .R>> <AGAIN>)>
               <COND (<AND <G? ,ENTRY-X 0> <G? ,ENTRY-Y 0>> <RETURN>)>>)
          (<RANDOM-POINT-IN-ROOM 1> ;"sets ENTRY-X and ENTRY-Y")
          (ELSE
           <SETG ENTRY-X <ROOM-GET 1 ,ROOM-CX>>
           <SETG ENTRY-Y <ROOM-GET 1 ,ROOM-CY>>)>
    <RTRUE>>

<ROUTINE ENTER-FLOOR-DOWN-STAIRS-NEED-FARTHEST? ()
    <OR <==? ,ENTRY-X 0>
        <==? ,ENTRY-Y 0>
        <AND <G? ,CURRENT-ROOM 0>
             <==? <ROOMID-AT ,ENTRY-X ,ENTRY-Y> ,CURRENT-ROOM>>>>

<ROUTINE ENTER-FLOOR-PICK-DOWN-STAIRS-FARTHEST ("AUX" BESTD D)
    ;"Fallback: farthest passable tile not in the entry room (or simply farthest)."
    <SET BESTD -1>
    <DO (YY 1 ,MAP-H)
        <DO (XX 1 ,MAP-W)
            <COND (<AND <FLOOR? .XX .YY>
                        <OR <L=? ,CURRENT-ROOM 0>
                            <N==? <ROOMID-AT .XX .YY> ,CURRENT-ROOM>>
                        <NOT <AND <==? .XX ,PLAYER-X> <==? .YY ,PLAYER-Y>>>>
                   <SET D <+ <ABS <- .XX ,PLAYER-X>> <ABS <- .YY ,PLAYER-Y>>>>
                   <COND (<G? .D .BESTD>
                          <SET BESTD .D>
                          <SETG ENTRY-X .XX>
                          <SETG ENTRY-Y .YY>)>)>>>
    <RTRUE>>

<ROUTINE ENTER-FLOOR-PERSIST-AND-PLACE-DOWN-STAIRS (F)
    <COND (<AND <G? ,ENTRY-X 0> <G? ,ENTRY-Y 0>>
           <FLOOR-SET-DOWN-STAIRS .F ,ENTRY-X ,ENTRY-Y>
           <FORCE-PASSABLE ,ENTRY-X ,ENTRY-Y>
           <PUTB ,MAP <MAP-INDEX ,ENTRY-X ,ENTRY-Y> ,TILE-STAIR-DOWN>)>
    <RTRUE>>

<ROUTINE ENTER-FLOOR-PLACE-DOWN-STAIRS (F)
    <COND (<L? .F ,MAX-FLOORS>
           ;"If exit stairs already exist for this floor, reuse them."
           <COND (<ENTER-FLOOR-TRY-REUSE-DOWN-STAIRS .F> <RTRUE>)>
           <ENTER-FLOOR-PICK-DOWN-STAIRS-ROOMCENTER>
           <COND (<ENTER-FLOOR-DOWN-STAIRS-NEED-FARTHEST?>
                  <SETG ENTRY-X 0>
                  <SETG ENTRY-Y 0>
                  <ENTER-FLOOR-PICK-DOWN-STAIRS-FARTHEST>)>
           <ENTER-FLOOR-PERSIST-AND-PLACE-DOWN-STAIRS .F>)>
    <RTRUE>>

;"Load/position per-floor static features (trader/interiors/treasures) and
  update discovery counters.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-LOAD-FEATURES (F)
    <LOAD-FLOOR-TRADER .F>
    <PLACE-INTERIOR-ENTRANCES .F>
    <LOAD-FLOOR-TRINV .F>
    <LOAD-FLOOR-TREASURES .F>
    <SETG DISCOVERED-ROOMS <COUNT-FLOOR-ROOMDISC .F>>
    <COND (<G? ,CURRENT-ROOM 0>
           <REVEAL-ROOM ,CURRENT-ROOM>)>
    <RTRUE>>

;"Load per-floor dynamic state and perform initial reveal.

Args:
  F: Floor number (1..MAX-FLOORS).

Returns:
  T."

<ROUTINE ENTER-FLOOR-LOAD-STATE-AND-REVEAL (F FIRST-VISIT?)
    <COND (.FIRST-VISIT?
           <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-WEAPON>
           <SPAWN-WEAPONS .F>
           <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-FOOD>
           <SPAWN-FOOD .F>
           <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-POTION>
           <SPAWN-POTION .F>
           ;"Generate treasure rooms after the free/spawn passes so treasure loot
            (which may be a weapon/potion) isn't immediately cleared."
           <PLACE-PRECOMPUTED-TREASURE-ROOM .F>
           <SPAWN-COFFERS .F>)>
    <LOAD-FLOOR-ENEMIES .F>
    <APPLY-PENDING-SPIRIT-SPAWNS .F>
    <REVEAL-AROUND ,PLAYER-X ,PLAYER-Y>
    <COND (<AND <G? <ROOMID-AT ,PLAYER-X ,PLAYER-Y> 0>
                <N==? <ROOMID-AT ,PLAYER-X ,PLAYER-Y> ,CURRENT-ROOM>>
           <SETG CURRENT-ROOM <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
            <REVEAL-ROOM ,CURRENT-ROOM>)>
    <RTRUE>>

;"Enters a floor: reseeds RNG to its floor seed, rebuilds the dungeon map,
  restores per-floor state, positions stairs and player, and performs reveal.

Args:
  F: Floor number (1..MAX-FLOORS).
  X, Y: Landing coordinates when moving between floors; 0/0 for initial entry.
  LANDING?: True when arriving from the previous floor (used to place UP stairs).

Returns:
  T."

<ROUTINE ENTER-FLOOR (F X Y LANDING? "AUX" FIRST-VISIT?)
    <SETG CURRENT-FLOOR .F>
    <SETG CURRENT-FLOOR-OBJ <FLOOR-OBJ .F>>
    <COND (<G? .F ,STATS-MAX-FLOOR-REACHED> <SETG STATS-MAX-FLOOR-REACHED .F>)>
    <SEED-RNG-32 <FLOOR-SEED-HI .F> <FLOOR-SEED-LO .F>>

    <SET FIRST-VISIT? <NOT <FSET? <FLOOR-OBJ .F> ,TOUCHBIT>>>

    <ENTER-FLOOR-SET-FORCED-ENTRY .F .X .Y .LANDING?>
    <BUILD-DUNGEON>

    <ENTER-FLOOR-PLACE-UP-STAIRS .F>
    <ENTER-FLOOR-ENSURE-TROPHY-EXIT .F>
    <ENTER-FLOOR-INIT-VISIT .F>
    <ENTER-FLOOR-PLACE-PLAYER .X .Y>
    <ENTER-FLOOR-PLACE-DOWN-STAIRS .F>
    <ENTER-FLOOR-LOAD-FEATURES .F>
    <PLACE-PENDING-KEYS .F>
    <ENTER-FLOOR-LOAD-STATE-AND-REVEAL .F .FIRST-VISIT?>
    <SETG FULL-REDRAW? T>
    <RTRUE>>

;"Object-backed ground item queries"

;"Find an item object on floor F at (X,Y).

If KIND is 0, matches any item (R-ITKIND > 0). Otherwise matches only items
whose R-ITKIND equals KIND.

Returns the item object, or 0 if none."

<ROUTINE FLOOR-ITEM-OBJ-AT (F X Y KIND "AUX" O K)
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<AND <G? .K 0>
                    <OR <L=? .KIND 0> <==? .K .KIND>>
                    <==? <GETP .O ,P?R-X> .X>
                    <==? <GETP .O ,P?R-Y> .Y>>
               <RETURN .O>)>
        <SET O <NEXT? .O>>>>

<ROUTINE ITEM-OBJ-AT (X Y KIND)
    <FLOOR-ITEM-OBJ-AT ,CURRENT-FLOOR .X .Y .KIND>>

<ROUTINE GOLD-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-GOLD>>

<ROUTINE FOOD-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-FOOD>>

<ROUTINE WEAPON-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-WEAPON>>

<ROUTINE TREASURE-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-TREASURE>>

<ROUTINE POTION-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-POTION>>

<ROUTINE POTION-AT? (X Y)
    <G? <POTION-OBJ-AT .X .Y> 0>>

<ROUTINE KEY-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-KEY>>

<ROUTINE LOCKEDDOOR-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-LOCKEDDOOR>>

<ROUTINE SHRINE-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-SHRINE>>

<ROUTINE COFFER-OBJ-AT (X Y)
    <ITEM-OBJ-AT .X .Y ,ITEMKIND-COFFER>>

<ROUTINE COFFER-MIMICK? (O)
    <AND <G? .O 0> <==? <GETP .O ,P?R-ITID> ,COFFER-TYPE-MIMICK>>>

<ROUTINE VISION-THREAT-AT? (X Y "AUX" O)
    <COND (<G? <ENEMY-AT .X .Y> 0> <RTRUE>)>
    <SET O <COFFER-OBJ-AT .X .Y>>
    <COND (<COFFER-MIMICK? .O> <RTRUE>)>
    <RFALSE>>

<ROUTINE LOCKED-DOOR-CLOSED-AT? (X Y "AUX" O)
    <SET O <LOCKEDDOOR-OBJ-AT .X .Y>>
    <AND <G? .O 0> <NOT <FSET? .O ,OPENBIT>>>>

<ROUTINE SHRINE-ACTIVE-AT? (X Y "AUX" O)
    <SET O <SHRINE-OBJ-AT .X .Y>>
    <AND <G? .O 0> <NOT <FSET? .O ,OPENBIT>>>>

<ROUTINE ADD-PENDING-KEY (F LOCKTYPE AVOIDRID "AUX" O)
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND ,ITEMKIND-KEY>
    <PUTP .O ,P?R-ITID .LOCKTYPE>
    ;"Pending same-floor keys temporarily store their locked room id in R-ITLVL
      so placement can avoid that room before the key receives coordinates."
    <PUTP .O ,P?R-ITLVL .AVOIDRID>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <MOVE .O <FLOOR-OBJ .F>>
    .O>

<ROUTINE ADD-LOCKEDDOOR (F X Y LOCKTYPE "AUX" O)
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND ,ITEMKIND-LOCKEDDOOR>
    <PUTP .O ,P?R-ITID .LOCKTYPE>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <MOVE .O <FLOOR-OBJ .F>>
    <FCLEAR .O ,OPENBIT>
    .O>

<ROUTINE ADD-SHRINE (F X Y "AUX" O)
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND ,ITEMKIND-SHRINE>
    <PUTP .O ,P?R-ITID 0>
    <PUTP .O ,P?R-ITLVL 0>
    <PUTP .O ,P?R-ITENCH 0>
    <PUTP .O ,P?R-ITAMT 0>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <MOVE .O <FLOOR-OBJ .F>>
    <FCLEAR .O ,OPENBIT>
    .O>

<ROUTINE ADD-COFFER (F X Y SUBTYPE GOLD "AUX" O)
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND ,ITEMKIND-COFFER>
    <PUTP .O ,P?R-ITID .SUBTYPE>
    <PUTP .O ,P?R-ITLVL 0>
    <PUTP .O ,P?R-ITENCH 0>
    <PUTP .O ,P?R-ITAMT .GOLD>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <MOVE .O <FLOOR-OBJ .F>>
    .O>

<ROUTINE COFFER-SPAWN-PCT (F)
    <* 5 <+ 1 </ <- .F 1> 5>>>>

<ROUTINE MIMICK-SPAWN-PCT (F)
    <+ 20 <* 10 </ <- .F 1> 5>>>>

<ROUTINE COFFER-GOLD-AMT (F "AUX" AMT)
    <SET AMT <+ ,COFFER-GOLD-BASE
                 <RNG ,COFFER-GOLD-VARIANCE>
                 <ENEMY-FLOOR-BONUS ,ETYPE-MIMICK .F>>>
    <COND (<G? .AMT 255> <SET AMT 255>)>
    .AMT>

<ROUTINE SPAWN-COFFERS (F "AUX" XY X Y GOLD SUBTYPE PCT MIMPCT)
    <SET PCT <COFFER-SPAWN-PCT .F>>
    <SET MIMPCT <MIMICK-SPAWN-PCT .F>>
    <DO (RID 1 ,ROOM-COUNT)
        <COND (<L=? <RNG 100> .PCT>
               <SET XY <FIND-TREASURE-SPOT-IN-ROOM .RID>>
               <COND (<G? .XY 0>
                      <SET X <WORD16-HI-BYTE .XY>>
                      <SET Y <WORD16-LO-BYTE .XY>>
                      <SET GOLD <COFFER-GOLD-AMT .F>>
                      <SET SUBTYPE
                           <COND (<L=? <RNG 100> .MIMPCT>
                                  ,COFFER-TYPE-MIMICK)
                                 (ELSE ,COFFER-TYPE-COFFER)>>
                      <COND (<ADD-COFFER .F .X .Y .SUBTYPE .GOLD>
                             <FORCE-PASSABLE .X .Y>)>)>)>>
    <RTRUE>>

<ROUTINE TRY-OPEN-COFFER ("AUX" O AMT)
    <SET O <COFFER-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .O 0> <RFALSE>)>
    <COND (<N==? <GETP .O ,P?R-ITID> ,COFFER-TYPE-COFFER> <RFALSE>)>
    <SET AMT <GETP .O ,P?R-ITAMT>>
    <REMOVE .O>
    <FREE-RASCAL-ITEM .O>
    <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
    <SETG PLAYER-GOLD <+ ,PLAYER-GOLD .AMT>>
    <LOG "You open the coffer and collect " N .AMT " gold pieces." CR>
    <RTRUE>>

<ROUTINE TRIGGER-MIMICK-AT (X Y "AUX" O E GOLD)
    <SET O <COFFER-OBJ-AT .X .Y>>
    <COND (<NOT <COFFER-MIMICK? .O>> <RFALSE>)>
    <SET GOLD <GETP .O ,P?R-ITAMT>>
    <COND (<NOT <SET E <ALLOC-RASCAL-ENEMY>>>
           <LOG "The coffer shudders, but nothing emerges." CR>
           <RTRUE>)>
    <PUTP .E ,P?R-ETYPE ,ETYPE-MIMICK>
    <PUTP .E ,P?R-EHP <ENEMY-START-HP ,ETYPE-MIMICK ,CURRENT-FLOOR>>
    <PUTP .E ,P?R-EGOLD .GOLD>
    <PUTP .E ,P?R-EWAIT 1>
    <PUTP .E ,P?R-X .X>
    <PUTP .E ,P?R-Y .Y>
    <MOVE .E ,CURRENT-FLOOR-OBJ>
    <REMOVE .O>
    <FREE-RASCAL-ITEM .O>
    <MARK-DIRTY .X .Y>
    <LOG "The coffer snaps open. It's a mimick!" CR>
    <RTRUE>>

<ROUTINE PLACE-PENDING-KEY-OBJ (O "AUX" TRIES X Y AVOID)
    <SET TRIES 0>
    ;"R-ITLVL contains the locked room ID"
    <SET AVOID <GETP .O ,P?R-ITLVL>>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 500> <RFALSE>)>
        <COND (<NOT <FIND-RANDOM-GROUND-ITEM-SPAWN-POINT 420>> <RFALSE>)>
        <SET X ,ENTRY-X>
        <SET Y ,ENTRY-Y>
        <COND (<G? <ITEM-OBJ-AT .X .Y 0> 0> <AGAIN>)>
        <COND (<AND <G? .AVOID 0>
                    <==? <ROOMID-AT .X .Y> .AVOID>>
               <AGAIN>)>
        <PUTP .O ,P?R-X .X>
        <PUTP .O ,P?R-Y .Y>
        <PUTP .O ,P?R-ITLVL 0>
        <FORCE-PASSABLE .X .Y>
        <RTRUE>>>

<ROUTINE PLACE-PENDING-KEYS (F "AUX" O NXT)
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN T>)>
        <SET NXT <NEXT? .O>>
        <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-KEY>
                    <==? <GETP .O ,P?R-X> 0>
                    <==? <GETP .O ,P?R-Y> 0>>
               <PLACE-PENDING-KEY-OBJ .O>)>
        <SET O .NXT>>
    <RTRUE>>

<ROUTINE ROOM-DEGREE (RID "AUX" DEG)
    <SET DEG 0>
    <DO (B 1 ,ROOM-COUNT)
        <COND (<AND <N==? .B .RID> <CONNECTED-PAIR? .RID .B>>
               <SET DEG <+ .DEG 1>>)>
        .DEG>>

<ROUTINE DOOR-ADJACENT-TO-ROOM? (RID X Y)
    <OR <AND <==? <ROOMID-AT <- .X 1> .Y> .RID>
           <N==? <TILE-AT <- .X 1> .Y> ,TILE-DOOR>>
       <AND <==? <ROOMID-AT <+ .X 1> .Y> .RID>
           <N==? <TILE-AT <+ .X 1> .Y> ,TILE-DOOR>>
       <AND <==? <ROOMID-AT .X <- .Y 1>> .RID>
           <N==? <TILE-AT .X <- .Y 1>> ,TILE-DOOR>>
       <AND <==? <ROOMID-AT .X <+ .Y 1>> .RID>
           <N==? <TILE-AT .X <+ .Y 1>> ,TILE-DOOR>>>>

<ROUTINE ROOM-NONDOOR-OPENING-COUNT (RID "AUX" X Y L T R B CNT)
    <SET L <ROOM-GET .RID ,ROOM-L>>
    <SET T <ROOM-GET .RID ,ROOM-T>>
    <SET R <ROOM-GET .RID ,ROOM-R>>
    <SET B <ROOM-GET .RID ,ROOM-B>>
    <SET CNT 0>

    <SET Y .T>
    <REPEAT ()
     <COND (<G? .Y .B> <RETURN .CNT>)>
     <SET X .L>
     <REPEAT ()
         <COND (<G? .X .R> <SET Y <+ .Y 1>> <RETURN>)>
         <COND (<N==? <ROOMID-AT .X .Y> .RID>
             <SET X <+ .X 1>>
             <AGAIN>)>

         <COND (<AND <IN-BOUNDS? <- .X 1> .Y>
               <N==? <ROOMID-AT <- .X 1> .Y> .RID>
               <FLOOR? <- .X 1> .Y>
               <N==? <TILE-AT <- .X 1> .Y> ,TILE-DOOR>>
             <SET CNT <+ .CNT 1>>)>
         <COND (<AND <IN-BOUNDS? <+ .X 1> .Y>
               <N==? <ROOMID-AT <+ .X 1> .Y> .RID>
               <FLOOR? <+ .X 1> .Y>
               <N==? <TILE-AT <+ .X 1> .Y> ,TILE-DOOR>>
             <SET CNT <+ .CNT 1>>)>
         <COND (<AND <IN-BOUNDS? .X <- .Y 1>>
               <N==? <ROOMID-AT .X <- .Y 1>> .RID>
               <FLOOR? .X <- .Y 1>>
               <N==? <TILE-AT .X <- .Y 1>> ,TILE-DOOR>>
             <SET CNT <+ .CNT 1>>)>
         <COND (<AND <IN-BOUNDS? .X <+ .Y 1>>
               <N==? <ROOMID-AT .X <+ .Y 1>> .RID>
               <FLOOR? .X <+ .Y 1>>
               <N==? <TILE-AT .X <+ .Y 1>> ,TILE-DOOR>>
             <SET CNT <+ .CNT 1>>)>

         <SET X <+ .X 1>>>>

        .CNT>

    <ROUTINE ROOM-HAS-ONLY-TREASURE-EXIT? (RID DOORX DOORY "AUX" X Y L T R B CNT)
        <SET L <ROOM-GET .RID ,ROOM-L>>
        <SET T <ROOM-GET .RID ,ROOM-T>>
        <SET R <ROOM-GET .RID ,ROOM-R>>
        <SET B <ROOM-GET .RID ,ROOM-B>>
        <SET CNT 0>

        <SET Y <- .T 1>>
        <REPEAT ()
            <COND (<G? .Y <+ .B 1>> <RETURN>)>
            <SET X <- .L 1>>
            <REPEAT ()
                <COND (<G? .X <+ .R 1>> <SET Y <+ .Y 1>> <RETURN>)>
                <COND (<VALID-TREASURE-DOOR? .RID .X .Y>
                       <SET CNT <+ .CNT 1>>
                       <COND (<OR <N==? .X .DOORX> <N==? .Y .DOORY>> <RFALSE>)>)>
                <SET X <+ .X 1>>>>

        <COND (<N==? .CNT 1> <RFALSE>)>
        <COND (<G? <ROOM-NONDOOR-OPENING-COUNT .RID> 0> <RFALSE>)>
        <RTRUE>>

<ROUTINE FIND-ROOM-DOOR-TILE (RID "AUX" X Y L T R B)
    <SET L <ROOM-GET .RID ,ROOM-L>>
    <SET T <ROOM-GET .RID ,ROOM-T>>
    <SET R <ROOM-GET .RID ,ROOM-R>>
    <SET B <ROOM-GET .RID ,ROOM-B>>
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>

    ;"Doors are tagged with the room ID, so scan the expanded room box."

    <SET Y <- .T 1>>
    <REPEAT ()
        <COND (<G? .Y <+ .B 1>> <RFALSE>)>
        <SET X <- .L 1>>
        <REPEAT ()
            <COND (<G? .X <+ .R 1>> <SET Y <+ .Y 1>> <RETURN>)>
            <COND (<AND <IN-BOUNDS? .X .Y>
                        <==? <TILE-AT .X .Y> ,TILE-DOOR>
                        <DOOR-ADJACENT-TO-ROOM? .RID .X .Y>
                        <L=? <ITEM-OBJ-AT .X .Y 0> 0>>
                   <SETG ENTRY-X .X>
                   <SETG ENTRY-Y .Y>
                   <RTRUE>)>
            <SET X <+ .X 1>>>>
    <RFALSE>>

<ROUTINE ROOM-HAS-TILE? (RID TILE "AUX" X Y L T R B)
    <SET L <ROOM-GET .RID ,ROOM-L>>
    <SET T <ROOM-GET .RID ,ROOM-T>>
    <SET R <ROOM-GET .RID ,ROOM-R>>
    <SET B <ROOM-GET .RID ,ROOM-B>>
    <SET Y .T>
    <REPEAT ()
        <COND (<G? .Y .B> <RFALSE>)>
        <SET X .L>
        <REPEAT ()
            <COND (<G? .X .R> <SET Y <+ .Y 1>> <RETURN>)>
            <COND (<AND <==? <ROOMID-AT .X .Y> .RID>
                        <==? <TILE-AT .X .Y> .TILE>>
                   <RTRUE>)>
            <SET X <+ .X 1>>>>>

<ROUTINE ROOM-BLOCKED-FOR-TREASURE? (RID "AUX" UPX UPY DOWNX DOWNY)
    <SET UPX <FLOOR-UP-X ,CURRENT-FLOOR>>
    <SET UPY <FLOOR-UP-Y ,CURRENT-FLOOR>>
    <SET DOWNX <FLOOR-DOWN-X ,CURRENT-FLOOR>>
    <SET DOWNY <FLOOR-DOWN-Y ,CURRENT-FLOOR>>
    <COND (<AND <G? .UPX 0> <G? .UPY 0> <==? <ROOMID-AT .UPX .UPY> .RID>>
           <RTRUE>)>
    <COND (<AND <G? .DOWNX 0>
                <G? .DOWNY 0>
                <==? <ROOMID-AT .DOWNX .DOWNY> .RID>>
           <RTRUE>)>
    <COND (<AND ,TRADER-ON?
                <G? ,TRADER-X 0>
                <G? ,TRADER-Y 0>
                <==? <ROOMID-AT ,TRADER-X ,TRADER-Y> .RID>>
           <RTRUE>)>
    ;"Stairs tiles sometimes have ROOMID=0 (forced passable), so also block
    any room whose carved area contains stairs regardless of ROOMIDS."
    <COND (<ROOM-HAS-TILE? .RID ,TILE-STAIR-UP> <RTRUE>)>
    <COND (<ROOM-HAS-TILE? .RID ,TILE-STAIR-DOWN> <RTRUE>)>
    <COND (<ROOM-HAS-TILE? .RID ,TILE-INTERIOR> <RTRUE>)>
    <RFALSE>>

<ROUTINE FIND-LEAF-ROOM-DOOR (ENTRYRID "AUX" TRIES RID DEG)
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 120> <RETURN 0>)>
        <SET RID <RNG ,ROOM-COUNT>>
        <COND (<==? .RID .ENTRYRID> <AGAIN>)>
        <SET DEG <ROOM-DEGREE .RID>>
        <COND (<N==? .DEG 1> <AGAIN>)>
        <COND (<ROOM-BLOCKED-FOR-TREASURE? .RID> <AGAIN>)>
        <COND (<FIND-ROOM-DOOR-TILE .RID>
               <COND (<ROOM-HAS-ONLY-TREASURE-EXIT? .RID ,ENTRY-X ,ENTRY-Y>
                      <RETURN .RID>)>)>
        <AGAIN>>>

<ROUTINE FIND-LEAF-ROOM-DOOR-ANY (ENTRYRID)
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    <DO (RID 1 ,ROOM-COUNT)
        <COND (<AND <N==? .RID .ENTRYRID>
                    <==? <ROOM-DEGREE .RID> 1>
                    <NOT <ROOM-BLOCKED-FOR-TREASURE? .RID>>
                    <FIND-ROOM-DOOR-TILE .RID>
                    <ROOM-HAS-ONLY-TREASURE-EXIT? .RID ,ENTRY-X ,ENTRY-Y>>
               <RETURN .RID>)>>
    0>

<ROUTINE FIND-TREASURE-SPOT-IN-ROOM (RID "AUX" TRIES X Y)
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 260> <RETURN 0>)>
        <COND (<NOT <RANDOM-POINT-IN-ROOM .RID>> <AGAIN>)>
        <SET X ,ENTRY-X>
        <SET Y ,ENTRY-Y>
        <COND (<NOT <IN-BOUNDS? .X .Y>> <AGAIN>)>
        <COND (<==? <TILE-AT .X .Y> ,TILE-DOOR> <AGAIN>)>
        <COND (<NOT <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
                        <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>>>
               <AGAIN>)>
        <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>
               <AGAIN>)>
        <COND (<TRADER-AT? .X .Y> <AGAIN>)>
        <COND (<G? <ENEMY-AT .X .Y> 0> <AGAIN>)>
        <COND (<G? <ITEM-OBJ-AT .X .Y 0> 0> <AGAIN>)>
        <RETURN <WORD16-FROM-BYTES .X .Y>>>>

<ROUTINE VALID-TREASURE-DOOR? (RID X Y "AUX" NX NY HAS-IN HAS-OUT)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <COND (<N==? <TILE-AT .X .Y> ,TILE-DOOR> <RFALSE>)>

    <SET HAS-IN <>>
    <SET HAS-OUT <>>

    <SET NX <- .X 1>>
    <SET NY .Y>
    <COND (<IN-BOUNDS? .NX .NY>
           <COND (<AND <==? <ROOMID-AT .NX .NY> .RID>
                       <NOT <==? <TILE-AT .NX .NY> ,TILE-DOOR>>>
                  <SET HAS-IN T>)
                 (<AND <N==? <ROOMID-AT .NX .NY> .RID> <FLOOR? .NX .NY>>
                  <SET HAS-OUT T>)>)>

    <SET NX <+ .X 1>>
    <SET NY .Y>
    <COND (<IN-BOUNDS? .NX .NY>
           <COND (<AND <==? <ROOMID-AT .NX .NY> .RID>
                       <NOT <==? <TILE-AT .NX .NY> ,TILE-DOOR>>>
                  <SET HAS-IN T>)
                 (<AND <N==? <ROOMID-AT .NX .NY> .RID> <FLOOR? .NX .NY>>
                  <SET HAS-OUT T>)>)>

    <SET NX .X>
    <SET NY <- .Y 1>>
    <COND (<IN-BOUNDS? .NX .NY>
           <COND (<AND <==? <ROOMID-AT .NX .NY> .RID>
                       <NOT <==? <TILE-AT .NX .NY> ,TILE-DOOR>>>
                  <SET HAS-IN T>)
                 (<AND <N==? <ROOMID-AT .NX .NY> .RID> <FLOOR? .NX .NY>>
                  <SET HAS-OUT T>)>)>

    <SET NX .X>
    <SET NY <+ .Y 1>>
    <COND (<IN-BOUNDS? .NX .NY>
           <COND (<AND <==? <ROOMID-AT .NX .NY> .RID>
                       <NOT <==? <TILE-AT .NX .NY> ,TILE-DOOR>>>
                  <SET HAS-IN T>)
                 (<AND <N==? <ROOMID-AT .NX .NY> .RID> <FLOOR? .NX .NY>>
                  <SET HAS-OUT T>)>)>

    <AND .HAS-IN .HAS-OUT>>

<ROUTINE SHRINE-NOOK-CANDIDATE? (X Y "AUX" NX NY WALLS DOORS T)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <COND (<NOT <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
                    <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>>>
           <RFALSE>)>
    <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
               <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>
               <==? <TILE-AT .X .Y> ,TILE-INTERIOR>>
           <RFALSE>)>
    <COND (<AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>> <RFALSE>)>
    <COND (<TRADER-AT? .X .Y> <RFALSE>)>
    <COND (<G? <ENEMY-AT .X .Y> 0> <RFALSE>)>
    <COND (<G? <ITEM-OBJ-AT .X .Y 0> 0> <RFALSE>)>

    <SET WALLS 0>
    <SET DOORS 0>
    <DO (DY -1 1)
        <DO (DX -1 1)
            <COND (<NOT <AND <==? .DX 0> <==? .DY 0>>>
                   <SET NX <+ .X .DX>>
                   <SET NY <+ .Y .DY>>
                   <COND (<NOT <IN-BOUNDS? .NX .NY>> <RFALSE>)>
                   <SET T <TILE-AT .NX .NY>>
                   <COND (<==? .T ,TILE-WALL> <SET WALLS <+ .WALLS 1>>)
                         (<==? .T ,TILE-DOOR> <SET DOORS <+ .DOORS 1>>)>)>>>

    <AND <==? .WALLS 7> <==? .DOORS 1>>>

<ROUTINE PRECOMPUTE-SHRINES-FOR-CURRENT-FLOOR (F "AUX" X Y DX DY CX CY)
    <SET Y 1>
    <REPEAT ()
        <COND (<G? .Y ,MAP-H> <RTRUE>)>
        <SET X 1>
        <REPEAT ()
            <COND (<G? .X ,MAP-W>
                   <SET Y <+ .Y 1>>
                   <RETURN>)>
            <COND (<==? <TILE-AT .X .Y> ,TILE-DOOR>
                   <SET DY -1>
                   <REPEAT ()
                       <COND (<G? .DY 1> <RETURN>)>
                       <SET DX -1>
                       <REPEAT ()
                           <COND (<G? .DX 1>
                                  <SET DY <+ .DY 1>>
                                  <RETURN>)>
                           <COND (<NOT <AND <==? .DX 0> <==? .DY 0>>>
                                  <SET CX <+ .X .DX>>
                                  <SET CY <+ .Y .DY>>
                                  <COND (<AND <SHRINE-NOOK-CANDIDATE? .CX .CY>
                                              <L=? <SHRINE-OBJ-AT .CX .CY> 0>
                                              <L=? <RNG 100> ,SHRINE-SPAWN-PCT>>
                                         <COND (<ADD-SHRINE .F .CX .CY>
                                                <FORCE-PASSABLE .CX .CY>)>)>)>
                           <SET DX <+ .DX 1>>>>)>
            <SET X <+ .X 1>>>>>

;"Pick a key floor for a locked door based on the lock metal.

Golden and silver keys may appear on any floor.
Bronze and copper keys may appear on the same floor or an earlier floor.
Nickel and cobalt keys always appear on the same floor as their lock."

<ROUTINE KEY-FLOOR-WEIGHT (DOORF TARGETF "AUX" DIST)
    <SET DIST <ABS <- .TARGETF .DOORF>>>
    <MAX 1 <- 8 .DIST>>>

<ROUTINE PICK-KEY-FLOOR-FOR-LOCK (DOORF LOCKTYPE "AUX" MINF MAXF TOTAL ROLL SUM)
    <COND (<OR <==? .LOCKTYPE ,LOCK-GOLDEN>
               <==? .LOCKTYPE ,LOCK-SILVER>>
           <SET MINF 1>
           <SET MAXF ,MAX-FLOORS>)
          (<OR <==? .LOCKTYPE ,LOCK-BRONZE>
               <==? .LOCKTYPE ,LOCK-COPPER>>
           <SET MINF 1>
           <SET MAXF .DOORF>)
          (ELSE
           <RETURN .DOORF>)>

    <SET TOTAL 0>
    <DO (F .MINF .MAXF)
        <SET TOTAL <+ .TOTAL <KEY-FLOOR-WEIGHT .DOORF .F>>>>

    <COND (<L=? .TOTAL 0> <RETURN .DOORF>)>
    <SET ROLL <RNG .TOTAL>>
    <SET SUM 0>
    <DO (F .MINF .MAXF)
        <SET SUM <+ .SUM <KEY-FLOOR-WEIGHT .DOORF .F>>>
        <COND (<L=? .ROLL .SUM> <RETURN .F>)>>

    .DOORF>

<ROUTINE TREASURE-ROOM-GOLD-AMT (LOCKTYPE "AUX" AMT)
    <SET AMT <+ ,TREASURE-LOOT-GOLD-BASE <RNG ,TREASURE-LOOT-GOLD-VARIANCE>>>
    <COND (<OR <==? .LOCKTYPE ,LOCK-GOLDEN>
               <==? .LOCKTYPE ,LOCK-SILVER>>
           <RETURN <PRICE-PLUS-HALF .AMT>>)
          (<OR <==? .LOCKTYPE ,LOCK-NICKEL>
               <==? .LOCKTYPE ,LOCK-COBALT>>
           <RETURN <MAX 1 </ <* .AMT 2> 3>>>)>
    .AMT>

<ROUTINE FIND-TREASURE-SPOT-IN-ROOM-EXCEPT (RID AVOIDX AVOIDY "AUX" TRIES LOOTXY X Y)
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 60> <RETURN 0>)>
        <SET LOOTXY <FIND-TREASURE-SPOT-IN-ROOM .RID>>
        <COND (<L=? .LOOTXY 0> <RETURN 0>)>
        <SET X <WORD16-HI-BYTE .LOOTXY>>
        <SET Y <WORD16-LO-BYTE .LOOTXY>>
        <COND (<AND <==? .X .AVOIDX> <==? .Y .AVOIDY>> <AGAIN>)>
        <RETURN .LOOTXY>>>

;"Store the precomputed treasure loot for a floor."

<ROUTINE PICK-STAT-BOOST-POTION-COLOR ("AUX" PICK TYPE COLOR)
    <SET PICK <RNG 3>>
    <SET TYPE
         <COND (<==? .PICK 1> ,POTION-MUSCLE)
               (<==? .PICK 2> ,POTION-HEALTH)
               (ELSE ,POTION-METTLE)>>
    <SET COLOR <GETB ,POTION-COLOR-FOR-TYPE <- .TYPE 1>>>
    <COND (<L? .COLOR 1>
           <SET COLOR <GETB ,POTION-COLOR-FOR-TYPE <- ,POTION-HEALTH 1>>>
           <COND (<L? .COLOR 1>
                  <SET COLOR
                       <GETB ,POTION-COLOR-FOR-TYPE <- ,POTION-MUSCLE 1>>>)>
           <COND (<L? .COLOR 1>
                  <SET COLOR
                       <GETB ,POTION-COLOR-FOR-TYPE <- ,POTION-METTLE 1>>>)>
           <COND (<L? .COLOR 1> <SET COLOR 1>)>)>
    .COLOR>

<ROUTINE PRECOMPUTE-TREASURE-LOOT (F LOCKTYPE CHOICE "AUX" TYPE LVL ENCH COLOR AMT)
    <PUTB ,TREASURE-ROOM-BONUS-GOLD-X <- .F 1> 0>
    <PUTB ,TREASURE-ROOM-BONUS-GOLD-Y <- .F 1> 0>
    <PUT ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1> 0>
    <COND (<==? .CHOICE 1>
           <SET TYPE <RNG ,WEAPON-COUNT>>
           <SET LVL <+ <ROLL-LOOT-WEAPON-LEVEL .F> 2>>
           <COND (<G? .LVL 15> <SET LVL 15>)>
           <SET ENCH <+ <ROLL-LOOT-WEAPON-ENCH> 2>>
           <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> ,TREASURE-LOOT-WEAPON>
           <PUTB ,TREASURE-ROOM-LOOT-ID <- .F 1> .TYPE>
           <PUTB ,TREASURE-ROOM-LOOT-LVL <- .F 1> .LVL>
           <PUTB ,TREASURE-ROOM-LOOT-ENCH <- .F 1> .ENCH>
           <SET AMT </ <TREASURE-ROOM-GOLD-AMT .LOCKTYPE> 3>>
           <PUT ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1> .AMT>)
          (<==? .CHOICE 2>
           <SET COLOR <PICK-STAT-BOOST-POTION-COLOR>>
           <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> ,TREASURE-LOOT-POTION>
           <PUTB ,TREASURE-ROOM-LOOT-ID <- .F 1> .COLOR>
           <SET AMT </ <TREASURE-ROOM-GOLD-AMT .LOCKTYPE> 3>>
           <PUT ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1> .AMT>)
          (ELSE
           <SET AMT <TREASURE-ROOM-GOLD-AMT .LOCKTYPE>>
           <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> ,TREASURE-LOOT-GOLD>
           <PUT ,TREASURE-ROOM-LOOT-AMT <- .F 1> .AMT>)>
    <RTRUE>>

;"Precompute treasure rooms and matching keys for all floors.

This simulates a full descent so keys can be placed on earlier floors."

<ROUTINE PRECOMPUTE-TREASURE-ROOM-PLANS ("AUX" LANDX LANDY ENTRYRID RID DOORX DOORY LOOTXY LOOTX LOOTY LOCKTYPE CHOICE BONUSXY)
    <SET LANDX 0>
    <SET LANDY 0>
    <DO (F 1 ,MAX-FLOORS)
        <SETG CURRENT-FLOOR .F>
        <SETG CURRENT-FLOOR-OBJ <FLOOR-OBJ .F>>
        <SEED-RNG-32 <FLOOR-SEED-HI .F> <FLOOR-SEED-LO .F>>

        <COND (<G? .F 1>
               <SETG FORCED-ENTRY? T>
               <SETG ENTRY-X .LANDX>
               <SETG ENTRY-Y .LANDY>
               <FLOOR-SET-UP-STAIRS .F .LANDX .LANDY>)
              (ELSE
               <SETG FORCED-ENTRY? <>>
               <SETG ENTRY-X 0>
               <SETG ENTRY-Y 0>)>

        <BUILD-DUNGEON>
        <ENTER-FLOOR-PLACE-UP-STAIRS .F>
        <ENTER-FLOOR-PLACE-PLAYER .LANDX .LANDY>
        <ENTER-FLOOR-PLACE-DOWN-STAIRS .F>

        <SET LANDX ,ENTRY-X>
        <SET LANDY ,ENTRY-Y>

        <LOAD-FLOOR-TRADER .F>
        <PLACE-INTERIOR-ENTRANCES .F>

        <SET ENTRYRID <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
        <COND (<L=? .ENTRYRID 0> <SET ENTRYRID ,CURRENT-ROOM>)>

        <SET DOORX 0>
        <SET DOORY 0>
        <SET LOOTX 0>
        <SET LOOTY 0>

        <SET RID <FIND-LEAF-ROOM-DOOR .ENTRYRID>>
        <COND (<G? .RID 0>
               <SET DOORX ,ENTRY-X>
               <SET DOORY ,ENTRY-Y>
               <SET LOOTXY <FIND-TREASURE-SPOT-IN-ROOM .RID>>
               <COND (<G? .LOOTXY 0>
                      <SET LOOTX <WORD16-HI-BYTE .LOOTXY>>
                      <SET LOOTY <WORD16-LO-BYTE .LOOTXY>>)>)>

        ;"Deterministic fallback: if random tries missed, scan for any valid opportunity."
        <COND (<OR <L=? .DOORX 0> <L=? .DOORY 0> <L=? .LOOTX 0> <L=? .LOOTY 0>>
               <SET DOORX 0>
               <SET DOORY 0>
               <SET LOOTX 0>
               <SET LOOTY 0>
               <SET RID <FIND-LEAF-ROOM-DOOR-ANY .ENTRYRID>>
               <COND (<G? .RID 0>
                      <SET DOORX ,ENTRY-X>
                      <SET DOORY ,ENTRY-Y>
                      <SET LOOTXY <FIND-TREASURE-SPOT-IN-ROOM .RID>>
                      <COND (<G? .LOOTXY 0>
                             <SET LOOTX <WORD16-HI-BYTE .LOOTXY>>
                             <SET LOOTY <WORD16-LO-BYTE .LOOTXY>>)>)>)>

        <COND (<OR <L=? .DOORX 0> <L=? .DOORY 0> <L=? .LOOTX 0> <L=? .LOOTY 0>>
               <PUTB ,TREASURE-ROOM-LOCKTYPE <- .F 1> 0>
               <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> 0>)
              (ELSE
               ;"Only lock a door that is valid for the chosen leaf room."
               <COND (<OR <L=? .RID 0>
                          <==? .RID .ENTRYRID>
                          <N==? <ROOM-DEGREE .RID> 1>
                          <ROOM-BLOCKED-FOR-TREASURE? .RID>
                          <NOT <ROOM-HAS-ONLY-TREASURE-EXIT? .RID
                                                             .DOORX
                                                             .DOORY>>>
                      <PUTB ,TREASURE-ROOM-LOCKTYPE <- .F 1> 0>
                      <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> 0>)
                     (ELSE
                      <SET LOCKTYPE <RNG ,LOCK-TYPE-COUNT>>
                      <PUTB ,TREASURE-ROOM-DOOR-X <- .F 1> .DOORX>
                      <PUTB ,TREASURE-ROOM-DOOR-Y <- .F 1> .DOORY>
                      <PUTB ,TREASURE-ROOM-LOCKTYPE <- .F 1> .LOCKTYPE>
                      <PUTB ,TREASURE-ROOM-LOOT-X <- .F 1> .LOOTX>
                      <PUTB ,TREASURE-ROOM-LOOT-Y <- .F 1> .LOOTY>
                      <SET CHOICE <RNG 3>>
                      <PRECOMPUTE-TREASURE-LOOT .F .LOCKTYPE .CHOICE>
                      <COND (<NOT <==? .CHOICE 3>>
                             <SET BONUSXY
                                  <FIND-TREASURE-SPOT-IN-ROOM-EXCEPT .RID
                                                                     .LOOTX
                                                                     .LOOTY>>
                             <COND (<G? .BONUSXY 0>
                                    <PUTB ,TREASURE-ROOM-BONUS-GOLD-X
                                          <- .F 1>
                                          <WORD16-HI-BYTE .BONUSXY>>
                                    <PUTB ,TREASURE-ROOM-BONUS-GOLD-Y
                                          <- .F 1>
                                          <WORD16-LO-BYTE .BONUSXY>>)>)>
                      <COND (<NOT <ADD-LOCKEDDOOR .F .DOORX .DOORY .LOCKTYPE>>
                             <PUTB ,TREASURE-ROOM-LOCKTYPE <- .F 1> 0>
                             <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> 0>
                             <PUTB ,TREASURE-ROOM-BONUS-GOLD-X <- .F 1> 0>
                             <PUTB ,TREASURE-ROOM-BONUS-GOLD-Y <- .F 1> 0>
                             <PUT ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1> 0>
                             <AGAIN>)>
                      <SET CHOICE <PICK-KEY-FLOOR-FOR-LOCK .F .LOCKTYPE>>
                      <ADD-PENDING-KEY .CHOICE
                                       .LOCKTYPE
                                       <COND (<==? .CHOICE .F> .RID)
                                             (ELSE 0)>>)>)>
        <PRECOMPUTE-SHRINES-FOR-CURRENT-FLOOR .F>>
    <RTRUE>>

;"Place precomputed treasure loot for a floor.

Locked doors and keys are created during startup precompute."

<ROUTINE PLACE-PRECOMPUTED-TREASURE-ROOM (F "AUX" DOORX DOORY LOCKTYPE LOOTX LOOTY LOOTKIND ID LVL ENCH AMT BONUSX BONUSY BONUSAMT)
    <COND (<G? <GETB ,TREASURE-ROOM-CREATED <- .F 1>> 0> <RTRUE>)>
    <SET LOCKTYPE <GETB ,TREASURE-ROOM-LOCKTYPE <- .F 1>>>
    <COND (<L=? .LOCKTYPE 0> <RTRUE>)>

    <SET DOORX <GETB ,TREASURE-ROOM-DOOR-X <- .F 1>>>
    <SET DOORY <GETB ,TREASURE-ROOM-DOOR-Y <- .F 1>>>
    <SET LOOTX <GETB ,TREASURE-ROOM-LOOT-X <- .F 1>>>
    <SET LOOTY <GETB ,TREASURE-ROOM-LOOT-Y <- .F 1>>>
    <COND (<OR <L=? .DOORX 0> <L=? .DOORY 0>> <RTRUE>)>

    ;"Door is expected to have been pre-placed during startup planning.
            Compatibility fallback: recreate it if absent."
    <COND (<L=? <LOCKEDDOOR-OBJ-AT .DOORX .DOORY> 0>
           <COND (<NOT <ADD-LOCKEDDOOR .F .DOORX .DOORY .LOCKTYPE>> <RTRUE>)>)>

    <PUTB ,TREASURE-ROOM-CREATED <- .F 1> 1>

    <COND (<OR <L=? .LOOTX 0> <L=? .LOOTY 0>> <RTRUE>)>
    <COND (<G? <ITEM-OBJ-AT .LOOTX .LOOTY 0> 0> <RTRUE>)>

    <SET LOOTKIND <GETB ,TREASURE-ROOM-LOOT-KIND <- .F 1>>>
    <SET ID <GETB ,TREASURE-ROOM-LOOT-ID <- .F 1>>>
    <COND (<==? .LOOTKIND ,TREASURE-LOOT-WEAPON>
           <SET LVL <GETB ,TREASURE-ROOM-LOOT-LVL <- .F 1>>>
           <SET ENCH <GETB ,TREASURE-ROOM-LOOT-ENCH <- .F 1>>>
           <ADD-WEAPON-PILE .LOOTX .LOOTY .ID .LVL .ENCH>)
          (<==? .LOOTKIND ,TREASURE-LOOT-POTION>
           <ADD-POTION-PILE .LOOTX .LOOTY .ID>)
          (<==? .LOOTKIND ,TREASURE-LOOT-GOLD>
           <SET AMT <GET ,TREASURE-ROOM-LOOT-AMT <- .F 1>>>
           <ADD-GOLD-PILE .LOOTX .LOOTY .AMT>)>

    <SET BONUSAMT <GET ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1>>>
    <COND (<L=? .BONUSAMT 0> <RTRUE>)>
    <SET BONUSX <GETB ,TREASURE-ROOM-BONUS-GOLD-X <- .F 1>>>
    <SET BONUSY <GETB ,TREASURE-ROOM-BONUS-GOLD-Y <- .F 1>>>
    <COND (<OR <L=? .BONUSX 0> <L=? .BONUSY 0>> <RTRUE>)>
    <COND (<L=? <ITEM-OBJ-AT .BONUSX .BONUSY 0> 0>
           <ADD-GOLD-PILE .BONUSX .BONUSY .BONUSAMT>)>
    <RTRUE>>

<ROUTINE SHRINE-SET-OFFER (SLOT KIND ID LVL ENCH AMT)
    <COND (<==? .SLOT 1>
           <SETG SHRINE-OFFER1-KIND .KIND>
           <SETG SHRINE-OFFER1-ID .ID>
           <SETG SHRINE-OFFER1-LVL .LVL>
           <SETG SHRINE-OFFER1-ENCH .ENCH>
           <SETG SHRINE-OFFER1-AMT .AMT>)
          (ELSE
           <SETG SHRINE-OFFER2-KIND .KIND>
           <SETG SHRINE-OFFER2-ID .ID>
           <SETG SHRINE-OFFER2-LVL .LVL>
           <SETG SHRINE-OFFER2-ENCH .ENCH>
           <SETG SHRINE-OFFER2-AMT .AMT>)>
    <RTRUE>>

<ROUTINE SHRINE-ROLL-OFFER (F SLOT "AUX" PICK TYPE LVL ENCH ID AMT)
    <SET PICK <RNG 4>>
    <COND (<==? .PICK 1>
           <SET TYPE <RNG ,WEAPON-COUNT>>
           <SET LVL <+ <ROLL-LOOT-WEAPON-LEVEL .F> 1>>
           <COND (<G? .LVL 15> <SET LVL 15>)>
           <SET ENCH <ROLL-LOOT-WEAPON-ENCH>>
           <COND (<L? .ENCH 1> <SET ENCH 1>)>
           <SHRINE-SET-OFFER .SLOT ,ITEMKIND-WEAPON .TYPE .LVL .ENCH 0>)
          (<==? .PICK 2>
           <SET AMT <+ ,SHRINE-OFFER-GOLD-BASE <RNG ,SHRINE-OFFER-GOLD-VARIANCE>>>
           <SHRINE-SET-OFFER .SLOT ,ITEMKIND-GOLD 0 0 0 .AMT>)
          (<==? .PICK 3>
           <SET ID <ROLL-LOOT-POTION-COLOR>>
           <SHRINE-SET-OFFER .SLOT ,ITEMKIND-POTION .ID 0 0 0>)
          (ELSE
           <SET ID <COND (<==? <RNG 2> 1> ,FOOD-MUFFIN) (ELSE ,FOOD-TURKEY)>>
           <SHRINE-SET-OFFER .SLOT ,ITEMKIND-FOOD .ID 0 0 0>)>
    <RTRUE>>

<ROUTINE SHRINE-OFFERS-MATCH? ()
    <AND <==? ,SHRINE-OFFER1-KIND ,SHRINE-OFFER2-KIND>
         <==? ,SHRINE-OFFER1-ID ,SHRINE-OFFER2-ID>
         <==? ,SHRINE-OFFER1-LVL ,SHRINE-OFFER2-LVL>
         <==? ,SHRINE-OFFER1-ENCH ,SHRINE-OFFER2-ENCH>
         <==? ,SHRINE-OFFER1-AMT ,SHRINE-OFFER2-AMT>>>

<ROUTINE SHRINE-OFFER-CONFLICT? ()
    <OR <AND <==? ,SHRINE-OFFER1-KIND ,ITEMKIND-GOLD>
             <==? ,SHRINE-OFFER2-KIND ,ITEMKIND-GOLD>>
        <AND <==? ,SHRINE-OFFER1-KIND ,ITEMKIND-WEAPON>
             <==? ,SHRINE-OFFER2-KIND ,ITEMKIND-WEAPON>
             <==? ,SHRINE-OFFER1-ID ,SHRINE-OFFER2-ID>>
        <AND <==? ,SHRINE-OFFER1-KIND ,ITEMKIND-POTION>
             <==? ,SHRINE-OFFER2-KIND ,ITEMKIND-POTION>
             <==? ,SHRINE-OFFER1-ID ,SHRINE-OFFER2-ID>>>>

<ROUTINE GENERATE-SHRINE-OFFERS (F SX SY "AUX" TRIES MIXHI MIXLO SEEDHI SEEDLO)
    <RNG-SAVE-STATE>

    <SET MIXHI <+ <* .SX 257> <* .SY 911> <* .F 37>>>
    <SET MIXLO <+ <* .SX 149> <* .SY 313> <* .F 71> 1>>
    <SET SEEDHI <RNG-XOR16 <FLOOR-SEED-HI .F> .MIXHI>>
    <SET SEEDLO <RNG-XOR16 <FLOOR-SEED-LO .F> .MIXLO>>
    <SEED-RNG-32 .SEEDHI .SEEDLO>

    <SHRINE-ROLL-OFFER .F 1>
    <SET TRIES 0>
    <REPEAT ()
        <SHRINE-ROLL-OFFER .F 2>
        <COND (<AND <NOT <SHRINE-OFFERS-MATCH?>> <NOT <SHRINE-OFFER-CONFLICT?>>>
               <RETURN>)>
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 16> <RETURN>)>>

    <RNG-RESTORE-STATE>
    <RTRUE>>

<ROUTINE SHRINE-PRINT-OFFER-TEXT (SLOT LOG? "AUX" KIND ID LVL ENCH AMT)
    <SET KIND <SHRINE-GET-OFFER-KIND .SLOT>>
    <SET ID <SHRINE-GET-OFFER-ID .SLOT>>
    <SET LVL <SHRINE-GET-OFFER-LVL .SLOT>>
    <SET ENCH <SHRINE-GET-OFFER-ENCH .SLOT>>
    <SET AMT <SHRINE-GET-OFFER-AMT .SLOT>>
    <COND (<==? .KIND ,ITEMKIND-WEAPON>
           <TELL/LOG .LOG? "a level " N .LVL>
           <COND (<G? .ENCH 0> <TELL/LOG .LOG? "+" N .ENCH>)>
           <TELL/LOG .LOG? " " WEAPON-NAME .ID>)
          (<==? .KIND ,ITEMKIND-GOLD> <TELL/LOG .LOG? N .AMT " gold pieces">)
          (<==? .KIND ,ITEMKIND-POTION>
           <TELL/LOG .LOG? <POTION-ARTICLE .ID> " " POTION-DISPLAY-NAME .ID>)
          (<==? .KIND ,ITEMKIND-FOOD> <TELL/LOG .LOG? "a " FOOD-NAME .ID>)
          (ELSE <TELL/LOG .LOG? "an offering">)>
    <RTRUE>>

<ROUTINE SHRINE-GET-OFFER-KIND (SLOT)
    <COND (<==? .SLOT 1> ,SHRINE-OFFER1-KIND)
          (ELSE ,SHRINE-OFFER2-KIND)>>

<ROUTINE SHRINE-GET-OFFER-ID (SLOT)
    <COND (<==? .SLOT 1> ,SHRINE-OFFER1-ID)
          (ELSE ,SHRINE-OFFER2-ID)>>

<ROUTINE SHRINE-GET-OFFER-LVL (SLOT)
    <COND (<==? .SLOT 1> ,SHRINE-OFFER1-LVL)
          (ELSE ,SHRINE-OFFER2-LVL)>>

<ROUTINE SHRINE-GET-OFFER-ENCH (SLOT)
    <COND (<==? .SLOT 1> ,SHRINE-OFFER1-ENCH)
          (ELSE ,SHRINE-OFFER2-ENCH)>>

<ROUTINE SHRINE-GET-OFFER-AMT (SLOT)
    <COND (<==? .SLOT 1> ,SHRINE-OFFER1-AMT)
          (ELSE ,SHRINE-OFFER2-AMT)>>

<ROUTINE TAKE-SHRINE-OFFER (SLOT "AUX" KIND ID LVL ENCH AMT O)
    <SET KIND <SHRINE-GET-OFFER-KIND .SLOT>>
    <SET ID <SHRINE-GET-OFFER-ID .SLOT>>
    <SET LVL <SHRINE-GET-OFFER-LVL .SLOT>>
    <SET ENCH <SHRINE-GET-OFFER-ENCH .SLOT>>
    <SET AMT <SHRINE-GET-OFFER-AMT .SLOT>>

    <COND (<==? .KIND ,ITEMKIND-GOLD>
           <SETG PLAYER-GOLD <+ ,PLAYER-GOLD .AMT>>
           <LOG "You take " N .AMT " gold pieces from the shrine." CR>
           <RTRUE>)>

    <SET O 0>
    <COND (<==? .KIND ,ITEMKIND-WEAPON>
           <SET O <INV-ADD-WEAPON .ID .LVL .ENCH>>)
          (<==? .KIND ,ITEMKIND-POTION>
           <SET O <INV-ADD ,ITEMKIND-POTION .ID>>)
          (<==? .KIND ,ITEMKIND-FOOD>
           <SET O <INV-ADD ,ITEMKIND-FOOD .ID>>)>

    <COND (<NOT .O>
           <SETG STATS-PACKFULL-PICKUP-BLOCKED
               <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
           <LOG "Your pack is too full to take ">
           <SHRINE-PRINT-OFFER-TEXT .SLOT T>
           <LOG " from the shrine." CR>
           <RFALSE>)>

    <LOG "You take ">
    <SHRINE-PRINT-OFFER-TEXT .SLOT T>
    <LOG " from the shrine." CR>
    <COND (<AND <==? .KIND ,ITEMKIND-WEAPON>
                <NOT ,EQUIPPED-WEAPON>
                <G=? <+ ,PLAYER-STR .ENCH> .LVL>>
           <SETG EQUIPPED-WEAPON .O>
           <LOG "You wield it." CR>)>
    <RTRUE>>

<ROUTINE TRY-ACTIVATE-SHRINE ("AUX" O C PICK)
    <SET O <SHRINE-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .O 0> <RFALSE>)>
    <COND (<FSET? .O ,OPENBIT> <RFALSE>)>

    <GENERATE-SHRINE-OFFERS ,CURRENT-FLOOR <GETP .O ,P?R-X> <GETP .O ,P?R-Y>>
    <SET C <POPUP-SHRINE-GETCHAR>>
    <SET PICK 0>
    <COND (<==? .C !\1> <SET PICK 1>)
          (<==? .C !\2> <SET PICK 2>)>
    <COND (<L? .PICK 1> <RFALSE>)>

    <COND (<TAKE-SHRINE-OFFER .PICK>
           <FSET .O ,OPENBIT>
           <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <RTRUE>)>
    <RFALSE>>

;"Find a poison potion object at (X,Y), or 0 if none or not poison."

<ROUTINE POISON-POTION-OBJ-AT (X Y "AUX" O COLOR TYPE)
    <SET O <POTION-OBJ-AT .X .Y>>
    <COND (<L=? .O 0> <RETURN 0>)>
    <SET COLOR <GETP .O ,P?R-ITID>>
    <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
    <COND (<==? .TYPE ,POTION-POISON> .O) (ELSE 0)>>

;"Search for a poison potion within Manhattan distance R of (X,Y).

Returns a potion object, or 0 if none."

<ROUTINE POISON-POTION-OBJ-IN-RANGE (X Y R "AUX" O K COLOR TYPE PX PY DIST)
    <SET O <FIRST? <FLOOR-OBJ ,CURRENT-FLOOR>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-POTION>
               <SET COLOR <GETP .O ,P?R-ITID>>
               <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
               <COND (<==? .TYPE ,POTION-POISON>
                      <SET PX <GETP .O ,P?R-X>>
                      <SET PY <GETP .O ,P?R-Y>>
                      <SET DIST <+ <ABS <- .PX .X>> <ABS <- .PY .Y>>>>
                      <COND (<L=? .DIST .R> <RETURN .O>)>)>)>
        <SET O <NEXT? .O>>>>

<ROUTINE REMOVE-POTION-OBJ (O "AUX" X Y)
    <COND (<NOT .O> <RTRUE>)>
    <SET X <GETP .O ,P?R-X>>
    <SET Y <GETP .O ,P?R-Y>>
    <REMOVE .O>
    <FREE-RASCAL-ITEM .O>
    <COND (<AND <G? .X 0> <G? .Y 0>> <MARK-DIRTY .X .Y>)>
    <RTRUE>>

;"Checks whether the trader NPC is at (X, Y) on the current floor.

Args:
  X, Y: Map coordinates.

Returns:
  T if present; FALSE otherwise."

<ROUTINE TRADER-AT? (X Y)
    <AND ,TRADER-ON?
         <G? ,TRADER-X 0>
         <G? ,TRADER-Y 0>
         <==? ,TRADER-X .X>
         <==? ,TRADER-Y .Y>>>

;"Checks whether a grounded treasure occupies (X, Y) on the current floor.

Args:
  X, Y: Map coordinates.

Returns:
  Treasure ID (1..TREASURE-COUNT), or 0 if none."

<ROUTINE TREASURE-AT? (X Y "AUX" T)
    <COND (<G? <SET T <TREASURE-OBJ-AT .X .Y>> 0> <GETP .T ,P?R-ITID>)
          (ELSE 0)>>

;"Enemies are stored directly as child enemy objects under FLOOR-OBJ."

;"Forces a floor's enemies to regenerate on next entry."

<ROUTINE CLEAR-FLOOR-ENEMIES (F)
    <PUTB ,FLOOR-ENEMY-INIT <- .F 1> 0>
    <FREE-RASCAL-ENEMY-CHILDREN <FLOOR-OBJ .F>>
    <RTRUE>>

;"Loads/spawns the floor's enemies.

Args:
    F: Floor number (1-based).

Returns:
    T."

<ROUTINE SPAWN-ENEMY-OBJ-OF-TYPE (F TYPE "AUX" X Y HP O)
    <COND (<NOT <FIND-RANDOM-ENEMY-SPAWN-POINT>> <RFALSE>)>
    <SET X ,ENTRY-X>
    <SET Y ,ENTRY-Y>
    <SET HP <ENEMY-START-HP .TYPE .F>>
    <COND (<NOT <SET O <ALLOC-RASCAL-ENEMY>>> <RFALSE>)>
    <PUTP .O ,P?R-ETYPE .TYPE>
    <PUTP .O ,P?R-EHP .HP>
    <PUTP .O ,P?R-X .X>
    <PUTP .O ,P?R-Y .Y>
    <MOVE .O ,CURRENT-FLOOR-OBJ>
    <RTRUE>>

<ROUTINE APPLY-PENDING-SPIRIT-SPAWNS (F "AUX" PENDING)
    <COND (<G=? .F ,MAX-FLOORS> <RTRUE>)>
    <SET PENDING <GETB ,PENDING-SPIRIT-SPAWNS <- .F 1>>>
    <REPEAT ()
        <COND (<L=? .PENDING 0> <RETURN T>)>
        <COND (<SPAWN-ENEMY-OBJ-OF-TYPE .F ,ETYPE-SPIRIT>
               <SET PENDING <- .PENDING 1>>
               <PUTB ,PENDING-SPIRIT-SPAWNS <- .F 1> .PENDING>
               <AGAIN>)>
        <RETURN T>>>

<ROUTINE LOAD-FLOOR-ENEMIES (F)
    <SETG CURRENT-FLOOR-OBJ <FLOOR-OBJ .F>>
    <COND (<G? <GETB ,FLOOR-ENEMY-INIT <- .F 1>> 0> <RTRUE>)>
    <SPAWN-ENEMIES .F>
    <PUTB ,FLOOR-ENEMY-INIT <- .F 1> 1>
    <RTRUE>>

;"Loads or chooses the per-floor trader location for floor F.

This is called after dungeon generation for the floor, so it can pick a room
center and then FORCE-PASSABLE that tile.

Args:
  F: Floor number (1-based).

Returns:
  T."
<ROUTINE LOAD-FLOOR-TRADER (F "AUX" X Y R TRIES)
    <SETG TRADER-ON? <>>
    <SETG TRADER-X 0>
    <SETG TRADER-Y 0>
    <COND (<NOT <TRADER-ON-FLOOR? .F>>
           <PUTP <TRADER-OBJ .F> ,P?R-X 0>
           <PUTP <TRADER-OBJ .F> ,P?R-Y 0>
           <RTRUE>)>
    <SET X <GETP <TRADER-OBJ .F> ,P?R-X>>
    <SET Y <GETP <TRADER-OBJ .F> ,P?R-Y>>
    <COND (<AND <G? .X 0> <G? .Y 0>>
           <SETG TRADER-ON? T>
           <SETG TRADER-X .X>
           <SETG TRADER-Y .Y>
           <PUTP <TRADER-OBJ .F> ,P?R-X .X>
           <PUTP <TRADER-OBJ .F> ,P?R-Y .Y>
           <FORCE-PASSABLE .X .Y>
           <RTRUE>)>
    ;"Pick a carved tile in a room for the trader and persist it."
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 120> <RTRUE>)>
        <SET R <RNG ,ROOM-COUNT>>
        <COND (<NOT <RANDOM-POINT-IN-ROOM .R>> <AGAIN>)>
        <SET X ,ENTRY-X>
        <SET Y ,ENTRY-Y>
        <COND (<OR <L=? .X 1>
                   <L=? .Y 1>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>
                   <G? <GOLD-OBJ-AT .X .Y> 0>
                   <G? <FOOD-OBJ-AT .X .Y> 0>
                   <G? <WEAPON-OBJ-AT .X .Y> 0>
                   <POTION-AT? .X .Y>
                   <G? <TREASURE-AT? .X .Y> 0>
                   <G? <ENEMY-AT .X .Y> 0>>
               <AGAIN>)>
        <FORCE-PASSABLE .X .Y>
        <SETG TRADER-ON? T>
        <SETG TRADER-X .X>
        <SETG TRADER-Y .Y>
        <PUTP <TRADER-OBJ .F> ,P?R-X .X>
        <PUTP <TRADER-OBJ .F> ,P?R-Y .Y>
        <RETURN>>
    <RTRUE>>

;"Spawns treasures that belong on floor F and re-hardens grounded treasures.

If a treasure's assigned TREASURE-SPAWN-FLOOR equals F and no live treasure
object with that ID exists anywhere, this chooses a carved tile in a room and
places it.

Args:
  F: Floor number (1-based).

Returns:
  T."
<ROUTINE LOAD-FLOOR-TREASURES (F "AUX" SF X Y R TRIES O NXT)
    <DO (ID 1 ,TREASURE-COUNT)
        <SET SF <GETB ,TREASURE-SPAWN-FLOOR <- .ID 1>>>
        <COND (<AND <==? .SF .F> <NOT <TREASURE-EXISTS? .ID>>>
               ;"Spawn it now (exactly once) on this floor."
               <SET TRIES 0>
               <REPEAT ()
                   <SET TRIES <+ .TRIES 1>>
                   <COND (<G? .TRIES 160> <RETURN>)>
                   <SET R <RNG ,ROOM-COUNT>>
                   <COND (<NOT <RANDOM-POINT-IN-ROOM .R>> <AGAIN>)>
                   <SET X ,ENTRY-X>
                   <SET Y ,ENTRY-Y>
                   <COND (<OR <NOT <FLOOR? .X .Y>>
                              <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                              <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>
                              <G? <ITEM-OBJ-AT .X .Y 0> 0>
                              <TRADER-AT? .X .Y>
                              <AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>
                              <G? <ENEMY-AT .X .Y> 0>>
                          <AGAIN>)>
                   <SET O <ALLOC-RASCAL-ITEM>>
                   <COND (<NOT .O> <RETURN>)>
                   <PUTP .O ,P?R-ITKIND ,ITEMKIND-TREASURE>
                   <PUTP .O ,P?R-ITID .ID>
                   <PUTP .O ,P?R-X .X>
                   <PUTP .O ,P?R-Y .Y>
                   <MOVE .O <FLOOR-OBJ .F>>
                   <FORCE-PASSABLE .X .Y>
                   <RETURN>>)>>

    ;"Ensure grounded treasures on this floor remain reachable."
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET NXT <NEXT? .O>>
        <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-TREASURE>
                    <G? <GETP .O ,P?R-X> 0>
                    <G? <GETP .O ,P?R-Y> 0>>
               <FORCE-PASSABLE <GETP .O ,P?R-X> <GETP .O ,P?R-Y>>)>
        <SET O .NXT>>

    <RTRUE>>

;"True if a treasure with ID exists anywhere in the live object graph (floor,
  inventory, or any trader)."

<ROUTINE TREASURE-EXISTS? (ID)
    <MAP-CONTENTS (O ,PLAYER-INVENTORY)
        <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-TREASURE>
                    <==? <GETP .O ,P?R-ITID> .ID>>
               <RTRUE>)>>

    <DO (F 1 ,MAX-FLOORS)
        <MAP-CONTENTS (O <TRADER-OBJ .F>)
            <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-TREASURE>
                        <==? <GETP .O ,P?R-ITID> .ID>>
                   <RTRUE>)>>>

    <DO (F 1 ,MAX-FLOORS)
        <MAP-CONTENTS (O <FLOOR-OBJ .F>)
            <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-TREASURE>
                        <==? <GETP .O ,P?R-ITID> .ID>>
                   <RTRUE>)>>>

    <RFALSE>>

;"Initializes the display, seeds the RNG, sets initial player state, and
  enters floor 1.

Returns:
  T."
<ROUTINE INIT ()
    ;"Split the screen so the fixed grid UI stays in the upper window,
      while messages print (and scroll) in the lower window."
    <SPLIT ,UPPER-HEIGHT>
    <SCREEN 1>
    <UI-LOG-COLOR>
    <CLEAR -2>
    ;"Seeded RNG: pick (or accept) a 32-bit seed, then seed the PRNG."
    <COND (<AND <==? ,SEED-HI 0> <==? ,SEED-LO 0>>
           <INIT-RNG-RANDOM>
           <SETG SEED-HI ,RNG-STATE-HI>
           <SETG SEED-LO ,RNG-STATE-LO>)
          (ELSE <SEED-RNG-32 ,SEED-HI ,SEED-LO>)>
    <MAKE-FLOOR-SEEDS>
    <DO (F 1 ,MAX-FLOORS)
        <FCLEAR <FLOOR-OBJ .F> ,TOUCHBIT>
        <FLOOR-SET-UP-STAIRS .F 0 0>
        <FLOOR-SET-DOWN-STAIRS .F 0 0>
        <PUTB ,TREASURE-ROOM-CREATED <- .F 1> 0>
        <PUTB ,PENDING-SPIRIT-SPAWNS <- .F 1> 0>
        <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-KEY>
        <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-LOCKEDDOOR>
        <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-SHRINE>
        <FREE-RASCAL-ITEM-CHILDREN <FLOOR-OBJ .F> ,ITEMKIND-COFFER>
        <PUTB ,TREASURE-ROOM-DOOR-X <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-DOOR-Y <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOCKTYPE <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-X <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-Y <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-KIND <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-ID <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-LVL <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-LOOT-ENCH <- .F 1> 0>
        <PUT ,TREASURE-ROOM-LOOT-AMT <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-BONUS-GOLD-X <- .F 1> 0>
        <PUTB ,TREASURE-ROOM-BONUS-GOLD-Y <- .F 1> 0>
        <PUT ,TREASURE-ROOM-BONUS-GOLD-AMT <- .F 1> 0>>
    <STATS-RESET>
    <INV-CLEAR>
    <INIT-START-WEAPON>
    <INIT-TREASURES>
    <INIT-POTIONS>
    <INIT-INTERIOR-ENTRANCES>
    <PRECOMPUTE-TREASURE-ROOM-PLANS>
    <SETG GAME-OVER? <>>
    <SETG YOU-WIN? <>>
    <COND (,EXPERT-MODE?
           <SETG IDENTIFY-COST 100>
           <SETG CARROT-COST 50>
           <SETG ENCHANT-COST 200>)
          (ELSE
           <SETG IDENTIFY-COST 50>
           <SETG CARROT-COST 5>
           <SETG ENCHANT-COST 100>)>
    <SETG CARROTS-SOLD 0>
    <IF-DEBUG
        <SETG DEBUG-DOUBLEKEY 0>
        <SETG DEBUG-USED? <>>
        <SETG DEBUG-OVERLAY-ONCE? <>>
        <SETG IMMORTAL? <>>
        <SETG OMNISCIENT? <>>
        <SETG STAIR-FINDER? <>>>
    <SETG PLAYER-MAX-HP ,INITIAL-PLAYER-MAX-HP>
    <SETG PLAYER-HP ,PLAYER-MAX-HP>
    <SETG PLAYER-STR <STARTING-PLAYER-STR>>
    <SETG PLAYER-DEF ,INITIAL-PLAYER-DEF>
    <SETG PLAYER-INVIS-TURNS 0>
    <SETG PLAYER-VISION-TURNS 0>
    <SETG PLAYER-TORPOR-TURNS 0>
    <SETG PLAYER-HUSTLE-TURNS 0>
    <SETG TRADER-ON? <>>
    <SETG TRADER-X 0>
    <SETG TRADER-Y 0>
    <SETG CURRENT-FLOOR 1>
    <ENTER-FLOOR 1 0 0 0>
    <SETG ORIG-SPAWN-X ,PLAYER-X>
    <SETG ORIG-SPAWN-Y ,PLAYER-Y>
    <COND (,EXPERT-MODE?
           <LOG "I hope you know what you're doing, rascal. Good luck." CR>)
          (ELSE
           <LOG "Welcome, rascal! Get gold. Don't die. Kill beasts." CR>)>
    <DRAW>
    <RTRUE>>
