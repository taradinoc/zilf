"Run statistics (for the end-of-game stats screen)"

<GLOBAL STATS-TURNS 0>
<GLOBAL STATS-MOVES 0>
<GLOBAL STATS-DIAG-MOVES 0>
<GLOBAL STATS-WAITS 0>
<GLOBAL STATS-STAIRS-UP 0>
<GLOBAL STATS-STAIRS-DOWN 0>
<GLOBAL STATS-MAX-FLOOR-REACHED 1>

<GLOBAL STATS-PLAYER-ATTACKS 0>
<GLOBAL STATS-PLAYER-CRITS 0>
<GLOBAL STATS-ENEMY-ATTACKS 0>

<GLOBAL STATS-DMG-DEALT 0>
<GLOBAL STATS-DMG-TAKEN 0>
<GLOBAL STATS-DMG-BLOCKED-BY-DEF 0>

<GLOBAL STATS-GOLD-SPENT-TRADER 0>
<GLOBAL STATS-GOLD-EARNED-TRADER 0>
<GLOBAL STATS-GOLD-SPENT-BLACKSMITH 0>
<GLOBAL STATS-GOLD-SPENT-CARROT-FARM 0>
<GLOBAL STATS-GOLD-EARNED-BUSKER 0>

<GLOBAL STATS-ROOMS-DISCOVERED-TOTAL 0>

<GLOBAL STATS-KEYS-FOUND 0>
<GLOBAL STATS-DOORS-OPENED 0>

<GLOBAL STATS-WALL-BUMPS 0>
<GLOBAL STATS-PACKFULL-PICKUP-BLOCKED 0>

<GLOBAL STATS-BIGGEST-HIT-DEALT 0>
<GLOBAL STATS-BIGGEST-HIT-TAKEN 0>

<GLOBAL STATS-POTIONS-PLACED 0>
<GLOBAL STATS-POTIONS-FOUND 0>

<GLOBAL STATS-POTIONS-DRANK <ITABLE ,POTION-TYPE-COUNT (WORD) 0>>

<GLOBAL STATS-FOODS-EATEN <ITABLE ,FOOD-TYPE-COUNT (WORD) 0>>

<GLOBAL STATS-ENEMIES-KILLED <ITABLE ,ETYPE-COUNT (WORD) 0>>

<GLOBAL STATS-TREASURES-PICKED <ITABLE ,TREASURE-COUNT (WORD) 0>>

<ROUTINE STATS-RESET ()
    <SETG STATS-TURNS 0>
    <SETG STATS-MOVES 0>
    <SETG STATS-DIAG-MOVES 0>
    <SETG STATS-WAITS 0>
    <SETG STATS-STAIRS-UP 0>
    <SETG STATS-STAIRS-DOWN 0>
    <SETG STATS-MAX-FLOOR-REACHED 1>
    <SETG STATS-PLAYER-ATTACKS 0>
    <SETG STATS-PLAYER-CRITS 0>
    <SETG STATS-ENEMY-ATTACKS 0>
    <SETG STATS-DMG-DEALT 0>
    <SETG STATS-DMG-TAKEN 0>
    <SETG STATS-DMG-BLOCKED-BY-DEF 0>
    <SETG STATS-GOLD-SPENT-TRADER 0>
    <SETG STATS-GOLD-EARNED-TRADER 0>
    <SETG STATS-GOLD-SPENT-BLACKSMITH 0>
    <SETG STATS-GOLD-SPENT-CARROT-FARM 0>
    <SETG STATS-GOLD-EARNED-BUSKER 0>
    <SETG STATS-ROOMS-DISCOVERED-TOTAL 0>
    <SETG STATS-KEYS-FOUND 0>
    <SETG STATS-DOORS-OPENED 0>
    <SETG STATS-WALL-BUMPS 0>
    <SETG STATS-PACKFULL-PICKUP-BLOCKED 0>
    <SETG STATS-BIGGEST-HIT-DEALT 0>
    <SETG STATS-BIGGEST-HIT-TAKEN 0>
    <SETG STATS-POTIONS-PLACED 0>
    <SETG STATS-POTIONS-FOUND 0>
    <DO (I 1 ,POTION-TYPE-COUNT) <PUT ,STATS-POTIONS-DRANK <- .I 1> 0>>
    <DO (I 1 ,FOOD-TYPE-COUNT) <PUT ,STATS-FOODS-EATEN <- .I 1> 0>>
    <DO (I 1 ,ETYPE-COUNT) <PUT ,STATS-ENEMIES-KILLED <- .I 1> 0>>
    <DO (I 1 ,TREASURE-COUNT) <PUT ,STATS-TREASURES-PICKED <- .I 1> 0>>
    <RTRUE>>

<ROUTINE STATS-INC-WORD-TABLE (TBL IDX "AUX" CUR)
    <SET CUR <GET .TBL .IDX>>
    <PUT .TBL .IDX <+ .CUR 1>>
    <RTRUE>>

"Scoring"

;"Bonus score per point of STR/MAXHP above defaults."
<CONSTANT SCORE-BONUS-PER-STR 100>
<CONSTANT SCORE-BONUS-PER-DEF 100>
<CONSTANT SCORE-BONUS-PER-MAXHP 25>
<CONSTANT SCORE-BONUS-WIN 5000>


<ROUTINE FINAL-SCORE ("AUX" INVVAL STRX HPX DEFX BONUS)
    <SET INVVAL <INVENTORY-SELL-VALUE>>
    <SET STRX <- ,PLAYER-STR ,INITIAL-PLAYER-STR>>
    <COND (<L? .STRX 0> <SET STRX 0>)>
    <SET HPX <- ,PLAYER-MAX-HP ,INITIAL-PLAYER-MAX-HP>>
    <COND (<L? .HPX 0> <SET HPX 0>)>
    <SET DEFX <- ,PLAYER-DEF ,INITIAL-PLAYER-DEF>>
    <COND (<L? .DEFX 0> <SET DEFX 0>)>
    <SET BONUS <+ <* .STRX ,SCORE-BONUS-PER-STR>
                  <* .HPX ,SCORE-BONUS-PER-MAXHP>
                  <* .DEFX ,SCORE-BONUS-PER-DEF>>>
    <COND (,YOU-WIN?
           <SET BONUS <+ .BONUS ,SCORE-BONUS-WIN>>)>
    <+ ,PLAYER-GOLD </ .INVVAL 2> .BONUS>>

<ROUTINE GAME-STATS ("AUX" C STATS-PAGE)
    <SET C 0>
    <SET STATS-PAGE 1>
    <REPEAT ()
        <COND (<L=? .STATS-PAGE 1> <SETG STATS-PAGE 1>)>
        <COND (<G? .STATS-PAGE 3> <SETG STATS-PAGE 3>)>
        <COND (<==? .STATS-PAGE 1> <DRAW-GAME-STATS-SUMMARY>)
              (<==? .STATS-PAGE 2> <DRAW-GAME-STATS-CONSUMABLES>)
              (ELSE <DRAW-GAME-STATS-COMBAT-LOOT>)>

        <SET C <GETCHAR>>
        <COND (<==? .C !\N !\n>
               <SETG STATS-PAGE <+ .STATS-PAGE 1>>
               <AGAIN>)
              (<==? .C !\P !\p>
               <SETG STATS-PAGE <- .STATS-PAGE 1>>
               <AGAIN>)
              (<==? .C !\Q !\q> <RETURN>)>>>

<ROUTINE GAME-STATS-CLEAR ()
    <SPLIT <LOWCORE SCRV>>
    <SCREEN 1>
    <CLEAR -2>
    <RTRUE>>

;"Calculates percentage while avoiding overflow.

Args:
  NUM: The numerator.
  DEN: The denominator.

Returns:
  NUM/DEN * 100, to the closest integer."

<ROUTINE STATS-PCT (NUM DEN "AUX" Q R QUOT ACC)
    <COND (<L=? .DEN 0> <RETURN 0>)>
    <SET Q </ .NUM .DEN>>
    <SET R <MOD .NUM .DEN>>
    <SET QUOT 0>
    <SET ACC </ .DEN 2>>
    <DO (I 1 100)
        <SET ACC <+ .ACC .R>>
        <COND (<G=? .ACC .DEN> <SET ACC <- .ACC .DEN>> <SET QUOT <+ .QUOT 1>>)>>
    <RETURN <+ <* .Q 100> .QUOT>>>

<ROUTINE COUNT-TOTAL-LOCKED-DOORS ("AUX" CNT)
    ;"Count how many treasure rooms (locked doors) were actually created."
    <SET CNT 0>
    <DO (F 1 ,MAX-FLOORS)
        <COND (<G? <GETB ,TREASURE-ROOM-LOCKTYPE <- .F 1>> 0>
               <SET CNT <+ .CNT 1>>)>>
    .CNT>

<ROUTINE RJ-NUMBER (N)
    <COND (<L? .N 10000> <PRINTC !\ >)>
    <COND (<L? .N 1000> <PRINTC !\ >)>
    <COND (<L? .N 100> <PRINTC !\ >)>
    <COND (<L? .N 10> <PRINTC !\ >)>
    <TELL N .N>>

<ROUTINE DRAW-GAME-STATS-SUMMARY ("AUX" INVVAL INVSCORE STRX HPX DEFX STRBON HPBON DEFBON TREAS-UNIQ POT-ALL FOOD-ALL KILL-ALL ;DIAGP ;POTFINDP)
    <GAME-STATS-CLEAR>
    <CURSET 1 1>
    <TELL "Statistics  page 1/3">

    <SET INVVAL <INVENTORY-SELL-VALUE>>
    <SET INVSCORE </ .INVVAL 2>>
    <SET STRX <- ,PLAYER-STR ,INITIAL-PLAYER-STR>>
    <COND (<L? .STRX 0> <SET STRX 0>)>
    <SET HPX <- ,PLAYER-MAX-HP ,INITIAL-PLAYER-MAX-HP>>
    <COND (<L? .HPX 0> <SET HPX 0>)>
    <SET DEFX <- ,PLAYER-DEF ,INITIAL-PLAYER-DEF>>
    <COND (<L? .DEFX 0> <SET DEFX 0>)>
    <SET STRBON <* .STRX ,SCORE-BONUS-PER-STR>>
    <SET HPBON <* .HPX ,SCORE-BONUS-PER-MAXHP>>
    <SET DEFBON <* .HPX ,SCORE-BONUS-PER-DEF>>

    <SET TREAS-UNIQ 0>
    <DO (I 1 ,TREASURE-COUNT)
        <COND (<G? <GET ,STATS-TREASURES-PICKED <- .I 1>> 0>
               <SET TREAS-UNIQ <+ .TREAS-UNIQ 1>>)>>

    <SET POT-ALL <SUM-WORD-TABLE ,STATS-POTIONS-DRANK ,POTION-TYPE-COUNT>>
    <SET FOOD-ALL <SUM-WORD-TABLE ,STATS-FOODS-EATEN ,FOOD-TYPE-COUNT>>
    <SET KILL-ALL <SUM-WORD-TABLE ,STATS-ENEMIES-KILLED ,ETYPE-COUNT>>
    ;<SET DIAGP <STATS-PCT ,STATS-DIAG-MOVES ,STATS-MOVES>>
    ;<SET POTFINDP <STATS-PCT ,STATS-POTIONS-FOUND ,STATS-POTIONS-PLACED>>

    <CURSET 3 1>
    <TELL "Final score: ">
    <CURSET 4 1>
    <RJ-NUMBER ,PLAYER-GOLD>
    <TELL " gold">
    <CURSET 5 1>
    <RJ-NUMBER .INVSCORE>
    <TELL " half of inventory sale value">
    <CURSET 6 1>
    <RJ-NUMBER <+ .STRBON .HPBON .DEFBON>>
    <TELL " stat upgrade value">
    <CURSET 7 1>
    <RJ-NUMBER ,SCORE-BONUS-WIN>
    <TELL " bonus for winning">
    <CURSET 8 1>
    <TELL "=====">
    <CURSET 9 1>
    <RJ-NUMBER <FINAL-SCORE>>
    <TELL " total">

    <CURSET 11 1>
    <TELL "Rooms discovered: " N ,STATS-ROOMS-DISCOVERED-TOTAL>
    ;"TODO: out of how many?"
    <CURSET 12 1>
    <TELL "Treasures acquired: " N .TREAS-UNIQ "/" N ,TREASURE-COUNT>
    <CURSET 13 1>
    <TELL "Keys found: " N ,STATS-KEYS-FOUND>
    <CURSET 14 1>
    <TELL "Locked doors opened: " N ,STATS-DOORS-OPENED "/" N <COUNT-TOTAL-LOCKED-DOORS>
          " (" N <STATS-PCT ,STATS-DOORS-OPENED <COUNT-TOTAL-LOCKED-DOORS>> "%)">
    <CURSET 15 1>
    <TELL "Potions found: " N ,STATS-POTIONS-FOUND "/" N ,STATS-POTIONS-PLACED
          "  Consumed: " N .POT-ALL>
    <CURSET 16 1>
    <TELL "   Food eaten: " N .FOOD-ALL>

    <CURSET 18 1>
    <TELL "Attacks: " N ,STATS-PLAYER-ATTACKS "  Crits: " N ,STATS-PLAYER-CRITS
          " (" N <STATS-PCT ,STATS-PLAYER-CRITS ,STATS-PLAYER-ATTACKS> "%)"
          "  Kills: " N .KILL-ALL>
    <CURSET 19 1>
    <TELL "Damage dealt: " N ,STATS-DMG-DEALT "  Taken: " N ,STATS-DMG-TAKEN
          " Dodged: " N ,STATS-DMG-BLOCKED-BY-DEF " (" N
          <STATS-PCT ,STATS-DMG-BLOCKED-BY-DEF <+ ,STATS-DMG-BLOCKED-BY-DEF ,STATS-DMG-TAKEN>>
          "%)">
    <CURSET 20 1>
    <TELL "Biggest hit dealt: " N ,STATS-BIGGEST-HIT-DEALT "  Taken: " N
          ,STATS-BIGGEST-HIT-TAKEN>

    <CURSET 5 45>
    <TELL "Gold spent at trader: " N ,STATS-GOLD-SPENT-TRADER>
    <CURSET 6 45>
    <TELL "  Earned from trader: " N ,STATS-GOLD-EARNED-TRADER>
    <CURSET 7 45>
    <TELL " Spent at blacksmith: " N ,STATS-GOLD-SPENT-BLACKSMITH>
    <CURSET 8 45>
    <TELL "Spent at carrot farm: " N ,STATS-GOLD-SPENT-CARROT-FARM>
    <CURSET 9 45>
    <TELL "  Earned from busker: " N ,STATS-GOLD-EARNED-BUSKER>

    <CURSET 11 45>
    <TELL "Turns: " N ,STATS-TURNS "  Waits: " N ,STATS-WAITS>
    <CURSET 12 45>
    <TELL "Moves: " N ,STATS-MOVES "  Diagonal: " N ,STATS-DIAG-MOVES " (" N
          <STATS-PCT ,STATS-DIAG-MOVES ,STATS-MOVES> "%)">
    <CURSET 13 45>
    <TELL "Stairs descended: " N ,STATS-STAIRS-DOWN>
    <CURSET 14 45>
    <TELL "Monkeys sold: " N ,BUSKER-MONKEY-SALES>

    <CURSET 16 45>
    <TELL "   Wall bumps: " N ,STATS-WALL-BUMPS>
    <CURSET 17 45>
    <TELL "Takes blocked: " N ,STATS-PACKFULL-PICKUP-BLOCKED>

    <CURSET 22 1>
    <TELL "[Press N for next, Q to quit.]">

    <RTRUE>>

<ROUTINE DRAW-GAME-STATS-CONSUMABLES ("AUX" ROW CNT)
    <GAME-STATS-CLEAR>
    <CURSET 1 1>
    <TELL "Statistics  page 2/3">

    <CURSET 3 1>
    <TELL "Potions drunk">
    <CURSET 3 34>
    <TELL "Food eaten">
    <CURSET 4 1>
    <TELL "------------------------------">
    <CURSET 4 34>
    <TELL "------------------------------">

    <SET ROW 5>
    <DO (I 1 ,POTION-TYPE-COUNT)
        <SET CNT <GET ,STATS-POTIONS-DRANK <- .I 1>>>
        <CURSET <+ .ROW <- .I 1>> 1>
        <TELL N .CNT "x " <POTION-TYPE-NAME .I>>>

    <DO (I 1 ,FOOD-TYPE-COUNT)
        <SET CNT <GET ,STATS-FOODS-EATEN <- .I 1>>>
        <CURSET <+ .ROW <- .I 1>> 34>
        <TELL N .CNT "x " <FOOD-NAME .I>>>

    <CURSET <+ .ROW %<MAX ,POTION-TYPE-COUNT ,FOOD-TYPE-COUNT> 2> 1>
    <TELL "[Press N for next, P for previous, Q to quit.]">

    <RTRUE>>

<ROUTINE DRAW-GAME-STATS-COMBAT-LOOT ("AUX" ROW CNT FOUND)
    <GAME-STATS-CLEAR>
    <CURSET 1 1>
    <TELL "Statistics  page 3/3">

    <CURSET 3 1>
    <TELL "Enemies killed">
    <CURSET 3 34>
    <TELL "Treasures">
    <CURSET 4 1>
    <TELL "------------------------------">
    <CURSET 4 34>
    <TELL "------------------------------">

    <SET ROW 5>
    <DO (I 1 ,ETYPE-COUNT)
        <SET CNT <GET ,STATS-ENEMIES-KILLED <- .I 1>>>
        <CURSET <+ .ROW <- .I 1>> 1>
        <TELL N .CNT "x " <ENEMY-NAME .I>>>

    <DO (I 1 ,TREASURE-COUNT)
        <SET CNT <GET ,STATS-TREASURES-PICKED <- .I 1>>>
        <SET FOUND <COND (<G? .CNT 0> "found") (ELSE "not found")>>
        <CURSET <+ .ROW <- .I 1>> 34>
        <TELL <TREASURE-NAME .I> " " .FOUND>>

    <CURSET <+ .ROW %<MAX ,ETYPE-COUNT ,TREASURE-COUNT> 2> 1>
    <TELL "[Press P for previous, Q to quit.]">

    <RTRUE>>
