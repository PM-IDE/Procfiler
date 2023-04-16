#include "ProcfilerCorProfilerCallback.h"

HRESULT ProcfilerCorProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnk) {
    printf("Started initializing CorProfiler callback");
    REFIID uuid = __uuidof(ICorProfilerInfo11);
    void** ptr = reinterpret_cast<void**>(&this->myProfilerInfo);
    HRESULT result = pICorProfilerInfoUnk->QueryInterface(uuid, ptr);
    if (FAILED(result)) {
        return E_FAIL;
    }

    DWORD eventMask = COR_PRF_MONITOR_JIT_COMPILATION |
                      COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                      COR_PRF_DISABLE_INLINING |
                      COR_PRF_MONITOR_MODULE_LOADS |
                      COR_PRF_DISABLE_ALL_NGEN_IMAGES;

    result = myProfilerInfo->SetEventMask(eventMask);
    if (FAILED(result)) {
        return E_FAIL;
    }

    printf("Initialized CorProfiler callback");
    return S_OK;
}

HRESULT ProcfilerCorProfilerCallback::Shutdown() {
    if (myProfilerInfo != nullptr) {
        myProfilerInfo->Release();
        myProfilerInfo = nullptr;
    }

    return S_OK;
}
