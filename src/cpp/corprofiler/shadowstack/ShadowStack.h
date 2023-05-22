#include "cor.h"
#include "corprof.h"
#include "../../util/types.h"
#include "../../logging/ProcfilerLogger.h"
#include <vector>
#include <map>
#include <string>
#include "../../util/util.h"
#include "../info/FunctionInfo.h"

#ifndef PROCFILER_SHADOW_STACK_H
#define PROCFILER_SHADOW_STACK_H

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
    static std::vector<FunctionEvent>* GetOrCreatePerThreadEvents(DWORD threadId);

    ProcfilerLogger* myLogger;
    std::atomic<int> myCurrentAddition{0};
    std::atomic<bool> myCanProcessFunctionEvents{true};
public:
    explicit ShadowStack(ProcfilerLogger* logger);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp);
    void AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp);

    void SuppressFurtherMethodsEvents();
    void WaitForPendingMethodsEvents();
    void AdjustShadowStacks();
    std::map<ThreadID, EventsWithThreadId*>* GetAllStacks() const;
};

#endif