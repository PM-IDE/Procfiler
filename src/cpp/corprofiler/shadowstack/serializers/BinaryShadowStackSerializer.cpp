#include "ShadowStackSerializer.h"
#include "../../../util/env_constants.h"


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
    if (mySavePath.length() == 0) return;
}
