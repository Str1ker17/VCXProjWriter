using CrosspathLib;

namespace VcxProjLib {
    public class IncludeDirectory : AbsoluteCrosspath {
        public IncludeDirectoryType Type { get; }

        public IncludeDirectory(AbsoluteCrosspath path, IncludeDirectoryType type) : base(path) {
            Type = type;
        }
    }
}
