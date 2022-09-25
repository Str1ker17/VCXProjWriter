using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CrosspathLib;

namespace VcxProjLib {
    public class Project {

        protected class ProjectFileComparer : IEqualityComparer<ProjectFile> {
            public Boolean Equals(ProjectFile x, ProjectFile y) {
                if (x == null || y == null) return false;
                Boolean ret = x.FilePath.Equals(y.FilePath);
                return ret;
            }

            public Int32 GetHashCode(ProjectFile obj) {
                return 0;
            }
        }
        protected static ProjectFileComparer pfComparer = new ProjectFileComparer();

        protected static int nextProjectId = 1;

        public int ProjectSerial { get; }
        public Guid Guid { get; }
        public String Name { get; set; }
        public Int64 ProjectHash { get; protected set; }

        public String Filename {
            get {
                return String.Format(SolutionStructure.ProjectFilePathFormat, Name);
            }
        }

        /// <summary>
        /// Do not write down this project.
        /// </summary>
        public Boolean Skip { get; set; }

        /// <summary>
        /// Projects are created in context of solution, so there's a link to owner.
        /// </summary>
        protected Solution OwnerSolution { get; }

        /// <summary>
        /// Do not mix compilers together in a single project since the compiler
        /// carry its specific built-in defines and standard include directories.
        /// </summary>
        public CompilerInstance CompilerInstance { get; }

        /// <summary>
        /// Project is a set of source files.
        /// Does not require to preserve order.
        /// </summary>
        public HashSet<ProjectFile> ProjectFiles { get; }

        public HashSet<String> ProjectFilters { get; }

        public Project(Guid guid, Int64 projectHash, Solution ownerSolution, CompilerInstance compilerInstance) {
            ProjectSerial = nextProjectId++;
            Guid = guid;
            ProjectHash = projectHash;
            OwnerSolution = ownerSolution;
            Name = $"Project_{ProjectSerial:D4}";
            CompilerInstance = compilerInstance;
            // initialize an empty set
            ProjectFiles = new HashSet<ProjectFile>(pfComparer);
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

            return true;
        }

        public void WriteToFile(AbsoluteCrosspath solutionDir) {
            String inheritFrom;

            if (Skip || CompilerInstance.BaseCompiler.Skip) {
                return;
            }

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
            projectNode.SetAttribute("ToolsVersion", "Current");
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

            XmlElement projectPropertyGroupIDU = doc.CreateElement("PropertyGroup");
            // maybe someday this will be helpful, but now it can be inherited from Solution.props
            XmlElement projectForcedIncludes = doc.CreateElement("NMakeForcedIncludes");
            // TODO: add compiler compat header to forced includes
            projectForcedIncludes.InnerText = $@"$(ProjectDir)\{compatLocal};$(SolutionPreCompilerCompat);$(CompilerCompat);$(SolutionPostCompilerCompat);$(ProjectDir)\{compatLocalPost}";
            projectPropertyGroupIDU.AppendChild(projectForcedIncludes);
            projectNode.AppendChild(projectPropertyGroupIDU);

            // source file list
            XmlElement projectItemGroupCompiles = doc.CreateElement("ItemGroup");
            projectItemGroupCompiles.SetAttribute("Label", "Source Files");
            foreach (ProjectFile projectFile in this.ProjectFiles) {
                // exclusion list comes here!
                // TODO: we need to access path before rebase occured...
                XmlElement projectFileXmlElement = doc.CreateElement("ClCompile");
                projectFileXmlElement.SetAttribute("Include", projectFile.FilePath.ToString());

                // IDU settings

                XmlElement pfIncludePaths = doc.CreateElement("AdditionalIncludeDirectories");
                // DONE?: intermix project include directories with compiler include directories
                // in the project file
                foreach (IncludeDirectory includePath in projectFile.IncludeDirectories) {
                    // append -idirafter to the very end, below compiler dirs
                    if (includePath.Type == IncludeDirectoryType.DirAfter) {
                        continue;
                    }
                    pfIncludePaths.InnerText += includePath.GetLocalProjectPath(solutionDir) + ";";
                }
                pfIncludePaths.InnerText += "%(AdditionalIncludeDirectories)";
                foreach (IncludeDirectory includePath in projectFile.IncludeDirectories) {
                    // append -idirafter to the very end, below compiler dirs
                    if (includePath.Type == IncludeDirectoryType.DirAfter) {
                        pfIncludePaths.InnerText += ";" + includePath.GetLocalProjectPath(solutionDir);
                    }
                }
                projectFileXmlElement.AppendChild(pfIncludePaths);

                // maybe someday this will be helpful, but now it can be inherited from Solution.props
                XmlElement pfForcedIncludes = doc.CreateElement("ForcedIncludeFiles");
                foreach (AbsoluteCrosspath projectForcedInclude in projectFile.ForceIncludes) {
                    pfForcedIncludes.InnerText += $"{projectForcedInclude};";
                }
                pfForcedIncludes.InnerText += $"%(ForcedIncludeFiles)";
                projectFileXmlElement.AppendChild(pfForcedIncludes);

                XmlElement pfDefines = doc.CreateElement("PreprocessorDefinitions");
                foreach (Define define in projectFile.Defines.Values) {
                    pfDefines.InnerText += define + ";";
                }
                pfDefines.InnerText += "$(PreprocessorDefinitions)";
                projectFileXmlElement.AppendChild(pfDefines);

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
            projectNode.SetAttribute("ToolsVersion", "Current");
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