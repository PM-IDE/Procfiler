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

    FunctionEvent(FunctionID id, FunctionEventKind eventKind) : Id(id), EventKind(eventKind) {}
};

struct EventsWithThreadId {
    std::vector<FunctionEvent>* Events;
    ThreadID ThreadId;

    explicit EventsWithThreadId(ThreadID threadId) {
        Events = new std::vector<FunctionEvent>();
        ThreadId = threadId;
    }
};

class ShadowStack {
private:
    static std::vector<FunctionEvent>* GetOrCreatePerThreadEvents(ThreadID threadId);

    std::string myDebugCallStacksSavePath;
    ICorProfilerInfo11* myProfilerInfo;
public:
    explicit ShadowStack(ICorProfilerInfo11* profilerInfo);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, ThreadID threadId);
    void AddFunctionFinished(FunctionID id, ThreadID threadId);
    void DebugWriteToFile();
};