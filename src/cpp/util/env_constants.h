#ifndef PROCFILER_ENV_VARS_H
#define PROCFILER_ENV_VARS_H

#include <string>

const std::string shadowStackDebugSavePath = "PROCFILER_DEBUG_SAVE_CALL_STACKS_PATH";
const std::string enableConsoleLogging = "PROCFILER_ENABLE_CONSOLE_LOGGING";
const std::string binaryStackSavePath = "PROCFILER_BINARY_SAVE_STACKS_PATH";


bool IsEnvVarDefined(const std::string& envVarName);
bool TryGetEnvVar(const std::string& envVarName, std::string& value);

#endif