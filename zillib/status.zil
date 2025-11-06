<FILE-FLAGS UNUSED-ROUTINES?>

;"TODO: Allow more than 1 line"

<VERSION? (ZIP)
          (ELSE

"Mechanism"

<DEFINE STATUS-LINE-SECTION (NAME "ARGS" PROPERTIES)
    #DECL ((NAME) ATOM
        (PROPERTIES) <LIST [REST <LIST ATOM ANY>]>)
    <PUTPROP .NAME STATUS-LINE-SECTION .PROPERTIES>>

<DEFINE STATUS-LINE (NAME "ARGS" PROPERTIES)
    #DECL ((NAME) ATOM
        (PROPERTIES) <LIST [REST <LIST ATOM ANY>]>)
    <PUTPROP .NAME STATUS-LINE .PROPERTIES>
    <DEFINE-STATUS-LINE-ROUTINE .PROPERTIES <PARSE <STRING "STATUS-LINE?" <SPNAME .NAME>>>>>

<DEFMAC USE-STATUS-LINE (NAME "AUX" (RTN <PARSE <STRING "STATUS-LINE?" <SPNAME .NAME>>>))
    #DECL ((NAME) ATOM)
    ;"Create a routine for the status line NAME if it doesn't already exist - XXX can't do this!"
    ;<COND (<NOT <GETPROP .RTN ZVAL>>
        <DEFINE-STATUS-LINE-ROUTINE <GETPROP .NAME STATUS-LINE> .RTN>)>
    ;"Store its address in the global"
    `<SETG CURRENT-STATUS-LINE ,~.RTN>>

<NEWTYPE RSEC VECTOR '!<VECTOR ATOM <OR '* FIX> <OR 'LEFT 'CENTER 'RIGHT> LIST>>
<SETG RSEC-RTN <OFFSET 1 RSEC ATOM>>
<SETG RSEC-WIDTH <OFFSET 2 RSEC '<OR '* FIX>>>
<SETG RSEC-JUSTIFY <OFFSET 3 RSEC '<OR 'LEFT 'CENTER 'RIGHT>>>
<SETG RSEC-CONTENT <OFFSET 4 RSEC LIST>>

<DEFINE DEFINE-STATUS-LINE-ROUTINE (PROPERTIES RTN "AUX" (TOTAL-WIDTH 0) (HAS-STAR? <>) (EXTRAS '()) (MEASURE-STAR '()) (NEED-BUFFER? <>))
    #DECL ((PROPERTIES) <LIST [REST <LIST ATOM ANY>]>
        (RTN) ATOM)
    ;"Define routines to print each section, if they don't already exist"
    <MAPF <>
        <FUNCTION (PROP)
            <COND (<==? <1 .PROP> SECTION>
                    <DEFINE-STATUS-LINE-SECTION-ROUTINE
                        <GETPROP <2 .PROP> STATUS-LINE-SECTION>
                        <PARSE <STRING "STATUS-LINE-SECTION?" <SPNAME <2 .PROP>>>>>)>>
        .PROPERTIES>
    ;"Define a routine named .RTN to draw a status line as per PROPERTIES"
    ;"The routine needs to:
    (1) select the upper window and reverse video
    (2) move to the beginning of the row
    (3) clear the row with spaces
    (4) calculate the width of the * section, if present
    (5) for each section, call the appropriate PRINT- function
    (6) select the lower window and normal video"
    <SET SECTION-FORMS <MAPF ,LIST
                            <FUNCTION (PROP "AUX" RSEC R J W F)
                                <COND (<N==? <1 .PROP> SECTION> <MAPRET>)>
                                <SET RSEC <RESOLVE-SECTION <REST .PROP>>>
                                <SET R <RSEC-RTN .RSEC>>
                                <SET J <RSEC-JUSTIFY .RSEC>>
                                <SET W <RSEC-WIDTH .RSEC>>
                                <COND (<==? .W *>
                                       <SET W '.STAR-WIDTH>
                                       <SET HAS-STAR? T>)
                                      (ELSE <SET TOTAL-WIDTH <+ .TOTAL-WIDTH .W>>)>
                                <SET F <COND (<==? .J LEFT>
                                              `<PRINT-LEFT 1 .COL ~.W ~.R>)
                                             (<==? .J CENTER>
                                              <SET NEED-BUFFER? T>
                                              `<PRINT-CENTER 1 .COL ~.W ~.R>)
                                             (<==? .J RIGHT>
                                              <SET NEED-BUFFER? T>
                                              `<PRINT-RIGHT 1 .COL ~.W ~.R>)>>
                                <MAPRET .F `<SET COL <+ .COL ~.W>>>>
                            .PROPERTIES>>
    <COND (.HAS-STAR?
           <SET EXTRAS '(STAR-WIDTH)>
           <SET MEASURE-STAR `(<SET STAR-WIDTH <- <LOWCORE SCRH> ~.TOTAL-WIDTH>>)>)>
    <COND (<AND .NEED-BUFFER? <NOT <GETPROP SL-CONTENT-BUFFER ZVAL>>>
           <CONSTANT SL-CONTENT-BUFFER <ITABLE 100 (BYTE) 0>>)>
    <EVAL `<ROUTINE ~.RTN ("AUX" (COL 1) ~!.EXTRAS)
               <SCREEN 1>
               <HLIGHT ,H-INVERSE>
               <CURSET 1 1>
               <DO (I <LOWCORE SCRH> 1 -1) <PRINTC !\ >>
               <CURSET 1 1>
               ~!.MEASURE-STAR
               ~!.SECTION-FORMS
               <SCREEN 0>
               <HLIGHT ,H-NORMAL>>>>

<DEFINE DEFINE-STATUS-LINE-SECTION-ROUTINE (PROPERTIES RTN "AUX" (CONTENT <>))
    #DECL ((PROPERTIES) <LIST [REST <LIST ATOM ANY>]>
        (RTN) ATOM)
    <COND (<NOT <GETPROP .RTN ZVAL>>
        ;"Caller handles justification, we just need to print the content"
        <MAPF <>
                <FUNCTION (PROP)
                    <COND (<==? <1 .PROP> CONTENT>
                            <SET CONTENT <REST .PROP>>)>>
                .PROPERTIES>
        <COND (<NOT .CONTENT>
                <ERROR NO-STATUS-LINE-SECTION-CONTENT .RTN>)>
        <EVAL `<ROUTINE ~.RTN () ~!.CONTENT>>)>>

<DEFINE RESOLVE-SECTION (PROPS "AUX" SPROPS NAME RTN WIDTH JUSTIFY CONTENT)
    <SET NAME <1 .PROPS>>
    <SET SPROPS <OR <GETPROP .NAME STATUS-LINE-SECTION> '()>>
    <SET PROPS (!.SPROPS !<REST .PROPS>)>
    <SET RTN <PARSE <STRING "STATUS-LINE-SECTION?" <SPNAME .NAME>>>>
    <MAPF <>
        <FUNCTION (PROP)
            <COND (<==? <1 .PROP> WIDTH>
                   <SET WIDTH <2 .PROP>>)
                  (<==? <1 .PROP> JUSTIFY>
                   <SET JUSTIFY <2 .PROP>>)
                  (<==? <1 .PROP> CONTENT>
                   <SET CONTENT <REST .PROP>>)>>
        .PROPS>
    <CHTYPE [.RTN .WIDTH .JUSTIFY .CONTENT] RSEC>>

<DEFMAC PRINT-LEFT ('ROW 'COL 'WIDTH 'CONTENT-RTN)
    `<PROG ()
        <CURSET ~.ROW ~.COL>
        <APPLY ~.CONTENT-RTN>>>

<ROUTINE PRINT-CENTER (ROW COL WIDTH CONTENT-RTN "AUX" CWID SLACK LPAD)
    <DIROUT 3 ,SL-CONTENT-BUFFER>
    <APPLY .CONTENT-RTN>
    <DIROUT -3>
    <SET CWID <GET ,SL-CONTENT-BUFFER 0>>
    <COND (<G? .CWID .WIDTH> <SET CWID .WIDTH>)>
    <CURSET .ROW .COL>
    <SET SLACK <- .WIDTH .CWID>>
    <SET LPAD </ .SLACK 2>>
    <CURSET .ROW <+ .COL .LPAD>>
    <SET CWID <+ .CWID 1>>
    <DO (I 2 .CWID) <PRINTC <GETB ,SL-CONTENT-BUFFER .I>>>>

<ROUTINE PRINT-RIGHT (ROW COL WIDTH CONTENT-RTN "AUX" CWID SLACK LPAD)
    <DIROUT 3 ,SL-CONTENT-BUFFER>
    <APPLY .CONTENT-RTN>
    <DIROUT -3>
    <SET CWID <GET ,SL-CONTENT-BUFFER 0>>
    <COND (<G? .CWID .WIDTH> <SET CWID .WIDTH>)>
    <CURSET .ROW .COL>
    <SET SLACK <- .WIDTH .CWID>>
    ;<COND (.SLACK <DO (I 1 .LPAD) <PRINTC !\ >>)>
    <CURSET .ROW <+ .COL .SLACK>>
    <SET CWID <+ .CWID 1>>
    <DO (I 2 .CWID) <PRINTC <GETB ,SL-CONTENT-BUFFER .I>>>>

"Reusable sections"

<STATUS-LINE-SECTION LOCATION
    (JUSTIFY LEFT)
    (WIDTH *)
    (CONTENT <TELL !\ >
             <COND (,HERE-LIT <TELL D ,HERE>)
                   (ELSE <TELL %,DARKNESS-STATUS-TEXT>)>)>

<STATUS-LINE-SECTION SCORE
    (JUSTIFY LEFT)
    (WIDTH 12)
    (CONTENT <TELL <LIBRARY-MESSAGE PARSER STATUS-LINE-SCORE> N ,SCORE>)>

<STATUS-LINE-SECTION MOVES
    (JUSTIFY LEFT)
    (WIDTH 10)
    (CONTENT <TELL <LIBRARY-MESSAGE PARSER STATUS-LINE-MOVES> N ,MOVES>)>

<STATUS-LINE-SECTION TIME-24H
    (JUSTIFY LEFT)
    (WIDTH 10)
    (CONTENT <COND (<L? ,SCORE 10> <TELL !\0>)>
             <TELL N ,SCORE>
             <COND (<L? ,MOVES 10> <TELL !\0>)>
             <TELL N ,MOVES>)>

<STATUS-LINE-SECTION TIME-12H
    (JUSTIFY LEFT)
    (WIDTH 10)
    (CONTENT <PROG ((H <MOD ,SCORE 12>))
                <COND (<==? .H 0> <SET H 12>)>
                <COND (<L? ,H 10> <TELL !\ >)>
                <TELL N ,H>
                <COND (<L? ,MOVES 10> <TELL !\0>)>
                <TELL N ,MOVES>
                <COND (<L? ,SCORE 12> <TELL " AM">) (ELSE <TELL " PM">)>>)>

"Status line formats"

<STATUS-LINE DEFAULT
    (SECTION LOCATION)
    (SECTION SCORE)
    (SECTION MOVES)>

<STATUS-LINE TIME-24H
    (SECTION LOCATION)
    (SECTION TIME-24H)>

<STATUS-LINE TIME-12H
    (SECTION LOCATION)
    (SECTION TIME-12H)>

<STATUS-LINE LOCATION
    (SECTION LOCATION (JUSTIFY CENTER))>

)>  ;"end VERSION?"
