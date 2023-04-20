
#include <cstdio>
#include "ProcfilerLogger.h"

void ProcfilerLogger::Log(const std::string& message) {
    auto patchedMessage = new char[message.length() + 1];
    auto i = 0;
    for (;i < message.length(); ++i) {
        patchedMessage[i] = message[i];
    }

    patchedMessage[i] = '\n';

    printf(patchedMessage);

    delete[] patchedMessage;
}