#!/usr/bin/env python

import os, sys, subprocess, shutil, re, pyclip, pty, glob

PrimaryColor = "#ffe847"
SecondaryColor = "#333300"
IdleColor = "#fac003"
PassColor = "#61ff2b"
FailColor = "#ff632b"
HeaderColor = PassColor

header = ""

backText = "󱞾 Back"

def GetColorScheme(): return (
    
    f"fg:#ffffff,bg:-1,hl:{PassColor},fg+:{PrimaryColor},bg+:{SecondaryColor},"
    f"hl+:{PassColor},info:#ffffff,prompt:{PrimaryColor},pointer:{PrimaryColor},"
    f"marker:{PrimaryColor},spinner:{PrimaryColor},header:{HeaderColor},"
    f"border:{SecondaryColor},label:#ffffff,query:#ffffff,gutter:-1"
)

def RunCmd(cmd, input="", capture=False, shell=True, stream=False):
    if not capture:
        return subprocess.run(cmd, shell=shell, input=input, text=True)

    master, slave = pty.openpty()
    proc = subprocess.Popen(cmd, shell=shell, stdin=subprocess.PIPE,
                             stdout=slave, stderr=slave, text=True)
    os.close(slave)

    if input:
        proc.stdin.write(input)
    proc.stdin.close()

    chunks = []
    try:
        while data := os.read(master, 4096):
            chunks.append(data)
            if stream:
                sys.stdout.write(data.decode("utf-8", errors="ignore"))
                sys.stdout.flush()
    except OSError:
        pass

    os.close(master)
    proc.wait()

    raw = b"".join(chunks).decode("utf-8", errors="ignore")
    raw = re.sub(r'\x1b\[\?[0-9;]*[a-zA-Z]|\x1b[=>]', '', raw)
    proc.stdout = raw
    
    return proc

def SetHeader(text, color):
    
    global header, HeaderColor
    
    header = text
    HeaderColor = color

def FzfMenu(data, index=False, back=False, extra_opts=None):

    lines = [str(item) for item in data] if isinstance(data, list) else str(data).strip().splitlines()
    lines = [l for l in lines if re.sub(r'\x1b\[[0-9;]*m', '', l).strip()]

    if back: lines.insert(0, f"\033[94m{backText}\033[0m")
    if index: lines = [f"{i+1}\t{line}" for i, line in enumerate(lines)]

    fzf = [
        "fzf", "--ansi", "--height=100%", "--border=rounded", "--layout=reverse", "--padding=1",
        "--prompt=🔍 ", "--border-label= Marmot 🐿️ ", "--border-label-pos=3",
        f"--color={GetColorScheme()}",
        "--info=inline", "--info-command=printf",
    ]

    if header: fzf.extend(["--header", header])
    if index: fzf.extend(["--with-nth=2..", "--accept-nth=1"])
    if extra_opts: fzf.extend(extra_opts)

    output = RunCmd(fzf, "\n".join(lines) + "\n", True, False).stdout.strip()
    
    SetHeader("", PassColor)
    
    return output

def FzfInput():
    
    fzf = [
        "fzf", "--print-query", "--height=100%", "--border=rounded", "--layout=reverse", "--padding=1",
        "--prompt=Project Name: ",
        f"--color={GetColorScheme()}",
        "--no-info", "--no-header",
        "--bind=preview-scroll-up:ignore,preview-scroll-down:ignore,scroll-up:ignore,scroll-down:ignore",
        "--preview=chafa --stretch -s {$FZF_PREVIEW_COLUMNS}x{$FZF_PREVIEW_LINES} tui/new_project.jpg",
        "--preview-window=down,99%,border-none,noinfo",
    ]
    
    output = RunCmd(fzf, capture=True, shell=False).stdout.strip()
    lines = output.splitlines() if output else []
    
    return lines[0] if lines else ""

def PickAndCopy(data, extra_opts=None):
    
    choice = FzfMenu(data, back=True, extra_opts=extra_opts)
    
    if choice and choice != backText:
        pyclip.copy(choice)
        SetHeader(f'Copied "{choice}" to clipboard!', PassColor)

def BuildRaylib():
    
    if not os.path.exists("lib/raylib"):
        RunCmd("git clone https://github.com/raysan5/raylib.git lib/raylib")

    os.chdir("lib/raylib")
    RunCmd("git pull")

    config_path = "src/config.h"
    if os.path.exists(config_path):
        with open(config_path, "r+") as f:
            content = re.sub(r'(#define\s+SUPPORT_GPU_SKINNING\s+)0', r'\g<1>1', f.read())
            f.seek(0)
            f.write(content)
            f.truncate()

    shutil.rmtree("build", ignore_errors=True)
    os.makedirs("build", exist_ok=True)
    os.chdir("build")

    RunCmd("cmake .. -DBUILD_SHARED_LIBS=ON -DGLFW_BUILD_X11=ON -DGLFW_BUILD_WAYLAND=ON")
    RunCmd(f"cmake --build . -j{os.cpu_count() or 1}")
    os.chdir("../../../")

def UpdateRaylib():
    
    if not os.path.exists("lib/raylib"):
        BuildRaylib()
        return
    
    else:
        os.chdir("lib/raylib")
        RunCmd("git remote update > /dev/null")
        local = RunCmd("git rev-parse @", capture=True).stdout.strip()
        remote = RunCmd("git rev-parse @{u}", capture=True).stdout.strip()
        os.chdir("../..")
        
        if local != remote:
            print("Updating Raylib...")
            BuildRaylib()
            
    print("Copying raylib...")
    shutil.copy2("lib/raylib/build/raylib/libraylib.so.6.0.0", "build/lib/libraylib.so")

def BuildEngine():
    
    runtime="linux-x64"
        
    shutil.rmtree("build", ignore_errors=True)
    
    print("Building engine...")
    RunCmd(f"dotnet publish src/slnx/marmot.slnx -r {runtime} -v:q")
    
    print("Copying resources...")
    shutil.copytree("src/res", "build/res")
    
    print("Copying libraries...")
    for lib in glob.glob("lib/*.py"): shutil.copy(lib, "build/lib")
    
    #shutil.copytree('src/py', 'build/lib', dirs_exist_ok=True)

def CleanBuild():
    
    print("Moving libraries...")
    for dll in set(glob.glob("build/*.dll")) - {"build/marmot.dll"}: shutil.move(dll, "build/lib")
    for so in glob.glob("build/*.so"): shutil.move(so, "build/lib")
    
    print("Cleaning build...")
    list(map(os.remove, glob.glob("build/*.dbg")))
    list(map(os.remove, glob.glob("build/*.pdb")))
    list(map(os.remove, glob.glob("build/*.json")))
    #list(map(os.remove, set(glob.glob("build/*.dll")) - {"build/marmot.dll"}))
    
    os.rename("build/cli", "build/marmot")

# Update command
def Update():
    print("Updating...")
    BuildEngine()
    UpdateRaylib()

# Cli
def EngineCli(args="", stream=False):
    if not os.path.exists("build/marmot"): Update()
    return RunCmd(f"build/marmot {args}", capture=True, stream=stream).stdout.strip()

def ProjectCli(folder, args="", stream=False): return EngineCli(f"project \"{folder}\" {args}", stream=stream)

# Project list menu
def ProjectList():
    
    choice = FzfMenu(EngineCli("project list"), index=True, back=True)
    
    if choice and choice != "1":
        Project(EngineCli(f"project {int(choice) - 2} get folder"))
        
    MainMenu()
    
# Project menu
def Project(folder):
    
    SetHeader(f"{ProjectCli(folder, "get name")} ({ProjectCli(folder, "get path")})", IdleColor)

    match FzfMenu([
        "Run",
        "Build",
        "Sync"
    ],back=True):
        case "Run": ProjectCli(folder, "run", stream=True); Project(folder)
        case "Build": ProjectCli(folder, "build", stream=True); Project(folder)
        case "Sync": ProjectCli(folder, "sync", stream=True); Project(folder)
        case _: ProjectList()

# New project menu
def NewProject():
    msg = EngineCli(f"project create {FzfInput()}")
    if (msg): SetHeader(msg.splitlines()[0], FailColor)

# About menu
def About():
    PickAndCopy(EngineCli("about"))

# Main menu
def MainMenu():
    
    match FzfMenu([
        "Projects",
        "New Project",
        "Update",
        "About",
        "\033[91mExit\033[0m"
    ],index=True):
        case "1": ProjectList()
        case "2": NewProject()
        case "3": Update()
        case "4": About()
        case _: sys.exit(0)
        
    MainMenu()

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] in ["--update", "-u", "update"]:
        Update()
    else:
        MainMenu()