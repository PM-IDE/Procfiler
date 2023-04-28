#include "cor.h"
#include "corprof.h"
#include "../../util/types.h"
#include "../../logging/ProcfilerLogger.h"
#include <vector>
#include <map>
#include <string>
#include "../../util/util.h"

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
    ThreadID ThreadId;

    explicit EventsWithThreadId(ThreadID threadId) {
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

    static std::vector<FunctionEvent>* GetOrCreatePerThreadEvents(ThreadID threadId);

    std::string myDebugCallStacksSavePath;
    ICorProfilerInfo13* myProfilerInfo;
    ProcfilerLogger* myLogger;

    EVENTPIPE_PROVIDER myEventPipeProvider;
    EVENTPIPE_EVENT myMethodStartEvent;
    EVENTPIPE_EVENT myMethodEndEvent;
    EVENTPIPE_EVENT myMethodInfoEvent;

    HRESULT DefineProcfilerEventPipeProvider();
    HRESULT DefineProcfilerMethodInfoEvent();

    HRESULT DefineProcfilerMethodStartEvent();
    HRESULT DefineProcfilerMethodEndEvent();

    static HRESULT DefineMethodStartOrEndEventInternal(const wstring& eventName,
                                                       EVENTPIPE_PROVIDER provider,
                                                       EVENTPIPE_EVENT* ourEventId,
                                                       ICorProfilerInfo13* profilerInfo,
                                                       UINT32 eventId);
public:
    explicit ShadowStack(ICorProfilerInfo13* profilerInfo, ProcfilerLogger* logger);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, ThreadID threadId, int64_t timestamp);
    void AddFunctionFinished(FunctionID id, ThreadID threadId, int64_t timestamp);
    void DebugWriteToFile();
    void WriteMethodsEventsToEventPipe();
};