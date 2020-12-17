using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace VcxProjLib {
    public class Solution {
        protected Dictionary<Int64, Project> Projects;
        protected HashSet<ProjectFile> SolutionFiles;
        protected Dictionary<IncludeDirectoryType, String> IncludeParam;
        protected HashSet<Guid> AcquiredGuids;

        protected Solution() {
            Projects = new Dictionary<Int64, Project>();
            SolutionFiles = new HashSet<ProjectFile>();
            IncludeParam = new Dictionary<IncludeDirectoryType, String> {
                    {IncludeDirectoryType.Generic, "-I"}, {IncludeDirectoryType.Quote, "-iquote"}
                  , {IncludeDirectoryType.System, "-isystem"}, {IncludeDirectoryType.DirAfter, "-idirafter"}
            };
            AcquiredGuids = new HashSet<Guid>();
        }

        protected Guid AllocateGuid() {
            Guid guid;
            while (true) {
                guid = Guid.NewGuid();
                if (!AcquiredGuids.Contains(guid))
                    break;
            }

            return guid;
        }

        public static Solution CreateSolutionFromCompileDB(String filename, String substBefore = "",
                String substAfter = "") {
            Solution sln = new Solution();
            sln.SetSubstitutePath(substBefore, substAfter);
            sln.ParseCompileDB(filename);
            return sln;
        }

        protected String SubstBefore = String.Empty;
        protected String SubstAfter = String.Empty;

        public void SetSubstitutePath(String before, String after) {
            SubstBefore = before;
            SubstAfter = after;
        }

        protected void ParseCompileDB(String filename) {
            // hope compiledb is not so large to eat all the memory
            String compiledbRaw = File.ReadAllText(filename);
            List<CompileDBEntry> entries = JsonConvert.DeserializeObject<List<CompileDBEntry>>(compiledbRaw);
            int projectSerial = 1;
            foreach (CompileDBEntry entry in entries) {
                // get full file path
                String filepath = entry.file;
                if (!Path.IsPathRooted(filepath)) {
                    filepath = Path.GetFullPath(
                            Path.Combine(entry.directory.Replace(SubstBefore, SubstAfter), filepath));
                }

                ProjectFile pf = new ProjectFile(filepath);
                Console.WriteLine("===== file {0} =====", filepath);
                // parse arguments to obtain all -I, -D, -U
                // start from 1 to skip compiler name
                for (int i = 1; i < entry.arguments.Count; i++) {
                    String arg = entry.arguments[i];
                    // includes:
                    // -I
                    // -iquote
                    // -isystem
                    // -idirafter
                    // ..., see https://gcc.gnu.org/onlinedocs/gcc/Directory-Options.html
                    // TODO: also process -nostdinc to control whether system include dirs should be added or not.
                    IncludeDirectoryType idt = IncludeDirectoryType.Null;
                    String includeDir = String.Empty;
                    foreach (IncludeDirectoryType idk in IncludeParam.Keys) {
                        if (arg.StartsWith(IncludeParam[idk])) {
                            idt = idk;
                            includeDir = arg.Substring(IncludeParam[idk].Length);
                            break;
                        }
                    }

                    if (idt != IncludeDirectoryType.Null) {
                        if (includeDir == "") {
                            // this is a bit hacky
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            includeDir = entry.arguments[i + 1];
                            ++i;
                        }

                        // TODO: determine more accurately which include is local and which is remote
                        if (includeDir[0] != '/') {
                            includeDir = Path.GetFullPath(Path.Combine(entry.directory.Replace(SubstBefore, SubstAfter),
                                    includeDir));
                        }

                        pf.AddIncludeDir(includeDir);
                        //Console.WriteLine("[i] Added -I{0}", includeDir);
                        continue;
                    }

                    // defines:
                    // -D
                    if (arg.StartsWith("-D")) {
                        String defineString = arg.Substring("-D".Length);
                        if (defineString == "") {
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            defineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.Define(defineString);
                        //Console.WriteLine("[i] Added -D{0}", defineString);
                        continue;
                    }

                    // undefines:
                    // -U
                    if (arg.StartsWith("-U")) {
                        String undefineString = arg.Substring("-U".Length);
                        if (undefineString == "") {
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            undefineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.Undefine(undefineString);
                        //Console.WriteLine("[i] Added -U{0}", undefineString);
                        // ReSharper disable once RedundantJumpStatement
                        continue;
                    }
                }

                if (!SolutionFiles.Add(pf)) {
                    throw new ApplicationException("[x] Attempt to add the same ProjectFile multiple times");
                }

                // add to project files now?
                //pf.DumpData();
                Int64 projectHash = pf.HashProjectID();
                if (!Projects.ContainsKey(projectHash)) {
                    Projects.Add(projectHash
                          , new Project(AllocateGuid(), String.Format("Project{0}", projectSerial++)
                                  , pf.IncludeDirectories, pf.Defines));
                }

                // add file to project
                if (!Projects[projectHash].TestWhetherProjectFileBelongs(pf)) {
                    throw new ApplicationException(
                            $"[x] Could not add '{pf.FilePath}' to project '{projectHash}' - hash function error");
                }

                if (!Projects[projectHash].AddProjectFile(pf)) {
                    throw new ApplicationException(
                            $"[x] Could not add '{pf.FilePath}' to project '{projectHash}' - already exists");
                }
            }

            Console.WriteLine("[i] Created a solution of {0} projects from {1} files", Projects.Count,
                    SolutionFiles.Count);
            Console.WriteLine();
            foreach (var projectKvp in Projects) {
                Console.WriteLine("# Project ID {0}", projectKvp.Key);
                foreach (var file in projectKvp.Value.ProjectFiles) {
                    Console.WriteLine("# > {0}", file.Value.FilePath);
                }

                using (var enumerator = projectKvp.Value.ProjectFiles.GetEnumerator()) {
                    enumerator.MoveNext();
                    enumerator.Current.Value.DumpData();
                }

                Console.WriteLine("============================================");
            }

            // put a breakpoint here
            //Console.Read();
        }

        public void WriteToDirectory(String directory) {
            Directory.CreateDirectory(directory);
            String pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(directory);

            foreach (var projectKvp in Projects) {
                // remember there will also be .vcxproj.filters
                projectKvp.Value.WriteToFile();
            }

            // TODO: add them as "Solution Items"
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionCompat, @"");
            File.WriteAllText(SolutionStructure.ForcedIncludes.CompilerCompat, @"");
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionPostCompat, @"
#undef _WIN32
");
//#undef _MSC_VER
//#undef _MSC_FULL_VER
//#undef _MSC_BUILD
//#undef _MSC_EXTENSIONS

            File.WriteAllBytes(SolutionStructure.SolutionPropsFilename, templates.SolutionProps);

            // write .sln itself
            // using simple text generator
            StreamWriter sw = new StreamWriter(SolutionStructure.SolutionFilename, false, Encoding.UTF8);
            sw.WriteLine(@"");
            sw.WriteLine(@"Microsoft Visual Studio Solution File, Format Version 12.00");
            sw.WriteLine(@"# Visual Studio Version 16");
            sw.WriteLine(@"VisualStudioVersion = 16.0.30804.86");
            sw.WriteLine(@"MinimumVisualStudioVersion = 10.0.40219.1");
            foreach (var project in Projects.Values) {
                sw.WriteLine(String.Format(@"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{3}""", AllocateGuid()
                      , project.Name, project.Filename, project.Guid));
                sw.WriteLine("EndProject");
            }

            sw.WriteLine(@"Global");
            sw.WriteLine('\t' + @"GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach (var cfg in SolutionStructure.SolutionConfigurations) {
                sw.WriteLine(String.Format("\t\t{0} = {0}", cfg));
            }

            sw.WriteLine(@"EndGlobalSection");
            sw.WriteLine('\t' + @"GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            String[] cfgStages = {"ActiveCfg", "Build.0", "Deploy.0"};
            foreach (var project in Projects.Values) {
                foreach (string cfg in SolutionStructure.SolutionConfigurations) {
                    foreach (String cfgStage in cfgStages) {
                        sw.WriteLine(String.Format("\t\t{{{0}}}.{1}.{2} = {1}", project.Guid, cfg, cfgStage));
                    }
                }
            }

            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
            sw.WriteLine("\t\tHideSolutionNode = FALSE");
            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
            sw.WriteLine(String.Format("\t\tSolutionGuid = {{{0}}}", AllocateGuid()));
            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("EndGlobal");
            sw.Close();
        }
    }
}