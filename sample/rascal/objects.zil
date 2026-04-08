"Rascal object model (floors, items, enemies, inventories)"

;"Rascal needs to store various bits of data for every object and every tile on
  every floor, and since we're targeting the Z-machine, RAM is at a premium.
  This means we have an important choice to make: how will we store it all?

  The most memory-efficient way to do it would be to define large tables. We
  could define a table for each property (ENEMY-X, ENEMY-Y, ENEMY-HP...), or we
  could use DEFSTRUCT to combine them all into one structure (similar to what
  zillib does for noun phrases and parser results).

  Another way is to define Z-machine objects. They're convenient and flexible,
  and in particular they make it possible to use exactly the same objects in
  interiors as in the dungeon inventory. The drawback is memory consumption: the
  object table and property headers require about 10 KB of dynamic memory (RAM).

  Since Rascal is limited in scope, and it's meant to serve as an example of
  coding in ZIL, we use Z-machine objects. The preallocated item and enemy pools
  hold unused objects until the game needs to place them somewhere.

  If memory consumption becomes an issue in the future, we can reclaim a few KB
  from the property headers by defining those large tables and then giving each
  object an index property."

<USE "TEMPLATE">

<CONSTANT RASCAL-ITEM-POOL-SIZE <* 10 ,MAX-FLOORS>>
<CONSTANT RASCAL-ENEMY-POOL-SIZE <* ,MAX-ENEMIES ,MAX-FLOORS>>

;"Object roots"

<OBJECT RASCAL-OBJECTS (IN ROOMS)>

<OBJECT RASCAL-FLOORS (IN RASCAL-OBJECTS)>

;"These flags are meaningless when set on the pools, but they have to be set
  on at least one object in order to be valid."
<OBJECT RASCAL-ITEM-POOL (IN RASCAL-OBJECTS) (FLAGS SEENBIT)>
<OBJECT RASCAL-ENEMY-POOL (IN RASCAL-OBJECTS) (FLAGS TAMEBIT SOLDBIT)>

;"Player inventory container: item objects are MOVEd here when carried."

<OBJECT PLAYER-INVENTORY (IN RASCAL-OBJECTS)>

;"Current floor container (set by ENTER-FLOOR)."

<GLOBAL CURRENT-FLOOR-OBJ RASCAL-FLOORS>

;"Equipped weapon: object reference, or <> when unarmed."

<GLOBAL EQUIPPED-WEAPON <>>

;"These properties are only used to hold single-byte values, so we use PROPDEF
  to write them as single-byte properties."

<PROPDEF R-ITKIND <> (R-ITKIND N:FIX = <BYTE .N>)>
<PROPDEF R-ITID <> (R-ITID N:FIX = <BYTE .N>)>
<PROPDEF R-ITLVL <> (R-ITLVL N:FIX = <BYTE .N>)>
<PROPDEF R-ITENCH <> (R-ITENCH N:FIX = <BYTE .N>)>
<PROPDEF R-ITAMT <> (R-ITAMT N:FIX = <BYTE .N>)>
<PROPDEF R-X <> (R-X N:FIX = <BYTE .N>)>
<PROPDEF R-Y <> (R-Y N:FIX = <BYTE .N>)>

<PROPDEF R-ETYPE <> (R-ETYPE N:FIX = <BYTE .N>)>
<PROPDEF R-EHP <> (R-EHP N:FIX = <BYTE .N>)>
<PROPDEF R-TON <> (R-TON N:FIX = <BYTE .N>)>

;"Templates"

<OBJECT-TEMPLATE RASCAL-ITEM = OBJECT
    (DESC "item")
    (LOC RASCAL-ITEM-POOL)
    (SYNONYM \,DUMMY-NOUN \,DUMMY-NOUN)
    ;"The third adjective slot is used by our parser hook."
    (ADJECTIVE \,DUMMY-ADJ \,DUMMY-ADJ \,DUMMY-ADJ)
    (PLURAL <> <>)
    (PDESC "")  ;"provide a nonzero PDESC so the parser knows they might be indistinguishable"
    (ARTICLE <>)
    (ACTION <>)
    (FLAGS TAKEBIT TRYTAKEBIT)
    (R-ITKIND 0)
    (R-ITID 0)
    (R-ITLVL 0)
    (R-ITENCH 0)
    (R-ITAMT 0)
    (R-X 0)
    (R-Y 0)>

<OBJECT-TEMPLATE RASCAL-ENEMY = OBJECT
    (DESC "enemy")
    (LOC RASCAL-ENEMY-POOL)
    (SYNONYM \,DUMMY-NOUN \,DUMMY-NOUN)
    (ADJECTIVE \,DUMMY-ADJ \,DUMMY-ADJ)
    (PLURAL <>)
    (PDESC "")
    (ARTICLE <>)
    (PRONOUN IT)
    (ACTION <>)
    (DESCFCN <>)
    (GENERIC <>)
    (FLAGS NDESCBIT)
    (R-ETYPE 0)
    (R-EHP 0)
    (R-X 0)
    (R-Y 0)>

<OBJECT-TEMPLATE RASCAL-FLOOR = OBJECT
    (FLAGS NDESCBIT)
    (R-SEEDS 0 0)
    (R-STAIRS 0 0)>

<OBJECT-TEMPLATE RASCAL-TRADER = OBJECT
    (R-TON 0)
    (R-X 0)
    (R-Y 0)>

;"Floor objects + per-floor trader objects"

;"A helper function to return a list of numbers from 1..N."
<DEFINE SEQ (N "AUX" (I 1))
    <MAPF ,LIST
          <FUNCTION ("AUX" (J .I))
              <SET I <+ .I 1>>
              <COND (<==? .J .N> <MAPSTOP .J>)
                    (ELSE <MAPRET .J>)>>>>

;"Define a RASCAL-FLOOR and RASCAL-TRADER for each floor..."
<BIND (L FAS TAS)
    <SET L <MAPF ,LIST
                 <FUNCTION (I "AUX" FA TA)
                     <SET FA <PARSE <STRING "R-FLOOR-" <UNPARSE .I>>>>
                     <SET TA <PARSE <STRING "R-TRADER-" <UNPARSE .I>>>>
                     <RASCAL-FLOOR .FA (IN RASCAL-FLOORS)>
                     <RASCAL-TRADER .TA (IN .FA)>
                     ;"Return the floor and trader names to be split into FAS/TAS"
                     (.FA .TA)>
                 <SEQ ,MAX-FLOORS>>>

    ;"...and two tables listing all of them."
    <SET FAS <MAPF ,LIST 1 .L>>
    <SET TAS <MAPF ,LIST 2 .L>>
    <CONSTANT FLOOR-OBJS <PTABLE !.FAS>>
    <CONSTANT TRADER-OBJS <PTABLE !.TAS>>>

<ROUTINE FLOOR-OBJ (F)
    <COND (<OR <L? .F 1> <G? .F ,MAX-FLOORS>> <SET F 1>)>
    <GET ,FLOOR-OBJS <- .F 1>>>

<ROUTINE TRADER-OBJ (F)
    <COND (<OR <L? .F 1> <G? .F ,MAX-FLOORS>> <SET F 1>)>
    <GET ,TRADER-OBJS <- .F 1>>>

<GLOBAL INV-SLOTS <ITABLE ,INV-SIZE (WORD) 0>>
<GLOBAL TRINV-SLOTS <ITABLE <* ,MAX-FLOORS ,TRINV-SIZE> (WORD) 0>>

<ROUTINE INV-SLOT-IDX (SLOT)
    <- .SLOT 1>>

<ROUTINE TRINV-SLOT-IDX (F SLOT)
    <+ <* <- .F 1> ,TRINV-SIZE> <- .SLOT 1>>>

<ROUTINE CLEAR-RASCAL-ITEM-SLOT-REFS (O)
    <DO (I 1 ,INV-SIZE)
        <COND (<==? <GET ,INV-SLOTS <INV-SLOT-IDX .I>> .O>
               <PUT ,INV-SLOTS <INV-SLOT-IDX .I> 0>)>>
    <DO (F 1 ,MAX-FLOORS)
        <DO (I 1 ,TRINV-SIZE)
            <COND (<==? <GET ,TRINV-SLOTS <TRINV-SLOT-IDX .F .I>> .O>
                   <PUT ,TRINV-SLOTS <TRINV-SLOT-IDX .F .I> 0>)>>>
    <RTRUE>>

;"Floor property helpers"

<ROUTINE WORD16-HI-BYTE (W)
    <BAND <LSH .W -8> 255>>

<ROUTINE WORD16-LO-BYTE (W)
    <BAND .W 255>>

<ROUTINE WORD16-FROM-BYTES (HI LO)
    <BAND <BOR <LSH .HI 8> .LO> -1>>

<ROUTINE FLOOR-SEED-HI (F "AUX" PT)
    <SET PT <GETPT <FLOOR-OBJ .F> ,P?R-SEEDS>>
    <GET .PT 0>>

<ROUTINE FLOOR-SEED-LO (F "AUX" PT)
    <SET PT <GETPT <FLOOR-OBJ .F> ,P?R-SEEDS>>
    <GET .PT 1>>

<ROUTINE FLOOR-SET-SEEDS (F HI LO "AUX" PT)
    <SET PT <GETPT <FLOOR-OBJ .F> ,P?R-SEEDS>>
    <PUT .PT 0 .HI>
    <PUT .PT 1 .LO>
    <RTRUE>>

;"R-STAIRS byte layout: UP-X, UP-Y, DOWN-X, DOWN-Y"

<ROUTINE FLOOR-UP-X (F)
    <GETB <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS> 0>>

<ROUTINE FLOOR-UP-Y (F)
    <GETB <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS> 1>>

<ROUTINE FLOOR-DOWN-X (F)
    <GETB <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS> 2>>

<ROUTINE FLOOR-DOWN-Y (F)
    <GETB <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS> 3>>

<ROUTINE FLOOR-SET-UP-STAIRS (F X Y "AUX" PT)
    <SET PT <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS>>
    <PUTB .PT 0 .X>
    <PUTB .PT 1 .Y>
    <RTRUE>>

<ROUTINE FLOOR-SET-DOWN-STAIRS (F X Y "AUX" PT)
    <SET PT <GETPT <FLOOR-OBJ .F> ,P?R-STAIRS>>
    <PUTB .PT 2 .X>
    <PUTB .PT 3 .Y>
    <RTRUE>>

;"Allocation helpers"

<ROUTINE CLEAR-RASCAL-ITEM (O)
    <PUTP .O ,P?R-ITKIND 0>
    <PUTP .O ,P?R-ITID 0>
    <PUTP .O ,P?R-ITLVL 0>
    <PUTP .O ,P?R-ITENCH 0>
    <PUTP .O ,P?R-ITAMT 0>
    <FCLEAR .O ,SEENBIT>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <RTRUE>>

<ROUTINE CLEAR-RASCAL-ENEMY (O)
    <PUTP .O ,P?R-ETYPE 0>
    <PUTP .O ,P?R-EHP 0>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <PUTP .O ,P?ACTION <>>
    <PUTP .O ,P?DESCFCN <>>
    <PUTP .O ,P?GENERIC <>>
    <FCLEAR .O ,PERSONBIT>
    <FCLEAR .O ,TAMEBIT>
    <FCLEAR .O ,SOLDBIT>
    <RTRUE>>

<ROUTINE ALLOC-RASCAL-ITEM ("AUX" O)
    <COND (<SET O <FIRST? ,RASCAL-ITEM-POOL>>
           <MOVE .O ,RASCAL-OBJECTS>
           <CLEAR-RASCAL-ITEM .O>
           .O)
          (ELSE <>)>>

<ROUTINE FREE-RASCAL-ITEM (O)
    <COND (<==? ,EQUIPPED-WEAPON .O> <SETG EQUIPPED-WEAPON <>>)>
    <CLEAR-RASCAL-ITEM-SLOT-REFS .O>
    <CLEAR-RASCAL-ITEM .O>
    <MOVE .O ,RASCAL-ITEM-POOL>
    <RTRUE>>

<ROUTINE ALLOC-RASCAL-ENEMY ("AUX" O)
    <COND (<SET O <FIRST? ,RASCAL-ENEMY-POOL>>
           <MOVE .O ,RASCAL-OBJECTS>
           <CLEAR-RASCAL-ENEMY .O>
           .O)
          (ELSE <>)>>

<ROUTINE FREE-RASCAL-ENEMY (O)
    <FREE-RASCAL-ITEM-CHILDREN .O 0>
    <CLEAR-RASCAL-ENEMY .O>
    <MOVE .O ,RASCAL-ENEMY-POOL>
    <RTRUE>>

;"Free item children under CONTAINER.

If KIND is 0, frees all item children. Otherwise frees only items whose
R-ITKIND matches KIND. Non-items are ignored."

<ROUTINE FREE-RASCAL-ITEM-CHILDREN (CONTAINER KIND "AUX" O NXT K)
    <SET O <FIRST? .CONTAINER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN T>)>
        <SET NXT <NEXT? .O>>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<AND <G? .K 0>
                    <OR <L=? .KIND 0> <==? .K .KIND>>>
               <FREE-RASCAL-ITEM .O>)>
        <SET O .NXT>>>

;"Free enemy children under CONTAINER. Non-enemies are ignored."

<ROUTINE FREE-RASCAL-ENEMY-CHILDREN (CONTAINER "AUX" O NXT T)
    <SET O <FIRST? .CONTAINER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN T>)>
        <SET NXT <NEXT? .O>>
        <SET T <GETP .O ,P?R-ETYPE>>
        <COND (<G? .T 0> <FREE-RASCAL-ENEMY .O>)>
        <SET O .NXT>>>

;"Item pool instances"

<REPEAT ((I 1))
    <COND (<G=? .I ,RASCAL-ITEM-POOL-SIZE> <RETURN>)>
    <RASCAL-ITEM <PARSE <STRING "R-ITEM-" <UNPARSE .I>>> (IN RASCAL-ITEM-POOL)>
    <SET I <+ .I 1>>>

<REPEAT ((I 1))
    <COND (<G=? .I ,RASCAL-ENEMY-POOL-SIZE> <RETURN>)>
    <RASCAL-ENEMY <PARSE <STRING "R-ENEMY-" <UNPARSE .I>>> (IN RASCAL-ENEMY-POOL)>
    <SET I <+ .I 1>>>
