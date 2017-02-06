<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT DIAL
    (IN STARTROOM)
    (DESC "dial")
    (SYNONYM DIAL)>

<SYNTAX TURN OBJECT TO OBJECT = V-TURN-TO>

<ROUTINE V-TURN-TO ()
    <COND (<N=? ,PRSI ,NUMBER> <TELL "Expected a number." CR>)
          (ELSE <TELL "You set " T ,PRSO " to " N ,P-NUMBER "." CR>)>>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,DIAL ,STARTROOM>>

<TEST-CASE ("Positive number")
    <COMMAND [TURN DIAL TO "45"]>
    <EXPECT "You set the dial to 45.|">
    <COMMAND [TURN DIAL TO "32767"]>
    <EXPECT "You set the dial to 32767.|">>

<TEST-CASE ("Negative number")
    <COMMAND [TURN DIAL TO "-10000"]>
    <EXPECT "You set the dial to -10000.|">
    <COMMAND [TURN DIAL TO "-32768"]>
    <EXPECT "You set the dial to -32768.|">>

<TEST-CASE ("Zero")
    <COMMAND [TURN DIAL TO "0"]>
    <EXPECT "You set the dial to 0.|">>

<TEST-CASE ("Overflow")
    <COMMAND [TURN DIAL TO "80000"]>
    <EXPECT "I don't know the word \"80000\".|">
    <COMMAND [TURN DIAL TO "65536"]>
    <EXPECT "I don't know the word \"65536\".|">
    <COMMAND [TURN DIAL TO "50000"]>
    <EXPECT "I don't know the word \"50000\".|">
    <COMMAND [TURN DIAL TO "32768"]>
    <EXPECT "I don't know the word \"32768\".|">
    <COMMAND [TURN DIAL TO "-32769"]>
    <EXPECT "I don't know the word \"-32769\".|">
    <COMMAND [TURN DIAL TO "-50000"]>
    <EXPECT "I don't know the word \"-50000\".|">
    <COMMAND [TURN DIAL TO "-80000"]>
    <EXPECT "I don't know the word \"-80000\".|">>

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
