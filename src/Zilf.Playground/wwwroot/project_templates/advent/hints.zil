"Adaptive hint system for Advent"

<DEFSTRUCT HINT VECTOR
    (HINT-NAME ATOM)
    (HINT-PENALTY FIX)
    (HINT-PATIENCE FIX)
    (HINT-EXTENSION <OR FIX FALSE>)
    (HINT-LOCATION <OR ATOM LIST>)
    (HINT-CONDITION <OR FORM FALSE>)
    (HINT-PROMPT STRING)
    (HINT-TEXT STRING)>

<SETG HINT-DEFINITIONS ()>

<DEFINE HINT (NAME "ARGS" FIELDS "AUX" CTOR-ARGS)
    ;"Translate object-style field list into defstruct constructor args:
        ((PENALTY 4) (LOCATION FOO BAR) (CONDITION <BAZ?>))
      becomes:
        ('HINT-PENALTY 4 'HINT-LOCATION (FOO BAR) 'HINT-CONDITION '<BAZ?>)"
    <SET CTOR-ARGS
         <MAPF ,LIST
               <FUNCTION (F "AUX" N V)
                   <COND (<LENGTH? .F 1> <ERROR HINT-FIELD-TOO-SHORT .F>)>
                   <SET V <2 .F>>
                   <SET N <PARSE <STRING "HINT-" <SPNAME <1 .F>>>>>
                   <COND (<NOT <LENGTH? .F 2>> <SET V <REST .F>>)
                         (<==? .N HINT-CONDITION> <SET V <FORM QUOTE .V>>)>
                   <MAPRET <FORM QUOTE .N> .V>>
               .FIELDS>>
    ;"Store the hint definition"
    <SETG HINT-DEFINITIONS (!,HINT-DEFINITIONS
                            <EVAL <FORM MAKE-HINT ''HINT-NAME .NAME !.CTOR-ARGS>>)>
    ;"Define the HNT?name constant"
    <EVAL <FORM CONSTANT <PARSE <STRING "HNT?" <SPNAME .NAME>>> <LENGTH ,HINT-DEFINITIONS>>>>

<DEFINE FINISH-HINTS ()
    ;"Writable tables"
    <CONSTANT HINT-PENALTY-TBL
        ;"This is writable so we can clear the penalty when a hint is shown."
        <LTABLE !<MAPF ,LIST ,HINT-PENALTY ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-TIMER-TBL
        <ITABLE <LENGTH ,HINT-DEFINITIONS> (LENGTH)>>
    
    ;"Pure tables"
    <CONSTANT HINT-PATIENCE-TBL
        <PLTABLE !<MAPF ,LIST ,HINT-PATIENCE ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-EXTENSION-TBL
        <PLTABLE !<MAPF ,LIST
                        <FUNCTION (I)
                            <OR <HINT-EXTENSION .I> <* 30 <HINT-PENALTY .I>>>>
                        ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-LOCATION-TBL
        <PLTABLE !<MAPF ,LIST
                        <FUNCTION (I "AUX" (V <HINT-LOCATION .I>))
                            <COND (<NOT .V> <>)
                                  (<TYPE? .V LIST> <PLTABLE !.V>)
                                  (ELSE <PLTABLE .V>)>>
                        ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-CONDITION-TBL
        <PLTABLE !<MAPF ,LIST
                        <FUNCTION (I "AUX" (V <HINT-CONDITION .I>) RN)
                            <COND (<NOT .V> <>)
                                  (ELSE
                                   <SET RN <PARSE <STRING <SPNAME <HINT-NAME .I>> "-HINT-COND">>>
                                   <EVAL <FORM ROUTINE .RN '() <FORM T? .V>>>)>>
                        ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-PROMPT-TBL
        <PLTABLE !<MAPF ,LIST ,HINT-PROMPT ,HINT-DEFINITIONS>>>
    <CONSTANT HINT-TEXT-TBL
        <PLTABLE !<MAPF ,LIST ,HINT-TEXT ,HINT-DEFINITIONS>>>>

;"Displays a specific hint, deducting points and awarding battery power if this is
  the first time it's been shown."
<ROUTINE SHOW-HINT (N "AUX" P)
    <TELL <GET ,HINT-TEXT-TBL .N> CR>
    <COND (<SET P <GET ,HINT-PENALTY-TBL .N>>
           <SETG SCORE <- ,SCORE .P>>
           <COND (<G=? ,LANTERN-POWER 30>
                  <SETG LANTERN-POWER <+ ,LANTERN-POWER <GET ,HINT-EXTENSION-TBL .N>>>)>
           <PUT ,HINT-PENALTY-TBL .N 0>)>>

;"Interrupt routine to offer a hint if the player seems stuck."
<ROUTINE I-OFFER-HINT ("AUX" (MAX <GET ,HINT-PENALTY-TBL 0>) P C T)
    <COND (<0? .MAX> <RFALSE>)>
    <DO (I 1 .MAX)
        <COND (<SET P <GET ,HINT-PENALTY-TBL .I>>
               <COND (<NOT <IN-HINT-LOCATION? .I>>
                      <PUT ,HINT-TIMER-TBL .I 0>)
                     (ELSE
                      <COND (<OR <NOT <SET C <GET ,HINT-CONDITION-TBL .I>>>
                                 <APPLY .C>>
                             <SET T <GET ,HINT-TIMER-TBL .I>>
                             <PUT ,HINT-TIMER-TBL .I <+ .T 1>>
                             <COND (<=? .T <GET ,HINT-PATIENCE-TBL .I>>
                                    <OFFER-HINT .I>
                                    <RTRUE>)>)>)>)>>
    <RFALSE>>

<ROUTINE OFFER-HINT (N)
    <TELL <GET ,HINT-PROMPT-TBL .N>>
    <COND (<YES?>
           <TELL "I am prepared to offer you a hint, but it will cost you "
                 N <GET ,HINT-PENALTY-TBL .N> " point">
           <OR <1? .N> <TELL !\s>>
           <TELL ". Do you want the hint?">
           <COND (<YES?> <CRLF> <SHOW-HINT .N>)>)>>

<ROUTINE IN-HINT-LOCATION? (N "AUX" (LOCS <GET ,HINT-LOCATION-TBL .N>) MAX)
    <COND (<NOT .LOCS> <RFALSE>)>
    <SET MAX <GET .LOCS 0>>
    <VERSION?
        (ZIP
            <DO (I 1 .MAX)
                <COND (<=? ,HERE <GET .LOCS .I>> <RTRUE>)>>
            <RFALSE>)
        (ELSE
            <T? <INTBL? ,HERE <REST .LOCS 2> .MAX>>)>>

<DEFMAC HINT-SEEN? ('N)
    <FORM NOT <FORM GET ,HINT-PENALTY-TBL .N>>>

<ROUTINE RESPOND-TO-HINT-REQUEST? ("AUX" (MAX <GET ,HINT-PENALTY-TBL 0>) C S U)
    <COND (<0? .MAX> <RFALSE>)>
    <DO (I 1 .MAX)
        <COND (<AND <IN-HINT-LOCATION? .I>
                    <OR <NOT <SET C <GET ,HINT-CONDITION-TBL .I>>>
                        <APPLY .C>>>
               <COND (<HINT-SEEN? .I>
                      <OR .S <SET S .I>>)
                     (ELSE
                      <SET U .I>
                      <RETURN>)>)>>
    <COND (.U <OFFER-HINT .U> <RTRUE>)
          (.S <SHOW-HINT .S> <RTRUE>)
          (ELSE <RFALSE>)>>
