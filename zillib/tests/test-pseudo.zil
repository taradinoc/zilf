<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (NORTH TO HALLWAY)
    (THINGS (DIRTY MESSY RATTY) (FLOOR CARPET GROUND) FLOOR-F
            <>                  LIGHT                 LIGHT-F
            <>                  (COUCH SOFA)          "It's what you sit on."
            (FRONT BACK)        DOOR                  "It leads out."
            GARBAGE             (CAN BIN)             GARBAGE-F
            LIGHT               SWITCH                SWITCH-F
            BORING              THING                 <>
            NEEDLEWORK          SAMPLER               ([READ] "Home is where the player starts."))
    (FLAGS LIGHTBIT)>

<ROUTINE FLOOR-F ()
    <COND (<VERB? EXAMINE> <TELL "You're standing on it." CR>)>>

<ROUTINE LIGHT-F ()
    <COND (<VERB? EXAMINE> <TELL "It illuminates the room." CR>)>>

<ROUTINE GARBAGE-F ()
    <COND (<VERB? EXAMINE> <TELL "It's where you put garbage." CR>)>>

<ROUTINE SWITCH-F ()
    <COND (<VERB? EXAMINE> <TELL "It turns the light on and off." CR>)>>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS TAKEBIT)>

<OBJECT HALLWAY
    (IN ROOMS)
    (DESC "Hallway")
    (SOUTH TO STARTROOM)
    (FLAGS LIGHTBIT)>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,PLAYER ,STARTROOM>>

<TEST-CASE ("Examine pseudo objects")
    <COMMAND [EXAMINE MESSY FLOOR]>
    <EXPECT "You're standing on it.|">
    <COMMAND [EXAMINE DIRTY GROUND]>
    <EXPECT "You're standing on it.|">
    <COMMAND [EXAMINE FLOOR]>
    <EXPECT "You're standing on it.|">
    <COMMAND [EXAMINE LIGHT]>
    <EXPECT "It illuminates the room.|">
    <COMMAND [EXAMINE COUCH]>
    <EXPECT "It's what you sit on.|">
    <COMMAND [EXAMINE SOFA]>
    <EXPECT "It's what you sit on.|">
    <COMMAND [EXAMINE FRONT DOOR]>
    <EXPECT "It leads out.|">
    <COMMAND [EXAMINE DOOR]>
    <EXPECT "It leads out.|">
    <COMMAND [EXAMINE GARBAGE CAN]>
    <EXPECT "It's where you put garbage.|">
    <COMMAND [EXAMINE BIN]>
    <EXPECT "It's where you put garbage.|">
    <COMMAND [EXAMINE BORING]>
    <EXPECT "You see nothing special about that.|">
    <COMMAND [EXAMINE SAMPLER]>
    <EXPECT "You see nothing special about that.|">
    <COMMAND [READ SAMPLER]>
    <EXPECT "Home is where the player starts.|">
    <COMMAND [READ IT]>
    <EXPECT "Home is where the player starts.|">
    <COMMAND [AGAIN]>
    <EXPECT "Home is where the player starts.|">
    <COMMAND [NORTH]>
    <COMMAND [READ IT]>
    <EXPECT "That is no longer here.|">
    <COMMAND [PRONOUNS]>
    <EXPECT "IT means some scenery in Start Room.|THEM means nothing.|
HIM means nothing.|HER means nothing.|">>

<TEST-GO ,STARTROOM>
