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

"Honors"

;"Honors are special achievements that the player can earn by following certain
  self-imposed challenges. They don't affect gameplay, but they give the player
  bragging rights and a sense of accomplishment. Some of them are mutually
  exclusive, so the player has to choose which ones to go for."

<CONSTANT HONOR-COUNT 19>

<CONSTANT HONOR-ANIMAL-LOVER 1>
<CONSTANT HONOR-ATTENTIVE 2>
<CONSTANT HONOR-BISHOP 3>
<CONSTANT HONOR-BRAWLER 4>
<CONSTANT HONOR-CHEAPSKATE 5>
<CONSTANT HONOR-DAREDEVIL 6>
<CONSTANT HONOR-EXPLORER 7>
<CONSTANT HONOR-HEALTH-NUT 8>
<CONSTANT HONOR-LAB-SAFETY 9>
<CONSTANT HONOR-OLD-FAITHFUL 10>
<CONSTANT HONOR-PACIFIST 11>
<CONSTANT HONOR-PAY-TO-WIN 12>
<CONSTANT HONOR-PICKY-EATER 13>
<CONSTANT HONOR-ROOK 14>

<CONSTANT FIRST-HONOR-ID ,HONOR-ANIMAL-LOVER>
<CONSTANT LAST-HONOR-BEFORE-SHOWOFF ,HONOR-ROOK>

<CONSTANT HONOR-SHOWOFF-I 15>
<CONSTANT HONOR-SHOWOFF-II 16>
<CONSTANT HONOR-SHOWOFF-III 17>
<CONSTANT HONOR-SHOWOFF-IV 18>
<CONSTANT HONOR-SHOWOFF-V 19>

<CONSTANT FIRST-SHOWOFF-HONOR ,HONOR-SHOWOFF-I>
<CONSTANT LAST-SHOWOFF-HONOR ,HONOR-SHOWOFF-V>

<GLOBAL HONOR-STARTER-WEAPON 0>
<GLOBAL HONOR-BRAWLER-BROKEN? <>>
<GLOBAL HONOR-OLD-FAITHFUL-BROKEN? <>>
<GLOBAL HONOR-HP-BELOW-HALF? <>>
<GLOBAL HONOR-DAREDEVIL-BROKEN? <>>
<GLOBAL HONOR-DRANK-UNIDENTIFIED? <>>
<GLOBAL HONOR-BOUGHT-TROPHY? <>>

<ROUTINE HONORS-RESET ()
    <SETG HONOR-STARTER-WEAPON 0>
    <SETG HONOR-BRAWLER-BROKEN? <>>
    <SETG HONOR-OLD-FAITHFUL-BROKEN? <>>
    <SETG HONOR-HP-BELOW-HALF? <>>
    <SETG HONOR-DAREDEVIL-BROKEN? <>>
    <SETG HONOR-DRANK-UNIDENTIFIED? <>>
    <SETG HONOR-BOUGHT-TROPHY? <>>
    <RTRUE>>

<ROUTINE HONORS-NOTE-STARTER-WEAPON (O)
    <SETG HONOR-STARTER-WEAPON .O>
    <SETG HONOR-OLD-FAITHFUL-BROKEN? <>>
    <RTRUE>>

<ROUTINE HONORS-NOTE-EQUIP-CHANGE ()
    <COND (<AND <NOT ,HONOR-OLD-FAITHFUL-BROKEN?>
                <G? ,HONOR-STARTER-WEAPON 0>
                <N==? ,EQUIPPED-WEAPON ,HONOR-STARTER-WEAPON>>
           <SETG HONOR-OLD-FAITHFUL-BROKEN? T>)>
    <RTRUE>>

<ROUTINE HP-BELOW-HALF? (HP MAXHP)
    <L? <* .HP 2> .MAXHP>>

<ROUTINE HONORS-NOTE-PLAYER-HP ()
    <COND (<HP-BELOW-HALF? ,PLAYER-HP ,PLAYER-MAX-HP>
           <SETG HONOR-HP-BELOW-HALF? T>)
          (<AND ,HONOR-HP-BELOW-HALF?
                <NOT ,HONOR-DAREDEVIL-BROKEN?>>
           <SETG HONOR-DAREDEVIL-BROKEN? T>)>
    <RTRUE>>

<ROUTINE HONORS-NOTE-PLAYER-ATTACK (USED-FISTS?)
    <COND (<NOT .USED-FISTS?>
           <SETG HONOR-BRAWLER-BROKEN? T>)>
    <RTRUE>>

<ROUTINE HONORS-NOTE-POTION-DRINK (WAS-DISCOVERED?)
    <COND (<NOT .WAS-DISCOVERED?>
           <SETG HONOR-DRANK-UNIDENTIFIED? T>)>
    <RTRUE>>

<ROUTINE HONORS-NOTE-TROPHY-PURCHASE (K ID)
    <COND (<AND <==? .K ,ITEMKIND-TREASURE>
                <==? .ID ,TREASURE-TROPHY>>
           <SETG HONOR-BOUGHT-TROPHY? T>)>
    <RTRUE>>

<ROUTINE HONORS-TOTAL-KILLS ()
    <SUM-WORD-TABLE ,STATS-ENEMIES-KILLED ,ETYPE-COUNT>>

<ROUTINE HONORS-TOTAL-SPENT ()
    <+ ,STATS-GOLD-SPENT-TRADER
       ,STATS-GOLD-SPENT-BLACKSMITH
       ,STATS-GOLD-SPENT-CARROT-FARM>>

<ROUTINE HONORS-FOOD-TYPES-EATEN ("AUX" CNT)
    <SET CNT 0>
    <DO (I 1 ,FOOD-TYPE-COUNT)
        <COND (<G? <GET ,STATS-FOODS-EATEN <- .I 1>> 0>
               <SET CNT <+ .CNT 1>>)>>
    .CNT>

<ROUTINE HONOR-SHOWOFF-THRESHOLD (ID)
    <COND (<==? .ID ,HONOR-SHOWOFF-I> 3)
          (<==? .ID ,HONOR-SHOWOFF-II> 5)
          (<==? .ID ,HONOR-SHOWOFF-III> 7)
          (<==? .ID ,HONOR-SHOWOFF-IV> 9)
          (<==? .ID ,HONOR-SHOWOFF-V> 11)
          (ELSE 0)>>

<ROUTINE HONOR-SHOWOFF? (ID)
    <AND <G=? .ID ,FIRST-SHOWOFF-HONOR>
         <L=? .ID ,LAST-SHOWOFF-HONOR>>>

<ROUTINE PRINT-HONOR-NAME (ID)
    <COND (<==? .ID ,HONOR-ANIMAL-LOVER> <TELL "Animal Lover">)
          (<==? .ID ,HONOR-ATTENTIVE> <TELL "Attentive">)
          (<==? .ID ,HONOR-BISHOP> <TELL "Bishop">)
          (<==? .ID ,HONOR-BRAWLER> <TELL "Brawler">)
          (<==? .ID ,HONOR-CHEAPSKATE> <TELL "Cheapskate">)
          (<==? .ID ,HONOR-DAREDEVIL> <TELL "Daredevil">)
          (<==? .ID ,HONOR-EXPLORER> <TELL "Explorer">)
          (<==? .ID ,HONOR-HEALTH-NUT> <TELL "Health Nut">)
          (<==? .ID ,HONOR-LAB-SAFETY> <TELL "Lab Safety">)
          (<==? .ID ,HONOR-OLD-FAITHFUL> <TELL "Old Faithful">)
          (<==? .ID ,HONOR-PACIFIST> <TELL "Pacifist">)
          (<==? .ID ,HONOR-PAY-TO-WIN> <TELL "Pay To Win">)
          (<==? .ID ,HONOR-PICKY-EATER> <TELL "Picky Eater">)
          (<==? .ID ,HONOR-ROOK> <TELL "Rook">)
          (<==? .ID ,HONOR-SHOWOFF-I> <TELL "Showoff I">)
          (<==? .ID ,HONOR-SHOWOFF-II> <TELL "Showoff II">)
          (<==? .ID ,HONOR-SHOWOFF-III> <TELL "Showoff III">)
          (<==? .ID ,HONOR-SHOWOFF-IV> <TELL "Showoff IV">)
          (<==? .ID ,HONOR-SHOWOFF-V> <TELL "Showoff V">)
          (ELSE <TELL "Unknown">)>>

<ROUTINE PRINT-HONOR-DESC (ID)
    <COND (<==? .ID ,HONOR-ANIMAL-LOVER> <TELL "don't kill any monkeys">)
          (<==? .ID ,HONOR-ATTENTIVE> <TELL "never bump into walls">)
          (<==? .ID ,HONOR-BISHOP> <TELL "move at least 90% diagonally">)
          (<==? .ID ,HONOR-BRAWLER> <TELL "only attack with fists">)
          (<==? .ID ,HONOR-CHEAPSKATE> <TELL "don't spend any gold">)
          (<==? .ID ,HONOR-DAREDEVIL> <TELL "go below 50% HP and never go back up">)
          (<==? .ID ,HONOR-EXPLORER> <TELL "visit every room">)
          (<==? .ID ,HONOR-HEALTH-NUT> <TELL "never let HP go below 50%">)
          (<==? .ID ,HONOR-LAB-SAFETY> <TELL "don't drink unidentified potions">)
          (<==? .ID ,HONOR-OLD-FAITHFUL> <TELL "never unequip the starter weapon">)
          (<==? .ID ,HONOR-PACIFIST> <TELL "don't kill any enemies">)
          (<==? .ID ,HONOR-PAY-TO-WIN> <TELL "buy the Trophy of Scryra">)
          (<==? .ID ,HONOR-PICKY-EATER> <TELL "only eat one type of food">)
          (<==? .ID ,HONOR-ROOK> <TELL "don't move diagonally">)
          (<==? .ID ,HONOR-SHOWOFF-I> <TELL "win with at least 3 other honors">)
          (<==? .ID ,HONOR-SHOWOFF-II> <TELL "win with at least 5 other honors">)
          (<==? .ID ,HONOR-SHOWOFF-III> <TELL "win with at least 7 other honors">)
          (<==? .ID ,HONOR-SHOWOFF-IV> <TELL "win with at least 9 other honors">)
          (<==? .ID ,HONOR-SHOWOFF-V> <TELL "win with at least 11 other honors">)
          (ELSE <TELL "Unknown">)>>

<ADD-TELL-TOKENS
    HONOR-NAME * <PRINT-HONOR-NAME .X>
    HONOR-DESC * <PRINT-HONOR-DESC .X>>

<ROUTINE HONOR-DISQUALIFIED? (ID IN-GAME? "AUX" MOVES DIAG THRESH)
    <SET MOVES ,STATS-MOVES>
    <SET DIAG ,STATS-DIAG-MOVES>
    <COND (<==? .ID ,HONOR-ANIMAL-LOVER>
           <G? <GET ,STATS-ENEMIES-KILLED <- ,ETYPE-MONKEY 1>> 0>)
          (<==? .ID ,HONOR-ATTENTIVE>
           <G? ,STATS-WALL-BUMPS 0>)
          (<==? .ID ,HONOR-PAY-TO-WIN>
           <AND <NOT .IN-GAME?> <NOT ,HONOR-BOUGHT-TROPHY?>>)
          (<==? .ID ,HONOR-BISHOP>
           <AND <NOT .IN-GAME?> <G? .MOVES 0> <L? <* 10 .DIAG> <* 9 .MOVES>>>)
          (<==? .ID ,HONOR-BRAWLER>
           ,HONOR-BRAWLER-BROKEN?)
          (<==? .ID ,HONOR-DAREDEVIL>
           <OR ,HONOR-DAREDEVIL-BROKEN?
               <AND <NOT .IN-GAME?> <NOT ,HONOR-HP-BELOW-HALF?>>>)
          (<==? .ID ,HONOR-EXPLORER>
           <AND <NOT .IN-GAME?>
                <N==? ,STATS-ROOMS-DISCOVERED-TOTAL <COUNT-TOTAL-ROOMS>>>)
          (<==? .ID ,HONOR-HEALTH-NUT>
           ,HONOR-HP-BELOW-HALF?)
          (<==? .ID ,HONOR-LAB-SAFETY>
           ,HONOR-DRANK-UNIDENTIFIED?)
          (<==? .ID ,HONOR-CHEAPSKATE>
           <G? <HONORS-TOTAL-SPENT> 0>)
          (<==? .ID ,HONOR-OLD-FAITHFUL>
           ,HONOR-OLD-FAITHFUL-BROKEN?)
          (<==? .ID ,HONOR-PACIFIST>
           <G? <HONORS-TOTAL-KILLS> 0>)
          (<==? .ID ,HONOR-PICKY-EATER>
           <G? <HONORS-FOOD-TYPES-EATEN> 1>)
          (<==? .ID ,HONOR-ROOK>
           <G? .DIAG 0>)
          (<HONOR-SHOWOFF? .ID>
           <SET THRESH <HONOR-SHOWOFF-THRESHOLD .ID>>
           <L? <HONORS-MAX-POSSIBLE-BASE .IN-GAME?> .THRESH>)
          (ELSE <>)>>

<ROUTINE HONOR-EARNED? (ID "AUX" MOVES DIAG THRESH)
    <SET MOVES ,STATS-MOVES>
    <SET DIAG ,STATS-DIAG-MOVES>
    <COND (<==? .ID ,HONOR-ANIMAL-LOVER>
           <NOT <HONOR-DISQUALIFIED? .ID <>>>)
          (<==? .ID ,HONOR-ATTENTIVE>
           <NOT <HONOR-DISQUALIFIED? .ID <>>>)
          (<==? .ID ,HONOR-PAY-TO-WIN>
           ,HONOR-BOUGHT-TROPHY?)
          (<==? .ID ,HONOR-BISHOP>
           <AND <G? .MOVES 0>
                <G=? <* 10 .DIAG> <* 9 .MOVES>>>)
          (<==? .ID ,HONOR-BRAWLER>
           <NOT ,HONOR-BRAWLER-BROKEN?>)
          (<==? .ID ,HONOR-DAREDEVIL>
           <AND ,HONOR-HP-BELOW-HALF?
                <NOT ,HONOR-DAREDEVIL-BROKEN?>>)
          (<==? .ID ,HONOR-EXPLORER>
           <==? ,STATS-ROOMS-DISCOVERED-TOTAL <COUNT-TOTAL-ROOMS>>)
          (<==? .ID ,HONOR-HEALTH-NUT>
           <NOT ,HONOR-HP-BELOW-HALF?>)
          (<==? .ID ,HONOR-LAB-SAFETY>
           <NOT ,HONOR-DRANK-UNIDENTIFIED?>)
          (<==? .ID ,HONOR-CHEAPSKATE>
           <L=? <HONORS-TOTAL-SPENT> 0>)
          (<==? .ID ,HONOR-OLD-FAITHFUL>
           <NOT ,HONOR-OLD-FAITHFUL-BROKEN?>)
          (<==? .ID ,HONOR-PACIFIST>
           <L=? <HONORS-TOTAL-KILLS> 0>)
          (<==? .ID ,HONOR-PICKY-EATER>
           <L=? <HONORS-FOOD-TYPES-EATEN> 1>)
          (<==? .ID ,HONOR-ROOK>
           <L=? .DIAG 0>)
          (<HONOR-SHOWOFF? .ID>
           <SET THRESH <HONOR-SHOWOFF-THRESHOLD .ID>>
           <G=? <HONORS-BASE-EARNED-COUNT> .THRESH>)
          (ELSE <>)>>

<ROUTINE HONORS-BASE-EARNED-COUNT ("AUX" CNT)
    <SET CNT 0>
    <DO (I ,FIRST-HONOR-ID ,LAST-HONOR-BEFORE-SHOWOFF)
        <COND (<HONOR-EARNED? .I>
               <SET CNT <+ .CNT 1>>)>>
    .CNT>

<ROUTINE HONORS-MAX-POSSIBLE-BASE (IN-GAME? "AUX" CNT)
    <SET CNT 0>
    <DO (I ,FIRST-HONOR-ID ,LAST-HONOR-BEFORE-SHOWOFF)
        <COND (<NOT <HONOR-DISQUALIFIED? .I .IN-GAME?>>
               <SET CNT <+ .CNT 1>>)>>
    .CNT>

<ROUTINE HONORS-TOTAL-EARNED-COUNT ("AUX" CNT)
    <SET CNT 0>
    <DO (I ,FIRST-HONOR-ID ,HONOR-COUNT)
        <COND (<HONOR-EARNED? .I>
               <SET CNT <+ .CNT 1>>)>>
    .CNT>

<ROUTINE HIGHEST-EARNED-HONOR-ID ("AUX" TOP)
    <SET TOP 0>
    <DO (I ,FIRST-HONOR-ID ,HONOR-COUNT)
        <COND (<HONOR-EARNED? .I>
               <SET TOP .I>)>>
    .TOP>

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
    <HONORS-RESET>
    <RTRUE>>

<ROUTINE STATS-INC-WORD-TABLE (TBL IDX "AUX" CUR)
    <SET CUR <GET .TBL .IDX>>
    <PUT .TBL .IDX <+ .CUR 1>>
    <RTRUE>>

"Scoring"

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

<ROUTINE COUNT-TOTAL-ROOMS ("AUX" CNT)
    <SET CNT 0>
    <DO (F 0 %<- ,MAX-FLOORS 1>)
        <SET CNT <+ .CNT <GETB ,FLOOR-ROOM-COUNT .F>>>>
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
    <TELL "Rooms discovered: " N ,STATS-ROOMS-DISCOVERED-TOTAL "/" N <COUNT-TOTAL-ROOMS>>
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
        <TELL N .CNT "x " POTION-TYPE-NAME .I>>

    <DO (I 1 ,FOOD-TYPE-COUNT)
        <SET CNT <GET ,STATS-FOODS-EATEN <- .I 1>>>
        <CURSET <+ .ROW <- .I 1>> 34>
        <TELL N .CNT "x " FOOD-NAME .I>>

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
        <TELL TREASURE-NAME .I " " .FOUND>>

    <CURSET <+ .ROW %<MAX ,ETYPE-COUNT ,TREASURE-COUNT> 2> 1>
    <TELL "[Press P for previous, Q to quit.]">

    <RTRUE>>
