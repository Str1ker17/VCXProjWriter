using System;
using VcxProjLib;

namespace VcxProjCLI {
    class Program {
        static void Main(string[] args) {
            String compiledb = "compile_commands.json";
            Solution sln = Solution.CreateSolutionFromCompileDB(compiledb, "/sonic/src/sonic-swss", @"D:\Workspace\Sources\sonic-buildimage\src\sonic-swss");
            sln.WriteToFile("solution.sln");
        }
    }
}