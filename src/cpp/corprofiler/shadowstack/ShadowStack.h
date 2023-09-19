#include "cor.h"
#include "corprof.h"
#include "../../util/types.h"
#include "../../logging/ProcfilerLogger.h"
#include <vector>
#include <map>
#include <string>
#include <stack>
#include <atomic>
#include <regex>
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
    std::map<FunctionID, bool>* ShouldLogFuncs;
    std::vector<FunctionEvent>* Events;
    std::stack<FunctionID>* CurrentStack;
    DWORD ThreadId;

    explicit EventsWithThreadId(DWORD threadId) {
        ShouldLogFuncs = new std::map<FunctionID, bool>;
        Events = new std::vector<FunctionEvent>();
        CurrentStack = new std::stack<FunctionID>();
        ThreadId = threadId;
    }

    ~EventsWithThreadId() {
        delete Events;
    }

    void AddFunctionEvent(FunctionEvent event) const {
        Events->push_back(event);

        if (event.EventKind == FunctionEventKind::Started) {
            CurrentStack->push(event.Id);
        } else {
            CurrentStack->pop();
        }
    }

    bool ShouldLog(FunctionID& id, bool* shouldLog) const {
        if (ShouldLogFuncs->find(id) == ShouldLogFuncs->end()) {
            return false;
        } else {
            *shouldLog = ShouldLogFuncs->at(id);
            return true;
        }
    }

    void PutFunctionShouldLogDecision(FunctionID& id, bool shouldLog) const {
        ShouldLogFuncs->insert({id, shouldLog});
    }
};

class ShadowStack {
private:
    static EventsWithThreadId* GetOrCreatePerThreadEvents(DWORD threadId);

    std::regex* myFilterRegex{nullptr};

    ICorProfilerInfo12* myProfilerInfo;
    ProcfilerLogger* myLogger;
    std::atomic<int> myCurrentAddition{0};
    std::atomic<bool> myCanProcessFunctionEvents{true};

    bool CanProcessFunctionEvents();
    bool ShouldAddFunc(FunctionID& id, DWORD threadId);
public:
    explicit ShadowStack(ICorProfilerInfo12* profilerInfo, ProcfilerLogger* logger);

    ~ShadowStack();
    void AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp);
    void AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp);
    void HandleExceptionCatchEnter(FunctionID catcherFunctionID, DWORD threadId, int64_t timestamp);

    void SuppressFurtherMethodsEvents();
    void WaitForPendingMethodsEvents();
    void AdjustShadowStacks();
    std::map<ThreadID, EventsWithThreadId*>* GetAllStacks() const;
};

struct FunctionEventProcessingCookie {
private:
    std::atomic<int>* myProcessingCount;
public:
    explicit FunctionEventProcessingCookie(std::atomic<int>* processingCount) {
        myProcessingCount = processingCount;
        myProcessingCount->fetch_add(1, std::memory_order_seq_cst);
    }

    ~FunctionEventProcessingCookie() {
        myProcessingCount->fetch_sub(1, std::memory_order_seq_cst);
    }
};

#endif