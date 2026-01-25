<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<OBJECT BANANA
    (IN STARTROOM)
    (DESC "banana")
    (SYNONYM BANANA)
    (FLAGS TAKEBIT EDIBLEBIT)>

<OBJECT HAT
    (IN STARTROOM)
    (DESC "hat")
    (SYNONYM HAT)
    (FLAGS TAKEBIT WEARBIT)>

<OBJECT CAGE
    (IN STARTROOM)
    (DESC "cage")
    (SYNONYM CAGE)
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT TAKEBIT)>

<OBJECT BOX
    (IN STARTROOM)
    (DESC "box")
    (SYNONYM BOX)
    (FLAGS CONTBIT OPENABLEBIT TAKEBIT)>

<OBJECT DESK
    (IN STARTROOM)
    (DESC "desk")
    (SYNONYM DESK)
    (FLAGS SURFACEBIT)>

<OBJECT BUCKET
    (IN STARTROOM)
    (DESC "bucket")
    (SYNONYM BUCKET)
    (FLAGS CONTBIT OPENBIT TAKEBIT)>

<OBJECT GLASS-CASE
    (IN STARTROOM)
    (DESC "glass case")
    (SYNONYM CASE)
    (ADJECTIVE GLASS)
    (CONTFCN GLASS-CASE-CONTFCN)
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT)>

<ROUTINE GLASS-CASE-CONTFCN (ARG)
    <COND (<AND <==? .ARG ,M-BLOCKER> <VERB? BURN>>
           <RETURN -1>)>>

<OBJECT CRYSTAL-CASE
    (DESC "crystal case")
    (SYNONYM CASE)
    (ADJECTIVE CRYSTAL)
    (CONTFCN CRYSTAL-CASE-CONTFCN)
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT)>

<ROUTINE CRYSTAL-CASE-CONTFCN (ARG)
    <COND (<==? .ARG ,M-BLOCKER>
           <COND (<VERB? BURN> <TELL "The crystal case is fireproof." CR>)>)>>

<OBJECT PLASTIC-CASE
    (DESC "plastic case")
    (SYNONYM CASE)
    (ADJECTIVE PLASTIC)
    (CONTFCN PLASTIC-CASE-CONTFCN)
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT)>

<ROUTINE PLASTIC-CASE-CONTFCN (ARG)
    <COND (<==? .ARG ,M-BLOCKER> -1)>>

<OBJECT DIAMOND
    (IN GLASS-CASE)
    (DESC "diamond")
    (SYNONYM DIAMOND)
    (ACTION DIAMOND-F)
    (FLAGS TAKEBIT)>

<ROUTINE DIAMOND-F ()
    <COND (<VERB? EXAMINE> <TELL "Shiny." CR>)
          (<VERB? RUB> <TELL "It cuts your finger, and you fall into a thousand-year slumber. You wake up refreshed." CR>)
          (<VERB? BURN> <TELL "You focus your thoughts on the diamond, and it bursts into flames." CR>)>>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,BANANA ,STARTROOM>
    <MOVE ,HAT ,STARTROOM>
    <FCLEAR ,HAT ,WORNBIT>
    <MOVE ,CAGE ,STARTROOM>
    <FCLEAR ,CAGE ,OPENBIT>
    <MOVE ,BOX ,STARTROOM>
    <FCLEAR ,BOX ,OPENBIT>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>
    <FSET ,BUCKET ,OPENBIT>
    <MOVE ,GLASS-CASE ,STARTROOM>
    <FCLEAR ,GLASS-CASE ,OPENBIT>
    <MOVE ,DIAMOND ,GLASS-CASE>
    <REMOVE ,CRYSTAL-CASE>
    <REMOVE ,PLASTIC-CASE>>

<TEST-CASE ("Open container, revealing contents")
    <MOVE ,APPLE ,BOX>
    <COMMAND [OPEN BOX]>
    <EXPECT "You open the box.|In the box is an apple.|">>

<TEST-CASE ("Take item from closed container")
    <MOVE ,APPLE ,CAGE>
    <COMMAND [TAKE APPLE]>
    <EXPECT "The cage is in the way.|">
    <COMMAND [TAKE CAGE]>
    <EXPECT "You pick up the cage.|">
    <COMMAND [TAKE APPLE]>
    <EXPECT "The cage is in the way.|">>

<TEST-CASE ("Interact with item in closed transparent container")
    <COMMAND [EXAMINE DIAMOND]>
    <EXPECT "Shiny.|">
    <COMMAND [TAKE DIAMOND]>
    <EXPECT "The glass case is in the way.|">
    <COMMAND [RUB DIAMOND]>
    <EXPECT "You can't reach the diamond.|">
    <COMMAND [BURN DIAMOND]>
    <EXPECT "You focus your thoughts on the diamond, and it bursts into flames.|">>

<TEST-CASE ("Interact with item in nested closed transparent container")
    <MOVE ,CRYSTAL-CASE ,GLASS-CASE>
    <MOVE ,PLASTIC-CASE ,CRYSTAL-CASE>
    <MOVE ,DIAMOND ,PLASTIC-CASE>
    <COMMAND [TAKE DIAMOND]>
    <EXPECT "The glass case is in the way.|">
    <COMMAND [RUB DIAMOND]>
    <EXPECT "You can't reach the diamond.|">
    <COMMAND [BURN DIAMOND]>
    <EXPECT "The crystal case is fireproof.|">
    <MOVE ,PLASTIC-CASE ,GLASS-CASE>
    <COMMAND [EXAMINE DIAMOND]>
    <EXPECT "Shiny.|">
    <COMMAND [TAKE DIAMOND]>
    <EXPECT "The glass case is in the way.|">
    <COMMAND [RUB DIAMOND]>
    <EXPECT "You can't reach the diamond.|">
    <COMMAND [BURN DIAMOND]>
    <EXPECT "You focus your thoughts on the diamond, and it bursts into flames.|">>

<TEST-GO ,STARTROOM>
