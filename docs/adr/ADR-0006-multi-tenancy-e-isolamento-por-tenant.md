# ADR-0006: Multi-tenancy e Isolamento por Tenant

## Status
Accepted

## Contexto
Dados de organizações diferentes não podem se misturar em consultas ou escritas.

## Decisão
- Isolamento por `OrganizationId` em entidades tenant-scoped.
- Filtros globais de tenant na camada de persistência.
- Interceptação de escrita para garantir preenchimento consistente do tenant.
- Validação de contexto de tenant na borda da API.

## Consequências
- Redução de risco de vazamento de dados entre tenants.
- Exige atenção em operações administrativas e cenários `ignoreQueryFilters`.

## Alternativas consideradas
- Isolamento por banco separado por tenant.
- Isolamento apenas por regras de aplicação sem filtros globais.
