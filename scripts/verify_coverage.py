"""Check rule-engine source-line coverage."""
from pathlib import Path
import json
import sys
import xml.etree.ElementTree as ET

WORKSPACE_ROOT = Path(__file__).resolve().parents[1]
RULE_FILES = {
    "FaturaKuralBase.cs", "ProvizyonKurali.cs", "TaniKurali.cs",
    "IslemTaniKurali.cs", "IslemKurali.cs", "TutarKurali.cs",
    "BelgeKurali.cs", "IlacKurali.cs", "SureKurali.cs", "KuralEngine.cs",
}
results_directory = Path(sys.argv[1]) if len(sys.argv) > 1 else WORKSPACE_ROOT / "artifacts/test-results"
coverage_files = sorted(results_directory.rglob("coverage.cobertura.xml"), key=lambda path: path.stat().st_mtime, reverse=True)
if not coverage_files:
    raise SystemExit('Coverage report missing. Run unit tests with --collect:"XPlat Code Coverage".')

class_results = []
covered_lines = set()
total_lines = set()
observed_files = set()
for class_element in ET.parse(coverage_files[0]).findall(".//class"):
    filename = class_element.attrib["filename"].replace("\\", "/")
    if Path(filename).name not in RULE_FILES:
        continue
    observed_files.add(Path(filename).name)
    for line in class_element.findall("./lines/line"):
        key = (filename, int(line.attrib["number"]))
        total_lines.add(key)
        if int(line.attrib["hits"]) > 0:
            covered_lines.add(key)
    class_results.append({"className": class_element.attrib["name"], "lineRate": float(class_element.attrib["line-rate"]), "branchRate": float(class_element.attrib["branch-rate"])})
result = {
    "sourceDirectory": "src/MedulaOnKontrol.Application/Rules",
    "totalLines": len(total_lines), "coveredLines": len(covered_lines),
    "lineCoveragePercent": 100 * len(covered_lines) / len(total_lines) if total_lines else 0,
    "classes": class_results,
}
output_directory = WORKSPACE_ROOT / "artifacts/verification"
output_directory.mkdir(parents=True, exist_ok=True)
(output_directory / "coverage.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(f'Kural line coverage: {result["lineCoveragePercent"]:.2f}% ({len(covered_lines)}/{len(total_lines)})')
if observed_files != RULE_FILES or not total_lines or covered_lines != total_lines:
    raise SystemExit(1)
