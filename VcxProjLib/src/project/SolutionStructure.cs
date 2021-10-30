using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public static class SolutionStructure {
        public static readonly String SolutionFilename = @"Solution.sln";
        public static readonly String SolutionPropsFilename = @"Solution.props";

        public static readonly List<String> SolutionConfigurations = new List<String> {
                "Debug"
              , "Release"
        };

        public static readonly String SolutionPlatformName = "Multiarch";

        public static readonly ForcedIncludesTemplate ForcedIncludes = new ForcedIncludesTemplate {
                LocalCompat = @"{0}local_compat.h"
              , SolutionCompat = "solution_compat.h"
              , CompilerInstanceCompat = @"compilers\{0}\instances\compiler_{0}_compat_{1}.h"
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
            public String LocalCompat { get; set; }
            public String SolutionCompat { get; set; }
            public String CompilerInstanceCompat { get; set; }
            public String SolutionPostCompat { get; set; }
            public String LocalPostCompat { get; set; }
        }

        public static readonly String CompilerPropsFileFormat = @"compilers\{0}\compiler_{0}.props";
        public static readonly String CompilerInstancePropsFileFormat = @"compilers\{0}\instances\instance_{1}.props";
        public static readonly String RemoteIncludePath = @"remote\{0}";
    }
}