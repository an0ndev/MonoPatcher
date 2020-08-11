using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace LibMonoPatcher {
    public class PatchApplicator {
        AssemblyDefinition assembly;
        bool ready = false;
        PatchContext context;
        public void loadAssembly (string assemblyPath) {
            if (ready) {
                throw new System.InvalidOperationException ("Assembly already loaded!");
            }
            // Load additional assemblies from the path the target assembly is stored in
            string assemblyFolder = new System.IO.FileInfo (assemblyPath).Directory.FullName;
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver ();
            assemblyResolver.AddSearchDirectory (assemblyFolder);
            ReaderParameters readerParameters = new ReaderParameters ();
            readerParameters.AssemblyResolver = assemblyResolver;

            assembly = AssemblyDefinition.ReadAssembly (assemblyPath, readerParameters);
            context = new PatchContext (assembly);

            ready = true;
        }
        public void saveAssembly (string assemblyPath) {
            assembly.Write (assemblyPath);
        }
        public void applyPatchFile (PatchFile patchFile) {
            if (!ready) {
                throw new System.InvalidOperationException ("Cannot apply patch without an assembly -- call loadAssembly first!");
            }
            for (int instructionNumber = 1; instructionNumber <= patchFile.instructions.Length; instructionNumber++) {
                PatchInstruction instruction = patchFile.instructions [instructionNumber - 1];
                try {
                    applyPatchInstruction (instruction);
                } catch (Exception patchApplicationException) {
                    Console.WriteLine (String.Format ("Encountered exception when applying instruction {0} in patch file at {1}", instructionNumber, patchFile.path));
                    throw patchApplicationException;
                }
            }
        }
        public void applyPatchInstruction (PatchInstruction patchInstruction) {
            System.Tuple<int, string[]> parseResult = context.parseInKeyword (patchInstruction.args);
            int contextAdditionCount = parseResult.Item1;
            patchInstruction.args = parseResult.Item2;
            try {
                switch (patchInstruction.type) {
                    case PatchInstructionType.SHOW: {
                        ContextItem topItem = context.items [context.items.Count - 1];
                        System.Func<System.Type, bool> check = type => topItem.type == type;
                        if (check (typeof (AssemblyDefinition))) {
                            AssemblyDefinition assemblyDefinition = (AssemblyDefinition) topItem.object_;
                            // Printing information about AssemblyDefinition
                            Console.WriteLine (String.Format ("Full name: {0}", assemblyDefinition.FullName));
                            Console.WriteLine (String.Format ("Main module: {0}", assemblyDefinition.MainModule.FileName));
                            foreach (ModuleDefinition childModuleDefinition in assemblyDefinition.Modules) {
                                Console.WriteLine (String.Format ("Module: {0}", childModuleDefinition.FileName));
                            }
                            break;
                        } else if (check (typeof (ModuleDefinition))) {
                            ModuleDefinition moduleDefinition = (ModuleDefinition) topItem.object_;
                            Console.WriteLine (String.Format ("File name: {0}", moduleDefinition.FileName));
                            foreach (TypeDefinition childTypeDefinition in moduleDefinition.Types) {
                                Console.WriteLine (String.Format ("Type: {0}", childTypeDefinition.Name));
                            }
                        } else if (check (typeof (TypeDefinition))) {
                            TypeDefinition typeDefinition = (TypeDefinition) topItem.object_;
                            Console.WriteLine (String.Format ("Name: {0}", typeDefinition.Name));
                            if (!typeDefinition.HasMethods) {
                                Console.WriteLine ("(No methods)");
                            } else {
                                foreach (MethodDefinition childMethodDefinition in typeDefinition.Methods) {
                                    Console.WriteLine (String.Format ("Method: {0}", childMethodDefinition.Name));
                                }
                            }
                            if (!typeDefinition.HasFields) {
                                Console.WriteLine ("(No fields)");
                            } else {
                                foreach (FieldDefinition childFieldDefinition in typeDefinition.Fields) {
                                    Console.WriteLine (String.Format ("Field: type {0}, name {1}", childFieldDefinition.FieldType.FullName, childFieldDefinition.Name));
                                }
                            }
                        } else if (check (typeof (MethodDefinition))) {
                            MethodDefinition methodDefinition = (MethodDefinition) topItem.object_;
                            Console.WriteLine (String.Format ("Name: {0}", methodDefinition.Name));
                            if (!methodDefinition.HasBody) {
                                Console.WriteLine ("(No body)");
                            } else {
                                foreach (Instruction childInstruction in methodDefinition.Body.Instructions) {
                                    System.Text.StringBuilder instructionTextBuilder = new System.Text.StringBuilder ();
                                    instructionTextBuilder.Append ("@" + childInstruction.Offset.ToString ("x4") + " ");
                                    instructionTextBuilder.Append ("." + childInstruction.OpCode.Name + " ");
                                    if (childInstruction.Operand != null) {
                                        instructionTextBuilder.Append ("w/[" + "(" + childInstruction.OpCode.OperandType.ToString () + ")" + " ");
                                        string operandString;
                                        switch (childInstruction.OpCode.OperandType) {
                                            case OperandType.InlineBrTarget:
                                            case OperandType.ShortInlineBrTarget:
                                                operandString = "@" + ((Instruction) childInstruction.Operand).Offset.ToString ("x4");
                                                break;
                                            default:
                                                operandString = childInstruction.Operand.ToString ();
                                                break;
                                        }
                                        instructionTextBuilder.Append (operandString);
                                        instructionTextBuilder.Append ("]");
                                    }
                                    // Console.WriteLine (string.Format ("Instruction: {0}", childInstruction.ToString ()));
                                    Console.WriteLine (string.Format ("Instruction: {0}", instructionTextBuilder.ToString ()));
                                }
                            }
                        } else if (check (typeof (Instruction))) {
                            Instruction instruction = (Instruction) topItem.object_;
                            Console.WriteLine ("Address: @{0}", instruction.Offset.ToString ("x4"));
                            Console.WriteLine ("Instruction: .{0}", instruction.OpCode.Name);
                            Console.WriteLine ("Operand type: ({0})", instruction.OpCode.OperandType.ToString ());
                            if (instruction.Operand != null) {
                                Console.WriteLine ("Operand value: {0}", instruction.Operand.ToString ());
                            }
                        } else {
                            // Unknown item on top of context
                            throw new System.InvalidOperationException ("Unknown item on top of context");
                        }
                        break;
                    }
                    case PatchInstructionType.REPLACE: {
                        ContextItem topItem = context.items [context.items.Count - 1];
                        if (topItem.type != typeof (Instruction)) {
                            throw new System.InvalidOperationException ("Object on top of context is not an instruction");
                        }
                        Instruction instruction = (Instruction) topItem.object_;
                        string opcodeString = patchInstruction.args [0];
                        if (!opcodeString.StartsWith (".")) {
                            throw new System.ArgumentException (string.Format ("Invalid opcode string {0}", opcodeString), "patchInstruction.args [0]");
                        }
                        opcodeString = opcodeString.Substring (1); // remove . from front
                        opcodeString = opcodeString.ToLower (); // Make all lower case
                        opcodeString = opcodeString.Replace (".", "_"); // replace . with _
                        string[] opcodeChunks = opcodeString.Split ("_"); // split into chunks so we can capitalize each letter
                        opcodeString = "";
                        for (int opcodeChunkIndex = 0; opcodeChunkIndex < opcodeChunks.Length; opcodeChunkIndex++) {
                            string opcodeChunk = opcodeChunks [opcodeChunkIndex];
                            opcodeString += opcodeChunk [0].ToString ().ToUpper () + opcodeChunk.Substring (1);
                            if (opcodeChunkIndex < (opcodeChunks.Length - 1)) {
                                opcodeString += "_";
                            }
                        }
                        System.Reflection.FieldInfo opcodeFieldInfo = typeof (OpCodes).GetField (opcodeString);
                        if (opcodeFieldInfo == null) {
                            throw new System.ArgumentException (string.Format ("Invalid opcode string {0}", opcodeString), "patchInstruction.args [0]");
                        }
                        OpCode opcode = (OpCode) opcodeFieldInfo.GetValue (null);
                        Instruction replacementInstruction;
                        switch (opcode.OperandType) {
                            case OperandType.InlineNone: // null
                                if (patchInstruction.args.Length > 1) {
                                    System.Console.WriteLine (string.Join (" ", patchInstruction.args));
                                    throw new System.ArgumentException ("A value was given, but a value is not needed for the given instruction type!", "patchInstruction.args");
                                }
                                break;
                            case OperandType.ShortInlineR: // float32
                            case OperandType.ShortInlineI: // int8
                                if (patchInstruction.args.Length < 2) {
                                    throw new System.ArgumentException ("A value is needed for the given instruction type!", "patchInstruction.args");
                                }
                                break;
                            default:
                                throw new System.NotImplementedException (string.Format ("No support for {0}", opcode.OperandType.ToString ()));
                        }
                        switch (opcode.OperandType) {
                            case OperandType.InlineNone:
                                replacementInstruction = Instruction.Create (opcode);
                                break;
                            case OperandType.ShortInlineR: // float32
                                replacementInstruction = Instruction.Create (opcode, System.Convert.ToSingle (patchInstruction.args [1]));
                                break;
                            case OperandType.ShortInlineI: // int8
                                replacementInstruction = Instruction.Create (opcode, System.Convert.ToSByte (patchInstruction.args [1]));
                                break;
                            default:
                                throw new System.NotImplementedException (string.Format ("No support for {0}", opcode.OperandType.ToString ()));
                        }
                        ILProcessor ilProcessor = ((MethodDefinition) context.items [context.items.Count - 2].object_).Body.GetILProcessor ();
                        ilProcessor.Replace (instruction, replacementInstruction);
                        context.items [context.items.Count - 1] = new ContextItem (typeof (Instruction), (System.Object) replacementInstruction);
                        break;
                    }
                    /*
                    case PatchInstructionType.SET_OPERAND_VALUE: {
                        ContextItem topItem = context.items [context.items.Count - 1];
                        if (topItem.type != typeof (Instruction)) {
                            throw new System.InvalidOperationException ("Object on top of context is not an instruction");
                        }
                        Instruction instruction = (Instruction) topItem.object_;
                        break;
                    }*/
                    case PatchInstructionType.ECHO: {
                        System.Console.WriteLine (string.Join (" ", patchInstruction.args));
                        break;
                    }
                    case PatchInstructionType.CONTEXT_PUSH: {
                        foreach (string contextAddition in patchInstruction.args) {
                            context.push (contextAddition);
                        }
                        break;
                    }
                    case PatchInstructionType.CONTEXT_POP: {
                        context.pop (1);
                        break;
                    }
                    case PatchInstructionType.CONTEXT_PRINT: {
                        System.Console.WriteLine (context.generatePrintout ());
                        break;
                    }
                    default: {
                        throw new NotImplementedException (string.Format ("Instruction type {0}", patchInstruction.type.ToString ())); // This instruction wasn't implemented.
                    }
                }
            } catch (Exception e) {
                throw e;
            } finally {
                context.pop (contextAdditionCount);
            }
        }
    }
}
