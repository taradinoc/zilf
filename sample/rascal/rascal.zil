"RASCAL: a tiny roguelike for ZILF"

<VERSION XZIP>
<CONSTANT RELEASEID 4>

<COMPILATION-FLAG-DEFAULT DEBUG <>>

<CONSTANT IFID-ARRAY
	<PTABLE (STRING) "UUID://191CA66D-29E1-4C20-8A2C-CE80B0633521//">>

;"Metadata for publishing"
<SETG PUBLISH-TITLE "Rascal">
<SETG PUBLISH-AUTHOR "Tara McGrew">
<SETG PUBLISH-COVER-ART "coverart.jpg">
<SETG PUBLISH-DESCRIPTION "A tiny roguelike for ZILF.">
<SETG PUBLISH-THEME "Litera">
<SETG PUBLISH-SOURCE? T>
<SETG PUBLISH-EXTRAS (("poster.png" "Poster"))>

;"Rascal's ASCII art (RASCII art?) uses '|', the traditional ZIL newline
  character, so we need to change CRLF-CHARACTER in order to be able to put it
  into a string. Since the most reasonable alternative ('^', Inform's newline
  character) is also used by the ASCII art, and future ASCII art might use any
  other character found on the keyboard, we forgo all of them and use this
  Unicode character that resembles the 'enter key' symbol."
<SETG CRLF-CHARACTER !\↲>

<USE "QQ">

;"Program entry point. This is the first routine defined because it has to be in
  the first 64k of memory."

<ROUTINE GO ()
    <CHECK-SCREEN>
    <SPLASH>
    <INIT>
    <RASCAL-MAIN-LOOP>>

;"balance.zil defines constants used elsewhere, so it has to be included first."
<INSERT-FILE "balance">

<INSERT-FILE "combat">
<INSERT-FILE "dungeon">
<INSERT-FILE "explore">
<INSERT-FILE "itemdef">
<INSERT-FILE "objects">
<INSERT-FILE "inventory">
<INSERT-FILE "loot">
<INSERT-FILE "misc">
<INSERT-FILE "random">
<INSERT-FILE "shop">
<INSERT-FILE "state">
<INSERT-FILE "statistics">
<INSERT-FILE "ui">

;"The aforementioned CRLF-CHARACTER at work:"
<CONSTANT GAME-BANNER "RASCAL↲
A Rogueish Romp by Tara McGrew">

;"Split '+' out into its own word for ease of parsing"
<SETG SIBREAKS ".,\"+">

;"interior.zil will redefine a few sections of the library"
<DELAY-DEFINITION HOOK-BEFORE-PARSER>
<DELAY-DEFINITION HOOK-MID-PARSE-CONSUME>
<DELAY-DEFINITION INDISTINGUISHABLE?>
<DELAY-DEFINITION FAILS-HAVE-CHECK?>

<INSERT-FILE "parser">
<INSERT-FILE "interior">
<INSERT-FILE "npc">
