import zipfile
import os
import os.path
import glob
import xml.etree.ElementTree as ET

TARGET_PROJ = 'LGSTrayGUI'
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

proj = ET.parse(f'./{TARGET_PROJ}/{TARGET_PROJ}.csproj').getroot()
TARGET_VER = proj[0].findtext('Version')

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
    solRoot = os.path.dirname(__file__)
    projRoot = os.path.join(solRoot, TARGET_PROJ)

    for profile, zip_suffix in PUB_PROFILES:
        publishRoot = os.path.join(projRoot, 'bin/Publish')
        safe_ver = TARGET_VER.replace('.', '_')
        zipName = f'Release_v{safe_ver}{zip_suffix}.zip'

        zipPath = os.path.join(publishRoot, zipName)
        zipFolder = os.path.join(publishRoot, profile)

        createZip(zipPath, zipFolder)

if __name__ == "__main__":
    print("Build the publish profiles within Visual Studio first.")
    main()