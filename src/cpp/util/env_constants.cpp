#include "env_constants.h"

bool IsEnvVarDefined(const std::string& envVarName) {
    return std::getenv(envVarName.c_str()) != nullptr;
}

bool TryGetEnvVar(const std::string& envVarName, std::string& value) {
    auto envVar = std::getenv(envVarName.c_str());
    bool isEnvVarDefined = envVar != nullptr;
    value = isEnvVarDefined ? std::string(envVar) : "";
    return isEnvVarDefined;
}
