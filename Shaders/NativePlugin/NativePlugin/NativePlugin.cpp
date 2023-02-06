//This is mostly boilerplate code from here: https://docs.unity3d.com/Manual/NativePluginInterface.html
//And also lots of help from here: http://xdpixel.com/native-rendering-plugin-in-unity/
#include "IUnityGraphics.h" //unity interface
#include "d3d11.h" //DirectX version we need
#include "IUnityGraphicsD3D11.h" //Specific unity graphics interface


static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;
static UnityGfxRenderer s_RenderType = kUnityGfxRendererNull;

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType type);
static void DoEventGraphicsDeviceD3D11(UnityGfxDeviceEventType type);
static void Render(IUnityInterfaces*interfaces, int eventid, int* data);
static void Initialise(IUnityInterfaces* interfaces, int eventid, int* data);
static bool initialised = false;

extern "C" //Any functions we want exposed to unity should be put in this extern "c" block
{
	//Below function is run when this plugin is loaded
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces) 
	{
		s_UnityInterfaces = interfaces;
		s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
		s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
		//Run OnGraphicsDeviceEvent(Initialize) manually on PluginLoad to not miss the event incase the device is already initialised
		OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
	}
	//Below function is run when this plugin is unloaded.
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() 
	{
		s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnRenderEvent(int eventId, int* data) 
	{
		if (!initialised) 
		{
			Initialise(s_UnityInterfaces, eventId, data);
			initialised = true;
		}
		Render(s_UnityInterfaces, eventId, data);
	}
	//We call this method when the IssuePluginAndEventData method is called from c#
	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunction() 
	{
		return UnityRenderingEvent(OnRenderEvent); 
	}
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetupResources(void* indexBuffer, void* vertexBuffer, void* texture) 
	{

	}
}

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType type) 
{
	UnityGfxRenderer currentDeviceType = s_RenderType;

	switch (type) 
	{
		case kUnityGfxDeviceEventInitialize:
			//initialisation code
			break;
		case kUnityGfxDeviceEventShutdown:
			//shutdown code
			break;
		case kUnityGfxDeviceEventBeforeReset:
			break;
		case kUnityGfxDeviceEventAfterReset:
			break;
	}
}
static void Render(IUnityInterfaces* interfaces, int eventId, int* data) 
{
	
}
////type alias - a FuncPtr is just a function that takes in a const char*.
//typedef void(*FuncPtr) (const char*);
//FuncPtr Debug;
//
///*The "extern" keyword has a number of different functionalities 
//depending on the context. Here, extern "C" specifies that the functions
//we are calling are both defined elsewhere and should be called using
//the C-language calling convention.
//A calling convention is a low-level scheme for how functions receive
//parameters from their caller, and how they return a result to their 
//caller.*/
//
///*UNITY_INTERFACE_EXPORT and UNITY_INTERFACE_API are both macros defined in the
//IUnityInterface.h file, which is an include in the IUnityGraphics.h file.
//UNITY_INTERFACE_EXPORT = _declspec(dllexport) (Allows us to export data, functions, classes and
//class members from the dll)
//UNITY_INTERFACE_API = _stdcall (defines how calls to the function will be made)
//*/
//static IUnityInterfaces* unityInterfaces = NULL;
//static IUnityGraphics* graphics = NULL;
//static UnityGfxRenderer rendererType = kUnityGfxRendererNull;
//
//namespace globals {
//	ID3D11Device* device = nullptr;
//	ID3D11DeviceContext* context = nullptr;
//}
//extern "C" {
//	UNITY_INTERFACE_EXPORT void SetDebugFunction(FuncPtr fp)
//	{
//		Debug = fp;
//	}
//	static void UNITY_INTERFACE_API OnRenderEvent(int eventId)
//	{
//		Debug("Hello world");
//	}
//
//	/*To handle main unity events, a plugin must export UnityPluginLoad and
//	UnityPluginUnload. These are both callback functions
//	which are called when the plugin is loaded/unloaded.*/
//
//	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
//	{
//		//auto is the same as "var" in c#
//		auto m_unityInterfaces = unityInterfaces;
//		IUnityGraphicsD3D11* d3d11 = m_unityInterfaces->Get<IUnityGraphicsD3D11>();
//		globals::device = d3d11->GetDevice();
//		globals::device->GetImmediateContext(&globals::context);
//	}
//	/*This function returns a UnityRenderingEvent. A UnityRenderingEvent is simply a void
//	function that takes a single integer parameter.*/
//	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API getEventFunction()
//	{
//		return OnRenderEvent;
//	}
//}