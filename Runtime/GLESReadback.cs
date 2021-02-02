/* 
*   NatRender
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Rendering {

    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Readback provider for OpenGL ES.
    /// This provider is only supported on Android when running with OpenGL ES3.
    /// </summary>
    public sealed class GLESReadback : AndroidJavaProxy, IReadback {

        #region --Client API--
        /// <summary>
        /// Create a readback provider for OpenGL ES.
        /// </summary>
        /// <param name="width">Output pixel buffer width.</param>
        /// <param name="height">Output pixel buffer height.</param>
        /// <param name="multithreading">Use multithreading. Setting `true` will typically increase performance.</param>
        public GLESReadback (int width, int height, bool multithreading = false) : base(@"api.natsuite.natrender.Readback$Callback") {
            // Get EGL context
            var egl = new AndroidJavaClass(@"android.opengl.EGL14");
            var contextTask = new TaskCompletionSource<AndroidJavaObject>();
            RenderThread.Run(() => {
                AndroidJNI.AttachCurrentThread();
                var context = egl.CallStatic<AndroidJavaObject>(@"eglGetCurrentContext");
                contextTask.SetResult(context);
            });
            var eglContext = contextTask.Task.Result;
            // Create readback
            var className = @"api.natsuite.natrender.GLReadback";
            this.readback = new AndroidJavaObject(className, width, height, eglContext, this, multithreading);
            this.clazz = new AndroidJavaClass(className);
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
        public unsafe void Request<T> (Texture texture, Action<NativeArray<T>> handler) where T : unmanaged => Request(texture, baseAddress => {
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(baseAddress.ToPointer(), bufferSize / Marshal.SizeOf<T>(), Allocator.None);
            handler(nativeArray);
        });

        /// <summary>
        /// Request a readback.
        /// </summary>
        /// <param name="texture">Input texture.</param>
        /// <param name="handler">Readback handler.</param>
        public void Request (Texture texture, Action<IntPtr> handler) {
            Graphics.Blit(texture, frameBuffer);
            readback.Call(@"readback", readbackTexture.ToInt32(), ((IntPtr)GCHandle.Alloc(handler, GCHandleType.Normal)).ToInt64());
        }

        /// <summary>
        /// Dispose the readback provider.
        /// </summary>
        public async void Dispose () {
            readback.Call(@"release");
            await Task.Yield();
            frameBuffer.Release();
        }
        #endregion


        #region --Operations--

        private readonly AndroidJavaObject readback;
        private readonly AndroidJavaClass clazz;
        private readonly RenderTexture frameBuffer;
        private readonly IntPtr readbackTexture;
        private readonly int bufferSize;
        
        [Preserve]
        private void onReadback (long context, AndroidJavaObject pixelBuffer) {
            var handle = (GCHandle)(IntPtr)context;
            var handler = handle.Target as Action<IntPtr>;
            handle.Free();
            var baseAddress = (IntPtr)clazz.CallStatic<long>(@"baseAddress", pixelBuffer);
            handler?.Invoke(baseAddress);
        }
        #endregion
    }
}