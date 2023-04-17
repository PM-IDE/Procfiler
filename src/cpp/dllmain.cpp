#include "ClassFactory.h"
#include <cstdio>


BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    printf("Profiler.dll!DllGetClassObject\n");
    fflush(stdout);
    return TRUE;
}

extern "C" HRESULT STDMETHODCALLTYPE DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv) {
    printf("Profiler.dll!DllGetClassObject\n");
    fflush(stdout);

    auto factory = new ClassFactory();
    return factory->QueryInterface(riid, ppv);
}

extern "C" HRESULT STDMETHODCALLTYPE DllCanUnloadNow() {
    return S_OK;
}

typedef void (*ProfilerCallback) (void);
extern "C" void STDMETHODCALLTYPE PassCallbackToProfiler(ProfilerCallback callback)
{
}

extern "C" void STDMETHODCALLTYPE DoPInvoke(int(*callback)(int), int i)
{
    printf("PInvoke received i=%d\n", callback(i));
}