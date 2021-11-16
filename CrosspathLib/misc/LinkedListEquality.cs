using System;
using System.Collections.Generic;

namespace CrosspathLib {
    public class LinkedListEquality {
        public static Boolean Equals<T>(LinkedList<T> a, LinkedList<T> b) {
            using (LinkedList<T>.Enumerator enumeratorA = a.GetEnumerator()) {
                using (LinkedList<T>.Enumerator enumeratorB = b.GetEnumerator()) {
                    while (true) {
                        Boolean thisSuccess = enumeratorA.MoveNext();
                        Boolean otherSuccess = enumeratorB.MoveNext();                        
                        
                        if (thisSuccess == false && otherSuccess == false) {
                            // Both reached the end
                            return true;
                        }

                        if (thisSuccess == false || otherSuccess == false) {
                            // Only one reached the end
                            return false;
                        }

                        if (enumeratorA.Current == null || enumeratorB.Current == null) {
                            throw new NullReferenceException("enumerator is null");
                        }

                        if (!enumeratorA.Current.Equals(enumeratorB.Current)) {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
