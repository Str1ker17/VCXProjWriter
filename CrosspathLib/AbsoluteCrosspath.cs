using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CrosspathLib {
    public class AbsoluteCrosspath : Crosspath, IEquatable<AbsoluteCrosspath> {
        protected internal static Crosspath CreateInstance() {
            return new AbsoluteCrosspath();
        }

        /// <summary>
        /// Only for internal usage
        /// </summary>
        protected AbsoluteCrosspath() {
        }

        /// <summary>
        /// Creates a copy of instance.
        /// </summary>
        /// <param name="source">Source AbsoluteCrosspath object, which will remain untouched.</param>
        public AbsoluteCrosspath(AbsoluteCrosspath source) : base(source) {
            // no new fields are introduced
        }

        /// <summary>
        /// Creates AbsoluteCrosspath from string.
        /// If string does not contain an absolute path, then throw an exception.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public new static AbsoluteCrosspath FromString(String path) {
            Crosspath xpath = Crosspath.FromString(path);
            if (!(xpath is AbsoluteCrosspath)) {
                throw new CrosspathLibException("the path provided is not absolute");
            }
            return xpath as AbsoluteCrosspath;
        }

        public static AbsoluteCrosspath FromString(String path, AbsoluteCrosspath workingDir) {
            Crosspath xpath = Crosspath.FromString(path);
            if (!(xpath is AbsoluteCrosspath)) {
                return ((RelativeCrosspath) xpath).Absolutized(workingDir);
            }
            return xpath as AbsoluteCrosspath;
        }

        public static AbsoluteCrosspath GetCurrentDirectory() {
            String pwd = Directory.GetCurrentDirectory();
            return AbsoluteCrosspath.FromString(pwd);
        }

        /// <summary>
        /// Appends a relative part to the path.
        /// </summary>
        /// <param name="part">Path to append.</param>
        /// <returns>Modified self object.</returns>
        public new AbsoluteCrosspath Append(RelativeCrosspath part) {
            return base.Append(part) as AbsoluteCrosspath;
        }

        /// <summary>
        /// Creates a copy of AbsoluteCrosspath object, appended with another part.
        /// </summary>
        /// <param name="part">Path to append.</param>
        /// <returns>New AbsoluteCrosspath object, appended with part.</returns>
        public AbsoluteCrosspath Appended(RelativeCrosspath part) {
            return new AbsoluteCrosspath(this).Append(part);
        }

        /// <summary>
        /// Removes last entry if exists and returns self.
        /// This is useful to get containing directory.
        /// </summary>
        /// <returns>Modified self object.</returns>
        public new virtual AbsoluteCrosspath ToContainingDirectory() {
            return base.ToContainingDirectory() as AbsoluteCrosspath;
        }

        /// <summary>
        /// Returns full (absolute) path as a string.
        /// </summary>
        /// <returns></returns>
        public override String ToString() {
            if (directories.Count == 0) {
                return "/";
            }

            StringBuilder sb = new StringBuilder();
            switch (Flavor) {
                case CrosspathFlavor.Windows:
                    sb.Append(WindowsRootDrive);
                    sb.Append(':');
                    break;
                case CrosspathFlavor.Unix:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (String dir in directories) {
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

                sb.Append(dir);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This call is identical to AbsoluteCrosspath.ToString(). It was placed here to support type-agnostic absolute path extraction.
        /// </summary>
        /// <returns></returns>
        public override String ToAbsolutizedString() {
            return ToString();
        }

        /// <summary>
        /// Replaces the beginning of path. Modifies original object and returns it.
        /// </summary>
        /// <param name="oldBase"></param>
        /// <param name="newBase"></param>
        /// <returns></returns>
        public AbsoluteCrosspath Rebase(AbsoluteCrosspath oldBase, AbsoluteCrosspath newBase) {
            if (directories.Count < oldBase.directories.Count) {
                return this;
            }

            Boolean cantRebase = false;
            using (var myIter = directories.GetEnumerator()) {
                using (var theirsIter = oldBase.directories.GetEnumerator()) {
                    for (int idx = 0; idx < oldBase.directories.Count; idx++) {
                        myIter.MoveNext();
                        theirsIter.MoveNext();
                        if (myIter.Current != theirsIter.Current) {
                            cantRebase = true;
                            break;
                        }
                    }
                }
            }

            if (cantRebase) {
                return this;
            }

            // rebasing
            if (directories.Count == oldBase.directories.Count) {
                // full rebase, replacing the whole path
                directories = new LinkedList<String>(newBase.directories);
            }
            else {
                for (int idx = 0; idx < oldBase.directories.Count; idx++) {
                    directories.RemoveFirst();
                }

                LinkedListNode<String> lln = directories.First;

                foreach (String dir in newBase.directories) {
                    directories.AddBefore(lln, dir);
                }
            }

            // rerooting
            if (newBase.Flavor == CrosspathFlavor.Windows) {
                WindowsRootDrive = newBase.WindowsRootDrive;
            }

            Flavor = newBase.Flavor;

            return this;
        }

        public RelativeCrosspath Relativized(AbsoluteCrosspath workingDir, Boolean dontGoOut = false) {
            return RelativeCrosspath.CreateRelativePath(this, workingDir, dontGoOut);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override Boolean Equals(Object obj) {
            if (obj is RelativeCrosspath relativeCrosspath) {
                return relativeCrosspath.EqualsToAbsolute(this);
            }

            if (obj is AbsoluteCrosspath absoluteCrosspath) {
                return this.Equals(absoluteCrosspath);
            }

            return false;
        }

        public bool Equals(AbsoluteCrosspath other) {
            if (other == null) return false;
            return this.ToString() == other.ToString();
        }
    }
}