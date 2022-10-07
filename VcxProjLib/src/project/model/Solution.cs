using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrosspathLib;
using Newtonsoft.Json;

namespace VcxProjLib {
    public class Solution {
        // Dependency injection. The object referenced there is mutable.
        internal Configuration config;

        /// <summary>
        /// The unique identifier of solution in terms of VS.
        /// </summary>
        protected readonly Guid selfGuid;

        /// <summary>
        /// Solution is a set of projects.
        /// </summary>
        internal readonly Dictionary<Int64, LinkedList<Project>> projects;

        // these containers have 2 targets:
        //   1. avoid duplicates;
        //   2. help in batch processing, like rebasing the whole solution from one directory to another.
        // regarding include directories:
        // since we can construct the same absolute path from different combination of relative path
        // and working directory, the 'String' is reconstructed abspath, which is unique.
        // TODO: use collection comparator instead to distinguish between same/different dirs
        // remember that solutionIncludeDirectories is provided for convenience of rebasing
        // but it does NOT need to be sorted and/or preserve order. go to Project for this
        // force includes are tracked as solution files too!
        protected List<ProjectFile> solutionFiles;
        protected HashSet<Compiler> solutionCompilers;
        protected HashSet<CompilerInstance> solutionCompilerInstances;
        protected Dictionary<String, IncludeDirectory> solutionIncludeDirectories;

        protected static HashSet<Guid> acquiredGuids = new HashSet<Guid>();

        public Solution(Configuration config) {
            this.config = config;
            projects = new Dictionary<Int64, LinkedList<Project>>();
            solutionFiles = new List<ProjectFile>();
            solutionCompilers = new HashSet<Compiler>(CompilerPossiblyRelativePathComparer.Instance);
            solutionCompilerInstances = new HashSet<CompilerInstance>();
            solutionIncludeDirectories = new Dictionary<String, IncludeDirectory>();
            selfGuid = AllocateGuid();
        }

        public static Guid AllocateGuid() {
            Guid guid;
            while (true) {
                guid = Guid.NewGuid();
                if (!acquiredGuids.Contains(guid)) {
                    // lol how I forgot this
                    acquiredGuids.Add(guid);
                    break;
                }
            }

            return guid;
        }

        /// <summary>
        /// Can be called multiple times.
        /// </summary>
        /// <param name="before">Old base (e.g. Unix path where compiled)</param>
        /// <param name="after">New base (e.g. Windows path where edited)</param>
        public void Rebase(AbsoluteCrosspath before, AbsoluteCrosspath after) {
            // move project files to another location
            foreach (ProjectFile projectFile in solutionFiles) {
                projectFile.FilePath.Rebase(before, after);
            }

            foreach (LinkedList<Project> chain in projects.Values) {
                foreach (Project prj in chain) {
                    foreach (ProjectFile projectFile in prj.ProjectFiles) {
                        projectFile.FilePath.Rebase(before, after);
                    }
                }
            }

            // move include directories to another location
            foreach (KeyValuePair<String, IncludeDirectory> includeDirPair in solutionIncludeDirectories) {
                includeDirPair.Value.Rebase(before, after);
            }

            if (this.config.BaseDir != null) {
                this.config.BaseDir.Rebase(before, after);
            }
        }

        internal ProjectFile TrackFile(ProjectFile pf, Boolean allowDuplicate = false) {
            if (!allowDuplicate) {
                foreach (ProjectFile pf1 in solutionFiles) {
                    if (pf1.FilePath.Equals(pf.FilePath)) {
                        return null;
                    }
                }
            }
            solutionFiles.Add(pf);
            return pf;
        }

        internal IncludeDirectory TrackIncludeDirectory(AbsoluteCrosspath includeDirPath, IncludeDirectoryType idt) {
            String includeDirStrReconstructed = includeDirPath.ToString();
            IncludeDirectory includeDir;
            if (!solutionIncludeDirectories.ContainsKey(includeDirStrReconstructed)) {
                includeDir = new IncludeDirectory(includeDirPath, idt);
                solutionIncludeDirectories.Add(includeDirStrReconstructed, includeDir);
                Logger.WriteLine(LogLevel.Debug, $"New include directory '{includeDirStrReconstructed}'");
            }
            else {
                // if this include directory is already known, then drop current object and get old reference
                includeDir = solutionIncludeDirectories[includeDirStrReconstructed];
                Logger.WriteLine(LogLevel.Trace, $"Reusing include directory '{includeDirStrReconstructed}'");
            }

            return includeDir;
        }

        public Boolean AddSourceFile(Crosspath compilerPath, AbsoluteCrosspath xpath, AbsoluteCrosspath workingDir, List<String> arguments) {
            // TODO: filter out entries by include and exclude lists
            // TODO: filter out compilers by include and exclude lists

            Compiler compiler = null;
            foreach (Compiler solutionCompiler in solutionCompilers) {
                if (solutionCompiler.ExePath.ToString().Equals(compilerPath.ToString())) {
                    // already registered
                    compiler = solutionCompiler;
                    break;
                }
            }
            if (compiler == null) {
                compiler = new Compiler(compilerPath);
                if (solutionCompilers.Add(compiler)) {
                    Logger.WriteLine(LogLevel.Info, $"New compiler '{compiler.ExePath}'");
                }
            }

            CompilerInstance compilerInstance = null;
            CompilerInstance compilerInstanceTmp = new CompilerInstance(compiler, arguments);
            foreach (CompilerInstance compilerInstanceInUse in compiler.Instances) {
                if (compilerInstanceInUse.Equals(compilerInstanceTmp)) {
                    compilerInstance = compilerInstanceInUse;
                    break;
                }
            }
            if (compilerInstance == null) {
                compilerInstance = compilerInstanceTmp;
                compiler.Instances.Add(compilerInstanceTmp);
                Logger.WriteLine(LogLevel.Info, $"New compiler instance '{compilerInstanceTmp.BaseCompiler.ExePath} {compilerInstanceTmp}'");
            }

            ProjectFile pf = new ProjectFile(this, xpath, compilerInstance);
            Logger.WriteLine(LogLevel.Trace, $"===== file {xpath} =====");

            pf.AddInfoFromCommandLine(workingDir, arguments);

            // put global override defines silently
            // TODO: write them to solution-wide props
            foreach (Define define in config.OverrideDefines) {
                pf.UnsetCppDefine(define.Name);
                pf.SetCppDefine(define.ToString());
            }

            // add to project files now?
            //pf.DumpData();
            Int64 projectHash = pf.HashProjectID();
            if (!projects.ContainsKey(projectHash)) {
                projects.Add(projectHash, new LinkedList<Project>());
            }

            // add file to project
            Boolean TryAddToChain(Project _prj) {
                if (!_prj.TestWhetherProjectFileBelongs(pf)) {
                    throw new ApplicationException(
                        $"[x] Could not add '{pf.FilePath}' to project chain '{projectHash}' - hash function error");
                }

                return _prj.AddProjectFile(pf);
            }

            Boolean added = false;
            foreach (Project prj in projects[projectHash]) {
                if (TryAddToChain(prj)) {
                    added = true;
                    break;
                }
            }

            if (!added) {
                // create a new project in chain for this file
                Project newProject = new Project(AllocateGuid(), projectHash, this, compilerInstance);
                projects[projectHash].AddLast(newProject);
                if (!TryAddToChain(newProject)) {
                    throw new ApplicationException(
                        $"[x] Could not add '{pf.FilePath}' to project chain '{projectHash}' - internal error");
                }
            }

            TrackFile(pf);
            return true;
        }

        public void ParseCompileDB(String filename) {
            // hope compiledb is not so large to eat all the memory
            String compiledbRaw = File.ReadAllText(filename);
            List<CompileDBEntry> entries = JsonConvert.DeserializeObject<List<CompileDBEntry>>(compiledbRaw);
            foreach (CompileDBEntry entry in entries) {
                // get full file path
                AbsoluteCrosspath workingDir = AbsoluteCrosspath.FromString(entry.directory);
                AbsoluteCrosspath xpath = AbsoluteCrosspath.FromString(entry.file, workingDir);

                // get compiler path
                // it can be absolute or relative
                Crosspath compilerPath = Crosspath.FromString(entry.arguments[0]);

                if (!AddSourceFile(compilerPath, xpath, workingDir, entry.arguments)) {
                    throw new ApplicationException("Could not add source file");
                }
            }

            Logger.WriteLine(LogLevel.Info, $"[i] Created a solution of {projects.Count} projects from {solutionFiles.Count} files");

            if (Logger.Level == LogLevel.Trace) {
                foreach (var projectKvp in projects) {
                    Logger.WriteLine(LogLevel.Trace, $"# Project ID {projectKvp.Key}");
                    foreach (var project in projectKvp.Value) {
                        foreach (var file in project.ProjectFiles) {
                            Logger.WriteLine(LogLevel.Trace, $"# > {file.FilePath}");
                        }
                        using (var enumerator = project.ProjectFiles.GetEnumerator()) {
                            if (!enumerator.MoveNext()) {
                                break;
                            }
                            if (enumerator.Current != null) {
                                enumerator.Current.DumpData();
                            }
                        }
                    }

                    Logger.WriteLine(LogLevel.Trace, "============================================");
                }
            }
        }

        public void RetrieveExtraInfoFromRemote(RemoteHost remote) {
            foreach (Compiler compiler in solutionCompilers) {
                foreach (CompilerInstance compilerInstance in compiler.Instances) {
                    compilerInstance.ExtractAdditionalInfo(remote);
                }
            }
        }

        public void FilterOutEntries() {
            foreach (Compiler compiler in solutionCompilers) {
                RelativeCrosspath compilerFilename = RelativeCrosspath.FromString(compiler.ExePath.LastEntry);
                if (config.ExcludeCompilers.Contains(compiler.ExePath) || config.ExcludeCompilers.Contains(compilerFilename)) {
                    // Remove projects
                    compiler.Skip = true;
                    Logger.WriteLine(LogLevel.Debug, $"Skipping compiler {compiler}, all inherent instances and projects");
                }
            }
        }

        public void DownloadCompilerIncludeDirectoriesFromRemote(RemoteHost remote, String dir) {
            String pwd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(dir);
            Directory.SetCurrentDirectory(dir);
            foreach (Compiler compiler in solutionCompilers) {
                compiler.DownloadStandardIncludeDirectories(remote);
            }
            Directory.SetCurrentDirectory(pwd);
        }

        public void CheckForTotalRebase(ref List<IncludeDirectory> remoteNotRebased) {
            foreach (IncludeDirectory includeDir in solutionIncludeDirectories.Values) {
                if (includeDir.Flavor == CrosspathFlavor.Unix) {
                    Logger.WriteLine(LogLevel.Warning, $"the remote include directory '{includeDir}' was not rebased");
                    if (remoteNotRebased != null) {
                        remoteNotRebased.Add(includeDir);
                    }
                }
                else if (includeDir.Flavor == CrosspathFlavor.Windows) {
                    if (!Directory.Exists(includeDir.ToString())) {
                        Logger.WriteLine(LogLevel.Warning, $"directory '{includeDir}' does not exist after rebase");
                    }
                }
            }

            foreach (ProjectFile pf in solutionFiles) {
                if (pf.FilePath.Flavor == CrosspathFlavor.Unix) {
                    Logger.WriteLine(LogLevel.Warning, $"project file '{pf.FilePath}' was not rebased and won't be accessible");
                }
                else if (pf.FilePath.Flavor == CrosspathFlavor.Windows) {
                    if (!File.Exists(pf.FilePath.ToString())) {
                        Logger.WriteLine(LogLevel.Warning, $"project file '{pf.FilePath}' does not exist after rebase");
                    }
                }
            }
        }

        public void WriteToDirectory(String directory) {
            Directory.CreateDirectory(directory);
            String pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(directory);

            AbsoluteCrosspath solutionDir = AbsoluteCrosspath.GetCurrentDirectory();

            foreach (Compiler compiler in solutionCompilers) {
                compiler.WriteToFile(solutionDir);
            }

            foreach (LinkedList<Project> projectChain in projects.Values) {
                foreach (Project projectKvp in projectChain) {
                    // remember there will also be .vcxproj.filters
                    projectKvp.WriteToFile(solutionDir);
                }
            }

            // TODO: add them as "Solution Items"
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionCompat, @"#pragma once");
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionPostCompat, templates.SolutionCompat);

            // regarding compiler_compat.h:
            // generate one file per compiler and reference them from projects
            // see Compiler.WriteToFile() instead

            File.WriteAllBytes(SolutionStructure.SolutionPropsFilename, templates.SolutionProps);

            // write .sln itself
            // using simple text generator
            using (StreamWriter sw = new StreamWriter(SolutionStructure.SolutionFilename, false, Encoding.UTF8)) {
                sw.WriteLine("");
                sw.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                sw.WriteLine("# Visual Studio Version 16");
                sw.WriteLine("VisualStudioVersion = 16.0.31613.86");
                sw.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                foreach (LinkedList<Project> projectChain in projects.Values) {
                    foreach (Project project in projectChain) {
                        if (project.Skip || project.CompilerInstance.BaseCompiler.Skip) {
                            continue;
                        }

                        // this magic GUID is "Windows (Visual C++)" project type
                        sw.WriteLine(
                            $"Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{project.Name}\", \"{project.Filename}\", \"{{{project.Guid.ToString().ToUpper()}}}\"");
                        sw.WriteLine("EndProject");
                    }
                }

                // this magic GUID is "Solution Folder"
                sw.WriteLine($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"Compat files\", \"Compat files\", \"{{{AllocateGuid()}}}\"");
                sw.WriteLine("\tProjectSection(SolutionItems) = preProject");
                sw.WriteLine("\t\tsolution_compat.h = solution_compat.h");
                sw.WriteLine("\t\tsolution_post_compiler_compat.h = solution_post_compiler_compat.h");
                sw.WriteLine("\tEndProjectSection");
                sw.WriteLine("EndProject");

                sw.WriteLine("Global");
                sw.WriteLine("GlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                    sw.WriteLine($"\t\t{cfg}|{SolutionStructure.SolutionPlatformName} = {cfg}|{SolutionStructure.SolutionPlatformName}");
                }

                sw.WriteLine("EndGlobalSection");
                sw.WriteLine("GlobalSection(ProjectConfigurationPlatforms) = postSolution");
                String[] cfgStages = { "ActiveCfg", "Build.0", "Deploy.0" };
                foreach (LinkedList<Project> projectChain in projects.Values) {
                    foreach (Project project in projectChain) {
                        if (project.Skip || project.CompilerInstance.BaseCompiler.Skip) {
                            continue;
                        }

                        foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                            foreach (String cfgStage in cfgStages) {
                                sw.WriteLine(
                                    $"\t\t{{{project.Guid.ToString().ToUpper()}}}.{cfg}|{SolutionStructure.SolutionPlatformName}.{cfgStage} = {cfg}|{project.CompilerInstance.VSPlatform}");
                            }
                        }
                    }
                }

                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
                sw.WriteLine("\t\tHideSolutionNode = FALSE");
                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
                sw.WriteLine($"\t\tSolutionGuid = {{{selfGuid}}}");
                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("EndGlobal");
                sw.Close();
            }

            Directory.SetCurrentDirectory(pwd);
        }
    }
}