#include "ShadowStackSerializer.h"
#include "../../../util/env_constants.h"
#include <iostream>
#include <fstream>

BinaryShadowStackSerializer::BinaryShadowStackSerializer(ICorProfilerInfo12* profilerInfo,
                                                         ProcfilerLogger* logger) {
    myProfilerInfo = profilerInfo;
    myLogger = logger;
}

void BinaryShadowStackSerializer::Init() {
    auto rawEnvVar = std::getenv(binaryStackSavePath.c_str());
    mySavePath = rawEnvVar == nullptr ? "" : std::string(rawEnvVar);
}

void BinaryShadowStackSerializer::Serialize(const ShadowStack& shadowStack) {
    if (mySavePath.length() == 0) {
        myLogger->Log("Will not serialize shadow stack to binary format as save path was not provided");
        return;
    }

    myLogger->Log("Started serializing shadow stack to binary file");
    std::ofstream fout(mySavePath, std::ios::binary);

    for (const auto& pair: *(shadowStack.GetAllStacks())) {
        auto threadId = pair.first;
        fout.write((char*)&threadId, sizeof(long));

        auto events = *pair.second->Events;
        auto framesCount = (long long)events.size();
        fout.write((char*)&framesCount, sizeof(long long));

        for (const auto& event: events) {
            char startOrEnd = event.EventKind == FunctionEventKind::Started ? 1 : 0;
            fout.write((char*)&startOrEnd, sizeof(char));
            fout.write((char*)&event.Timestamp, sizeof(long long));
            fout.write((char*)&event.Id, sizeof(long long));
        }
    }

    fout.close();

    myLogger->Log("Finished serializing shadow stack to binary file");
}
