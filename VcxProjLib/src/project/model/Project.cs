﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CrosspathLib;

namespace VcxProjLib {
    public class Project {
        protected static int nextProjectId = 1;

        public int ProjectSerial { get; }
        public Guid Guid { get; }
        public String Name { get; set; }

        public String Filename {
            get {
                return String.Format(SolutionStructure.ProjectFilePathFormat, Name);
            }
        }

        /// <summary>
        /// Do not mix compilers together in a single project since the compiler
        /// carry its specific built-in defines and standard include directories.
        /// </summary>
        public CompilerInstance CompilerInstance { get; }

        /// <summary>
        /// Not only extra include directories, but system too.
        /// PRESERVE ORDER
        /// </summary>
        public IncludeDirectoryList IncludeDirectories { get; }

        /// <summary>
        /// The project in VS shares a common set of preprocessor macros.
        /// Does not require to preserve order since we are processing command-line defines ourselves,
        /// producing out only a 'clean' set of defines with their final values.
        /// </summary>
        public HashSet<Define> Defines { get; }

        /// <summary>
        /// The build system can enforce inclusion by the '-include' param
        /// and this is a precise code analyze dependency and a distinguisher factor too.
        /// </summary>
        public HashSet<AbsoluteCrosspath> ForcedIncludes { get; }

        /// <summary>
        /// Project is a set of source files.
        /// Does not require to preserve order.
        /// </summary>
        public HashSet<ProjectFile> ProjectFiles { get; }

        public HashSet<String> ProjectFilters { get; }

        public Project(Guid guid, CompilerInstance compilerInstance, IncludeDirectoryList includeDirectories
                     , HashSet<Define> defines, HashSet<AbsoluteCrosspath> forcedIncludes) {
            ProjectSerial = nextProjectId++;
            Guid = guid;
            Name = $"Project_{ProjectSerial:D4}";
            CompilerInstance = compilerInstance;
            // copy references
            IncludeDirectories = includeDirectories;
            Defines = defines;
            ForcedIncludes = forcedIncludes;
            // initialize an empty set
            ProjectFiles = new HashSet<ProjectFile>();
            ProjectFilters = new HashSet<String>();
        }

        public Boolean AddProjectFile(ProjectFile pf) {
            if (ProjectFiles.Contains(pf)) {
                // TODO: maybe the build system is dumb and recompiles the same file multiple times?
                return false;
            }

            ProjectFiles.Add(pf);

            if (pf.ProjectFolder != null) {
                // create project structure
                RelativeCrosspath projectFolder = new RelativeCrosspath(pf.ProjectFolder);
                Stack<String> todo = new Stack<String>();
                while (true) {
                    String fldr = projectFolder.ToString().Replace('/', '\\');
                    if (fldr == "." || ProjectFilters.Contains(fldr)) {
                        break;
                    }

                    todo.Push(fldr);
                    projectFolder.ToContainingDirectory();
                }

                foreach (String filter in todo) {
                    ProjectFilters.Add(filter);
                }
            }

            return true;
        }

        public Boolean TestWhetherProjectFileBelongs(ProjectFile pf) {
            // TODO: allow relax if some defines are absent in one of sets
            if (!CompilerPossiblyRelativePathComparer.Instance.Equals(CompilerInstance.BaseCompiler,
                    pf.CompilerOfFile.BaseCompiler)) {
                return false;
            }

            if (Solution.internalConfiguration.RelaxIncludeDirsOrder) {
                if (!IncludeDirectories.ListIdenticalRelaxOrder(pf.IncludeDirectories)) {
                    return false;
                }
            }
            else {
                if (!IncludeDirectories.ListIdentical(pf.IncludeDirectories)) {
                    return false;
                }
            }

            if (!Defines.SetEquals(pf.SetOfDefines)) {
                return false;
            }

            if (!ForcedIncludes.SetEquals(pf.ForceIncludes)) {
                return false;
            }

            return true;
        }

        public void WriteToFile(AbsoluteCrosspath solutionDir) {
            String inheritFrom;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (CompilerInstance.HaveAdditionalInfo) {
                inheritFrom = $@"$(SolutionDir)\{CompilerInstance.PropsFileName}";
            }
            else {
                inheritFrom = @"$(SolutionDir)\Solution.props";
            }

            Directory.CreateDirectory(Path.GetDirectoryName(this.Filename) ?? Directory.GetCurrentDirectory());

            // compatibility files
            // DONE: add compatibility files directly to the project node
            String compatPrefix = SolutionStructure.SeparateProjectsFromEachOther ? "" : $"{Name}.";
            String compatLocal = String.Format(SolutionStructure.ForcedIncludes.LocalCompat, compatPrefix);
            String compatLocalPost = String.Format(SolutionStructure.ForcedIncludes.LocalPostCompat, compatPrefix);
            File.WriteAllText($@"{Path.GetDirectoryName(Filename)}\{compatLocal}", @"");
            File.WriteAllText($@"{Path.GetDirectoryName(Filename)}\{compatLocalPost}", @"");

            // create with XML API
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

            XmlElement projectNode = doc.CreateElement("Project");
            projectNode.SetAttribute("DefaultTargets", "Build");
            projectNode.SetAttribute("ToolsVersion", "15.0");
            projectNode.SetAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlElement projectImportProps = doc.CreateElement("Import");
            projectImportProps.SetAttribute("Project", inheritFrom);
            projectNode.AppendChild(projectImportProps);

            XmlElement projectPropertyGroupGlobals = doc.CreateElement("PropertyGroup");
            projectPropertyGroupGlobals.SetAttribute("Label", "Globals");
            XmlElement projectGuid = doc.CreateElement("ProjectGuid");
            projectGuid.InnerText = $"{{{Guid}}}";
            projectPropertyGroupGlobals.AppendChild(projectGuid);
            projectNode.AppendChild(projectPropertyGroupGlobals);

            // IDU settings
            XmlElement projectPropertyGroupIDU = doc.CreateElement("PropertyGroup");

            XmlElement projectIncludePaths = doc.CreateElement("NMakeIncludeSearchPath");
            // DONE?: intermix project include directories with compiler include directories
            // in the project file
            foreach (IncludeDirectory includePath in IncludeDirectories) {
                // append -idirafter to the very end, below compiler dirs
                if (includePath.Type == IncludeDirectoryType.DirAfter) {
                    continue;
                }
                projectIncludePaths.InnerText += includePath.GetLocalProjectPath(solutionDir) + ";";
            }
            projectIncludePaths.InnerText += "$(NMakeIncludeSearchPath)";
            foreach (IncludeDirectory includePath in IncludeDirectories) {
                // append -idirafter to the very end, below compiler dirs
                if (includePath.Type == IncludeDirectoryType.DirAfter) {
                    projectIncludePaths.InnerText += ";" + includePath.GetLocalProjectPath(solutionDir);
                }
            }
            projectPropertyGroupIDU.AppendChild(projectIncludePaths);

            // maybe someday this will be helpful, but now it can be inherited from Solution.props
            XmlElement projectForcedIncludes = doc.CreateElement("NMakeForcedIncludes");
            // TODO: add compiler compat header to forced includes
            projectForcedIncludes.InnerText = $@"$(ProjectDir)\{compatLocal};$(SolutionPreCompilerCompat);$(CompilerCompat);$(SolutionPostCompilerCompat);$(ProjectDir)\{compatLocalPost}";
            foreach (AbsoluteCrosspath projectForcedInclude in ForcedIncludes) {
                projectForcedIncludes.InnerText += $";{projectForcedInclude}";
            }
            projectPropertyGroupIDU.AppendChild(projectForcedIncludes);

            XmlElement projectDefines = doc.CreateElement("NMakePreprocessorDefinitions");
            foreach (Define define in Defines) {
                projectDefines.InnerText += define + ";";
            }
            projectDefines.InnerText += "$(NMakePreprocessorDefinitions)";
            projectPropertyGroupIDU.AppendChild(projectDefines);

            projectNode.AppendChild(projectPropertyGroupIDU);

            // source file list
            XmlElement projectItemGroupCompiles = doc.CreateElement("ItemGroup");
            projectItemGroupCompiles.SetAttribute("Label", "Source Files");
            foreach (ProjectFile projectFile in this.ProjectFiles) {
                XmlElement projectFileXmlElement = doc.CreateElement("ClCompile");
                projectFileXmlElement.SetAttribute("Include", projectFile.FilePath.ToAbsolutizedString());
                projectItemGroupCompiles.AppendChild(projectFileXmlElement);
            }

            // add local_compat.h and local_post_compiler_compat.h
            XmlElement projectFileLocalCompatXmlElement = doc.CreateElement("ClCompile");
            projectFileLocalCompatXmlElement.SetAttribute("Include", "local_compat.h");
            projectItemGroupCompiles.AppendChild(projectFileLocalCompatXmlElement);
            XmlElement projectFileLocalPostCompatXmlElement = doc.CreateElement("ClCompile");
            projectFileLocalPostCompatXmlElement.SetAttribute("Include", "local_post_compiler_compat.h");
            projectItemGroupCompiles.AppendChild(projectFileLocalPostCompatXmlElement);

            projectNode.AppendChild(projectItemGroupCompiles);

            // TODO: add headers somehow...
            XmlElement projectItemGroupIncludes = doc.CreateElement("ItemGroup");
            projectItemGroupIncludes.SetAttribute("Label", "Header Files");
            /*
            foreach (ProjectFile projectFile in this.ProjectDepends.Values) {
                XmlElement projectFileXmlElement = doc.CreateElement("ClInclude");
                projectFileXmlElement.SetAttribute("Include", projectFile.FilePath);
                projectItemGroupIncludes.AppendChild(projectFileXmlElement);
            }
            */
            projectNode.AppendChild(projectItemGroupIncludes);

            doc.AppendChild(projectNode);
            doc.Save(this.Filename);

            WriteStructureToFile();
        }

        protected void WriteStructureToFile() {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

            XmlElement projectNode = doc.CreateElement("Project");
            projectNode.SetAttribute("DefaultTargets", "Build");
            projectNode.SetAttribute("ToolsVersion", "4.0");
            projectNode.SetAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlElement projectFolders = doc.CreateElement("ItemGroup");
            foreach (String filter in ProjectFilters) {
                XmlElement projectFolder = doc.CreateElement("Filter");
                projectFolder.SetAttribute("Include", filter);
                XmlElement projectFolderId = doc.CreateElement("UniqueIdentifier");
                projectFolderId.InnerText = Solution.AllocateGuid().ToString();
                projectFolder.AppendChild(projectFolderId);
                projectFolders.AppendChild(projectFolder);
            }
            projectNode.AppendChild(projectFolders);

            XmlElement projectFiles = doc.CreateElement("ItemGroup");
            foreach (ProjectFile pf in ProjectFiles) {
                if (pf.ProjectFolder == null) {
                    continue;
                }

                XmlElement projectFile = doc.CreateElement("ClCompile");
                projectFile.SetAttribute("Include", pf.FilePath.ToString());
                XmlElement projectFileFolder = doc.CreateElement("Filter");
                projectFileFolder.InnerText = pf.ProjectFolder.ToString().Replace('/', '\\');
                projectFile.AppendChild(projectFileFolder);
                projectFiles.AppendChild(projectFile);
            }
            projectNode.AppendChild(projectFiles);

            doc.AppendChild(projectNode);
            doc.Save($"{this.Filename}.filters");
        }
    }
}