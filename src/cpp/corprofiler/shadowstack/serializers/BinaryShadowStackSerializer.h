#ifndef PROCFILER_BINARYSHADOWSTACKSERIALIZER_H
#define PROCFILER_BINARYSHADOWSTACKSERIALIZER_H

#include <set>
#include "ShadowStackSerializer.h"

class BinaryShadowStackSerializer : public ShadowStackSerializer {
private:
    std::string mySavePath;
    ICorProfilerInfo12* myProfilerInfo;
    ProcfilerLogger* myLogger;
    bool myUseSeparateBinstacksFiles;

    void WriteThreadStack(ThreadID threadId,
                          std::vector<FunctionEvent>* events,
                          std::ofstream& fout,
                          std::set<FunctionID>& filteredOutFunctions,
                          std::regex* methodsFilterRegex);
public:
    explicit BinaryShadowStackSerializer(ICorProfilerInfo12* profilerInfo, ProcfilerLogger* logger);
    ~BinaryShadowStackSerializer() override = default;

    void Init() override;
    void Serialize(ShadowStack* shadowStack) override;
};


#endif //PROCFILER_BINARYSHADOWSTACKSERIALIZER_H