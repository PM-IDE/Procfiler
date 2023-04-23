#include <thread>
#include "ShadowStack.h"
#include "../info/FunctionInfo.h"
#include <mutex>
#include <fstream>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

void ShadowStack::AddFunctionEnter(FunctionID id, ThreadID threadId) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Started));
}

void ShadowStack::AddFunctionFinished(FunctionID id, ThreadID threadId) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Finished));
}

ShadowStack::ShadowStack(ICorProfilerInfo11* profilerInfo) {
    myProfilerInfo = profilerInfo;
}

ShadowStack::~ShadowStack() {
    //TODO: proper destructor
}

std::vector<FunctionEvent>* ShadowStack::GetOrCreatePerThreadEvents(ThreadID threadId) {
    if (!ourIsInitialized) {
        ourEvents = new EventsWithThreadId(threadId);
        ourIsInitialized = true;
        std::unique_lock<std::mutex> lock{ourEventsPerThreadMutex};
        ourEventsPerThreads[threadId] = ourEvents;
    }

    return ourEvents->Events;
}

void ShadowStack::DebugWriteToFile(const std::string& filePath) {
    std::ofstream fout(filePath);
    std::map<FunctionID, FunctionInfo> resolvedFunctions;
    const std::string startPrefix = "[START]: ";
    const std::string endPrefix = "[ END ]: ";

    for (const auto& pair : ourEventsPerThreads) {
        auto threadFrame = "Thread(" + std::to_string(pair.first) + ")\n";
        fout << startPrefix << threadFrame;
        auto indent = 1;

        for (auto event : *(pair.second->Events)) {
            if (!resolvedFunctions.count(event.Id)) {
                resolvedFunctions[event.Id] = FunctionInfo::GetFunctionInfo(myProfilerInfo, event.Id);
            }

            auto& functionInfo = resolvedFunctions[event.Id];
            const std::string indentString = "  ";
            std::string funcName;

            if (event.EventKind == FunctionEventKind::Finished) {
                --indent;
            }

            for (int i = 0; i < indent; ++i) {
                funcName += indentString;
            }

            auto prefix = event.EventKind == FunctionEventKind::Started ? startPrefix : endPrefix;
            funcName += prefix + functionInfo.GetFullName() + "\n";
            fout << funcName;

            if (event.EventKind == FunctionEventKind::Started) {
                ++indent;
            }
        }

        fout << endPrefix << threadFrame;
        fout << "\n\n\n";
    }

    fout.close();
}
