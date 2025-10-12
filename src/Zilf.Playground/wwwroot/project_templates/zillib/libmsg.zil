"Library Message System"

<PACKAGE "LIBMSG">

<USE "READER-MACROS">  ;"for SET-SOURCE-INFO"

<ENTRY DEFAULT-LIBRARY-MESSAGES REPLACE-LIBRARY-MESSAGES LIBRARY-MESSAGE>

;"Library messages are stored in the GVALs of atoms which are inserted into
  OBLISTs created for this purpose.
  For example, the library message SUCCESS in the category TAKE is stored
  in the GVAL of the atom SUCCESS!-TAKE!-LIBRARY-MESSAGES.
  The value stored depends on whether the message is defined an an alias.
  If it is an alias, the value stored is an atom. For example:
    <DEFAULT-LIBRARY-MESSAGES TAKE (TOO-BULKY = TOO-HEAVY)>
  is stored as
    <SETG TOO-BULKY!-TAKE!-LIBRARY-MESSAGES TOO-HEAVY!-TAKE!-LIBRARY-MESSAGES>
  If the message is not an alias, the value stored is a list containing
  the message's EXPANSION. For example:
    <DEFAULT-LIBRARY-MESSAGES TAKE (SUCCESS \"You take \" T .OBJ !\.)>
  is stored as
    <SETG SUCCESS!-TAKE!-LIBRARY-MESSAGES '(\"You take \" T .OBJ !\.)>
  "

<SETG LIBMSG-OL <MOBLIST LIBRARY-MESSAGES>>

<DEFINE CATEGORY-OBLIST (CATEGORY "AUX" (N <SPNAME .CATEGORY>))
    #DECL ((VALUE) OBLIST (CATEGORY) ATOM)
    <MOBLIST <OR <LOOKUP .N ,LIBMSG-OL>
                 <INSERT .N ,LIBMSG-OL>>>>

<DEFINE MESSAGE-ATOM (CATEGORY NAME "AUX" (C-OL <CATEGORY-OBLIST .CATEGORY>) (N <SPNAME .NAME>))
    #DECL ((VALUE CATEGORY NAME) ATOM)
    <OR <LOOKUP .N .C-OL>
        <INSERT .N .C-OL>>>

;"Defines a set of library messages for a given category.
  This set is non-exhaustive, and the category may be extended by additional
  calls to DEFAULT-LIBRARY-MESSAGES.
  Each message may be defined by a list of the form (NAME EXPANSION...) where
  NAME is an atom and EXPANSION is any sequence of values that may appear in a
  TELL. NAMEs must be unique within the CATEGORY, but may be reused in
  different categories.
  Alternatively, a message may be defined as an alias for another message,
  using the forms (ALIAS-NAME = OTHER-NAME) or
  (ALIAS-NAME = OTHER-CATEGORY OTHER-NAME).
  When the library message is invoked by a call to LIBRARY-MESSAGE, the values
  in EXPANSION are passed through to TELL unchanged, except for any LVALs.
  LVALs will be replaced with the values provided at the call site.
  EXPANSION must not contain any additional LVALs besides those whose values
  are provided at the call site.
  Example:
    <DEFAULT-LIBRARY-MESSAGES TAKE
      (SUCCESS \"You take \" T .OBJ !\.)
      (SHORT-SUCCESS \"Taken.\")
      (FIXED-IN-PLACE CT .OBJ \" is fixed in place.\")
      (TOO-HEAVY \"You are unable to lift \" T .OBJ !\.)
      (TOO-BULKY = TOO-HEAVY)>
  "
<DEFINE DEFAULT-LIBRARY-MESSAGES (CATEGORY "ARGS" MESSAGES)
    #DECL ((CATEGORY) ATOM
           (MESSAGES) <LIST [REST <LIST ATOM ANY>]>)
    ;"Insert each message into the OBLIST, raising an error if it already exists."
    <MAPF <>
          <FUNCTION (L "AUX" HEAD MA OC ON)
              <COND (<LENGTH? .L 0>
                     <ERROR MISSING-DEFINITION .CATEGORY>)
                    (<LENGTH? .L 1>
                     <ERROR MISSING-DEFINITION .CATEGORY <1 .L>>)
                    (<NOT <TYPE? <SET HEAD <1 .L>> ATOM>>
                     <ERROR BAD-MESSAGE-NAME .CATEGORY .HEAD>)
                    (<GASSIGNED? <SET MA <MESSAGE-ATOM .CATEGORY .HEAD>>>
                     <ERROR MESSAGE-ALREADY-DEFINED .CATEGORY .HEAD>)
                    (<==? <2 .L> =>
                     ;"It's an alias definition"
                     <COND (<OR <LENGTH? .L 2> <NOT <LENGTH? .L 4>>>
                            <ERROR BAD-ALIAS-DEFINITION .CATEGORY .HEAD>)
                           (<NOT <AND <TYPE? <SET OC <COND (<LENGTH? .L 3> .CATEGORY) (ELSE <3 .L>)>> ATOM>
                                      <TYPE? <SET ON <COND (<LENGTH? .L 3> <3 .L>) (ELSE <4 .L>)>> ATOM>>>
                            <ERROR BAD-ALIAS-TARGET .CATEGORY .HEAD .OC .ON>)
                           (ELSE
                            <SETG .MA <MESSAGE-ATOM .OC .ON>>)>)
                    (ELSE
                     ;"It's a regular definition"
                     <SETG .MA <REST .L>>)>>
          .MESSAGES>
    T>


;"Overrides some of the default messages within a given category.
  The syntax is the same as DEFAULT-LIBRARY-MESSAGES.
  The NAMEs overridden must be defined elsewhere with
  DEFAULT-LIBRARY-MESSAGES.
  Each EXPANSION may only contain the LVALs that were used in the original
  definition.
  If the original definition was an alias, the new definition may be given
  its own EXPANSION, breaking the alias relationship, or it may be redefined as
  an alias of a different message.
  If the original definition was used as the OTHER-NAME in an alias, the new
  definition will affect the alias as well.
  Example:
    <REPLACE-LIBRARY-MESSAGES TAKE
      (SUCCESS \"You pick up \" T .OBJ !\.)
      (TOO-HEAVY CT .OBJ \" is too heavy to lift.\")>
  "
<DEFINE REPLACE-LIBRARY-MESSAGES (CATEGORY "ARGS" MESSAGES)
    #DECL ((CATEGORY) ATOM
           (MESSAGES) <LIST [REST <LIST ATOM ANY>]>)
    <MAPF <>
          <FUNCTION (L "AUX" HEAD MA)
              <COND (<LENGTH? .L 0>
                     <ERROR MISSING-DEFINITION .CATEGORY>)
                    (<LENGTH? .L 1>
                     <ERROR MISSING-DEFINITION .CATEGORY <1 .L>>)
                    (<NOT <TYPE? <SET HEAD <1 .L>> ATOM>>
                     <ERROR BAD-MESSAGE-NAME .CATEGORY .HEAD>)
                    (<NOT <GASSIGNED? <SET MA <MESSAGE-ATOM .CATEGORY .HEAD>>>>
                     <ERROR MESSAGE-UNDEFINED .CATEGORY .HEAD>)
                    (<==? <2 .L> =>
                     ;"It's an alias definition"
                     <COND (<LENGTH? .L 2>
                            <ERROR BAD-ALIAS-DEFINITION .CATEGORY .HEAD>)
                           (ELSE
                            <SETG .MA <MESSAGE-ATOM .CATEGORY <3 .L>>>)>)
                     (ELSE
                      ;"It's a regular definition"
                      <SETG .MA <REST .L>>)>>
          .MESSAGES>
    T>

;"Invokes a library message by expanding and substituting its EXPANSION
  when used as part of the argument list to TELL.
  CATEGORY and NAME are atoms identifying the message, as given in the
  original call to DEFAULT-LIBRARY-MESSAGES.
  The optional BINDINGS argument is a list of the form
    ((NAME1 VALUE1) (NAME2 VALUE2)...)
  where NAMEs are atoms and VALUEs are any values that may appear in a TELL.
  The VALUEs are substituted for the LVALs in the EXPANSION of the message.
  If the message is an alias, the alias is looked up by its OTHER-CATEGORY
  and OTHER-NAME (recursively, if need be), and the BINDINGS are passed
  through to the aliased message.
  An error results if the message is not defined, or if the BINDINGS contain
  any LVALs that are not present in the EXPANSION.
  Example:
    <TELL <LIBRARY-MESSAGE TAKE SUCCESS ((.OBJ ,PRSO))> \".\" CR>
  "
<DEFMAC LIBRARY-MESSAGE (CATEGORY NAME "OPT" ('BINDINGS '()) "AUX" (MA <MESSAGE-ATOM .CATEGORY .NAME>) V)
    #DECL ((VALUE) SPLICE
           (CATEGORY NAME) ATOM
           (BINDINGS) <LIST [REST !<LIST ATOM ANY>]>)
    <COND (<GASSIGNED? .MA>
           <CHTYPE <RESOLVE-MESSAGE-DEFINITION <GVAL .MA> .BINDINGS> SPLICE>)
          (ELSE
           <ERROR NO-SUCH-MESSAGE .CATEGORY .NAME>)>>

<DEFINE RESOLVE-MESSAGE-DEFINITION (DEF BINDINGS "AUX" (TRIED ()))
    #DECL ((VALUE) LIST
           (DEF) <OR ATOM LIST>)
    ;"If DEF is an atom, it's an alias and needs to be looked up.
      Add it to TRIED to avoid infinite recursion and restart the function with AGAIN."
    <REPEAT ()
        <COND (<TYPE? .DEF ATOM>
               <COND (<MEMQ .DEF .TRIED>
                     <ERROR RECURSIVE-MESSAGE-ALIAS .DEF>)>
               <SET TRIED (.DEF !.TRIED)>
               <SET DEF <GVAL .DEF>>)
              (ELSE <RETURN>)>>
    ;"DEF is now a list containing the EXPANSION from a non-alias definition.
      We now need to substitute the BINDINGS into it."
    <RESOLVE-IMPL .DEF .BINDINGS>
>

<DEFINE RESOLVE-IMPL (STRUC BINDINGS "AUX" (P <PRIMTYPE .STRUC>) (T <TYPE .STRUC>) B)
    <COND (<TYPE? .STRUC LVAL>
           <COND (<SET B <GET-BINDING <CHTYPE .STRUC ATOM> .BINDINGS>>
                  <2 .B>)
                 (ELSE
                  <ERROR NO-SUCH-BINDING .STRUC>)>)
          (<==? .T ATOM>
           ;"It's probably on the wrong OBLIST, so look it up in the current context."
           <PARSE <SPNAME .STRUC>>)
          (<OR <==? .T STRING>
               <NOT <STRUCTURED? .STRUC>>>
           .STRUC)
          (<NOT <AND <GBOUND? .P> <APPLICABLE? ,.P>>>
           <ERROR CANNOT-RECREATE-PRIMTYPE .P>)
          (ELSE
           <SET-SOURCE-INFO <CHTYPE <APPLY ,.P !<RESOLVE-ELEMS .STRUC .BINDINGS>> .T> .STRUC>)>>

<DEFINE RESOLVE-ELEMS (STRUC BINDINGS)
    <MAPF ,VECTOR
          <FUNCTION (I) <RESOLVE-IMPL .I .BINDINGS>>
          .STRUC>>

<DEFINE GET-BINDING (ATM BINDINGS "AUX" (NAME <SPNAME .ATM>))
    #DECL ((VALUE) <OR !<LIST ATOM ANY> FALSE>
           (ATM) ATOM
           (BINDINGS) <LIST [REST !<LIST ATOM ANY>]>)
    <MAPF <>
          <FUNCTION (PAIR)
              <COND (<=? <SPNAME <1 .PAIR>> .NAME>
                     <MAPLEAVE .PAIR>)>>
          .BINDINGS>>

<ENDPACKAGE>
