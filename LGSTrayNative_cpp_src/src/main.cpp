#include <iostream>
#include <thread>

#include "LGSTrayHID_lib.hpp"

using namespace std::chrono_literals;
std::string temp;

int main() {

	int asdf;
	asdf = 0;
	Register_battery_update_cb([](const char* dev_id, int bat_percent, bool charging, double mileage, int bat_voltage) {
		std::cout << "CALLBACK" << std::endl;

		std::cout << "Dev id: " << dev_id << std::endl;
		std::cout << "bat_percent: " << bat_percent << std::endl;
		std::cout << "charging: " << charging << std::endl;
		std::cout << "mileage: " << mileage << std::endl;
		std::cout << "bat_voltage: " << bat_voltage << std::endl;
	});

	Register_device_ready_cb([](const char* dev_id, int dev_type, const char* dev_name) {
		std::cout << "CALLBACK" << std::endl;

		std::cout << "Dev id: " << dev_id << std::endl;
		std::cout << "dev_type: " << dev_type << std::endl;
		std::cout << "dev_name: " << dev_name << std::endl;

		temp = std::string(dev_id);
	});

	Load_devices();

	std::this_thread::sleep_for(5s);

	for (int i = 0; i < 3; ++i) {
		Update_device_battery(temp.c_str());
	}

	std::this_thread::sleep_for(5s);

	Load_devices();

	std::this_thread::sleep_for(5s);

	for (int i = 0; i < 3; ++i) {
		Update_device_battery(temp.c_str());
	}

	std::this_thread::sleep_for(5s);

	return 0;
}