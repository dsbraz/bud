# ADR-0004: Autenticação, autorização e políticas

## Status
Accepted

## Contexto

A aplicação precisava controlar acesso por usuário, tenant e permissões de negócio,
com regras centralizadas e auditáveis.

## Decisão

Adotar:

- Autenticação por JWT (fluxo passwordless por e-mail)
- Policies para autorização (`TenantSelected`, `GlobalAdmin`, `OrganizationOwner`, `OrganizationWrite`)
- Middleware de tenant obrigatório para endpoints `/api/*` (com exceções explícitas)
- Header `X-Tenant-Id` para seleção de organização quando aplicável

## Consequências

- Regras de segurança centralizadas e reutilizáveis
- Menor dispersão de `if` de segurança em serviços
- Exige manutenção cuidadosa de claims/policies conforme evolução de regras

## Alternativas consideradas

- Autorização ad-hoc apenas em serviços: maior risco de inconsistência
- Modelo RBAC isolado sem tenant explícito: insuficiente para isolamento organizacional requerido
