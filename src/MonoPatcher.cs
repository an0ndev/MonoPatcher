using System;
using LibMonoPatcher;

class MonoPatcher {
	static void Main (string[] args) {
		if (args.Length != 3) {
            Console.WriteLine ("usage: mono MonoPatcher.exe [input assembly file] [patch directory] [output assembly file]");
            return;
        }
        string inputAssembly = args [0];
        string patchDirectory = args [1];
        string outputAssembly = args [2];
        LibMonoPatcher.LibMonoPatcher.patch (inputAssembly, patchDirectory, outputAssembly);
        return;
	}
}
