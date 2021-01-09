using System;

namespace VcxProjLib.CrosspathLib {
    public partial class Crosspath {
        public partial class RelativeCrosspath {
            protected Crosspath outer;

            public RelativeCrosspath(Crosspath outer) {
                this.outer = outer;
            }

            public Crosspath WorkingDirectory { get; private set; }

            public void SetWorkingDirectory(Crosspath workdir) {
                if (workdir.Origin != CrosspathOrigin.Absolute) {
                    throw new ArgumentOutOfRangeException(nameof(workdir), "should be absolute");
                }

                this.WorkingDirectory = workdir;
            }

            public Crosspath Absolutized() {
                // assume that:
                // - self is relative
                // - working directory is absolute
                return new Crosspath(WorkingDirectory).Append(outer);
            }
        }
    }
}