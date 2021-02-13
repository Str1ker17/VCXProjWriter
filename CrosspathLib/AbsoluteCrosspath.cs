using System;
using System.Collections.Generic;
using System.Text;

namespace CrosspathLib {
    public class AbsoluteCrosspath : Crosspath {
        internal static AbsoluteCrosspath CreateInstance() {
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
        /// <param name="source"></param>
        public AbsoluteCrosspath(AbsoluteCrosspath source) : base(source) {
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

            if (directories.Count == 0) {
            }

            Boolean cantRebase = false;
            var myIter = directories.GetEnumerator();
            var theirsIter = oldBase.directories.GetEnumerator();
            for (int idx = 0; idx < oldBase.directories.Count; idx++) {
                myIter.MoveNext();
                theirsIter.MoveNext();
                if (myIter.Current != theirsIter.Current) {
                    cantRebase = true;
                    break;
                }
            }

            myIter.Dispose();
            theirsIter.Dispose();

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
    }
}