using System;
using System.Collections.Generic;
using System.Text;

namespace VcxProjLib.CrosspathLib {
    public partial class Crosspath {
        public partial class AbsoluteCrosspath {
            protected Crosspath outer;

            public AbsoluteCrosspath(Crosspath outer) {
                this.outer = outer;
            }

            public String Get() {
                return this.Get(outer.Flavor);
            }

            public String Get(CrosspathFlavor flavor) {
                if (outer.Origin != CrosspathOrigin.Absolute) {
                    throw new NotSupportedException("instance is not absolute");
                }

                switch (flavor) {
                    case CrosspathFlavor.Unix:

                        break;

                    case CrosspathFlavor.Windows:
                        break;
                }

                throw new NotImplementedException();
            }

            public override String ToString() {
                // filter out .. and .
                Stack<String> dir_filtered = new Stack<String>();
                foreach (String dir in outer.directories) {
                    if (dir == ".")
                        continue;
                    if (dir == "..") {
                        dir_filtered.Pop();
                        continue;
                    }
                    dir_filtered.Push(dir);
                }

                StringBuilder sb = new StringBuilder();
                switch (outer.Flavor) {
                    case CrosspathFlavor.Windows:
                        sb.Append(outer.WindowsRootDrive);
                        sb.Append(':');
                        break;
                    case CrosspathFlavor.Unix:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (String dir in dir_filtered) {
                    switch (outer.Flavor) {
                        case CrosspathFlavor.Windows:
                            sb.Append('\\');
                            break;
                        case CrosspathFlavor.Unix:
                            sb.Append('/');
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    sb.Append(dir);
                }

                return sb.ToString();
            }
        }
    }
}