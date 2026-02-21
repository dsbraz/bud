# ADR-0007: Autenticação e Autorização por Políticas

## Status
Accepted

## Contexto
A segurança do domínio depende de autenticação confiável e autorização consistente.

## Decisão
- Autenticação via JWT.
- Autorização orientada por políticas e handlers.
- Regras de permissão centralizadas na camada de autorização.

## Consequências
- Redução de condicionais de permissão espalhadas no código.
- Facilidade de evolução de regras de acesso por política.

## Alternativas consideradas
- Autorização ad-hoc por `if` em cada controller/repositório.
- Misturar autenticação e autorização em serviços de domínio.
