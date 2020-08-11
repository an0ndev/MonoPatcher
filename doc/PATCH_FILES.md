# Patch file format
## Basic format
Each line indicates a separate patch instruction. Prior to all processing, whitespace is removed from the front and end of each line, so the selection of an indentation style is left up to the patcher.

Instructions are split on spaces, with the first entry in the split instruction being the type of instruction and all other entries serving as arguments.

## Exceptions to the format
Exceptions to this format are as follows:
- The empty string "": is ignored (intended for use as spacing).
- Any text starting with "#": is ignored (intended for use as a comment); code placed before a # on a line is still processed.
- Any text surrounded with "/\*" "\*/": is ignored (intended for use as block comments).
(WARNING: All text on the lines containing "/\*" or "\*/" is completely ignored; this includes text placed before "/\*" and after "\*/".)

## How contexts work
Some instructions must be associated with a specific type and instance of an object to operate on, which is known as the instruction's target. To determine the target of an individual command, a context, a list of objects stored in memory, shows the path taken to traverse the tree of objects in an assembly.

For example, a context that is completely full; that is, one that refers to the highest-level target in the tree (an Instruction object), will contain the following objects in a list:
- the AssemblyDefinition (the object used to modify the loaded assembly definition)
- a ModuleDefinition (an object used to modify a specific module) referring to a module on the AssemblyDefinition object
- a TypeDefinition (an object used to modify a specific class) referring to a type on the ModuleDefinition object
- a MethodBody (an object used to modify a specific method) referring to a method on the TypeDefinition object
- an ILProcessor (an object specifically used to modify the instructions in the method) referring to a MethodBody object
- an Instruction (an object used to modify a specific instruction in the method) referring to an instruction on the ILProcessor object

A context that is completely empty; that is, one that refers to the lowest-level target in the tree (an AssemblyDefinition), will contain only the AssemblyDefinition object in a list.

Usage of the commands and keyword used to modify the current context, and by extension the target of instructions, are known collectively as tree traversal.

## Tree traversal
To determine the context present when your instructions run, use one of two methods, traversal by instruction or traversal by keyword:
### Traversal by instruction
Traversal by instruction consists of the usage of two special instruction types, `CONTEXT_PUSH` and `CONTEXT_POP`. `CONTEXT_PUSH` simply pushes the object with the given name to the context so any future instructions will operate using the new context, until `CONTEXT_POP` is specified. To improve code clarity, it's recommended to indent any instructions between the `CONTEXT_PUSH` and `CONTEXT_POP` instructions. For example, to perform the operation `op` on a context of `1`, `2`, `3` with a current context of [`1`]
### The `in` keyword
To increase the level of the context of a single instruction, simply add the `in` keyword followed by the given name to push the name to the context. The name is automatically removed from  the context after the instruction completes. (This makes it more efficient to perform single operations on contexts since only one instruction can be used.) `in` keywords can be chained in reverse order to add multiple names to the context. For example, to perform the operation `op` on ``
### A note on the names used in tree traversal
Since each tier of objects in the tree has its own type, the type of the given name in tree traversal operations is determined by the type of the next tier of objects. Although the implementation of this check is a bit more complicated, if desired, this can determined by hand by using the length of the current context as an index into contextItemOrder, a static variable present on the PatchApplicator object that contains an array of types.

## Types of instructions
Types of instructions are non-case-sensitive. Specification of an invalid type will trigger an exception.
### Generic
These work no matter what object is selected/specified.
#### show
Prints some information about the target.
#### assert
Ensures the value of some aspect of the target. (TODO)
#### echo
All arguments, joined to form a string using a space, are printed to the patcher console.
#### print_context
Prints the length and contents of the context.
