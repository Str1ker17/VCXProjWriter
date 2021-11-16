using System;
using System.Text;

namespace CrosspathLib {
    public class RelativeCrosspath : Crosspath {
        /// <summary>
        /// A relative path differs from absolute in a way that a relative path
        /// is relative to something, i.e. it depends on a working directory.
        /// </summary>
        public AbsoluteCrosspath WorkingDirectory { get; protected set; }

        protected internal static RelativeCrosspath CreateInstance() {
            return new RelativeCrosspath();
        }

        protected RelativeCrosspath() {
        }

        /// <summary>
        /// Creates a copy of instance.
        /// </summary>
        /// <param name="source">Source RelativeCrosspath object, which will remain untouched.</param>
        public RelativeCrosspath(RelativeCrosspath source) : base(source) {
            this.WorkingDirectory = source.WorkingDirectory;
        }

        public void SetWorkingDirectory(AbsoluteCrosspath workdir) {
            this.WorkingDirectory = workdir;
        }

        /// <summary>
        /// Construct a new AbsoluteCrosspath object from this RelativeCrosspath object and its WorkingDirectory.
        /// </summary>
        /// <returns>AbsoluteCrosspath; exception if WorkingDirectory is not set.</returns>
        public AbsoluteCrosspath Absolutized() {
            // assume that:
            // - self is relative
            // - working directory is absolute
            return new AbsoluteCrosspath(WorkingDirectory).Append(this);
        }

        /// <summary>
        /// Construct a new AbsoluteCrosspath object from this RelativeCrosspath object and custom working directory.
        /// </summary>
        /// <param name="root">A working directory for this relative path.</param>
        /// <returns>AbsoluteCrosspath</returns>
        public AbsoluteCrosspath Absolutized(AbsoluteCrosspath root) {
            return new AbsoluteCrosspath(root).Append(this);
        }

        /// <summary>
        /// Creates RelativeCrosspath from string.
        /// If string does not contain a relative path, then throw an exception.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public new static RelativeCrosspath FromString(String path) {
            Crosspath xpath = Crosspath.FromString(path);
            if (!(xpath is RelativeCrosspath)) {
                throw new CrosspathLibException("the path provided is not relative");
            }
            return xpath as RelativeCrosspath;
        }

        public override String ToString() {
            // the .. and . are already filtered out on the creation stage, i.e. inside Chdir()
            if (directories.Count == 0) {
                return ".";
            }

            StringBuilder sb = new StringBuilder();
            foreach (String dir in directories) {
                sb.Append(dir);
                switch (Flavor) {
                    case CrosspathFlavor.Windows:
                        sb.Append('\\');
                        break;
                    case CrosspathFlavor.Unix:
                        sb.Append('/');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // cut trailing (back)slash
            return sb.ToString(0, sb.Length - 1);
        }

        public override String ToAbsolutizedString() {
            if (WorkingDirectory is null) {
                throw new PolymorphismException($"attempt to absolutize RelativePath '{this}' without a WorkingDirectory");
            }

            // This creates much garbage. Optimize when some good (or bad) times will come.
            return new AbsoluteCrosspath(WorkingDirectory).Append(this).ToString();
        }

        public static RelativeCrosspath CreateRelativePath(AbsoluteCrosspath xpath, AbsoluteCrosspath workingDirectory, Boolean dontGoOut) {
            if (xpath.Flavor != workingDirectory.Flavor) {
                throw new PolymorphismException("can relativize only identically-flavored paths");
            }

            if (xpath.Flavor == CrosspathFlavor.Windows && xpath.WindowsRootDrive != workingDirectory.WindowsRootDrive) {
                throw new CrosspathLibException("cannot relativize different root drives");
            }

            /*
            RelativeCrosspath ret = RelativeCrosspath.CreateInstance();
            ret.Flavor = xpath.Flavor;
            ret.Origin = CrosspathOrigin.Relative;
            ret.WindowsRootDrive = xpath.WindowsRootDrive;
            ret.directories = new LinkedList<String>();
            */
            RelativeCrosspath ret = RelativeCrosspath.FromString("");
            ret.Flavor = xpath.Flavor;
            ret.WindowsRootDrive = xpath.WindowsRootDrive;
            ret.SetWorkingDirectory(workingDirectory);

            using (var myIter = xpath.directories.GetEnumerator()) {
                using (var theirsIter = workingDirectory.directories.GetEnumerator()) {
                    Boolean myMoved;
                    Boolean theirsMoved;

                    // first find a diverging point
                    while(true) {
                        myMoved = myIter.MoveNext();
                        theirsMoved = theirsIter.MoveNext();
                        if (!myMoved || !theirsMoved) {
                            break;
                        }
                        if (myIter.Current != theirsIter.Current) {
                            break;
                        }
                    }

                    if (theirsMoved && dontGoOut) {
                        throw new CrosspathLibException("path is outide of working dir");
                    }

                    // then go back from workingDirectory and go forth on xpath

                    for (; theirsMoved; theirsMoved = theirsIter.MoveNext()) {
                        ret.Chdir("..");
                    }

                    for (; myMoved; myMoved = myIter.MoveNext()) {
                        ret.Chdir(myIter.Current);
                    }
                }
            }

            return ret;
        }

        public override Int32 GetHashCode() {
            return base.GetHashCode();
        }

        /// <summary>
        /// Support compare against AbsoluteCrosspath if they actually point to the same file.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object obj) {
            if (obj is RelativeCrosspath relativeCrosspath) {
                return EqualsToRelative(relativeCrosspath);
            }

            if (obj is AbsoluteCrosspath absoluteCrosspath) {
                return EqualsToAbsolute(absoluteCrosspath);
            }

            return false;
        }

        internal Boolean EqualsToAbsolute(AbsoluteCrosspath absoluteCrosspath) {
            if (WorkingDirectory is null) {
                return false;
            }

            // Do not use base comparer there!
            return this.ToAbsolutizedString() == absoluteCrosspath.ToAbsolutizedString();
        }

        internal Boolean EqualsToRelative(RelativeCrosspath relativeCrosspath) {
            if (this.WorkingDirectory == null) {
                if (relativeCrosspath.WorkingDirectory != null) {
                    return false;
                }
            }

            if (this.WorkingDirectory != null && !this.WorkingDirectory.Equals(relativeCrosspath.WorkingDirectory)) {
                return false;
            }

            return base.Equals(relativeCrosspath);
        }
    }
}
