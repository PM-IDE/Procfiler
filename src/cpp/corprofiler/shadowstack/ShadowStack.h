#include "cor.h"
#include "corprof.h"
#include <vector>
#include <map>
#include <string>

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
        Timestamp(timestamp) {}
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
    static std::vector<FunctionEvent>* GetOrCreatePerThreadEvents(ThreadID threadId);

    std::string myDebugCallStacksSavePath;
    ICorProfilerInfo13* myProfilerInfo;
public:
    explicit ShadowStack(ICorProfilerInfo13* profilerInfo);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, ThreadID threadId, int64_t timestamp);
    void AddFunctionFinished(FunctionID id, ThreadID threadId, int64_t timestamp);
    void DebugWriteToFile();
};