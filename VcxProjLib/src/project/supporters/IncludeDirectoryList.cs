using System;
using System.Collections;
using System.Collections.Generic;

namespace VcxProjLib {
    public class IncludeDirectoryList : IEnumerable<IncludeDirectory> {
        protected List<IncludeDirectory>[] includeDirectoryArray;

        public IncludeDirectoryList() {
            includeDirectoryArray = new List<IncludeDirectory>[4];
            for (int index = 0; index < includeDirectoryArray.Length; index++) {
                includeDirectoryArray[index] = new List<IncludeDirectory>();
            }
        }

        public void AddIncludeDirectory(IncludeDirectory includeDir) {
            int index;
            switch (includeDir.Type) {
                case IncludeDirectoryType.Quote: index = 0; break;
                case IncludeDirectoryType.Generic: index = 1; break;
                case IncludeDirectoryType.System: index = 2; break;
                case IncludeDirectoryType.DirAfter: index = 3; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (   includeDirectoryArray[index].Count > 0
                && includeDirectoryArray[index][includeDirectoryArray[index].Count - 1].ToString() == includeDir.ToString()) {
                return;
            }

            includeDirectoryArray[index].Add(includeDir);
        }

        public IEnumerator<IncludeDirectory> GetEnumerator() {
            // watch out the order in AddIncludeDirectory()!
            foreach (List<IncludeDirectory> includeDirectories in includeDirectoryArray) {
                foreach (IncludeDirectory includeDirectory in includeDirectories) {
                    yield return includeDirectory;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public override String ToString() {
            int totalDirs = 0;
            foreach (List<IncludeDirectory> includeDirectories in includeDirectoryArray) {
                totalDirs += includeDirectories.Count;
            }
            return $"Total: {totalDirs}";
        }

        public Boolean ListIdentical(IncludeDirectoryList other) {
            using (IEnumerator<IncludeDirectory> enumeratorThis = this.GetEnumerator()) {
                using (IEnumerator<IncludeDirectory> enumeratorOther = other.GetEnumerator()) {

                    while (true) {
                        Boolean thisSuccess = enumeratorThis.MoveNext();
                        Boolean otherSuccess = enumeratorOther.MoveNext();
                        if (thisSuccess == false && otherSuccess == false) {
                            return true;
                        }

                        if (thisSuccess == false || otherSuccess == false) {
                            return false;
                        }

                        if (enumeratorThis.Current != enumeratorOther.Current) {
                            return false;
                        }
                    }
                }
            }
        }
    }
}