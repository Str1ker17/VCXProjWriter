using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace VcxProjLib {
    public static class SolutionStructure {
        public static readonly String SolutionFilename = @"Solution.sln";
        public static readonly String SolutionPropsFilename = @"Solution.props";

        public static readonly List<String> SolutionConfigurations = new List<String> {
                @"Debug|x64"
              , @"Release|x64"
        };

        public static readonly ForcedIncludesStruct forcedIncludes;

        // remember that forced includes may be per-project
        public static readonly Boolean SeparateProjectsFromEachOther = true;

        public static String ProjectFilePathFormat {
            get {
                if (SeparateProjectsFromEachOther)
                    return @"projects\{0:x16}\{0:x16}.vcxproj";

                return @"projects\{0:x16}.vcxproj";
            }
        }

        public struct ForcedIncludesStruct {
            public String LocalCompat => @"{0}\{1}local_compat.h";
            public String SolutionCompat => "solution_compat.h";
            public String CompilerCompat => "compiler_compat.h";
            public String SolutionPostCompat => "solution_post_compiler_compat.h";
            public String LocalPostCompat => @"{0}\{1}local_post_compiler_compat.h";
        }
    }
}