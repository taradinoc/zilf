<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "green apple")
    (ADJECTIVE GREEN)
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT)>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>>

<TEST-CASE ("GIVE ME THE GREEN APPLE")
    <COMMAND [GIVE ME THE GREEN APPLE]>
    <EXPECT "You aren't holding the green apple.|">

    <COMMAND [GIVE ME GREEN APPLE]>
    <EXPECT "You aren't holding the green apple.|">

    <COMMAND [GIVE ME APPLE GREEN]>
    <EXPECT "That sentence has too many objects.|">>

<TEST-GO ,STARTROOM>
