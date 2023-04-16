#include "cor.h"
#include "corprof.h"
#include "pal_mstypes.h"

class ProcfilerCorProfilerCallback : public ICorProfilerCallback11 {
private:
    ICorProfilerInfo11* myProfilerInfo;


public:
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
};