﻿using System.Collections.Generic;

namespace SuperSocket.ClientEngine
{
    /// <summary>
    /// The generic list interface with position
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPosList<T> : IList<T>
    {
        /// <summary>
        /// Gets or sets the position of current item.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        int Position { get; set; }
    }

    /// <summary>
    /// The generic list with position
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PosList<T> : List<T>, IPosList<T>
    {
        /// <summary>
        /// Gets or sets the position of current item.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public int Position { get; set; }
    }
}