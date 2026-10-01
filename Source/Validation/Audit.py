"""Check imported content, definitions, and the production delivery layout."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    imports = json.loads((ROOT / "Source/Documentation/Import-Manifest.json").read_text(encoding="utf-8"))
    missing, changed_assets, altered_originals, changed_sources = [], [], [], []
    original_count = 0
    for module in imports:
        for entry in module["files"]:
            target = ROOT / entry["destination"]
            if not target.is_file():
                missing.append(entry["destination"])
            elif digest(target) != entry["sha256"]:
                collection = changed_assets if target.suffix.lower() in (".png", ".ogg", ".wav", ".jpg") else changed_sources
                collection.append(entry["destination"])
            original = ROOT.parent / module["folder"] / entry["source"]
            if original.is_file():
                original_count += 1
                if digest(original) != entry["sha256"]:
                    altered_originals.append(str(original))
    definitions, malformed, duplicates = {}, [], []
    for folder in ("Defs", "Patches", "Languages", "About"):
        for file in (ROOT / folder).rglob("*.xml"):
            try:
                document = ET.parse(file)
            except ET.ParseError as exc:
                malformed.append(f"{file.relative_to(ROOT)}: {exc}")
                continue
            if folder != "Defs":
                continue
            for node in document.getroot():
                name = node.findtext("defName")
                if name:
                    key = (node.tag, name)
                    if key in definitions:
                        duplicates.append(f"{key}: {definitions[key]}, {file.relative_to(ROOT)}")
                    definitions[key] = str(file.relative_to(ROOT))
    dlls = sorted(p.relative_to(ROOT).as_posix() for p in (ROOT / "Assemblies").rglob("*.dll"))
    production = ROOT / "Assemblies/MarvelUnification.dll"
    report = {
        "modules": len(imports),
        "importedFiles": sum(len(m["files"]) for m in imports),
        "originalFilesChecked": original_count,
        "originalFilesAltered": altered_originals,
        "missingImportedFiles": missing,
        "modifiedTexturesOrSounds": changed_assets,
        "modifiedSourceOrDefinitions": changed_sources,
        "malformedXml": malformed,
        "duplicateDefinitions": duplicates,
        "runtimeAssemblies": dlls,
        "productionSha256": digest(production) if production.is_file() else None,
        "testCodeInProduction": production.is_file() and b"MarvelUnification.Validation" in production.read_bytes(),
    }
    passed = not any((missing, changed_assets, altered_originals, malformed, duplicates, report["testCodeInProduction"])) and dlls == ["Assemblies/MarvelUnification.dll"]
    report["passed"] = passed
    print(json.dumps(report, indent=2))
    raise SystemExit(0 if passed else 1)


if __name__ == "__main__":
    main()
