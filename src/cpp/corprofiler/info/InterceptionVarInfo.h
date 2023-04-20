#pragma once

#include <cor.h>


struct InterceptionVarInfo {
    mdTypeRef TypeRef;
    int LocalVarIndex;

    InterceptionVarInfo(mdTypeRef typeRef, int localVarIndex) : TypeRef(typeRef), LocalVarIndex(localVarIndex) {}
};