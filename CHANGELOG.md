# Change Log

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project may someday adhere to
~~[Semantic Versioning](http://semver.org/spec/v2.0.0.html)~~.

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
  argument inside a `PROG` (or similar) in a routine. Previously, 

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

[0.9]: https://bitbucket.org/jmcgrew/zilf/branches/compare/0.9..0.8
