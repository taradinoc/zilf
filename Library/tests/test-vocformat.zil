<VERSION ZIP>

<SETG NEW-VOC? T>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT)>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>>

<TEST-CASE ("Vocabulary format")
    <COMMAND [GET APPLE]>
    <EXPECT "You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-GO ,STARTROOM>
