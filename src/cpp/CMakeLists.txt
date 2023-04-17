cmake_minimum_required(VERSION 3.14.5)

add_compile_options(-fpermissive)
project(Procfiler)

set(SOURCES
    procfiler.def
    ClassFactory.cpp
    dllmain.cpp
    corprofiler/ProcfilerCorProfilerCallback.cpp guids.cpp)

include_directories(./3rdparty/coreclr/pal/prebuilt/inc)

if(NOT WIN32)
    include_directories(./3rdparty/coreclr/pal/inc/rt ./3rdparty/coreclr/pal/inc ./3rdparty/coreclr/inc)
    add_compile_options(-DPAL_STDCPP_COMPAT)
    if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-null-arithmetic)
    else(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-conversion-null -Wno-pointer-arith)
    endif(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
endif(NOT WIN32)

# add the executable
add_library(Procfiler SHARED ${SOURCES})

if(WIN32)
    set(DEPENDENCIES Advapi32.lib oleaut32.lib Ole32.lib)
else(WIN32)
    set(DEPENDENCIES pthread dl)
endif(WIN32)

target_link_libraries(Procfiler PUBLIC ${DEPENDENCIES})

install(TARGETS Procfiler DESTINATION bin)

#if(WIN32)
#    install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}> DESTINATION bin OPTIONAL)
#endif(WIN32)
