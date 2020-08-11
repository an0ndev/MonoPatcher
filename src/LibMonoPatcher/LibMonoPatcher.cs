using System;
using System.IO;

namespace LibMonoPatcher {
    public class LibMonoPatcher {
        public static void patch (string inputAssembly, string patchDirectory, string outputAssembly) {
            FileAttributes inputAssemblyAttrs = File.GetAttributes (inputAssembly);
            FileAttributes patchDirectoryAttrs = File.GetAttributes (patchDirectory);
            if (!inputAssemblyAttrs.HasFlag (FileAttributes.Normal)) {
                throw new System.ArgumentException ("Input assembly is not a normal file!", "inputAssembly");
            }
            if (!patchDirectoryAttrs.HasFlag (FileAttributes.Directory)) {
                throw new System.ArgumentException ("Patch directory is not a directory!", "patchDirectory");
            }

            string[] patchFilePaths = Directory.GetFiles (patchDirectory, "*.monopatch", SearchOption.TopDirectoryOnly);
            if (patchFilePaths.Length < 1) {
                throw new System.ArgumentException ("Empty patch directory!", "patchDirectory");
            }
            System.Collections.Generic.List<PatchFile> patchFileList = new System.Collections.Generic.List<PatchFile> ();
            foreach (string patchFilePath in patchFilePaths) {
                PatchFile patchFile = PatchFileParser.parsePatchFile (patchFilePath);
                patchFileList.Add (patchFile);
            }

            PatchApplicator applicator = new PatchApplicator ();
            applicator.loadAssembly (inputAssembly);

            foreach (PatchFile patchFile in patchFileList) {
                applicator.applyPatchFile (patchFile);
            }

            applicator.saveAssembly (outputAssembly);
        }
    }
}
