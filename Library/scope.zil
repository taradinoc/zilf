"Scope"

"Basic definitions"

;"Status codes to explain why MAP-SCOPE exited"
<CONSTANT MS-FINISHED 0>
<CONSTANT MS-NO-LIGHT 1>

<GLOBAL MAP-SCOPE-STATUS <>>

;"Defining and checking scope stages"
<SETG SCOPE-STAGES ()>

<DEFINE SCOPE-STAGE (NAME STATE-WORDS 'INIT-CODE 'NEXT-CODE)
    <SETG SCOPE-STAGES
          <LIST !,SCOPE-STAGES
                <VECTOR .NAME .STATE-WORDS .INIT-CODE .NEXT-CODE>>>>

<DEFMAC SCOPE-STAGE? ('NAME)
    <FORM ==?
          '<GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE>
          <PARSE <STRING <SPNAME .NAME> "-SCOPE-STAGE">>>>

<DEFMAC SCOPE-EXIT ('STATUS)
    <FORM BIND ()
          <FORM SETG MAP-SCOPE-STATUS .STATUS>
          '<RETURN -1 .SCOPE-STAGE-ACTIVATION>>>

"Scope stages"

<SCOPE-STAGE GENERIC 2
    (<PUT SCOPE-STATE 0 <FIRST? ,GENERIC-OBJECTS>>
     <PUT SCOPE-STATE 1 ,GENERIC-OBJECTS>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE CHECK-LIGHT 0
    (<COND (<NOT <OR <FSET? ,HERE ,LIGHTBIT>>
                     <SEARCH-FOR-LIGHT>>
            ;<TELL "It's too dark to see anything here." CR>
            <SCOPE-EXIT ,MS-NO-LIGHT>)>)
    (<>)>

<SCOPE-STAGE LOCATION 2
    (<PUT SCOPE-STATE 0 <FIRST? ,HERE>>
     <PUT SCOPE-STATE 1 ,HERE>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE INVENTORY 2
    (<PUT SCOPE-STATE 0 <FIRST? ,WINNER>>
     <PUT SCOPE-STATE 1 ,WINNER>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE GLOBALS 2
    (<PUT SCOPE-STATE 0 <FIRST? ,GLOBAL-OBJECTS>>
     <PUT SCOPE-STATE 1 ,GLOBAL-OBJECTS>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE LOCAL-GLOBALS 2
    (<PUT SCOPE-STATE 0 <FIRST? ,LOCAL-GLOBALS>>
     <PUT SCOPE-STATE 1 ,LOCAL-GLOBALS>)
    (<REPEAT (N)
         <SET N <SCOPE-CRAWL>>
         <COND (<NOT .N> <RFALSE>)
               (<GLOBAL-IN? .N ,HERE> <RETURN .N>)>>)>

"Scope machinery"

;"Define scope stage routines"
<MAPF
    <>
    <FUNCTION (S "AUX" NAME INIT-CODE NEXT-CODE)
        <SET NAME <PARSE <STRING <SPNAME <1 .S>> "-SCOPE-STAGE">>>
        <SET INIT-CODE <3 .S>>
        <SET NEXT-CODE <4 .S>>
        <EVAL
            <FORM ROUTINE .NAME SCOPE-STAGE-ACTIVATION '(INIT)
                  <FORM COND <LIST '.INIT !.INIT-CODE>
                        <LIST ELSE !.NEXT-CODE>>>>>
    ,SCOPE-STAGES>

;"Define enough state for the most demanding stage"
<CONSTANT SCOPE-STATE-SIZE %<MAPF ,MAX 2 ,SCOPE-STAGES>>
<CONSTANT SCOPE-STATE <ITABLE NONE ,SCOPE-STATE-SIZE>>
<CONSTANT SCOPE-CURRENT-STAGES-SIZE %<LENGTH ,SCOPE-STAGES>>
<CONSTANT SCOPE-CURRENT-STAGES <ITABLE WORD ,SCOPE-CURRENT-STAGES-SIZE>>
<GLOBAL SCOPE-CURRENT-STAGE -1>

<DEFMAC MAP-SCOPE ('VAR "ARGS" BODY "AUX" STAGES INIT-STAGES)
    <COND (<TYPE? .VAR LIST> <SET VAR <1 .VAR>>)
          (ELSE <ERROR WRONG-ARG-TYPE 1 .VAR>)>
    ;"User can specify stages, or default to all stages in definition order"
    <COND (<TYPE? <1 .BODY> LIST>
           <SET STAGES <1 .BODY>>
           <SET BODY <REST .BODY>>)
          (ELSE
           <SET STAGES <MAPF ,LIST 1 ,SCOPE-STAGES>>)>
    <COND (<G? <LENGTH .STAGES> ,SCOPE-CURRENT-STAGES-SIZE>
           <ERROR TOO-MANY-STAGES>)>
    ;"Generate code to initialize SCOPE-CURRENT-STAGES"
    <BIND ((I 0))
        <SET INIT-STAGES
             <MAPF ,LIST
                   <FUNCTION (S)
                       <SET I <+ .I 1>>
                       <FORM PUT
                             ',SCOPE-CURRENT-STAGES
                             .I
                             <PARSE <STRING <SPNAME .S> "-SCOPE-STAGE">>>>
                   .STAGES>>>
    <FORM PROG '()
          <FORM PUT ',SCOPE-CURRENT-STAGES 0 <LENGTH .INIT-STAGES>>
          !.INIT-STAGES
          '<COND (<NOT <MAP-SCOPE-START>>
                  <RETURN>)>
          <FORM REPEAT <LIST .VAR>
                <FORM SET .VAR '<MAP-SCOPE-NEXT>>
                <FORM COND <LIST <FORM 0? <FORM LVAL .VAR>>
                                 '<RETURN>>>
                !.BODY>>>

<ROUTINE MAP-SCOPE-START ("AUX" V (LEN <GET ,SCOPE-CURRENT-STAGES 0>))
    <SETG SCOPE-CURRENT-STAGE 0>
    <REPEAT ()
        <COND (<G? <SETG SCOPE-CURRENT-STAGE <+ ,SCOPE-CURRENT-STAGE 1>> .LEN>
               <SETG MAP-SCOPE-STATUS ,MS-FINISHED>
               <RFALSE>)
              (<SET V <APPLY <GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE> T>>
               <COND (<==? .V -1>
                      ;"If stage returns -1, abort. It already set MAP-SCOPE-STATUS."
                      <RFALSE>)>
               <RTRUE>)>>>

<ROUTINE MAP-SCOPE-NEXT MSN ("AUX" N (LEN <GET ,SCOPE-CURRENT-STAGES 0>) (INIT <>))
    <REPEAT ()
        <COND (<G? ,SCOPE-CURRENT-STAGE .LEN>
               <SETG MAP-SCOPE-STATUS ,MS-FINISHED>
               <RFALSE>)
              (<SET N <APPLY <GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE> .INIT>>
               <COND (<==? .N -1>
                      ;"If stage returns -1, abort. It already set MAP-SCOPE-STATUS."
                      <RFALSE>)>
               <RETURN .N .MSN>)
              (ELSE
               <SETG SCOPE-CURRENT-STAGE <+ ,SCOPE-CURRENT-STAGE 1>>
               <SET INIT T>)>>>

<ROUTINE SCOPE-CRAWL ("AUX" O N C L)
    <SET O <GET ,SCOPE-STATE 0>>
    <OR .O <RFALSE>>
    <SET C <GET ,SCOPE-STATE 1>>
    <COND (<AND <SET N <FIRST? .O>>
                <SEE-INSIDE? .O>>
           ;"Next is O's child")
          (<SET N <NEXT? .O>>
           ;"Next is O's sibling")
          (ELSE
           <SET L <LOC .O>>
           <REPEAT ()
               <COND (<==? .L .C <>>
                      ;"Reached the scope ceiling"
                      <SET N <>>
                      <RETURN>)
                     (<SET N <NEXT? .L>>
                      ;"Next is L's sibling"
                      <RETURN>)
                     (ELSE
                      <SET L <LOC .L>>)>>)>
    <PUT ,SCOPE-STATE 0 .N>
    .O>
