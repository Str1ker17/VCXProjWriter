using System;
using System.Collections;
using System.Collections.Generic;


namespace VcxProjLib {
    public class PriorityQueue<T> {
        protected LinkedList<Tuple<T, Int32>> elements;

        public PriorityQueue() {
            elements = new LinkedList<Tuple<T, Int32>>();
        }

        public PriorityQueue(IEnumerable<T> source, Int32 defaultPriority = 0) {
            elements = new LinkedList<Tuple<T, Int32>>();
            foreach (T sourceElem in source) {
                elements.AddLast(new Tuple<T, Int32>(sourceElem, defaultPriority));
            }
        }

        // TODO: know more about standard enumerators
        /*
        public bool Enqueue(T obj, int priority) {
            foreach (Tuple<T, Int32> element in elements) {
                if(element.Item2 )
            }
            return false;
        }
        */
    }
}
