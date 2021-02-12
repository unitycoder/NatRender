/* 
*   NatRender
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Rendering {

    using AOT;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Readback provider for Metal.
    /// This provider is only supported on iOS when running with Metal.
    /// </summary>
    public sealed class MTLReadback : IReadback {

        #region --Client API--
        /// <summary>
        /// Create a readback provider for Metal.
        /// </summary>
        /// <param name="width">Output pixel buffer width.</param>
        /// <param name="height">Output pixel buffer height.</param>
        /// <param name="multithreading">Use multithreading. Setting `true` will typically increase performance.</param>
        public MTLReadback (int width, int height, bool multithreading = false) {
            this.readback = Create(width, height, multithreading);
            this.frameBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            this.frameBuffer.Create();
            this.readbackTexture = frameBuffer.GetNativeTexturePtr();
            this.bufferSize = width * height * 4;
        }

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        public unsafe void Request (Texture texture, ReadbackDelegate handler) => Request(texture, baseAddress => {
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(baseAddress, bufferSize, Allocator.None);
            handler(nativeArray);
        });

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        public unsafe void Request (Texture texture, NativeReadbackDelegate handler) {
            Graphics.Blit(texture, frameBuffer);
            Request(readback, readbackTexture, OnReadback, (void*)(IntPtr)GCHandle.Alloc(handler, GCHandleType.Normal));
        }

        /// <summary>
        /// Dispose the readback provider.
        /// </summary>
        public async void Dispose () {
            Dispose(readback);
            await Task.Yield();
            frameBuffer.Release();
        }
        #endregion


        #region --Operations--

        private readonly IntPtr readback;
        private readonly RenderTexture frameBuffer;
        private readonly IntPtr readbackTexture;
        private readonly int bufferSize;

        [MonoPInvokeCallback(typeof(ReadbackHandler))]
        private static unsafe void OnReadback (void* context, void* pixelBuffer) {
            var handle = (GCHandle)(IntPtr)context;
            var handler = handle.Target as NativeReadbackDelegate;
            handle.Free();
            handler?.Invoke(pixelBuffer);
        }
        #endregion


        #region --Bridge--
        
        private unsafe delegate void ReadbackHandler (void* context, void* pixelBuffer);

        #if UNITY_IOS //&& !UNITY_EDITOR
        [DllImport(@"__Internal", EntryPoint = @"NRCreateReadback")]
        private static extern IntPtr Create (int width, int height, bool multithreading);
        [DllImport(@"__Internal", EntryPoint = @"NRRequestReadback")]
        private static extern unsafe void Request (IntPtr readback, IntPtr texture, ReadbackHandler completionHandler, void* context);
        [DllImport(@"__Internal", EntryPoint = @"NRDisposeReadback")]
        private static extern void Dispose (IntPtr readback);
        #else
        private static IntPtr Create (int width, int height, bool multithreading) => IntPtr.Zero;
        private static unsafe void Request (IntPtr readback, IntPtr texture, ReadbackHandler completionHandler, void* context) { }
        private static void Dispose (IntPtr readback) { }
        #endif
        #endregion
    }
}