#include "parser.h"
#include "TypeInfo.h"
#include "../../util/const.h"
#include "../../util/helpers.h"


TypeInfo::TypeInfo(const std::vector<BYTE>& raw) : myRaw(raw) {
    myIsRefType = raw.size() > 0 && raw[0] == ELEMENT_TYPE_BYREF;

    myIsVoid = raw.size() > 0 && raw[0] == ELEMENT_TYPE_VOID;

    auto shift = myIsRefType ? 1 : 0;

    switch (raw[myIsRefType]) {
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_STRING:
            //case ELEMENT_TYPE_VALUETYPE:
            myTypeDef = raw[myIsRefType];
            myIsBoxed = true;
            break;
        case ELEMENT_TYPE_MVAR: {
            myIsGenericMethodRef = true;
            auto iter = this->myRaw.begin();
            std::advance(iter, 1 + shift);
            ParseNumber(iter, myGenericRefNumber);
            break;
        }
        case ELEMENT_TYPE_VAR: {
            myIsGenericClassRef = true;
            auto iter = this->myRaw.begin();
            std::advance(iter, 1 + shift);
            ParseNumber(iter, myGenericRefNumber);
            break;
        }
        default:
            myTypeDef = ELEMENT_TYPE_OBJECT;
            myIsBoxed = false;
            break;
    }
}

void TypeInfo::TryParseGeneric() {
    auto iter = myRaw.begin();
    ULONG elementType = 0;
    ParseNumber(iter, elementType); // => ELEMENT_TYPE_GENERICINST
    ParseNumber(iter, elementType); // => ELEMENT_TYPE_CLASS, ELEMENT_TYPE_VALUETYPE

    if (elementType != ELEMENT_TYPE_CLASS && elementType != ELEMENT_TYPE_VALUETYPE) {
        return;
    }

    ParseNumber(iter, elementType); // => token

    ULONG number = 0;

    ParseNumber(iter, number); // => number of generic arguments

    for (size_t i = 0; i < number; i++) {
        auto begin = iter;
        if (!ParseType(iter)) {
            break;
        }

        myGenerics.push_back(TypeInfo(std::vector<BYTE>(begin, iter)));
    }
}

TypeInfo TypeInfo::GetTypeInfo(IMetaDataImport2* metadataImport, mdToken token) {
    std::vector<WCHAR> typeName(MAX_CLASS_NAME, (WCHAR) 0);
    DWORD typeNameLength = 0;

    HRESULT hr = E_FAIL;
    const auto token_type = TypeFromToken(token);
    switch (token_type) {
        case mdtTypeDef:
            hr = metadataImport->GetTypeDefProps(token, &typeName[0], MAX_CLASS_NAME,
                                                 &typeNameLength, nullptr, nullptr);
            break;
        case mdtTypeRef:
            hr = metadataImport->GetTypeRefProps(token, nullptr, &typeName[0],
                                                 MAX_CLASS_NAME, &typeNameLength);
            break;
        case mdtTypeSpec: {
            PCCOR_SIGNATURE signature{};
            ULONG signature_length{};

            hr = metadataImport->GetTypeSpecFromToken(token, &signature,
                                                      &signature_length);

            if (FAILED(hr) || signature_length < 3) {
                return {};
            }

            if (signature[0] & ELEMENT_TYPE_GENERICINST) {
                mdToken typeToken;
                auto length = CorSigUncompressToken(&signature[2], &typeToken);
                auto ti = GetTypeInfo(metadataImport, typeToken);
                ti.myRaw = ToRaw(signature, signature_length);
                ti.TryParseGeneric();

                return ti;
            }
        }
            break;
        case mdtModuleRef:
            metadataImport->GetModuleRefProps(token, &typeName[0], MAX_CLASS_NAME, &typeNameLength);
            break;
        case mdtMemberRef:
            return FunctionInfo::GetFunctionInfo(metadataImport, token).GetTypeInfo();
    }

    if (FAILED(hr) || typeNameLength == 0) {
        return {};
    }

    return {token, ToString(typeName, typeNameLength), {}};
}

mdToken TypeInfo::GetToken() {
    return myToken;
}

wstring TypeInfo::GetName() {
    return myName;
}

std::vector<BYTE> TypeInfo::GetRawInfo() {
    return myRaw;
}

std::vector<TypeInfo> TypeInfo::GetGenerics() {
    return myGenerics;
}

bool TypeInfo::IsRefType() {
    return myIsRefType;
}

void TypeInfo::SetRefType(bool isRefType) {
    myIsRefType = isRefType;
}

bool TypeInfo::IsBoxed() {
    return myIsBoxed;
}

bool TypeInfo::IsVoid() {
    return myIsVoid;
}

bool TypeInfo::IsGenericClassRef() {
    return myIsGenericClassRef;
}

bool TypeInfo::IsGenericMethodRef() {
    return myIsGenericMethodRef;
}

BYTE TypeInfo::GetTypeDef() {
    return myTypeDef;
}

ULONG TypeInfo::GetGenericRefNumber() {
    return myGenericRefNumber;
}
