#pragma once

#include <iomanip>
#include "../../util/util.h"
#include "TypeInfo.h"

struct MethodSignature {
private:
    size_t myArgumentsOffset = 0;
    std::vector<BYTE> myRawSignature{};
    TypeInfo myReturnType{};
    std::vector<TypeInfo> myArguments{};

public:
    MethodSignature() = default;

    explicit MethodSignature(std::vector<BYTE> rawSignature);

    void ParseArguments();

    std::vector<BYTE> GetRawSignature();
    TypeInfo GetReturnTypeInfo();
    std::vector<TypeInfo> GetArguments();

    COR_SIGNATURE MethodSignature::CallingConvention() const;
    bool MethodSignature::IsInstanceMethod() const;
    bool MethodSignature::IsGeneric() const;
    ULONG MethodSignature::NumberOfArguments() const;
};