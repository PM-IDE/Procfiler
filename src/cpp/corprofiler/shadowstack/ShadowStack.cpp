#include "ShadowStack.h"
#include "../../util/env_constants.h"
#include "../info/FunctionInfo.h"
#include <mutex>
#include <fstream>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

void ShadowStack::AddFunctionEnter(FunctionID id, ThreadID threadId, int64_t timestamp) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Started, timestamp));
}

void ShadowStack::AddFunctionFinished(FunctionID id, ThreadID threadId, int64_t timestamp) {
    GetOrCreatePerThreadEvents(threadId)->emplace_back(FunctionEvent(id, FunctionEventKind::Finished, timestamp));
}

ShadowStack::ShadowStack(ICorProfilerInfo13* profilerInfo, ProcfilerLogger* logger) {
    auto rawEnvVar = std::getenv(shadowStackDebugSavePath.c_str());
    myDebugCallStacksSavePath = rawEnvVar == nullptr ? "" : std::string(rawEnvVar);
    myProfilerInfo = profilerInfo;
    myLogger = logger;
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

void ShadowStack::DebugWriteToFile() {
    if (myDebugCallStacksSavePath.length() == 0) {
        return;
    }

    std::ofstream fout(myDebugCallStacksSavePath);
    std::map<FunctionID, FunctionInfo> resolvedFunctions;
    const std::string startPrefix = "[START]: ";
    const std::string endPrefix = "[ END ]: ";

    for (const auto& pair: ourEventsPerThreads) {
        auto threadFrame = "Thread(" + std::to_string(pair.first) + ")\n";
        fout << startPrefix << threadFrame;
        auto indent = 1;

        for (auto event: *(pair.second->Events)) {
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

void ShadowStack::WriteMethodsEventsToEventPipe() {
    HRESULT hr;
    if ((hr = DefineProcfilerEventPipeProvider()) != S_OK) {
        auto logMessage = "Failed to initialize Event Pipe Provider, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return;
    }

    if ((hr = DefineProcfilerMethodStartEvent()) != S_OK) {
        auto logMessage = "Failed to initialize method start event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return;
    }

    if ((hr = DefineProcfilerMethodEndEvent()) != S_OK) {
        auto logMessage = "Failed tp initialize method end event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return;
    }

    if ((hr = DefineProcfilerMethodInfoEvent()) != S_OK) {
        auto logMessage = "Failed to initialize method info event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return;
    }

    std::map<FunctionID, FunctionInfo> resolvedFunctions;
    for (const auto& pair: ourEventsPerThreads) {
        auto threadId = pair.first;
        for (auto event : *(pair.second->Events)) {
            if (!resolvedFunctions.count(event.Id)) {
                resolvedFunctions[event.Id] = FunctionInfo::GetFunctionInfo(myProfilerInfo, event.Id);
            }

            auto eventPipeEvent = event.EventKind == FunctionEventKind::Started ? myMethodStartEvent : myMethodEndEvent;

            COR_PRF_EVENT_DATA eventData[3];

            eventData[0].ptr = reinterpret_cast<UINT64>(&event.Timestamp);
            eventData[0].size = sizeof(int64_t);

            eventData[1].ptr = reinterpret_cast<UINT64>(&event.Id);
            eventData[1].ptr = sizeof(FunctionID);

            eventData[2].ptr = reinterpret_cast<UINT64>(&threadId);
            eventData[2].size = sizeof(ThreadID);

            auto dataCount = sizeof(eventData) / sizeof(COR_PRF_EVENT_DATA);

            myProfilerInfo->EventPipeWriteEvent(eventPipeEvent, dataCount, eventData, NULL, NULL);
        }
    }

    for (const auto& pair : resolvedFunctions) {
        COR_PRF_EVENT_DATA eventData[2];
        
        eventData[0].ptr = reinterpret_cast<UINT64>(&pair.first);
        eventData[0].size = sizeof(FunctionID);

        auto functionName = pair.second.GetName();
        eventData[1].ptr = reinterpret_cast<UINT64>(&functionName);
        eventData[1].size = static_cast<UINT32>(functionName.length() + 1) * sizeof(WCHAR);

        auto dataCount = sizeof(eventData) / sizeof(COR_PRF_EVENT_DATA);

        myProfilerInfo->EventPipeWriteEvent(myMethodInfoEvent, dataCount, eventData, NULL, NULL);
    }
}

HRESULT ShadowStack::DefineProcfilerMethodStartEvent() {
    return DefineMethodStartOrEndEventInternal(W("ProcfilerMethodStart"),
                                               myEventPipeProvider,
                                               &myMethodStartEvent,
                                               myProfilerInfo,
                                               ourMethodStartEventId);
}

HRESULT ShadowStack::DefineProcfilerMethodEndEvent() {
    return DefineMethodStartOrEndEventInternal(W("ProcfilerMethodEnd"),
                                               myEventPipeProvider,
                                               &myMethodStartEvent,
                                               myProfilerInfo,
                                               ourMethodEndEventId);
}

HRESULT ShadowStack::DefineProcfilerMethodInfoEvent() {
    COR_PRF_EVENTPIPE_PARAM_DESC eventParameters[] = {
            {COR_PRF_EVENTPIPE_UINT64, 0, W("FunctionId")},
            {COR_PRF_EVENTPIPE_STRING, 0, W("FunctionName")},
    };

    auto paramsCount = sizeof(eventParameters) / sizeof(COR_PRF_EVENTPIPE_PARAM_DESC);

    return myProfilerInfo->EventPipeDefineEvent(
            myEventPipeProvider,             // Provider
            W("ProcfilerMethodInfo"),        // Name
            ourMethodInfoEventId,            // ID
            0,                               // Keywords
            1,                               // Version
            COR_PRF_EVENTPIPE_LOGALWAYS,     // Level
            0,                               // opcode
            false,                           // Needs stack
            paramsCount,                     // size of params
            eventParameters,                 // Param descriptors
            &myMethodInfoEvent               // [OUT] event ID
    );
}

HRESULT ShadowStack::DefineProcfilerEventPipeProvider() {
    return myProfilerInfo->EventPipeCreateProvider(ourEventPipeProviderName.c_str(), &myEventPipeProvider);
}

HRESULT ShadowStack::DefineMethodStartOrEndEventInternal(const wstring& eventName,
                                                         EVENTPIPE_PROVIDER provider,
                                                         EVENTPIPE_EVENT* outEventId,
                                                         ICorProfilerInfo13* profilerInfo,
                                                         UINT32 eventId) {
    COR_PRF_EVENTPIPE_PARAM_DESC eventParameters[] = {
            {COR_PRF_EVENTPIPE_UINT64, 0, W("Timestamp")},
            {COR_PRF_EVENTPIPE_UINT64, 0, W("FunctionId")},
            {COR_PRF_EVENTPIPE_UINT64, 0, W("ThreadId")},
    };

    auto paramsCount = sizeof(eventParameters) / sizeof(COR_PRF_EVENTPIPE_PARAM_DESC);

    return profilerInfo->EventPipeDefineEvent(
            provider,                        // Provider
            eventName.c_str(),               // Name
            eventId,                            // ID
            0,                               // Keywords
            1,                               // Version
            COR_PRF_EVENTPIPE_LOGALWAYS,     // Level
            0,                               // opcode
            false,                           // Needs stack
            paramsCount,                     // size of params
            eventParameters,                 // Param descriptors
            outEventId                          // [OUT] event ID
    );
}