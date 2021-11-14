using System;
using System.Collections.Generic;
using CrosspathLib;

namespace VcxProjLib {
    public class NTree<T> {
        protected T value;
        protected NTree<T> parent;
        protected SortedDictionary<T, NTree<T>> children;

        public NTree(T value) {
            this.value = value;
            parent = null;
            children = new SortedDictionary<T, NTree<T>>();
        }

        public NTree(NTree<T> parent, T value) : this(value) {
            this.parent = parent;
        }
    }

#if FALSE
    abstract class FileSystem : NTree<String> {
        protected CrosspathFlavor flavor;

        public Boolean AddEntry(AbsoluteCrosspath path) {
            NTree<String> fs = (NTree<String>)this;
            foreach (String pathEntry in path) {
                if (fs.children[pathEntry] == null) {
                    fs.children[pathEntry] = new NTree<String>(fs, pathEntry);
                }
                else {
                    fs = fs.children[pathEntry];
                }
            }

            return false;
        }

        protected FileSystem() : base("") {
        }
    }
#endif
}