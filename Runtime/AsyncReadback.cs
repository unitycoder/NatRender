/* 
*   NatRender
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Rendering {

    using System;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Readback provider that uses Unity's AsyncGPUReadback API.
    /// This provider is only supported when `SystemInfo.supportsAsyncGPUReadback`.
    /// </summary>
    public sealed class AsyncReadback : IReadback {

        #region --Client API--
        /// <summary>
        /// Create an async readback provider
        /// </summary>
        public AsyncReadback () { } // Should we throw exception in ctor?

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        public void Request (Texture texture, ReadbackDelegate handler) => AsyncGPUReadback.Request(texture, 0, request => handler(request.GetData<byte>()));

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        public unsafe void Request (Texture texture, NativeReadbackDelegate handler) => Request(texture, buffer => handler(NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(buffer)));

        /// <summary>
        /// Dispose the readback provider.
        /// </summary>
        public void Dispose () { }
        #endregion
    }
}