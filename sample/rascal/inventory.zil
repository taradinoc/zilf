"Inventory management"

;"Inventory is stored as child item objects under PLAYER-INVENTORY.
  EQUIPPED-WEAPON (global, defined in objects.zil) points to the equipped
  weapon item object, or <> when unarmed."

<ROUTINE INV-COUNT ("AUX" O C)
    <SET C 0>
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN .C>)>
        <SET C <+ .C 1>>
        <SET O <NEXT? .O>>>>

;"Returns the Nth inventory item object (1-based), or 0 if out of range."

<ROUTINE INV-NTH-OBJ (SLOT "AUX" O I)
    <COND (<OR <L? .SLOT 1> <G? .SLOT ,INV-SIZE>> <RETURN 0>)>
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <SET I 1>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <COND (<==? .I .SLOT> <RETURN .O>)>
        <SET I <+ .I 1>>
        <SET O <NEXT? .O>>>>

;"Moves an existing item object into the player's inventory.

Returns the object if moved; 0 if pack is full or O invalid."

<ROUTINE INV-TAKE-OBJ (O)
    <COND (<NOT .O> <RETURN 0>)>
    <COND (<G=? <INV-COUNT> ,INV-SIZE> <RETURN 0>)>
    <REMOVE .O>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <MOVE .O ,PLAYER-INVENTORY>
    .O>

"Core inventory operations"

;"Checks whether the player is currently carrying the Trophy of Scryra."

<ROUTINE PLAYER-HAS-TROPHY? ("AUX" O)
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN <>>)>
        <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-TREASURE>
                    <==? <GETP .O ,P?R-ITID> ,TREASURE-TROPHY>>
               <RETURN T>)>
        <SET O <NEXT? .O>>>>

;"Clears the player's inventory.

Args:
  (none)

Returns:
  T."

<ROUTINE INV-CLEAR ("AUX" O N)
    <SET O <FIRST? ,PLAYER-INVENTORY>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <REMOVE .O>
        <FREE-RASCAL-ITEM .O>
        <SET O .N>>
    <SETG EQUIPPED-WEAPON <>>
    <RTRUE>>

;"Appends an item to the player's inventory.

Args:
  KIND: Item kind code (ITEMKIND-*).
  ID: Item ID within that kind.

Returns:
  T if added; FALSE if inventory is full."

<ROUTINE INV-ADD (KIND ID "AUX" O)
    <COND (<G=? <INV-COUNT> ,INV-SIZE> <RETURN 0>)>
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND .KIND>
    <PUTP .O ,P?R-ITID .ID>
    <PUTP .O ,P?R-ITLVL 0>
    <PUTP .O ,P?R-ITENCH 0>
    <PUTP .O ,P?R-ITAMT 0>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <MOVE .O ,PLAYER-INVENTORY>
    .O>

;"Appends a weapon to the player's inventory.

Args:
  TYPE: Weapon type code (WEAPON-*).
  LVL: Weapon level.

Returns:
  T if added; FALSE if inventory is full."

<ROUTINE INV-ADD-WEAPON (TYPE LVL ENCH "AUX" O)
    <COND (<NOT <SET O <INV-ADD ,ITEMKIND-WEAPON .TYPE>>> <RETURN 0>)>
    <PUTP .O ,P?R-ITLVL .LVL>
    <PUTP .O ,P?R-ITENCH .ENCH>
    .O>

;"Removes an inventory slot, shifting down later slots to keep inventory packed.

Args:
  SLOT: 1-based slot number.

Returns:
  T if removed; FALSE if slot is out of range."

<ROUTINE INV-REMOVE (SLOT "AUX" O)
    <SET O <INV-NTH-OBJ .SLOT>>
    <COND (<L=? .O 0> <RFALSE>)>
    <COND (<==? ,EQUIPPED-WEAPON .O> <SETG EQUIPPED-WEAPON <>>)>
    <REMOVE .O>
    <FREE-RASCAL-ITEM .O>
    <RTRUE>>

;"Formats an inventory slot as a display name.

Args:
  SLOT: 1-based slot number.

Returns:
  Item name string, or empty string if slot is invalid."

<ROUTINE INV-NAME (SLOT "AUX" O K ID)
    <SET O <INV-NTH-OBJ .SLOT>>
    <COND (<L=? .O 0> "")>
    <SET K <GETP .O ,P?R-ITKIND>>
    <SET ID <GETP .O ,P?R-ITID>>
    <COND (<==? .K ,ITEMKIND-FOOD> <FOOD-NAME .ID>)
          (<==? .K ,ITEMKIND-TREASURE> <TREASURE-NAME .ID>)
          (<==? .K ,ITEMKIND-WEAPON> <WEAPON-NAME .ID>)
          (<==? .K ,ITEMKIND-POTION> <POTION-DISPLAY-NAME .ID>)
          (<==? .K ,ITEMKIND-KEY> <KEY-NAME .ID>)
          (ELSE "item")>>

  <ROUTINE INV-FIND-KEY-OBJ (LOCKTYPE "AUX" O)
      <SET O <FIRST? ,PLAYER-INVENTORY>>
      <REPEAT ()
          <COND (<NOT .O> <RETURN 0>)>
          <COND (<AND <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-KEY>
                      <==? <GETP .O ,P?R-ITID> .LOCKTYPE>>
                 <RETURN .O>)>
          <SET O <NEXT? .O>>>>

  <ROUTINE INV-CONSUME-KEY (LOCKTYPE "AUX" O)
      <SET O <INV-FIND-KEY-OBJ .LOCKTYPE>>
      <COND (<L=? .O 0> <RFALSE>)>
      <REMOVE .O>
      <FREE-RASCAL-ITEM .O>
      <RTRUE>>

"Inventory selection popup (upper window)"

<ROUTINE PRINTC-REPEAT (CH COUNT)
    <DO (I 1 .COUNT) <PRINTC .CH>>
    <RTRUE>>

;"Draws an inventory picker as a popup box in the upper window, reads one key,
  clears the popup with spaces, then redraws the game UI.

MODE:
  0 = Drop prompt
  1 = Equip prompt
  2 = Ingest prompt

Returns:
  ZSCII character code from GETCHAR."

<ROUTINE POPUP-INVENTORY-GETCHAR (MODE "AUX" W INNERW H TOP LEFT C ROW INTERIORCNT CLEARCNT CNT O)
    ;"Size and position the box roughly centered in the upper window."
    <SET W <- <LOWCORE SCRH> 4>>
    <COND (<G? .W 60> <SET W 60>)>
    <COND (<L? .W 34> <SET W 34>)>
    <SET INNERW <- .W 2>>

    ;"Top border + prompt + items + bottom border."
    <SET CNT <INV-COUNT>>
    <SET H <+ .CNT 3>>
    <COND (<G? .H ,UPPER-HEIGHT> <SET H ,UPPER-HEIGHT>)>
    <SET TOP <+ 1 </ <- ,UPPER-HEIGHT .H> 2>>>
    <COND (<L? .TOP 1> <SET TOP 1>)>
    <SET LEFT <+ 1 </ <- <LOWCORE SCRH> .W> 2>>>
    <COND (<L? .LEFT 1> <SET LEFT 1>)>

    <SCREEN 1>
    <UI-LOG-COLOR>

    ;"Top border."
    <CURSET .TOP .LEFT>
    <PRINTC !\+>
    <PRINTC-REPEAT !\- .INNERW>
    <PRINTC !\+>

    ;"Interior rows (blank, with side borders)."
    <SET INTERIORCNT <- .H 2>>
    <DO (I 1 .INTERIORCNT)
        <SET ROW <+ .TOP .I>>
        <CURSET .ROW .LEFT>
        <PRINTC !\|>
        <PRINTC-REPEAT !\  .INNERW>
        <PRINTC !\|>>

    ;"Bottom border."
    <CURSET <+ .TOP <- .H 1>> .LEFT>
    <PRINTC !\+>
    <PRINTC-REPEAT !\- .INNERW>
    <PRINTC !\+>

    ;"Prompt line."
    <CURSET <+ .TOP 1> <+ .LEFT 2>>
    <COND (<==? .MODE 1>
           <TELL "Equip which weapon? (1-" N .CNT "; 0=10; Q cancels)">)
          (<==? .MODE 2>
           <TELL "Ingest which item? (1-" N .CNT "; 0=10; Q cancels)">)
          (ELSE <TELL "Drop which item? (1-" N .CNT "; 0=10; Q cancels)">)>

    ;"Inventory lines."
    <DO (I 1 .CNT)
        <COND (<G? .I <- .H 3>> <RETURN>)>
        <CURSET <+ .TOP <+ .I 1>> <+ .LEFT 2>>
        <SET O <INV-NTH-OBJ .I>>
        <COND (<AND <G? .O 0> <==? <GETP .O ,P?R-ITKIND> ,ITEMKIND-WEAPON>>
               <TELL N .I ") L" N <GETP .O ,P?R-ITLVL>>
               <COND (<G? <GETP .O ,P?R-ITENCH> 0> <TELL "+" N <GETP .O ,P?R-ITENCH>>)>
               <TELL " " <INV-NAME .I>>
               <COND (<==? .O ,EQUIPPED-WEAPON> <TELL " (equipped)">)>)
              (ELSE <TELL N .I ") " <INV-NAME .I>>)>>

    <PROG ()
        <SET C <GETCHAR>>
        <COND (<==? .C 254 ;"mouse click"> <AGAIN>)>>

    ;"Clear the popup with spaces."
    <SET CLEARCNT <- .H 1>>
    <DO (I 0 .CLEARCNT)
        <CURSET <+ .TOP .I> .LEFT>
        <PRINTC-REPEAT !\  .W>>

    ;"Restore the UI immediately. (Main loop may redraw again; that's fine.)"
    <SETG FULL-REDRAW? T>
    <DRAW>

    .C>

<ROUTINE INVENTORY-SELL-VALUE ("AUX" SUM K ID LVL ENCH CNT O)
    <SET SUM 0>
    <SET CNT <INV-COUNT>>
    <DO (I 1 .CNT)
        <SET O <INV-NTH-OBJ .I>>
        <COND (<NOT .O> <AGAIN>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <SET ID <GETP .O ,P?R-ITID>>
        <SET LVL <GETP .O ,P?R-ITLVL>>
        <SET ENCH <GETP .O ,P?R-ITENCH>>
        <SET SUM <+ .SUM <TRADER-BUY-PRICE .K .ID .LVL .ENCH>>>>
    .SUM>

"Equipment operations"

;"Prompts for an inventory slot and equips that weapon if the player can wield it.

Args:
  (none)

Returns:
  T if a weapon was equipped; FALSE otherwise."

<ROUTINE TRY-EQUIP-WEAPON ("AUX" C SLOT K ID LVL ENCH WSTR NEED)
    <COND (<L=? <INV-COUNT> 0> <LOG "You have nothing to equip." CR> <RFALSE>)>
    <SET C <POPUP-INVENTORY-GETCHAR 1>>
    <COND (<==? .C !\Q !\q> <LOG "Never mind." CR> <RFALSE>)>
    <SET SLOT <DIGIT-TO-SLOT .C>>
    <COND (<OR <L? .SLOT 1> <G? .SLOT <INV-COUNT>>>
           <LOG "Never mind." CR>
           <RFALSE>)>
    <SET K <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITKIND>>
    <SET ID <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITID>>
    <SET LVL <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITLVL>>
    <SET ENCH <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITENCH>>
    <COND (<N==? .K ,ITEMKIND-WEAPON> <LOG "That's not a weapon." CR> <RFALSE>)>
    <SET WSTR <+ ,PLAYER-STR .ENCH>>
    <COND (<L? .WSTR .LVL>
           <SET NEED <- .LVL .ENCH>>
           <COND (<L? .NEED 1> <SET NEED 1>)>
           <LOG "You're not strong enough to wield that (need STR >= "
                N
                .NEED
                ")."
                CR>
           <RFALSE>)>
    <SETG EQUIPPED-WEAPON <INV-NTH-OBJ .SLOT>>
    <LOG "You wield the level " N .LVL>
    <COND (<G? .ENCH 0> <LOG "+" N .ENCH>)>
    <LOG " " <WEAPON-NAME .ID> "." CR>
    <RTRUE>>

;"Equips the best weapon currently carried (highest level that STR can wield).

Args:
  (none)

Returns:
  T if a weapon was equipped; FALSE if no wieldable weapon exists."

<ROUTINE AUTO-EQUIP-WEAPON ("AUX" BESTS BESTLVL BESTDMG K LVL ENCH WSTR DMG ID CNT)
    <SET BESTS 0>
    <SET BESTLVL 0>
    <SET BESTDMG 0>
    <SET CNT <INV-COUNT>>
    <DO (I 1 .CNT)
        <SET K <GETP <INV-NTH-OBJ .I> ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-WEAPON>
               <SET LVL <GETP <INV-NTH-OBJ .I> ,P?R-ITLVL>>
               <SET ENCH <GETP <INV-NTH-OBJ .I> ,P?R-ITENCH>>
               <SET WSTR <+ ,PLAYER-STR .ENCH>>
               <COND (<G=? .WSTR .LVL>
                      <SET ID <GETP <INV-NTH-OBJ .I> ,P?R-ITID>>
                      <SET DMG <WEAPON-BASE-DMG .ID>>
                      <COND (<OR <L? .BESTS 1>
                                 <G? .LVL .BESTLVL>
                                 <AND <==? .LVL .BESTLVL> <G? .DMG .BESTDMG>>>
                             <SET BESTS .I>
                             <SET BESTLVL .LVL>
                             <SET BESTDMG .DMG>)>)>)>>
    <SETG EQUIPPED-WEAPON <COND (<G? .BESTS 0> <INV-NTH-OBJ .BESTS>) (ELSE <>)>>
    <COND (<G? .BESTS 0>
           <LOG "You wield the level " N .BESTLVL>
           <COND (<G? <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH> 0>
                  <LOG "+" N <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH>>)>
           <LOG " " <WEAPON-NAME <GETP ,EQUIPPED-WEAPON ,P?R-ITID>> "." CR>
           <RTRUE>)>
    <RFALSE>>

<ROUTINE PRINT-EQUIPPED-WEAPON ()
    <COND (<AND ,EQUIPPED-WEAPON
                <==? <GETP ,EQUIPPED-WEAPON ,P?R-ITKIND> ,ITEMKIND-WEAPON>
                <G? <GETP ,EQUIPPED-WEAPON ,P?R-ITLVL> 0>
                <G? <GETP ,EQUIPPED-WEAPON ,P?R-ITID> 0>>
           <TELL "L" N <GETP ,EQUIPPED-WEAPON ,P?R-ITLVL>>
           <COND (<G? <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH> 0>
                  <TELL "+" N <GETP ,EQUIPPED-WEAPON ,P?R-ITENCH>>)>
           <TELL " " <WEAPON-NAME <GETP ,EQUIPPED-WEAPON ,P?R-ITID>>>)
          (ELSE <TELL "fists">)>>

"Pickup operations"

;"If the player is standing on a gold pile, picks it up and shows a message.

Args:
  (none)

Returns:
  T if picked up; FALSE otherwise."

<ROUTINE TRY-PICKUP-GOLD ("AUX" O TOTAL)
    <SET O <GOLD-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .O 0> <RFALSE>)>
    <SET TOTAL <GETP .O ,P?R-ITAMT>>
    <REMOVE .O>
    <FREE-RASCAL-ITEM .O>
  <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
    <SETG PLAYER-GOLD <+ ,PLAYER-GOLD .TOTAL>>
    <LOG "You pick up " N .TOTAL " gold pieces." CR>
    <RTRUE>>

;"If the player is standing on food, picks it up into inventory.

Args:
  (none)

Returns:
  T if any food was picked up (or pack-full message was shown); FALSE otherwise."

<ROUTINE TRY-PICKUP-FOOD ("AUX" TYPE ANY)
    <SET ANY <FOOD-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .ANY 0> <RFALSE>)>
    <SET TYPE <GETP .ANY ,P?R-ITID>>
    <COND (<INV-ADD ,ITEMKIND-FOOD .TYPE>
           <LOG "You pick up the " <FOOD-NAME .TYPE> "." CR>
           <REMOVE .ANY>
           <FREE-RASCAL-ITEM .ANY>
          <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <RTRUE>)
          (ELSE
           <SETG STATS-PACKFULL-PICKUP-BLOCKED
               <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
           <LOG "Your pack is too full to pick up the "
                <FOOD-NAME .TYPE>
                "."
                CR>
           <RTRUE>)>>

;"If the player is standing on a potion, attempts to pick it up into inventory.

Args:
  (none)

Returns:
  T if handled (picked up or pack-full message); FALSE otherwise."

<ROUTINE TRY-PICKUP-POTION ("AUX" O COLOR)
    <SET O <POTION-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .O 0> <RFALSE>)>
    <SET COLOR <GETP .O ,P?R-ITID>>
    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>> <RFALSE>)>
    <COND (<INV-ADD ,ITEMKIND-POTION .COLOR>
           <COND (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>
                  <LOG "You pick up a " <POTION-DISPLAY-NAME .COLOR> "." CR>)
                 (ELSE
                  <LOG "You pick up "
                       <POTION-ARTICLE .COLOR>
                       " "
                       <POTION-DISPLAY-NAME .COLOR>
                       "."
                       CR>)>
           <REMOVE .O>
           <FREE-RASCAL-ITEM .O>
          <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <RTRUE>)
          (ELSE
           <SETG STATS-PACKFULL-PICKUP-BLOCKED
               <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
           <LOG "Your pack is too full to pick up the "
                <POTION-DISPLAY-NAME .COLOR>
                "."
                CR>
           <RTRUE>)>>

  <ROUTINE TRY-PICKUP-KEY ("AUX" O LOCKTYPE)
      <SET O <KEY-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
      <COND (<L=? .O 0> <RFALSE>)>
      <SET LOCKTYPE <GETP .O ,P?R-ITID>>
      <COND (<INV-ADD ,ITEMKIND-KEY .LOCKTYPE>
             <LOG "You pick up the " <KEY-NAME .LOCKTYPE> "." CR>
             <SETG STATS-KEYS-FOUND <+ ,STATS-KEYS-FOUND 1>>
             <REMOVE .O>
             <FREE-RASCAL-ITEM .O>
              <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
             <RTRUE>)
            (ELSE
             <SETG STATS-PACKFULL-PICKUP-BLOCKED
                 <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
             <LOG "Your pack is too full to pick up the "
                  <KEY-NAME .LOCKTYPE>
                  "."
                  CR>
             <RTRUE>)>>

;"If the player is standing on a grounded weapon, attempts to pick it up into
  inventory.

Args:
  (none)

Returns:
  T if handled (picked up or pack-full message); FALSE if no weapon."

<ROUTINE TRY-PICKUP-WEAPON ("AUX" SLOT TYPE LVL ENCH)
    <SET SLOT <WEAPON-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .SLOT 0> <RFALSE>)>
    <SET TYPE <GETP .SLOT ,P?R-ITID>>
    <SET LVL <GETP .SLOT ,P?R-ITLVL>>
    <SET ENCH <GETP .SLOT ,P?R-ITENCH>>
    <COND (<INV-TAKE-OBJ .SLOT>
           <LOG "You pick up a level " N .LVL>
           <COND (<G? .ENCH 0> <LOG "+" N .ENCH>)>
           <LOG " " <WEAPON-NAME .TYPE> "." CR>
           <COND (<AND <NOT ,EQUIPPED-WEAPON> <G=? <+ ,PLAYER-STR .ENCH> .LVL>>
                  <SETG EQUIPPED-WEAPON .SLOT>
                  <LOG "You wield it." CR>)>
          <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <RTRUE>)
          (ELSE
           <SETG STATS-PACKFULL-PICKUP-BLOCKED
               <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
           <LOG "Your pack is too full to pick up the level " N .LVL>
           <COND (<G? .ENCH 0> <LOG "+" N .ENCH>)>
           <LOG " " <WEAPON-NAME .TYPE> "." CR>
           <RTRUE>)>
    <RTRUE>>

;"If the player is standing on a grounded treasure, attempts to pick it up
  into inventory.

Args:
  (none)

Returns:
  T if handled (picked up or pack-full message); FALSE if no treasure."

<ROUTINE TRY-PICKUP-TREASURE ("AUX" O ID)
    <SET O <TREASURE-OBJ-AT ,PLAYER-X ,PLAYER-Y>>
    <COND (<L=? .O 0> <RFALSE>)>
    <SET ID <GETP .O ,P?R-ITID>>
    <COND (<INV-TAKE-OBJ .O>
           <STATS-INC-WORD-TABLE ,STATS-TREASURES-PICKED <- .ID 1>>
           <LOG "You pick up the " <TREASURE-NAME .ID> "." CR>
          <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <RTRUE>)
          (ELSE
           <SETG STATS-PACKFULL-PICKUP-BLOCKED
               <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
           <LOG "Your pack is too full to pick up the "
                <TREASURE-NAME .ID>
                "."
                CR>
           <RTRUE>)>
    <RTRUE>>

"Consumables"

"Potion drinking (shared by dungeon + interiors)"

<CONSTANT POTIONFX-FL-LOG 1>
<CONSTANT POTIONFX-FL-INTERIOR 2>

"Set when a motion potion is drunk inside a parser interior, so we can teleport after the interior exits back to the roguelike."
<GLOBAL PENDING-INTERIOR-TELEPORT? <>>

<CONSTANT POTION-TIMER-WARNING-TURNS 10>

<ROUTINE APPLY-POTION-EFFECT (TYPE FLAGS "AUX" NEWHP LOG?)
    <SET LOG? <ANDB .FLAGS ,POTIONFX-FL-LOG>>

    <COND (<AND <G? .TYPE 0> <L=? .TYPE ,POTION-TYPE-COUNT>>
           <STATS-INC-WORD-TABLE ,STATS-POTIONS-DRANK <- .TYPE 1>>)>

    <COND (<==? .TYPE ,POTION-MUSCLE>
           <SETG PLAYER-STR <+ ,PLAYER-STR 1>>
           <TELL/LOG .LOG? "Your strength is now " N ,PLAYER-STR "." CR>)
          (<==? .TYPE ,POTION-METTLE>
           <SETG PLAYER-DEF <+ ,PLAYER-DEF 1>>
           <TELL/LOG .LOG? "Your defense is now " N ,PLAYER-DEF "." CR>)
          (<==? .TYPE ,POTION-HEALTH>
           <SETG PLAYER-MAX-HP <+ ,PLAYER-MAX-HP 2>>
           <SET NEWHP <+ ,PLAYER-HP </ <+ ,PLAYER-MAX-HP 1> 2>>>
           <COND (<G? .NEWHP ,PLAYER-MAX-HP> <SET NEWHP ,PLAYER-MAX-HP>)>
           <SETG PLAYER-HP .NEWHP>
           <TELL/LOG .LOG? "Your maximum HP is now " N ,PLAYER-MAX-HP "." CR>)
          (<==? .TYPE ,POTION-HIDING>
           <SETG PLAYER-INVIS-TURNS ,HIDING-POTION-DURATION>
           <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
           <TELL/LOG .LOG? "You fade from sight." CR>)
          (<==? .TYPE ,POTION-POISON>
           <SETG PLAYER-HP <- ,PLAYER-HP 3>>
           <TELL/LOG .LOG? "You feel sick." CR>
           <CHECK-END>)
          (<==? .TYPE ,POTION-VISION>
           <SETG PLAYER-VISION-TURNS ,VISION-POTION-DURATION>
           <MARK-ENEMY-TILES>
           <TELL/LOG .LOG? "Your vision sharpens." CR>)
          (<==? .TYPE ,POTION-MOTION>
           <TELL/LOG .LOG? "The world lurches." CR>
           <COND (<ANDB .FLAGS ,POTIONFX-FL-INTERIOR>
                  <SETG PENDING-INTERIOR-TELEPORT? T>)
                 (ELSE <TELEPORT-PLAYER>)>)
          (<==? .TYPE ,POTION-SHADOW>
           <SETG PLAYER-SHADOW-TURNS ,SHADOW-POTION-DURATION>
           <MARK-ALL-DIRTY>
           <TELL/LOG .LOG? "A heavy darkness settles over your eyes." CR>)
          (ELSE <TELL/LOG .LOG? "Nothing seems to happen." CR>)>
    <RTRUE>>

<ROUTINE TICK-POTION-TIMERS ("AUX" SHOLD RID)
    ;"potion of hiding"
    <COND (<G? ,PLAYER-INVIS-TURNS 0>
           <SETG PLAYER-INVIS-TURNS <- ,PLAYER-INVIS-TURNS 1>>
           <COND (<==? ,PLAYER-INVIS-TURNS ,POTION-TIMER-WARNING-TURNS>
                  <LOG "You begin to reappear." CR>)
           (<==? ,PLAYER-INVIS-TURNS 0>
            <MARK-DIRTY ,PLAYER-X ,PLAYER-Y>
            <LOG "You are fully visible again." CR>)>)>
    ;"potion of vision"
    <COND (<G? ,PLAYER-VISION-TURNS 0>
           <SETG PLAYER-VISION-TURNS <- ,PLAYER-VISION-TURNS 1>>
           <COND (<==? ,PLAYER-VISION-TURNS ,POTION-TIMER-WARNING-TURNS>
                  <LOG "Your vision begins to dull." CR>)
           (<==? ,PLAYER-VISION-TURNS 0>
            <MARK-ALL-DIRTY>
            <LOG "Your vision returns to normal." CR>)>)>
    ;"potion of shadow"
    <SET SHOLD ,PLAYER-SHADOW-TURNS>
    <COND (<G? ,PLAYER-SHADOW-TURNS 0>
           <SETG PLAYER-SHADOW-TURNS <- ,PLAYER-SHADOW-TURNS 1>>
           <COND (<==? ,PLAYER-SHADOW-TURNS ,POTION-TIMER-WARNING-TURNS>
                  <LOG "The darkness begins to lift." CR>)
           (<==? ,PLAYER-SHADOW-TURNS 0>
            <MARK-ALL-DIRTY>
            <LOG "You can see your surroundings again." CR>)>)>
    <COND (<AND <G? .SHOLD 0> <L=? ,PLAYER-SHADOW-TURNS 0>>
           <SET RID <ROOMID-AT ,PLAYER-X ,PLAYER-Y>>
           <COND (<G? .RID 0> <SETG CURRENT-ROOM .RID> <REVEAL-ROOM .RID>)>)>>

;"Drinks a potion given by color code, applying its effect and handling discovery.

FLAGS bitmask:
    POTIONFX-FL-LOG: write to the lower window via TELL/LOG
    POTIONFX-FL-INTERIOR: special-case motion teleport for interiors

Returns:
    T if this drink newly discovers the potion color, else FALSE."
<ROUTINE DRINK-POTION-COLOR (COLOR FLAGS "AUX" DISC TYPE LOG?)
    <SET LOG? <ANDB .FLAGS ,POTIONFX-FL-LOG>>

    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>>
           <SET DISC T>
           <SET TYPE 0>)
          (ELSE
           <SET DISC <G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>>
           <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>)>

    <TELL/LOG .LOG?
              "You drink the "
              <COND (<AND <G? .COLOR 0> <L=? .COLOR ,POTION-COLOR-COUNT>>
                     <POTION-DISPLAY-NAME .COLOR>)
                    (ELSE "potion")>
              "."
              CR>

    <APPLY-POTION-EFFECT .TYPE .FLAGS>

    <COND (<AND <NOT .DISC> <G? .COLOR 0> <L=? .COLOR ,POTION-COLOR-COUNT>>
           <PUTB ,POTION-DISCOVERED <- .COLOR 1> 1>
           <TELL/LOG .LOG?
                     "You discover it was a "
                     <POTION-TYPE-NAME .TYPE>
                     "."
                     CR>
           <RTRUE>)
          (ELSE <RFALSE>)>>

;"Prompts for an inventory slot and drops that item onto a nearby open tile.

Args:
  (none)

Returns:
  T if an item was dropped; FALSE if cancelled/blocked."

<ROUTINE TRY-DROP-INVENTORY ("AUX" C SLOT K ID LVL ENCH NX NY WASE O)
    <COND (<L=? <INV-COUNT> 0> <LOG "You have nothing to drop." CR> <RFALSE>)>
    <SET C <POPUP-INVENTORY-GETCHAR 0>>
    <COND (<==? .C !\Q !\q> <LOG "Never mind." CR> <RFALSE>)>
    <SET SLOT <DIGIT-TO-SLOT .C>>
    <COND (<OR <L? .SLOT 1> <G? .SLOT <INV-COUNT>>>
           <LOG "Never mind." CR>
           <RFALSE>)>
    <SET O <INV-NTH-OBJ .SLOT>>
    <COND (<L=? .O 0> <RFALSE>)>
    <SET K <GETP .O ,P?R-ITKIND>>
    <SET ID <GETP .O ,P?R-ITID>>
    <SET LVL <GETP .O ,P?R-ITLVL>>
    <SET ENCH <GETP .O ,P?R-ITENCH>>
    <SET WASE <==? ,EQUIPPED-WEAPON .O>>
    <COND (<NOT <FIND-ADJACENT-DROP-TILE ,PLAYER-X
                                         ,PLAYER-Y
                                         ,DROPMODE-INVENTORY>>
           <LOG ,NO-ROOM-TO-DROP-THAT CR>
           <RFALSE>)>
    <SET NX ,DROP-CAND-X>
    <SET NY ,DROP-CAND-Y>
    <COND (<==? .K ,ITEMKIND-TREASURE>
           <PUTP .O ,P?R-X .NX>
           <PUTP .O ,P?R-Y .NY>
           <MOVE .O <FLOOR-OBJ ,CURRENT-FLOOR>>
           <MARK-DIRTY .NX .NY>
           <LOG "You drop the " <TREASURE-NAME .ID> "." CR>
           <RTRUE>)
          (<==? .K ,ITEMKIND-WEAPON>
           <COND (<ADD-WEAPON-PILE .NX .NY .ID .LVL .ENCH>
                  <LOG "You drop the level " N .LVL>
                  <COND (<G? .ENCH 0> <LOG "+" N .ENCH>)>
                  <LOG " " <WEAPON-NAME .ID> "." CR>
                  <INV-REMOVE .SLOT>
                  <COND (.WASE
                         <COND (<AUTO-EQUIP-WEAPON>)
                               (ELSE <LOG "You are now unarmed." CR>)>)>
                  <RTRUE>)
                 (ELSE
                  <LOG ,NO-ROOM-TO-DROP-THAT CR>
                  <RFALSE>)>)
          (<==? .K ,ITEMKIND-POTION>
           <COND (<ADD-POTION-PILE .NX .NY .ID>
                  <LOG "You drop the " <POTION-DISPLAY-NAME .ID> "." CR>
                  <INV-REMOVE .SLOT>
                  <RTRUE>)
                 (ELSE
                  <LOG ,NO-ROOM-TO-DROP-THAT CR>
                  <RFALSE>)>)
          (<==? .K ,ITEMKIND-FOOD>
           <COND (<ADD-FOOD-PILE .NX .NY .ID>
                  <LOG "You drop the " <FOOD-NAME .ID> "." CR>
                  <INV-REMOVE .SLOT>
                  <RTRUE>)
                 (ELSE
                  <LOG ,NO-ROOM-TO-DROP-THAT CR>
                  <RFALSE>)>)
          (<==? .K ,ITEMKIND-KEY>
           <COND (<ADD-KEY-PILE .NX .NY .ID>
                  <LOG "You drop the " <KEY-NAME .ID> "." CR>
                  <INV-REMOVE .SLOT>
                  <RTRUE>)
                 (ELSE
                  <LOG ,NO-ROOM-TO-DROP-THAT CR>
                  <RFALSE>)>)
          (ELSE <RFALSE>)>
    <RTRUE>>

<CONSTANT NO-ROOM-TO-DROP-THAT "There's no room to drop that.">

;"Prompts for an inventory slot and ingests that item if it is consumable.

Args:
  (none)

Returns:
    T if something was consumed; FALSE otherwise."

<ROUTINE TRY-INGEST-INVENTORY ("AUX" C SLOT K ID HEAL NEWHP)
    <COND (<L=? <INV-COUNT> 0> <LOG "You have nothing to ingest." CR> <RFALSE>)>
    <SET C <POPUP-INVENTORY-GETCHAR 2>>
    <COND (<==? .C !\Q !\q> <LOG "Never mind." CR> <RFALSE>)>
    <SET SLOT <DIGIT-TO-SLOT .C>>
    <COND (<OR <L? .SLOT 1> <G? .SLOT <INV-COUNT>>>
           <LOG "Never mind." CR>
           <RFALSE>)>
    <SET K <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITKIND>>
    <SET ID <GETP <INV-NTH-OBJ .SLOT> ,P?R-ITID>>
    <COND (<==? .K ,ITEMKIND-FOOD>
           <SET HEAL <FOOD-HEAL-AMT .ID>>
           <SET NEWHP <+ ,PLAYER-HP .HEAL>>
           <COND (<G? .NEWHP ,PLAYER-MAX-HP> <SET NEWHP ,PLAYER-MAX-HP>)>
           <SETG PLAYER-HP .NEWHP>
           <COND (<AND <G? .ID 0> <L=? .ID ,FOOD-TYPE-COUNT>>
                  <STATS-INC-WORD-TABLE ,STATS-FOODS-EATEN <- .ID 1>>)>
           <LOG "You eat the "
                <FOOD-NAME .ID>
                " and recover "
                N
                .HEAL
                " HP."
                CR>
           <INV-REMOVE .SLOT>
           <RTRUE>)
          (<==? .K ,ITEMKIND-POTION>
           <DRINK-POTION-COLOR .ID ,POTIONFX-FL-LOG>
           <INV-REMOVE .SLOT>
           <RTRUE>)
          (ELSE
           <LOG "That doesn't look ingestible." CR>
           <RFALSE>)>>
