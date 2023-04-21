#include <thread>
#include "ShadowStack.h"

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;

void ShadowStack::AddFunctionEnter(FunctionID id, ThreadID threadId) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Started));
}

void ShadowStack::AddFunctionFinished(FunctionID id, ThreadID threadId) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Finished));
}

ShadowStack::~ShadowStack() {
    //TODO: proper destructor
}

std::vector<FunctionEvent>* ShadowStack::GetOrCreatePerThreadEvents(ThreadID threadId) {
    if (!ourIsInitialized) {
        ourEvents = new EventsWithThreadId(threadId);
        ourIsInitialized = true;
    }

    return ourEvents->Events;
}
