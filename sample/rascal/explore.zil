"Exploration state (map reveal, room discovery)"

<GLOBAL BITMASKS <PTABLE (BYTE) 1 2 4 8 16 32 64 128>>

<CONSTANT REVEAL-BYTES </ <+ ,MAP-SIZE 7> 8>>

<GLOBAL FLOOR-REVEALED <ITABLE <* ,MAX-FLOORS ,REVEAL-BYTES> (BYTE) 0>>

;"Per-floor room discovery tracking.

FLOOR-ROOM-DISCOVERED stores 0/1 flags for each (floor, room id) pair.
DISCOVERED-ROOMS is the cached count for the current floor."

<GLOBAL FLOOR-ROOM-DISCOVERED <ITABLE <* ,MAX-FLOORS ,MAX-ROOMS> (BYTE) 0>>

<GLOBAL DISCOVERED-ROOMS 0>

;"Stair traversal helper: stash a tamed monkey object reference while ENTER-FLOOR
  rebuilds CURRENT-FLOOR-OBJ. 0 means none."

<GLOBAL PENDING-STAIR-MONKEY 0>

;"Computes the linear slot offset into the per-floor room discovery table.

Args:
  F: Floor number (1-based).
  RID: Room ID (1..MAX-ROOMS).

Returns:
  Byte offset into FLOOR-ROOM-DISCOVERED."

<ROUTINE ROOMDISC-IDX (F RID)
    <+ <* <- .F 1> ,MAX-ROOMS> <- .RID 1>>>

;"Clears the discovered-room flags for a given floor.

Args:
  F: Floor number (1-based).

Returns:
  T."

<ROUTINE CLEAR-FLOOR-ROOMDISC (F)
    <DO (RID 1 ,MAX-ROOMS)
        <PUTB ,FLOOR-ROOM-DISCOVERED <ROOMDISC-IDX .F .RID> 0>>
    <RTRUE>>

;"Counts discovered rooms for the current floor based on FLOOR-ROOM-DISCOVERED.

Args:
  F: Floor number (1-based).

Returns:
  Count of discovered rooms (0..ROOM-COUNT)."

<ROUTINE COUNT-FLOOR-ROOMDISC (F "AUX" C)
    <SET C 0>
    <DO (RID 1 ,ROOM-COUNT)
        <COND (<G? <GETB ,FLOOR-ROOM-DISCOVERED <ROOMDISC-IDX .F .RID>> 0>
               <SET C <+ .C 1>>)>>
    .C>

;"Clears the reveal bitset for a given floor.

Args:
  F: Floor number (1-based).

Returns:
  T."

<ROUTINE CLEAR-FLOOR-REVEAL (F "AUX" BASE)
    <SET BASE <FLOOR-BASE .F>>
    <DO (I 1 ,REVEAL-BYTES) <PUTB ,FLOOR-REVEALED <+ .BASE <- .I 1>> 0>>>

;"Attempts to descend stairs to the next floor (>) if standing on down stairs.

Args:
  (none)

Returns:
  T if the move happens; FALSE if blocked (wrong tile or at bottom)."

<ROUTINE GO-DOWN ("AUX" MONKEY)
    <COND (<G=? ,CURRENT-FLOOR ,MAX-FLOORS> <RFALSE>)>
    <COND (<N==? <TILE-AT ,PLAYER-X ,PLAYER-Y> ,TILE-STAIR-DOWN> <RFALSE>)>
    <SET MONKEY <ENEMY-OBJ-OF-TYPE ,CURRENT-FLOOR-OBJ ,ETYPE-MONKEY>>
    <COND (<AND <G? .MONKEY 0> <FSET? .MONKEY ,TAMEBIT>>
           <SETG PENDING-STAIR-MONKEY .MONKEY>)
          (ELSE <SETG PENDING-STAIR-MONKEY 0>)>
    <SETG STATS-STAIRS-DOWN <+ ,STATS-STAIRS-DOWN 1>>
    <LOG "You descend to floor " N <+ ,CURRENT-FLOOR 1> "." CR>
    <ENTER-FLOOR <+ ,CURRENT-FLOOR 1> ,PLAYER-X ,PLAYER-Y T>
    <COND (<G? ,PENDING-STAIR-MONKEY 0>
           <MOVE ,PENDING-STAIR-MONKEY ,CURRENT-FLOOR-OBJ>
           <DO (D 1 8)
               <COND (<TRY-ENEMY-MOVE ,PENDING-STAIR-MONKEY
                                      <+ ,PLAYER-X <DIR8-DX .D>>
                                      <+ ,PLAYER-Y <DIR8-DY .D>>>
                      <RETURN>)>>
           <SETG PENDING-STAIR-MONKEY 0>)>
    <RTRUE>>

;"Attempts to ascend stairs to the previous floor (<) if standing on up stairs.

Args:
  (none)

Returns:
  T if the move happens; FALSE if blocked (wrong tile or at top)."

<ROUTINE GO-UP ("AUX" MONKEY)
    <COND (<N==? <TILE-AT ,PLAYER-X ,PLAYER-Y> ,TILE-STAIR-UP> <RFALSE>)>

    <SET MONKEY <ENEMY-OBJ-OF-TYPE ,CURRENT-FLOOR-OBJ ,ETYPE-MONKEY>>
    <COND (<AND <G? .MONKEY 0> <FSET? .MONKEY ,TAMEBIT>>
           <SETG PENDING-STAIR-MONKEY .MONKEY>)
          (ELSE <SETG PENDING-STAIR-MONKEY 0>)>

    ;"With the trophy, ascending from floor 1 is a win condition."
    <COND (<AND <==? ,CURRENT-FLOOR 1> <PLAYER-HAS-TROPHY?>>
           <SETG YOU-WIN? T>
           <SETG GAME-OVER? T>
           <RTRUE>)>

    <COND (<L=? ,CURRENT-FLOOR 1>
           <LOG "An unseen force stops you from ascending." CR>
           <RFALSE>)>
    <SETG STATS-STAIRS-UP <+ ,STATS-STAIRS-UP 1>>
    <LOG "You ascend to floor " N <- ,CURRENT-FLOOR 1> "." CR>

    ;"Carrying the trophy repopulates monsters on each upward move."
    <COND (<PLAYER-HAS-TROPHY?> <CLEAR-FLOOR-ENEMIES <- ,CURRENT-FLOOR 1>>)>
    <ENTER-FLOOR <- ,CURRENT-FLOOR 1> ,PLAYER-X ,PLAYER-Y <>>
    <COND (<G? ,PENDING-STAIR-MONKEY 0>
           <MOVE ,PENDING-STAIR-MONKEY ,CURRENT-FLOOR-OBJ>
           <DO (D 1 8)
               <COND (<TRY-ENEMY-MOVE ,PENDING-STAIR-MONKEY
                                      <+ ,PLAYER-X <DIR8-DX .D>>
                                      <+ ,PLAYER-Y <DIR8-DY .D>>>
                      <RETURN>)>>
           <SETG PENDING-STAIR-MONKEY 0>)>
    <RTRUE>>

;"Computes the byte offset of the reveal bitset for a given floor.

Args:
  F: Floor number (1-based).

Returns:
  Base offset into FLOOR-REVEALED for that floor."

<ROUTINE FLOOR-BASE (F)
    <* <- .F 1> ,REVEAL-BYTES>>

;"Checks whether (X, Y) has been revealed on the current floor.
	Reveal state is stored as a bitset per floor in FLOOR-REVEALED.

Args:
	X, Y: Map coordinates.

Returns:
	T if revealed; FALSE otherwise."

<ROUTINE REVEALED? (X Y "AUX" IDX Q BIT BASE BYTE MASK)
    <SET IDX <MAP-INDEX .X .Y>>
    <SET Q </ .IDX 8>>
    <SET BIT <- .IDX <* .Q 8>>>
    <SET BASE <FLOOR-BASE ,CURRENT-FLOOR>>
    <SET BYTE <GETB ,FLOOR-REVEALED <+ .BASE .Q>>>
    <SET MASK <GETB ,BITMASKS .BIT>>
    <G? <BAND .BYTE .MASK> 0>>

;"Marks a linear map index as revealed for the current floor.

Args:
  IDX: Linear index from MAP-INDEX.

Returns:
  T."

<ROUTINE REVEAL-IDX (IDX "AUX" Q BIT BASE AT MASK OLD)
    <SET Q </ .IDX 8>>
    <SET BIT <- .IDX <* .Q 8>>>
    <SET BASE <FLOOR-BASE ,CURRENT-FLOOR>>
    <SET AT <+ .BASE .Q>>
    <SET MASK <GETB ,BITMASKS .BIT>>
    <SET OLD <GETB ,FLOOR-REVEALED .AT>>
    <PUTB ,FLOOR-REVEALED .AT <BOR .OLD .MASK>>
    <RTRUE>>

;"Reveals a single tile (if in bounds).

Args:
  X, Y: Map coordinates.

Returns:
  T if in bounds; FALSE otherwise."

<ROUTINE REVEAL-TILE (X Y)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <REVEAL-IDX <MAP-INDEX .X .Y>>
    <RTRUE>>

;"Reveals a 3x3 square centered on (X, Y).

Args:
  X, Y: Map coordinates.

Returns:
  (none)"

<ROUTINE REVEAL-AROUND (X Y)
    <DO (DY -1 1) <DO (DX -1 1) <REVEAL-TILE <+ .X .DX> <+ .Y .DY>>>>>

;"Reveals any adjacent stair tiles around (X, Y).

This is used when revealing an entire room: stairs can be forced passable and
have ROOMIDS=0, so they might not be revealed by REVEAL-ROOM alone.

Args:
  X, Y: Map coordinates.

Returns:
  T."

<ROUTINE REVEAL-STAIRS-NEAR (X Y "AUX" NX NY)
    <DO (DY -1 1)
        <DO (DX -1 1)
            <SET NX <+ .X .DX>>
            <SET NY <+ .Y .DY>>
            <COND (<AND <IN-BOUNDS? .NX .NY>
                        <OR <==? <TILE-AT .NX .NY> ,TILE-STAIR-UP>
                            <==? <TILE-AT .NX .NY> ,TILE-STAIR-DOWN>>>
                   <REVEAL-TILE .NX .NY>)>>>
    <RTRUE>>

;"Reveals all tiles belonging to a given room ID.

Args:
  RID: Room ID.

Returns:
  (none)"

<ROUTINE REVEAL-ROOM (RID "AUX" X Y IDX)
    ;"Track discovered rooms per floor so the UI can show discovered/total."
    <COND (<AND <G? .RID 0>
                <L=? .RID ,MAX-ROOMS>
                <L=? ,CURRENT-FLOOR ,MAX-FLOORS>
                <L=? <GETB ,FLOOR-ROOM-DISCOVERED
                           <ROOMDISC-IDX ,CURRENT-FLOOR .RID>>
                     0>>
           <PUTB ,FLOOR-ROOM-DISCOVERED <ROOMDISC-IDX ,CURRENT-FLOOR .RID> 1>
           <SETG DISCOVERED-ROOMS <+ ,DISCOVERED-ROOMS 1>>
           <SETG STATS-ROOMS-DISCOVERED-TOTAL
               <+ ,STATS-ROOMS-DISCOVERED-TOTAL 1>>)>
    <SET Y 1>
    <REPEAT ()
        <COND (<G? .Y ,MAP-H> <RETURN>)>
        <SET X 1>
        <REPEAT ()
            <COND (<G? .X ,MAP-W> <SET Y <+ .Y 1>> <RETURN>)>
            <SET IDX <MAP-INDEX .X .Y>>
            <COND (<==? <GETB ,ROOMIDS .IDX> .RID>
                   <REVEAL-IDX .IDX>
                   ;"If the room contains stairs whose ROOMIDS=0 (e.g. forced
			         passable), they should still become visible when the room is
			         revealed. Reveal any adjacent stair tiles."
                   <REVEAL-STAIRS-NEAR .X .Y>)>
            <SET X <+ .X 1>>>>>

"Player movement, stairs, and turn input"

;"Teleports the player to a random safe passable tile on the current floor.

The destination is restricted to ordinary passable tiles (floor/corridor/door)
and avoids stairs, the trader, and enemy-occupied tiles.

Args:
  (none)

Returns:
  T."
<ROUTINE TELEPORT-PLAYER ("AUX" TRIES X Y)
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES ,TELEPORT-TRY-LIMIT>
               <LOG "Nothing happens." CR>
               <RETURN T>)>
        <SET X <+ 1 <RNG ,MAP-W>>>
        <SET Y <+ 1 <RNG ,MAP-H>>>
        <COND (<NOT <IN-BOUNDS? .X .Y>> <AGAIN>)>
        <COND (<NOT <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
                        <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
                        <==? <TILE-AT .X .Y> ,TILE-DOOR>>>
               <AGAIN>)>
        <COND (<OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                   <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>
               <AGAIN>)>
        <COND (<TRADER-AT? .X .Y> <AGAIN>)>
        <COND (<G? <ENEMY-AT .X .Y> 0> <AGAIN>)>
        <SETG PLAYER-X .X>
        <SETG PLAYER-Y .Y>
        <AFTER-PLAYER-RELOCATE>
        <RETURN T>>>

<IF-DEBUG
    ;"Debug helper: teleport the player to the up/down stairs on the current floor.

    Args:
    DOWN?: True to target down stairs (>); false to target up stairs (<).

    Returns:
    T."
    <ROUTINE STAIR-FINDER-TELEPORT (DOWN? "AUX" X Y TILE RID)
        <SETG DEBUG-USED? T>
        <COND (.DOWN?
        <SET X <FLOOR-DOWN-X ,CURRENT-FLOOR>>
        <SET Y <FLOOR-DOWN-Y ,CURRENT-FLOOR>>
            <SET TILE ,TILE-STAIR-DOWN>)
            (ELSE
        <SET X <FLOOR-UP-X ,CURRENT-FLOOR>>
        <SET Y <FLOOR-UP-Y ,CURRENT-FLOOR>>
            <SET TILE ,TILE-STAIR-UP>)>
        <COND (<OR <==? .X 0> <==? .Y 0>>
            <LOG "Stair finder: stairs not recorded for this floor." CR>
            <RTRUE>)>
        <COND (<NOT <IN-BOUNDS? .X .Y>>
            <LOG "Stair finder: stair coords invalid: (" N .X "," N .Y ")." CR>
            <RTRUE>)>
        <LOG "Stair finder: target (" N .X "," N .Y "), tile=" N <TILE-AT .X .Y>>
        <SET RID <ROOMID-AT .X .Y>>
        <LOG ", roomid=" N .RID ", ROOM-COUNT=" N ,ROOM-COUNT CR>
        <COND (<AND <G? .RID 0> <L=? .RID ,ROOM-COUNT>>
            <LOG "  Room " N .RID " bounds: L=" N <ROOM-GET .RID ,ROOM-L>
                    " T=" N <ROOM-GET .RID ,ROOM-T>
                    " R=" N <ROOM-GET .RID ,ROOM-R>
                    " B=" N <ROOM-GET .RID ,ROOM-B> CR>)>
        <LOG "  Neighbors: N=" N <TILE-AT .X <- .Y 1>>
            " S=" N <TILE-AT .X <+ .Y 1>>
            " W=" N <TILE-AT <- .X 1> .Y>
            " E=" N <TILE-AT <+ .X 1> .Y> CR>
        <COND (<N==? <TILE-AT .X .Y> .TILE>
            <LOG "Stair finder: WARNING: expected stair tile at (" N .X "," N .Y ") but map has tile " N <TILE-AT .X .Y> "." CR>)>
        <SETG PLAYER-X .X>
        <SETG PLAYER-Y .Y>
        <AFTER-PLAYER-RELOCATE>
        <LOG "Stair finder: teleported to (" N .X "," N .Y ")." CR>
        <RTRUE>>>

;"Common post-move side effects after PLAYER-X/PLAYER-Y changes.

Reveals, picks up any items on the tile, and updates room discovery."

<ROUTINE AFTER-PLAYER-RELOCATE ("AUX" ID)
    <SET ID <POTION-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<AND <G? .ID 0> <NOT <FSET? .ID ,SEENBIT>>>
           <FSET .ID ,SEENBIT>
           <SETG STATS-POTIONS-FOUND <+ ,STATS-POTIONS-FOUND 1>>)> 
    <REVEAL-AROUND ,PLAYER-X ,PLAYER-Y>
    <TRY-PICKUP-KEY>
    <TRY-PICKUP-POTION>
    <TRY-PICKUP-WEAPON>
    <TRY-PICKUP-TREASURE>
    <TRY-PICKUP-FOOD>
    <TRY-PICKUP-GOLD>
    <COND (<AND <G? <ROOMID-AT ,PLAYER-X ,PLAYER-Y> 0>
                <N==? <ROOMID-AT ,PLAYER-X ,PLAYER-Y> ,CURRENT-ROOM>>
           <SETG CURRENT-ROOM <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
           <COND (<L=? ,PLAYER-SHADOW-TURNS 0> <REVEAL-ROOM ,CURRENT-ROOM>)>)>
    <SETG INTERIOR-LAUNCHED? <>>
    <COND (<==? <TILE-AT ,PLAYER-X ,PLAYER-Y> ,TILE-INTERIOR>
           <SET ID <INTERIOR-ID-AT ,CURRENT-FLOOR ,PLAYER-X ,PLAYER-Y>>
           <COND (<G? .ID 0>
                  <SETG INTERIOR-LAUNCHED? <LAUNCH-INTERIOR-ID .ID>>)>)>
    <RTRUE>>
