<VERSION ZIP>

<INSERT-FILE "parser">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT DIAL
    (IN STARTROOM)
    (DESC "dial")
    (SYNONYM DIAL)>

<INSERT-FILE "testing">

<SYNTAX TURN OBJECT TO OBJECT = V-TURN-TO>

<ROUTINE V-TURN-TO ()
    <COND (<N=? ,PRSI ,NUMBER> <TELL "Expected a number." CR>)
          (ELSE <TELL "You set " T ,PRSO " to " N ,P-NUMBER "." CR>)>>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,DIAL ,STARTROOM>>

<TEST-CASE ("Positive number")
    <COMMAND [TURN DIAL TO "45"]>
    <EXPECT "You set the dial to 45.|">>

<TEST-CASE ("Negative number")
    <COMMAND [TURN DIAL TO "-10000"]>
    <EXPECT "You set the dial to -10000.|">>

<TEST-CASE ("Zero")
    <COMMAND [TURN DIAL TO "0"]>
    <EXPECT "You set the dial to 0.|">>

<TEST-CASE ("Numbers for PRSO and PRSI")
    <COMMAND [TURN "5" TO "6"]>
    <EXPECT "You can't use more than one number in a command.|">>

<TEST-CASE ("Numbers as multiple objects")
    <COMMAND [EXAMINE "5" AND "6"]>
    <EXPECT "You can't use more than one number in a command.|">>

<TEST-CASE ("TAKE 5")
    <COMMAND [TAKE "5"]>
    <CHECK <NOT <IN? ,NUMBER ,WINNER>>>>

<TEST-CASE ("EXAMINE ALL shouldn't include the number")
    <COMMAND [EXAMINE ALL]>
    <EXPECT "You see nothing special about the dial.|">>

<TEST-GO ,STARTROOM>
