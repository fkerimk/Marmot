#!/usr/bin/env python

import os, subprocess, shutil, re, glob

buildLinux = True
buildWin = False

def RunCmd(cmd): return subprocess.run(cmd, shell=True)

def BuildRaylib():
    
    os.chdir("lib/raylib")
    
    config_path = "src/config.h"
    if os.path.exists(config_path):
        with open(config_path, "r+") as f:
            content = re.sub(r'(#define\s+SUPPORT_GPU_SKINNING\s+)0', r'\g<1>1', f.read())
            f.seek(0); f.write(content); f.truncate()

    os.makedirs("build", exist_ok=True)
    os.chdir("build")
    
    cpus = os.cpu_count() or 1
    
    # Linux build
    RunCmd("cmake -S .. -B linux -DBUILD_SHARED_LIBS=ON -DGLFW_BUILD_X11=ON -DGLFW_BUILD_WAYLAND=ON")
    RunCmd(f"cmake --build linux -j{cpus}")
    
    # Windows build
    RunCmd("cmake -S .. -B win -DCMAKE_SYSTEM_NAME=Windows -DCMAKE_C_COMPILER=x86_64-w64-mingw32-gcc -DCMAKE_CXX_COMPILER=x86_64-w64-mingw32-g++ -DBUILD_SHARED_LIBS=ON")
    RunCmd(f"cmake --build win -j{cpus}")
    
    os.chdir("../../../")

def UpdateRaylib():
    
    if not os.path.exists("lib/raylib/build/linux/raylib/libraylib.so") or not os.path.exists("lib/raylib/build/win/raylib/libraylib.dll"):
        BuildRaylib(); return
        
    print("Copying raylib...")
    shutil.copy2("lib/raylib/build/linux/raylib/libraylib.so.6.0.0", "build/lib/libraylib.so")
    shutil.copy2("lib/raylib/build/win/raylib/libraylib.dll", "build/lib/libraylib.dll")

def BuildEngine():
    
    print("Building engine...")
    
    if buildLinux: RunCmd("dotnet publish src/slnx/marmot.slnx -r linux-x64 -v:q")
    if buildWin: RunCmd("dotnet publish src/slnx/marmot.slnx -r win-x64 -v:q")

def CleanBuild():
    
    print("Cleaning build...")
    
    for ext in ("*.dbg", "*.pdb", "*.json"):
        for f in glob.glob(f"build/{ext}"): os.remove(f)
    
    if os.path.exists("build/cli"): os.rename("build/cli", "build/marmot")
    if os.path.exists("build/cli.exe"): os.rename("build/cli.exe", "build/marmot.exe")

def Build():
    
    print("Building...")
    
    shutil.rmtree("build", ignore_errors=True)
    
    BuildEngine()
    UpdateRaylib()
    
    print("Copying resources...")
    shutil.copytree("src/res", "build/res", dirs_exist_ok=True)
    
    print("Copying libraries...")
    os.makedirs("build/lib/py", exist_ok=True)
    for lib in glob.glob("lib/py/*.py"): shutil.copy(lib, "build/lib/py")
    
    CleanBuild()

if __name__ == "__main__":
    Build()