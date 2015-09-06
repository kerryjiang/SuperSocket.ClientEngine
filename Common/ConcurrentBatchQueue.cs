using System;
using System.Collections.Generic;
using System.Threading;

namespace SuperSocket.ClientEngine
{
    /// <summary>
    /// Concurrent BatchQueue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentBatchQueue<T> : IBatchQueue<T>
    {
        private Entity m_Entity;
        private Entity m_BackEntity;

        private static readonly T m_Null = default(T);

        private Func<T, bool> m_NullValidator;

        class Entity
        {
            public T[] Array { get; set; }
            public int[] ArrayElementPresent { get; set; } // used to indicate that copy of Array's element has finished
            // Often Array elements have been Dequeued being not fully Enqueued
            // e.g. T = ArraySegment<byte>, and Array[i].Count == 0 (enqueued > 0)
            // as a result e.BytesTransferred == 0 and disconnections in AsyncTcpSession.Sending_Completed
            public int Count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBatchQueue&lt;T&gt;"/> class.
        /// </summary>
        public ConcurrentBatchQueue()
            : this(16)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBatchQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the queue.</param>
        public ConcurrentBatchQueue(int capacity)
            : this(new T[capacity])
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBatchQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="nullValidator">The null validator.</param>
        public ConcurrentBatchQueue(int capacity, Func<T, bool> nullValidator)
            : this(new T[capacity], nullValidator)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBatchQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="array">The array.</param>
        public ConcurrentBatchQueue(T[] array)
            : this(array, (t) => t == null)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBatchQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="nullValidator">The null validator.</param>
        public ConcurrentBatchQueue(T[] array, Func<T, bool> nullValidator)
        {
            var length = array.Length;
            m_Entity = new Entity
            {
                Array = array,
                ArrayElementPresent = new int[length]
            };
            for (int i = 0; i < length; ++i) m_Entity.ArrayElementPresent[i] = nullValidator(array[i]) ? 0  :1;

            m_BackEntity = new Entity
            {
                Array = new T[length],
                ArrayElementPresent = new int[length]
            };
            for (int i = 0; i < length; ++i) m_BackEntity.ArrayElementPresent[i] = 0;

            m_NullValidator = nullValidator;
        }

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public bool Enqueue(T item)
        {
            bool full;

            while (true)
            {
                if (TryEnqueue(item, out full) || full)
                    break;
            }

            return !full;
        }

        private bool TryEnqueue(T item, out bool full)
        {
            full = false;

            var entity = m_Entity;
            var array = entity.Array;
            var arrayElement = entity.ArrayElementPresent;
            var count = entity.Count;

            if (count == array.Length)
            {
                full = true;
                return false;
            }

            if (entity != m_Entity)
                return false;

            int oldCount = Interlocked.CompareExchange(ref entity.Count, count + 1, count);

            if (oldCount != count)
                return false;

            array[count] = item; // not atomic
            Interlocked.Exchange(ref arrayElement[count], 1);

            return true;
        }

        /// <summary>
        /// Enqueues the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        public bool Enqueue(IList<T> items)
        {
            bool full;

            while (true)
            {
                if (TryEnqueue(items, out full) || full)
                    break;
            }

            return !full;
        }

        private bool TryEnqueue(IList<T> items, out bool full)
        {
            full = false;

            var entity = m_Entity;
            var array = entity.Array;
            var arrayElement = entity.ArrayElementPresent;
            var count = entity.Count;

            int newItemCount = items.Count;
            int expectedCount = count + newItemCount;

            if (expectedCount > array.Length)
            {
                full = true;
                return false;
            }

            if (entity != m_Entity)
                return false;

            int oldCount = Interlocked.CompareExchange(ref entity.Count, expectedCount, count);

            if (oldCount != count)
                return false;

            foreach (var item in items)
            {
                array[count] = item;
                Interlocked.Exchange(ref arrayElement[count++], 1);
            }

            return true;
        }

        /// <summary>
        /// Tries the dequeue.
        /// </summary>
        /// <param name="outputItems">The output items.</param>
        /// <returns></returns>
        public bool TryDequeue(IList<T> outputItems)
        {
            var entity = m_Entity;
            var spinWait = new SpinWait();

            while (ReferenceEquals(entity, m_BackEntity)) // other Thread is in TryDequeue already coz m_Entity==m_BackEntity
            {
                spinWait.SpinOnce();
                entity = m_Entity;
            }

            if (!ReferenceEquals(Interlocked.CompareExchange(ref m_Entity, m_BackEntity, entity), entity))
                return false; // m_Entity set to m_BackEntity already

            int count;
            int oldCount = entity.Count;
            do
            {
                spinWait.SpinOnce();
                count = oldCount;
                oldCount = Interlocked.CompareExchange(ref entity.Count, int.MaxValue, count); // outstanding entity enqueuers should not be able to enqueue
            } while (oldCount != count); // wait for Enqueue to complete

            if (count <= 0)
            {
                entity.Count = 0;
                m_BackEntity = entity; // make m_BackEntity available again, m_BackEntity.Count == 0 already
                return false;
            }

            var array = entity.Array;
            var arrayElement = entity.ArrayElementPresent;

            var i = 0;

            while (true)
            {
                var item = array[i]; //not atomic

                while (m_NullValidator(item)/*can see not fully copied item*/ 
                    || arrayElement[i] == 0 /*ensure array[i] is ready*/)
                {
                    spinWait.SpinOnce();
                    item = array[i]; //not atomic
                }

                outputItems.Add(item);
                array[i] = m_Null;
                Interlocked.Exchange(ref arrayElement[i], 0);

                if (count <= (i + 1))
                    break;

                i++;
            }

            entity.Count = 0;
            m_BackEntity = entity; // make m_BackEntity available again, m_BackEntity.Count == 0 already

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return m_Entity.Count <= 0; }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get { return m_Entity.Count; }
        }
    }
}
