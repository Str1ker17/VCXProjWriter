using System;
using System.Collections.Generic;
using System.IO;
using CrosspathLib;
using VcxProjLib;

namespace VcxProjCLI {
    class Program {
        static void Main(String[] args) {
            String compiledb = "compile_commands.json";
            List<Tuple<String, String>> substitutions = new List<Tuple<String, String>>();
            substitutions.Add(new Tuple<String, String>(@"/local/store/bin-src/qemu", @"D:\Workspace\Sources\qemu"));
            substitutions.Add(new Tuple<String, String>(@"/sonic/src/sonic-swss", @"D:\Workspace\Sources\sonic-buildimage\src\sonic-swss"));

            Solution sln = Solution.CreateSolutionFromCompileDB(compiledb);
            foreach (Tuple<String, String> substitution in substitutions) {
                //sln.SubstitutePath(substitution.Item1, substitution.Item2);
                sln.Rebase(Crosspath.FromString(substitution.Item1) as AbsoluteCrosspath, Crosspath.FromString(substitution.Item2) as AbsoluteCrosspath);
            }
            
            try {
                Directory.Delete("output", true);
            }
            catch {
                // ignored
            }
            sln.WriteToDirectory("output");
        }
    }
}