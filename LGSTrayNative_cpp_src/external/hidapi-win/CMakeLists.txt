cmake_minimum_required (VERSION 3.14)

message("-- hidapi-win")

include(FetchContent)
FetchContent_Declare(
  hidapi_win
  URL https://github.com/libusb/hidapi/releases/download/hidapi-0.12.0/hidapi-win.zip
)

FetchContent_GetProperties(hidapi_win)
if(NOT hidapi_win_POPULATED)
	FetchContent_Populate(hidapi_win)
endif()

if(!MSVC)
message(FATAL_ERROR "Toolchain not setup for non MSVC builds")
endif()

if(CMAKE_SIZEOF_VOID_P GREATER 4)
	message("    x64 hidapi")
	set(hidapi_win_LIB_DIR "${hidapi_win_SOURCE_DIR}/x64")
else()
	message("    x86 hidapi")
	set(hidapi_win_LIB_DIR "${hidapi_win_SOURCE_DIR}/x86")
endif()

add_library(hidapi_win INTERFACE)
target_include_directories(hidapi_win INTERFACE "${hidapi_win_SOURCE_DIR}/include/")
target_link_libraries(hidapi_win INTERFACE "${hidapi_win_LIB_DIR}/hidapi.lib")
file(COPY "${hidapi_win_LIB_DIR}/hidapi.dll" DESTINATION ${CMAKE_BINARY_DIR})