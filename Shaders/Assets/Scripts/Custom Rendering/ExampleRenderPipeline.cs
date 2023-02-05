using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
//Very useful article: http://xdpixel.com/native-rendering-plugin-in-unity
/*Direct3D stuff: 
 * https://learn.microsoft.com/en-us/windows/win32/direct3d11/d3d11-graphics-reference-d3d11-shader-interfaces 
 
 */

public class ExampleRenderPipeline : RenderPipeline
{
    public ExampleRenderPipeline() 
    {

    }
    public const string pluginName = "NativePlugin";

    /*The extern modifier is used to declare a method that is implemented externally. A common use of the extern modifier is with the DllImport attribute when you are using Interop services to call into unmanaged code. In this case, the method must also be declared as static.
     More succinctly, this header tells the compiler that the below function is
    coming from the specified dll.
    
    /*Marshalling is the process of transforming a representation of an object (ie, JSON) into a data format (like a class). IntPtr is a marshalling type for a pointer. This getEventFunction is called from our c++ code
      every iteration of the unity render loop.
     */
    [DllImport(pluginName)]
    private static extern IntPtr getEventFunction();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DebugDelegate(string str);

    /*This is going to be called by our cpp code. Whatever string that cpp
     code gives us this function will print.*/
    static void CallBackFunction(string str)
    {
        Debug.Log(str);
    }

    [DllImport(pluginName)]
    public static extern void SetDebugFunction(IntPtr functionPointer);
    
    /*CommandBuffers hold lists of rendering commands (set render target, 
     draw mesh). They can be used to execute at various points during
    camera rendering (Camera.AddCommandBuffer), light rendering (Light.
    AddCommandBuffer) or be executed immediately (Graphics.ExecuteCommandBuffer)*/
    private CommandBuffer commandBuffer;

    /*In the Scriptable Render Pipeline (SRP), the ScriptableRenderContext class acts an an interface butween the C# render pipeline and 
     Unity's low level graphics code.
    SRP rendering works using DELAYED EXECUTION; you can use the 
    ScriptableRenderContext to build up a list of rendering commands
    and then tell unity to execute them. Unity's low level graphics architecture will then send graphics instructions to the graphics
    API.*/

    /*The Render command is called once per frame*/
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        
    }

    protected void Render2(ScriptableRenderContext context, Camera[] cameras)
    {
        DebugDelegate callbackDelegate = new DebugDelegate(CallBackFunction);
        IntPtr intPtrDelegate = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
        commandBuffer = new CommandBuffer();
        commandBuffer.name = pluginName;
        
        /*This will return our getEventFunction, defined in our cpp code.*/
        commandBuffer.IssuePluginEvent(getEventFunction(), 0);
        context.Submit();

    }
}
