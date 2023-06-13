#include "ShadowStackSerializer.h"
#include "../../../util/env_constants.h"
#include <iostream>
#include <fstream>
#include <regex>
#include <set>

BinaryShadowStackSerializer::BinaryShadowStackSerializer(ICorProfilerInfo12* profilerInfo,
                                                         ProcfilerLogger* logger) {
    myProfilerInfo = profilerInfo;
    myLogger = logger;
}

void BinaryShadowStackSerializer::Init() {
    if (!TryGetEnvVar(binaryStackSavePath, this->mySavePath)) {
        myLogger->LogError("Binary shadow stack save path was not defined");
        return;
    }
}

void BinaryShadowStackSerializer::Serialize(ShadowStack* shadowStack) {
    if (mySavePath.length() == 0) {
        myLogger->LogError("Will not serialize shadow stack to binary format as save path was not provided");
        return;
    }

    std::string methodFilterRegex;
    std::regex* regex = nullptr;
    if (TryGetEnvVar(filterMethodsRegex, methodFilterRegex)) {
        try {
            myLogger->LogInformation("Creating regex from " + methodFilterRegex);
            regex = new std::regex(methodFilterRegex);
        }
        catch (const std::regex_error &e) {
            myLogger->LogError("Failed to create regex from " + methodFilterRegex);
            regex = nullptr;
        }
    }

    myLogger->LogInformation("Started serializing shadow stack to binary file");
    std::ofstream fout(mySavePath, std::ios::binary);

    std::set<FunctionID> filteredOutFunctions;
    for (const auto& pair: *(shadowStack->GetAllStacks())) {
        auto threadId = (long long)pair.first;
        fout.write((char*)&threadId, sizeof(long long));

        auto events = *pair.second->Events;
        auto framesCount = (long long)events.size();
        auto countPos = fout.tellp();
        fout.write((char*)&framesCount, sizeof(long long));

        long long writtenFrames = 0;
        for (const auto& event: events) {
            if (filteredOutFunctions.find(event.Id) != filteredOutFunctions.end()) {
                continue;
            }

            auto shouldSkip = false;
            if (regex != nullptr) {
                auto functionName = FunctionInfo::GetFunctionInfo(myProfilerInfo, event.Id).GetFullName();
                std::smatch m;
                if (!std::regex_search(functionName, m, *regex)) {
                    shouldSkip = true;
                    filteredOutFunctions.insert(event.Id);
                }
            }

            if (shouldSkip) {
                continue;
            }

            char startOrEnd = event.EventKind == FunctionEventKind::Started ? 1 : 0;
            fout.write((char*)&startOrEnd, sizeof(char));
            fout.write((char*)&event.Timestamp, sizeof(long long));
            fout.write((char*)&event.Id, sizeof(long long));
            ++writtenFrames;
        }

        auto streamPos = fout.tellp();
        fout.seekp(countPos);
        fout.write((char*)&writtenFrames, sizeof(long long));
        fout.seekp(streamPos);
    }

    fout.close();
    delete regex;

    myLogger->LogInformation("Finished serializing shadow stack to binary file");
}
