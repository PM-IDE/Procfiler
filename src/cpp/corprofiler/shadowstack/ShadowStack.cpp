#include "ShadowStack.h"
#include "../../util/env_constants.h"
#include <mutex>
#include <stack>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

ShadowStack::ShadowStack(ProcfilerLogger* logger) {
    myLogger = logger;
}

ShadowStack::~ShadowStack() {
    //TODO: proper destructor
}

void ShadowStack::AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp) {
    if (!myCanProcessFunctionEvents.load(std::memory_order_seq_cst)) return;

    myCurrentAddition.fetch_add(1, std::memory_order_seq_cst);
    const auto event = FunctionEvent(id, FunctionEventKind::Started, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
    myCurrentAddition.fetch_sub(1, std::memory_order_seq_cst);
}

void ShadowStack::AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp) {
    if (!myCanProcessFunctionEvents.load(std::memory_order_seq_cst)) return;

    myCurrentAddition.fetch_add(1, std::memory_order_seq_cst);
    const auto event = FunctionEvent(id, FunctionEventKind::Finished, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
    myCurrentAddition.fetch_sub(1, std::memory_order_seq_cst);
}

std::vector<FunctionEvent>* ShadowStack::GetOrCreatePerThreadEvents(DWORD threadId) {
    if (!ourIsInitialized) {
        ourIsInitialized = true;
        std::unique_lock<std::mutex> lock{ourEventsPerThreadMutex};
        ourEvents = new EventsWithThreadId(threadId);
        ourEventsPerThreads[threadId] = ourEvents;
    }

    return ourEvents->Events;
}

std::map<ThreadID, EventsWithThreadId*>* ShadowStack::GetAllStacks() const {
    return &ourEventsPerThreads;
}

void ShadowStack::AdjustShadowStacks() {
    if (myCanProcessFunctionEvents.load(std::memory_order_seq_cst)) {
        myLogger->Log("Can not adjust stack while additions are still allowed");
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

            myLogger->Log("Encountered inconsistent stack");
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
