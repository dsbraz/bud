# ADR-0003: Persistência com EF Core e PostgreSQL

## Status
Accepted

## Contexto

O sistema precisa de persistência relacional com boa produtividade de evolução de schema
e suporte robusto a consultas tenant-scoped. Durante desenvolvimento, não há dados de produção a preservar.

## Decisão

Adotar EF Core + PostgreSQL com:

- `ApplicationDbContext` central
- Configuração de relacionamentos via `IEntityTypeConfiguration<T>` em `Data/Configurations/`, carregados por `ApplyConfigurationsFromAssembly`
- Query filters de multi-tenancy no `OnModelCreating`
- Schema criado a partir do modelo via `EnsureCreated()` no startup em Development
- Uso de `OrganizationId` denormalizado em entidades tenant-scoped para eficiência de filtro
- Migrations serão introduzidas apenas quando houver ambiente de produção com dados a preservar

## Consequências

- Schema sempre derivado do modelo atual, sem overhead de arquivos de migration
- Dados locais recriados pelo `DbSeeder` quando o modelo muda
- Boa integração com testes de integração via banco real

## Alternativas consideradas

- Migrations versionadas no repositório: overhead sem valor enquanto não há produção
- SQL manual sem ORM: menor controle de evolução e maior risco operacional
- ORM alternativo: possível, sem vantagem clara no contexto atual
