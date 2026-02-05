<VERSION ZIP>

<DELAY-DEFINITION HOOK-MID-PARSE-CONSUME>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT BOX1
    (IN STARTROOM)
    (DESC "large box")
    (SYNONYM BOX)
    (ADJECTIVE \,DUMMY-ADJ)
    (SIZE 2)
    (FLAGS TAKEBIT)>

<OBJECT BOX2
    (IN STARTROOM)
    (DESC "small box")
    (SYNONYM BOX)
    (ADJECTIVE \,DUMMY-ADJ)
    (SIZE 1)
    (FLAGS TAKEBIT)>

<CONSTANT LARGE-WORD <VOC "LARGE">>
<CONSTANT SMALL-WORD <VOC "SMALL">>

<CONSTANT SELECTED-WORD <VOC ",SELECTED" ADJ>>
<CONSTANT SELECTED-ADJ A?\,SELECTED>

<REPLACE-DEFINITION HOOK-MID-PARSE-CONSUME
    <ROUTINE HOOK-MID-PARSE-CONSUME (WN "AUX" (W <GETWORD? .WN>))
        <COND (<==? .W ,LARGE-WORD> <SELECT-OBJECTS .WN 2>)
              (<==? .W ,SMALL-WORD> <SELECT-OBJECTS .WN 1>)>
        <RETURN 0>>>

<ROUTINE SELECT-OBJECTS (WN SIZE)
    <PUTWORD .WN ,SELECTED-WORD>
    <MAYBE-SELECT ,BOX1 .SIZE>
    <MAYBE-SELECT ,BOX2 .SIZE>>

<ROUTINE MAYBE-SELECT (OBJ SIZE "AUX" PT)
    <SET PT <GETPT .OBJ ,P?ADJECTIVE>>
    <COND (.PT
           <COND (<==? <GETP .OBJ ,P?SIZE> .SIZE> <PUTB .PT 0 ,SELECTED-ADJ>)
                 (ELSE <PUTB .PT 0 <>>)>)>>

<TEST-SETUP ()
    <MOVE ,PLAYER ,STARTROOM>
    <MOVE ,BOX1 ,STARTROOM>
    <MOVE ,BOX2 ,STARTROOM>>

<TEST-CASE ("Take large box")
    <COMMAND [TAKE LARGE BOX]>
    <EXPECT "You pick up the large box.|">>

<TEST-CASE ("Orphan response")
    <COMMAND [TAKE BOX]>
    <EXPECT "Which do you mean, the small box or the large box?|">
    <COMMAND [SMALL]>
    <EXPECT "You pick up the small box.|">>

<TEST-CASE ("Using oops")
    <COMMAND [TAKE "XLARGEX" BOX]>
    <EXPECT "I don't know the word \"xlargex\".|">
    <COMMAND [OOPS LARGE]>
    <EXPECT "You pick up the large box.|">>

<TEST-CASE ("Orphan response + oops")
    <COMMAND [TAKE BOX]>
    <EXPECT "Which do you mean, the small box or the large box?|">
    <COMMAND ["XSMALLX"]>
    <EXPECT "I don't know the word \"xsmallx\".|">
    <COMMAND [OOPS SMALL]>
    <EXPECT "You pick up the small box.|">>

<TEST-GO ,STARTROOM>
