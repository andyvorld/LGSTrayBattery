# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#
cmake_minimum_required (VERSION 3.14)
include(FetchContent)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED TRUE)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release")

project("libHidTest")

add_subdirectory("${CMAKE_SOURCE_DIR}/external/hidapi-win")

add_library(LGSTrayNative_lib SHARED
	"src/LGSTrayHID_lib.cpp" 
	"src/HIDDevice.cpp" 
	"src/HIDPPMsg.cpp"
	"src/LogiDevice.cpp" "src/LGSTrayHID_common.cpp" "src/Battery1001.cpp" "src/Battery1000.cpp" "src/Battery1004.cpp" "src/MapWrapper.cpp" "src/hid_io_debug.cpp"
)

target_link_libraries(
	LGSTrayNative_lib PUBLIC
	hidapi_win
)
target_include_directories(
	LGSTrayNative_lib PUBLIC 
	"include"
)

if(MSVC)
	target_compile_options(LGSTrayNative_lib 
		PRIVATE "/external:anglebrackets"
		PRIVATE "/external:W0"
	)
endif(MSVC)

add_custom_command (TARGET LGSTrayNative_lib POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
    $<TARGET_FILE:hidapi_win> $<TARGET_FILE_DIR:LGSTrayNative_lib>
)

add_executable(lib_test
	"src/main.cpp"
)

target_include_directories(
	lib_test
	PUBLIC "include"
)

target_link_libraries(
	lib_test
	PUBLIC LGSTrayNative_lib
)