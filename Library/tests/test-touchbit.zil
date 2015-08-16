<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)
    (NORTH TO CLOSET)>

<OBJECT CLOSET
    (IN ROOMS)
    (DESC "Closet")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)
    (SOUTH TO STARTROOM)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (FDESC "A jolly, candy-like apple catches your eye.")
    (LDESC "A dusty old apple is here.")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <FCLEAR ,APPLE ,TOUCHBIT>
    <FCLEAR ,STARTROOM ,TOUCHBIT>>

<TEST-CASE ("Untouched object should show FDESC")
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Ldesc.||A jolly, candy-like apple catches your eye.|">>

<TEST-CASE ("Touched object should show LDESC")
    <COMMAND [GET APPLE]>
    <COMMAND [DROP APPLE]>
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Ldesc.||A dusty old apple is here.|">>

<TEST-CASE ("Start room should be visited after looking")
    <COMMAND [LOOK]>
    <CHECK <FSET? ,STARTROOM ,TOUCHBIT>>>

<TEST-CASE ("Only unvisited rooms should show LDESC in BRIEF mode")
    <COMMAND [BRIEF]>
    <COMMAND [LOOK]>
    <COMMAND [NORTH]>
    <EXPECT "Closet|Ldesc.|">
    
    <COMMAND [SOUTH]>
    <EXPECT "Start Room||A jolly, candy-like apple catches your eye.|">
    
    <COMMAND [NORTH]>
    <EXPECT "Closet|">>

<TEST-GO ,STARTROOM>
