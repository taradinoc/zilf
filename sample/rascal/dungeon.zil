"Dungeon generation"

<CONSTANT MAP-W 60>
<CONSTANT MAP-H 20>

<CONSTANT MAX-FLOORS 25>

<CONSTANT MAP-SIZE <* ,MAP-W ,MAP-H>>

<CONSTANT MAX-ROOMS 12>
<CONSTANT ROOM-STRIDE 7>
<CONSTANT ROOM-L 0>
<CONSTANT ROOM-T 1>
<CONSTANT ROOM-R 2>
<CONSTANT ROOM-B 3>
<CONSTANT ROOM-CX 4>
<CONSTANT ROOM-CY 5>
<CONSTANT ROOM-SHAPE 6>

<CONSTANT ROOMSHAPE-RECT 1>
<CONSTANT ROOMSHAPE-CIRCLE 2>
<CONSTANT ROOMSHAPE-DIAMOND 3>
<CONSTANT ROOMSHAPE-L-NW 4>
<CONSTANT ROOMSHAPE-L-NE 5>
<CONSTANT ROOMSHAPE-L-SW 6>
<CONSTANT ROOMSHAPE-L-SE 7>
<CONSTANT ROOMSHAPE-U-UP 8>
<CONSTANT ROOMSHAPE-U-DOWN 9>
<CONSTANT ROOMSHAPE-U-LEFT 10>
<CONSTANT ROOMSHAPE-U-RIGHT 11>

<CONSTANT MAP <ITABLE <* ,MAP-W ,MAP-H> (BYTE) 0>>
<CONSTANT ROOMIDS <ITABLE <* ,MAP-W ,MAP-H> (BYTE) 0>>

<CONSTANT ROOMS-TABLE <ITABLE <* ,MAX-ROOMS ,ROOM-STRIDE> (WORD) 0>>

<CONSTANT CONNECTED <ITABLE ,MAX-ROOMS (BYTE) 0>>

<CONSTANT CONNECTIONS <ITABLE <* ,MAX-ROOMS ,MAX-ROOMS> (BYTE) 0>>

<GLOBAL FORCED-ENTRY? <>>
<GLOBAL ENTRY-X 0>
<GLOBAL ENTRY-Y 0>

;"Original (floor 1) spawn coordinate for the run."

<GLOBAL ORIG-SPAWN-X 0>
<GLOBAL ORIG-SPAWN-Y 0>

;"Builds the current floor's dungeon into MAP/ROOMIDS/ROOMS.

Args:
  (none)

Returns:
  T."

<ROUTINE BUILD-DUNGEON ()
    <CLEAR-MAP>
    <COND (,FORCED-ENTRY? <PLACE-ENTRY-ROOM>)>
    <PLACE-ROOMS>
    <CONNECT-ROOMS>>

;"Resets MAP and ROOMIDS to all-walls, and resets ROOM-COUNT.

Args:
  (none)

Returns:
  (none)"

<ROUTINE CLEAR-MAP ("AUX" X Y IDX)
    <SETG ROOM-COUNT 0>
    <SET Y 1>
    <REPEAT ()
        <COND (<G? .Y ,MAP-H> <RETURN>)>
        <SET X 1>
        <REPEAT ()
            <COND (<G? .X ,MAP-W> <SET Y <+ .Y 1>> <RETURN>)>
            <SET IDX <MAP-INDEX .X .Y>>
            <PUTB ,MAP .IDX ,TILE-WALL>
            <PUTB ,ROOMIDS .IDX 0>
            <SET X <+ .X 1>>>>>

;"Attempts to place up to MAX-ROOMS non-overlapping rooms.

Args:
  (none)

Returns:
  (none)"

<ROUTINE PLACE-ROOMS ("AUX" TRIES)
    <SET TRIES 0>
    <REPEAT ()
        <COND (<G=? ,ROOM-COUNT ,MAX-ROOMS> <RETURN>)>
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 300> <RETURN>)>
        <TRY-ADD-ROOM>>
    <PUTB ,FLOOR-ROOM-COUNT <- ,CURRENT-FLOOR 1> ,ROOM-COUNT>>

;"Attempts to generate and carve a single room.

Args:
  (none)

Returns:
  T if a room was placed; FALSE otherwise."

<ROUTINE PICK-ROOM-SHAPE ("AUX" R)
    <SET R <RNG 100>>
    <COND (<L=? .R 80> ,ROOMSHAPE-RECT)
          (<L=? .R 85> ,ROOMSHAPE-CIRCLE)
          (<L=? .R 90> ,ROOMSHAPE-DIAMOND)
          (ELSE <+ ,ROOMSHAPE-L-NW <- <RNG 4> 1>>)
          ;"U-shaped rooms look weird"
          ;(<L=? .R 95> <+ ,ROOMSHAPE-L-NW <- <RNG 4> 1>>)
          ;(ELSE <+ ,ROOMSHAPE-U-UP <- <RNG 4> 1>>)>>

<ROUTINE TRY-ADD-ROOM ("AUX" W H MAXX MAXY X Y R SHAPE)
    <SET SHAPE <PICK-ROOM-SHAPE>>
    <COND (<==? .SHAPE ,ROOMSHAPE-RECT>
           <SET W <+ 4 <RNG 8>>>
           ;"5..12"
           <SET H <+ 3 <RNG 6>>>)
          (<OR <==? .SHAPE ,ROOMSHAPE-CIRCLE> <==? .SHAPE ,ROOMSHAPE-DIAMOND>>
           ;"Odd sizes: 5,7,9,11"
           <SET W <+ 3 <* <RNG 4> 2>>>
           <SET H .W>)
          (<OR <==? .SHAPE ,ROOMSHAPE-L-NW>
               <==? .SHAPE ,ROOMSHAPE-L-NE>
               <==? .SHAPE ,ROOMSHAPE-L-SW>
               <==? .SHAPE ,ROOMSHAPE-L-SE>>
           <SET W <+ 5 <RNG 6>>>
           ;"6..11"
           <SET H <+ 5 <RNG 5>>>)
          (ELSE
           <SET W <+ 6 <RNG 6>>>
           ;"7..12"
           <SET H <+ 5 <RNG 5>>>)>
    ;"6..10"
    <SET MAXX <- ,MAP-W <+ .W 1>>>
    ;"left <= MAP-W - W - 1"
    <SET MAXY <- ,MAP-H <+ .H 1>>>
    ;"top  <= MAP-H - H - 1"
    <COND (<OR <L? .MAXX 3> <L? .MAXY 3>> <RFALSE>)>
    <SET X <+ 2 <RNG <- .MAXX 2>>>>
    <SET Y <+ 2 <RNG <- .MAXY 2>>>>
    <COND (<NOT <CAN-PLACE-ROOM? .X .Y .W .H>> <RFALSE>)>
    <SET R <+ ,ROOM-COUNT 1>>
    <SETG ROOM-COUNT .R>
    <CARVE-ROOM .X .Y .W .H .R .SHAPE>
    <RTRUE>>

;"Checks whether a room can be placed at the given location (with a 1-tile
  margin) without overlapping existing carved tiles.

Args:
  X, Y: Room top-left.
  W, H: Room size.

Returns:
  T if the room can be placed; FALSE otherwise."

<ROUTINE CAN-PLACE-ROOM? (X Y W H "AUX" LX RX TY BY XX YY)
    <SET LX <- .X 1>>
    <SET TY <- .Y 1>>
    <SET RX <+ .X .W>>
    <SET BY <+ .Y .H>>
    <SET YY .TY>
    <REPEAT ()
        <COND (<G? .YY .BY> <RTRUE>)>
        <SET XX .LX>
        <REPEAT ()
            <COND (<G? .XX .RX> <SET YY <+ .YY 1>> <RETURN>)>
            <COND (<NOT <IN-BOUNDS? .XX .YY>> <RFALSE>)>
            <COND (<N==? <TILE-AT .XX .YY> ,TILE-WALL> <RFALSE>)>
            <SET XX <+ .XX 1>>>>>

;"Bounds check for map coordinates.

Args:
	X, Y: Map coordinates (1-based).

Returns:
	T if inside the map; FALSE otherwise."

<ROUTINE IN-BOUNDS? (X Y)
    <AND <G=? .X 1> <L=? .X ,MAP-W> <G=? .Y 1> <L=? .Y ,MAP-H>>>

;"Returns interior thickness for L/U rooms from a room bounding box.

Args:
	L, T, R, B: Room bounding box.

Returns:
	Integer wall thickness >= 2."

<ROUTINE ROOM-SHAPE-THICKNESS (L T R B "AUX" W H TH)
    <SET W <+ 1 <- .R .L>>>
    <SET H <+ 1 <- .B .T>>>
    <SET TH </ .W 3>>
    <COND (<L? .H .W> <SET TH </ .H 3>>)>
    <SET TH <+ .TH 1>>
    <COND (<L? .TH 2> <SET TH 2>)>
    <COND (<G? .TH 3> <SET TH 3>)>
    .TH>

;"Returns true if (X,Y) should be carved for the room shape in [L..R]x[T..B].

Args:
	SHAPE: ROOMSHAPE-* value.
	X, Y: Candidate map coordinate.
	L, T, R, B: Room bounding box.

Returns:
	T if inside the shape; FALSE otherwise."

<ROUTINE ROOM-SHAPE-FILL? (SHAPE X Y L T R B "AUX" W H RAD CX CY DX DY TH)
    <COND (<OR <L? .X .L> <G? .X .R> <L? .Y .T> <G? .Y .B>> <RFALSE>)>

    <COND (<==? .SHAPE ,ROOMSHAPE-RECT> <RTRUE>)>

    <SET W <+ 1 <- .R .L>>>
    <SET H <+ 1 <- .B .T>>>
    <SET RAD </ <- .W 1> 2>>
    <SET CX <+ .L .RAD>>
    <SET CY <+ .T </ <- .H 1> 2>>>
    <SET DX <ABS <- .X .CX>>>
    <SET DY <ABS <- .Y .CY>>>

    <COND (<==? .SHAPE ,ROOMSHAPE-CIRCLE>
           <COND (<L=? <+ <* .DX .DX> <* .DY .DY>> <* .RAD .RAD>> <RTRUE>)>
           <RFALSE>)>

    <COND (<==? .SHAPE ,ROOMSHAPE-DIAMOND>
           <COND (<L=? <+ .DX .DY> .RAD> <RTRUE>)>
           <RFALSE>)>

    <SET TH <ROOM-SHAPE-THICKNESS .L .T .R .B>>

    <COND (<==? .SHAPE ,ROOMSHAPE-L-NW>
           <COND (<OR <L=? .X <+ .L <- .TH 1>>> <L=? .Y <+ .T <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-L-NE>
           <COND (<OR <G=? .X <- .R <- .TH 1>>> <L=? .Y <+ .T <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-L-SW>
           <COND (<OR <L=? .X <+ .L <- .TH 1>>> <G=? .Y <- .B <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-L-SE>
           <COND (<OR <G=? .X <- .R <- .TH 1>>> <G=? .Y <- .B <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-U-UP>
           <COND (<OR <L=? .X <+ .L <- .TH 1>>>
                      <G=? .X <- .R <- .TH 1>>>
                      <G=? .Y <- .B <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-U-DOWN>
           <COND (<OR <L=? .X <+ .L <- .TH 1>>>
                      <G=? .X <- .R <- .TH 1>>>
                      <L=? .Y <+ .T <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-U-LEFT>
           <COND (<OR <L=? .Y <+ .T <- .TH 1>>>
                      <G=? .Y <- .B <- .TH 1>>>
                      <G=? .X <- .R <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)
          (<==? .SHAPE ,ROOMSHAPE-U-RIGHT>
           <COND (<OR <L=? .Y <+ .T <- .TH 1>>>
                      <G=? .Y <- .B <- .TH 1>>>
                      <L=? .X <+ .L <- .TH 1>>>>
                  <RTRUE>)>
           <RFALSE>)>

    <RFALSE>>

;"Carves a room into MAP/ROOMIDS, and writes its bounds/center/shape into ROOMS.

Args:
	X, Y: Room top-left.
	W, H: Room size.
  RID: Room ID (1-based).
  SHAPE: ROOMSHAPE-* value.

Returns:
	(none)"

<ROUTINE CARVE-ROOM (X Y W H RID SHAPE "AUX" XX YY RX BY CX CY)
    <SET RX <+ .X <- .W 1>>>
    <SET BY <+ .Y <- .H 1>>>
    <SET YY .Y>
    <REPEAT ()
        <COND (<G? .YY .BY> <RETURN>)>
        <SET XX .X>
        <REPEAT ()
            <COND (<G? .XX .RX> <SET YY <+ .YY 1>> <RETURN>)>
            <COND (<ROOM-SHAPE-FILL? .SHAPE .XX .YY .X .Y .RX .BY>
                   <PUTB ,MAP <MAP-INDEX .XX .YY> ,TILE-FLOOR>
                   <PUTB ,ROOMIDS <MAP-INDEX .XX .YY> .RID>)>
            <SET XX <+ .XX 1>>>>

    <SET CX <+ .X </ .W 2>>>
    <SET CY <+ .Y </ .H 2>>>
    <ROOM-SET .RID ,ROOM-L .X>
    <ROOM-SET .RID ,ROOM-T .Y>
    <ROOM-SET .RID ,ROOM-R .RX>
    <ROOM-SET .RID ,ROOM-B .BY>
    <ROOM-SET .RID ,ROOM-CX .CX>
    <ROOM-SET .RID ,ROOM-CY .CY>
    <ROOM-SET .RID ,ROOM-SHAPE .SHAPE>>

;"Picks a random carved coordinate inside room RID.

Returns coordinates via ENTRY-X/ENTRY-Y and T on success; FALSE on failure."

<ROUTINE RANDOM-POINT-IN-ROOM (RID "AUX" TRIES X Y L T R B)
    <SET L <ROOM-GET .RID ,ROOM-L>>
    <SET T <ROOM-GET .RID ,ROOM-T>>
    <SET R <ROOM-GET .RID ,ROOM-R>>
    <SET B <ROOM-GET .RID ,ROOM-B>>
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    <SET TRIES 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 120> <RFALSE>)>
        <SET X <+ .L <- <RNG <+ 1 <- .R .L>>> 1>>>
        <SET Y <+ .T <- <RNG <+ 1 <- .B .T>>> 1>>>
        <COND (<AND <IN-BOUNDS? .X .Y>
                    <==? <ROOMID-AT .X .Y> .RID>>
               <SETG ENTRY-X .X>
               <SETG ENTRY-Y .Y>
               <RTRUE>)>
        <AGAIN>>>

;"Finds the in-room edge X for RID on row Y, searching toward DIR.

Args:
  RID: Room ID.
  Y: Row.
  DIR: +1 for right edge, -1 for left edge.

Returns:
  Edge X inside the room (fallback: room center X)."

<ROUTINE FIND-ROOM-EDGE-X (RID Y DIR "AUX" L R X)
    <SET L <ROOM-GET .RID ,ROOM-L>>
    <SET R <ROOM-GET .RID ,ROOM-R>>
    <COND (<G? .DIR 0>
           <SET X .R>
           <REPEAT ()
               <COND (<L? .X .L> <RETURN <ROOM-GET .RID ,ROOM-CX>>)>
               <COND (<==? <ROOMID-AT .X .Y> .RID> <RETURN .X>)>
               <SET X <- .X 1>>>)
          (ELSE
           <SET X .L>
           <REPEAT ()
               <COND (<G? .X .R> <RETURN <ROOM-GET .RID ,ROOM-CX>>)>
               <COND (<==? <ROOMID-AT .X .Y> .RID> <RETURN .X>)>
               <SET X <+ .X 1>>>)>>

;"Finds the in-room edge Y for RID on column X, searching toward DIR.

Args:
  RID: Room ID.
  X: Column.
  DIR: +1 for bottom edge, -1 for top edge.

Returns:
  Edge Y inside the room (fallback: room center Y)."

<ROUTINE FIND-ROOM-EDGE-Y (RID X DIR "AUX" T B Y)
    <SET T <ROOM-GET .RID ,ROOM-T>>
    <SET B <ROOM-GET .RID ,ROOM-B>>
    <COND (<G? .DIR 0>
           <SET Y .B>
           <REPEAT ()
               <COND (<L? .Y .T> <RETURN <ROOM-GET .RID ,ROOM-CY>>)>
               <COND (<==? <ROOMID-AT .X .Y> .RID> <RETURN .Y>)>
               <SET Y <- .Y 1>>>)
          (ELSE
           <SET Y .T>
           <REPEAT ()
               <COND (<G? .Y .B> <RETURN <ROOM-GET .RID ,ROOM-CY>>)>
               <COND (<==? <ROOMID-AT .X .Y> .RID> <RETURN .Y>)>
               <SET Y <+ .Y 1>>>)>>
"Connectivity"

;"Connects rooms with corridors/doors using a spanning tree plus a limited
  number of extra edges for loops.

Args:
  (none)

Returns:
  (none)"

<ROUTINE CONNECT-ROOMS ()
    <COND (<L? ,ROOM-COUNT 2> <RETURN>)>
    <RESET-CONNECTIONS>
    <RESET-CONNECTED>
    <SET-CONNECTED 1>
    <CONNECT-SPANNING-TREE>
    <ADD-EXTRA-LOOPS>>

;"Clears CONNECTED[] tracking table (used by spanning-tree construction).

Args:
	(none)

Returns:
	(none)"

<ROUTINE RESET-CONNECTED ()
    <DO (I 1 ,MAX-ROOMS) <PUTB ,CONNECTED <- .I 1> 0>>>

;"Marks a room as connected in CONNECTED[].

Args:
	R: Room ID.

Returns:
	(none)"

<ROUTINE SET-CONNECTED (R)
    <PUTB ,CONNECTED <- .R 1> 1>>

;"Checks CONNECTED[] for a room.

Args:
	R: Room ID.

Returns:
	T if connected; FALSE otherwise."

<ROUTINE CONNECTED? (R)
    <G? <GETB ,CONNECTED <- .R 1>> 0>>

;"Clears CONNECTIONS[][] tracking table.

Args:
	(none)

Returns:
	(none)"

<ROUTINE RESET-CONNECTIONS ()
    <DO (I 1 <G? .I <* ,MAX-ROOMS ,MAX-ROOMS>>) <PUTB ,CONNECTIONS <- .I 1> 0>>>

;"Computes linear index into CONNECTIONS[][] for pair (A, B).

Args:
	A, B: Room IDs.

Returns:
	Linear byte index."

<ROUTINE CONN-IDX (A B)
    <+ <* <- .A 1> ,MAX-ROOMS> <- .B 1>>>

;"Checks whether we've already connected room A to room B.

Args:
	A, B: Room IDs.

Returns:
	T if the pair is connected; FALSE otherwise."

<ROUTINE CONNECTED-PAIR? (A B)
    <G? <GETB ,CONNECTIONS <CONN-IDX .A .B>> 0>>

;"Marks rooms A and B as connected in CONNECTIONS[][].

Args:
	A, B: Room IDs.

Returns:
	T."

<ROUTINE NOTE-CONNECTION (A B)
    <PUTB ,CONNECTIONS <CONN-IDX .A .B> 1>
    <PUTB ,CONNECTIONS <CONN-IDX .B .A> 1>
    <RTRUE>>

;"Manhattan distance between the centers of rooms A and B.

Args:
	A, B: Room IDs.

Returns:
	Non-negative integer distance."

<ROUTINE DIST (A B "AUX" DX DY)
    <SET DX <ABS <- <ROOM-GET .A ,ROOM-CX> <ROOM-GET .B ,ROOM-CX>>>>
    <SET DY <ABS <- <ROOM-GET .A ,ROOM-CY> <ROOM-GET .B ,ROOM-CY>>>>
    <+ .DX .DY>>

;"Connects rooms with a greedy spanning tree: repeatedly pick the shortest
	edge from the connected set to an unconnected room.

Args:
	(none)

Returns:
	(none)"

<ROUTINE CONNECT-SPANNING-TREE ("AUX" COUNT D BESTA BESTB BESTD)
    <SET COUNT 1>
    <REPEAT ()
        <COND (<G=? .COUNT ,ROOM-COUNT> <RETURN>)>
        <SET BESTD 32767>
        <SET BESTA 1>
        <SET BESTB 2>
        <DO (A 1 ,ROOM-COUNT)
            <DO (B 1 ,ROOM-COUNT)
                <COND (<AND <CONNECTED? .A>
                            <NOT <CONNECTED? .B>>
                            <NOT <CONNECTED-PAIR? .A .B>>>
                       <SET D <DIST .A .B>>
                       <COND (<L? .D .BESTD>
                              <SET BESTD .D>
                              <SET BESTA .A>
                              <SET BESTB .B>)
                             (<AND <==? .D .BESTD> <==? <RNG 2> 1>>
                              ;"Random tie-breaker to keep layouts varied."
                              <SET BESTA .A>
                              <SET BESTB .B>)>)>>>
        <CONNECT-PAIR .BESTA .BESTB>
        <NOTE-CONNECTION .BESTA .BESTB>
        <SET-CONNECTED .BESTB>
        <SET COUNT <+ .COUNT 1>>>>

;"Adds a few extra corridor connections to create loops.

Args:
  (none)

Returns:
  (none)"

<ROUTINE ADD-EXTRA-LOOPS ("AUX" MAXEXTRA ADDED TRIES A B)
    ;"Add a few extra edges to create loops, but cap them to avoid spaghetti."
    <COND (<L? ,ROOM-COUNT 4> <RETURN>)>
    <SET MAXEXTRA </ ,ROOM-COUNT 3>>
    <COND (<L? .MAXEXTRA 1> <SET MAXEXTRA 1>)>
    <SET ADDED 0>
    <SET TRIES 0>
    <REPEAT ()
        <COND (<G=? .ADDED .MAXEXTRA> <RETURN>)>
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 80> <RETURN>)>
        <SET A <+ 1 <RNG <- ,ROOM-COUNT 1>>>>
        <SET B <+ 1 <RNG <- ,ROOM-COUNT 1>>>>
        <COND (<OR <==? .A .B>
                   <CONNECTED-PAIR? .A .B>
                   ;"Prefer shorter loop edges."
                   <G? <DIST .A .B> 18>>
               <>)
              (ELSE
               <CONNECT-PAIR .A .B>
               <NOTE-CONNECTION .A .B>
               <SET ADDED <+ .ADDED 1>>)>>>

;"Connects two rooms A and B by carving a corridor with doors.

Args:
  A, B: Room IDs.

Returns:
  (none)"

<ROUTINE CONNECT-PAIR (A B "AUX" AX AY BX BY ADX ADY)
    <SET AX <ROOM-GET .A ,ROOM-CX>>
    <SET AY <ROOM-GET .A ,ROOM-CY>>
    <SET BX <ROOM-GET .B ,ROOM-CX>>
    <SET BY <ROOM-GET .B ,ROOM-CY>>

    <SET ADX <ABS <- .BX .AX>>>
    <SET ADY <ABS <- .BY .AY>>>
    <COND (<G=? .ADX .ADY> <CONNECT-H .A .B>) (ELSE <CONNECT-V .A .B>)>>

;"Connects two rooms primarily with a horizontal corridor.

Args:
  ARID, BRID: Room IDs.

Returns:
  (none)"

<ROUTINE CONNECT-H (ARID BRID "AUX" AX AY BX BY Y1 Y2 EDGE1 EDGE2 XDOOR1 XSTART1 XDOOR2 XSTART2)
    <SET AX <ROOM-GET .ARID ,ROOM-CX>>
    <SET AY <ROOM-GET .ARID ,ROOM-CY>>
    <SET BX <ROOM-GET .BRID ,ROOM-CX>>
    <SET BY <ROOM-GET .BRID ,ROOM-CY>>

    <SET Y1 <CLAMP .AY <ROOM-GET .ARID ,ROOM-T> <ROOM-GET .ARID ,ROOM-B>>>
    <SET Y2 <CLAMP .BY <ROOM-GET .BRID ,ROOM-T> <ROOM-GET .BRID ,ROOM-B>>>
    <COND (<G? .BX .AX>
           <SET EDGE1 <FIND-ROOM-EDGE-X .ARID .Y1 1>>
           <SET EDGE2 <FIND-ROOM-EDGE-X .BRID .Y2 -1>>
           <SET XDOOR1 <+ .EDGE1 1>>
           <SET XSTART1 <+ .EDGE1 2>>
           <SET XDOOR2 <- .EDGE2 1>>
           <SET XSTART2 <- .EDGE2 2>>)
          (ELSE
           <SET EDGE1 <FIND-ROOM-EDGE-X .ARID .Y1 -1>>
           <SET EDGE2 <FIND-ROOM-EDGE-X .BRID .Y2 1>>
           <SET XDOOR1 <- .EDGE1 1>>
           <SET XSTART1 <- .EDGE1 2>>
           <SET XDOOR2 <+ .EDGE2 1>>
           <SET XSTART2 <+ .EDGE2 2>>)>
    <SET XSTART1 <CLAMP .XSTART1 1 ,MAP-W>>
    <SET XSTART2 <CLAMP .XSTART2 1 ,MAP-W>>
    <PLACE-DOOR .ARID .XDOOR1 .Y1>
    <CARVE-PASSAGE .XSTART1 .Y1>
    <PLACE-DOOR .BRID .XDOOR2 .Y2>
    <CARVE-PASSAGE .XSTART2 .Y2>
    <CARVE-TUNNEL .XSTART1 .Y1 .XSTART2 .Y2>>

;"Connects two rooms primarily with a vertical corridor.

Args:
  ARID, BRID: Room IDs.

Returns:
  (none)"

<ROUTINE CONNECT-V (ARID BRID "AUX" AX AY BX BY X1 X2 EDGE1 EDGE2 YDOOR1 YSTART1 YDOOR2 YSTART2)
    <SET AX <ROOM-GET .ARID ,ROOM-CX>>
    <SET AY <ROOM-GET .ARID ,ROOM-CY>>
    <SET BX <ROOM-GET .BRID ,ROOM-CX>>
    <SET BY <ROOM-GET .BRID ,ROOM-CY>>

    <SET X1 <CLAMP .AX <ROOM-GET .ARID ,ROOM-L> <ROOM-GET .ARID ,ROOM-R>>>
    <SET X2 <CLAMP .BX <ROOM-GET .BRID ,ROOM-L> <ROOM-GET .BRID ,ROOM-R>>>
    <COND (<G? .BY .AY>
           <SET EDGE1 <FIND-ROOM-EDGE-Y .ARID .X1 1>>
           <SET EDGE2 <FIND-ROOM-EDGE-Y .BRID .X2 -1>>
           <SET YDOOR1 <+ .EDGE1 1>>
           <SET YSTART1 <+ .EDGE1 2>>
           <SET YDOOR2 <- .EDGE2 1>>
           <SET YSTART2 <- .EDGE2 2>>)
          (ELSE
           <SET EDGE1 <FIND-ROOM-EDGE-Y .ARID .X1 -1>>
           <SET EDGE2 <FIND-ROOM-EDGE-Y .BRID .X2 1>>
           <SET YDOOR1 <- .EDGE1 1>>
           <SET YSTART1 <- .EDGE1 2>>
           <SET YDOOR2 <+ .EDGE2 1>>
           <SET YSTART2 <+ .EDGE2 2>>)>
    <SET YSTART1 <CLAMP .YSTART1 1 ,MAP-H>>
    <SET YSTART2 <CLAMP .YSTART2 1 ,MAP-H>>
    <PLACE-DOOR .ARID .X1 .YDOOR1>
    <CARVE-PASSAGE .X1 .YSTART1>
    <PLACE-DOOR .BRID .X2 .YDOOR2>
    <CARVE-PASSAGE .X2 .YSTART2>
    <CARVE-TUNNEL .X1 .YSTART1 .X2 .YSTART2>>

;"Places a door tile at (X, Y) and tags it with a room ID.

Args:
  RID: Room ID.
  X, Y: Map coordinates.

Returns:
  T if in bounds; FALSE otherwise."

<ROUTINE PLACE-DOOR (RID X Y)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <PUTB ,MAP <MAP-INDEX .X .Y> ,TILE-DOOR>
    <PUTB ,ROOMIDS <MAP-INDEX .X .Y> .RID>
    <RTRUE>>

;"Carves a corridor tile at (X, Y) if currently a wall.

Args:
  X, Y: Map coordinates.

Returns:
  T if in bounds; FALSE otherwise."

<ROUTINE CARVE-PASSAGE (X Y)
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <COND (<==? <TILE-AT .X .Y> ,TILE-WALL>
           <PUTB ,MAP <MAP-INDEX .X .Y> ,TILE-CORRIDOR>)>
    <RTRUE>>

;"Carves an L-shaped tunnel from (X1, Y1) to (X2, Y2).

Args:
  X1, Y1: Start.
  X2, Y2: End.

Returns:
  (none)"

<ROUTINE CARVE-TUNNEL (X1 Y1 X2 Y2 "AUX" MID)
    ;"L-shaped tunnel: horizontal then vertical."
    <SET MID .X2>
    <CARVE-HLINE .X1 .MID .Y1>
    <CARVE-VLINE .Y1 .Y2 .MID>>

;"Carves a horizontal corridor from X1 to X2 at row Y.

Args:
  X1, X2: Endpoints.
  Y: Row.

Returns:
  (none)"

<ROUTINE CARVE-HLINE (X1 X2 Y "AUX" STEP X)
    <COND (<G? .X1 .X2>
           <SET STEP -1>
           <SET X .X1>)
          (ELSE
           <SET STEP 1>
           <SET X .X1>)>
    <REPEAT ()
        <CARVE-PASSAGE .X .Y>
        <COND (<==? .X .X2> <RETURN>)>
        <SET X <+ .X .STEP>>>>

;"Carves a vertical corridor from Y1 to Y2 at column X.

Args:
	Y1, Y2: Endpoints.
	X: Column.

Returns:
	(none)"

<ROUTINE CARVE-VLINE (Y1 Y2 X "AUX" STEP Y)
    <COND (<G? .Y1 .Y2>
           <SET STEP -1>
           <SET Y .Y1>)
          (ELSE
           <SET STEP 1>
           <SET Y .Y1>)>
    <REPEAT ()
        <CARVE-PASSAGE .X .Y>
        <COND (<==? .Y .Y2> <RETURN>)>
        <SET Y <+ .Y .STEP>>>>

;"Computes the ROOMS table index for room R and field offset O.

Args:
    R: Room ID.
    O: Field offset (ROOM-L, ROOM-T, ...).

Returns:
    Word index into ROOMS."

<ROUTINE ROOM-OFFSET (R O)
    <+ <* <- .R 1> ,ROOM-STRIDE> .O>>

;"Reads a field from the ROOMS table.

Args:
    R: Room ID.
    O: Field offset.

Returns:
    Word value."

<ROUTINE ROOM-GET (R O)
    <GET ,ROOMS-TABLE <ROOM-OFFSET .R .O>>>

;"Writes a field to the ROOMS table.

Args:
    R: Room ID.
    O: Field offset.
    V: Value.

Returns:
    (none)"

<ROUTINE ROOM-SET (R O V)
    <PUT ,ROOMS-TABLE <ROOM-OFFSET .R .O> .V>>


;"Initializes the interior entrance list.

For extensibility: multiple entrances across floors are supported by the
INTERIOR-ENTRANCE-* tables.

Args:
  (none)

Returns:
  T."

<ROUTINE INIT-INTERIOR-ENTRANCES ()
    <SETG INTERIOR-ENTRANCE-COUNT 0>
    <DO (I 1 ,MAX-INTERIOR-ENTRANCES)
        <PUTB ,INTERIOR-ENTRANCE-FLOOR <- .I 1> 0>
        <PUTB ,INTERIOR-ENTRANCE-X <- .I 1> 0>
        <PUTB ,INTERIOR-ENTRANCE-Y <- .I 1> 0>
        <PUTB ,INTERIOR-ENTRANCE-ID <- .I 1> 0>>

    ;"Info Booth: always on floor 1 (random placement happens on first visit)."
    <SETG INTERIOR-ENTRANCE-COUNT <+ ,INTERIOR-ENTRANCE-COUNT 1>>
    <PUTB ,INTERIOR-ENTRANCE-FLOOR 0 1>
    <PUTB ,INTERIOR-ENTRANCE-X 0 0>
    <PUTB ,INTERIOR-ENTRANCE-Y 0 0>
    <PUTB ,INTERIOR-ENTRANCE-ID 0 ,INTERIOR-INFO-BOOTH>

    ;"Guaranteed Carrot Farm on a random one of the first 5 floors.
      The Info Booth occupies floor 1, so pick from floors 2..5."
    <SETG GUARANTEED-CARROT-FARM-FLOOR <+ 1 <RNG 4>>>
    <COND (<G? ,GUARANTEED-CARROT-FARM-FLOOR ,MAX-FLOORS>
           <SETG GUARANTEED-CARROT-FARM-FLOOR ,MAX-FLOORS>)>
    <SETG INTERIOR-ENTRANCE-COUNT <+ ,INTERIOR-ENTRANCE-COUNT 1>>
    <PUTB ,INTERIOR-ENTRANCE-FLOOR 1 ,GUARANTEED-CARROT-FARM-FLOOR>
    <PUTB ,INTERIOR-ENTRANCE-X 1 0>
    <PUTB ,INTERIOR-ENTRANCE-Y 1 0>
    <PUTB ,INTERIOR-ENTRANCE-ID 1 ,INTERIOR-CARROT-FARM>

    ;"Guaranteed Blacksmith on the last floor."
    <SETG INTERIOR-ENTRANCE-COUNT <+ ,INTERIOR-ENTRANCE-COUNT 1>>
    <PUTB ,INTERIOR-ENTRANCE-FLOOR 2 ,MAX-FLOORS>
    <PUTB ,INTERIOR-ENTRANCE-X 2 0>
    <PUTB ,INTERIOR-ENTRANCE-Y 2 0>
    <PUTB ,INTERIOR-ENTRANCE-ID 2 ,INTERIOR-BLACKSMITH>

    ;"Guaranteed Busker on exactly one random non-guaranteed floor.
          Pick from 2..(MAX-1), excluding the guaranteed carrot farm floor."
    <SETG GUARANTEED-BUSKER-FLOOR 0>
    <DO (I 1 20)
        <SETG GUARANTEED-BUSKER-FLOOR <+ 1 <RNG <- ,MAX-FLOORS 2>>>>
        <COND (<N==? ,GUARANTEED-BUSKER-FLOOR ,GUARANTEED-CARROT-FARM-FLOOR>
               <RETURN>)>>
    ;"Defensive: if we somehow failed to pick, fall back to floor 2 (unless it's carrot)."
    <COND (<L=? ,GUARANTEED-BUSKER-FLOOR 0>
           <SETG GUARANTEED-BUSKER-FLOOR 2>
           <COND (<==? ,GUARANTEED-BUSKER-FLOOR ,GUARANTEED-CARROT-FARM-FLOOR>
                  <SETG GUARANTEED-BUSKER-FLOOR 3>)>)>

    <COND (<G=? ,INTERIOR-ENTRANCE-COUNT ,MAX-INTERIOR-ENTRANCES> <RETURN>)>
    <SETG INTERIOR-ENTRANCE-COUNT <+ ,INTERIOR-ENTRANCE-COUNT 1>>
    <PUTB ,INTERIOR-ENTRANCE-FLOOR
          <- ,INTERIOR-ENTRANCE-COUNT 1>
          ,GUARANTEED-BUSKER-FLOOR>
    <PUTB ,INTERIOR-ENTRANCE-X <- ,INTERIOR-ENTRANCE-COUNT 1> 0>
    <PUTB ,INTERIOR-ENTRANCE-Y <- ,INTERIOR-ENTRANCE-COUNT 1> 0>
    <PUTB ,INTERIOR-ENTRANCE-ID <- ,INTERIOR-ENTRANCE-COUNT 1> ,INTERIOR-BUSKER>

    ;"Other floors: chance of an interior.
      The info booth never appears beyond floor 1."
    <DO (F 2 %<- ,MAX-FLOORS 1>)
        <COND (<AND <N==? .F ,GUARANTEED-CARROT-FARM-FLOOR>
                    <N==? .F ,GUARANTEED-BUSKER-FLOOR>
                    <L=? <RNG 100> ,INTERIOR-ENTRANCE-SPAWN-PCT>>
               <COND (<G=? ,INTERIOR-ENTRANCE-COUNT ,MAX-INTERIOR-ENTRANCES>
                      <RETURN>)>
               <PUTB ,INTERIOR-ENTRANCE-FLOOR ,INTERIOR-ENTRANCE-COUNT .F>
               <PUTB ,INTERIOR-ENTRANCE-X ,INTERIOR-ENTRANCE-COUNT 0>
               <PUTB ,INTERIOR-ENTRANCE-Y ,INTERIOR-ENTRANCE-COUNT 0>
               <PUTB ,INTERIOR-ENTRANCE-ID
                     ,INTERIOR-ENTRANCE-COUNT
                     <PICK-RANDOM-INTERIOR-ID>>
               <SETG INTERIOR-ENTRANCE-COUNT <+ ,INTERIOR-ENTRANCE-COUNT 1>>)>>
    <RTRUE>>

;"Picks a random non-booth interior ID for procedural placement."

<ROUTINE PICK-RANDOM-INTERIOR-ID ("AUX" R)
    <SET R <RNG 6>>
    <COND (<==? .R 1 2> ,INTERIOR-CARROT-FARM)
          (<==? .R 3 4> ,INTERIOR-BLACKSMITH)
          (<==? .R 5> ,INTERIOR-BEES)
          (ELSE ,INTERIOR-ORACLE-GROTTO)>>

;"Returns true if (X,Y) is a valid passable tile for placing an interior entrance."

<ROUTINE VALID-INTERIOR-TILE? (X Y)
    <AND <G? .X 0>
         <G? .Y 0>
         <L=? .X ,MAP-W>
         <L=? .Y ,MAP-H>
         <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
             <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
             <==? <TILE-AT .X .Y> ,TILE-DOOR>>
         <NOT <TRADER-AT? .X .Y>>
         <NOT <AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>>>>

;"Finds a random valid tile anywhere on the map for an interior entrance.
Returns coordinates via ENTRY-X/ENTRY-Y globals, or 0/0 if not found."

<ROUTINE FIND-RANDOM-INTERIOR-SPOT ("AUX" TRIES X Y)
    <SET TRIES 0>
    <SETG ENTRY-X 0>
    <SETG ENTRY-Y 0>
    <REPEAT ()
        <SET TRIES <+ .TRIES 1>>
        <COND (<G? .TRIES 400> <RFALSE>)>
        <SET X <+ 1 <RNG ,MAP-W>>>
        <SET Y <+ 1 <RNG ,MAP-H>>>
        <COND (<VALID-INTERIOR-TILE? .X .Y>
               <SETG ENTRY-X .X>
               <SETG ENTRY-Y .Y>
               <RTRUE>)>
        <AGAIN>>>

;"Fallback: scans the entire map for any valid interior tile."

<ROUTINE FIND-INTERIOR-SPOT-ANYWHERE ()
    <DO (YY 1 ,MAP-H)
        <DO (XX 1 ,MAP-W)
            <COND (<VALID-INTERIOR-TILE? .XX .YY>
                   <SETG ENTRY-X .XX>
                   <SETG ENTRY-Y .YY>
                   <RTRUE>)>>>
    <RFALSE>>

;"Precomputes a deterministic RNG seed for each floor.

Args:
  (none)

Returns:
  T."

<ROUTINE MAKE-FLOOR-SEEDS ("AUX" HI LO)
    ;"Generate per-floor seeds up front so map generation doesn't perturb them."
    <DO (F 1 ,MAX-FLOORS)
        <SET HI <RNG-WORD16>>
        <SET LO <RNG-WORD16>>
        <COND (<AND <==? .HI 0> <==? .LO 0>> <SET LO 1>)>
    <FLOOR-SET-SEEDS .F .HI .LO>>

    <RTRUE>>

;"Returns the interior ID for an entrance located at (X,Y) on floor F, or 0."

<ROUTINE INTERIOR-ID-AT (F X Y)
    <DO (I 1 ,INTERIOR-ENTRANCE-COUNT)
        (;else <RFALSE>)
        <COND (<AND <==? <GETB ,INTERIOR-ENTRANCE-FLOOR <- .I 1>> .F>
                    <==? <GETB ,INTERIOR-ENTRANCE-X <- .I 1>> .X>
                    <==? <GETB ,INTERIOR-ENTRANCE-Y <- .I 1>> .Y>>
               <RETURN <GETB ,INTERIOR-ENTRANCE-ID <- .I 1>>>)>>>

;"Converts (X, Y) into a linear index into MAP/ROOMIDS.

Args:
	X, Y: Map coordinates (1-based).

Returns:
	Linear index (0-based) suitable for GETB/PUTB."

<ROUTINE MAP-INDEX (X Y)
    <+ <* <- .Y 1> ,MAP-W> <- .X 1>>>

;"Gets the tile code at (X, Y).

Args:
	X, Y: Map coordinates.

Returns:
	Byte tile code."

<ROUTINE TILE-AT (X Y)
    <GETB ,MAP <MAP-INDEX .X .Y>>>

;"Gets the room ID at (X, Y), or 0 if not in a room.

Args:
	X, Y: Map coordinates.

Returns:
	Byte room ID."

<ROUTINE ROOMID-AT (X Y)
    <GETB ,ROOMIDS <MAP-INDEX .X .Y>>>

;"Returns true if a tile is walkable.

Args:
  X, Y: Map coordinates.

Returns:
  T if walkable; FALSE otherwise."

<ROUTINE FLOOR? (X Y)
    <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
        <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>
        <AND <==? <TILE-AT .X .Y> ,TILE-DOOR>
             <NOT <LOCKED-DOOR-CLOSED-AT? .X .Y>>>
        <==? <TILE-AT .X .Y> ,TILE-INTERIOR>
        <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
        <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>>

;"Returns true if (X,Y) is a valid passable tile for placing a ground item (food/potion)."

<ROUTINE VALID-GROUND-ITEM-TILE? (X Y)
    <AND <G? .X 0>
         <G? .Y 0>
         <L=? .X ,MAP-W>
         <L=? .Y ,MAP-H>
         <OR <==? <TILE-AT .X .Y> ,TILE-FLOOR>
             <==? <TILE-AT .X .Y> ,TILE-CORRIDOR>>
         <NOT <AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>>
         <NOT <OR <==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
                  <==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>>>
         <NOT <TRADER-AT? .X .Y>>
         <L=? <TREASURE-AT? .X .Y> 0>
         <L=? <ENEMY-AT .X .Y> 0>
         <L=? <GOLD-OBJ-AT .X .Y> 0>
         <L=? <FOOD-OBJ-AT .X .Y> 0>
         <L=? <WEAPON-OBJ-AT .X .Y> 0>
         <NOT <POTION-AT? .X .Y>>>>

;"Places any interior entrance tiles for floor F onto the newly built map.

If an entrance has X/Y=0, it will be placed near room 1's center.

Args:
  F: Floor number (1-based).

Returns:
  T."

<ROUTINE PLACE-INTERIOR-ENTRANCES (F "AUX" X Y)
    <DO (I 1 ,INTERIOR-ENTRANCE-COUNT)
        <COND (<==? <GETB ,INTERIOR-ENTRANCE-FLOOR <- .I 1>> .F>
               <SET X <GETB ,INTERIOR-ENTRANCE-X <- .I 1>>>
               <SET Y <GETB ,INTERIOR-ENTRANCE-Y <- .I 1>>>
               ;"If coordinates are unset, find a spot."
               <COND (<NOT <AND <G? .X 0> <G? .Y 0>>>
                      <SETG ENTRY-X 0>
                      <SETG ENTRY-Y 0>
                      <COND (<FIND-RANDOM-INTERIOR-SPOT>)
                            (<FIND-INTERIOR-SPOT-ANYWHERE>)>
                      <SET X ,ENTRY-X>
                      <SET Y ,ENTRY-Y>)>
               ;"Place the tile if we found a valid spot."
               <COND (<AND <G? .X 0> <G? .Y 0>>
                      <PUTB ,INTERIOR-ENTRANCE-X <- .I 1> .X>
                      <PUTB ,INTERIOR-ENTRANCE-Y <- .I 1> .Y>
                      <PUTB ,MAP <MAP-INDEX .X .Y> ,TILE-INTERIOR>)>)>>
    <RTRUE>>

;"Checks whether a floor should contain a trader.

Args:
  F: Floor number (1-based).

Returns:
  T if the trader should appear on that floor; FALSE otherwise."

<ROUTINE TRADER-ON-FLOOR? (F)
    <==? <MOD .F 5> 0>>

;"For forced-entry floors (landing from stairs), carves room 1 around the
  landing coordinate so the stairs always land in a room.

Args:
  (none) Uses ENTRY-X/ENTRY-Y globals.

Returns:
  T."

<ROUTINE PLACE-ENTRY-ROOM ("AUX" W H L T)
    ;"Carve room 1 around the entry coordinate so the stairs are always usable."
    <SET W 9>
    <SET H 7>
    <SET L <CLAMP <- ,ENTRY-X </ .W 2>> 2 <+ 1 <- ,MAP-W .W>>>>
    <SET T <CLAMP <- ,ENTRY-Y </ .H 2>> 2 <+ 1 <- ,MAP-H .H>>>>
    <CARVE-ROOM .L .T .W .H 1 ,ROOMSHAPE-RECT>
    <SETG ROOM-COUNT 1>
    <RTRUE>>

;"Forces a map cell to be passable (corridor) regardless of generation.
  Used to harden stair landing coordinates against edge cases.

Args:
  X, Y: Map coordinates.

Returns:
  T if in bounds; FALSE otherwise."

<ROUTINE FORCE-PASSABLE (X Y)
    ;"Ensure stairs land on a walkable tile even if the regen map differs."
    <COND (<NOT <IN-BOUNDS? .X .Y>> <RFALSE>)>
    <PUTB ,MAP <MAP-INDEX .X .Y> ,TILE-CORRIDOR>
    <PUTB ,ROOMIDS <MAP-INDEX .X .Y> 0>
    <RTRUE>>
