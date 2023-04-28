#include "ShadowStack.h"
#include "../../util/env_constants.h"
#include <mutex>
#include <fstream>

static thread_local bool ourIsInitialized = false;
static thread_local EventsWithThreadId* ourEvents;
static std::map<ThreadID, EventsWithThreadId*> ourEventsPerThreads;
static std::mutex ourEventsPerThreadMutex;

void ShadowStack::AddFunctionEnter(FunctionID id, ThreadID threadId, int64_t timestamp) {
    const auto event = FunctionEvent(id, FunctionEventKind::Started, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
}

void ShadowStack::AddFunctionFinished(FunctionID id, ThreadID threadId, int64_t timestamp) {
    const auto event = FunctionEvent(id, FunctionEventKind::Finished, timestamp);
    GetOrCreatePerThreadEvents(threadId)->emplace_back(event);
}

ShadowStack::ShadowStack(ICorProfilerInfo13* profilerInfo, ProcfilerLogger* logger) {
    auto rawEnvVar = std::getenv(shadowStackDebugSavePath.c_str());
    myDebugCallStacksSavePath = rawEnvVar == nullptr ? "" : std::string(rawEnvVar);
    myProfilerInfo = profilerInfo;
    myLogger = logger;

    InitializeProvidersAndEvents();
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

    for (const auto& pair: ourEventsPerThreads) {
        auto threadId = pair.first;
        for (const auto& event : *(pair.second->Events)) {
            if (FAILED(LogFunctionEvent(event, threadId))) {
                myLogger->Log("Failed to send a method start or end event, error: " + std::to_string(hr));
            }
        }
    }

    myLogger->Log("Logged method start and end events to event pipe");

    for (const auto& pair : myResolvedFunctions) {
        if (FAILED(LogMethodInfo(pair.first, pair.second))) {
            myLogger->Log("Failed to send a method info event, error: " + std::to_string(hr));
        }
    }

    myLogger->Log("Logged all method info events");
}

HRESULT ShadowStack::DefineProcfilerMethodStartEvent() {
    return DefineMethodStartOrEndEventInternal(ToWString("ProcfilerMethodStart"),
                                               myEventPipeProvider,
                                               &myMethodStartEvent,
                                               myProfilerInfo,
                                               ourMethodStartEventId);
}

HRESULT ShadowStack::DefineProcfilerMethodEndEvent() {
    return DefineMethodStartOrEndEventInternal(ToWString("ProcfilerMethodEnd"),
                                               myEventPipeProvider,
                                               &myMethodEndEvent,
                                               myProfilerInfo,
                                               ourMethodEndEventId);
}

HRESULT ShadowStack::DefineProcfilerMethodInfoEvent() {
    COR_PRF_EVENTPIPE_PARAM_DESC eventParameters[] = {
        {COR_PRF_EVENTPIPE_UINT64, 0, ToWString("FunctionId").c_str()},
        {COR_PRF_EVENTPIPE_STRING, 0, ToWString("FunctionName").c_str()},
    };

    auto paramsCount = sizeof(eventParameters) / sizeof(COR_PRF_EVENTPIPE_PARAM_DESC);

    return myProfilerInfo->EventPipeDefineEvent(
        myEventPipeProvider,
        ourMethodInfoEventName.c_str(),
        ourMethodInfoEventId,
        0,
        1,
        COR_PRF_EVENTPIPE_LOGALWAYS,
        0,
        false,
        paramsCount,
        eventParameters,
        &myMethodInfoEvent
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
        {COR_PRF_EVENTPIPE_UINT64, 0, ToWString("Timestamp").c_str()},
        {COR_PRF_EVENTPIPE_UINT64, 0, ToWString("FunctionId").c_str()},
        {COR_PRF_EVENTPIPE_UINT64, 0, ToWString("ThreadId").c_str()},
    };

    auto paramsCount = sizeof(eventParameters) / sizeof(COR_PRF_EVENTPIPE_PARAM_DESC);

    return profilerInfo->EventPipeDefineEvent(
        provider,
        eventName.c_str(),
        eventId,
        0,
        1,
        COR_PRF_EVENTPIPE_LOGALWAYS,
        0,
        false,
        paramsCount,
        eventParameters,
        outEventId
    );
}

HRESULT ShadowStack::InitializeProvidersAndEvents() {
    myLogger->Log("Starting writing shadow stack to event pipe");

    HRESULT hr;
    if ((hr = DefineProcfilerEventPipeProvider()) != S_OK) {
        auto logMessage = "Failed to initialize Event Pipe Provider, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return hr;
    }

    if ((hr = DefineProcfilerMethodStartEvent()) != S_OK) {
        auto logMessage = "Failed to initialize method start event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return hr;
    }

    if ((hr = DefineProcfilerMethodEndEvent()) != S_OK) {
        auto logMessage = "Failed to initialize method end event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return hr;
    }

    if ((hr = DefineProcfilerMethodInfoEvent()) != S_OK) {
        auto logMessage = "Failed to initialize method info event, HR = " + std::to_string(hr);
        myLogger->Log(logMessage);
        return hr;
    }

    myLogger->Log("Initialized provider and all needed events");

    return S_OK;
}

HRESULT ShadowStack::LogFunctionEvent(const FunctionEvent& event, const ThreadID& threadId) {
    if (!myResolvedFunctions.count(event.Id)) {
        myResolvedFunctions[event.Id] = FunctionInfo::GetFunctionInfo(myProfilerInfo, event.Id);
    }

    auto eventPipeEvent = event.EventKind == FunctionEventKind::Started ? myMethodStartEvent : myMethodEndEvent;

    COR_PRF_EVENT_DATA eventData[3];

    eventData[0].ptr = reinterpret_cast<UINT64>(&event.Timestamp);
    eventData[0].size = sizeof(int64_t);

    eventData[1].ptr = reinterpret_cast<UINT64>(&event.Id);
    eventData[1].size = sizeof(FunctionID);

    eventData[2].ptr = reinterpret_cast<UINT64>(&threadId);
    eventData[2].size = sizeof(ThreadID);

    auto dataCount = sizeof(eventData) / sizeof(COR_PRF_EVENT_DATA);

    return myProfilerInfo->EventPipeWriteEvent(eventPipeEvent, dataCount, eventData, NULL, NULL);
}

HRESULT ShadowStack::LogMethodInfo(const FunctionID& functionId, const FunctionInfo& functionInfo) {
    COR_PRF_EVENT_DATA eventData[2];

    eventData[0].ptr = reinterpret_cast<UINT64>(&functionId);
    eventData[0].size = sizeof(FunctionID);

    auto functionName = functionInfo.GetName();
    eventData[1].ptr = reinterpret_cast<UINT64>(&functionName);
    eventData[1].size = static_cast<UINT32>(functionName.length() + 1) * sizeof(WCHAR);

    auto dataCount = sizeof(eventData) / sizeof(COR_PRF_EVENT_DATA);

    return myProfilerInfo->EventPipeWriteEvent(myMethodInfoEvent, dataCount, eventData, NULL, NULL);
}
