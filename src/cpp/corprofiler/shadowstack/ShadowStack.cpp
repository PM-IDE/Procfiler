#include "ShadowStack.h"
#include "../../util/env_constants.h"
#include <mutex>
#include <fstream>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

void ShadowStack::AddFunctionEnter(FunctionID id, DWORD threadId, int64_t timestamp) {
    const auto event = FunctionEvent(id, FunctionEventKind::Started, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
}

void ShadowStack::AddFunctionFinished(FunctionID id, DWORD threadId, int64_t timestamp) {
    const auto event = FunctionEvent(id, FunctionEventKind::Finished, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
}

ShadowStack::~ShadowStack() {
    //TODO: proper destructor
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