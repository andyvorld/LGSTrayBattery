import glob
import os
import os.path
import subprocess
import xml.etree.ElementTree as ET
import zipfile

PUB_PROFILES = [
    ('Framedep', ''),
    ('Standalone', '-standalone')
]

FILE_TYPES = [
    '*.exe',
    '*.pdb',
    '*.dll',
    '*.ini'
]

TARGET_PROJ = 'LGSTrayUI'
PROJ_FILE = f'./{TARGET_PROJ}/{TARGET_PROJ}.csproj'
proj = ET.parse(PROJ_FILE).getroot()
TARGET_VER = proj.findall('./PropertyGroup/Version')[0].text

def fileList(zipFolder):
    output = list()

    for fileType in FILE_TYPES:
        output += (glob.glob(os.path.join(zipFolder, fileType), recursive=True))

    return output

def createZip(zipPath, zipFolder):
    with zipfile.ZipFile(zipPath, 'w', zipfile.ZIP_DEFLATED) as zip:
        for file in fileList(zipFolder):
            zip.write(file, os.path.basename(file))

def main():
    publishRoot = os.path.join('./bin/Release/Publish/win-x64')
    
    for profile, zip_suffix in PUB_PROFILES:
        safe_ver = TARGET_VER.replace('.', '_')

        for proj in ["LGSTrayHID", "LGSTrayUI"]:
            subprocess.run(
                ["dotnet", "publish", f"{proj}/{proj}.csproj", f"/p:PublishProfile={profile}"],
                shell=False
            )

        zipName = f'Release_v{safe_ver}{zip_suffix}.zip'

        zipPath = os.path.join(publishRoot+"/..", zipName)
        zipFolder = os.path.join(publishRoot, profile)

        print("\n---")
        print(f"Zipping {profile} ...")
        createZip(zipPath, zipFolder)
        print("---")

if __name__ == "__main__":
    main()
    input("Packaging done.")