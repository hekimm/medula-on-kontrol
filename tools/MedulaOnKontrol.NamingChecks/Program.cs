using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var workspaceRoot = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
if (!File.Exists(Path.Combine(workspaceRoot, "MedulaOnKontrol.sln")))
    throw new ArgumentException("Pass the workspace root containing MedulaOnKontrol.sln.");

var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", "artifacts", "logs", "secrets", ".git", ".vs", "__pycache__" };
var sourceFiles = new[] { "src", "tests", "tools" }.SelectMany(folder => EnumerateFiles(Path.Combine(workspaceRoot, folder)))
    .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)).ToArray();
var errors = new List<string>();
var declarationCount = 0;
foreach (var sourceFile in sourceFiles)
{
    var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile)).GetRoot();
    var relativePath = Path.GetRelativePath(workspaceRoot, sourceFile).Replace('\\', '/');
    var pathParts = relativePath.Split('/');
    var projectName = pathParts[1];
    var expectedNamespace = string.Join('.', new[] { projectName }.Concat(pathParts.Skip(2).SkipLast(1)));
    if (projectName.EndsWith("Tests", StringComparison.Ordinal)) expectedNamespace = projectName;
    foreach (var namespaceNode in syntax.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        if (namespaceNode.Name.ToString() != expectedNamespace) Report(namespaceNode.Name.GetFirstToken(), $"namespace must be {expectedNamespace}");

    foreach (var node in syntax.DescendantNodes())
    {
        switch (node)
        {
            case BaseTypeDeclarationSyntax type:
                Check(type.Identifier, type is InterfaceDeclarationSyntax ? "I[A-Z][A-Za-z0-9]*" : "[A-Z][A-Za-z0-9]*", "type PascalCase / interface IPascalCase");
                if (type.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax && Path.GetFileName(sourceFile).Split('.')[0] != type.Identifier.ValueText)
                    Report(type.Identifier, "file name must match its top-level type");
                break;
            case EnumMemberDeclarationSyntax member:
                Check(member.Identifier, "[A-Z][A-Za-z0-9]*", "enum member PascalCase");
                if (member.Identifier.ValueText.Length > 2 && member.Identifier.ValueText.All(character => !char.IsLetter(character) || char.IsUpper(character)))
                    Report(member.Identifier, "enum members must not use all capitals");
                break;
            case MethodDeclarationSyntax method:
                Check(method.Identifier, "[A-Z][A-Za-z0-9]*", "method PascalCase");
                var returnName = method.ReturnType.ToString().Split('<')[0].Split('.').Last();
                var isController = method.Ancestors().OfType<ClassDeclarationSyntax>().Any(type => type.Identifier.ValueText.EndsWith("Controller", StringComparison.Ordinal));
                if (returnName is "Task" or "ValueTask" && !isController && !method.Identifier.ValueText.EndsWith("Async", StringComparison.Ordinal))
                    Report(method.Identifier, "Task/ValueTask methods require Async suffix");
                break;
            case LocalFunctionStatementSyntax function:
                Check(function.Identifier, "[A-Z][A-Za-z0-9]*", "local function PascalCase");
                break;
            case PropertyDeclarationSyntax property:
                Check(property.Identifier, "[A-Z][A-Za-z0-9]*", "property PascalCase");
                break;
            case EventDeclarationSyntax eventNode:
                Check(eventNode.Identifier, "[A-Z][A-Za-z0-9]*", "event PascalCase");
                break;
            case TypeParameterSyntax parameter:
                Check(parameter.Identifier, "T(?:[A-Z][A-Za-z0-9]*)?", "generic parameter T / TPascalCase");
                break;
            case ParameterSyntax parameter:
                var isRecordProperty = parameter.Parent?.Parent is RecordDeclarationSyntax;
                Check(parameter.Identifier, isRecordProperty ? "[A-Z][A-Za-z0-9]*" : "(?:[a-z][A-Za-z0-9]*|_)", isRecordProperty ? "record property PascalCase" : "parameter camelCase");
                break;
            case VariableDeclaratorSyntax variable:
                var field = variable.Parent?.Parent as FieldDeclarationSyntax;
                var isConstant = field?.Modifiers.Any(SyntaxKind.ConstKeyword) == true || variable.Parent?.Parent is LocalDeclarationStatementSyntax local && local.Modifiers.Any(SyntaxKind.ConstKeyword);
                var isPrivateField = field != null && !field.Modifiers.Any(SyntaxKind.PublicKeyword) && !field.Modifiers.Any(SyntaxKind.InternalKeyword) && !field.Modifiers.Any(SyntaxKind.ProtectedKeyword);
                Check(variable.Identifier, isConstant ? "[A-Z][A-Za-z0-9]*" : isPrivateField ? "_[a-z][A-Za-z0-9]*" : field != null ? "[A-Z][A-Za-z0-9]*" : "(?:[a-z][A-Za-z0-9]*|_)", "constant/member PascalCase, private field _camelCase, local camelCase");
                break;
            case ForEachStatementSyntax loop:
                Check(loop.Identifier, "[a-z][A-Za-z0-9]*", "iteration variable camelCase");
                break;
            case CatchDeclarationSyntax catchNode when !catchNode.Identifier.IsKind(SyntaxKind.None):
                Check(catchNode.Identifier, "[a-z][A-Za-z0-9]*", "exception variable camelCase");
                break;
            case SingleVariableDesignationSyntax designation:
                Check(designation.Identifier, "[a-z][A-Za-z0-9]*", "pattern/out variable camelCase");
                break;
        }
    }

    void Check(SyntaxToken identifier, string pattern, string rule)
    {
        declarationCount++;
        if (!Regex.IsMatch(identifier.ValueText, "^(?:" + pattern + ")$", RegexOptions.CultureInvariant)) Report(identifier, rule);
    }

    void Report(SyntaxToken identifier, string rule) => errors.Add($"{relativePath}:{identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {identifier.ValueText}: {rule}");
}

foreach (var error in errors) Console.Error.WriteLine(error);
Console.WriteLine($"C# naming: {sourceFiles.Length} files, {declarationCount} declarations, {errors.Count} violations.");
return errors.Count == 0 ? 0 : 1;

IEnumerable<string> EnumerateFiles(string directory)
{
    foreach (var file in Directory.EnumerateFiles(directory)) yield return file;
    foreach (var child in Directory.EnumerateDirectories(directory).Where(path => !excludedDirectories.Contains(Path.GetFileName(path))))
        foreach (var file in EnumerateFiles(child)) yield return file;
}
