using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GameLoop.Networking.Collections
{
    internal struct ConcurrentPoolEntry<T>
    {
        public T Entry;
        public bool IsSet;
    }
    
    public class ConcurrentPool<T>
    {
        private readonly ConcurrentPoolEntry<T>[] _pool;
        private int _enqueuePointer;
        private int _dequeuePointer;
        
        public ConcurrentPool(int size)
        {
            if(size < 1) throw new ArgumentException("Size cannot be less than 1.", nameof(size));
            _pool = new ConcurrentPoolEntry<T>[size];
            _enqueuePointer = 0;
            _dequeuePointer = 0;
        }

        public void Enqueue(T entry)
        {
            var pointer = Interlocked.Increment(ref _enqueuePointer);

            if (!_pool[pointer].IsSet)
            {
                _pool[pointer].Entry = entry;
                _pool[pointer].IsSet = true;
            }
            else
            {
                throw new Exception("This element is already set.");
            }
        }
    }
}