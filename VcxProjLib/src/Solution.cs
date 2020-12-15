using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Build.Construction;
using Newtonsoft.Json;
// TODO: process .sln files
//using Microsoft.Build.Construction;
//using net.r_eg.MvsSln;
//using net.r_eg.MvsSln.Core;
//using net.r_eg.MvsSln.Core.ObjHandlers;
//using net.r_eg.MvsSln.Core.SlnHandlers;
//using net.r_eg.MvsSln.Extensions;
using VcxProjLib.HelperClasses.Projects;

namespace VcxProjLib {
    public class Solution {
        protected Dictionary<Int64, Project> Projects;
        protected HashSet<ProjectFile> SolutionFiles;
        protected Dictionary<IncludeDirectoryType, String> IncludeParam;

        protected Solution() {
            Projects = new Dictionary<Int64, Project>();
            SolutionFiles = new HashSet<ProjectFile>();
            IncludeParam = new Dictionary<IncludeDirectoryType, String> {
                    {IncludeDirectoryType.Generic, "-I"},
                    {IncludeDirectoryType.Quote, "-iquote"},
                    {IncludeDirectoryType.System, "-isystem"},
                    {IncludeDirectoryType.DirAfter, "-idirafter"}
            };
        }

        public static Solution CreateSolutionFromCompileDB(String filename, String substBefore = "",
                String substAfter = "") {
            Solution sln = new Solution();
            sln.SetSubstitutePath(substBefore, substAfter);
            //sln.ParseCompileDB(filename);
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

                        includeDir = Path.GetFullPath(Path.Combine(entry.directory.Replace(SubstBefore, SubstAfter),
                                includeDir));
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
                    Projects.Add(projectHash, new Project(pf.IncludeDirectories, pf.Defines));
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
            Console.Read();
        }

        public void WriteToFile(String filename) {
            try {
                Directory.Delete("output", true);
            }
            catch {
                // ignored
            }

            //HelperClasses.VCXProj.Project vs2019LinuxProject = new HelperClasses.VCXProj.Project();
            //vs2019LinuxProject.ToolsVersion = 15.0M;
            //vs2019LinuxProject.DefaultTargets = "Build";
            //vs2019LinuxProject.Items = 
/*
            net.r_eg.MvsSln.Sln sln = new Sln(SlnItems.All & ~SlnItems.ProjectDependenciesXml, String.Empty);
            sln.Result.Header.SetFormatVersion("12.00");
            sln.Result.Header.SetProgramVersion("16");
            sln.Result.Header.SetVisualStudioVersion("16.0.30804.86");
            sln.Result.Header.SetMinimumVersion("10.0.40219.1");
*/
#if USE_MVSSLN
            net.r_eg.MvsSln.Sln sln = new Sln(SlnItems.All & ~SlnItems.ProjectDependenciesXml, @"
Microsoft Visual Studio Solution File, Format Version 12.00
/# Visual Studio Version 16
VisualStudioVersion = 16.0.30804.86
MinimumVisualStudioVersion = 10.0.40219.1
Global
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {994F3729-6258-43E8-9347-93E80A14E154}
	EndGlobalSection
EndGlobal
");



            var whandlers = new Dictionary<Type, HandlerValue>() {
                    [typeof(LProject)] = new HandlerValue(),
            };
            using(SlnWriter w = new SlnWriter("modified.sln", whandlers)) {
                w.Write(sln.Result.Map);
            }

            // these are identical
            Microsoft.Build.Evaluation.Project prj = new Microsoft.Build.Evaluation.Project();
            prj.Save("project.vcxproj");

            XProject xprj = new XProject();
            xprj.Save("xproject.vcxproj", Encoding.UTF8);

            XProject xprj2 = new XProject(@"C:\Users\Str1ker\source\repos\Project2\Project2.vcxproj");

            Sln sln2 =
 new Sln(@"C:\Users\Str1ker\source\repos\Project2\Project2.sln", SlnItems.All & ~SlnItems.ProjectDependenciesXml);

            var whandlers2 = new Dictionary<Type, HandlerValue>() {
                    [typeof(LProject)] =
 new HandlerValue(new WProject(new [] {xprj2.ProjectItem.project}, new LProjectDependencies())),
            };

            using(var w = new SlnWriter(@"modified2.sln", whandlers2)) {
                w.Write(sln.Result.Map);
            }
#endif

            // create programmatically from template
            /*
            var prj_slim = new VcxProjLib.HelperClasses.Projects.VS2019LinuxProject();
            prj_slim.ToolsVersion = 15.0M;
            prj_slim.DefaultTargets = "Build";
            prj_slim.Import = new ProjectImport { Project = @"$(SolutionDir)\Solution.props" };
            prj_slim.PropertyGroup
            */

            // create by XML classes backend from template
            //XmlSerializer serdes = new XmlSerializer(typeof(VS2019LinuxProject));
            //VS2019LinuxProject deser = (VS2019LinuxProject)serdes.Deserialize(new MemoryStream(templates.Project2_vcxproj));

            // create with Microsoft API
            //Microsoft.Build.Evaluation.Project prj = new Microsoft.Build.Evaluation.Project();
            //prj.SetGlobalProperty("ToolsVersion", "15.0");
            //var inst = prj.CreateProjectInstance();
            //var root = inst.ToProjectRootElement();
            /*
            ProjectRootElement root = ProjectRootElement.Create();
            root.ToolsVersion = "15.0";
            root.DefaultTargets = "Build";
            //root.AddImport(@"$(SolutionDir)\Solution.props");
            var pgGuid = root.AddPropertyGroup();
            pgGuid.Label = "Globals";
            pgGuid.AddProperty("ProjectGuid", String.Format("{{{0}}}", Guid.NewGuid()));
            var pgNMake = root.AddPropertyGroup();
            pgNMake.AddProperty("NMakeIncludeSearchPath", "$(NMakeIncludeSearchPath)");
            pgNMake.AddProperty("NMakeForcedIncludes", "$(NMakeForcedIncludes)");
            pgNMake.AddProperty("NMakePreprocessorDefinitions", "$(NMakePreprocessorDefinitions)");
            pgNMake.AddProperty("AdditionalOptions", "$(AdditionalOptions)");
            var igSources = root.AddItemGroup();
            igSources.AddItem("ClCompile", "Source1.cpp");
            igSources.AddItem("ClCompile", @"src\Source2.cpp");
            var igHeaders = root.AddItemGroup();
            igHeaders.AddItem("ClInclude", "Header1.h");
            root.AddImport(@"$(SolutionDir)\Solution.props");
            root.Save("empty.vcxproj");
            //prj.Save("empty.vcxproj");
            */

            // create with XML API
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement projectNode = doc.CreateElement("Project");
            projectNode.SetAttribute("DefaultTargets", "Build");
            projectNode.SetAttribute("ToolsVersion", "15.0");
            projectNode.SetAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
            XmlElement projectImportProps = doc.CreateElement("Import");
            projectImportProps.SetAttribute("Project", @"$(SolutionDir)\Solution.props");
            projectNode.AppendChild(projectImportProps);
            XmlElement projectPropertyGroupGlobals = doc.CreateElement("PropertyGroup");
            projectPropertyGroupGlobals.SetAttribute("Label", "Globals");
            XmlElement projectGuid = doc.CreateElement("ProjectGuid");
            projectGuid.InnerText = String.Format("{{{0}}}", Guid.NewGuid());
            projectPropertyGroupGlobals.AppendChild(projectGuid);
            projectNode.AppendChild(projectPropertyGroupGlobals);
            doc.AppendChild(projectNode);
            doc.Save("empty.vcxproj");

            //XmlSerializer formatter = new XmlSerializer(prj_slim.GetType());
            //using (FileStream fs = new FileStream("empty.vcxproj", FileMode.OpenOrCreate)) {
            //    formatter.Serialize(fs, prj_slim);
            //}
        }
    }
}