<VERSION ZIP>

<GLOBAL PROVOKE? <>>

<REPLACE-DEFINITION PROVIDE-MISSING-VERB?
    <ROUTINE PROVIDE-MISSING-VERB? ()
        <COND (<0? ,P-NOBJ> <RFALSE>)
              (,PROVOKE? ,W?\,PROVOKE)
              (ELSE ,W?\,INVOKE)>>

    <ROUTINE PRINT-MISSING-VERB ()
        <TELL IFELSE ,PROVOKE? "provoke" "invoke">>>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT RED-BOX
    (IN STARTROOM)
    (DESC "red box")
    (SYNONYM BOX)
    (ADJECTIVE RED)
    (FLAGS TAKEBIT)>

<OBJECT RED-APPLE
    (IN STARTROOM)
    (DESC "red apple")
    (SYNONYM APPLE)
    (ADJECTIVE RED)
    (FLAGS TAKEBIT)>

<OBJECT STICK
    (IN STARTROOM)
    (DESC "stick")
    (SYNONYM STICK)
    (FLAGS TAKEBIT)>

<SYNTAX \,INVOKE OBJECT = V-INVOKE>
<SYNTAX \,PROVOKE OBJECT (MANY) WITH OBJECT = V-PROVOKE>

<ROUTINE V-INVOKE ()
    <TELL "You invoke " T ,PRSO "." CR>>

<ROUTINE V-PROVOKE ()
    <TELL "You provoke " T ,PRSO "." CR>>

<TEST-SETUP ()
    <SETG PROVOKE? <>>
    <MOVE ,RED-BOX ,STARTROOM>
    <MOVE ,RED-APPLE ,STARTROOM>>

<TEST-CASE ("Noun phrase with no verb")
    <COMMAND [APPLE]>
    <EXPECT "You invoke the red apple.|">>

<TEST-CASE ("Ambiguous noun phrase and orphan response")
    <COMMAND [RED]>
    <EXPECT "Which do you mean, the red apple or the red box?|">
    <SETG PROVOKE? T>
    <COMMAND [BOX]>
    <EXPECT "You invoke the red box.|">>

<TEST-CASE ("Multiple objects when not allowed")
    <COMMAND [BOX AND APPLE]>
    <EXPECT "You can't use multiple direct objects like that.|">>

<TEST-CASE ("Multiple objects when allowed")
    <SETG PROVOKE? T>
    <COMMAND [BOX AND APPLE WITH STICK]>
    <EXPECT "red box: You provoke the red box.|red apple: You provoke the red apple.|">>

<TEST-CASE ("Missing PRSI")
    <SETG PROVOKE? T>
    <COMMAND [BOX]>
    <EXPECT "What do you want to provoke the red box with?|">>

<TEST-CASE ("No object")
    <COMMAND [THEN]>
    <EXPECT "That sentence has no verb.|">>

<TEST-GO ,STARTROOM>
