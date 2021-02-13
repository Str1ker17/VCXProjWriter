using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public static class SolutionStructure {
        public static readonly String SolutionFilename = @"Solution.sln";
        public static readonly String SolutionPropsFilename = @"Solution.props";

        public static readonly List<String> SolutionConfigurations = new List<String> {
                @"Debug|x64"
              , @"Release|x64"
        };

        public static readonly ForcedIncludesTemplate ForcedIncludes = new ForcedIncludesTemplate {
                LocalCompat = @"{0}local_compat.h"
              , SolutionCompat = "solution_compat.h"
              , CompilerCompat = @"compilers\{0}\compiler_{0}_compat.h"
              , SolutionPostCompat = "solution_post_compiler_compat.h"
              , LocalPostCompat = @"{0}local_post_compiler_compat.h"
        };

        // remember that forced includes may be per-project
        public static readonly Boolean SeparateProjectsFromEachOther = true;

        public static String ProjectFilePathFormat {
            get {
                if (SeparateProjectsFromEachOther)
                    return @"projects\{0:x16}\{0:x16}.vcxproj";

                return @"projects\{0:x16}.vcxproj";
            }
        }

        public class ForcedIncludesTemplate {
            public String LocalCompat;
            public String SolutionCompat;
            public String CompilerCompat;
            public String SolutionPostCompat;
            public String LocalPostCompat;
        }

        public static readonly String CompilerPropsFileFormat = @"compilers\{0}\compiler_{0}.props";
        public static readonly String RemoteIncludePath = @"remote\{0}";
    }
}