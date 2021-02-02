/* 
*   NatRender
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Rendering {

    using System;
    using UnityEngine;
    using Unity.Collections;

    /// <summary>
    /// Readback provider for fetching pixel data from a 2D texture on the GPU.
    /// </summary>
    public interface IReadback : IDisposable {
        
        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        void Request<T> (Texture texture, Action<NativeArray<T>> handler) where T : unmanaged;

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        void Request (Texture texture, Action<IntPtr> handler);
    }
}