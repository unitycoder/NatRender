/* 
*   NatRender
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Rendering {

    using AOT;
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Interface with Unity's primary render thread.
    /// </summary>
    public static class RenderThread {

        #region --Client API--
        /// <summary>
        /// Run a delegate on the render thread.
        /// </summary>
        /// <param name="action">Delegate to run.</param>
        public static void Run (Action action) {
            var commandBuffer = new CommandBuffer();
            commandBuffer.IssuePluginEventAndData(CallbackPtr, 0, (IntPtr)GCHandle.Alloc(action, GCHandleType.Normal));
            Graphics.ExecuteCommandBuffer(commandBuffer);
        }
        #endregion


        #region --Operations--

        private static readonly IntPtr CallbackPtr;

        static RenderThread () => CallbackPtr = Marshal.GetFunctionPointerForDelegate<UnityRenderingEventAndData>(OnRenderThreadInvoke);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UnityRenderingEventAndData (int _, IntPtr data);

        [MonoPInvokeCallback(typeof(UnityRenderingEventAndData))]
        private static void OnRenderThreadInvoke (int _, IntPtr context) {
            var handle = (GCHandle)context;
            var action = handle.Target as Action;
            handle.Free();
            action?.Invoke();
        }
        #endregion
    }
}