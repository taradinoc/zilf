<VERSION ZIP>

<INSERT-FILE "parser">

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
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT)>

<OBJECT DESK
    (IN STARTROOM)
    (DESC "desk")
    (SYNONYM DESK)
    (FLAGS SURFACEBIT)>

<OBJECT BUCKET
    (IN STARTROOM)
    (DESC "bucket")
    (SYNONYM BUCKET)
    (FLAGS CONTBIT OPENBIT)>

<INSERT-FILE "testing">

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,BANANA ,STARTROOM>
    <MOVE ,HAT ,STARTROOM>
    <FCLEAR ,HAT ,WORNBIT>
    <MOVE ,CAGE ,STARTROOM>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>>

<TEST-CASE ("Take all")
    <COMMAND [TAKE ALL]>
    <EXPECT "hat: You wear the hat.|
banana: You pick up the banana.|
apple: You pick up the apple.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Exclude one object with BUT")
    <COMMAND [TAKE ALL BUT BANANA]>
    <EXPECT "hat: You wear the hat.|
apple: You pick up the apple.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Exclude two objects with BUT")
    <COMMAND [TAKE ALL BUT BANANA AND APPLE]>
    <EXPECT "You wear the hat.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Take individual objects with AND")
    <COMMAND [TAKE HAT AND BANANA]>
    <EXPECT "hat: You wear the hat.|
banana: You pick up the banana.|">
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <NOT <IN? APPLE ,WINNER>>>>

<TEST-CASE ("Drop all")
    <MOVE ,HAT ,WINNER>
    <MOVE ,BANANA ,WINNER>
    <MOVE ,APPLE ,WINNER>
    <COMMAND [DROP ALL]>
    <EXPECT "hat: You drop the hat.|
banana: You drop the banana.|
apple: You drop the apple.|">
    <CHECK <NOT <IN? ,HAT ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>>

<TEST-GO ,STARTROOM>
