# ADR-0005: Multi-tenancy por OrganizationId

## Status
Accepted

## Contexto

A aplicação opera com múltiplas organizações e precisa garantir isolamento de dados por tenant.
O risco principal é vazamento de dados entre organizações.

## Decisão

Implementar multi-tenancy com `OrganizationId` como discriminador:

- `ITenantProvider` para resolver tenant do usuário autenticado
- Query filters globais no EF Core para entidades tenant-scoped
- `TenantRequiredMiddleware` para exigir contexto de tenant em `/api/*`
- Header opcional `X-Tenant-Id` para usuários com acesso a múltiplas organizações

## Consequências

- Isolamento consistente no acesso a dados
- Segurança reforçada por múltiplas camadas (middleware + filtros + autorização)
- Requer atenção em operações administrativas e cenários com `IgnoreQueryFilters()`

## Alternativas consideradas

- Filtragem manual por tenant em cada query: propensa a falhas humanas
- Banco separado por tenant: mais isolamento, porém maior custo operacional para o contexto atual
