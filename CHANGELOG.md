# Change Log

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to
[Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- You can now replace the `INV-EXTRA-DETAILS` section, containing
  `INV-PRINT-EXTRA-DETAILS` and `INV-SAME-EXTRA-DETAILS?`, to add more details
  like "(worn)" to the inventory listing; or you can override the defaults by
  replacing `INV-PRINT-DETAILS` (and probably also `INV-INDISTINGUISHABLE?`).

- Using `GET` with a second argument that looks like a property name now issues
  warning ZIL0513.

### Changed

- Updated `zillib/LICENSE.txt` to clarify that you may redistribute games
  compiled for targets other than Z-code (i.e., Glulx and Cornerstone) and that
  you may satisfy the redistribution requirement by printing the compiler
  version number (the library version number is no longer required).

- `zillib` no longer has its own version number separate from the compiler.
  `,ZILLIB-VERSION` is defined as equal to `,ZIL-VERSION`, i.e., the compiler
  version number.

### Fixed

- Fixed a bug where `SUPPRESS-WARNINGS?` could only suppress one warning code
  at a time.

- Fixed `INSERT-HELD-WORD` and `REPLACE-HELD-WORD` cutting off the last
  character in V5+.

- Fixed `GO` breaking the object tree when used with an object instead of a
  direction.

## [1.8](April 8, 2026)

### Added

- Added an experimental feature to compile to the Cornerstone VM, a.k.a. the
  μ-Machine, using the `--cornerstone` switch. This mode provides most of the
  functionality of Z-Machine version 3, including a status bar, wrapped text
  output, and editable text input. It doesn't provide save/restore or restart;
  those operations will always fail. It also doesn't provide access to the
  unique features of the μ-Machine such as module-level globals, file channels,
  or memory allocation.

  The output is `.cas` assembly code, which can be assembled into `.mme` and
  `.obj` files with [Chisel](https://github.com/taradinoc/linchpin) and then run
  with [Linchpin](https://github.com/taradinoc/linchpin) or MME (the original
  Cornerstone interpreter). If Chisel is installed in the same location as ZILF,
  it will be invoked automatically (unless skipped with `-S`).

- `COLOR` and `TCOLOR` are now allowed on Glulx, and will work as long as the
  interpreter supports the GarGlk color extensions.

- Added a new form of comment: two semicolons (`;;`) turn the rest of the line
  into a comment, without the need to add quotes or backslashes.

## [1.7] (March 26, 2026)

### Added

- Added a new execution mode: `zilf new <project-name>` will create a new
  ZIL file, in a new directory, using the `empty.zil` sample as a template.

- When using `BLORB-PICTURE` to bundle image resources into a Blorb file, ZILF
  now also packages the story file after assembly into a `.zblorb` or `.gblorb`
  file (unless the `-S` option is used to stop before assembly).

- Added the ability to bundle the game, an interpreter, and (optionally) the
  game's source code into a static website, suitable for online or offline play.
  This is accomplished with the `zilf publish` command (or by adding `--publish`
  to your regular build command). The GVALs of `PUBLISH-TITLE`,
  `PUBLISH-AUTHOR`, `PUBLISH-COVER-ART`, `PUBLISH-DESCRIPTION`, `PUBLISH-THEME`,
  `PUBLISH-SOURCE?`, and `PUBLISH-EXTRAS` will control how the site is
  generated. You can also run the new tool ZilfPub on its own to generate a site
  for any story file, even one not produced with ZILF.

- When running `zilf repl`, typing `help`, `quit`, or `exit` will print a
  reminder of how to exit the REPL. You can also cancel multiline input by
  typing `.` on a line by itself.

### Changed

- `<VERSION GLULX>` can now be used to select Glulx output from source code.
  It cannot be combined with `--glulx16`, which is meant to be used to emulate
  the Z-machine.

- Assigning a default value to a required argument in ROUTINE or DEFINE now
  produces error MDL0135 instead of a misleading MDL0117.

## [1.6.1] (March 2, 2026)

### Fixed

- Fixed a bug where assembly output used a Unicode minus sign for negative
  numbers on some locales, causing assembly errors.

## [1.6] (March 1, 2026)

### Added

- Added support for the `BUFSCR` opcode in V6 (aka `buffer_screen`) in ZAPF and
  ZILF, and the 4-argument forms of `SAVE` and `RESTORE` in ZILF.

- Added library message `NO-SPECS` for commands with empty noun phrases, like
  `GET THE`.

- Indistinguishable objects are now combined in listings (contents, inventory,
  etc.), as long as the indistinguishable objects have a `PDESC` property
  containing their plural name. Inventory listings use `INV-INDISTINGUISHABLE?`
  to decide whether to combine objects, accounting for their listed state (worn,
  providing light, etc.).

- The parser now understands plurals, as long as the relevant objects have
  vocab words set in their `PLURAL` properties (using the same syntax as
  `SYNONYM`). `TAKE BANANAS` is equivalent to `TAKE ALL BANANAS`.

- The parser now understands quantifiers, i.e., numbers before parts of a noun
  phrase. `TAKE TWO BANANAS` (equivalent to `TAKE ANY TWO BANANAS`) will cause
  the parser to pick two bananas arbitrarily, or to give an error if fewer than
  two are available.

- Added warning MDL0134 for a `ROUTINE` that seems to contain nested definitions
  like `ROUTINE` or `GLOBAL`, which typically means a close bracket is missing.

- `STATUS-LINE` can now have conditional overloads: add multiple status line
  definitions with the same name, distinguished by a `(WHEN ...)` clause. The
  default status line templates use this for condensed layouts on small screens.

### Changed

- Optimized code generation for INC/SUB pairs, DEC/ADD pairs, and INC/DEC pairs.

### Fixed

- Error MDL0440 is properly issued when an expression requires temporary
  variables that cause a routine to exceed the Z-machine limit of 15.

- Fixed a bug where `PROG` et al. would sometimes give the wrong error message
  for a type mismatch.

- Fixed a bug where Unicode characters weren't translated to ZSCII properly when
  used as character literals (`!\x`).

## [1.5] (February 10, 2026)

### Added

- Added support for the `TCOLOR` opcode (aka `set_true_colour`) in ZAPF and
  ZILF, and the V6 form of `COLOR` in ZILF.

- New library messages under `PARSER`: `PROMPT`, `NOTHING-ENTERED`, and
  `MANY-HEADER`.

- Added `HOOK-BEFORE-READLINE` and `HOOK-AFTER-READLINE`.

- Added `HOOK-MID-PARSE-CONSUME`, which can modify or delete words just before
  the parser sees them.

- Added error `MDL0440` when defining a routine with too many local variables.

- The parser can now remove indistinguishable objects from consideration before
  asking the player which one they mean, to avoid nonsensical questions like
  "Which do you mean, the cube or the cube?". By default, nothing is considered
  indistinguishable; the game has to opt in by replacing the definition of
  `INDISTINGUISHABLE?`. The function `DISTINGUISHABLE-BY-VOCAB?` may be useful
  when writing such a replacement. The parser will keep one of each set of
  indistinguishable objects and ignore the rest.

- Added warnings `ZIL0510` and `ZIL0511` for suspicious complex forms in a DO
  loop's condition and increment parts, respectively.

- Added `rascal` sample.

### Changed

- Made `IN-WTBL?` and `IN-BTBL?` into macros on versions where they map directly
  to `INTBL?`.

### Fixed

- The `EVERYWHERE` search flag now looks inside closed containers and people.

- ZAPF: Defining a function with too many local variables no longer causes
  extra "local labels not allowed outside a function" errors.

- Fixed `REMOVE-SYNTAX` breaking verb numbering when removing all the syntax
  lines for a particular verb.

- Fixed Unicode characters causing assembler errors.

- A Unicode character used as `CRLF-CHARACTER` no longer gets added to the
  Unicode translation table.

- The abbreviation finder now correctly counts substrings that overlap with
  themselves (e.g. repeated characters like "------"), resulting in better
  abbreviation choices in some cases.

### Removed

- Removed `dragon` sample. Those wishing to fight dragons are encouraged to try
  `rascal` instead.

## [1.4] (January 25, 2026)

### Added

- Added a scoring system, which can be enabled with `<SETG USE-SCORING? T>`
  before inserting the parser. If enabled, you must also define the `MAX-SCORE`
  constant. This gets you score notifications (controlled by the command
  `NOTIFY [ON/OFF]`) and `<AWARD-POINTS 10>` (which simply adds to `SCORE`).
  If you replace the `PRINT-RANK` definition section, you can define a
  `PRINT-RANK` routine to give the player a rating based on their score.

  You can also classify the points you award, using `SCORING-ACHIEVEMENTS` and a
  second argument to `AWARD-POINTS`; see `advent.zil` for an example. The player
  can see the classification with `FULL SCORE`.

- Added `hooks.zil` to keep track of multiple features that need to run "finish"
  functions before compilation starts.

- Added touchability checks. Many of the standard syntax lines now have the
  `TOUCH` flag on one of their objects, which means the player must be able to
  touch the object for the action to proceed. Specifically, there must not be
  a closed container between the player and the object, even a transparent one.
  The touch check happens after the `HAVE`/`TAKE` check, and before preactions.

  When the touch check fails, before blocking the action, the parser calls the
  `CONTFCN` on every closed container between the player and the object, with
  `M-BLOCKER` as the argument. The `CONTFCN` can return -1 to let the action
  proceed anyway, 0 to block the action, or 1 if it handles the action itself.

- Added a `NEW-SFLAGS` syntax to define new additive search flags that don't
  replace the defaults. `HAVE`, `TAKE`, and `MANY` already worked this way; the
  library uses the new syntax to define `TOUCH`.

- Added a `.CREATOR` directive to ZAPF to set the creator info in the header,
  which can now be up to 8 characters. The command line options take precedence.

### Changed

- Optimized code generation for some cases where a variable is set to a constant
  and immediately tested for zero.

- Changed compiled-in metadata to reflect the ZILF version number.

- Optimized code generation for arithmetic operations when multiple consecutive
  arguments, but not all, are constants.

- The words "me" and "myself" are now implemented as pronouns in `pronoun.zil`
  rather than synonyms of the `PLAYER` object. They expand to the value of
  `CURRENT-PLAYER`.

### Fixed

- Pronouns can now be used with AND.

- Fixed `V-UNWEAR` and `V-TELL-ABOUT` not using the library message system.

- Fixed grammar of the error message when taking is blocked by a plural object.

- Fixed uncaught exception when using a non-comparable type with SORT without
  providing a comparison function.

- Fixed incorrect type name in some error messages when passing the wrong type
  to a SUBR.

## [1.3] (January 17, 2026)

### Added

- Similar to the `#2 101010` syntax for binary numbers, hexadecimal numbers can
  now be entered like `#16 ABC123`.

- Added Blorb generation. `<BLORB-PICTURE "path/to/image.png">` returns a unique
  resource number for the image (PNG or JPEG). If any images are added this way,
  ZILF will write them to a `.blorb` file after compilation. The story file
  isn't added to the Blorb, so if you want to distribute your game as a single
  file, you'll need to use something like
  [BlorbTool](https://eblong.com/zarf/blorb/blorbtool/run.html) to add it
  afterward. This is most likely only useful when compiling to Glulx or V6.

- The `--ide-info` JSON report now includes diagnostic source spans (start/end
  line and column) for file-backed origins, and `formatVersion` is now `2`.

- Added `WIDE`, which `--glulx16` games can use to perform 32-bit operations in
  limited contexts. (Under `--glulx`, it's a no-op.)

- Implemented `POP` for Z-machine versions below 6.

- Implemented the `"CALL"` binding, which lets a function bind the `FORM` that
  was used to call it (in place of all other argument bindings), as in MDL.

- `REMOVE` can now be used as a synonym for `TAKE OFF`.

- Added warning ZIL0509 when `FSET` is used as a condition rather than `FSET?`.

- Added `-D` command line option to define one or more compilation flags. For
  example, instead of adding `<COMPILATION-FLAG DEBUG T>` to the source code,
  you can run `zilf -D DEBUG`.

### Changed

- The Cloak of Darkness sample has been updated to be more faithful to the
  original. There's also an illustrated version for Glulx, cloak_glk, included
  as an example of using Glk.

- Swapped the definition order of PUT ON and PUT IN, so that the parser will
  infer PUT ON instead of PUT IN if both are equally applicable (e.g. if only
  a surface is available).

- If a missing object has to be inferred, but that inference would fail for some
  of the candidate syntax lines, the parser now prefers the line(s) where it
  would succeed. For example, PUT IN will be preferred over PUT ON if only a
  container is available.

- Debugging verbs are now enabled by default when the `DEBUG` flag is enabled.
  They can be disabled with an explicit `<COMPILATION-FLAG DEBUGGING-VERBS <>>`.

### Fixed

- Fixed binary syntax being parsed incorrectly when followed by whitespace.

- Fixed bug where failing to give something to an actor would turn them plural.

- Fixed bug where the variable in an `"ARGS"` binding couldn't be referenced
  later in the binding list.

- Fixed incorrect error message when calling a varargs function with too few
  arguments.

- Fixed bug where an otherwise unused routine would still be compiled if it was
  referenced within an unselected branch of a `VERSION?` or `IFFLAG` check.

## [1.2] (December 24, 2025)

### Added

- Glulx games can now use the `GLK` builtin function to call Glk directly. This
  is supported even for `--glulx16`, but 32-bit parameters may be tricky to use.
  You can determine whether Glk is available (i.e., if either `--glulx` or
  `--glulx16` are enabled) by checking the value of `,GLK` at compile time.

- Implemented the two-argument form of `FONT` (for V6).

### Changed

- Added a global variable `CURRENT-PLAYER` with `PLAYER` as its default value.
  This obviates the need to replace `ORDERING?` and `RESET-WINNER` in order to
  have multiple player characters. Note: `CURRENT-PLAYER` (the character being
  controlled by the human at the keyboard) is still distinct from `WINNER` (the
  character performing an action).

### Fixed

- Fixed NPCs being unable to respond to orders involving the player's
  possessions. Anything held by `PLAYER` is now in scope as long as `PLAYER` is.

- Fixed status line display for V6.

## [1.1] (December 16, 2025)

### Added

- Added the `--ide-info` switch to emit data for the VS Code extension (or
  similar) to use to guide and augment code analysis.

- Added an even more experimental feature to compile to Glulx without needing to
  modify the game source code, enabled with the `--glulx16` command-line switch.
  Unlike regular `--glulx` mode, this isn't treated as a separate "version" of
  the VM, so it won't run code inside `<VERSION? (GLULX ...)>` blocks, nor does
  it lift any of the Z-machine's limitations: the game will run as if it were
  compiled for Z-machine, using the existing `VERSION` directive if present.
  This is most likely to work with V3 games at the moment, and some very
  low-level Z-machine tricks may not work as expected.

- Added the search flag `EVERYWHERE`, which causes the parser to look for the
  object in every room, not just the current one. (However, only the current
  room's `LOCAL-GLOBALS` are included.) For example:
  `<SYNTAX FOLLOW OBJECT (EVERYWHERE) = V-FOLLOW>`.

- Added a way for players to enter an arbitrary series of words as part of a
  command: replace `OBJECT` in a syntax definition with `TOPIC`. For an example
  of how to use it, see the teleportation system in `advent.zil`, which uses
  topics to parse room names.

- Added a replaceable definition section, `PROVIDE-MISSING-VERB?`, which can be
  used to allow commands with no verb. If the routine (or macro)
  `PROVIDE-MISSING-VERB?` returns a vocab word, it will be used in place of the
  missing verb. If you replace the section, you also need to provide a
  `PRINT-MISSING-VERB` to print a suitable action name for parser messages like
  "What do you want to [missing verb] the [noun] with?"

### Changed

- The parser now takes the syntax flags (search flags, `HAVE`/`TAKE`, and
  `FIND`) into account when deciding which syntax line to use and which objects
  to consider for replacing a missing noun. For example, if both SHOW PAULINE
  THE GUN and SHOW THE GUN TO PAULINE are possible, the parser can know that
  SHOW PAULINE (with the other object omitted) should match the first syntax and
  SHOW GUN should match the second, because you need to be holding something to
  show it (`HAVE`), and you usually only show things to people
  (`FIND PERSONBIT`).

### Fixed

- Fixed a bug where a misleading source line was shown when a call to a
  generated DEFSTRUCT constructor caused it to throw an error.

## [1.0.1] (December 10, 2025)

### Fixed

- Fixed a bug where passing an output filename to ZILF resulted in the wrong
  arguments being passed to ZAPF or Glazer.

## [1.0] (December 9, 2025)

### Added

- `<SETG COMPACT-PREACTIONS? T>` now switches to a more efficient format for the
  preaction table, as proposed by Matthew Russotto.

- ZILF now invokes ZAPF automatically after compiling ZIL source code, unless
  `-S` (`--stop-after-compile`) is specified. Additional arguments can be
  passed to ZAPF with `--zapf-options`. If a second filename is passed to ZILF,
  it will be used to name the final output (story file), unless `-S` is
  specified or it ends with `.zap` or `.asm`.

- Error MDL0117 is issued when a binding in a routine header doesn't have
  exactly two elements.

- In V4+, the status line can now be customized with `USE-STATUS-LINE` and
  related macros. A few standard options are available: score/moves, 12-hour
  time, 24-hour time, and location only. See `zillib/status.zil` for details.

- ZAPF: Added .FORM and .OPERAND directives for use by Dezapf.

- Added `REMOVE-SYNTAX` to remove previously defined syntax rules based on a
  pattern. `<REMOVE-SYNTAX GET *>` removes all syntaxes for the verb GET,
  `<REMOVE-SYNTAX * = V-TAKE>` removes all syntaxes with the action routine
  V-TAKE, `<REMOVE-SYNTAX GET IN OBJECT>` removes that specific syntax, etc.

- Added `REMOVE-SYNONYM` to undo the effect of a previous `SYNONYM` definition.
  `<REMOVE-SYNONYM GRAB>` will undo both `<SYNONYM GRAB TAKE>` and
  `<SYNONYM TAKE GRAB>`.

- Added an experimental feature to compile to Glulx instead of the Z-Machine
  when enabled with the `--glulx` command-line switch. You'll need to use
  [Glazer](https://gitlab.com/andwj/glazer) to assemble the resulting `.asm`
  file into a runnable `.ulx` file.

  Since this is experimental, some Z-Machine features may be unimplemented, and
  some projects may encounter bugs and incompatibilities. This will **not** work
  with unmodified historical source like Zork I, for instance.

  The most important difference from an author's perspective is that Glulx deals
  with 32-bit values instead of 16-bit, so all numbers, addresses, and entries
  in word tables are 4 bytes long instead of 2. The library defines a
  `WORD-SIZE` constant to help with compatibility. You can check for Glulx with
  `<VERSION? (GLULX ...)>`, but generally you shouldn't need to.

  Other than word size, ZILF and `zillib` conspire to present Glulx as "a bigger
  Z-Machine", for the most part: most ZIL functions work the same on Glulx as on
  the Z-Machine, and most tables can be accessed using the same code (as long as
  you use `WORD-SIZE` when relevant). Some of the Z-Machine's limits are raised
  in Glulx mode, such as the number of flags and properties. On the other hand,
  many features of Glulx and Glk are not exposed to the game.

- ZILF now properly handles strings with Unicode characters other than those in
  the default ZSCII set. In V5+, it creates a Unicode translation table (using
  the new ZAPF directive `.UNICHR`). In V3-4, Unicode translation tables aren't
  available, so it issues error ZIL0191. In Glulx, Unicode is supported natively
  without any special effort.

### Changed

- Overhauled the command-line syntax. See `zapf --help` and `zilf --help` for
  details. Among other things, you can now type `zilf build` to rebuild your
  project if the main .zil file has the same name as the directory.

- Split the unhelpfully generic errors MDL0113 and MDL0122 into more specific
  errors: MDL0131, MDL0228, MDL0229, MDL0230, MDL0231, MDL0232, MDL0319,
  MDL0320, MDL0322, MDL0431, MDL0432, MDL0433, MDL0434, MDL0435, MDL0436,
  MDL0437, MDL0438, MDL0509, MDL0608, MDL0609, and MDL0610.

- An orphaning response that names multiple objects is now accepted even
  when it doesn't use `ALL` or `BOTH`. In other words, the player can
  respond to "Which do you mean, the red cube or the blue cube?" with
  "red and blue" and it will work as expected.

### Fixed

- Fixed `WEAR` not using the library message system.

- When using a SEGMENT to splice a list at the end of a new list (as in
  `(A B !.L)`), the spliced list is now linked in directly instead of copied.

- Fixed an internal type name appearing in some error messages for PUTREST.

- Fixed an unhelpful error message when passing the wrong type of argument to
  `SUBSTRUC`.

- Fixed PRINT-MATCHING-WORD in zillib (used by some of the debugging verbs).

- When the assembly output files have an extension other than `.zap`, the
  extension is now properly included in the generated `.INSERT` directives.

- Fixed a bug where ZILF would crash when trying to make a word its own synonym.

## [0.11.1] (October 31, 2025)

### Fixed

- Fixed a bug where all string literals were being registered as global strings,
  leading to assembler errors and increased story file size.

## [0.11] (October 30, 2025)

### Added

- Error ZIL0125 is issued when a bare atom that isn't an existing TELL token
  or a property constant is used in TELL.

- Added a TAKE FROM verb, which is like TAKE but verifies that the object is
  in/on the container first.

- The parser can now be told to find PRSI before PRSO for certain verbs, via the
  macro `MATCH-PRSI-FIRST?`. This is done for TAKE FROM, since `ALL-INCLUDES?`
  needs to check whether a potential match is held by PRSI in order to make
  TAKE ALL FROM X work correctly.

- ZAPF now issues a warning when a `.BYTE` directive has a value outside the
  range of a signed or unsigned byte.

- Warning ZIL0213 is issued when a routine is never referenced outside of its
  own definition, unless `<FILE-FLAGS UNUSED-ROUTINES?>` is enabled. Such
  routines will also be excluded from the compilation unless
  `<FILE-FLAGS KEEP-ROUTINES?>` is enabled. `<ROUTINE-FLAGS KEEP?>` and
  `<ROUTINE-FLAGS UNUSED?>` can also be used to flag only the next routine
  defined.

- Error MDL0214, which is issued when a global entity is defined more than once,
  now includes info MDL0227 pointing to the previous definition, when available.

### Changed

- Source file paths in debug information and diagnostics are now absolute paths.

- Changed ADVENT-PLAYER-F to be a slightly better role model. A few things
  that were handled by the player's ACTION routine are now handled by
  overriding verb routines or default messages, or moved into a room's
  ACTION routine.

- `CONS` now requires its second argument to be a LIST.

- `SET`/`SETG` now allow a complex expression as the first argument (to compute
  the variable number) even in value context.

- Changed `ALL-INCLUDES?` to exclude held objects from ALL when the verb is TAKE
  and unheld objects from ALL when the verb is DROP.

- `BOTH` can now be used like `ALL` to match multiple objects.

- Optimized code generation for `BAND`/`BOR` when operating on named constants.

- ZAPF no longer restarts the pass when it reaches a `.NEW` directive.

- Optimized code generation for conditional branches based the result of SETting
  a variable to a constant which is known to be nonzero because of its type.

### Fixed

- Fixed OBJECT-TEMPLATE breaking when defining more than one template at a time
  (which the documentation in templates.zil suggests ought to work).

- Restored separate default messages for CLIMB X vs. CLIMB.

- Fixed `zapf -la` listing some labels twice.

- Fixed unhelpful error messages when passing the wrong type of argument to
  STRING or arithmetic functions.

## [0.10] (October 14, 2025)

### Added

- Added warning MDL0428 for `LEXV` tables initialized with the wrong number
  of elements.

- Added limited support for "reader macros": `MAKE-PREFIX-MACRO` from the
  `READER-MACROS` package can be used to define new prefix syntaxes.

- Added the `QQ` package for quasiquoting. This is similar to regular
  quoting with `QUOTE` (the `'` prefix), but `QUASIQUOTE` uses the `` ` `` prefix
  and allows inserting evaluated expressions with `TILDE` (the `~` prefix),
  making macro implementations more readable. For example,
  `<FORM + '.A '.B .FOO !.BAR>` can be written as `` `<+ .A .B ~.FOO ~!.BAR> ``.

- The frequent words file ZILF generates now has actual abbreviations rather
  than placeholders. This will result in smaller story files without having
  to run `zapf -ab` to generate abbreviations separately. If the text of the
  game changes significantly, the frequent words file should be regenerated
  (e.g. by deleting the existing file).

- Added warnings ZIL0211 and ZIL0212 for unused object flags and unused
  properties, respectively.

- Added warning ZIL0310 (suppressed by default) when vocab words are merged
  because of the vocab resolution limit. Info ZIL0311 is added when the merge
  can be avoided by targeting a different Z-machine version.

- Added warning ZIL0429 for suspiciously quoted atoms in `SYNONYM` and
  `ADJECTIVE` lists. For example, `PIRATE'S` should probably be `PIRATE\'S`.

- Added warning ZIL0430 for tables defined with a length prefix that's
  too narrow to actually hold the table's length.

- ZAPF now reports an error when the values of `START` or some other header
  fields are too large for a 16-bit word.

- The library now implements `VEHBIT` and vehicles, as described in
  _Learning ZIL_.

- Library messages have been centralized in `libmsg-defaults.zil`, and they
  can be replaced with `REPLACE-LIBRARY-MESSAGES` without having to edit
  the library.

- Added `dragon` and `mandelbrot` samples.

### Changed

- Upgraded to .NET 9.

- Changed the primitive representation of `OBLIST` to be compatible with MDL.

- Two empty `FORM`s are now considered identical (`==?`), as in MDL.

- `MIN` and `MAX` now allow zero arguments, as in MDL.

- `ASSOCIATIONS` and `NEXT` now enumerate associations in the same order as MDL
  (the reverse of the order in which the item/indicator pairs were added).

- Error ZIL0400 is now issued (sometimes with info ZIL0403) for arguments
  defined on the `GO` routine in Z-machine versions other than V6.

- Wearable objects can now be held without wearing them, and taken off without
  dropping them.

- Improved the algorithms for finding and applying abbreviations, using an
  approach described by Matthew Russotto.

- On case-sensitive file systems, when a file included with `INSERT-FILE` or
  `USE` isn't found, ZILF will now try lowercasing the filename and/or
  uppercasing the extension before giving up.

- `COND` clauses in Z-code routines must now be either `<>` or non-empty
  `LIST`s (or macro invocations expanding to such); error ZIL0100 is
  issued otherwise.

- Subdiagnostics (i.e., info messages attached to warnings or errors) are now
  indented in the output.

- The internal mechanisms for parsing the arguments to built-in functions
  have been redesigned, which should be mostly invisible to users, but may
  have inadvertently introduced bugs by changing error messages in rare cases.

- Error ZIL0404 (sometimes with info ZIL0403) is now issued when too many
  properties are defined.

### Fixed

- Fixed a couple bugs related to using `OBLIST`s as structured values.

- Fixed some header fields being left blank in V7 builds.

- Fixed compiler hanging when `WORD-FLAGS-LIST` contains duplicate entries.

- Fixed the AGAIN command not working after `KLUDGEBIT` actions.

- Fixed inconsistent ordering of lists in some error messages across platforms.

- Fixed newlines being retained in the `DESC` pseudo-property.

- Improved handling of some syntax errors.

- Fixed the `FORM` inside a `SEGMENT` missing source line information.

- Fixed debug files missing source line information for local variable
  initializers.

- Fixed `=` showing up as "Eq" when printing a `PROPDEF`.

- Fixed return value when an assignment to a soft global is used in value
  context.

- Fixed issue where two words that were both used as prepositions couldn't
  be made synonyms of each other.

- Fixed `PROPDEF` `DIRECTIONS` incorrectly defining a property called
  `DIRECTIONS`.

- Macros that return `SPLICE` to expand into multiple values now work in
  calls to Z-code builtins and routines.

- Fixed an unhandled exception when the arguments to `TABLE` inside a routine
  include a macro call.

- Fixed `DESCFCN` not being called with `M-OBJDESC?` in some cases.

- Fixed `PUT IN` and `PUT ON` incorrectly checking the object's
  `SIZE` against the container's `SIZE` (in addition to its `CAPACITY`).

- Fixed unhandled exception from `(NORTH TO X IF Y)` when global Y isn't
  defined.

- Fixed unhandled exception when calling a routine with too many
  arguments for the targeted Z-machine version.

- Fixed off-by-one error when generating placeholders for frequent words files.

## [0.9] (August 11, 2019)

### Added

- Optimized `REST`/`ZREST` to compile as a ZAP constant instead of an
  `ADD` instruction when possible.

- Optimized `VALUE` to compile as a variable reference instead of a `VALUE`
  instruction when possible.

- Improved code generation when values are returned from a `PROG` (or
  similar).

- Added a few new ZIL libraries in `zillib/experimental`, which is now part
  of the default include path.

- `RELEASEID` now defaults to 0 in all Z-machine versions.

- ZILF now stops after 100 compiler errors.

- Added `<FILE-FLAGS SENTENCE-ENDS?>` to use sentence spaces when compiling
  to V6.

- Added info message ZIL0506 to suggest the cause of an "undeclared
  compilation flag" warning.

- `<GLOBAL FOO BAR>` is now accepted when `BAR` is a global variable. This
  initializes `FOO` to the variable index of BAR and issues a warning.

- Macros invoked inside routine definitions can now define global variables
  by calling `GLOBAL`, which will be available in every routine.

- Added a way to control the behavior of `RETURN` without an activation
  argument inside a `PROG` (or similar) in a routine.

- ZAPF now allows `%`, `!`, `'`, and `/` in symbol names.

- Implemented the `.ALIGN` directive in ZAPF, and ignored a few unsupported
  directives.

- The `XCALL` and `IXCALL` opcodes can now be written as `CALL` and `ICALL`
  instead.

- ZAPF now defines the `FLAGS` and `RELEASEID` constants automatically if
  needed.

- Added replaceable library sections `DESCRIBE-OBJECTS` and `STATUS-LINE`.

- The game name now prints in bold on V4+.

- Added the function `DESC-BUILTINS!-YOMIN`, which provides signature info
  to the Visual Studio Code plugin.

- Implemented the `.DEBUG-MAP` ZAP directive.

- Added error MDL0427 when defining `"TUPLE"`, `"ARGS"`, or `"BIND"`
  arguments for a routine.

- Added error ZIL0209 when a routine name is used instead of an object name
  in one of the contexts where objects can be defined implicitly, i.e. the
  `IN`/`LOC` pseudo-property and the `GLOBAL` property.

- Improved error ZIL0123 to appear for misplaced end blocks in loops as well
  as in `COND`, and to appear even in void context.

- Added `-we` command line option to make ZILF treat warnings as errors,
  `-ws` to suppress specific warnings, and `-w` to enable all warnings. Some
  warnings are now suppressed by default.

- Added the `<SUPPRESS-WARNINGS?>` directive, to control warning suppression
  from source code.

- Added tab completion and command history to ZILF's interactive mode.

- Added warning ZIL0410 for unprintable characters in strings.

- Added warning ZIL0210 for unused local variables.

- Added replaceable sections for the parts of `MAIN-LOOP`.

- ZILF will now detect and use a preexisting frequent words file named
  without an underscore, such as `foofreq.zap` instead of `foo_freq.zap`.

- Implemented substring search for `MEMBER`.

### Changed

- Ported the projects to .NET Core 2.2, and changed the build scripts to
  make self-contained applications. Installing Mono is no longer necessary
  to run on Linux and Mac OS.

- Replaced the Antlr grammars for ZIL and ZAP source code with a hand-coded
  parser. The Antlr DLLs are no longer needed, and syntax error messages
  are improved.

- Changed the mechanism for non-local control flow, used by functions like
  `AGAIN` and `MAPRET`, to not rely on exceptions. Running ZILF inside a
  debugger is now much faster.

- Setting the release number on the ZAPF command line with `-r` now takes
  precedence over `RELEASEID`.

- Relaxed error MDL0502 ("duplicate default for section") to a warning.

- Changed the way a few functions operate on the values represented by
  expressions like `.L` and `,G`. These were syntactic sugar for forms
  invoking `LVAL` or `GVAL` in MDL; in MIM, however, `LVAL` and `GVAL` were
  separate types based on `ATOM`. In ZILF, they're now treated as a hybrid:
  they're still syntactic sugar for forms, but such forms now also get
  special treatment from `==?`, `TYPE?`, `CHTYPE`, and `DECL`s, so they can
  be treated as atom-like values, similar to MIM.

- Moved the sample games into their own subdirectories.

- Converted the Cloak Plus sample's notes to Markdown and updated them.

- Renamed the `library` directory to `zillib`.

- Changed the error ZAPF prints when the story file is too big to avoid
  implying that the wrong Z-machine version is being targeted. The message
  now also suggests using abbreviations if appropriate.

### Fixed

- Improved error handling and reporting in many ways, especially syntax
  errors and line numbering.

- Corrected the "Inform mode" name for `EQUAL?` to `je` (not `jeq`).

- The routine argument of `SOUND` is no longer ignored.

- `TABLE`s behave correctly when `CHTYPE`d.

- `ZREST`, `ZGET`, `ZPUT`, etc. handle invalid offsets correctly.

- `REPEAT` no longer leaks values onto the stack.

- `PROPDEF`s can be `CHTYPE`d.

- Implemented the standard "structured value" functions for all built-in
  types with structured primtypes. This affected `ASOC`, `CONSTANT`,
  `GLOBAL`, `OBJECT`, `PROPDEF`, `ROUTINE`, and `WORD`.

- `PUTREST` works with all list-based types, not only `LIST`.

- Fixed inconsistencies in the various ways ZIL atom names are translated
  into ZAP symbols, which occasionally led to assembly errors for atoms
  containing exotic characters.

- `INSERT-FILE` now respects the file extension if one is provided.

- Vectors resized with `GROW` now work correctly when being printed, mapped,
  and `REST`ed.

- Fixed an optimizer bug where unreachable code that became reachable after
  an optimization would still be deleted.

- Fixed duplicate debug info being generated for reused action routines.

- Fixed overflow when defining more than 255 actions.

- Fixed ZAP symbol collision when local and global variables have the same
  name. Variable shadowing isn't a concern in ZIL, thanks to `SET`/`SETG` and
  `LVAL`/`GVAL`, but it is in ZAP, so ZILF now renames the local variable.

- Fixed a library bug where implicitly taking an object inside a container
  was reported as a failure even if it succeeded.

- Cleaned up some unused local variables, bad formatting, and ugly code in
  the library.

- Calling a `FUNCTION` with too many arguments now raises an error.

- Fixed some cases where debug line addresses would be corrupted,
  duplicated, or lost.

- Fixed the library response for undoing too many turns.

- Fixed `MAP-DIRECTIONS` ignoring the end block if one was provided.

- Fixed `GOTO` moving the wrong player when called during the response to an
  order, and returning an unpredictable value.

- `RESTART` is now correctly considered a `GAME-VERB?`.

- Corrected a fundamental misconception about the relationship between
  the empty form `<>` and `#FALSE ()`. `<>` is now read as an empty form,
  which may also be created with `<FORM>`, and it will be converted to
  `#FALSE ()` when it's evaluated. (Yes, ZILF got this wrong for ten years.)

- Fixed parsing of the syntax `WALK IN OBJECT`, which was being mistaken for
  directional movement.

### Removed

- "Inform mode" in ZAPF no longer changes the overall assembly syntax. It
  still changes the opcode and special operand names.

- The ZilFormat project, which never got very far, is now gone. The new
  experimental `pprint` package may be used instead.

- Removed the `-v` command line option from ZAPF. ZAP code is specific to a
  Z-machine version, and retargeting assembly code from one version to
  another is a mistake.

[0.9]: https://foss.heptapod.net/zilf/zilf/-/compare/0.8...0.9
[0.10]: https://foss.heptapod.net/zilf/zilf/-/compare/0.9...0.10
[0.11]: https://foss.heptapod.net/zilf/zilf/-/compare/0.10...0.11
[0.11.1]: https://foss.heptapod.net/zilf/zilf/-/compare/0.11...0.11.1
[1.0]: https://foss.heptapod.net/zilf/zilf/-/compare/0.11.1...1.0
[1.0.1]: https://foss.heptapod.net/zilf/zilf/-/compare/1.0...1.0.1
[1.1]: https://foss.heptapod.net/zilf/zilf/-/compare/1.0.1...1.1
[1.2]: https://foss.heptapod.net/zilf/zilf/-/compare/1.1...1.2
[1.3]: https://foss.heptapod.net/zilf/zilf/-/compare/1.2...1.3
[1.4]: https://foss.heptapod.net/zilf/zilf/-/compare/1.3...1.4
[1.5]: https://foss.heptapod.net/zilf/zilf/-/compare/1.4...1.5
[1.6]: https://foss.heptapod.net/zilf/zilf/-/compare/1.5...1.6
[1.6.1]: https://foss.heptapod.net/zilf/zilf/-/compare/1.6...1.6.1
[1.7]: https://foss.heptapod.net/zilf/zilf/-/compare/1.6.1...1.7
[1.8]: https://foss.heptapod.net/zilf/zilf/-/compare/1.7...1.8
