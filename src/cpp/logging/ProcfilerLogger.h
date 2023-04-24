#include "iostream"

class ProcfilerLogger {
private:
    bool myIsEnabled;

public:
    ProcfilerLogger();

    void Log(const std::string& message);
};
