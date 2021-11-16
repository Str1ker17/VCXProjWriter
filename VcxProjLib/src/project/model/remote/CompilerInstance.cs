using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using CrosspathLib;

namespace VcxProjLib {
    public class CompilerInstance {
        public Compiler BaseCompiler { get; }
        protected Guid instanceGuid { get; }

        public String PropsFileName { get { return String.Format(SolutionStructure.CompilerInstancePropsFileFormat, BaseCompiler.ShortName, this.Name); } }
        public String CompilerInstanceCompatHeaderPath { get { return String.Format(SolutionStructure.ForcedIncludes.CompilerInstanceCompat, BaseCompiler.ShortName, this.Name); } }

        /// <summary>
        /// Can only be known from querying an instance.
        /// </summary>
        public String Version { get; protected set; }
        public Platform VSPlatform = Platform.Unknown;
        public HashSet<String> identityOptions { get; }
        protected static UInt32 serial = 1;
        public String Name { get; }
        public Boolean HaveAdditionalInfo { get; protected set; }

        /// <summary>
        /// Since we are parsing output which is order-dependent, we have to preserve inserting order.
        /// Assuming that remote compiler removes duplicates on its own
        /// </summary>
        protected List<IncludeDirectory> IncludeDirectories { get; }
        protected HashSet<Define> Defines { get; }

        /// <summary>
        /// Readable identity by differentiating options.
        /// </summary>
        /// <returns></returns>
        public override String ToString() {
            return String.Join(" ", identityOptions).TrimEnd(' ');
        }

        public override Int32 GetHashCode() {
            return BaseCompiler.ShortName.GetHashCode();
        }

        public override Boolean Equals(Object obj) {
            if (ReferenceEquals(this, obj)) return true;
            if (this.GetType() != obj.GetType()) return false;
            return this.BaseCompiler.Equals(((CompilerInstance)obj).BaseCompiler) &&
                   this.identityOptions.SetEquals(((CompilerInstance)obj).identityOptions);
        }

        public CompilerInstance(Compiler baseCompiler, List<String> args) {
            this.BaseCompiler = baseCompiler;
            identityOptions = new HashSet<String>();
            instanceGuid = Solution.AllocateGuid();
            IncludeDirectories = new List<IncludeDirectory>();
            Defines = new HashSet<Define>(); // TODO: add comparer

            foreach (String arg in args) {
                // Machine-dependent options can change defines and include dirs
                // See also: https://gcc.gnu.org/onlinedocs/cpp/Common-Predefined-Macros.html
                if (arg.StartsWith("-m", StringComparison.Ordinal)
                 || arg.StartsWith("-fstack-protector", StringComparison.Ordinal) // __SSP__
                 || arg.StartsWith("-O", StringComparison.Ordinal) // __OPTIMIZE__
                 || arg.StartsWith("-funsigned-char", StringComparison.Ordinal) // __CHAR_UNSIGNED__
                 || arg.StartsWith("-fsanitize=", StringComparison.Ordinal) // __SANITIZE_ADDRESS__
                 || arg.StartsWith("-ffast-math", StringComparison.Ordinal)
                ) {
                    identityOptions.Add(arg);
                }
            }

            Name = $"{serial:D4}";
            ++serial;
        }

        /// <summary>
        /// Call RemoteHost.ExtractInfoFromCompiler() instead.
        /// Requires these utilities on the remote system: pwd, echo, which, touch, sort, pushd, popd, zip
        /// </summary>
        /// <param name="remote">Remote host where compiler installed</param>
        public void ExtractAdditionalInfo(RemoteHost remote) {
            String remoteTempFile = String.Format(Compiler.RemoteTempFile, BaseCompiler.ShortName, instanceGuid.ToString());
            if (remote.Execute("pwd || echo $PWD", out String pwd) != RemoteHost.Success) {
                throw new ApplicationException("could not get current directory name");
            }
            pwd = pwd.TrimEnd(RemoteHost.LineEndingChars);

            if (remote.Execute($"which {BaseCompiler.ExePath}", out String absExePath) != RemoteHost.Success) {
                throw new ApplicationException("could not get absolute compiler path");
            }
            absExePath = absExePath.TrimEnd(RemoteHost.LineEndingChars);

            if (remote.Execute($"{BaseCompiler.ExePath} -dumpversion", out String version) != RemoteHost.Success) {
                throw new ApplicationException("could not extract version from compiler");
            }
            Version = version.TrimEnd(RemoteHost.LineEndingChars);

            // create temporary .c file for auto-distinguishment between C and C++
            if (remote.Execute($"touch {remoteTempFile} || echo > {remoteTempFile}", out String _) != RemoteHost.Success) {
                throw new ApplicationException("could not create temporary file");
            }

            if (remote.Execute($"{BaseCompiler.ExePath} {this} -E -dM - < {remoteTempFile} | sort", out String defines) != RemoteHost.Success) {
                throw new ApplicationException("could not extract defines from compiler");
            }

            if (remote.Execute($"{BaseCompiler.ExePath} {this} -E -Wp,-v {remoteTempFile} 2>&1 1> /dev/null", out String includeDirs) != RemoteHost.Success) {
                throw new ApplicationException("could not extract include dirs from compiler");
            }

            AbsoluteCrosspath xpwd = AbsoluteCrosspath.FromString(pwd);
            AbsoluteCrosspath xcompiler = AbsoluteCrosspath.FromString(absExePath);
            if (!xcompiler.ToString().Equals(BaseCompiler.ExePath.ToString())) {
                AbsoluteCrosspath compilerLocatedIn = new AbsoluteCrosspath(xcompiler);
                ((RelativeCrosspath)BaseCompiler.ExePath).SetWorkingDirectory(xcompiler.ToContainingDirectory());
                Logger.WriteLine(LogLevel.Info, $"compiler '{BaseCompiler.ExePath}' actually located at '{xcompiler}'");
            }

            Defines.Clear();
            Platform fallbackPlatform = Platform.x64;
            foreach (String macro in defines.Split(RemoteHost.LineEndings, StringSplitOptions.RemoveEmptyEntries)) {
                // assuming standard format '#define MACRO some thing probably with spaces'
                String[] defArray = macro.Split(new[] {' '}, 3);
                // try to auto-detect the platform
                if (VSPlatform == Platform.Unknown) {
                    switch (defArray[1]) {
                        case "__x86_64__": VSPlatform = Platform.x64; break;
                        case "__i386__": VSPlatform = Platform.x86; break;
                        case "__arm__": VSPlatform = Platform.ARM; break;
                        case "__aarch64__": VSPlatform = Platform.ARM64; break;
                        case "__mips__": VSPlatform = Platform.MIPS; break;

                        case "__WORDSIZE__": /* this seems to be standard */
                        case "__INTPTR_WIDTH__": /* this not, but use as fallback */
                            if (Int32.Parse(defArray[2]) == 32) {
                                fallbackPlatform = Platform.x86;
                            }
                            break;
                    }
                }
                Defines.Add(new Define(defArray[1], defArray[2]));
            }
            if (VSPlatform == Platform.Unknown) {
                VSPlatform = fallbackPlatform;
            }

            IncludeDirectories.Clear();
            IncludeDirectoryType incDirType = IncludeDirectoryType.Null;
            foreach (String cppLine in includeDirs.Split(RemoteHost.LineEndings, StringSplitOptions.RemoveEmptyEntries)) {
                // sample output:
/*
ignoring nonexistent directory "/opt/gcc-4.1.2-glibc-2.5-binutils-2.17-kernel-2.6.18/arm-v5te-linux-gnueabi/include"
#include "..." search starts here:
#include <...> search starts here:
 /opt/gcc-4.1.2-glibc-2.5-binutils-2.17-kernel-2.6.18/bin/../lib/gcc/arm-v5te-linux-gnueabi/4.1.2/include
 /opt/gcc-4.1.2-glibc-2.5-binutils-2.17-kernel-2.6.18/bin/../sysroot-arm-v5te-linux-gnueabi/usr/include
End of search list.
*/
                // regarding priority:
                // -iquote has the highest priority affecting #include "..." not only #include <...>
                // -I has lower priority than -iquote
                // -isystem has lower priority but yet higher priority than system-wide headers
                //   then follows the priority of system-wide-headers
                // -idirafter has the lowest priority possible
                // example (gcc 9):
/*
$ gcc -x c -c -Wp,-v -iquote /proc/1 -I /proc/10 -isystem /proc/12 -I /proc/14 -idirafter /proc/1204 - < /dev/null
ignoring nonexistent directory "/usr/local/include/x86_64-linux-gnu"
ignoring nonexistent directory "/usr/lib/gcc/x86_64-linux-gnu/9/include-fixed"
ignoring nonexistent directory "/usr/lib/gcc/x86_64-linux-gnu/9/../../../../x86_64-linux-gnu/include"
#include "..." search starts here:
 /proc/1
#include <...> search starts here:
 /proc/10
 /proc/14
 /proc/12
 /usr/lib/gcc/x86_64-linux-gnu/9/include
 /usr/local/include
 /usr/include/x86_64-linux-gnu
 /usr/include
 /proc/1204
End of search list.
*/
                // usually gcc does not have any include directories in the #include "..." section.
                // so the reason for parsing this section is when using some compiler wrapper that puts some paths to -iquote.
                if (cppLine == "#include \"...\" search starts here:") {
                    incDirType = IncludeDirectoryType.Quote;
                    continue;
                }
                if (cppLine == "#include <...> search starts here:") {
                    incDirType = IncludeDirectoryType.System;
                    continue;
                }

                if (cppLine.Length > 0 && cppLine[0] == ' ') {
                    Crosspath xpath = Crosspath.FromString(cppLine.Substring(1));
                    if (xpath is RelativeCrosspath relPath) {
                        relPath.SetWorkingDirectory(xpwd);
                        xpath = relPath.Absolutized();
                    }
                    IncludeDirectory incDir = new IncludeDirectory(xpath as AbsoluteCrosspath, incDirType);
                    IncludeDirectory incDirCached = BaseCompiler.TrackIncludeDir(incDir);
                    if (incDirCached != null) {
                        incDir = incDirCached;
                    }
                    IncludeDirectories.Add(incDir);
                    
                    continue;
                }

                if (cppLine == "End of search list.") {
                    break;
                }
            }

            HaveAdditionalInfo = true;

            Logger.WriteLine(LogLevel.Info, $"{absExePath} {this} is {VSPlatform} compiler");
        }

        /// <summary>
        /// Writes compiler_${ShortName}.props and compiler_$(ShortName}_compat.h
        /// </summary>
        public void WriteToFile(AbsoluteCrosspath solutionDir) {
            if (BaseCompiler.Skip) {
                return;
            }

            AbsoluteCrosspath xpath = solutionDir.Appended(RelativeCrosspath.FromString(CompilerInstanceCompatHeaderPath)).ToContainingDirectory();
            Directory.CreateDirectory(xpath.ToString());
            using (StreamWriter sw = new StreamWriter(CompilerInstanceCompatHeaderPath, false, Encoding.UTF8)) {
                sw.WriteLine(@"#pragma once");
                sw.WriteLine($"/* This is generated from compiler instance {BaseCompiler} {this} */");
                foreach (Define compilerInternalDefine in Defines) {
                    sw.WriteLine($"#define {compilerInternalDefine.Name} {compilerInternalDefine.Value}");
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

            XmlElement projectNode = doc.CreateElement("Project");
            projectNode.SetAttribute("DefaultTargets", "Build");
            projectNode.SetAttribute("ToolsVersion", "Current");
            projectNode.SetAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlElement projectImportProps = doc.CreateElement("Import");
            projectImportProps.SetAttribute("Project", $@"$(SolutionDir)\{BaseCompiler.PropsFileName}");
            projectNode.AppendChild(projectImportProps);

            // Platform settings
            /*
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
             */
            XmlElement projectItemGroupPlatform = doc.CreateElement("ItemGroup");
            projectItemGroupPlatform.SetAttribute("Label", "ProjectConfigurations");

            foreach (String buildConfiguration in SolutionStructure.SolutionConfigurations) {
                XmlElement projectPlatformConfiguration = doc.CreateElement("ProjectConfiguration");
                projectPlatformConfiguration.SetAttribute("Include", $"{buildConfiguration}|{VSPlatform}");
                XmlElement projectPlatformConfiguration_Configuration = doc.CreateElement("Configuration");
                projectPlatformConfiguration_Configuration.InnerText = buildConfiguration;
                projectPlatformConfiguration.AppendChild(projectPlatformConfiguration_Configuration);
                XmlElement projectPlatformConfiguration_Platform = doc.CreateElement("Platform");
                projectPlatformConfiguration_Platform.InnerText = VSPlatform.ToString();
                projectPlatformConfiguration.AppendChild(projectPlatformConfiguration_Platform);
                projectItemGroupPlatform.AppendChild(projectPlatformConfiguration);
            }

            projectNode.AppendChild(projectItemGroupPlatform);

            // IDU settings
            XmlElement projectPropertyGroupIDU = doc.CreateElement("PropertyGroup");

            XmlElement projectIncludePaths = doc.CreateElement("NMakeIncludeSearchPath");
            // DONE: intermix project include directories with compiler include directories
            foreach (IncludeDirectory includePath in IncludeDirectories) {
                projectIncludePaths.InnerText += includePath.GetLocalProjectPath(solutionDir) + ";";
            }
            projectIncludePaths.InnerText += "$(CompilerIncludeDirAfter);$(NMakeIncludeSearchPath)";
            projectPropertyGroupIDU.AppendChild(projectIncludePaths);

            // maybe someday this will be helpful, but now it can be inherited from Solution.props
            XmlElement projectForcedIncludes = doc.CreateElement("NMakeForcedIncludes");
            // DONE: add compiler compat header to forced includes
            projectForcedIncludes.InnerText = $@"$(SolutionDir)\solution_compat.h;$(SolutionDir)\{CompilerInstanceCompatHeaderPath};$(SolutionDir)\solution_post_compiler_compat.h";
            projectPropertyGroupIDU.AppendChild(projectForcedIncludes);

            XmlElement compilerForcedIncludes = doc.CreateElement("CompilerCompat");
            // DONE: add compiler compat header to forced includes
            compilerForcedIncludes.InnerText = $@"$(SolutionDir)\{CompilerInstanceCompatHeaderPath}";
            projectPropertyGroupIDU.AppendChild(compilerForcedIncludes);

            projectNode.AppendChild(projectPropertyGroupIDU);

            doc.AppendChild(projectNode);
            doc.Save(PropsFileName);
        }
    }
}