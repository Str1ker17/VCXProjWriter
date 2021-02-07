using System;
using System.Collections.Generic;
using CrosspathLib;

namespace VcxProjLib {
    /// <summary>
    /// Follow the convention that compiler lives on the RemoteHost, so
    /// RemoteHost have a reference to Compiler, and Compiler does not reference anything.
    /// </summary>
    public class Compiler {
        public String ExePath { get; }
        public String Version { get; protected set; }
        /// <summary>
        /// Since we are parsing output which is order-dependent, we have to preserve inserting order.
        /// Assuming that remote compiler removes duplicates on its own
        /// </summary>
        public List<IncludeDirectory> IncludeDirectories { get; }
        public SortedSet<Define> Defines { get; }

        protected static readonly String[] LineEndings = {"\r\n", "\n", "\r"};
        protected static readonly Char[] LineEndingsChar = {'\n', '\r'};

        public Compiler(String path) {
            ExePath = path;
            IncludeDirectories = new List<IncludeDirectory>();
            Defines = new SortedSet<Define>(); // TODO: add comparer
        }

        /// <summary>
        /// Call RemoteHost.ExtractInfoFromCompiler() instead.
        /// </summary>
        /// <param name="remote">Remote host where compiler installed</param>
        public void ExtractAdditionalInfo(RemoteHost remote) {
            if (remote.Execute($"pwd || echo $PWD", out String pwd) != RemoteHost.Success) {
                throw new ApplicationException("could not get current directory name");
            }

            if (remote.Execute($"which {ExePath}", out String absExePath) != RemoteHost.Success) {
                throw new ApplicationException("could not get absolute compiler path");
            }

            if (remote.Execute($"{ExePath} -dumpversion", out String version) != RemoteHost.Success) {
                throw new ApplicationException("could not extract version from compiler");
            }

            // create temporary .c file for auto-distinguishment between C and C++
            String tempFile = "/tmp/VCXProjWriterTemp.c";
            if (remote.Execute($"touch {tempFile} || echo > {tempFile}", out String nothing) != RemoteHost.Success) {
                throw new ApplicationException("could not create temporary file");
            }

            if (remote.Execute($"{ExePath} -E -dM {tempFile}", out String defines) != RemoteHost.Success) {
                throw new ApplicationException("could not extract defines from compiler");
            }

            if (remote.Execute($"{ExePath} -c -Wp,-v {tempFile} 2>&1 1> /dev/null", out String includeDirs) != RemoteHost.Success) {
                throw new ApplicationException("could not extract include dirs from compiler");
            }

            AbsoluteCrosspath xpwd = Crosspath.FromString(pwd.TrimEnd(LineEndingsChar)) as AbsoluteCrosspath;
            if (Crosspath.FromString(absExePath.TrimEnd(LineEndingsChar)) is AbsoluteCrosspath xCompilerPath && xCompilerPath.ToString() != ExePath) {
                Logger.WriteLine(LogLevel.Info, $"compiler '{ExePath}' actually located at '{xCompilerPath.ToString()}'");
            }
            Version = version.TrimEnd(LineEndingsChar);

            Defines.Clear();
            foreach (String macro in defines.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries)) {
                // assuming standard format '#define MACRO some thing probably with spaces'
                String[] defArray = macro.Split(new[] {' '}, 3);
                Defines.Add(new Define(defArray[1], defArray[2]));
            }

            IncludeDirectories.Clear();
            IncludeDirectoryType incDirType = IncludeDirectoryType.Null;
            foreach (String cppLine in includeDirs.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries)) {
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
                    IncludeDirectories.Add(incDir);
                    continue;
                }

                if (cppLine == "End of search list.") {
                    break;
                }
            }
        }

        public void DownloadAdditionalInfo(RemoteHost remote) {
            // use sftp, or, when not possible, ssh cat
        }

        public override Int32 GetHashCode() {
            return ExePath.GetHashCode();
        }

        public override Boolean Equals(Object obj) {
            if (obj == null) return false;
            if (!(obj is Compiler)) return false;
            return ExePath == ((Compiler) obj).ExePath;
        }
    }
}