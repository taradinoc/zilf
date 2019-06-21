<VERSION ZIP>

<GLOBAL RANDOM-IN-RANGE-IMPL <>>

<REPLACE-DEFINITION RANDOM-IN-RANGE
    <DEFMAC RANDOM-IN-RANGE ("ARGS" A) <FORM APPLY ',RANDOM-IN-RANGE-IMPL !.A>>

    <ROUTINE RIR-RETURN-LO (LO HI)
        .LO>

    <ROUTINE RIR-RETURN-HI (LO HI)
        .HI>
>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (ACTION APPLE-F)
    (FLAGS TAKEBIT)>

<ROUTINE APPLE-F ()
    <COND (<VERB? EXAMINE>
           <PRINT <PICK-ONE ,APPLE-MESSAGES>>
           <CRLF>)
          (<VERB? TAKE>
           <PRINT <PICK-ONE-R <REST ,APPLE-MESSAGES 2>> ;"pretend the 2 at the start of the table is a length word">
           <CRLF>)>>

<CONSTANT APPLE-MESSAGES-INIT
    <LTABLE
        2
        "You're not hungry."
        "That's a mealy one."
        "Bad season for apples."
        "Not after last time."
        "Come on.">>

<CONSTANT APPLE-MESSAGES-SIZE <+ 1 <ZGET ,APPLE-MESSAGES-INIT 0>>>

<CONSTANT APPLE-MESSAGES <ITABLE ,APPLE-MESSAGES-SIZE>>

<TEST-SETUP ()
    <COPY-TABLE ,APPLE-MESSAGES-INIT ,APPLE-MESSAGES ,APPLE-MESSAGES-SIZE>
    <MOVE ,WINNER ,STARTROOM>>

<TEST-CASE ("PICK-ONE from start of remaining items")
        <SETG RANDOM-IN-RANGE-IMPL ,RIR-RETURN-LO>
        ;"picks each item in order, starting with the first"
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "That's a mealy one.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Bad season for apples.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Not after last time.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Come on.|">
        ;"repeats the same as before:"
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "That's a mealy one.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Bad season for apples.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Not after last time.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Come on.|">
        >

<TEST-CASE ("PICK-ONE from end of remaining items")
        <SETG RANDOM-IN-RANGE-IMPL ,RIR-RETURN-HI>
        ;"picks the last item, then each item in order starting with the first"
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Come on.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "That's a mealy one.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Bad season for apples.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Not after last time.|">
        <COMMAND [EXAMINE APPLE]>
        ;"picks the last item again, then repeats in order"
        <EXPECT "Not after last time.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Come on.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "That's a mealy one.|">
        <COMMAND [EXAMINE APPLE]>
        <EXPECT "Bad season for apples.|">
        >

<TEST-CASE ("PICK-ONE-R from start of remaining items")
        <SETG RANDOM-IN-RANGE-IMPL ,RIR-RETURN-LO>
        ;"always picks the first item"
        <COMMAND [TAKE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [TAKE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [TAKE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [TAKE APPLE]>
        <EXPECT "You're not hungry.|">
        <COMMAND [TAKE APPLE]>
        <EXPECT "You're not hungry.|">
        >

<TEST-GO ,STARTROOM>
