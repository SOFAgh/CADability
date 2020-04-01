using System;
using System.Collections;

namespace CADability
{
    /// <summary>
    /// Ein Set im Sinne von STL. Implementiert als HashTable nur mit key, ohne Value
    /// </summary>
    internal class UntypedSet : IEnumerable, ICollection
    {
        private Hashtable hashtable;
        private class Enumerate : IEnumerator
        {
            private IDictionaryEnumerator dicionaryEnumerator;
            public Enumerate(Hashtable hashtable)
            {
                dicionaryEnumerator = hashtable.GetEnumerator();
            }
            #region IEnumerator Members

            public void Reset()
            {
                dicionaryEnumerator.Reset();
            }

            public object Current
            {
                get
                {
                    DictionaryEntry e = (DictionaryEntry)dicionaryEnumerator.Current;
                    return e.Key;
                }
            }

            public bool MoveNext()
            {
                return dicionaryEnumerator.MoveNext();
            }

            #endregion
        }
        public UntypedSet()
        {
            hashtable = new Hashtable();
        }
        private UntypedSet(Hashtable ht)
        {
            hashtable = ht;
        }
        public void Add(object ToAdd)
        {
            hashtable.Add(ToAdd, null);
        }
        public void Remove(object ToRemove)
        {
            hashtable.Remove(ToRemove);
        }
        public bool Contains(object ToTest)
        {
            return hashtable.ContainsKey(ToTest);
        }
        public void Clear()
        {
            hashtable.Clear();
        }
        public UntypedSet Clone()
        {
            return new UntypedSet((Hashtable)hashtable.Clone());
        }
        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new Enumerate(hashtable);
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get
            {
                return hashtable.IsSynchronized;
            }
        }

        public int Count
        {
            get
            {
                return hashtable.Count;
            }
        }

        public void CopyTo(Array array, int index)
        {
            hashtable.CopyTo(array, index);
        }

        public object SyncRoot
        {
            get
            {
                return hashtable.SyncRoot;
            }
        }

        #endregion
    }
}
