#pragma once

#include "AssemblyInfo.h"
#include "../../util/util.h"


struct ModuleInfo {
    ModuleID id;
    wstring path;
    AssemblyInfo assembly;
    DWORD flags;

    ModuleInfo() : id(0), path(""_W), assembly({}), flags(0) {}
    ModuleInfo(ModuleID id, wstring path, AssemblyInfo assembly, DWORD flags)
        : id(id), path(path), assembly(assembly), flags(flags) {}

    static ModuleInfo GetModuleInfo(ICorProfilerInfo11* info, ModuleID moduleId);
};
