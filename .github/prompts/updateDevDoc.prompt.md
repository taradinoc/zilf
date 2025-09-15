---
mode: agent
description: "Update the devdoc files and Copilot instructions to reflect recent changes in the project."
model: GPT-5 mini (copilot)
tools: ['createFile', 'createDirectory', 'editFiles', 'search', 'todos', 'usages', 'changes']
---
Look over the files in `devdoc`, as well as .github/copilot-instructions.md and, if needed, update them to reflect the current state of the project.

Add new files in #file:../../devdoc as needed, such as:
  * If new features have been added that are complex enough to require their own documentation
  * If existing features have become complex enough to require their own documentation
  * If there were gaps in your understanding of the project that you had to fill in order to make your changes

Make sure to include any new coding conventions or practices that have been adopted recently.
Make sure to remove any references to practices or tools that are no longer used.
Be concise but thorough in your updates.
