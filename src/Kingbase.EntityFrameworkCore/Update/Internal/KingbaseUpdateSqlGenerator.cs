using Microsoft.EntityFrameworkCore.Update;

namespace Kingbase.EntityFrameworkCore.Update.Internal;

public sealed class KingbaseUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
    : UpdateSqlGenerator(dependencies);
