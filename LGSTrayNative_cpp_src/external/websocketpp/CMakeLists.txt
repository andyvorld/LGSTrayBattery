cmake_minimum_required (VERSION 3.14)

message("-- websocketpp")

set(Boost_USE_STATIC_LIBS ON) 
set(Boost_USE_MULTITHREADED ON)  
set(Boost_USE_STATIC_RUNTIME OFF) 
find_package(Boost 1.79.0 REQUIRED COMPONENTS filesystem system thread regex)

include(FetchContent)
FetchContent_Declare(
  websocketpp
  URL https://github.com/zaphoyd/websocketpp/archive/refs/tags/0.8.2.zip
)

FetchContent_GetProperties(websocketpp)
if(NOT websocketpp_POPULATED)
	FetchContent_Populate(websocketpp)
endif()

FetchContent_GetProperties(websocketpp)
if(NOT websocketpp_POPULATED)
  FetchContent_Populate(websocketpp)
  add_subdirectory(${websocketpp_SOURCE_DIR} ${websocketpp_BINARY_DIR} EXCLUDE_FROM_ALL)
endif()

add_library(websocketpp INTERFACE)
target_include_directories(websocketpp INTERFACE ${websocketpp_SOURCE_DIR})
target_link_libraries(websocketpp INTERFACE Boost::system Boost::thread Boost::regex)