#include "cor.h"
#include "corprof.h"
#include "../../util/types.h"
#include "../../logging/ProcfilerLogger.h"
#include <vector>
#include <map>
#include <string>
#include "../../util/util.h"
#include "../info/FunctionInfo.h"

enum FunctionEventKind {
    Started,
    Finished
};

struct FunctionEvent {
    FunctionID Id;
    FunctionEventKind EventKind;
    int64_t Timestamp;

    FunctionEvent(FunctionID id, FunctionEventKind eventKind, int64_t timestamp) :
        Id(id),
        EventKind(eventKind),
        Timestamp(timestamp) {
    }
};

struct EventsWithThreadId {
    std::vector<FunctionEvent>* Events;
    DWORD ThreadId;

    explicit EventsWithThreadId(DWORD threadId) {
        Events = new std::vector<FunctionEvent>();
        ThreadId = threadId;
    }

    ~EventsWithThreadId() {
        delete Events;
    }
};

class ShadowStack {
private:
    const UINT32 ourMethodStartEventId = 8000;
    const UINT32 ourMethodEndEventId = 8001;
    const UINT32 ourMethodInfoEventId = 8002;

    const wstring ourMethodStartEventName = ToWString("ProcfilerMethodStart");
    const wstring ourMethodEndEventName = ToWString("ProcfilerMethodEnd");
    const wstring ourMethodInfoEventName = ToWString("ProcfilerMethodInfo");
    const wstring ourEventPipeProviderName = ToWString("ProcfilerCppEventPipeProvider");

    std::map<FunctionID, FunctionInfo> myResolvedFunctions;

    static std::vector<FunctionEvent>* GetOrCreatePerThreadEvents(DWORD threadId);

    std::string myDebugCallStacksSavePath;
    ICorProfilerInfo12* myProfilerInfo;
    ProcfilerLogger* myLogger;

    EVENTPIPE_PROVIDER myEventPipeProvider{};
    EVENTPIPE_EVENT myMethodStartEvent{};
    EVENTPIPE_EVENT myMethodEndEvent{};
    EVENTPIPE_EVENT myMethodInfoEvent{};

    HRESULT InitializeProvidersAndEvents();
    HRESULT DefineProcfilerEventPipeProvider();
    HRESULT DefineProcfilerMethodInfoEvent();

    HRESULT DefineProcfilerMethodStartEvent();
    HRESULT DefineProcfilerMethodEndEvent();

    HRESULT LogFunctionEvent(const FunctionEvent& event, const DWORD& threadId);
    HRESULT LogMethodInfo(const FunctionID& functionId, const FunctionInfo& functionInfo);

    static HRESULT DefineMethodStartOrEndEventInternal(const wstring& eventName,
                                                       EVENTPIPE_PROVIDER provider,
                                                       EVENTPIPE_EVENT* ourEventId,
                                                       ICorProfilerInfo12* profilerInfo,
                                                       UINT32 eventId);
public:
    explicit ShadowStack(ICorProfilerInfo12* profilerInfo, ProcfilerLogger* logger);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp);
    void AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp);
    void DebugWriteToFile();
    void WriteEventsToEventPipe();
};