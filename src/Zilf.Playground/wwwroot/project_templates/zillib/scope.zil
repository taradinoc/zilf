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

<DEFMAC SCOPE-STAGE? ("ARGS" NAMES "AUX" SSS)
    <SET SSS
        <MAPF
            ,LIST
            <FUNCTION (N) <PARSE <STRING <SPNAME .N> "-SCOPE-STAGE">>>
            .NAMES>>
    <FORM ==?
          '<GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE>
          !.SSS>>

<DEFMAC SCOPE-EXIT ('STATUS)
    <FORM BIND ()
          <FORM SETG MAP-SCOPE-STATUS .STATUS>
          '<RETURN -1 .SCOPE-STAGE-ACTIVATION>>>

"Scope stages"

<SCOPE-STAGE INVENTORY 2
    (<PUT SCOPE-STATE 0 <FIRST? ,WINNER>>
     <PUT SCOPE-STATE 1 ,WINNER>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE LOCATION 2
    (<PUT SCOPE-STATE 0 <FIRST? ,HERE>>
     <PUT SCOPE-STATE 1 ,HERE>)
    (<SCOPE-CRAWL>)>

;"TODO: Just step through the GLOBAL property instead of checking everything
  in LOCAL-GLOBALS."
<SCOPE-STAGE LOCAL-GLOBALS 2
    (<PUT SCOPE-STATE 0 <FIRST? ,LOCAL-GLOBALS>>
     <PUT SCOPE-STATE 1 ,LOCAL-GLOBALS>)
    (<REPEAT (N)
         <SET N <SCOPE-CRAWL>>
         <COND (<NOT .N> <RFALSE>)
               (<GLOBAL-IN? .N ,HERE> <RETURN .N>)>>)>

<SCOPE-STAGE GLOBALS 2
    (<PUT SCOPE-STATE 0 <FIRST? ,GLOBAL-OBJECTS>>
     <PUT SCOPE-STATE 1 ,GLOBAL-OBJECTS>)
    (<SCOPE-CRAWL>)>

<SCOPE-STAGE GENERIC 2
    (<PUT SCOPE-STATE 0 <FIRST? ,GENERIC-OBJECTS>>
     <PUT SCOPE-STATE 1 ,GENERIC-OBJECTS>)
    (<SCOPE-CRAWL>)>

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
<CONSTANT SCOPE-STATE-SIZE <MAPF ,MAX 2 ,SCOPE-STAGES>>
<CONSTANT SCOPE-STATE <ITABLE NONE ,SCOPE-STATE-SIZE>>
<CONSTANT SCOPE-CURRENT-STAGES-SIZE <LENGTH ,SCOPE-STAGES>>
<CONSTANT SCOPE-CURRENT-STAGES <ITABLE WORD ,SCOPE-CURRENT-STAGES-SIZE>>
<GLOBAL SCOPE-CURRENT-STAGE -1>

<CONSTANT MSO-NEED-LIGHT 1>
<GLOBAL MAP-SCOPE-OPTIONS <>>

<DEFMAC MAP-SCOPE ('VAR "ARGS" BODY "AUX" STAGES INIT-OPTIONS INIT-STAGES OPTS)
    <COND (<NOT <TYPE? .VAR LIST>> <ERROR WRONG-ARG-TYPE 1 .VAR>)>
    ;"To specify stages explicitly:
        [STAGES (INVENTORY LOCATION)]
      Or set them from search bits:
        [BITS .B]
      Or default to all stages in definition order (or use [BITS -1]).

      To turn off the light requirement:
        [NO-LIGHT]"
    <SET OPTS <REST .VAR>>
    <SET VAR <1 .VAR>>
    <SET STAGES <MAPF ,LIST 1 ,SCOPE-STAGES>>
    <SET INIT-STAGES <>>
    <SET INIT-OPTIONS '<SETG MAP-SCOPE-OPTIONS ,MSO-NEED-LIGHT>>
    <REPEAT ()
        <COND (<EMPTY? .OPTS> <RETURN>)>
        <BIND ((SV <1 .OPTS>))
            <COND (<NOT <TYPE? .SV VECTOR>> <ERROR BAD-OPTION .SV>)
                  (<AND <==? <LENGTH .SV> 2> <==? <1 .SV> BITS>>
                   <SET STAGES <>>
                   <SET INIT-STAGES
                       <LIST <FORM MAP-SCOPE-INIT-STAGES-FROM-BITS <2 .SV>>>>)
                  (<AND <==? <LENGTH .SV> 2> <==? <1 .SV> STAGES>>
                   <SET STAGES <2 .SV>>
                   <SET INIT-STAGES <>>)
                  (<AND <==? <LENGTH .SV> 1> <==? <1 .SV> NO-LIGHT>>
                   <SET INIT-OPTIONS '<SETG MAP-SCOPE-OPTIONS <>>>)
                  (ELSE <ERROR BAD-OPTION .SV>)>>
        <SET OPTS <REST .OPTS>>>
    ;"Generate code to initialize SCOPE-CURRENT-STAGES"
    <COND (<NOT .INIT-STAGES>
           <COND (<AND <ASSIGNED? STAGES>
                       <G? <LENGTH .STAGES> ,SCOPE-CURRENT-STAGES-SIZE>>
                  <ERROR TOO-MANY-STAGES>)>
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
           <SET INIT-STAGES
               <CONS <FORM PUT ',SCOPE-CURRENT-STAGES 0 <LENGTH .INIT-STAGES>>
                     .INIT-STAGES>>)>
    <FORM PROG '()
          !.INIT-STAGES
          .INIT-OPTIONS
          '<COND (<NOT <MAP-SCOPE-START>>
                  <RETURN>)>
          <FORM REPEAT <LIST .VAR>
                <FORM SET .VAR '<MAP-SCOPE-NEXT>>
                <FORM COND <LIST <FORM 0? <FORM LVAL .VAR>>
                                 '<RETURN>>>
                !.BODY>>>

<ROUTINE MAP-SCOPE-INIT-STAGES-FROM-BITS (BITS "AUX" (CNT 0))
    ;"Special case: -1 means all stages in definition order."
    <COND (<=? -1 .BITS>
           <PUT ,SCOPE-CURRENT-STAGES 0 ,SCOPE-CURRENT-STAGES-SIZE>
           %<FORM PROG '()
                  !<MAPF ,LIST
                      <FUNCTION (I "AUX" (S <1 .I>))
                          <FORM PUT ',SCOPE-CURRENT-STAGES
                                    '<SET CNT <+ .CNT 1>>
                                    <PARSE <STRING <SPNAME .S> "-SCOPE-STAGE">>>>
                      ,SCOPE-STAGES>>
           <RETURN>)>
    ;"We don't distinguish between HELD and CARRIED, or ON-GROUND and IN-ROOM."
    <COND (<OR <BTST .BITS ,SF-HELD>
               <BTST .BITS ,SF-CARRIED>>
           <PUT ,SCOPE-CURRENT-STAGES <SET CNT <+ .CNT 1>> ,INVENTORY-SCOPE-STAGE>)>
    <COND (<OR <BTST .BITS ,SF-ON-GROUND>
               <BTST .BITS ,SF-IN-ROOM>>
           <PUT ,SCOPE-CURRENT-STAGES <SET CNT <+ .CNT 1>> ,LOCATION-SCOPE-STAGE>
           <PUT ,SCOPE-CURRENT-STAGES <SET CNT <+ .CNT 1>> ,GLOBALS-SCOPE-STAGE>
           <PUT ,SCOPE-CURRENT-STAGES <SET CNT <+ .CNT 1>> ,LOCAL-GLOBALS-SCOPE-STAGE>)>
    <PUT ,SCOPE-CURRENT-STAGES 0 .CNT>>

<ROUTINE MAP-SCOPE-START ("AUX" V (LEN <GET ,SCOPE-CURRENT-STAGES 0>) (NEED-LIGHT <>))
    <COND (<AND <NOT ,HERE-LIT> <BTST ,MAP-SCOPE-OPTIONS ,MSO-NEED-LIGHT>>
           <SET NEED-LIGHT T>)>
    <COND (.NEED-LIGHT <SETG MAP-SCOPE-STATUS ,MS-NO-LIGHT>)
          (ELSE <SETG MAP-SCOPE-STATUS ,MS-FINISHED>)>
    <SETG SCOPE-CURRENT-STAGE 0>
    <REPEAT ()
        <COND (<G? <SETG SCOPE-CURRENT-STAGE <+ ,SCOPE-CURRENT-STAGE 1>> .LEN>
               ;<SETG MAP-SCOPE-STATUS ,MS-FINISHED>
               <RFALSE>)
              (<AND .NEED-LIGHT <NOT <DARKNESS-F ,M-SCOPE?>>>
               <AGAIN>)
              (<SET V <APPLY <GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE> T>>
               <COND (<==? .V -1>
                      ;"If stage returns -1, abort. It already set MAP-SCOPE-STATUS."
                      <RFALSE>)>
               <RTRUE>)>>>

<ROUTINE MAP-SCOPE-NEXT MSN ("AUX" S N (LEN <GET ,SCOPE-CURRENT-STAGES 0>) (INIT <>) (NEED-LIGHT <>))
    <COND (<AND <NOT ,HERE-LIT> <BTST ,MAP-SCOPE-OPTIONS ,MSO-NEED-LIGHT>>
           <SET NEED-LIGHT T>)>
    <REPEAT ()
        <COND (<G? ,SCOPE-CURRENT-STAGE .LEN>
               ;<SETG MAP-SCOPE-STATUS ,MS-FINISHED>
               <RFALSE>)
              (<AND <OR <NOT .NEED-LIGHT> <DARKNESS-F ,M-SCOPE?>>
                    <SET S <GET ,SCOPE-CURRENT-STAGES ,SCOPE-CURRENT-STAGE>>
                    <OR <NOT .INIT> <APPLY .S T>>
                    <SET N <APPLY .S>>>
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
