"Dragon sample for ZILF"

<VERSION EZIP>
<CONSTANT RELEASEID 1>

<CONSTANT GAME-BANNER
"Dragon|
A Dice-Based Battle|
By Tara McGrew">

<ROUTINE GO ()
    <CRLF>
    <CRLF>
    <TELL "The war wizard ushers you into the keep's armory. "
          "A dragon coils in the eastern lair, daring you to claim its hoard."
          CR CR>
    <INIT-STATUS-LINE>
    <V-VERSION> <CRLF>
    <SETG DRAGON-HP 30>
    <SETG HERO-HP 12>
    <SETG HERE ,ARMORY>
    <MOVE ,PLAYER ,HERE>
    <V-LOOK>
    <MAIN-LOOP>>

<INSERT-FILE "parser">

<SYNTAX ATTACK OBJECT (FIND ATTACKBIT) WITH OBJECT (FIND TOOLBIT) = V-ATTACK>

<PROPDEF DICE 1>
<PROPDEF SIDES 4>
<PROPDEF BONUS 0>
<PROPDEF TOHIT 0>

<GLOBAL DRAGON-HP 30>
<GLOBAL HERO-HP 12>
<CONSTANT DRAGON-AC 16>

<ROUTINE DESCRIBE-WEAPON (W)
    <TELL "It promises " <GETP .W ,P?TEXT> "." CR>>

<ROUTINE WEAPON-R ()
    <COND (<VERB? EXAMINE>
           <DESCRIBE-WEAPON ,PRSO>
           <RTRUE>)
          (ELSE <RFALSE>)>>

<OBJECT DRAGON
    (DESC "ancient dragon")
    (SYNONYM DRAGON WYRM)
    (ADJECTIVE ANCIENT)
    (IN LAIR)
    (FDESC "An ancient dragon coils protectively around a glittering hoard.")
    (FLAGS PERSONBIT ATTACKBIT)
    (ACTION DRAGON-R)>

<ROUTINE DRAGON-STATUS ()
    <COND (<L=? ,DRAGON-HP 0>
            <TELL "The dragon lies mortally wounded but still smolders with residual heat." CR>)
          (<L=? ,DRAGON-HP 10>
            <TELL "Deep gashes stripe the dragon's scales." CR>)
          (<L=? ,DRAGON-HP 20>
            <TELL "Soot and blood mark the dragon's chest." CR>)
          (ELSE
            <TELL "The dragon watches you with unblinking, uninjured eyes." CR>)>>

<ROUTINE DRAGON-R ()
    <COND (<VERB? EXAMINE>
           <TELL "The dragon's wings scrape the cavern ceiling, smoke wisping from its nostrils." CR>
           <DRAGON-STATUS>
           <RTRUE>)
          (<VERB? TAKE>
           <TELL "You seize nothing; the dragon recoils and snarls." CR>
           <RTRUE>)
          (<VERB? ATTACK>
           <COND (<L=? ,DRAGON-HP 0>
                   <TELL "The dragon is already defeated." CR>
                   <RTRUE>)
                 (<OR <==? ,PRSI ,MANY-OBJECTS>
                      ;"use GWIM to try to find a weapon"
                      <AND <NOT ,PRSI> <NOT <SETG PRSI <GWIM ,TOOLBIT -1 ,PR?WITH>>>>>
                   <TELL "With what weapon?" CR>
                   <RTRUE>)
                 (<NOT <IN? ,PRSI ,WINNER>>
                   <TELL "You must be holding " T ,PRSI " first." CR>
                   <RTRUE>)
                 (<NOT <FSET? ,PRSI ,TOOLBIT>>
                   <TELL "That isn't forged for battle with dragons." CR>
                   <RTRUE>)
                 (ELSE
                  <PROG ((HITBONUS <GETP ,PRSI ,P?TOHIT>)
                         (ROLL <+ <RANDOM 20> .HITBONUS>))
                      <TELL "You ready " T ,PRSI " and charge!" CR>
                      <COND (<G? .ROLL ,DRAGON-AC>
                             <PROG ((DICE <GETP ,PRSI ,P?DICE>)
                                    (SIDES <GETP ,PRSI ,P?SIDES>)
                                    (BONUS <GETP ,PRSI ,P?BONUS>)
                                    (DMG 0))
                                 <COND (<L=? .DICE 0> <SET DICE 1>)>
                                 <DO (I 1 .DICE)
                                     <SET DMG <+ .DMG <RANDOM .SIDES>>>>
                                 <SET DMG <+ .DMG .BONUS>>
                                 <SETG DRAGON-HP <- ,DRAGON-HP .DMG>>
                                 <COND (<L=? ,DRAGON-HP 0>
                                        <TELL "Your strike pierces the dragon's heart." CR>
                                        <JIGS-UP "The dragon shudders once before collapsing. The hoard is yours!">)
                                       (ELSE
                                        <TELL "The blow lands; the dragon roars in fury." CR>
                                        <PROG ((RETALIATION <+ <RANDOM 6> 2>)
                                                (REMAIN <- ,HERO-HP .RETALIATION>))
                                             <SETG HERO-HP .REMAIN>
                                             <COND (<L=? .REMAIN 0>
                                                    <TELL "Flames engulf you as the dragon counterattacks." CR>
                                                    <JIGS-UP "You fall beneath the dragon's fire. The keep mourns.">)
                                                   (ELSE
                                                    <TELL "You stumble back through the flames, badly singed." CR>)>>)>>)
                            (ELSE
                             <TELL "The dragon slips aside; your swing bites only air." CR>
                             <PROG ((RETALIATION <+ <RANDOM 6> 1>)
                                     (REMAIN <- ,HERO-HP .RETALIATION>))
                                  <SETG HERO-HP .REMAIN>
                                  <COND (<L=? .REMAIN 0>
                                         <TELL "The dragon's tail smash sends you sprawling." CR>
                                         <JIGS-UP "Your quest ends beneath iron-hard scales.">)
                                        (ELSE
                                         <TELL "Claws rake across your armor, but you fight on." CR>)>>)>
                      <RTRUE>>)>)
          (ELSE <RFALSE>)>>

<ROOM ARMORY
    (DESC "Armory")
    (IN ROOMS)
    (LDESC "Weapon racks line the walls of this cramped stone room. An archway leads east into the dragon's lair.")
    (EAST TO LAIR)
    (FLAGS LIGHTBIT)>

<OBJECT RACK
    (DESC "weapon rack")
    (SYNONYM RACK)
    (ADJECTIVE STONE)
    (IN ARMORY)
    (FDESC "A stone rack presents a small selection of weapons.")
    (FLAGS SURFACEBIT CONTBIT)>

<OBJECT RUSTED-SWORD
    (DESC "rusted sword")
    (SYNONYM SWORD BLADE)
    (ADJECTIVE RUSTED)
    (IN RACK)
    (FDESC "A rusted sword lies on the rack.")
    (FLAGS TAKEBIT TOOLBIT)
    (ACTION WEAPON-R)
    (TEXT "1d6 damage, +1 to hit")
    (DICE 1)
    (SIDES 6)
    (BONUS 0)
    (TOHIT 1)>

<OBJECT WARHAMMER
    (DESC "dwarven warhammer")
    (SYNONYM HAMMER WARHAMMER)
    (ADJECTIVE WEIGHTY DWARVEN WAR)
    (IN RACK)
    (FDESC "A weighty dwarven warhammer balances against the stone.")
    (FLAGS TAKEBIT TOOLBIT)
    (ACTION WEAPON-R)
    (TEXT "1d8+1 damage, +2 to hit")
    (DICE 1)
    (SIDES 8)
    (BONUS 1)
    (TOHIT 2)>

<OBJECT SPEAR
    (DESC "gleaming spear")
    (SYNONYM SPEAR LANCE)
    (ADJECTIVE GLEAMING)
    (IN RACK)
    (FDESC "A gleaming spear hums softly with enchantment.")
    (FLAGS TAKEBIT TOOLBIT)
    (ACTION WEAPON-R)
    (TEXT "2d6 damage, +3 to hit")
    (DICE 2)
    (SIDES 6)
    (BONUS 0)
    (TOHIT 3)>

<ROOM LAIR
    (DESC "Dragon's Lair")
    (IN ROOMS)
    (LDESC "The cavern opens wide, scorched black by countless blasts of flame. The armory lies to the west; a mound of treasure sparkles beyond the dragon.")
    (WEST TO ARMORY)
    (FLAGS LIGHTBIT)
    (ACTION LAIR-R)>

<ROUTINE LAIR-R (RARG)
    <COND (<==? .RARG ,M-ENTER>
            <COND (<L=? ,DRAGON-HP 0>
                    <TELL "The slain dragon sprawls beside its hoard." CR>)
                  (ELSE
                    <TELL "The dragon's gaze fixes on you as you enter." CR>)>)
          (ELSE <RFALSE>)>>

<OBJECT HOARD
    (DESC "dragon hoard")
    (SYNONYM HOARD COINS JEWELS WEALTH)
    (ADJECTIVE GOLD DRAGON\'S GLITTERING TREASURE)
    (IN LAIR)
    (FDESC "Coins and jewels spill around the dragon like a molten sea.")
    (ACTION HOARD-R)>

<ROUTINE HOARD-R ()
    <COND (<VERB? EXAMINE>
            <COND (<L=? ,DRAGON-HP 0>
                    <TELL "The treasure is finally yours for the taking." CR>)
                  (ELSE
                    <TELL "The dragon's tail coils protectively around its wealth." CR>)>
            <RTRUE>)
          (<VERB? TAKE>
            <COND (<L=? ,DRAGON-HP 0>
                    <TELL "You scoop up a handful of glittering trophies. Your legend begins!" CR>
                    <JIGS-UP "With the dragon slain, the realm hails you as champion.">)
                  (ELSE
                    <TELL "The dragon lashes out before you can lay a finger on its hoard." CR>)>
            <RTRUE>)
          (ELSE <RFALSE>)>>
