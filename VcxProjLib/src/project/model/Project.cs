using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CrosspathLib;

namespace VcxProjLib {
    public class Project {
        public Guid Guid { get; }
        public String Name { get; set; }

        public String Filename {
            // ReSharper disable once ArrangeAccessorOwnerBody
            get {
                return String.Format(SolutionStructure.ProjectFilePathFormat, Name);
            }
        }

        public Compiler Compiler { get; }

        /// <summary>
        /// Not only extra include directories, but system too.
        /// PRESERVE ORDER
        /// </summary>
        public HashSet<AbsoluteCrosspath> IncludeDirectories { get; }

        /// <summary>
        /// Undefines form defines, removing item from then.
        /// Does not require to preserve order.
        /// </summary>
        public HashSet<Define> Defines { get; }

        public Dictionary<String, ProjectFile> ProjectFiles { get; }


        public Project(Guid guid, String name, Compiler compiler, HashSet<AbsoluteCrosspath> includeDirectories, HashSet<Define> defines) {
            Guid = guid;
            Name = name;
            Compiler = compiler;
            // create copies, not references
            IncludeDirectories = new HashSet<AbsoluteCrosspath>(includeDirectories);
            Defines = new HashSet<Define>(defines, new DefineExactComparer());
            // initialize an empty set
            ProjectFiles = new Dictionary<String, ProjectFile>();
        }

        public Boolean AddProjectFile(ProjectFile pf) {
            if (ProjectFiles.ContainsKey(pf.FilePath.ToAbsolutizedString())) {
                return false;
            }

            ProjectFiles.Add(pf.FilePath.ToAbsolutizedString(), pf);
            return true;
        }

        public Boolean TestWhetherProjectFileBelongs(ProjectFile pf) {
            // TODO: allow relax if some defines are absent in one of sets
            return ((Compiler.ExePath == pf.Compiler.ExePath) 
                  && IncludeDirectories.SetEquals(pf.IncludeDirectories)
                  && Defines.SetEquals(pf.Defines));
        }

        public void WriteToFile() {
            String inheritFrom;
            if (Compiler.HaveAdditionalInfo) {
                inheritFrom = $@"$(SolutionDir)\{Compiler.PropsFileName}";
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
            // TODO: intermix project include directories with compiler include directories
            foreach (AbsoluteCrosspath includePath in IncludeDirectories) {
                projectIncludePaths.InnerText += includePath + ";";
            }
            projectIncludePaths.InnerText += "$(NMakeIncludeSearchPath)";
            projectPropertyGroupIDU.AppendChild(projectIncludePaths);

            // maybe someday this will be helpful, but now it can be inherited from Solution.props
            XmlElement projectForcedIncludes = doc.CreateElement("NMakeForcedIncludes");
            // TODO: add compiler compat header to forced includes
            projectForcedIncludes.InnerText = $@"{compatLocal};$(NMakeForcedIncludes);{compatLocalPost}";
            projectPropertyGroupIDU.AppendChild(projectForcedIncludes);

            XmlElement projectDefines = doc.CreateElement("NMakePreprocessorDefinitions");
            foreach (Define define in Defines) {
                projectDefines.InnerText += define + ";";
            }
            projectPropertyGroupIDU.AppendChild(projectDefines);

            projectNode.AppendChild(projectPropertyGroupIDU);

            // source file list
            XmlElement projectItemGroupCompiles = doc.CreateElement("ItemGroup");
            projectItemGroupCompiles.SetAttribute("Label", "Source Files");
            foreach (ProjectFile projectFile in this.ProjectFiles.Values) {
                XmlElement projectFileXmlElement = doc.CreateElement("ClCompile");
                projectFileXmlElement.SetAttribute("Include", projectFile.FilePath.ToAbsolutizedString());
                projectItemGroupCompiles.AppendChild(projectFileXmlElement);
            }

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
        }
    }
}