<VERSION ZIP>

<COMPILATION-FLAG DEBUG T>

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

<OBJECT RED-CUBE
    (IN STARTROOM)
    (DESC "red cube")
    (SYNONYM CUBE)
    (ADJECTIVE RED)
    (FLAGS TAKEBIT)>

<OBJECT GREEN-CUBE
    (IN STARTROOM)
    (DESC "green cube")
    (SYNONYM CUBE)
    (ADJECTIVE GREEN)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CUBE
    (IN STARTROOM)
    (DESC "blue cube")
    (SYNONYM CUBE)
    (ADJECTIVE BLUE)
    (FLAGS TAKEBIT)>

<TEST-SETUP ()
    <SETG TRACE-LEVEL 0>
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,STARTROOM>
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

<TEST-CASE ("OOPS while orphaning (ambiguous noun)")
    <COMMAND [TAKE CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND ["GREN"]>
    <EXPECT "I don't know the word \"gren\".|">
    <COMMAND [OOPS GREEN]>
    <EXPECT "You pick up the green cube.|">
    <COMMAND [TAKE CUBE]>
    <EXPECT "Which do you mean, the blue cube or the red cube?|">
    ;<CHECK <SETG TRACE-LEVEL 5>>
    <COMMAND [THE "BLU"]>
    <EXPECT "I don't know the word \"blu\".|">
    <COMMAND [OOPS BLUE]>
    <EXPECT "You pick up the blue cube.|">>

<TEST-CASE ("OOPS while orphaning (missing noun)")
    <COMMAND [TAKE]>
    <EXPECT "What do you want to take?|">
    <COMMAND ["GREN" CUBE]>
    <EXPECT "I don't know the word \"gren\".|">
    <COMMAND [OOPS GREEN]>
    <EXPECT "You pick up the green cube.|">>

<TEST-GO ,STARTROOM>
