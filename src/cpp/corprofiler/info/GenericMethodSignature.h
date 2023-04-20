#pragma once

#include "../../util/ComPtr.h"
#include "../../util/util.h"
#include "TypeInfo.h"


struct GenericMethodSignature {
    std::vector<BYTE> Raw{};
    std::vector<TypeInfo> Generics{};

    explicit GenericMethodSignature(std::vector<BYTE> raw);

    GenericMethodSignature() = default;
};