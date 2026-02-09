---
name: zil-coding
description: Write working, efficient ZIL code. Use this skill when the user asks to work on a game written in ZIL (or using ZILF), or when writing ZIL or MDL test cases. Avoids common pitfalls.
---
This skill guides writing ZIL code.

# ZIL language overview

ZIL is a domain-specific extension of MDL. ZILF is an interpreter for a large subset of MDL, with some new constructs built in, plus a compiler for an embedded language similar to, but distinct from, MDL. It isn't a full MDL implementation; it only supports the constructs needed for ZIL. ZIL constructs like `ROUTINE` and `OBJECT` build structures in the interpreter's context that are then used during compilation to generate Z-machine code and data structures.

MDL is not LISP, though it has some LISP-like syntax. It's a distinct language with its own semantics and constructs. MDL has various data types besides lists, and notably, it distinguishes between lists and forms. Lists, written with parens, are merely data structures; forms, with angle brackets, are code expressions that can be executed. Thus, evaluating `(+ 1 2)` will simply return the same list, but evaluating `<+ 1 2>` will perform the addition and return 3.

It's VERY important to keep track of bracket nesting. Unbalanced brackets cause syntax errors that take a long time to fix.

The embedded language implemented by the compiler (available inside `ROUTINE`), is similar to but distinct from the one implemented by the interpreter (available outside `ROUTINE`). 

The interpreted language is dynamically typed. The embedded language is untyped, and all values exist at runtime as 16-bit words; the compiler does some static typing to facilitate optimizations, but the Z-machine itself doesn't enforce types.

## Specific advice

### DO loops

DO loops are part of the embedded language. Be careful when writing them. The syntax is:
```
<DO (variable init condition [step])
    [(else-body)]
    body>
```
where `step` and `else-body` are optional.
- `else-body`, if present, is executed after the end condition is reached,
  unless the loop is exited early with `RETURN`.
- `variable` is rebound in the loop and doesn't need to be listed in the
  routine's `"AUX"` section.
- `condition` and `step` can either be constants, LVAL/GVALs, or complex
  forms (i.e. not LVAL/GVAL).
- If `condition` is a complex form, it should be a predicate for the loop end
  condition (e.g. `<G? .I .MAX>`).
- If `step` is a complex form, it should modify the variable (e.g.
  `<SET I <+ .I .STRIDE>>`).
- A `RETURN` inside the loop exits the loop (setting the return value of `DO`),
  it doesn't exit the function. `RTRUE` and `RFALSE` exit the function.
- The return value of `DO` itself is only meaningful when `DO` is exited with
  `RETURN`.

When in doubt, you can write an explicit `REPEAT` loop instead.

### Constant tables

When defining a table whose contents won't change at runtime, make it a "pure"
(ROM) table with `PTABLE`:
```
<CONSTANT BITS <PTABLE 1 2 4 8 16>>
```

### Quasiquoting

Quasiquoting is provided by the `QQ` package, which is automatically imported
if the project includes zillib (`<INSERT-FILE "parser">`), or can be
explicitly imported (`<USE "QQ">`) otherwise. Quasiquote uses the backtick and
tilde prefixes, like this:
```
<DEFMAC SAVE-IT ('A)
    `<SETG MY-GLOBAL ~.A>>

<DEFMAC MY-TELL ("ARGS" AA)
    `<TELL ~!.AA>>
```

### Comment syntax

ZIL/MDL does NOT have line comments or block comments like most languages.
It has *prefix* comments: `;` is a prefix that comments out the next value.
Comments are typically prefixed strings: `;"a comment"`. Quotation marks inside
the string must be escaped: `;"a \"fun\" comment"`.

### Equality comparison

In the embedded language, `=?`/`==?`/`EQUAL?` are all equivalent (likewise
`N=?`/`N==?`), and they can take several arguments (implicit OR):
`<==? .A .THIS .THAT .THE-OTHER>`.

In the interpreted language, `=?` is value equality and `==?` is reference
equality, and they can only take 2 arguments.

### Function/routine length

Avoiding overly long functions is extra important in ZIL/MDL, because long
functions make it hard to follow the bracket nesting. Create helper functions
when appropriate to make the code more readable and less deeply nested.

### Read-time evaluation

A `%` prefix means the value is evaluated at read time:
`<OBJECT FOO (NUMBER %<+ ,A-CONSTANT ,ANOTHER-CONSTANT>)>`.

Sometimes it's used with `VOC` inside a routine, to define a vocab word that
needs to be matched against player input but doesn't appear in any `OBJECT` or
`SYNTAX`:
```
<ROUTINE MAGIC-WORD? (W)
    <==? .W %<VOC "XYZZY">>>
```

### MAP-CONTENTS

If you need to do something for all the children of an object, use
`MAP-CONTENTS`, which compiles to an idiomatic loop using `FIRST?` and `NEXT?`:
```
<MAP-CONTENTS (O ,PARENT)
    <DO-SOMETHING-WITH .O>>
```

### AGAIN

Unlike `continue` in C, `AGAIN` inside a loop jumps to the top of the loop
body rather than the bottom. This means it won't advance the loop counter. Using
`AGAIN` inside a loop is a common source of hangs. This goes for `MAP-CONTENTS`
as well as `DO`.
