cmake_minimum_required (VERSION 3.17)

project("Procfiler")

IF (APPLE)
    add_compile_options(-std=c++11 -fPIC -fms-extensions -stdlib=libc++)
    add_compile_options(-DHOST_64BIT -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE -DHOST_ARM64)
    add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined -Wno-pragma-pack)
ENDIF()

add_library("Procfiler.static" STATIC
    corprofiler/ProcfilerCorProfilerCallback.cpp corprofiler/ProcfilerCorProfilerCallback.h)

target_include_directories(
    "Procfiler.static"
    PUBLIC ./3rdparty/inc/
    PUBLIC .
)

IF (WIN32)
ELSE()
    target_include_directories(
        "Procfiler.static"
        PUBLIC ./3rdparty/pal/inc/rt/
        PUBLIC ./3rdparty/pal/inc/
        PUBLIC ./3rdparty/pal/prebuilt/inc
        PUBLIC ./3rdparty/pal/pal/inc/
    )
ENDIF()

IF (WIN32)
    add_library("Procfiler" SHARED
        profiler.def
        dllmain.cpp
    )
ELSE()
    add_library("Procfiler" SHARED
        dllmain.cpp
    )
ENDIF()

target_link_libraries("Procfiler" "Procfiler.static")