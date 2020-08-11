namespace LibMonoPatcher {
    public class PatchInstruction {
        public PatchInstruction (PatchInstructionType _type, string[] _args) {
            type = _type;
            args = _args;
        }
        public PatchInstructionType type;
        public string[] args;
    }
    public enum PatchInstructionType {
        SHOW, // Shows information about a specific item or the currently selected one
        ASSERT, // Confirms information about a specific item before continuing, good for maintainability across versions
        SELECT, // Select a specific item
        INSERT, // Inserts an item into another item
        ECHO, // Prints all passed arguments
        CONTEXT_PRINT, // Prints the length and contents of the context
        CONTEXT_PUSH, // Adds items to the context
        CONTEXT_POP, // Removes an item from the context
        REPLACE // Replaces an instruction
    }
    public class PatchFile {
        public PatchFile (string _path, PatchInstruction[] _instructions) {
            path = _path;
            instructions = _instructions;
        }
        public string path;
        public PatchInstruction[] instructions;
    }
    public class PatchFileParsingException : System.Exception {
        public PatchFileParsingException () {}
        public PatchFileParsingException (string message) : base (message) {}
        public PatchFileParsingException (string message, System.Exception inner) : base (message, inner) {}
    }
    public class PatchFileParser {
        public static PatchFile parsePatchFile (string path) {
            System.IO.StreamReader patchFileReader = new System.IO.StreamReader (path);
            System.Collections.Generic.List<PatchInstruction> instructionList = new System.Collections.Generic.List<PatchInstruction> ();
            string line;
            bool inBlockComment = false;
            while ((line = patchFileReader.ReadLine ()) != null) {
                line = line.Trim (); // Remove whitespace from the beginning and end
                if (line == "") continue; // Ignore empty lines
                if (line.StartsWith ("#")) continue; // Ignore lines starting with "#" (comments)
                int hashtagPosition;
                while ((hashtagPosition = line.IndexOf ("#")) != -1) { // Ignore all portions of lines including and after "#"
                    line = line.Substring (0, hashtagPosition);
                    line = line.Trim ();
                }
                if (line.Contains ("/*")) {
                    inBlockComment = true;
                    continue;
                }
                if (line.Contains ("*/")) {
                    inBlockComment = false;
                    continue;
                }
                if (inBlockComment) continue;
                string[] typeAndArgs = line.Split (" ");
                if (typeAndArgs.Length < 1) throw new PatchFileParsingException ("No type or arguments specified in non-empty line!");
                PatchInstructionType type;
                if (!System.Enum.TryParse <PatchInstructionType> (typeAndArgs [0], true /* case-insensitive */, out type)) {
                    throw new PatchFileParsingException (System.String.Format ("Invalid type {0}!", typeAndArgs [0]));
                }
                string[] args;
                if (typeAndArgs.Length > 1) {
                    args = new string[typeAndArgs.Length - 1];
                    System.Array.Copy (typeAndArgs, 1, args, 0, typeAndArgs.Length - 1); // Equivalent of typeAndArgs [1:] in Python
                } else {
                    args = new string[0];
                }
                PatchInstruction instruction = new PatchInstruction (type, args);
                instructionList.Add (instruction);
            }
            patchFileReader.Close ();
            if (inBlockComment) throw new PatchFileParsingException ("EOF reached before block comment was terminated");
            PatchInstruction[] instructions = instructionList.ToArray ();
            PatchFile file = new PatchFile (path, instructions);
            return file;
        }
    }
}
