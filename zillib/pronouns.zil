"Pronouns"

<SETG PRONOUN-DEFINITIONS ()>

<DEFSTRUCT PRONOUN VECTOR
    (PRO-NAME ATOM)
    (PRO-BINDINGS LIST)
    (PRO-STMTS LIST)>

<DEFINE PRONOUN (NAME BINDINGS "ARGS" STMTS "AUX" CTOR-ARGS)
    <SET CTOR-ARGS (''PRO-NAME .NAME
                    ''PRO-BINDINGS .BINDINGS
                    ''PRO-STMTS <FORM QUOTE .STMTS>)>
    <SETG PRONOUN-DEFINITIONS (!,PRONOUN-DEFINITIONS
                               <EVAL <FORM MAKE-PRONOUN !.CTOR-ARGS>>)>>

<DEFINE FINISH-PRONOUNS ("AUX" RTN)
    <MAPF <>
          <FUNCTION (P "AUX" N RTN)
              <SET N <PRO-NAME .P>>
              <BIND ((TBLSIZE <+ 1 ,P-MAX-OBJECTS>)
                     (TBLFLAGS <VERSION? (ZIP '(BYTE)) (ELSE '(WORD))>)
                     OBJS-TBL-NAME CONDITION)
                  ;"Define table P-PRO-THEM-OBJS to hold the objects."
                  <SET OBJS-TBL-NAME <PARSE <STRING "P-PRO-" <SPNAME .N> "-OBJS">>>
                  <EVAL <FORM CONSTANT .OBJS-TBL-NAME '<ITABLE .TBLSIZE .TBLFLAGS>>>
                  
                  ;"Define routine PRO-TRY-SET-THEM to try to set the pronoun."
                  <SET RTN-NAME <PARSE <STRING "PRO-TRY-SET-" <SPNAME .N>>>>
                  <SET CONDITION <PRO-STMTS .P>>
                  <COND (<LENGTH? .CONDITION 1> <SET CONDITION <1 .CONDITION>>)
                        (ELSE <SET CONDITION <FORM PROG '() !.CONDITION>>)>
                  <SET RTN
                      <FORM ROUTINE .RTN-NAME
                                    <LIST <1 <PRO-BINDINGS .P>>
                                          PRO?OBJS
                                          !<REST <PRO-BINDINGS .P>>>
                            <FORM COND
                                  <LIST .CONDITION
                                        <FORM
                                            <PARSE <STRING "PRO-FORCE-SET-" <SPNAME .N>>>
                                            '.PRO?OBJS>>>>>
                  <EVAL .RTN>
                  
                  ;"Define routine PRO-FORCE-SET-THEM to unconditionally set the pronoun."
                  <SET RTN-NAME <PARSE <STRING "PRO-FORCE-SET-" <SPNAME .N>>>>
                  <SET RTN
                      <FORM ROUTINE .RTN-NAME '(PRO?OBJS)
                            <FORM TRACE 4 <STRING "[setting " <SPNAME .N> " to ">>
                            <FORM TRACE-DO 4
                                <FORM LIST-OBJECTS '.PRO?OBJS <> <+ ,L-PRSTABLE ,L-THE>>
                                '<TELL "]" CR>>
                            <FORM COPY-PRSTBL
                                '.PRO?OBJS
                                <FORM GVAL .OBJS-TBL-NAME>>
                            '<RTRUE>>>
                  <EVAL .RTN>>>
          ,PRONOUN-DEFINITIONS>

    ;"Define SET-PRONOUNS"
    <SET RTN
        <FORM ROUTINE SET-PRONOUNS '(O OBJS "AUX" PT MAX)
            '<COND (<=? .O <> ,ROOMS> <RFALSE>)
                   (<SET PT <GETPT .O ,P?PRONOUN>>
                    <SET MAX <- </ <PTSIZE .PT> 2> 1>>
                    <DO (I 0 .MAX)
                        <APPLY <GET .PT .I> .OBJS>>
                    <RTRUE>)>
            !<MAPF ,LIST
                   <FUNCTION (P "AUX" N RTN-NAME OBJS-TBL-NAME)
                       <SET N <PRO-NAME .P>>
                       <SET RTN-NAME <PARSE <STRING "PRO-TRY-SET-" <SPNAME .N>>>>
                       <FORM .RTN-NAME '.O '.OBJS>>
                   ,PRONOUN-DEFINITIONS>>>
    <EVAL .RTN>

    ;"Define EXPAND-PRONOUN"
    <SET RTN
        <FORM ROUTINE EXPAND-PRONOUN '(W OBJS "AUX" CNT)
            <FORM COND
                  !<MAPF ,LIST
                         <FUNCTION (P "AUX" N OBJS-TBL-NAME)
                             <SET N <PRO-NAME .P>>
                             <SET OBJS-TBL-NAME <PARSE <STRING "P-PRO-" <SPNAME .N> "-OBJS">>>
                             <LIST <FORM =? '.W <VOC <SPNAME .N> OBJECT>>
                                   <FORM COPY-PRSTBL .OBJS-TBL-NAME '.OBJS>>>
                         ,PRONOUN-DEFINITIONS>
                  '(ELSE <RFALSE>)>
            '<SET CNT <GETB .OBJS 0>>
            '<COND (<0? .CNT>
                    <TELL "You haven't seen any \"" B .W "\" yet." CR>
                    <RETURN ,EXPAND-PRONOUN-FAILED>)>
            '<COND (<NOT <STILL-VISIBLE-CHECK .OBJS>>
                    <RETURN ,EXPAND-PRONOUN-FAILED>)>
            '<COND (<1? .CNT> <RETURN <GET/B .OBJS 1>>)
                   (ELSE <RETURN ,MANY-OBJECTS>)>>>
    <EVAL .RTN>
    
    ;"Define V-PRONOUNS"
    <SET RTN
        <FORM ROUTINE V-PRONOUNS '()
            !<MAPF ,LIST
                   <FUNCTION (P "AUX" N OBJS-TBL-NAME)
                       <SET N <PRO-NAME .P>>
                       <SET OBJS-TBL-NAME <PARSE <STRING "P-PRO-" <SPNAME .N> "-OBJS">>>
                       <FORM PROG '()
                           <FORM TELL <STRING <SPNAME .N> " means ">>
                           <FORM LIST-OBJECTS
                                 <FORM GVAL .OBJS-TBL-NAME>
                                 <>
                                 <+ ,L-PRSTABLE ,L-THE ,L-SCENERY>>
                           <FORM TELL "." CR>>>
                   ,PRONOUN-DEFINITIONS>>>
    <EVAL .RTN>>

<CONSTANT EXPAND-PRONOUN-FAILED -1>

;"Sets the appropriate pronouns to refer to an object."
<ROUTINE THIS-IS-IT (O)
    <PUTB ,P-XOBJS 0 1>
    <PUT/B ,P-XOBJS 1 .O>
    <SET-PRONOUNS .O ,P-XOBJS>>

;"Sets the appropriate pronouns to refer to the contents of an object,
  possibly after filtering through a routine."
<ROUTINE CONTENTS-ARE-IT (CTNR "OPT" FILTER "AUX" N)
    <MAP-CONTENTS (I .CTNR)
        <COND (<OR <NOT .FILTER> <APPLY .FILTER .I>>
               <SET N <+ .N 1>>
               <PUT/B ,P-XOBJS .N .I>
               <COND (<=? .N ,P-MAX-OBJECTS> <RETURN>)>)>>
    <PUTB ,P-XOBJS 0 .N>
    <COND (<0? .N> <RETURN>)
          (<1? .N> <SET N <GET/B ,P-XOBJS 1>>)
          (ELSE <SET N ,MANY-OBJECTS>)>
    <SET-PRONOUNS .N ,P-XOBJS>>

;"Helper for the PRONOUN property. This turns (PRONOUN IT HIM) into
  (PRONOUN PRO-FORCE-SET-IT PRO-FORCE-SET-HIM)."
<DEFINE PRONOUN-PROPSPEC (L)
    <CONS <>
          <MAPF ,LIST
                <FUNCTION (A "AUX" R)
                    <SET R <PARSE <STRING "PRO-FORCE-SET-" <SPNAME .A>>>>
                    <COND (<NOT <TYPE? <GETPROP .R ZVAL> ROUTINE>>
                           <ERROR NO-SUCH-PRONOUN .A>)
                          (ELSE .R)>>
                <REST .L>>>>

<PUTPROP PRONOUN PROPSPEC PRONOUN-PROPSPEC>
