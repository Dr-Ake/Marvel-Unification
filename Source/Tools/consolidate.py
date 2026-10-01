"""Import the nine source modules without their binaries or build artifacts."""
from pathlib import Path
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
MODULES = {
    'Cyclops': 'Cyclops-X-Gene',
    'Deadpool': 'Rimworld-Deadpool-healing',
    'Gambit': 'RimWorld-Gambit',
    'Hulk': 'Rimworld-Incredible-Hulk',
    'JeanGrey': 'Rimworld-Jean-Gray',
    'Magneto': 'Rimworld-Magneto-X-Gene',
    'MultipleMan': 'Rimworld-Multiple-Man-X-Gene',
    'Nightcrawler': 'Rimworld-Nightcrawler-X-Gene',
    'Storm': 'Rimworld-Storm',
}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    records, collisions, defs = [], [], {}
    for module, folder in MODULES.items():
        src = ROOT.parent / folder
        files = []
        for file in sorted((src / 'Source').rglob('*.cs')):
            rel = file.relative_to(src / 'Source')
            if any(p in ('obj', 'bin', 'packages') for p in rel.parts) or file.name == 'AssemblyInfo.cs':
                continue
            files.append((file, ROOT / 'Source' / 'Modules' / module / rel))
        for content in ('Defs', 'Patches', 'Textures', 'Sounds', 'Languages'):
            for base in (src, src / '1.6'):
                directory = base / content
                if not directory.is_dir():
                    continue
                for file in sorted(directory.rglob('*')):
                    if not file.is_file() or file.name == '.DS_Store':
                        continue
                    rel = file.relative_to(directory)
                    dest = ROOT / content / rel if content in ('Textures', 'Sounds', 'Languages') else ROOT / content / module / rel
                    files.append((file, dest))
        imported = []
        for file, dest in files:
            if dest.exists() and digest(file) != digest(dest):
                collisions.append({'module': module, 'source': str(file), 'destination': str(dest)})
                continue
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(file, dest)
            imported.append({'source': str(file.relative_to(src)).replace('\\', '/'), 'destination': str(dest.relative_to(ROOT)).replace('\\', '/'), 'sha256': digest(file)})
            if dest.suffix == '.xml' and 'Defs' in dest.relative_to(ROOT).parts:
                for node in ET.parse(dest).getroot():
                    key = (node.tag, node.findtext('defName'))
                    if key[1]:
                        defs.setdefault(key, []).append(str(dest.relative_to(ROOT)))
        records.append({'module': module, 'folder': folder, 'packageId': ET.parse(src / 'About' / 'About.xml').findtext('packageId'), 'files': imported})
    out = ROOT / 'Source' / 'Documentation'
    out.mkdir(parents=True, exist_ok=True)
    (out / 'Import-Manifest.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    duplicates = {f'{k[0]}:{k[1]}': v for k, v in defs.items() if len(v) > 1}
    print(json.dumps({'modules': len(records), 'files': sum(len(m['files']) for m in records), 'conflictingFiles': collisions, 'duplicateDefs': duplicates}, indent=2))

if __name__ == '__main__':
    main()
