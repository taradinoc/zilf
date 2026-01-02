<GLOBAL INVENTORY-PANEL-ID <>>
<GLOBAL ROOM-PICTURE-PANEL-ID <>>
<CONSTANT INVENTORY-PANEL-ROCK 11111>
<CONSTANT ROOM-PICTURE-PANEL-ROCK 22222>

<CONSTANT ROCK-BUFFER <TABLE 0 0>>

<CONSTANT GLK-WINDOW-ITERATE 32 ;"#16 20">
<CONSTANT GLK-WINDOW-GET-ROOT 34 ;"#16 22">
<CONSTANT GLK-WINDOW-OPEN 35 ;"#16 23">
<CONSTANT GLK-WINDOW-GET-SIZE 37 ;"#16 25">
<CONSTANT GLK-WINDOW-CLEAR 42 ;"#16 2A">
<CONSTANT GLK-SET-WINDOW 47 ;"#16 2F">
<CONSTANT GLK-STREAM-GET-CURRENT 72 ;"#16 48">
<CONSTANT GLK-STREAM-SET-CURRENT 71 ;"#16 47">
<CONSTANT GLK-IMAGE-GET-INFO 224 ;"#16 E0">
<CONSTANT GLK-IMAGE-DRAW-SCALED 226 ;"#16 E2">
<CONSTANT GLK-WINDOW-SET-BACKGROUND-COLOR 235 ;"#16 EB">
<CONSTANT GLK-IMAGE-DRAW-SCALED-EXT 236 ;"#16 EC">

<CONSTANT WINMETHOD-LEFT 0>
<CONSTANT WINMETHOD-RIGHT 1>
<CONSTANT WINMETHOD-ABOVE 2>
<CONSTANT WINMETHOD-BELOW 3>
<CONSTANT WINMETHOD-FIXED 16 ;"#16 10">
<CONSTANT WINMETHOD-PROPORTIONAL 32 ;"#16 20">
<CONSTANT WINMETHOD-BORDER 0>
<CONSTANT WINMETHOD-NOBORDER 256 ;"#16 100">

<CONSTANT WINTYPE-ALLTYPES 0>
<CONSTANT WINTYPE-PAIR 1>
<CONSTANT WINTYPE-BLANK 2>
<CONSTANT WINTYPE-TEXTBUFFER 3>
<CONSTANT WINTYPE-TEXTGRID 4>
<CONSTANT WINTYPE-GRAPHICS 5>

<CONSTANT IMAGERULE-WIDTHORIG 1>
<CONSTANT IMAGERULE-WIDTHFIXED 2>
<CONSTANT IMAGERULE-WIDTHRATIO 3>
<CONSTANT IMAGERULE-HEIGHTORIG 4>
<CONSTANT IMAGERULE-HEIGHTFIXED 8>
<CONSTANT IMAGERULE-ASPECTRATIO 12>

<CONSTANT M-PICTURE 100>

<COND (<NOT ,GLK>
       <ROUTINE INIT-GLK () <>>)
      (ELSE

<ROUTINE INIT-GLK ("AUX" ROOT)
    <RECOVER-GLK>
    <SET ROOT <GLK ,GLK-WINDOW-GET-ROOT>>
    <OR ,INVENTORY-PANEL-ID
        <SETG INVENTORY-PANEL-ID <GLK ,GLK-WINDOW-OPEN
                                      .ROOT
                                      <+ ,WINMETHOD-RIGHT
                                         ,WINMETHOD-PROPORTIONAL
                                         ,WINMETHOD-BORDER>
                                      25
                                      ,WINTYPE-TEXTBUFFER
                                      ,INVENTORY-PANEL-ROCK>>>
    <SET ROOT <GLK ,GLK-WINDOW-GET-ROOT>>
    <OR ,ROOM-PICTURE-PANEL-ID
        <SETG ROOM-PICTURE-PANEL-ID <GLK ,GLK-WINDOW-OPEN
                                         .ROOT
                                         <+ ,WINMETHOD-ABOVE
                                            ,WINMETHOD-PROPORTIONAL
                                            ,WINMETHOD-BORDER>
                                         33
                                         ,WINTYPE-GRAPHICS
                                         ,ROOM-PICTURE-PANEL-ROCK>>>
    <GLK ,GLK-WINDOW-SET-BACKGROUND-COLOR ,ROOM-PICTURE-PANEL-ID 0>>

<ROUTINE RECOVER-GLK ("AUX" (ID 0) ROCK)
    <REPEAT ()
        <SET ID <GLK ,GLK-WINDOW-ITERATE .ID ,ROCK-BUFFER>>
        <COND (<NOT .ID> <RETURN>)>
        <SET ROCK <GET ,ROCK-BUFFER <VERSION? (GLULX 0) (T 1)>>>
        <COND (<==? .ROCK ,INVENTORY-PANEL-ROCK> <SETG INVENTORY-PANEL-ID .ID>)
              (<==? .ROCK ,ROOM-PICTURE-PANEL-ROCK> <SETG ROOM-PICTURE-PANEL-ID .ID>)>>>

<ROUTINE DRAW-INVENTORY ("AUX" OSTR)
    <GLK ,GLK-WINDOW-CLEAR ,INVENTORY-PANEL-ID>
    <SET OSTR <GLK ,GLK-STREAM-GET-CURRENT>>
    <GLK ,GLK-SET-WINDOW ,INVENTORY-PANEL-ID>
    <CRLF> <CRLF>
    <V-INVENTORY>
    <GLK ,GLK-STREAM-SET-CURRENT .OSTR>>

<CONSTANT GLK-SIZE-TBL <TABLE 0 0>>

<ROUTINE DRAW-ROOM-PICTURE ("AUX" IMG)
    <GLK ,GLK-WINDOW-CLEAR ,ROOM-PICTURE-PANEL-ID>
    <COND (<NOT ,HERE-LIT>)
          (<APPLY <GETP ,HERE ,P?ACTION> ,M-PICTURE>)
          (<NOT <SET IMG <GETP ,HERE ,P?PICTURE>>>)
          (ELSE <DRAW-PICTURE .IMG>)>>

<ROUTINE DRAW-PICTURE (IMG "AUX" L R WW WH IW IH DW DH DX DY)
    ;"Fit to window"
    ;"Get window size"
    <GLK ,GLK-WINDOW-GET-SIZE ,ROOM-PICTURE-PANEL-ID ,GLK-SIZE-TBL <REST ,GLK-SIZE-TBL 4>>
    <SET WW <WIDE <GET ,GLK-SIZE-TBL 0>>>
    <SET WH <WIDE <GET ,GLK-SIZE-TBL 1>>>
    ;"Get image size"
    <GLK ,GLK-IMAGE-GET-INFO .IMG ,GLK-SIZE-TBL <REST ,GLK-SIZE-TBL 4>>
    <SET IW <WIDE <GET ,GLK-SIZE-TBL 0>>>
    <SET IH <WIDE <GET ,GLK-SIZE-TBL 1>>>
    ;"Determine whether we're limited by width or height"
    <SET L <WIDE <* .WW .IH>>>
    <SET R <WIDE <* .WH .IW>>>
    <COND (<WIDE <L=? .L .R>>
            ;"Width-limited"
            <SET DW .WW>
            <SET DH <WIDE </ <* .IH .WW> .IW>>>)
            (ELSE
            ;"Height-limited"
            <SET DH .WH>
            <SET DW <WIDE </ <* .IW .WH> .IH>>>)>
    ;"Center in window"
    <SET DX <WIDE </ <- .WW .DW> 2>>>
    <SET DY <WIDE </ <- .WH .DH> 2>>>
    <GLK ,GLK-IMAGE-DRAW-SCALED
        ,ROOM-PICTURE-PANEL-ID
        .IMG
        .DX     ;"X position"
        .DY     ;"Y position"
        .DW     ;"width"
        .DH     ;"height">>

;"Hook DRAW-INVENTORY and DRAW-ROOM-PICTURE in before PARSER and UPDATE-STATUS-LINE"
<VERSION?
    (ZIP
     ;"Glulx16 V3 doesn't call UPDATE-STATUS-LINE before READ, so hook before PARSER"
     <GUNASSIGN HOOK-BEFORE-PARSER>
     <ROUTINE HOOK-BEFORE-PARSER ()
         <DRAW-INVENTORY>
         <DRAW-ROOM-PICTURE>>

     ;"...but it does call UPDATE-STATUS-LINE when the window is resized"
     <BIND ((REDEFINE T))
         <ROUTINE UPDATE-STATUS-LINE ()
             <DRAW-INVENTORY>
             <DRAW-ROOM-PICTURE>
             <WRAP-FOR-DARK-STATUS <USL>>>>)
    (ELSE
     ;"Glulx16 V4+, and Glulx, call UPDATE-STATUS-LINE in both cases"
     <GUNASSIGN UPDATE-STATUS-LINE>     ;"replace the macro"
     <ROUTINE UPDATE-STATUS-LINE ()
         <DRAW-INVENTORY>
         <DRAW-ROOM-PICTURE>
         <APPLY ,CURRENT-STATUS-LINE>>)>

)>  ;"GLK feature check"
