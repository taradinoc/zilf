<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT BOX
    (IN STARTROOM)
    (DESC "box")
    (SYNONYM BOX)
    (FLAGS CONTBIT OPENABLEBIT TAKEBIT)>

<TEST-SETUP () <MOVE ,WINNER ,STARTROOM>
    <MOVE ,BOX ,STARTROOM>>

<TEST-CASE ("GET THE")
    <COMMAND [GET THE]>
    <EXPECT "That phrase doesn't refer to anything.|">>

<TEST-CASE ("GET A")
    <COMMAND [GET A]>
    <EXPECT "That phrase doesn't refer to anything.|">>

<TEST-GO ,STARTROOM>
