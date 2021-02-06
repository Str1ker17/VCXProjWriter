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
        public HashSet<AbsoluteCrosspath> IncludeDirectories { get; }
        public HashSet<Define> Defines { get; }

        public Compiler(String path) {
            ExePath = path;
        }

        /// <summary>
        /// Call RemoteHost.ExtractInfoFromCompiler() instead.
        /// </summary>
        /// <param name="remote"></param>
        [Obsolete]
        public void ExtractAdditionalInfo(RemoteHost remote) {
            throw new NotImplementedException("call RemoteHost.ExtractInfoFromCompiler() to fill in");
        }
    }
}