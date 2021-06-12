// Pool to avoid allocations
using System;
using System.Collections.Generic;

namespace libuv2k
{
    public class Pool<T>
    {
        // libuv uses an event loop. simple stack is fine, no need for thread
        // safe collections.
        readonly Stack<T> objects = new Stack<T>();

        // some types might need additional parameters in their constructor, so
        // we use a Func<T> generator
        readonly Func<T> objectGenerator;

        // some types might require special cleanup, like a .Dispose() call to
        // clean up pinned memory.
        readonly Action<T> objectDisposer;

        public Pool(Func<T> objectGenerator, Action<T> objectDisposer)
        {
            this.objectGenerator = objectGenerator;
            this.objectDisposer = objectDisposer;
        }

        // take an element from the pool, or create a new one if empty
        public T Take() => objects.Count > 0 ? objects.Pop() : objectGenerator();

        // return an element to the pool
        public void Return(T item) => objects.Push(item);

        // clear the pool with the disposer function applied to each object
        public void Clear()
        {
            foreach (T obj in objects)
            {
                objectDisposer(obj);
            }
            objects.Clear();
        }

        // count to see how many objects are in the pool. useful for tests.
        public int Count => objects.Count;
    }
}