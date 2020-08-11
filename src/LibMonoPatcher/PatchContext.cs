using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LibMonoPatcher {
    public class ContextItem {
        public System.Type type;
        public System.Object object_;
        public ContextItem (System.Type _type, System.Object _object) { // initialize like item.GetType (), (System.Object) item
            type = _type;
            object_ = _object;
        }
    }
    class PatchContext {
        public System.Collections.Generic.List<ContextItem> items;
        /*
        static System.Type[] contextItemOrder = new System.Type[6] {
            typeof (AssemblyDefinition),
            typeof (ModuleDefinition),
            typeof (TypeDefinition),
            typeof (MethodDefinition),
            typeof (Instruction)
        };*/
        // System.Collections.Generic.List<ContextItemContainer> context = new System.Collections.Generic.List<ContextItemContainer> ();

        public PatchContext (AssemblyDefinition _assembly) {
            items = new System.Collections.Generic.List<ContextItem> ();
            ContextItem assemblyItem = new ContextItem (typeof (AssemblyDefinition), (System.Object) _assembly);
            items.Add (assemblyItem);
        }
        public System.Tuple<int, string[]> parseInKeyword (string[] args) {
            int indexOfIn;
            System.Collections.Generic.List<string> contextAdditions = new System.Collections.Generic.List<string> ();
            while ((indexOfIn = System.Array.LastIndexOf (args, "in")) != -1) {
                if (!((indexOfIn + 2) <= args.Length)) {
                    // There's no string after "in", e.g. ["select", "bruh", "in"] (note missing target)
                    throw new System.ArgumentException ("No target for in keyword");
                }
                contextAdditions.Add (args [indexOfIn + 1]);
                System.Collections.Generic.List<string> argList = new System.Collections.Generic.List<string> (args);
                argList.RemoveAt (indexOfIn);
                argList.RemoveAt (indexOfIn); // the index of the name is now indexOfIn
                args = argList.ToArray ();
            }
            foreach (string contextAddition in contextAdditions) {
                push (contextAddition);
            }
            return System.Tuple.Create (contextAdditions.Count, args);
        }
        public void push (string contextAddition) {
            // TODO fix dummy implementation
            ContextItem topItem = items [items.Count - 1];
            System.Func<System.Type, bool> check = type => topItem.type == type;
            System.Action<string> throwInvalid = typeOfInvalidThing => throw new System.ArgumentException (string.Format ("Invalid {0} {1}", typeOfInvalidThing, contextAddition), "contextAddition");

            if (check (typeof (AssemblyDefinition))) {
                // Finding a ModuleDefinition in this AssemblyDefinition
                AssemblyDefinition assemblyDefinition = (AssemblyDefinition) topItem.object_;
                bool foundModuleDefinition = false;
                foreach (ModuleDefinition childModuleDefinition in assemblyDefinition.Modules) {
                    if (childModuleDefinition.FileName == contextAddition) {
                        items.Add (new ContextItem (typeof (ModuleDefinition), (System.Object) childModuleDefinition));
                        foundModuleDefinition = true;
                        break;
                    }
                }
                if (!foundModuleDefinition) throwInvalid ("module file name");
            } else if (check (typeof (ModuleDefinition))) {
                // Finding a TypeDefinition in this ModuleDefinition
                ModuleDefinition moduleDefinition = (ModuleDefinition) topItem.object_;
                bool foundTypeDefinition = false;
                foreach (TypeDefinition childTypeDefinition in moduleDefinition.Types) {
                    if (childTypeDefinition.Name == contextAddition) {
                        items.Add (new ContextItem (typeof (TypeDefinition), (System.Object) childTypeDefinition));
                        foundTypeDefinition = true;
                        break;
                    }
                }
                if (!foundTypeDefinition) throwInvalid ("type name");
            } else if (check (typeof (TypeDefinition))) {
                // Finding a MethodDefinition in this TypeDefinition
                TypeDefinition typeDefinition = (TypeDefinition) topItem.object_;
                bool foundMethodDefinition = false;
                foreach (MethodDefinition childMethodDefinition in typeDefinition.Methods) {
                    if (childMethodDefinition.Name == contextAddition) {
                        items.Add (new ContextItem (typeof (MethodDefinition), (System.Object) childMethodDefinition));
                        foundMethodDefinition = true;
                        break;
                    }
                }
                if (!foundMethodDefinition) throwInvalid ("method name");
            } else if (check (typeof (MethodDefinition))) {
                // Finding an Instruction in this MethodDefinition(.Body)
                MethodDefinition methodDefinition = (MethodDefinition) topItem.object_;
                bool foundInstruction = false;
                foreach (Instruction childInstruction in methodDefinition.Body.Instructions) {
                    if (("@" + childInstruction.Offset.ToString ("x4")) == contextAddition) {
                        items.Add (new ContextItem (typeof (Instruction), (System.Object) childInstruction));
                        foundInstruction = true;
                        break;
                    }
                }
                if (!foundInstruction) throwInvalid ("instruction address");
            } else if (check (typeof (Instruction))) {
                // Context is full
                throw new System.InvalidOperationException ("Cannot push onto full context");
            } else {
                // Unknown item on top of context
                throw new System.InvalidOperationException ("Unknown item on top of context");
            }
        }
        public void pop (int contextLevelIncreaseCounter) {
            for (int popCounter = 0; popCounter < contextLevelIncreaseCounter; popCounter++) {
                items.RemoveAt (items.Count - 1);
            }
        }
        public string generatePrintout () {
            return string.Format (
                "Context: length {0}, topmost type {1}",
                items.Count,
                items [items.Count - 1].type.ToString ()
            );
        }
    }
}
