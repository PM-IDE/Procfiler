#pragma once

#include "cor.h"
#include "corprof.h"
#include "../corprofiler/info/FunctionInfo.h"
#include "../corprofiler/info/ModuleInfo.h"
#include "ComPtr.h"


void GetMsCorLibRef(HRESULT& hr, const ComPtr<IMetaDataAssemblyEmit>& metadataAssemblyEmit, mdModuleRef& libRef);
void GetWrapperRef(HRESULT& hr,
                   const ComPtr<IMetaDataAssemblyEmit>& metadataAssemblyEmit,
                   mdModuleRef& libRef,
                   const wstring& assemblyName);

mdToken GetTypeToken(ComPtr<IMetaDataEmit2>& metadataEmit, mdAssemblyRef mscorlibRef, std::vector<BYTE>& type);
