#pragma once

#include "AssemblyInfo.h"
#include "../../util/util.h"


struct ModuleInfo {
private:
    ModuleID myId;
    wstring myPath;
    AssemblyInfo myAssembly;
    DWORD myFlags;

public:
    ModuleInfo() : myId(0), myPath(""_W), myAssembly({}), myFlags(0) {}

    ModuleInfo(ModuleID id, wstring path, AssemblyInfo assembly, DWORD flags)
        : myId(id), myPath(path), myAssembly(assembly), myFlags(flags) {}

    static ModuleInfo GetModuleInfo(ICorProfilerInfo11* info, ModuleID moduleId);

    ModuleID GetId();
    wstring GetPath();
    AssemblyInfo GetAssemblyInfo();
    DWORD GetFlags();
};
