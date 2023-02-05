using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderingPluginExample : MonoBehaviour
{

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct RenderData 
    {
        public const int size = 48 * sizeof(float);
        /*The MarshalAs attribute indicates how to marshal data between managed
         and unmanaged code.*/
        [MarshalAs(UnmanagedType.ByValArray)]
        public float[] localToWorldMatrix;
        [MarshalAs(UnmanagedType.ByValArray)]
        public float[] worldToViewMatrix;
        [MarshalAs(UnmanagedType.ByValArray)]
        public float[] viewToProjectionMatrix;
    };
    public MeshFilter mf;
    public Texture2D texture;
    public Transform WorldToObjectT;
    public const string pluginName = "NativePlugin";

    //SetupResources is a function in our native rendering plugin.
    [DllImport(pluginName)]
    private static extern void SetUpResources(IntPtr indexBuffer, IntPtr vertBuffer, IntPtr texPointer);

    /*The GetRenderEventFunction returns a native function pointer to our DoRenderEvent method, defined in our
     native plugin c++ code. This will initialise our native graphics state objects and handle the rendering of
    the mesh*/
    [DllImport(pluginName)]
    private static extern IntPtr GetRenderEventFunction();

    //Command buffers hold lists of rendering commands.
    CommandBuffer commandBuffer;
    RenderData data;
    IntPtr dataPtr;

    private void Awake()
    {
        Mesh mesh = new Mesh();
        mf.mesh = mesh;


        IntPtr indexBufferPtr = mesh.GetNativeIndexBufferPtr();
        IntPtr vertexBufferPtr = mesh.GetNativeVertexBufferPtr(0);
        IntPtr textureBufferPtr = texture.GetNativeTexturePtr();

        //pass the pointers to the native plugin
        SetUpResources(indexBufferPtr, vertexBufferPtr, textureBufferPtr);

        data = new RenderData();
        data.localToWorldMatrix = new float[16];
        data.worldToViewMatrix = new float[16];
        data.viewToProjectionMatrix = new float[16];
        commandBuffer = new CommandBuffer();
    }

    //Unity executes a beginCameraEvent before it renders each active camera in every frame
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += PreRenderSetup;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= PreRenderSetup;
    }

    void PreRenderSetup(ScriptableRenderContext context, Camera cam) 
    {
        //We update the data in the camera's command buffer every frame.
        Camera.main.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
        commandBuffer.Release();

        /*We don't pass the camera's projection matrix directly into the plugin; we use
         the below function to make it suitable for our particular graphics library*/
        Matrix4x4 p = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);

        data.localToWorldMatrix = MatrixToFloatArray(WorldToObjectT.localToWorldMatrix);
        data.worldToViewMatrix = MatrixToFloatArray(Camera.main.worldToCameraMatrix);
        data.viewToProjectionMatrix = MatrixToFloatArray(p);

       
        commandBuffer = new CommandBuffer();
        commandBuffer.name = pluginName;
        /*Copy our managed data into the unmanaged memory, as pointed to by the NativeDataRenderingPtr*/
        Marshal.StructureToPtr(data, dataPtr, true);


        commandBuffer.IssuePluginEventAndData(GetRenderEventFunction(), 0, dataPtr);
        Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
    }

    private float[] MatrixToFloatArray(Matrix4x4 input) 
    {
        float[] arr = new float[16];
        for (int i = 0; i < 4; i++) 
        {
            float4 row = input.GetRow(i);
            for (int j = 0; j < 4; j++) 
            {
                arr[j] = row[j];
            }
        }
        return arr;
    }
    private void OnDestroy()
    {
        // Perform cleanup
        commandBuffer.Release();
        Marshal.FreeHGlobal(dataPtr);
        dataPtr = IntPtr.Zero;
    }
}
