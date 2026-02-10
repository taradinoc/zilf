;"Constants and globals"

"Tile and UI constants"

<CONSTANT TILE-WALL !\#>
<CONSTANT TILE-FLOOR !\.>
<CONSTANT TILE-CORRIDOR !\,>
<CONSTANT TILE-DOOR !\+>
<CONSTANT TILE-LOCKEDDOOR !\X>
<CONSTANT TILE-KEY !\->
<CONSTANT TILE-PLAYER !\@>
<CONSTANT TILE-GOBLIN !\g>
<CONSTANT TILE-SPHINX !\s>
<CONSTANT TILE-KRAKEN !\k>
<CONSTANT TILE-DRAGON !\D>
<CONSTANT TILE-WRAITH !\w>
<CONSTANT TILE-BEES !\B>
<CONSTANT TILE-MONKEY !\m>
<CONSTANT TILE-SPIRIT !\S>
<CONSTANT TILE-STAIR-UP !\<>
<CONSTANT TILE-STAIR-DOWN !\>>
<CONSTANT TILE-UNKNOWN !\ >
<CONSTANT TILE-INTERIOR !\?>
<CONSTANT TILE-GOLD !\$>
<CONSTANT TILE-POTION !\!>
<CONSTANT TILE-TREASURE !\*>
<CONSTANT TILE-TRADER !\t>
<CONSTANT TILE-WEAPON !\)>
<CONSTANT TILE-BANANA !\b>
<CONSTANT TILE-CHEESE !\C>
<CONSTANT TILE-GRAPES !\G>
<CONSTANT TILE-MUFFIN !\M>
<CONSTANT TILE-TURKEY !\T>
<CONSTANT TILE-CARROT !\r>
<CONSTANT TILE-CAVIAR !\c>

<CONSTANT HEADER-ROW 1>
<CONSTANT CONTROLS-ROW 2>
<CONSTANT MAP-ROW 4>
<CONSTANT MAP-COL 1>

;"Height of the upper window needed to draw the fixed UI (header + map).
    Everything printed to the lower window will scroll below it."
<CONSTANT UPPER-HEIGHT <+ ,MAP-ROW <- ,MAP-H 1>>>
<CONSTANT RECOMMENDED-SCRV 30>
<CONSTANT RECOMMENDED-SCRH 80>

<CONSTANT KEY-F8 140>
<CONSTANT KEY-F9 141>
<CONSTANT KEY-F10 142>
<CONSTANT KEY-F11 143>
<CONSTANT KEY-F12 144>

<IF-DEBUG
    <GLOBAL DEBUG-OVERLAY-ONCE? <>>

    ;"Debug/testing toggles"

    ;"Stores the last function key pressed for 'double press' debug commands."

    <GLOBAL DEBUG-DOUBLEKEY 0>

    ;"Set when the player uses any debug command during the run."

    <GLOBAL DEBUG-USED? <>>

    ;"If true, the player's HP is forced to max each turn."

    <GLOBAL IMMORTAL? <>>

    ;"If true, the whole map is drawn (no fog-of-war)."

    <GLOBAL OMNISCIENT? <>>

    ;"If true, pressing < or > while not on the corresponding stair teleports
    the player to the stair tile."

    <GLOBAL STAIR-FINDER? <>>

    <ROUTINE DBG-PLAYER-TELEPORT-CANDIDATE? (X Y)
        <AND <IN-BOUNDS? .X .Y>
            <FLOOR? .X .Y>
            <NOT <TRADER-AT? .X .Y>>
            <L=? <ENEMY-AT .X .Y> 0>
            <N==? <TILE-AT .X .Y> ,TILE-STAIR-UP>
            <N==? <TILE-AT .X .Y> ,TILE-STAIR-DOWN>
            <N==? <TILE-AT .X .Y> ,TILE-INTERIOR>>>

    <ROUTINE DBG-MONKEY-SPAWN-CANDIDATE? (X Y)
        <AND <DBG-PLAYER-TELEPORT-CANDIDATE? .X .Y>
            <NOT <AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>>
            <L=? <GOLD-OBJ-AT .X .Y> 0>
            <L=? <FOOD-OBJ-AT .X .Y> 0>
            <L=? <WEAPON-OBJ-AT .X .Y> 0>
            <NOT <POTION-AT? .X .Y>>
            <L=? <TREASURE-AT? .X .Y> 0>>>

    <ROUTINE DBG-BUSKER-ENTRANCE-IDX (F "AUX" MAX)
        <SET MAX <- ,INTERIOR-ENTRANCE-COUNT 1>>
        <DO (I 0 .MAX)
            <COND (<AND <==? <GETB ,INTERIOR-ENTRANCE-FLOOR .I> .F>
                        <==? <GETB ,INTERIOR-ENTRANCE-ID .I> ,INTERIOR-BUSKER>>
                <RETURN .I>)>>
        -1>

    <ROUTINE DBG-MOVE-PLAYER-NEAR-BUSKER ("AUX" IDX EX EY NX NY)
        <SET IDX <DBG-BUSKER-ENTRANCE-IDX ,CURRENT-FLOOR>>
        <COND (<L? .IDX 0> <RFALSE>)>
        <SET EX <GETB ,INTERIOR-ENTRANCE-X .IDX>>
        <SET EY <GETB ,INTERIOR-ENTRANCE-Y .IDX>>
        <COND (<OR <L=? .EX 0> <L=? .EY 0>> <RFALSE>)>
        <DO (D 1 8)
            <SET NX <+ .EX <DIR8-DX .D>>>
            <SET NY <+ .EY <DIR8-DY .D>>>
            <COND (<DBG-PLAYER-TELEPORT-CANDIDATE? .NX .NY>
                <SETG PLAYER-X .NX>
                <SETG PLAYER-Y .NY>
                <AFTER-PLAYER-RELOCATE>
                <RTRUE>)>>
        <RFALSE>>

    <ROUTINE DBG-SPAWN-TAMED-MONKEY-NEAR-PLAYER ("AUX" O HP X Y)
        <DO (D 1 8)
            <SET X <+ ,PLAYER-X <DIR8-DX .D>>>
            <SET Y <+ ,PLAYER-Y <DIR8-DY .D>>>
            <COND (<DBG-MONKEY-SPAWN-CANDIDATE? .X .Y>
                <SET O <ENEMY-OBJ-OF-TYPE ,CURRENT-FLOOR-OBJ ,ETYPE-MONKEY>>
                <COND (<NOT .O>
                        <COND (<NOT <SET O <ALLOC-RASCAL-ENEMY>>>
                                <LOG "[DEBUG] No enemy slots." CR>
                                <RFALSE>)>
                        <PUTP .O ,P?R-ETYPE ,ETYPE-MONKEY>
                        <SET HP <ENEMY-START-HP ,ETYPE-MONKEY ,CURRENT-FLOOR>>
                        <PUTP .O ,P?R-EHP .HP>
                        <MOVE .O ,CURRENT-FLOOR-OBJ>)>
                <PUTP .O ,P?R-X .X>
                <PUTP .O ,P?R-Y .Y>
                <FSET .O ,TAMEBIT>
                <LOG "[DEBUG] Spawned a tame monkey." CR>
                <RTRUE>)>>
        <LOG "[DEBUG] No space to spawn a monkey." CR>
        <RFALSE>>

    <ROUTINE DBG-F8-TELEPORT-TO-BUSKER ("AUX" F UPX UPY)
        <SET F ,GUARANTEED-BUSKER-FLOOR>
        <COND (<OR <L? .F 1> <G? .F ,MAX-FLOORS>>
            <LOG "[DEBUG] No busker floor is available." CR>
            <RFALSE>)>
        <SET UPX <FLOOR-UP-X .F>>
        <SET UPY <FLOOR-UP-Y .F>>
        <LOG "[DEBUG] Teleporting to busker floor " N .F "." CR>
        <COND (<AND <G? .UPX 0> <G? .UPY 0>> <ENTER-FLOOR .F .UPX .UPY T>)
            (ELSE <ENTER-FLOOR .F ,PLAYER-X ,PLAYER-Y T>)>
        <DBG-MOVE-PLAYER-NEAR-BUSKER>
        <DBG-SPAWN-TAMED-MONKEY-NEAR-PLAYER>
        <RTRUE>>
>

;"Macros"

;"Reads one character of input (ZSCII) without echo.

Returns:
  ZSCII character code."

<DEFMAC GETCHAR () '<INPUT 1>>

;"Prints a message to the lower window.

Args:
  The arguments to TELL.

Returns:
  T."

<DEFMAC LOG ("ARGS" A)
    `<BIND () <SCREEN 0> <UI-LOG-COLOR> <TELL ~!.A> <SCREEN 1>>>

;"Prints a message, optionally switching to the lower window and back.

Args:
  LOG?: True to switch windows, or false not to.
  A: The arguments to TELL.

Returns:
  T."

<DEFMAC TELL/LOG ('LOG? "ARGS" A)
    `<BIND ()
        <AND ~.LOG? <SCREEN 0> <UI-LOG-COLOR>>
        <TELL ~!.A>
        <AND ~.LOG? <SCREEN 1>>
        T>>

;"Initialization screens"

<GLOBAL HAS-TCOLOR? <>>
<GLOBAL WANT-TCOLOR? <>>

<ROUTINE CHECK-SCREEN ()
    ;"Check for Standard 1.1, which provides the TCOLOR opcode"
    <COND (<G=? <LOWCORE STDREV> #16 0101>
           <SETG HAS-TCOLOR? T>
           <SETG WANT-TCOLOR? T>)>
    ;"Check screen dimensions"
    <COND (<OR <L? <LOWCORE SCRH> ,RECOMMENDED-SCRH>
               <L? <LOWCORE SCRV> ,RECOMMENDED-SCRV>>
           <TELL "A minimum screen size of " N ,RECOMMENDED-SCRH "x" N
                 ,RECOMMENDED-SCRV " is recommended." CR CR
                 "Your screen size is " N <LOWCORE SCRH> "x" N <LOWCORE SCRV>
                 ". Please resize it if possible." CR CR
                 "[Press any key to continue.]" CR>
           <GETCHAR>)>>

<ADD-TELL-TOKENS RSERIAL <PRINT-RSERIAL>>

<ROUTINE PRINT-RSERIAL ()
    <TELL N ,RELEASEID !\/>
    <LOWCORE-TABLE SERIAL 6 PRINTC>>

<DEFMAC CTCOLOR ('FG 'BG)
    `<COND (,WANT-TCOLOR? <TCOLOR ~.FG ~.BG>)>>

<DEFMAC RGB (R G B)
    #DECL ((R G B) FIX)
    <+ .R <* .G 32> <* .B 32 32>>>

;"UI colors (Brogue-ish). Use TCOLOR when available; fall back to COLOR."

<CONSTANT ZCOL-DEFAULT 1>
<CONSTANT ZCOL-BLACK 2>
<CONSTANT ZCOL-RED 3>
<CONSTANT ZCOL-GREEN 4>
<CONSTANT ZCOL-YELLOW 5>
<CONSTANT ZCOL-BLUE 6>
<CONSTANT ZCOL-MAGENTA 7>
<CONSTANT ZCOL-CYAN 8>
<CONSTANT ZCOL-WHITE 9>

<CONSTANT UI-RGB-TEXT <RGB 28 28 28>>
<CONSTANT UI-RGB-LABEL <RGB 18 18 18>>
<CONSTANT UI-RGB-ALERT <RGB 31 8 8>>

<CONSTANT UI-RGB-FLOOR <RGB 12 12 12>>
<CONSTANT UI-RGB-CORRIDOR <RGB 7 7 7>>

<CONSTANT UI-RGB-TANBG <RGB 20 16 10>>
<CONSTANT UI-RGB-BROWNBG <RGB 10 6 2>>
<CONSTANT UI-RGB-CHARCOAL <RGB 4 4 4>>
<CONSTANT UI-RGB-ORANGE <RGB 31 18 2>>

<CONSTANT UI-RGB-WALLFG <RGB 20 16 10>>
<CONSTANT UI-RGB-WALLBG <RGB 5 3 1>>
<CONSTANT UI-RGB-DOOR <RGB 24 14 6>>
<CONSTANT UI-RGB-UNKNOWN <RGB 6 6 6>>
<CONSTANT UI-RGB-INTERIOR <RGB 20 12 28>>

<CONSTANT UI-RGB-PLAYER <RGB 31 31 31>>
<CONSTANT UI-RGB-PLAYER-INVIS <RGB 12 20 31>>

<CONSTANT UI-RGB-GOLD <RGB 31 27 6>>
<CONSTANT UI-RGB-POTION <RGB 28 10 28>>
<CONSTANT UI-RGB-TREASURE <RGB 10 26 31>>
<CONSTANT UI-RGB-TRADER <RGB 12 20 31>>
<CONSTANT UI-RGB-WEAPON <RGB 20 24 31>>
<CONSTANT UI-RGB-FOOD <RGB 16 28 12>>
<CONSTANT UI-RGB-STAIRS <RGB 12 28 12>>

<CONSTANT UI-RGB-GOBLIN <RGB 8 26 8>>
<CONSTANT UI-RGB-SPHINX <RGB 30 26 10>>
<CONSTANT UI-RGB-KRAKEN <RGB 10 22 28>>
<CONSTANT UI-RGB-DRAGON <RGB 31 8 4>>
<CONSTANT UI-RGB-WRAITH <RGB 20 10 28>>
<CONSTANT UI-RGB-BEES <RGB 31 26 6>>
<CONSTANT UI-RGB-MONKEY <RGB 30 26 10>>
<CONSTANT UI-RGB-SPIRIT <RGB 20 10 28>>

<DEFMAC UI-FG ('FGRGB 'BASICFG)
  `<COND (,WANT-TCOLOR? <TCOLOR ~.FGRGB 0>) (ELSE <COLOR ~.BASICFG 0>)>>

<DEFMAC UI-COL ('FGRGB 'BGRGB 'BASICFG 'BASICBG)
  `<COND (,WANT-TCOLOR? <TCOLOR ~.FGRGB ~.BGRGB>) (ELSE <COLOR ~.BASICFG ~.BASICBG>)>>

<DEFMAC UI-LOG-COLOR ()
  `<COND (,WANT-TCOLOR? <TCOLOR ,UI-RGB-TEXT 0>) (ELSE <COLOR 1 1>)>>

<DEFMAC UI-ALERT ()
  `<COND (,WANT-TCOLOR? <TCOLOR ,UI-RGB-ALERT 0>) (ELSE <COLOR ,ZCOL-RED 0>)>>

<DEFMAC UI-RESET ()
  `<COND (,WANT-TCOLOR? <TCOLOR -1 0>) (ELSE <COLOR ,ZCOL-DEFAULT ,ZCOL-DEFAULT>)>>

<ROUTINE APPLY-SPRITE-COLOR (CH)
    <COND (<==? .CH ,TILE-PLAYER> <UI-FG ,UI-RGB-PLAYER ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-WALL>
           <UI-COL ,UI-RGB-WALLFG ,UI-RGB-WALLBG ,ZCOL-DEFAULT ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-DOOR>
           <UI-COL ,UI-RGB-ORANGE ,UI-RGB-BROWNBG ,ZCOL-DEFAULT ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-LOCKEDDOOR>
           <UI-COL ,UI-RGB-ORANGE ,UI-RGB-BROWNBG ,ZCOL-DEFAULT ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-FLOOR> <UI-FG ,UI-RGB-FLOOR ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-CORRIDOR> <UI-FG ,UI-RGB-CORRIDOR ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-UNKNOWN> <UI-FG ,UI-RGB-UNKNOWN ,ZCOL-DEFAULT>)
          (<==? .CH ,TILE-INTERIOR> <UI-FG ,UI-RGB-INTERIOR ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-STAIR-UP ,TILE-STAIR-DOWN>
           <UI-FG ,UI-RGB-STAIRS ,ZCOL-GREEN>)
          (<==? .CH ,TILE-GOLD> <UI-FG ,UI-RGB-GOLD ,ZCOL-GREEN>)
          (<==? .CH ,TILE-KEY> <UI-FG ,UI-RGB-GOLD ,ZCOL-GREEN>)
          (<==? .CH ,TILE-POTION> <UI-FG ,UI-RGB-POTION ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-TREASURE> <UI-FG ,UI-RGB-TREASURE ,ZCOL-CYAN>)
          (<==? .CH ,TILE-TRADER> <UI-FG ,UI-RGB-TRADER ,ZCOL-CYAN>)
          (<==? .CH ,TILE-WEAPON> <UI-FG ,UI-RGB-WEAPON ,ZCOL-CYAN>)
          (<==? .CH ,TILE-BANANA ,TILE-CHEESE ,TILE-CARROT>
           <UI-FG ,UI-RGB-FOOD ,ZCOL-GREEN>)
          (<==? .CH ,TILE-GRAPES ,TILE-MUFFIN ,TILE-TURKEY ,TILE-CAVIAR>
           <UI-FG ,UI-RGB-FOOD ,ZCOL-GREEN>)
          (<==? .CH ,TILE-GOBLIN> <UI-FG ,UI-RGB-GOBLIN ,ZCOL-GREEN>)
          (<==? .CH ,TILE-SPHINX> <UI-FG ,UI-RGB-SPHINX ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-KRAKEN> <UI-FG ,UI-RGB-KRAKEN ,ZCOL-CYAN>)
          (<==? .CH ,TILE-DRAGON> <UI-FG ,UI-RGB-DRAGON ,ZCOL-RED>)
          (<==? .CH ,TILE-WRAITH> <UI-FG ,UI-RGB-WRAITH ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-BEES> <UI-FG ,UI-RGB-BEES ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-MONKEY> <UI-FG ,UI-RGB-MONKEY ,ZCOL-MAGENTA>)
          (<==? .CH ,TILE-SPIRIT> <UI-FG ,UI-RGB-SPIRIT ,ZCOL-MAGENTA>)
          (ELSE <UI-RESET>)>
    <RTRUE>>

<ROUTINE SPLASH ("AUX" COL C)
    <SPLIT <LOWCORE SCRV>>
    <SCREEN 1>
    <COND (,WANT-TCOLOR? <TCOLOR -1 0>) (ELSE <COLOR 1 1>)>
    <CLEAR -2>
    <SET COL </ <- <LOWCORE SCRH> 58> 2>>
    <CURSET 5 .COL>
    <CTCOLOR <RGB 30 6 6> 0>
    <TELL "@@@@@@@    @@@@@@    @@@@@@    @@@@@@@   @@@@@@   @@@     ">
    <CURSET 6 .COL>
    <CTCOLOR <RGB 27 6 8> 0>
    <TELL "@@@@@@@@  @@@@@@@@  @@@@@@@   @@@@@@@@  @@@@@@@@  @@@     ">
    <CURSET 7 .COL>
    <CTCOLOR <RGB 25 7 10> 0>
    <TELL "@@!  @@@  @@!  @@@  !@@       !@@       @@!  @@@  @@!     ">
    <CURSET 8 .COL>
    <CTCOLOR <RGB 22 7 12> 0>
    <TELL "!@!  @!@  !@!  @!@  !@!       !@!       !@!  @!@  !@!     ">
    <CURSET 9 .COL>
    <CTCOLOR <RGB 19 8 14> 0>
    <TELL "@!@!!@!   @!@!@!@!  !!@@!!    !@!       @!@!@!@!  @!!     ">
    <CURSET 10 .COL>
    <CTCOLOR <RGB 17 8 17> 0>
    <TELL "!!@!@!    !!!@!!!!   !!@!!!   !!!       !!!@!!!!  !!!     ">
    <CURSET 11 .COL>
    <CTCOLOR <RGB 14 9 19> 0>
    <TELL "!!: :!!   !!:  !!!       !:!  :!!       !!:  !!!  !!:     ">
    <CURSET 12 .COL>
    <CTCOLOR <RGB 11 9 21> 0>
    <TELL ":!:  !:!  :!:  !:!      !:!   :!:       :!:  !:!   :!:    ">
    <CURSET 13 .COL>
    <CTCOLOR <RGB 9 10 23> 0>
    <TELL "::   :::  ::   :::  :::: ::    ::: :::  ::   :::   :: ::::">
    <CURSET 14 .COL>
    <CTCOLOR <RGB 6 10 25> 0>
    <TELL " :   : :   :   : :  :: : :     :: :: :   :   : :  : :: : :">

    <CTCOLOR <RGB 22 22 22> 0>
    <IF-DEBUG
        <CURSET 16 .COL>
        <TELL "                       DEBUG BUILD                        ">>
    <CURSET 17 .COL>
    <TELL "        RASCAL v" RSERIAL
          %<STRING " (" ,ZIL-VERSION ") by Tara McGrew">>

    <CTCOLOR <RGB 31 31 31> 0>
    <COND (,HAS-TCOLOR?
           <CURSET 19 .COL>
           <TELL "               Press C to toggle color mode               ">)>
    <CURSET 20 .COL>
    <TELL "               Press I for instructions                   ">
    <CURSET 21 .COL>
    <TELL "               Press S to enter a seed                    ">
    <CURSET 22 .COL>
    <TELL "               Press any other key to start               ">
    <SET C <GETCHAR>>
    <COND (<==? .C !\I !\i>
           <INSTRUCTIONS>
           <AGAIN>)
          (<==? .C !\S !\s> <COND (<NOT <INPUT-SEED>> <AGAIN>)>)
          (<==? .C !\C !\c> <COND (,HAS-TCOLOR? <SETG WANT-TCOLOR? <NOT ,WANT-TCOLOR?>>)> <AGAIN>)
          (<==? .C 254 ;"mouse click"> <AGAIN>)
          (ELSE <CLEAR -1>)>>

<ROUTINE INSTRUCTIONS ("OPT" IN-GAME? "AUX" COL C)
    <COLOR 1 1>
    <COND (.IN-GAME? <CLEAR 1>)
          (ELSE
           <SPLIT <LOWCORE SCRV>>
           <SCREEN 1>
           <CLEAR -2>)>
    <SET COL </ <- <LOWCORE SCRH> 78> 2>>
    <CURSET 1 .COL>
    <TELL "         Symbols                                    Inputs                    ">
    <CURSET 2 .COL>
    <TELL "                                                                              ">
    <CURSET 3 .COL>
    <TELL "  You are the rascal  @           Move up/down/left/right   Move diagonally   ">
    <CURSET 4 .COL>
    <TELL "         Walk around  . , # +         W            K            Y   U         ">
    <CURSET 5 .COL>
    <TELL "        Climb stairs  < >           A   D   or   H   L                        ">
    <CURSET 6 .COL>
    <TELL "          Get riches  $               S            J            B   N         ">
    <CURSET 7 .COL>
    <TELL "          Get relics  *                                                       ">
    <CURSET 8 .COL>
    <TELL "          Get potion  !                  Climb stairs            Wait         ">
    <CURSET 9 .COL>
    <TELL "          Get weapon  )                     <    >            .    or  space   "> ;"collapsed space after ."
    <CURSET 10 .COL>
    <TELL "          Get edible  b C ...                                                  "> ;"collapsed space after ."
    <CURSET 11 .COL>
    <TELL "        Fight beasts  g w ...        Drop item     Eat/drink    Equip weapon   "> ;"collapsed space after ."
    <CURSET 12 .COL>
    <TELL "     Discover others  t ?               T             I             G         ">
    <CURSET 13 .COL>
    <TELL "                                                                              ">
    <CURSET 14 .COL>
    <TELL "   gold=123  floor=1/25  hp=12/12  str=4  def=2  wpn=L1 scythe  inv=1/10      ">
    <CURSET 15 .COL>
    <TELL "       |         |           |       |      |          |           |          ">
    <CURSET 16 .COL>
    <TELL "   Your riches   |      Your health  |  Your defense   |     Your inventory   ">
    <CURSET 17 .COL>
    <TELL "                 |                   |                 |                      ">
    <CURSET 18 .COL>
    <TELL "           Your location       Your strength      Your weapon                 ">
    <CURSET 19 .COL>
    <TELL "                                                                              ">
    <CURSET 20 .COL>
    <TELL "                         [Press any key to continue.]                         ">
    <SET C <GETCHAR>>
    <COND (<==? .C 254 ;"mouse click"> <AGAIN>)
          (.IN-GAME? <CLEAR 1>)
          (ELSE <CLEAR -1>)>>

;"Prompts the player to enter a numeric RNG seed for the upcoming game.

The chosen seed is stored in globals SEED-HI/SEED-LO. INIT will use it (if
nonzero) to reseed the PRNG and generate per-floor seeds.

Controls:
  - Hex digits (0-9, A-F): append
  - Backspace/Delete: erase last digit
  - Enter: accept (if nonzero)
  - Esc/Q: cancel (leave seed unset)

Args:
    (none)

Returns:
    T."

<ROUTINE INPUT-SEED ("AUX" COL C N LEN)
    <SET N 0>
    <SET LEN 0>
    <SETG SEED-HI 0>
    <SETG SEED-LO 0>
    <DO (I 1 8) <PUTB ,SEED-BUF <- .I 1> 0>>
    <COLOR 1 1>
    <CLEAR -2>
    <SET COL </ <- <LOWCORE SCRH> 58> 2>>
    <CURSET 10 .COL>
    <TELL "Enter seed (8 hex digits): ">
    <REPEAT ()
        <SET C <GETCHAR>>
        <COND (<==? .C 27 !\Q !\q>
               <SETG SEED-HI 0>
               <SETG SEED-LO 0>
               <CLEAR -1>
               <RFALSE>)
              (<OR <==? .C 13> <==? .C 10>>
               <COND (<AND <NOT <AND <==? ,SEED-HI 0> <==? ,SEED-LO 0>>>
                           <G? .LEN 0>>
                      <CLEAR -1>
                      <RTRUE>)
                     (ELSE
                      <SET N 0>
                      <SET LEN 0>
                      <SETG SEED-HI 0>
                      <SETG SEED-LO 0>
                      <DO (I 1 8) <PUTB ,SEED-BUF <- .I 1> 0>>
                      <CURSET 12 .COL>
                      <TELL "Invalid seed; must be nonzero hex.">
                      <CURSET 10 .COL>
                      <TELL "Enter seed (8 hex digits):                    ">
                      <CURSET 10 <+ .COL 27 .LEN>>)>)
              (<==? .C 8 127>
               <COND (<G? .LEN 0>
                      <SET LEN <- .LEN 1>>
                      <PUTB ,SEED-BUF .LEN 0>
                      ;"Shift 32-bit seed right by 4 bits (erase last hex digit)."
                      <SETG SEED-LO <BOR <LSH ,SEED-LO -4> <LSH ,SEED-HI 12>>>
                      <SETG SEED-HI <LSH ,SEED-HI -4>>
                      <COND (<==? .LEN 0> <SETG SEED-HI 0> <SETG SEED-LO 0>)>)>
               <CURSET 10 .COL>
               <TELL "Enter seed (8 hex digits): ">
               <COND (<G? .LEN 0> <PRINT-SEEDBUF .LEN>)>
               <TELL "                    ">
               <CURSET 10 <+ .COL 27 .LEN>>)
              (<G? <HEXCHAR-TO-NIBBLE .C> -1>
               <COND (<L? .LEN 8>
                      <SET N <HEXCHAR-TO-NIBBLE .C>>
                      <PUTB ,SEED-BUF .LEN <GETB ,HEXCHARS .N>>
                      <SET LEN <+ .LEN 1>>
                      ;"Shift 32-bit seed left by 4 bits and add nibble."
                      <SETG SEED-HI <BOR <LSH ,SEED-HI 4> <LSH ,SEED-LO -12>>>
                      <SETG SEED-LO <BOR <BAND <LSH ,SEED-LO 4> -1> .N>>
                      <CURSET 10 .COL>
                      <TELL "Enter seed (8 hex digits): ">
                      <PRINT-SEEDBUF .LEN>
                      <TELL "                    ">
                      <CURSET 10 <+ .COL 27 .LEN>>)>)>
        <AGAIN>>
    <CLEAR -1>
    <RTRUE>>

<GLOBAL SEED-BUF <ITABLE 8 (BYTE) 0>>

<GLOBAL HEXCHARS
    <PTABLE (BYTE) !\0 !\1 !\2 !\3 !\4 !\5 !\6 !\7 !\8 !\9 !\a !\b !\c !\d !\e !\f>>

;"Converts a hex digit keystroke into a nibble value (0..15), or -1 if invalid."

<ROUTINE HEXCHAR-TO-NIBBLE (C)
    <COND (<AND <G=? .C !\0> <L=? .C !\9>> <- .C !\0>)
          (<AND <G=? .C !\A> <L=? .C !\F>> <+ 10 <- .C !\A>>)
          (<AND <G=? .C !\a> <L=? .C !\f>> <+ 10 <- .C !\a>>)
          (ELSE -1)>>

;"Prints the current 32-bit seed in hex.

Args:
  LEN: Prints only the lowest LEN hex digits (useful for live input).

Returns:
  T."

<ROUTINE PRINT-SEEDBUF (LEN)
    <DO (I 1 .LEN) <PRINTC <GETB ,SEED-BUF <- .I 1>>>>
    <RTRUE>>

<ROUTINE PRINT-HEX16 (W "AUX" N)
    <SET N <BAND <LSH .W -12> 15>>
    <PRINTC <GETB ,HEXCHARS .N>>
    <SET N <BAND <LSH .W -8> 15>>
    <PRINTC <GETB ,HEXCHARS .N>>
    <SET N <BAND <LSH .W -4> 15>>
    <PRINTC <GETB ,HEXCHARS .N>>
    <SET N <BAND .W 15>>
    <PRINTC <GETB ,HEXCHARS .N>>
    <RTRUE>>

<ROUTINE PRINT-SEED-HEX32 ()
    <PRINT-HEX16 ,SEED-HI>
    <PRINT-HEX16 ,SEED-LO>
    <RTRUE>>

;"Main loop and game end"

;"Checks and applies end-of-game conditions (currently: player HP <= 0).

Args:
    (none)

Returns:
    (none)"

<ROUTINE CHECK-END ()
    <COND (<L=? ,PLAYER-HP 0>
           <SETG PLAYER-HP 0>
           <SETG GAME-OVER? T>
           <SETG YOU-WIN? <>>
           <COND (,DEATH-LOGGED? <RTRUE>)>
           <SETG DEATH-LOGGED? T>
           <LOG "You died on floor " N ,CURRENT-FLOOR ". Final score: " N <FINAL-SCORE>>
           <IF-DEBUG <COND (,DEBUG-USED? <LOG "*">)>>
           <LOG ". Press Q to quit, R to restart." CR>)
          (<AND ,YOU-WIN? ,GAME-OVER?> <VICTORY>)>>

"Gameplay loop and input"

;"Main game loop. Each iteration clears messages, reads one character, and
    either handles input (movement/stairs/wait) or advances the enemies."

<ROUTINE RASCAL-MAIN-LOOP ("AUX" C)
    <REPEAT ()
        <SET C <GETCHAR>>
        <COND (,GAME-OVER?
               ;"After game over, Q exits and R restarts; ignore everything else."
               <COND (<==? .C !\Q !\q> <RETURN>) (<==? .C !\R !\r> <RESTART>)>
               <DRAW>)
              (ELSE
               <COND (<==? .C !\Q !\q>
                      <SET C <POPUP-QUIT-CONFIRM-GETCHAR>>
                      <COND (<==? .C !\Y !\y>
                             <COLOR 1 1>
                             <CLEAR -1>
                             <TELL "Thanks for playing." CR>
                             <RETURN>)
                            (<==? .C !\R !\r> <RESTART>)
                            (ELSE
                             <LOG "Never mind." CR>
                             <DRAW>
                             <AGAIN>)>)>
               <COND (<HANDLE-INPUT .C> <DRAW> <AGAIN>)>
               <STEP-ENEMIES>
               <SETG STATS-TURNS <+ ,STATS-TURNS 1>>
               <UPDATE-BEE-SWARM>
               <IF-DEBUG <APPLY-IMMORTAL>>
               <TICK-POTION-TIMERS>
               <REVEAL-AROUND ,PLAYER-X ,PLAYER-Y>
               <CHECK-END>
               <DRAW>)>>>

;"Draws a quit confirmation popup in the upper window, reads one key,
  clears the popup with spaces, then redraws the game UI.

Returns:
  ZSCII character code from GETCHAR."
<ROUTINE POPUP-QUIT-CONFIRM-GETCHAR ("AUX" W INNERW H TOP LEFT C CLEARCNT)
    ;"Size and position the box roughly centered in the upper window."
    <SET W 50>
    <COND (<G? .W <- <LOWCORE SCRH> 4>> <SET W <- <LOWCORE SCRH> 4>>)>
    <COND (<L? .W 34> <SET W 34>)>
    <SET INNERW <- .W 2>>

    ;"Top border + prompt + bottom border."
    <SET H 3>
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

    ;"Prompt line."
    <CURSET <+ .TOP 1> .LEFT>
    <PRINTC !\|>
    <PRINTC-REPEAT !\  .INNERW>
    <CURSET <+ .TOP 1> <+ .LEFT 2>>
    <TELL "Really quit? (Y/N, R to restart)">
    <CURSET <+ .TOP 1> <+ .LEFT <- .W 1>>>
    <PRINTC !\|>

    ;"Bottom border."
    <CURSET <+ .TOP 2> .LEFT>
    <PRINTC !\+>
    <PRINTC-REPEAT !\- .INNERW>
    <PRINTC !\+>

    <PROG ()
        <SET C <GETCHAR>>
        <COND (<==? .C 254 ;"mouse click"> <AGAIN>)>>

    ;"Clear the popup with spaces."
    <SET CLEARCNT <- .H 1>>
    <DO (I 0 .CLEARCNT)
        <CURSET <+ .TOP .I> .LEFT>
        <PRINTC-REPEAT !\  .W>>

    ;"Restore the UI immediately. (Main loop may redraw again; that's fine.)"
    <DRAW>

    .C>

<CONSTANT ATSIGN-COLOR <RGB 10 25 25>>
<CONSTANT CROWN-COLOR <RGB 24 19 0>>
<CONSTANT JEWEL-COLOR-1 <RGB 24 4 0>>
<CONSTANT JEWEL-COLOR-2 <RGB 4 24 0>>
<CONSTANT TROPHY-COLOR <RGB 28 23 0>>

<ROUTINE VICTORY ("AUX" COL C)
    <SPLIT <LOWCORE SCRV>>
    <SCREEN 1>
    <COLOR 1 1>
    <CLEAR -2>
    <SET COL </ <- <LOWCORE SCRH> 40> 2>>
    <CURSET 1 .COL><TELL "__   __           __        ___       _ ">
    <CURSET 2 .COL><TELL "\\ \\ / /__  _   _  \\ \\      / (_)_ __ | |">
    <CURSET 3 .COL><TELL " \\ V / _ \\| | | |  \\ \\ /\\ / /| | '_ \\| |">
    <CURSET 4 .COL><TELL "  | | (_) | |_| |   \\ V  V / | | | | |_|">
    <CURSET 5 .COL><TELL "  |_|\\___/ \\__,_|    \\_/\\_/  |_|_| |_(_)">
    <CURSET 6 .COL><TELL "                                        ">
    <CTCOLOR ,ATSIGN-COLOR -1>
    <CURSET 7 .COL><TELL "       o   O   o                        ">
    <CURSET 8 .COL><TELL "       |\\_/ \\_/|                        ">
    <CURSET 9 .COL><TELL "      .\\+__+__+/.                       ">
    <CURSET 10 .COL><TELL "     d8*\"     `\"8N.    ">
    <CURSET 11 .COL><TELL "   .@8F   .ucu.. %8L   ">
    <CURSET 12 .COL><TELL "   @8E  .@8\"\"988  8N   ">
    <CURSET 13 .COL><TELL "  '88>  @8~  98F  98   ">
    <CURSET 14 .COL><TELL "  ~88  X8E   98~  8E   ">
    <CURSET 15 .COL><TELL "  '88> 98&  d88  X8    ">
    <CURSET 16 .COL><TELL "   %8N '88W@\"%8ed*`   ">
    <CURSET 17 .COL><TELL "    %8b. `\"   ``       ">
    <CURSET 18 .COL><TELL "     `*8bu.. ..u@      ">
    <CURSET 19 .COL><TELL "        ^\"***%\"`        ">
    <CURSET 20 .COL><TELL "                                        ">
    <COLOR 1 1>
    <CURSET 21 .COL><TELL "  You escape with the Trophy of Scryra! ">
    <CURSET 22 .COL><TELL "           Final score: " N <FINAL-SCORE> "           ">
    <CURSET 23 .COL><TELL "                                        ">
    <CURSET 24 .COL><TELL "    [Press Q to quit, R to restart,     ">
    <CURSET 25 .COL><TELL "         S to see statistics.]          ">

    ;"crown"
    <CURSET 7 <+ .COL 7>><CTCOLOR ,JEWEL-COLOR-1 -1><TELL "o   "><CTCOLOR ,JEWEL-COLOR-2 -1><TELL "O   "><CTCOLOR ,JEWEL-COLOR-1 -1><TELL "o">
    <CTCOLOR ,CROWN-COLOR -1>
    <CURSET 8 <+ .COL 7>><TELL "|\\_/ \\_/|">
    <CURSET 9 <+ .COL 7>><TELL "\\+__+__+/">

    ;"trophy"
    <CTCOLOR ,TROPHY-COLOR -1>
    <CURSET 10 <+ .COL 23>><TELL "  ___________    ">
    <CURSET 11 <+ .COL 23>><TELL " '._==_==_=_.'   ">
    <CURSET 12 <+ .COL 23>><TELL " .-\\:      /-.   ">
    <CURSET 13 <+ .COL 23>><TELL "| (|:.      |) |  ">
    <CURSET 14 <+ .COL 23>><TELL " '-|:.      |-'   ">
    <CURSET 15 <+ .COL 23>><TELL "   \\::.     /     ">
    <CURSET 16 <+ .COL 23>><TELL "    '::. .'      ">
    <CURSET 17 <+ .COL 23>><TELL "      ) (        ">
    <CURSET 18 <+ .COL 23>><TELL "    _.' '._      ">
    <CURSET 19 <+ .COL 23>><TELL "   `\"\"\"\"\"\"\"`     ">

    <COLOR 1 1>
    <SET C <GETCHAR>>
    <COND (<==? .C !\Q !\q> <QUIT>)
          (<==? .C !\R !\r> <RESTART>)
          (<==? .C !\S !\s>
           <GAME-STATS>
           <AGAIN>)
          (ELSE <AGAIN>)>>

;"Input handling"

<CONSTANT UP-KEY 129>
<CONSTANT DOWN-KEY 130>
<CONSTANT LEFT-KEY 131>
<CONSTANT RIGHT-KEY 132>
<CONSTANT NUM0-KEY 145>
<CONSTANT NUM1-KEY 146>
<CONSTANT NUM2-KEY 147>
<CONSTANT NUM3-KEY 148>
<CONSTANT NUM4-KEY 149>
<CONSTANT NUM5-KEY 150>
<CONSTANT NUM6-KEY 151>
<CONSTANT NUM7-KEY 152>
<CONSTANT NUM8-KEY 153>
<CONSTANT NUM9-KEY 154>

;"Interprets a single-character command.

Args:
    C: Input character code (ZSCII).

Returns:
    T if the input consumes the turn without advancing enemies (stairs/UI-only);
    FALSE if enemies should take a turn afterwards (movement/combat/wait)."

<ROUTINE HANDLE-INPUT (C "AUX" DX DY NX NY ENTERTR)
    <SET DX 0>
    <SET DY 0>
    <IF-DEBUG
        ;"Debug keys: require a double-press (F-key twice in a row)."
        <COND (<OR <==? .C ,KEY-F8>
                   <==? .C ,KEY-F9>
                   <==? .C ,KEY-F10>
                   <==? .C ,KEY-F11>
                   <==? .C ,KEY-F12>>
               <COND (<==? ,DEBUG-DOUBLEKEY .C>
                      <SETG DEBUG-DOUBLEKEY 0>
                      <COND (<==? .C ,KEY-F12>
                             <SETG DEBUG-USED? T>
                             <SETG DEBUG-OVERLAY-ONCE? T>
                             <RTRUE>)
                            (<==? .C ,KEY-F8>
                             <SETG DEBUG-USED? T>
                             <DBG-F8-TELEPORT-TO-BUSKER>
                             <RTRUE>)
                            (<==? .C ,KEY-F11>
                             <SETG DEBUG-USED? T>
                             <SETG IMMORTAL? <NOT ,IMMORTAL?>>
                             <LOG "Immortal mode: "
                                  <COND (,IMMORTAL? "ON") (ELSE "OFF")>
                                  "."
                                  CR>
                             <RTRUE>)
                            (<==? .C ,KEY-F10>
                             <SETG DEBUG-USED? T>
                             <SETG OMNISCIENT? <NOT ,OMNISCIENT?>>
                             <LOG "Omniscient mode: "
                                  <COND (,OMNISCIENT? "ON") (ELSE "OFF")>
                                  "."
                                  CR>
                             <RTRUE>)
                            (<==? .C ,KEY-F9>
                             <SETG DEBUG-USED? T>
                             <SETG STAIR-FINDER? <NOT ,STAIR-FINDER?>>
                             <LOG "Stair finder: "
                                  <COND (,STAIR-FINDER? "ON") (ELSE "OFF")>
                                  "."
                                  CR>
                             <RTRUE>)>)
                     (ELSE
                      <SETG DEBUG-DOUBLEKEY .C>
                      <RTRUE>)>)>
        <SETG DEBUG-DOUBLEKEY 0>>
    <COND (<==? .C 62 ;">">
           <IF-DEBUG
               <COND (<AND ,STAIR-FINDER?
                           <N==? <TILE-AT ,PLAYER-X ,PLAYER-Y>
                                 ,TILE-STAIR-DOWN>>
                      <STAIR-FINDER-TELEPORT T>
                      <RTRUE>)>>
           <RETURN <GO-DOWN>>)>
    <COND (<==? .C 60 ;"<">
           <IF-DEBUG
               <COND (<AND ,STAIR-FINDER?
                           <N==? <TILE-AT ,PLAYER-X ,PLAYER-Y> ,TILE-STAIR-UP>>
                      <STAIR-FINDER-TELEPORT <>>
                      <RTRUE>)>>
           <RETURN <GO-UP>>)>
    <COND (<==? .C !\T !\t>
           <COND (<TRY-DROP-INVENTORY> <RFALSE>) (ELSE <RTRUE>)>)>
    <COND (<==? .C !\I !\i>
           <COND (<TRY-INGEST-INVENTORY>
                  <COND (,GAME-OVER? <RTRUE>) (ELSE <RFALSE>)>)
                 (ELSE <RTRUE>)>)>
    <COND (<OR <==? .C !\G> <==? .C !\g>> <TRY-EQUIP-WEAPON> <RFALSE>)>
    <COND (<==? .C !\?>
           <INSTRUCTIONS T>
           <RTRUE>)
          (<==? .C !\A !\a !\H !\h ,LEFT-KEY ,NUM4-KEY> <SET DX -1>)
          (<==? .C !\D !\d !\L !\l ,RIGHT-KEY ,NUM6-KEY> <SET DX 1>)
          (<==? .C !\W !\w !\K !\k ,UP-KEY ,NUM8-KEY> <SET DY -1>)
          (<==? .C !\S !\s !\J !\j ,DOWN-KEY ,NUM2-KEY> <SET DY 1>)
          (<==? .C !\Y !\y ,NUM7-KEY>
           <SET DX -1>
           <SET DY -1>)
          (<==? .C !\U !\u ,NUM9-KEY>
           <SET DX 1>
           <SET DY -1>)
          (<==? .C !\B !\b ,NUM1-KEY>
           <SET DX -1>
           <SET DY 1>)
          (<==? .C !\N !\n ,NUM3-KEY>
           <SET DX 1>
           <SET DY 1>)
          (<==? .C !\. !\ >
           ;"Wait."
           <SETG STATS-WAITS <+ ,STATS-WAITS 1>>
           <RFALSE>)
          (<==? .C !\C !\c>
           <COND (,HAS-TCOLOR? <SETG WANT-TCOLOR? <NOT ,WANT-TCOLOR?>>)>
           <RTRUE>)
          (ELSE <RETURN>)>

    <SET NX <+ ,PLAYER-X .DX>>
    <SET NY <+ ,PLAYER-Y .DY>>
    <SET ENTERTR <TRADER-AT? .NX .NY>>

    <COND (<LOCKED-DOOR-CLOSED-AT? .NX .NY>
           <COND (<TRY-UNLOCK-LOCKED-DOOR .NX .NY> <RTRUE>) (ELSE <RETURN>)>)>

    <COND (<NOT <FLOOR? .NX .NY>>
           <SETG STATS-WALL-BUMPS <+ ,STATS-WALL-BUMPS 1>>
           <RETURN>)>
    <COND (<G? <ENEMY-AT .NX .NY> 0>
           <COND (<AND <==? <GETP <ENEMY-AT .NX .NY> ,P?R-ETYPE> ,ETYPE-MONKEY>
                       <FSET? <ENEMY-AT .NX .NY> ,TAMEBIT>>
                  ;"Swap places with a tamed monkey instead of attacking."
                  <PUTP <ENEMY-AT .NX .NY> ,P?R-X ,PLAYER-X>
                  <PUTP <ENEMY-AT .NX .NY> ,P?R-Y ,PLAYER-Y>
                  <SETG PLAYER-X .NX>
                  <SETG PLAYER-Y .NY>
                  <AFTER-PLAYER-RELOCATE>
                  <RFALSE>)
                 (ELSE
                  <PLAYER-ATTACK <ENEMY-AT .NX .NY>>
                  <COND (<L=? <ENEMY-AT .NX .NY> 0>
                         <SETG PLAYER-X .NX>
                         <SETG PLAYER-Y .NY>)
                        (ELSE <RFALSE>)>)>)>
    <SETG PLAYER-X .NX>
    <SETG PLAYER-Y .NY>
    <AFTER-PLAYER-RELOCATE>
    <COND (<OR <N==? .DX 0> <N==? .DY 0>>
           <SETG STATS-MOVES <+ ,STATS-MOVES 1>>
           <COND (<AND <N==? .DX 0> <N==? .DY 0>>
                  <SETG STATS-DIAG-MOVES <+ ,STATS-DIAG-MOVES 1>>)>)>
    <COND (.ENTERTR <TRADER-SHOP>)>
    <COND (,INTERIOR-LAUNCHED? <RTRUE>) (ELSE <RFALSE>)>>

<ROUTINE TRY-UNLOCK-LOCKED-DOOR (X Y "AUX" DOOR LOCKTYPE)
    <SET DOOR <LOCKEDDOOR-OBJ-AT .X .Y>>
    <COND (<L=? .DOOR 1> <RFALSE>)>
    <COND (<FSET? .DOOR ,OPENBIT> <RTRUE>)>
    <SET LOCKTYPE <GETP .DOOR ,P?R-ITID>>
    <COND (<INV-CONSUME-KEY .LOCKTYPE>
           <FSET .DOOR ,OPENBIT>
           <LOG "Unlocked." CR>
           <RTRUE>)>
    <LOG "The locked door requires a " <KEY-NAME .LOCKTYPE> "." CR>
    <RFALSE>>

"Rendering"

;"Writes a one-line status summary.

Args:
  ROW, COL: Screen position in upper window.
  MODE: 0 for the main HUD header; 1 for trader UI header.

Returns:
  (none)"

<ROUTINE DRAW-STATUS-LINE (ROW COL MODE)
    <CURSET .ROW .COL>
    <UI-RESET>
    <COND (<==? .MODE 1>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "gold=">
           <UI-FG ,UI-RGB-GOLD ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-GOLD>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  hp=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-HP "/" N ,PLAYER-MAX-HP>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  str=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-STR>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  def=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-DEF>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  wpn=">
           <UI-FG ,UI-RGB-WEAPON ,ZCOL-DEFAULT>
           <PRINT-EQUIPPED-WEAPON>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  floor=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,CURRENT-FLOOR "/" N ,MAX-FLOORS>
           <UI-RESET>)
          (ELSE
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "gold=">
           <UI-FG ,UI-RGB-GOLD ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-GOLD>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  floor=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,CURRENT-FLOOR "/" N ,MAX-FLOORS>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  hp=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-HP "/" N ,PLAYER-MAX-HP>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  str=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-STR>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  def=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N ,PLAYER-DEF>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  wpn=">
           <UI-FG ,UI-RGB-WEAPON ,ZCOL-DEFAULT>
           <PRINT-EQUIPPED-WEAPON>
           <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
           <TELL "  inv=">
           <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
           <TELL N <INV-COUNT> "/" N ,INV-SIZE>
           <UI-RESET>)>>

;"Redraws the entire upper-window UI (header, controls, map, messages)."

<ROUTINE DRAW ()
    <SCREEN 1>
    <COLOR 1 1>
    <CTCOLOR -1 0>
    <CLEAR 1>
    <DRAW-HEADER>
    <DRAW-CONTROLS>
    <DRAW-MAP>
    <IF-DEBUG
        <COND (,DEBUG-OVERLAY-ONCE?
               <SETG DEBUG-OVERLAY-ONCE? <>>
               <DRAW-DEBUG-OVERLAY>)>>
    <COND (,HIT-FLASH?
           <SETG HIT-FLASH? <>>
           <SETG HIT-FLASH-EX 0>
           <SETG HIT-FLASH-EY 0>)>>

<IF-DEBUG
    ;"Draws a one-shot debug overlay (in red) showing all enemies and stairs,
    regardless of fog-of-war. Triggered by F12 (key code 144)."

    <ROUTINE DRAW-DEBUG-OVERLAY ("AUX" UPX UPY DOWNX DOWNY HP X Y ROW COL O TYPE)
        <UI-ALERT>
        ;"Stairs (from per-floor persisted positions)."
        <SET UPX <FLOOR-UP-X ,CURRENT-FLOOR>>
        <SET UPY <FLOOR-UP-Y ,CURRENT-FLOOR>>
        <COND (<AND <G? .UPX 0> <G? .UPY 0>>
               <SET ROW <+ ,MAP-ROW <- .UPY 1>>>
               <SET COL <+ ,MAP-COL <- .UPX 1>>>
               <CURSET .ROW .COL>
               <PRINTC ,TILE-STAIR-UP>)>
        <SET DOWNX <FLOOR-DOWN-X ,CURRENT-FLOOR>>
        <SET DOWNY <FLOOR-DOWN-Y ,CURRENT-FLOOR>>
        <COND (<AND <G? .DOWNX 0> <G? .DOWNY 0>>
               <SET ROW <+ ,MAP-ROW <- .DOWNY 1>>>
               <SET COL <+ ,MAP-COL <- .DOWNX 1>>>
               <CURSET .ROW .COL>
               <PRINTC ,TILE-STAIR-DOWN>)>
        ;"Enemies (from per-floor enemy objects)."
        <SET O <FIRST? ,CURRENT-FLOOR-OBJ>>
        <REPEAT ()
            <COND (<NOT .O> <RETURN>)>
            <SET TYPE <GETP .O ,P?R-ETYPE>>
            <SET HP <GETP .O ,P?R-EHP>>
            <COND (<AND <G? .TYPE 0> <G? .HP 0>>
                <SET X <GETP .O ,P?R-X>>
                <SET Y <GETP .O ,P?R-Y>>
                <COND (<AND <G? .X 0> <G? .Y 0>>
                       <SET ROW <+ ,MAP-ROW <- .Y 1>>>
                       <SET COL <+ ,MAP-COL <- .X 1>>>
                       <CURSET .ROW .COL>
                       <PRINTC <ENEMY-TILE-FOR-TYPE .TYPE>>)>)>
            <SET O <NEXT? .O>>>
        <COLOR 1 0>>
>

;"Draws the status/header line (seed/floor/hp/gold/room)."

<ROUTINE DRAW-HEADER ()
    <DRAW-STATUS-LINE ,HEADER-ROW 1 0>
    <IF-DEBUG
        <COND (,DEBUG-OVERLAY-ONCE?
               <TELL "  room=" N ,DISCOVERED-ROOMS "/" N ,ROOM-COUNT>)>>>

;"Draws the controls/help line."

<ROUTINE DRAW-CONTROLS ()
    <CURSET ,CONTROLS-ROW 1>
    ;"two spaces after '.' get collapsed into one"
    <UI-RESET>
    <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
    <TELL "seed=">
    <UI-FG ,UI-RGB-TEXT ,ZCOL-DEFAULT>
    <PRINT-SEED-HEX32>
    <UI-FG ,UI-RGB-LABEL ,ZCOL-DEFAULT>
    <TELL "  Wait: .   Drop: T  Ingest: I  Equip: G  Quit: Q  Instructions: ?">
    <UI-RESET>>

;"Draws the visible map area (MAP-H rows by MAP-W columns)."

<ROUTINE DRAW-MAP ("AUX" X Y ROW COL SPR)
    <SET Y 1>
    <REPEAT ()
        <COND (<G? .Y ,MAP-H> <RETURN>)>
        <SET ROW <+ ,MAP-ROW <- .Y 1>>>
        <SET X 1>
        <REPEAT ()
            <COND (<G? .X ,MAP-W> <SET Y <+ .Y 1>> <RETURN>)>
            <SET COL <+ ,MAP-COL <- .X 1>>>
            <CURSET .ROW .COL>
            <SET SPR <SPRITE .X .Y>>
            <COND (<AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>>
                   <COND (,HIT-FLASH? <HLIGHT ,H-INVERSE>)>
                   <COND (<G? ,PLAYER-INVIS-TURNS 0>
                          <UI-FG ,UI-RGB-PLAYER-INVIS ,ZCOL-CYAN>
                          <PRINTC .SPR>
                          <UI-RESET>)
                         (ELSE
                          <APPLY-SPRITE-COLOR .SPR>
                          <PRINTC .SPR>)>
                   <COND (,HIT-FLASH? <HLIGHT ,H-NORMAL>)>)
                  (<AND <G? ,PLAYER-VISION-TURNS 0> <G? <ENEMY-AT .X .Y> 0>>
                   <UI-ALERT>
                   <PRINTC .SPR>
                   <UI-RESET>)
                  (<AND ,HIT-FLASH?
                        <==? .X ,HIT-FLASH-EX>
                        <==? .Y ,HIT-FLASH-EY>
                        <G? <ENEMY-AT .X .Y> 0>>
                   <UI-ALERT>
                   <HLIGHT ,H-INVERSE>
                   <PRINTC .SPR>
                   <HLIGHT ,H-NORMAL>
                   <UI-RESET>)
                  (ELSE
                   <APPLY-SPRITE-COLOR .SPR>
                   <PRINTC .SPR>)>
            <SET X <+ .X 1>>>>>

;"Returns true if (X, Y) is within the player's shadow-limited vision.

While PLAYER-SHADOW-TURNS is active, the player can only see the 8 neighboring
tiles (and their own tile).

Args:
  X, Y: Map coordinates.

Returns:
  T if within shadow vision; FALSE otherwise."

<ROUTINE SHADOW-VISIBLE? (X Y)
    <AND <L=? <ABS <- .X ,PLAYER-X>> 1> <L=? <ABS <- .Y ,PLAYER-Y>> 1>>>

;"Returns the character to draw at (X, Y), including overlays:
    player, fog-of-war, enemies, and gold.

Args:
  X, Y: Map coordinates.

Returns:
  ZSCII character constant (TILE-*)."

<ROUTINE SPRITE (X Y)
    <COND (<AND <==? .X ,PLAYER-X> <==? .Y ,PLAYER-Y>> ,TILE-PLAYER)
          (<AND <G? ,PLAYER-VISION-TURNS 0> <G? <ENEMY-AT .X .Y> 0>>
           <ENEMY-TILE-FOR-TYPE <GETP <ENEMY-AT .X .Y> ,P?R-ETYPE>>)
          (<AND <G? ,PLAYER-SHADOW-TURNS 0>
                <OR <NOT <IF-DEBUG ,OMNISCIENT?>> <==? <TILE-AT .X .Y> ,TILE-WALL>>
                <NOT <SHADOW-VISIBLE? .X .Y>>>
           ,TILE-UNKNOWN)
          (<AND <OR <NOT <IF-DEBUG ,OMNISCIENT?>> <==? <TILE-AT .X .Y> ,TILE-WALL>>
                <NOT <REVEALED? .X .Y>>>
           ,TILE-UNKNOWN)
          (<G? <ENEMY-AT .X .Y> 0>
           <ENEMY-TILE-FOR-TYPE <GETP <ENEMY-AT .X .Y> ,P?R-ETYPE>>)
          (<TRADER-AT? .X .Y> ,TILE-TRADER)
          (<G? <KEY-OBJ-AT .X .Y> 0> ,TILE-KEY)
          (<POTION-AT? .X .Y> ,TILE-POTION)
          (<G? <TREASURE-AT? .X .Y> 0> ,TILE-TREASURE)
          (<G? <WEAPON-OBJ-AT .X .Y> 0> ,TILE-WEAPON)
          (<G? <FOOD-OBJ-AT .X .Y> 0>
           <FOOD-TILE-FOR-TYPE <GETP <FOOD-OBJ-AT .X .Y> ,P?R-ITID>>)
          (<G? <GOLD-OBJ-AT .X .Y> 0> ,TILE-GOLD)
          (<AND <==? <TILE-AT .X .Y> ,TILE-DOOR>
                <LOCKED-DOOR-CLOSED-AT? .X .Y>>
           ,TILE-LOCKEDDOOR)
          (ELSE <TILE-AT .X .Y>)>>
