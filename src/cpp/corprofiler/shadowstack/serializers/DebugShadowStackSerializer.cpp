#include <fstream>
#include "ShadowStackSerializer.h"
#include "../../../util/env_constants.h"

DebugShadowStackSerializer::DebugShadowStackSerializer(ICorProfilerInfo12* profilerInfo) {
    myProfilerInfo = profilerInfo;
}

void DebugShadowStackSerializer::Init() {
    auto rawEnvVar = std::getenv(shadowStackDebugSavePath.c_str());
    mySavePath = rawEnvVar == nullptr ? "" : std::string(rawEnvVar);
}

void DebugShadowStackSerializer::Serialize(ShadowStack* shadowStack) {
    if (mySavePath.length() == 0) {
        return;
    }

    std::ofstream fout(mySavePath);
    std::map<FunctionID, FunctionInfo> resolvedFunctions;
    const std::string startPrefix = "[START]: ";
    const std::string endPrefix = "[ END ]: ";

    for (const auto& pair: *(shadowStack->GetAllStacks())) {
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
