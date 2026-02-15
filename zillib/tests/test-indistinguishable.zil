<VERSION ZIP>

<DELAY-DEFINITION INDISTINGUISHABLE?>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT CLONE1
    (IN STARTROOM)
    (DESC "clone1")
    (SYNONYM CLONE)
    (FLAGS TAKEBIT)>

<OBJECT CLONE2
    (IN STARTROOM)
    (DESC "clone2")
    (SYNONYM CLONE)
    (FLAGS TAKEBIT)>

<OBJECT RED-CONE
    (IN STARTROOM)
    (DESC "red cone")
    (SYNONYM CONE)
    (ADJECTIVE RED)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CONE1
    (IN STARTROOM)
    (DESC "blue cone1")
    (SYNONYM CONE)
    (ADJECTIVE BLUE)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CONE2
    (IN STARTROOM)
    (DESC "blue cone2")
    (SYNONYM CONE)
    (ADJECTIVE BLUE)
    (FLAGS TAKEBIT)>

<REPLACE-DEFINITION INDISTINGUISHABLE?
    <DEFMAC INDISTINGUISHABLE? ('A 'B)
        `<NOT <DISTINGUISHABLE-BY-VOCAB? ~.A ~.B>>>>

<TEST-SETUP ()
    <MOVE ,PLAYER ,STARTROOM>
    <MOVE ,CLONE1 ,STARTROOM>
    <MOVE ,CLONE2 ,STARTROOM>
    <MOVE ,RED-CONE ,STARTROOM>
    <MOVE ,BLUE-CONE1 ,STARTROOM>
    <MOVE ,BLUE-CONE2 ,STARTROOM>>

<TEST-CASE ("Indistinguishable by vocab")
    <COMMAND [TAKE CLONE]>
    <EXPECT "You pick up the clone2.|">>

<TEST-CASE ("Distinguishable by vocab")
    <COMMAND [TAKE CONE]>
    <EXPECT "Which do you mean, the blue cone2 or the red cone?|">>

<TEST-GO ,STARTROOM>