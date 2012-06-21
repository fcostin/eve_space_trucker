using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{

    //priority queue, sorted by decreasing K.
    public class PriorityQueue<K, V> where K : IComparable<K>
    {
        public class Pair
        {
            public K key;
            public V value;

            public Pair(K key, V value)
            {
                this.key = key;
                this.value = value;
            }

            public override string ToString()
            {
                return "(" + key.ToString() + "," + value.ToString() + ")";
            }
        }

        private const int HEAP_SIZE = 1024;

        private List<Pair> list;

        public PriorityQueue()
        {
            list = new List<PriorityQueue<K,V>.Pair>();
            //pad out the first entry in the list so we have 1-based indexing
            list.Add(null);
        }

        private int left(int i)
        {
            return i * 2;
        }

        private int right(int i)
        {
            return i * 2 + 1;
        }

        private int parent(int i)
        {
            return i / 2;
        }

        private void swap(int i, int j)
        {
            Pair temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        private void heapifyDownward(int i)
        {
            int largest = i, l = left(i), r = right(i);
            if (l < list.Count && (list[l] != null) && (list[l].key.CompareTo(list[i].key)>0))
            {
                largest = l;
            }

            if (r < list.Count && (list[r] != null) && (list[r].key.CompareTo(list[largest].key) > 0))
            {
                largest = r;
            }

            if (largest != i)
            {
                swap(largest, i);
                heapifyDownward(largest);
            }
        }

        private void heapifyUpward(int i)
        {
            int p = parent(i);
            //if the element at index i is greater than its parent, push it upwards through the tree
            //pre: we have a tree, so parents are non-null (apart from the root node, as list[0]=null)
            while (i > 1 && list[p].key.CompareTo(list[i].key)<0)
            {
                swap(i, p);
                i = p;
                p = parent(i);
            }
        }

        public void push(K key, V value)
        {
            //add new pair to the end of the list
            Pair pair = new PriorityQueue<K,V>.Pair(key,value);
            list.Add(pair);
            //push it upwards according to key value
            heapifyUpward(list.Count - 1);
        }

        public Pair pop()
        {
            if (list.Count > 1)
            {
                //return the root pair
                Pair head = list[1];
                //move lowest priority element to head of queue, shrink queue length by 1
                swap(1, list.Count - 1);
                list.RemoveAt(list.Count - 1);
                //push low priority element back down through queue
                heapifyDownward(1);

                return head;
            }
            else
            {
                return null;
            }
        }

        public int count()
        {
            return list.Count - 1;
        }
    }
}
