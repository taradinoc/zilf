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
                     OBJS-TBL-NAME)
                  ;"Define table P-PRO-THEM-OBJS to hold the objects."
                  <SET OBJS-TBL-NAME <PARSE <STRING "P-PRO-" <SPNAME .N> "-OBJS">>>
                  <CONSTANT .OBJS-TBL-NAME <ITABLE .TBLSIZE .TBLFLAGS>>
                  
                  ;"Define routine PRO-SET-THEM to try to set the pronoun."
                  <SET RTN-NAME <PARSE <STRING "PRO-SET-" <SPNAME .N>>>>
                  <SET RTN
                      <FORM ROUTINE .RTN-NAME
                                    <LIST <1 <PRO-BINDINGS .P>>
                                          PRO?OBJS
                                          !<REST <PRO-BINDINGS .P>>>
                            <FORM COND
                                  <LIST <FORM PROG '() !<PRO-STMTS .P>>
                                        <FORM TRACE 3 "[setting " <SPNAME .N> "]" CR>
                                        <FORM COPY-PRSTBL
                                              <FORM LVAL PRO?OBJS>
                                              <FORM GVAL .OBJS-TBL-NAME>>>>>>
                  <EVAL .RTN>>>
          ,PRONOUN-DEFINITIONS>

    ;"Define SET-PRONOUNS"
    <SET RTN
        <FORM ROUTINE SET-PRONOUNS '(O OBJS)
            '<COND (<=? .O <> ,ROOMS> <RFALSE>)>
            !<MAPF ,LIST
                   <FUNCTION (P "AUX" N RTN-NAME OBJS-TBL-NAME)
                       <SET N <PRO-NAME .P>>
                       <SET RTN-NAME <PARSE <STRING "PRO-SET-" <SPNAME .N>>>>
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
                                 <+ ,L-PRSTABLE ,L-THE>>
                           <FORM TELL "." CR>>>
                   ,PRONOUN-DEFINITIONS>>>
    <EVAL .RTN>>

<CONSTANT EXPAND-PRONOUN-FAILED -1>
