<VERSION ZIP>

<INSERT-FILE "parser">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT)>

<INSERT-FILE "testing">

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,WINNER ,STARTROOM>>

<TEST-CASE ("Correct an unrecognized word")
    <COMMAND [TAKE "APEL"]>
    <EXPECT "I don't know the word \"apel\".|">
    <COMMAND [OOPS APPLE]>
    <EXPECT "You pick up the apple.|">
    <COMMAND [DROP "APPPPPLLLLEEEE"]>
    <EXPECT "I don't know the word \"appppplllleeee\".|">
    <COMMAND [DROP APPLE]>
    <EXPECT "You drop the apple.|">>
    

<TEST-GO ,STARTROOM>
