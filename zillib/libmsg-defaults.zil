"Default Library Messages"

<PACKAGE "LIBMSG-DEFAULTS">

<USE "LIBMSG">

;"This file defines a set of default library messages for common actions.
  These messages are used by the parser and the standard action routines.
  If a different set of messages is desired, this file may be replaced
  with another file that defines a different set of messages, or the
  messages defined here may be overridden by calling
  REPLACE-LIBRARY-MESSAGES after this file is loaded."

<DEFAULT-LIBRARY-MESSAGES OOPS
    (NO-MISTAKE "Nothing to correct.")
    (NO-WORD "It's OK.")
    (TOO-MANY-WORDS "You can only correct one word at a time.")>

<DEFAULT-LIBRARY-MESSAGES PARSER
    (UNKNOWN-WORD "I don't know the word \"" WORD .WN "\".")
    (TOO-MANY-OBJECTS "That sentence has too many objects.")
    (TOO-MANY-SPECS "That phrase mentions too many objects.")
    (UNEXPECTED-WORD "I didn't expect the word \"" WORD .WN "\" there.")
    (NO-VERB "That sentence has no verb.")
    (UNEXPECTED-DIRECTION "I don't understand what \"" WORD .WN "\" is doing in that sentence.")
    (UNEXPECTED-MODE "You can't use \"" B .W "\" there.")
    (NO-MATCHING-SYNTAX "I don't understand that sentence.")
    (MANY-WINNERS-NOT-ALLOWED "You can only address one person at a time.")
    (MANY-OBJECTS-NOT-ALLOWED "You can't use multiple " IF .INDIRECT? "in" "direct objects with \"" VERB-WORD "\".")
    (DONT-SEE-THAT-HERE "You don't see that here.")
    (TOO-MANY-NUMBERS "You can't use more than one number in a command.")
    (TOO-MANY-MANY "You can't use multiple direct and indirect objects together.")
    (STATUS-LINE-SCORE "Score: ")
    (STATUS-LINE-MOVES "Moves: ")

    (IMPLICIT-TAKE-MANY-1 "[taking ")
    (IMPLICIT-TAKE-MANY-2 "]")
    (IMPLICIT-TAKE-SINGLE "[taking " T .OBJ "]")

    (FAILED-HAVE-CHECK-MANY-1 "You aren't holding ")
    (FAILED-HAVE-CHECK-MANY-2 ".")
    (FAILED-HAVE-CHECK-SINGLE "You aren't holding " T .OBJ ".")

    ;"This is used when 'ALL' matches no objects, either because there are none
      or because 'BUT' excluded all of them."
    (NONE-AVAILABLE "There are none at all available!")

    ;"This is used when the player uses a pronoun to refer to an object that's
      no longer available."
    (NOT-STILL-VISIBLE " no longer here.")

    ;"These are used around the message when GWIM infers a missing object:
        [the coin]
        [to the merchant]"
    (GWIM-1 "[")
    (GWIM-2 "]")

    ;"This is used when 'ANY' picks a random object."
    (INFERRED-RANDOM-OBJECT "[" T .OBJ "]")>

<DEFAULT-LIBRARY-MESSAGES ORPHANING
    ;"These are used to build an orphaning question such as:
        What do you want to examine?
        Which way do you want to go?
        What do you want to attack the troll with?
        Whom do you want Officer Cupcake to interrogate?"
    (WHAT-DO-YOU-WANT-1-DIRECTION "Which way")
    (WHAT-DO-YOU-WANT-1-PERSON "Whom")
    (WHAT-DO-YOU-WANT-1-OBJECT "What")
    (WHAT-DO-YOU-WANT-2 " do you want")
    (WHAT-DO-YOU-WANT-3 " to ")
    (WHAT-DO-YOU-WANT-4 "?")

    ;"These are used to build an orphaning question such as:
        Which do you mean, the red apple or the green apple?"
    (WHICH-DO-YOU-MEAN-1 "Which do you mean, ")
    (WHICH-DO-YOU-MEAN-2 "?")

    (NOT-AN-OPTION "That wasn't an option.")
    (SUCCESS-PARTIAL "That narrowed it down a little. ")
    (TRY-REPHRASING " Try rephrasing the command.")
    (FAILED "That didn't narrow it down at all.")>

<DEFAULT-LIBRARY-MESSAGES JIGS-UP
    (GAME-OVER "    ****  The game is over  ****")
    (PROMPT-WITH-UNDO "Would you like to RESTART, UNDO, RESTORE, or QUIT? >")
    (PROMPT-WITHOUT-UNDO "Would you like to RESTART, RESTORE, or QUIT? >")
    (REPROMPT-WITH-UNDO "(Please type RESTART, UNDO, RESTORE, or QUIT) >")
    (REPROMPT-WITHOUT-UNDO "(Please type RESTART, RESTORE, or QUIT) >")>

<DEFAULT-LIBRARY-MESSAGES YES?
    (PROMPT " (y/n) >")
    (REPROMPT "(Please type y or n) >")>

<DEFAULT-LIBRARY-MESSAGES VERBS
    ;"The action doesn't sensibly apply to this object"
    (NOT-POSSIBLE "That's not something you can " .V ".")
    ;"YES or NO"
    (RHETORICAL "That was a rhetorical question.")
    ;"Missing object we can't/won't infer for the player"
    (BE-SPECIFIC "You'll have to be more specific.")
    ;"The action makes no sense"
    (SILLY "You must be joking.")
    ;"The action sounds ribald"
    (TSD "Not here, not now.")
    ;"The action would invade someone's personal space"
    (YOU-MASHER "I don't think " T .WHOM " would appreciate that.")
    ;"The action could sensibly apply, but isn't worth trying"
    (POINTLESS " doesn't seem like it will help.")>

<DEFAULT-LIBRARY-MESSAGES DARKNESS
    (LOOK "It is pitch black. You can't see a thing.")
    (TOO-DARK "It's too dark.")
    (TOO-DARK-TO-SEE "It's too dark to see anything here.")
    (NOW-DARK "You are plunged into darkness.")
    (NOW-LIT "You can see your surroundings now.")>

<DEFAULT-LIBRARY-MESSAGES INVENTORY
    (CONTENTS-1-SURFACE " (holding ")
    (CONTENTS-1-CONTAINER " (containing ")
    (NOTHING "nothing")
    (CONTENTS-2 ")")
    (HEADER "You are carrying:")
    (WORN " (worn)")
    (LIGHTING " (providing light)")
    (OPEN " (open)")
    (CLOSED " (closed)")
    (EMPTY-HANDED "You are empty-handed.")
    (TOO-DARK "It's too dark to see what you're carrying.")>

<DEFAULT-LIBRARY-MESSAGES WALK
    (CANT-GO-THAT-WAY "You can't go that way.")
    (NO-DIRECTION "You must give a direction to walk in.")
    (BLOCKED-BY-DOOR "You'll have to open " T .DOOR " first.")>

<DEFAULT-LIBRARY-MESSAGES QUIT
    (PROMPT "Are you sure you want to quit?")
    (GOODBYE "Thanks for playing.")
    (ABORTED "OK, not quitting.")>

<DEFAULT-LIBRARY-MESSAGES EXAMINE
    (PLAYER "You look like you're up for an adventure.")
    (OPENABLE CT .OBJ " is " IFELSE .OPEN? "open" "closed" ".")
    (DEFAULT "You see nothing special about " T .OBJ ".")>

<DEFAULT-LIBRARY-MESSAGES LOOK-UNDER
    (DEFAULT "You can't see anything of interest.")>

<DEFAULT-LIBRARY-MESSAGES SEARCH
    (CLOSED CT .OBJ IFELSE .PLURAL? " are" " is" " closed.")
    (EMPTY CT .OBJ IFELSE .PLURAL? " are" " is" " empty.")>

<DEFAULT-LIBRARY-MESSAGES TAKE
    (GET-ME "Not quite.")
    (PICK-ME-UP "You aren't my type.")
    (ALREADY-HELD "You already have that.")
    (BLOCKED-BY-PERSON "That seems to belong to " T .HOLDER ".")
    (BLOCKED-BY-OBJECT CT .HOLDER " is in the way.")
    (TAKE-FROM-INSIDE "You can't pick up " T .OBJ " while you're " IFELSE .SURFACE? "on " "in " "it.")
    (SUCCESS "You pick up " T .OBJ ".")
    (SUCCESS-SHORT "Taken.")
    (SUCCESS-CONTAINER "You reach into " T .HOLDER " and take " T .OBJ ".")
    (TOO-HEAVY-SHORT "You're carrying too much.")
    (TOO-HEAVY "You're carrying too much to lift " T .OBJ ".")>

<DEFAULT-LIBRARY-MESSAGES DROP
    (NOT-HELD "You don't have that.")
    (SUCCESS "You drop " T .OBJ ".")
    (SUCCESS-SHORT "Dropped.")>

<DEFAULT-LIBRARY-MESSAGES PUT-ON
    (NOT-HELD "You don't have that.")
    (PUT-ON-ITSELF "You can't put something on itself.")
    ;"The object's weight is greater than the container's capacity"
    (TOO-BIG "That won't fit on " T .HOLDER ".")
    ;"The total weight would be greater than the container's capacity"
    (NO-ROOM "There's not enough room on " T .HOLDER ".")
    (SUCCESS "You put " T .OBJ " on " T .HOLDER ".")
    (SUCCESS-SHORT "Done.")>

<DEFAULT-LIBRARY-MESSAGES PUT-IN
    (CLOSED CT .HOLDER " is closed.")
    (NOT-OPENABLE "You see no way to put things into " T .HOLDER ".")
    (NOT-HELD "You don't have that.")
    (PUT-IN-ITSELF "You can't put something in itself.")
    ;"The object's size is greater than the container's capacity"
    (TOO-BIG "That won't fit in " T .HOLDER ".")
    ;"The total size would be greater than the container's capacity"
    (NO-ROOM "There isn't enough room in " T .HOLDER ".")
    (SUCCESS "You put " T .OBJ " in " T .HOLDER ".")
    (SUCCESS-SHORT "Done.")>

<DEFAULT-LIBRARY-MESSAGES WEAR
    (SUCCESS "You wear " T .OBJ ".")
    (ALREADY-WORN "You're already wearing that.")>

<DEFAULT-LIBRARY-MESSAGES UNWEAR
    (NOT-WORN "You aren't wearing that.")
    (SUCCESS "You take off " T .OBJ ".")>

<DEFAULT-LIBRARY-MESSAGES EAT
    (NOT-EDIBLE "That's hardly edible.")
    (SUCCESS "You devour " T .OBJ ".")
    (SUCCESS-SHORT "Eaten.")>

<DEFAULT-LIBRARY-MESSAGES VERSION
    (RELEASE-AND-SERIAL "Release " N .RELEASE " / Serial number ")>

<DEFAULT-LIBRARY-MESSAGES THINK-ABOUT
    (THINK-ABOUT-ME "Yes, yes, you're very important.")
    (DEFAULT "You contemplate " T .OBJ " for a bit, but nothing fruitful comes to mind.")>

<DEFAULT-LIBRARY-MESSAGES OPEN
    (ALREADY-OPEN "It's already open.")
    (LOCKED "You'll have to unlock it first.")
    (SUCCESS "You open " T .OBJ ".")
    (SUCCESS-SHORT "Opened.")>

<DEFAULT-LIBRARY-MESSAGES CLOSE
    (ALREADY-CLOSED "It's already closed.")
    (SUCCESS "You close " T .OBJ ".")
    (SUCCESS-SHORT "Closed.")>

<DEFAULT-LIBRARY-MESSAGES WAIT
    (SUCCESS "Time passes.")>

<DEFAULT-LIBRARY-MESSAGES AGAIN
    (NO-COMMAND "Nothing to repeat.")>

<DEFAULT-LIBRARY-MESSAGES READ
    (NOT-HELD "You must be holding that to read it.")>

<DEFAULT-LIBRARY-MESSAGES TURN-ON
    (ALREADY-ON "It's already on.")
    (SUCCESS "You switch on " T .OBJ ".")
    (SUCCESS-SHORT "Switched on.")>

<DEFAULT-LIBRARY-MESSAGES TURN-OFF
    (TURN-ME-OFF <PICK-ONE-R <PLTABLE "Baseball." "Cold showers.">>)
    (NOT-ON "It's already off.")
    (SUCCESS "You switch off " T .OBJ)
    (SUCCESS-SHORT "Switched off.")>

<DEFAULT-LIBRARY-MESSAGES FLIP
    (POINTLESS-WHAT "Taking your frustration out on")>

<DEFAULT-LIBRARY-MESSAGES PUSH
    (PUSH-ME "No, you seem close to the edge.")>

<DEFAULT-LIBRARY-MESSAGES PULL
    (PULL-ME "That would demean both of us.")>

<DEFAULT-LIBRARY-MESSAGES DRINK
    (DEFAULT "You aren't " ITALIC "that" " thirsty.")>

<DEFAULT-LIBRARY-MESSAGES SMELL
    (DEFAULT "You smell nothing unexpected.")>

<DEFAULT-LIBRARY-MESSAGES ATTACK
    (ATTACK-ME "Let's hope it doesn't come to that.")
    (POINTLESS-WHAT "Taking your frustration out on")>

<DEFAULT-LIBRARY-MESSAGES THROW-AT
    (THROW-AT-ME "Get " IFELSE .PLURAL? "them" "it" " yourself.")
    (POINTLESS-WHAT "Taking your frustration out on")>

<DEFAULT-LIBRARY-MESSAGES GIVE
    (GIVE-ME-ALREADY-HELD = TAKE ALREADY-HELD)
    (GIVE-ME = THROW-AT THROW-AT-ME)
    (DEFAULT CT .WHOM IFELSE .PLURAL? " don't" " doesn't" " take " T .OBJ ".")>

<DEFAULT-LIBRARY-MESSAGES TELL
    (DEFAULT-1 "Talking to ")
    (DEFAULT-2-YOURSELF "yourself")
    (DEFAULT-2-OBJECT A .OBJ)
    (DEFAULT-3 ", huh?")>

<DEFAULT-LIBRARY-MESSAGES SING
    (DEFAULT "You give a stirring performance of \"MacArthur Park\". Bravo!")>

<DEFAULT-LIBRARY-MESSAGES DANCE
    (DEFAULT "Dancing is forbidden.")>

<DEFAULT-LIBRARY-MESSAGES WAKE
    (WAKE-ME "If only this were a dream.")>

<DEFAULT-LIBRARY-MESSAGES BURN
    (BURN-ME "What is this, the Friars Club?")
    (POINTLESS-WHAT "Recklessly incinerating")>

<DEFAULT-LIBRARY-MESSAGES UNDO
    (SUCCESS "Previous turn undone.")
    (NO-UNDO-STATE "Cannot undo any further.")
    (FAILED "Undo failed.")
    (NOT-SUPPORTED "Undo is not available in this version.")>

<DEFAULT-LIBRARY-MESSAGES SAVE
    (SAVING "Saving...")
    (FAILED "Save failed.")>

<DEFAULT-LIBRARY-MESSAGES RESTORE
    (FAILED "Restore failed.")>

<DEFAULT-LIBRARY-MESSAGES RESTART
    (PROMPT "Are you sure you want to restart?")
    (ABORTED "OK, not restarting.")>

<DEFAULT-LIBRARY-MESSAGES BRIEF
    (SUCCESS "Brief descriptions.")>

<DEFAULT-LIBRARY-MESSAGES VERBOSE
    (SUCCESS "Verbose descriptions.")>

<DEFAULT-LIBRARY-MESSAGES SUPERBRIEF
    (SUCCESS "Superbrief descriptions.")>

<DEFAULT-LIBRARY-MESSAGES SCRIPT
    (ALREADY-ON "Transcript already on.")
    (SUCCESS "This begins a transcript of ")
    (FAILED "Failed.")>

<DEFAULT-LIBRARY-MESSAGES UNSCRIPT
    (ALREADY-OFF "Transcript already off.")
    (SUCCESS "End of transcript.")
    (FAILED "Failed.")>

<ENDPACKAGE>
