# ADR-0009: Persistência com EF Core, PostgreSQL e migrations

## Status
Accepted

## Contexto

O sistema precisava de persistência relacional com boa produtividade de evolução de schema
e suporte robusto a consultas tenant-scoped.

## Decisão

Adotar EF Core + PostgreSQL com:

- `ApplicationDbContext` central
- Configuração de relacionamentos e query filters no `OnModelCreating`
- Migrations versionadas no repositório
- Aplicação automática de migrations em Development no startup
- Uso de `OrganizationId` denormalizado em entidades tenant-scoped para eficiência de filtro

## Consequências

- Evolução de schema rastreável e reproduzível
- Boa integração com testes de integração via banco real
- Atenção necessária em mudanças de schema para manter compatibilidade de dados

## Alternativas consideradas

- SQL manual sem migrations: menor controle de evolução e maior risco operacional
- ORM alternativo com menor convenção: possível, sem vantagem clara no contexto atual
