"""Check owned paths, Python identifiers, Razor IDs and local file references."""
import ast
import os
from pathlib import Path
import re
import sys

WORKSPACE_ROOT = Path(__file__).resolve().parents[1]
EXCLUDED_DIRECTORIES = {
    '.git', '.vs', 'artifacts', 'bin', 'obj', 'node_modules', 'logs', 'secrets',
    'vendor', '__pycache__', 'test-results', 'playwright-report',
}
KEBAB_CASE = re.compile(r'[a-z0-9]+(?:-[a-z0-9]+)*')
SNAKE_CASE = re.compile(r'[a-z_][a-z0-9_]*')
PASCAL_CASE = re.compile(r'[A-Z][A-Za-z0-9]*(?:\.[A-Z][A-Za-z0-9]*)*')
STANDARD_FILES = {
    'README.md', 'CONTRIBUTING.md', 'LICENSE', 'Dockerfile', 'Directory.Build.props', 'global.json',
    'package.json', 'package-lock.json', 'packages.lock.json', 'playwright.config.mjs',
}


def source_paths():
    for directory, folders, filenames in os.walk(WORKSPACE_ROOT):
        folders[:] = sorted(folder for folder in folders if folder not in EXCLUDED_DIRECTORIES)
        for filename in sorted(filenames):
            if not filename.startswith('.'):
                yield Path(directory) / filename


def verify_paths(paths):
    errors = []
    seen = {}
    for path in paths:
        relative = path.relative_to(WORKSPACE_ROOT)
        folded = relative.as_posix().casefold()
        if folded in seen:
            errors.append(f'{relative}: case-insensitive collision with {seen[folded]}')
        seen[folded] = relative
        for directory in relative.parts[:-1]:
            if not (KEBAB_CASE.fullmatch(directory) or PASCAL_CASE.fullmatch(directory)):
                errors.append(f'{relative}: invalid directory name {directory}')
        if path.name in STANDARD_FILES:
            continue
        stem = path.stem
        if path.suffix == '.py':
            valid = SNAKE_CASE.fullmatch(stem)
        elif path.suffix in {'.cs', '.csproj', '.sln'}:
            valid = PASCAL_CASE.fullmatch(stem)
        elif path.suffix == '.cshtml':
            valid = PASCAL_CASE.fullmatch(stem.removeprefix('_'))
        elif path.suffix == '.sql':
            valid = re.fullmatch(r'(?:[VS]\d{3}__[a-z][a-z0-9_]*|[A-Z][A-Z0-9_]*)', stem)
        else:
            valid = KEBAB_CASE.fullmatch(stem.removesuffix('.spec'))
        if not valid:
            errors.append(f'{relative}: file name does not follow its language convention')
    return errors


def verify_python(path):
    errors = []
    for node in ast.walk(ast.parse(path.read_text(encoding='utf-8-sig'))):
        name = None
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            name = node.name
        elif isinstance(node, ast.Name) and isinstance(node.ctx, ast.Store):
            name = node.id
        elif isinstance(node, ast.arg):
            name = node.arg
        if name and not (SNAKE_CASE.fullmatch(name) or re.fullmatch(r'[A-Z][A-Z0-9_]*', name)):
            errors.append(f'{path.relative_to(WORKSPACE_ROOT)}:{node.lineno}: invalid Python identifier {name}')
    return errors


def main():
    paths = list(source_paths())
    owned_paths = paths
    errors = verify_paths(owned_paths)
    for path in owned_paths:
        if path.suffix == '.py':
            errors.extend(verify_python(path))
        elif path.suffix == '.cshtml':
            content = path.read_text(encoding='utf-8')
            for identifier in re.findall(r'\bid="([A-Za-z][A-Za-z0-9_-]*)"', content):
                if not KEBAB_CASE.fullmatch(identifier):
                    errors.append(f'{path.relative_to(WORKSPACE_ROOT)}: HTML id must use kebab-case: {identifier}')
        elif path.suffix == '.md':
            for target in re.findall(r'\]\(([^\s)]+)\)', path.read_text(encoding='utf-8')):
                if target.startswith(('#', 'http:', 'https:', 'mailto:')):
                    continue
                target_path = (path.parent / target.split('#')[0]).resolve()
                if not target_path.exists():
                    errors.append(f'{path.relative_to(WORKSPACE_ROOT)}: missing linked file {target}')
    for error in errors:
        print(error, file=sys.stderr)
    print(f'Workspace naming: {len(owned_paths)} owned files, {len(errors)} violations.')
    return 1 if errors else 0


if __name__ == '__main__':
    raise SystemExit(main())
