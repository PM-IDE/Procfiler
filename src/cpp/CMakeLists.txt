cmake_minimum_required (VERSION 3.17)

project("Procfiler")

IF (WIN32)
    add_definitions(-DUNICODE -D_UNICODE -DWIN32 -DNDEBUG -D_WINDOWS -D_USRDLL -D_WIN32 -DWINVER=0x0602 -D_WIN32_WINNT=0x0602 -DWIN32_LEAN_AND_MEAN -D_CRT_SECURE_NO_WARNINGS)
ENDIF()

IF (UNIX AND NOT APPLE)
    add_compile_options(-std=c++11 -fPIC -fms-extensions -stdlib=libstdc++)
    add_compile_options(-DBIT64 -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE)
    add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined -Wno-pragma-pack -g -Wall -Wno-null-conversion)
ENDIF()

IF (APPLE)
    add_compile_options(-std=c++11 -fPIC -fms-extensions -stdlib=libc++)
    add_compile_options(-DHOST_64BIT -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE -DHOST_ARM64)
    add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined -Wno-pragma-pack)
ENDIF()


add_library("Procfiler.static" STATIC
    corprofiler/ProcfilerCorProfilerCallback.cpp
    ClassFactory.cpp)

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