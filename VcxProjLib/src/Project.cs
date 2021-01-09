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
            get { return string.Format(SolutionStructure.ProjectFilePathFormat, Name); }
        }

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


        public Project(Guid guid, String name, HashSet<AbsoluteCrosspath> includeDirectories, HashSet<Define> defines) {
            Guid = guid;
            Name = name;
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
            return IncludeDirectories.SetEquals(pf.IncludeDirectories) && Defines.SetEquals(pf.Defines);
        }

        public void WriteToFile() {
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
            projectGuid.InnerText = string.Format("{{{0}}}", Guid);
            projectPropertyGroupGlobals.AppendChild(projectGuid);
            projectNode.AppendChild(projectPropertyGroupGlobals);

            // IDU settings
            XmlElement projectPropertyGroupIDU = doc.CreateElement("PropertyGroup");
            XmlElement projectIncludePaths = doc.CreateElement("NMakeIncludeSearchPath");
            foreach (Crosspath includePath in IncludeDirectories) {
                projectIncludePaths.InnerText += includePath + ";";
            }

            projectPropertyGroupIDU.AppendChild(projectIncludePaths);
            // maybe someday this will be helpful, but now it can be inherited from Solution.props
            //XmlElement projectForcedIncludes = doc.CreateElement("NMakeForcedIncludes");
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
            Directory.CreateDirectory(Path.GetDirectoryName(this.Filename) ?? Directory.GetCurrentDirectory());
            doc.Save(this.Filename);

            // compatibility files
            // TODO: add compatibility files directly to the project node
            File.WriteAllText(string.Format(SolutionStructure.ForcedIncludes.LocalCompat, Path.GetDirectoryName(Filename)), @"");
            File.WriteAllText(string.Format(SolutionStructure.ForcedIncludes.LocalPostCompat, Path.GetDirectoryName(Filename)), @"");
        }

        protected class DefineExactComparer : IEqualityComparer<Define> {
            public Boolean Equals(Define x, Define y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name && x.Value == y.Value;
            }

            public Int32 GetHashCode(Define obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}