using System;
using CrosspathLib;

namespace VcxProjLib {
    public class IncludeDirectory : AbsoluteCrosspath {
        public IncludeDirectoryType Type { get; }
        public String ShortName { get; }

        protected static int serial = 1;

        public IncludeDirectory(AbsoluteCrosspath path, IncludeDirectoryType type) : base(path) {
            Type = type;
            ShortName = $"{serial:D4}";
            ++serial;
        }
    }
}
