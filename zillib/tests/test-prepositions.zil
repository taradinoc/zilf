<VERSION ZIP>

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

<SYNTAX CARESS OBJECT GENTLY OBJECT (FIND KLUDGEBIT) = V-GENTLY-CARESS>

<ROUTINE V-GENTLY-CARESS ()
    <TELL "You gently caress " T ,PRSO "." CR>>

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

<TEST-CASE ("Don't infer a trailing preposition with KLUDGEBIT")
    <COMMAND [CARESS APPLE]>
    <EXPECT "I don't understand that sentence.|">>

<TEST-GO ,STARTROOM>
