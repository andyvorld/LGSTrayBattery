cmake_minimum_required (VERSION 3.14)

message("-- nlohmann_json")

FetchContent_Declare(json 
	URL https://github.com/nlohmann/json/releases/download/v3.11.2/json.tar.xz
)

FetchContent_MakeAvailable(json)