#ifndef PROCFILER_SHADOW_STACK_H
#define PROCFILER_SHADOW_STACK_H

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
#include "EventsWithThreadId.h"

class ShadowStack {
private:
    static EventsWithThreadId* GetOrCreatePerThreadEvents(DWORD threadId, bool onlineSerialization);

    std::regex* myFilterRegex{nullptr};

    bool myOnlineSerialization{false};
    ICorProfilerInfo12* myProfilerInfo;
    ProcfilerLogger* myLogger;
    std::atomic<int> myCurrentAddition{0};
    std::atomic<bool> myCanProcessFunctionEvents{true};

    bool CanProcessFunctionEvents();
    bool ShouldAddFunc(FunctionID& id, DWORD threadId);
public:
    explicit ShadowStack(ICorProfilerInfo12* profilerInfo, ProcfilerLogger* logger, bool onlineSerialization);

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