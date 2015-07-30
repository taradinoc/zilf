<VERSION ZIP>

<SETG NEW-VOC? T>

<INSERT-FILE "parser">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (NORTH TO HALLWAY)
    (FLAGS LIGHTBIT)>

<OBJECT HALLWAY
    (IN ROOMS)
    (DESC "Hallway")
    (NORTH TO CLOSET)
    (SOUTH TO STARTROOM)
    (FLAGS LIGHTBIT)>

<OBJECT CLOSET
    (IN ROOMS)
    (DESC "Closet")
    (SOUTH TO HALLWAY)
    (FLAGS LIGHTBIT)>

<INSERT-FILE "testing">

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,WINNER ,STARTROOM>>

<TEST-CASE ("GO without direction")
    <COMMAND [GO]>
    <EXPECT "Which way do you want to go?|">
    <CHECK <IN? ,WINNER ,STARTROOM>>>

<TEST-CASE ("Repeat motion with AGAIN")
    <COMMAND [NORTH]>
    <EXPECT "Hallway|">
    <COMMAND [AGAIN]>
    <EXPECT "Closet|">
    <CHECK <IN? ,WINNER ,CLOSET>>>

<TEST-GO ,STARTROOM>
