<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Home sweet home.")
    (FLAGS LIGHTBIT)>

<OBJECT TABLE
    (IN STARTROOM)
    (DESC "table")
    (LDESC "A lovely table is here.")
    (DESCFCN TABLE-DESC-F)
    (SYNONYM TABLE)
    (ADJECTIVE LOVELY)
    (FLAGS CONTBIT SURFACEBIT TRANSBIT)>

<ROUTINE TABLE-DESC-F (RARG)
    <COND (<=? .RARG ,M-OBJDESC?>
           ;"use descfcn when the apple is on the table and the mushroom isn't"
           <AND <IN? ,APPLE ,TABLE> <NOT <IN? ,MUSHROOM ,TABLE>>>)
          (<=? .RARG ,M-OBJDESC>
           <TELL "A juicy red apple is here, sitting on a lovely table." CR>)>>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (LDESC "A juicy red apple is here.")
    (DESCFCN APPLE-DESC-F)
    (SYNONYM APPLE)
    (ADJECTIVE JUICY RED)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<ROUTINE APPLE-DESC-F (RARG)
    <COND (<=? .RARG ,M-OBJDESC?>
           ;"use descfcn when the apple and the mushroom are together"
           <IN? ,APPLE <LOC ,MUSHROOM>>)
          (<=? .RARG ,M-OBJDESC>
           <TELL "A juicy red apple is here, keeping the mushroom in line." CR>)>>

<OBJECT MUSHROOM
    (IN STARTROOM)
    (DESC "mushroom")
    (LDESC "A terrifying mushroom is here.")
    (DESCFCN MUSHROOM-DESC-F)
    (SYNONYM MUSHROOM)
    (ADJECTIVE TERRIFYING)
    (FLAGS TAKEBIT)>

<ROUTINE MUSHROOM-DESC-F (RARG)
    <COND (<=? .RARG ,M-OBJDESC?>
           ;"use descfcn when the apple and the mushroom aren't together"
           <NOT <IN? ,APPLE <LOC ,MUSHROOM>>>)
          (<=? .RARG ,M-OBJDESC>
           <TELL "A terrifying mushroom is here, lurking ominously." CR>)>>

<TEST-SETUP ()
    <MOVE ,MUSHROOM ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <SETG WINNER ,PLAYER>>

<TEST-CASE ("Initial room description")
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Home sweet home.||A juicy red apple is here, keeping the mushroom in line.||A terrifying mushroom is here.||A lovely table is here.|">>

<TEST-CASE ("Apple on table")
    <MOVE ,APPLE ,TABLE>
    ;<COMMAND [PUT APPLE ON TABLE]>
    ;<EXPECT "You put the apple on the table.|">
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Home sweet home.||A terrifying mushroom is here, lurking ominously.||A juicy red apple is here, sitting on a lovely table.|">>

<TEST-CASE ("Mushroom on table")
    <MOVE ,MUSHROOM ,TABLE>
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Home sweet home.||A juicy red apple is here.||A lovely table is here.|On the table is a mushroom.|">>

<TEST-CASE ("Both on table")
    <MOVE ,APPLE ,TABLE>
    <MOVE ,MUSHROOM ,TABLE>
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Home sweet home.||A lovely table is here.|On the table are a mushroom and an apple.|">>

<TEST-GO ,STARTROOM>
