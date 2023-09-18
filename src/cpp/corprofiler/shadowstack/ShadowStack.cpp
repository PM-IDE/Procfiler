#include "ShadowStack.h"
#include "../../util/env_constants.h"
#include <mutex>
#include <stack>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

ShadowStack::ShadowStack(ICorProfilerInfo12* profilerInfo, ProcfilerLogger* logger) {
    myProfilerInfo = profilerInfo;

    std::string value;
    TryGetEnvVar(filterMethodsDuringRuntime, value);

    if (value == "1") {
        if (TryGetEnvVar(filterMethodsRegex, value)) {
            try {
                myFilterRegex = new std::regex(value);
            }
            catch (const std::regex_error &e) {
                myFilterRegex = nullptr;
            }
        } else {
            myFilterRegex = nullptr;
        }
    }

    myLogger = logger;
}

ShadowStack::~ShadowStack() {
    //TODO: proper destructor
}

void ShadowStack::AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp) {
    if (!CanProcessFunctionEvents()) return;

    FunctionEventProcessingCookie cookie(&this->myCurrentAddition);

    if (!CanProcessFunctionEvents()) return;

    if (ShouldAddFunc(id)) {
        const auto event = FunctionEvent(id, FunctionEventKind::Started, timestamp);
        GetOrCreatePerThreadEvents(threadId)->AddFunctionEvent(event);
    }
}

bool ShadowStack::ShouldAddFunc(FunctionID& id) {
    if (myFilterRegex == nullptr) return true;

    auto functionName = FunctionInfo::GetFunctionInfo(myProfilerInfo, id).GetFullName();

    std::smatch m;
    return std::regex_search(functionName, m, *myFilterRegex);
}

void ShadowStack::AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp) {
    if (!CanProcessFunctionEvents()) return;

    FunctionEventProcessingCookie cookie(&this->myCurrentAddition);

    if (!CanProcessFunctionEvents()) return;

    if (ShouldAddFunc(id)) {
        const auto event = FunctionEvent(id, FunctionEventKind::Finished, timestamp);
        GetOrCreatePerThreadEvents(threadId)->AddFunctionEvent(event);
    }
}

void ShadowStack::HandleExceptionCatchEnter(FunctionID catcherFunctionId, DWORD threadId, int64_t timestamp) {
    if (!CanProcessFunctionEvents()) return;

    FunctionEventProcessingCookie cookie(&this->myCurrentAddition);

    if (!CanProcessFunctionEvents()) return;

    auto events = GetOrCreatePerThreadEvents(threadId);
    auto stack = events->CurrentStack;
    while (!stack->empty()) {
        auto top = stack->top();
        if (top == catcherFunctionId) {
            break;
        }

        events->AddFunctionEvent(FunctionEvent(top, FunctionEventKind::Finished, timestamp));
    }
}

EventsWithThreadId* ShadowStack::GetOrCreatePerThreadEvents(DWORD threadId) {
    if (!ourIsInitialized) {
        std::unique_lock<std::mutex> lock{ourEventsPerThreadMutex};
        if (!ourIsInitialized) {
            ourEvents = new EventsWithThreadId(threadId);
            ourEventsPerThreads[threadId] = ourEvents;
            ourIsInitialized = true;
        }
    }

    return ourEvents;
}

std::map<ThreadID, EventsWithThreadId*>* ShadowStack::GetAllStacks() const {
    return &ourEventsPerThreads;
}

void ShadowStack::AdjustShadowStacks() {
    if (myCanProcessFunctionEvents.load(std::memory_order_seq_cst)) {
        myLogger->LogError("Can not adjust stack while additions are still allowed");
        return;
    }

    std::stack<FunctionID> stack;
    for (const auto& pair : *GetAllStacks()) {
        auto events = pair.second->Events;
        int64_t timestamp;
        for (const auto& event: *events) {
            timestamp = event.Timestamp;
            if (event.EventKind == FunctionEventKind::Started) {
                stack.push(event.Id);
                continue;
            }

            if (stack.top() == event.Id) {
                stack.pop();
                continue;
            }

            myLogger->LogError("Encountered inconsistent stack");
        }

        while (!stack.empty()) {
            const auto& top = stack.top();
            events->push_back(FunctionEvent(top, FunctionEventKind::Finished, timestamp));
            stack.pop();
        }
    }
}

void ShadowStack::SuppressFurtherMethodsEvents() {
    myCanProcessFunctionEvents.store(false, std::memory_order_seq_cst);
}

void ShadowStack::WaitForPendingMethodsEvents() {
    while (myCurrentAddition.load(std::memory_order_seq_cst) != 0) {
    }
}

bool ShadowStack::CanProcessFunctionEvents() {
    return myCanProcessFunctionEvents.load(std::memory_order_seq_cst);
}