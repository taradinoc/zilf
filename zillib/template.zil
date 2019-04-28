<PACKAGE "TEMPLATE">

<ENTRY OBJECT-TEMPLATE ADDITIVE-PROPERTIES>

"Usage:

<OBJECT-TEMPLATE
    LIGHT-ROOM = ROOM (FLAGS LIGHTBIT)
    PROP = OBJECT (FLAGS NDESCBIT) (ACTION PROP-F) (LOC LOCAL-GLOBALS)
    FURNITURE = OBJECT (FLAGS CONTBIT OPENBIT SURFACEBIT) (ACTION FURNITURE-F)
    NPC = OBJECT (FLAGS PERSONBIT) (ACTION NPC-F)

The template functions created from those definitions can be used like
OBJECT or ROOM:

<LIGHT-ROOM MEADOW
    (DESC \"Meadow\")
    (IN ROOMS)>

Properties passed to the template functions will override the default values
in the template, for most properties:

<PROP ROAD
    (ACTION ROAD-F)>   ;[PROP-F IS NOT USED]

However, for properties listed in ,ADDITIVE-PROPERTIES (such as FLAGS),
the new values will be appended.  The old values can be removed with a
special syntax:

<NPC ROBOT
    (FLAGS ROBOTBIT '<NOT PERSONBIT>)>

Finally, the property values in the template are evaluated when defining an
object (whether or not they're overridden!):

<SETG CHAPTER-NUMBER 0>
<OBJECT-TEMPLATE
    CHAPTER = OBJECT (NUMBER <SETG CHAPTER-NUMBER <+ ,CHAPTER-NUMBER 1>>)>

>"

<SETG ADDITIVE-PROPERTIES '(SYNONYM ADJECTIVE GLOBAL FLAGS)>

<DEFINE OBJECT-TEMPLATE ("ARGS" ARGS)
    <MAPF <>
          <FUNCTION (X) <HANDLE-DEF !.X>>
          <SPLIT-DEFS .ARGS>>>

<DEFINE SPLIT-DEFS (L "AUX" (NAME <>) (TYPE <>) (PROPS '()) (SIDE LEFT))
    <MAPF ,LIST
          <FUNCTION (X)
              <COND (<==? .SIDE LEFT>
                     <COND (<AND <NOT .NAME> <TYPE? .X ATOM>>
                            <SET NAME .X>)
                           (<AND .NAME <==? .X =>>
                            <SET SIDE RIGHT>)
                           (ELSE <ERROR UNEXPECTED-TOKEN .X>)>)
                    (<AND <NOT .TYPE> <MEMQ .X '(ROOM OBJECT)>>
                     <SET TYPE .X>)
                    (<AND .TYPE <TYPE? .X LIST>>
                     <SET X <CANONICALIZE-PROP .X>>
                     <SET PROPS (!.PROPS <1 .X> <REST .X>)>)
                    (<AND .TYPE <TYPE? .X ATOM>>
                     <SET SIDE LEFT>
                     <MAPRET (.NAME .TYPE .PROPS)>
                     <SET NAME .X>
                     <SET TYPE <>>
                     <SET PROPS '()>)
                    (ELSE <ERROR UNEXPECTED-TOKEN .X>)>
              <MAPRET>>
          (!.L $END)>>

<DEFINE CANONICALIZE-PROP (DEF)
    <COND (<AND <==? <1 .DEF> IN>
                <==? <LENGTH? .DEF 2> 2>
                <TYPE? <2 .DEF> ATOM>>
           <LIST LOC <2 .DEF>>)
          (ELSE .DEF)>>

<DEFINE HANDLE-DEF (NAME TYPE PROPS "AUX" F)
    <SET F <FORM DEFINE .NAME '(NAME "TUPLE" PROPS "AUX" PS)
                 <FORM SET PS <FORM MERGE-PROPS .PROPS '.PROPS>>
                 <FORM .TYPE '.NAME '!.PS>>>
    <EVAL .F>>

<DEFINE MERGE-PROPS (OLD NEW "AUX" RES A)
    <SET RES <LIST !.OLD>>
    <MAPF <>
          <FUNCTION (X)
              <SET X <CANONICALIZE-PROP .X>>
              <BIND ((N <1 .X>) (VS <REST .X>) I)
                  <COND (<SET I <MEMQ .N .RES>>
                         <2 .I <MERGE-PROP-VALUES .N <2 .I> .VS>>)
                        (ELSE
                         <SET RES (!.RES .N .VS)>)>>>
          .NEW>
    <MAPF ,LIST
          <FUNCTION (X)
              <COND (<TYPE? .X ATOM> <SET A .X> <MAPRET>)
                    (ELSE <LIST .A !.X>)>>
          .RES>>

<DEFINE FILTER (PRED L)
    <MAPF ,LIST
          <FUNCTION (X) <COND (<APPLY .PRED .X> .X) (ELSE <MAPRET>)>>
          .L>>

<DEFINE MERGE-PROP-VALUES (NAME OLD NEW)
    <COND (<MEMQ .NAME ,ADDITIVE-PROPERTIES>
           <MERGE-ADDITIVE-PROP-VALUES .OLD .NEW>)
          (ELSE .NEW)>>

<DEFINE MERGE-ADDITIVE-PROP-VALUES (OLD NEW
                                    "AUX"
                                    (RES (<> !.OLD))
                                    (TAIL <FIND-TAIL .RES>)
                                    EXCL)
    <MAPF <>
          <FUNCTION (X)
              <COND (<DECL? .X '!<FORM 'NOT ANY>>
                     <SET EXCL <2 .X>>
                     <PUTREST .RES
                              <FILTER <FUNCTION (X) <N=? .X .EXCL>>
                                      <REST .RES>>>
                     <SET TAIL <FIND-TAIL .RES>>)
                    (ELSE
                     <SET TAIL <REST <PUTREST .TAIL (.X)>>>)>>
          .NEW>
    <REST .RES>>

<DEFINE FIND-TAIL (L)
    <REPEAT ()
        <COND (<OR <EMPTY? .L> <EMPTY? <REST .L>>>
               <RETURN .L>)
              (ELSE
               <SET L <REST .L>>)>>>

<ENDPACKAGE>
