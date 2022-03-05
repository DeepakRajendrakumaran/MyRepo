// ThreadingApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//


// References
// https://www.intel.com/content/www/us/en/developer/articles/guide/alder-lake-developer-guide.html
// https://www.intel.com/content/dam/develop/external/us/en/documents-tps/348851-optimizing-x86-hybrid-cpus.pdf

#include <windows.h>
#include <iostream>
#include <thread>
#include <processthreadsapi.h>
#include <ntddstor.h>
#include <mutex>

std::atomic<bool> myLock = { false };

void lock() { while (myLock.exchange(true, std::memory_order_acquire)); }

void unlock() { myLock.store(false, std::memory_order_release); }

int myVal = 0;

// Case 1
void myThread1_def() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX-1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_def() {
    int val = 6;
    for (long int i = 1; i <= INT_MAX-1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() <<  std::endl;
    unlock();
}

// Case 2 : The suggested Method
inline bool EnablePowerThrottling(HANDLE threadHandle)
{
    THREAD_POWER_THROTTLING_STATE throttlingState;
    RtlZeroMemory(&throttlingState, sizeof(throttlingState));

    throttlingState.Version = THREAD_POWER_THROTTLING_CURRENT_VERSION;
    throttlingState.ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;
    throttlingState.StateMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;

    return SetThreadInformation(threadHandle, ThreadPowerThrottling,
        &throttlingState,
        sizeof(throttlingState));
}

inline bool DisablePowerThrottling(HANDLE threadHandle)
{
    THREAD_POWER_THROTTLING_STATE throttlingState;
    RtlZeroMemory(&throttlingState, sizeof(throttlingState));

    throttlingState.Version = THREAD_POWER_THROTTLING_CURRENT_VERSION;
    throttlingState.ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;
    throttlingState.StateMask = 0;

    return SetThreadInformation(threadHandle, ThreadPowerThrottling,
        &throttlingState,
        sizeof(throttlingState));
}

    
void myThread1_case2() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_case2() {
    EnablePowerThrottling(GetCurrentThread());
    int val = 6;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    DisablePowerThrottling(GetCurrentThread());
}

// Case 3 : Set thread affinity - not suggested
void myThread1_case3() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_case3() {
    SetThreadAffinityMask(GetCurrentThread(), (1 << 22));
    int val = 6;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
}

// Case 4 : Other experiment - SetThreadIdealProcessor
void myThread1_case4() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_case4() {
    SetThreadIdealProcessor(GetCurrentThread(), 23);
    int val = 6;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
}

// Case 5 : Other experiment - SetThreadPriority
void myThread1_case5() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_case5() {
    SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_LOWEST);
    int val = 6;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
}

// Case 6 : Other experiment - SetThreadSelectedCpuSets
void myThread1_case6() {
    int val = 13;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 4) {
            val += 3;
        }
        else {
            val %= 7;
        }
    }
    lock();
    Sleep(1);
    myVal *= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
    return;
}


void myThread2_case6() {
    ULONG cpuId[] = { 17, 18, 19, 20, 21, 22, 23 };
    ULONG retID = 7;
    SetThreadSelectedCpuSets(GetCurrentThread(), (PULONG)cpuId, retID);
    int val = 6;
    for (long int i = 1; i <= INT_MAX - 1; ++i) {
        if (i % 3) {
            val += 2;
        }
        else {
            val %= 5;
        }
    }
    lock();
    Sleep(1);
    myVal -= val;
    std::cout << "After myThread2 Thread ID = " << std::this_thread::get_id() << std::endl;
    unlock();
}

// Some values differentiating E vs P cores. 
void PrintCPUInfo() {
    // Get total number (size) of elements in the data structure.
    ULONG size;
    GetSystemCpuSetInformation(nullptr, 0, &size, GetCurrentProcess(), 0);

    // Allocate data structures based on size returned from first call.
    std::unique_ptr<uint8_t[]> buffer(new uint8_t[size]);
    PSYSTEM_CPU_SET_INFORMATION cpuSets = reinterpret_cast<PSYSTEM_CPU_SET_INFORMATION>(buffer.get());
    PSYSTEM_CPU_SET_INFORMATION nextCPUSet;
    GetSystemCpuSetInformation(cpuSets, size, &size, GetCurrentProcess(), 0);

    nextCPUSet = cpuSets;

    // Iterate through each logical processor.
    for (DWORD offset = 0;
        offset + sizeof(SYSTEM_CPU_SET_INFORMATION) <= size;
        offset += sizeof(SYSTEM_CPU_SET_INFORMATION), nextCPUSet++)
    {
        // Make sure CPU Set Type is valid. (nextCPUSet->Type == CPU_SET_INFORMATION_TYPE::CpuSetInformation)
        std::cout << "Core ind = " << (int)nextCPUSet->CpuSet.CoreIndex << ", Log Ind = "
            << (int)nextCPUSet->CpuSet.LogicalProcessorIndex << " Efficiency Class = "
            << (int)(nextCPUSet->CpuSet.EfficiencyClass) << ", Sched Class = "
            << (int)nextCPUSet->CpuSet.SchedulingClass << ", Group = "
            << (int)nextCPUSet->CpuSet.Group << std::endl;
    }
}
int main(int count, char* args[]) {
    //PrintCPUInfo();
    std::thread threadE{ myThread2_def };
    std::thread threadP1{ myThread1_def };
    threadE.join();
    threadP1.join();
    std::cout << "myVal: " << myVal << std::endl;

    return 0;

}