"Trader inventory management"

<GLOBAL TRADER-X 0>
<GLOBAL TRADER-Y 0>
<GLOBAL TRADER-ON? <>>

;"Trader inventory is stored as child item objects under the per-floor trader
    object (see objects.zil). Slot contents live in the TRINV-SLOTS table."

<ROUTINE TRINV-NTH-OBJ-FLOOR (F SLOT)
    <COND (<OR <L? .SLOT 1> <G? .SLOT ,TRINV-SIZE>> <RETURN 0>)>
    <GET ,TRINV-SLOTS <TRINV-SLOT-IDX .F .SLOT>>>

<ROUTINE TRINV-SET-OBJ-FLOOR (F SLOT O)
    <COND (<OR <L? .SLOT 1> <G? .SLOT ,TRINV-SIZE>> <RETURN 0>)>
    <PUT ,TRINV-SLOTS <TRINV-SLOT-IDX .F .SLOT> .O>
    .O>

<ROUTINE TRINV-COUNT-FLOOR (F "AUX" O C)
    <SET C 0>
    <DO (I 1 ,TRINV-SIZE)
        <SET O <TRINV-NTH-OBJ-FLOOR .F .I>>
        <COND (.O <SET C <+ .C 1>>)>
    .C>>

<ROUTINE TRINV-COUNT ()
    <TRINV-COUNT-FLOOR ,CURRENT-FLOOR>>

<ROUTINE TRINV-FIRST-FREE-SLOT-FLOOR (F)
    <DO (I 1 ,TRINV-SIZE)
        <COND (<NOT <TRINV-NTH-OBJ-FLOOR .F .I>> <RETURN .I>)>>
    0>

<ROUTINE TRINV-FIRST-FREE-SLOT ()
    <TRINV-FIRST-FREE-SLOT-FLOOR ,CURRENT-FLOOR>>

<ROUTINE TRINV-NTH-OBJ (SLOT)
    <TRINV-NTH-OBJ-FLOOR ,CURRENT-FLOOR .SLOT>>

;"Computes the trader buy price for an item (what the trader pays the player).

Args:
  KIND: Item kind code (ITEMKIND-*).
  ID: Item ID within that kind.
    LVL: Weapon level for weapons; else 0.
    ENCH: Weapon enchantment for weapons; else 0.

Returns:
  Gold value as a positive integer."

<ROUTINE TRADER-BUY-PRICE (KIND ID LVL ENCH)
    <COND (<==? .KIND ,ITEMKIND-TREASURE> <TREASURE-VALUE .ID>)
          (<==? .KIND ,ITEMKIND-FOOD> <FOOD-VALUE .ID>)
          (<==? .KIND ,ITEMKIND-POTION> ,POTION-VALUE)
          (<==? .KIND ,ITEMKIND-WEAPON> <WEAPON-VALUE .ID .LVL .ENCH>)
      (<==? .KIND ,ITEMKIND-KEY> 150)
          (ELSE 1)>>

;"Computes the trader sell price for an item (what the player pays the trader).

The trader sells for 2x what he buys for.

Args:
  KIND: Item kind code (ITEMKIND-*).
  ID: Item ID within that kind.
  LVL: Weapon level for weapons; else 0.
  ENCH: Weapon enchantment for weapons; else 0.

Returns:
  Gold value as a positive integer."

<ROUTINE TRADER-SELL-PRICE (KIND ID LVL ENCH)
    <* 2 <TRADER-BUY-PRICE .KIND .ID .LVL .ENCH>>>

<ROUTINE TRINV-ADD-FLOOR (F KIND ID LVL ENCH "AUX" O SLOT)
    <SET SLOT <TRINV-FIRST-FREE-SLOT-FLOOR .F>>
    <COND (<L=? .SLOT 0> <RETURN 0>)>
    <SET O <ALLOC-RASCAL-ITEM>>
    <COND (<NOT .O> <RETURN 0>)>
    <PUTP .O ,P?R-ITKIND .KIND>
    <PUTP .O ,P?R-ITID .ID>
    <PUTP .O ,P?R-ITLVL .LVL>
    <PUTP .O ,P?R-ITENCH .ENCH>
    <PUTP .O ,P?R-ITAMT 0>
    <PUTP .O ,P?R-X 0>
    <PUTP .O ,P?R-Y 0>
    <MOVE .O <TRADER-OBJ .F>>
    <TRINV-SET-OBJ-FLOOR .F .SLOT .O>
    .O>

;"Prints a trader inventory slot as a display name.

Args:
  SLOT: 1-based slot number.

Returns:
    T."

<ROUTINE PRINT-TRINV-NAME (SLOT "AUX" O K ID)
    <COND (<NOT <SET O <TRINV-NTH-OBJ .SLOT>>> <RTRUE>)>
    <SET K <GETP .O ,P?R-ITKIND>>
    <SET ID <GETP .O ,P?R-ITID>>
    <COND (<==? .K ,ITEMKIND-FOOD> <TELL FOOD-NAME .ID>)
          (<==? .K ,ITEMKIND-TREASURE> <TELL TREASURE-NAME .ID>)
          (<==? .K ,ITEMKIND-WEAPON> <TELL WEAPON-NAME .ID>)
          (<==? .K ,ITEMKIND-POTION> <TELL POTION-DISPLAY-NAME .ID>)
          (<==? .K ,ITEMKIND-KEY> <TELL KEY-NAME .ID>)
          (ELSE <TELL "item">)>
    <RTRUE>>

<ADD-TELL-TOKENS TRINV-NAME * <PRINT-TRINV-NAME .X>>

"Trader shop UI + interaction"

;"Draws the trader shop UI in the upper window.

Two columns: trader inventory on the left (buy), player inventory on the right
(sell). Prices shown are what you pay (left) or what you get (right).

Returns:
  T."

<ROUTINE DRAW-TRADER-SHOP ("AUX" ROW K ID LVL ENCH PRICE O)
    <SCREEN 1>
    <CLEAR 1>
    <DRAW-STATUS-LINE 1 1 1>
    <CURSET 3 1>
    <TELL "Trader (B then 1-0 to buy)">
    <CURSET 3 42>
    <TELL "You (S then 1-0 to sell)">
    <CURSET 4 1>
    <TELL "-------------------------------">
    <CURSET 4 42>
    <TELL "-------------------------------">
    <DO (I 1 ,TRINV-SIZE)
        <SET ROW <+ 4 .I>>
        <CURSET .ROW 1>
        <COND (<SET O <TRINV-NTH-OBJ .I>>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <SET LVL <GETP .O ,P?R-ITLVL>>
               <SET ENCH <GETP .O ,P?R-ITENCH>>
               <SET PRICE <TRADER-SELL-PRICE .K .ID .LVL .ENCH>>
               <TELL N .I ") ">
               <COND (<==? .K ,ITEMKIND-WEAPON>
                      <TELL "L" N .LVL>
                      <COND (<G? .ENCH 0> <TELL "+" N .ENCH>)>
                      <TELL " ">)>
               <TELL TRINV-NAME .I>
               <CURSET .ROW 22>
               <TELL N .PRICE " gold">)>
        <CURSET .ROW 42>
        <COND (<SET O <INV-NTH-OBJ .I>>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <SET LVL <GETP .O ,P?R-ITLVL>>
               <SET ENCH <GETP .O ,P?R-ITENCH>>
               <SET PRICE <TRADER-BUY-PRICE .K .ID .LVL .ENCH>>
               <TELL N .I ") ">
               <COND (<==? .K ,ITEMKIND-WEAPON>
                      <TELL "L" N .LVL>
                      <COND (<G? .ENCH 0> <TELL "+" N .ENCH>)>
                      <TELL " ">)>
               <TELL INV-NAME .I>
               <CURSET .ROW <+ 42 22>>
               <TELL N .PRICE " gold">)>>
    <CURSET <+ 6 ,TRINV-SIZE> 1>
    <TELL "Q exits">
    <RTRUE>>

;"Runs the interactive trader shop.

Stepping onto the trader's tile should invoke this.

Returns:
  T."

<ROUTINE TRADER-SHOP ("AUX" C C2 C3 SLOT K ID LVL ENCH PRICE O)
    <COND (<NOT <TRADER-AT? ,PLAYER-X ,PLAYER-Y>> <RFALSE>)>
    <SCREEN 1>
    <REPEAT ()
        <DRAW-TRADER-SHOP>
        <SET C <GETCHAR>>
        <COND (<==? .C !\Q !\q>
               <CLEAR 1>
               <SETG FULL-REDRAW? T>
               <DRAW>
               <COND (<NOT ,EQUIPPED-WEAPON> <AUTO-EQUIP-WEAPON>)>
               <RTRUE>)>
        <COND (<==? .C !\B !\b>
               <COND (<0? <TRINV-COUNT>> <LOG "Nothing to buy." CR> <AGAIN>)>
             <LOG "Buy which item? (1-" N ,TRINV-SIZE "; 0=10; Q cancels)" CR>
               <SET C2 <GETCHAR>>
               <COND (<==? .C2 !\Q !\q> <LOG "Never mind." CR> <AGAIN>)>
               <SET SLOT <DIGIT-TO-SLOT .C2>>
             <COND (<OR <L? .SLOT 1> <G? .SLOT ,TRINV-SIZE>>
                      <LOG "No such item." CR>
                      <AGAIN>)>
               <SET O <TRINV-NTH-OBJ .SLOT>>
               <COND (<NOT .O> <LOG "No such item." CR> <AGAIN>)>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <SET LVL <GETP .O ,P?R-ITLVL>>
               <SET ENCH <GETP .O ,P?R-ITENCH>>
               <SET PRICE <TRADER-SELL-PRICE .K .ID .LVL .ENCH>>
               <COND (<L? ,PLAYER-GOLD .PRICE>
                      <LOG "You can't afford that." CR>
                      <AGAIN>)>
               <COND (<G=? <INV-COUNT> ,INV-SIZE>
                      <SETG STATS-PACKFULL-PICKUP-BLOCKED
                          <+ ,STATS-PACKFULL-PICKUP-BLOCKED 1>>
                      <LOG "Your pack is full." CR>
                      <AGAIN>)>
               <COND (<NOT <INV-TAKE-OBJ .O>>
                      <LOG "Your pack is full." CR>
                      <AGAIN>)>
               <COND (<==? .K ,ITEMKIND-TREASURE>
                      <STATS-INC-WORD-TABLE ,STATS-TREASURES-PICKED <- .ID 1>>)>
             <HONORS-NOTE-TROPHY-PURCHASE .K .ID>
               <SETG PLAYER-GOLD <- ,PLAYER-GOLD .PRICE>>
               <SETG STATS-GOLD-SPENT-TRADER
                   <+ ,STATS-GOLD-SPENT-TRADER .PRICE>>
               <LOG "You buy the " ITEM-NAME .O "." CR>
               <AGAIN>)>
        <COND (<==? .C !\S !\s>
             <COND (<L=? <INV-COUNT> 0> <LOG "Nothing to sell." CR> <AGAIN>)>
             <LOG "Sell which item? (1-" N ,INV-SIZE "; 0=10; Q cancels)" CR>
               <SET C2 <GETCHAR>>
               <COND (<==? .C2 !\Q !\q> <LOG "Never mind." CR> <AGAIN>)>
               <SET SLOT <DIGIT-TO-SLOT .C2>>
             <COND (<OR <L? .SLOT 1> <G? .SLOT ,INV-SIZE>>
                      <LOG "No such item." CR>
                      <AGAIN>)>
               <COND (<G=? <TRINV-COUNT> ,TRINV-SIZE>
                      <LOG "The trader has no room for that." CR>
                      <AGAIN>)>
               <SET O <INV-NTH-OBJ .SLOT>>
               <COND (<NOT .O> <LOG "No such item." CR> <AGAIN>)>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <SET LVL <GETP .O ,P?R-ITLVL>>
               <SET ENCH <GETP .O ,P?R-ITENCH>>
               <SET PRICE <TRADER-BUY-PRICE .K .ID .LVL .ENCH>>
               <COND (<AND <==? .K ,ITEMKIND-TREASURE>
                           <==? .ID ,TREASURE-TROPHY>>
                      <SET C3 <POPUP-TROPHY-SELL-CONFIRM-GETCHAR>>
                      <COND (<==? .C3 !\N !\n>
                             <LOG "Never mind." CR>
                             <AGAIN>)>)>
               <COND (<==? ,EQUIPPED-WEAPON .O> <SETG EQUIPPED-WEAPON <>>)>
               <HONORS-NOTE-EQUIP-CHANGE>
               <INV-CLEAR-SLOT .SLOT>
               <REMOVE .O>
               <PUTP .O ,P?R-X 0>
               <PUTP .O ,P?R-Y 0>
               <MOVE .O <TRADER-OBJ ,CURRENT-FLOOR>>
               <TRINV-SET-OBJ-FLOOR ,CURRENT-FLOOR <TRINV-FIRST-FREE-SLOT> .O>
               <SETG PLAYER-GOLD <+ ,PLAYER-GOLD .PRICE>>
               <SETG STATS-GOLD-EARNED-TRADER
                   <+ ,STATS-GOLD-EARNED-TRADER .PRICE>>
               <LOG "The trader buys your item for " N .PRICE " gold." CR>
               <AGAIN>)>
        <AGAIN>>>

<ROUTINE TRADER-ROLL-WEAPON-LVL (F "AUX" LVL)
    <SET LVL <+ <ROLL-LOOT-WEAPON-LEVEL .F> 2>>
    <COND (<G? .LVL 15> <SET LVL 15>)>
    .LVL>

<ROUTINE TRADER-PICK-POTION-COLOR ("AUX" TYPE COLOR)
    <SET TYPE <RNG ,POTION-TYPE-COUNT>>
    <SET COLOR <GETB ,POTION-COLOR-FOR-TYPE <- .TYPE 1>>>
    <COND (<L? .COLOR 1> <SET COLOR <RNG ,POTION-COLOR-COUNT>>)>
    .COLOR>

<ROUTINE TRADER-ADD-RANDOM-STOCK (F "AUX" CHOICE TYPE LVL COLOR ENCH)
    <SET CHOICE <RNG 3>>
    <COND (<==? .CHOICE 1> <TRINV-ADD-FLOOR .F ,ITEMKIND-FOOD <RNG 5> 0 0>)
          (<==? .CHOICE 2>
           <SET TYPE <RNG ,WEAPON-COUNT>>
           <SET LVL <ROLL-LOOT-WEAPON-LEVEL .F>>
           <SET ENCH <ROLL-LOOT-WEAPON-ENCH>>
           <TRINV-ADD-FLOOR .F ,ITEMKIND-WEAPON .TYPE .LVL .ENCH>)
          (ELSE
           <SET COLOR <TRADER-PICK-POTION-COLOR>>
           <TRINV-ADD-FLOOR .F ,ITEMKIND-POTION .COLOR 0 0>)>
    <RTRUE>>

<ROUTINE INIT-TRADER-STOCK (F "AUX" TYPE LVL COLOR EXTRA ENCH)
    <FREE-RASCAL-ITEM-CHILDREN <TRADER-OBJ .F> 0>
    ;"1 random food item."
    <TRINV-ADD-FLOOR .F ,ITEMKIND-FOOD <RNG 5> 0 0>
    ;"1 random weapon, better than typical floor loot."
    <SET TYPE <RNG ,WEAPON-COUNT>>
    <SET LVL <TRADER-ROLL-WEAPON-LVL .F>>
    <SET ENCH <ROLL-LOOT-WEAPON-ENCH>>
    <TRINV-ADD-FLOOR .F ,ITEMKIND-WEAPON .TYPE .LVL .ENCH>
    ;"1 random potion."
    <SET COLOR <TRADER-PICK-POTION-COLOR>>
    <TRINV-ADD-FLOOR .F ,ITEMKIND-POTION .COLOR 0 0>
    ;"Sometimes a key, always priced at 300 gold."
    <COND (<==? <RNG 4> 1>
           <TRINV-ADD-FLOOR .F ,ITEMKIND-KEY <RNG ,LOCK-TYPE-COUNT> 0 0>)>
    ;"1-2 other random items."
    <SET EXTRA <RNG 2>>
    <DO (I 1 .EXTRA)
        <TRADER-ADD-RANDOM-STOCK .F>>
    <RTRUE>>
