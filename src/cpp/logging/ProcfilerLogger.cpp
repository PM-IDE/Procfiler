#include <cstdio>
#include "ProcfilerLogger.h"
#include "../util/env_constants.h"

void ProcfilerLogger::Log(const std::string& message) {
    if (!myIsEnabled) return;

    auto patchedMessage = new char[message.length() + 1];
    auto i = 0;
    for (;i < message.length(); ++i) {
        patchedMessage[i] = message[i];
    }

    patchedMessage[i] = '\n';

    printf(patchedMessage);

    delete[] patchedMessage;
}

ProcfilerLogger::ProcfilerLogger() {
    auto enableLoggingEnv = std::getenv(enableConsoleLogging.c_str());
    myIsEnabled = enableLoggingEnv != nullptr && std::string(enableLoggingEnv) == "1";
}
