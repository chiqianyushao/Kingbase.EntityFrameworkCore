using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace Kingbase.EntityFrameworkCore.Design.Internal;

public sealed class KingbaseProviderCodeGenerator()
    : ProviderCodeGenerator(new ProviderCodeGeneratorDependencies([]))
{
    public override MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment? providerOptions)
        => new("UseKdbndp", providerOptions is null ? [connectionString] : [connectionString, providerOptions]);
}

public sealed class KingbaseAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
    : AnnotationCodeGenerator(dependencies);

public sealed class KingbaseCSharpRuntimeAnnotationCodeGenerator(
    CSharpRuntimeAnnotationCodeGeneratorDependencies dependencies,
    RelationalCSharpRuntimeAnnotationCodeGeneratorDependencies relationalDependencies)
    : RelationalCSharpRuntimeAnnotationCodeGenerator(dependencies, relationalDependencies);
