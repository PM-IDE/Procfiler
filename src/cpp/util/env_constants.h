#ifndef PROCFILER_ENV_VARS_H
#define PROCFILER_ENV_VARS_H

#include <string>

const std::string trueEnvVarValue = "1";

const std::string shadowStackDebugSavePath = "PROCFILER_DEBUG_SAVE_CALL_STACKS_PATH";
const std::string enableConsoleLogging = "PROCFILER_ENABLE_CONSOLE_LOGGING";
const std::string binaryStackSavePath = "PROCFILER_BINARY_SAVE_STACKS_PATH";
const std::string eventPipeSaveShadowStack = "PROCFILER_EVENT_PIPE_SAVE_STACKS";

const std::string filterMethodsRegex = "PROCFILER_FILTER_METHODS_REGEX";
const std::string filterMethodsDuringRuntime = "PROCFILER_FILTER_METHODS_DURING_RUNTIME";
const std::string useSeparateBinStacksFiles = "PROCFILER_USE_SEPARATE_BINSTACKS_FILES";


bool IsEnvVarDefined(const std::string& envVarName);
bool TryGetEnvVar(const std::string& envVarName, std::string& value);

#endif