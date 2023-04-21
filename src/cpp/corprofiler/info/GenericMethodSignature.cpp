#include "GenericMethodSignature.h"
#include "parser.h"


GenericMethodSignature::GenericMethodSignature(std::vector<BYTE> rawSignature) : myRawSignature(rawSignature) {
    auto iter = this->myRawSignature.begin();
    ULONG skip = 0;
    ParseNumber(iter, skip);

    ULONG number = 0;
    ParseNumber(iter, number);

    for (size_t i = 0; i < number; i++)
    {
        auto begin = iter;
        if (!ParseType(iter))
        {
            break;
        }

        myGenerics.push_back(TypeInfo(std::vector<BYTE>(begin, iter)));
    }
}

std::vector<BYTE> GenericMethodSignature::GetRawSignature() {
    return myRawSignature;
}

std::vector<TypeInfo> GenericMethodSignature::GetGenericsTypes() {
    return myGenerics;
}
