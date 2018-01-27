﻿using System;
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
        private object m_Entity;
        private Entity m_BackEntity;

        private static readonly T m_Null = default(T);

        private Func<T, bool> m_NullValidator;

        private class Entity
        {
            public T[] Array { get; set; }

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
            var entity = new Entity();
            entity.Array = array;
            m_Entity = entity;

            m_BackEntity = new Entity();
            m_BackEntity.Array = new T[array.Length];

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

            EnsureNotRebuild();

            var entity = m_Entity as Entity;
            var array = entity.Array;
            var count = entity.Count;

            if (count >= array.Length)
            {
                full = true;
                return false;
            }

            if (entity != m_Entity)
                return false;

            int oldCount = Interlocked.CompareExchange(ref entity.Count, count + 1, count);

            if (oldCount != count)
                return false;

            array[count] = item;

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

            var entity = m_Entity as Entity;
            var array = entity.Array;
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
                array[count++] = item;
            }

            return true;
        }

        private void EnsureNotRebuild()
        {
            if (!m_Rebuilding)
                return;

            while (true)
            {
                Thread.SpinWait(1);

                if (!m_Rebuilding)
                    break;
            }
        }

        private bool m_Rebuilding = false;

        /// <summary>
        /// Tries the dequeue.
        /// </summary>
        /// <param name="outputItems">The output items.</param>
        /// <returns></returns>
        public bool TryDequeue(IList<T> outputItems)
        {
            var entity = m_Entity as Entity;
            int count = entity.Count;

            if (count <= 0)
                return false;

            var oldEntity = Interlocked.CompareExchange(ref m_Entity, m_BackEntity, entity);

            if (!ReferenceEquals(oldEntity, entity))
                return false;

            Thread.SpinWait(1);

            count = entity.Count;

            var array = entity.Array;

            var i = 0;

            while (true)
            {
                var item = array[i];

                while (m_NullValidator(item))
                {
                    Thread.SpinWait(1);
                    item = array[i];
                }

                outputItems.Add(array[i]);
                array[i] = m_Null;

                if (entity.Count <= (i + 1))
                    break;

                i++;
            }

            entity.Count = 0;
            m_BackEntity = entity;

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
            get { return Count <= 0; }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get { return (m_Entity as Entity).Count; }
        }
    }
}