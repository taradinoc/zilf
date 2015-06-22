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
    <MOVE ,APPLE ,STARTROOM>>

<TEST-CASE ("Preposition before noun")
    <COMMAND [PICK UP APPLE]>
    <EXPECT "You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>
    
    <COMMAND [PUT DOWN APPLE]>
    <EXPECT "You drop the apple.|">
    <CHECK <IN? ,APPLE ,HERE>>>

<TEST-CASE ("Preposition at end")
    <COMMAND [PICK APPLE UP]>
    <EXPECT "You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>
    
    <COMMAND [PUT APPLE DOWN]>
    <EXPECT "You drop the apple.|">
    <CHECK <IN? ,APPLE ,HERE>>>

<TEST-GO ,STARTROOM>
