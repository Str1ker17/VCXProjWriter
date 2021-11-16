using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using CrosspathLib;

namespace VcxProjLib {
    /// <summary>
    /// Follow the convention that compiler lives on the RemoteHost, so
    /// RemoteHost have a reference to Compiler, and Compiler does not reference anything.
    /// </summary>
    public class Compiler {
        public Crosspath ExePath { get; }
        public String ShortName { get; }

        public String PropsFileName { get { return String.Format(SolutionStructure.CompilerPropsFileFormat, ShortName); } }

        public List<CompilerInstance> Instances { get; }
        /// <summary>
        /// Collect references to all include directories used by instances.
        /// </summary>
        protected List<IncludeDirectory> IncludeDirectories { get; }

        public Boolean Skip { get; set; }

        public static readonly String RemoteTempFile = "/tmp/VCXProjWriter_{0}_{1}.c";

        public Compiler(Crosspath path) {
            ExePath = path; // warning saves reference
            ShortName = ExePath.LastEntry;
            Instances = new List<CompilerInstance>();
            IncludeDirectories = new List<IncludeDirectory>();
        }

        public override String ToString() {
            return ExePath.ToString();
        }

        public IncludeDirectory TrackIncludeDir(IncludeDirectory includeDirectory) {
            // cached object
            foreach (IncludeDirectory incDir in IncludeDirectories) {
                if (incDir.ToAbsolutizedString() == includeDirectory.ToAbsolutizedString()) {
                    return incDir;
                }
            }
            // new object
            IncludeDirectories.Add(includeDirectory);
            return null;
        }

        public void DownloadStandardIncludeDirectories(RemoteHost remote) {
            if (Skip) {
                return;
            }
            // use sftp, or, when not possible, ssh cat
            AbsoluteCrosspath xpwd = AbsoluteCrosspath.GetCurrentDirectory();
            AbsoluteCrosspath xCompilerDir = xpwd.Appended(RelativeCrosspath.FromString($@"compilers\{ShortName}"));
            Directory.CreateDirectory(xCompilerDir.ToString());
            foreach (IncludeDirectory includeDirectory in IncludeDirectories) {
                includeDirectory.RebaseToLocal(remote, xCompilerDir);
            }
        }

        /// <summary>
        /// Writes compiler_${ShortName}.props and compiler_$(ShortName}_compat.h
        /// </summary>
        public void WriteToFile(AbsoluteCrosspath solutionDir) {
            if (Skip) {
                return;
            }

            AbsoluteCrosspath compilerDir = RelativeCrosspath.FromString(PropsFileName).Absolutized(AbsoluteCrosspath.GetCurrentDirectory()).ToContainingDirectory();
            Directory.CreateDirectory(compilerDir.ToString());

            foreach (CompilerInstance compilerInstance in Instances) {
                compilerInstance.WriteToFile(solutionDir);
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

            XmlElement projectNode = doc.CreateElement("Project");
            projectNode.SetAttribute("DefaultTargets", "Build");
            projectNode.SetAttribute("ToolsVersion", "Current");
            projectNode.SetAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlElement projectImportProps = doc.CreateElement("Import");
            projectImportProps.SetAttribute("Project", @"$(SolutionDir)\Solution.props");
            projectNode.AppendChild(projectImportProps);

            XmlElement projectPropertyGroupCompiler = doc.CreateElement("PropertyGroup");
            XmlElement projectCompilerExeName = doc.CreateElement("RemoteCCompileToolExe");
            projectCompilerExeName.InnerText = ExePath.ToString();
            projectPropertyGroupCompiler.AppendChild(projectCompilerExeName);
            XmlElement projectCompilerCppExeName = doc.CreateElement("RemoteCppCompileToolExe");
            projectCompilerCppExeName.InnerText = ExePath.ToString();
            projectPropertyGroupCompiler.AppendChild(projectCompilerCppExeName);

            projectNode.AppendChild(projectPropertyGroupCompiler);

            doc.AppendChild(projectNode);
            doc.Save(PropsFileName);
        }

        public override Int32 GetHashCode() {
            return ExePath.GetHashCode();
        }

        public override Boolean Equals(Object obj) {
            if (obj == null) return false;
            if (!(obj is Compiler compiler)) return false;
            return ExePath == compiler.ExePath;
        }

        /// <summary>
        /// Parse generic compiler argument, i.e. extract value of parameter.
        /// </summary>
        /// <returns></returns>
        public static bool ParseCommandLineArgument() {
            return false;
        }
    }
}