# ADRs (Architecture Decision Records)

Esta pasta registra decisões arquiteturais vigentes do sistema.
Cada ADR é autocontida e descreve contexto, decisão, consequências e alternativas.

## Convenção

- Arquivos no formato `ADR-XXXX-titulo-curto.md`
- Numeração contínua com 4 dígitos
- Um ADR por decisão arquitetural relevante

## Sequência vigente

1. `ADR-0001` Linguagem Ubíqua e Bounded Contexts do Server
2. `ADR-0002` Arquitetura DDD Estrita e Regras de Dependência
3. `ADR-0003` Agregados, Entidades, Value Objects e Invariantes
4. `ADR-0004` Repositórios por Agregado e Unit of Work
5. `ADR-0005` Persistência com EF Core e Mapeamento de Infraestrutura
6. `ADR-0006` Multi-tenancy e Isolamento por Tenant
7. `ADR-0007` Autenticação e Autorização por Políticas
8. `ADR-0008` Casos de Uso da Aplicação
9. `ADR-0009` Eventos de Domínio e Notificações
10. `ADR-0010` Contratos HTTP, Tratamento de Erros e Camada de Borda
11. `ADR-0011` Estratégia de Validação
12. `ADR-0012` Estratégia de Testes e Governança Arquitetural

## Status possíveis

- `Proposed`
- `Accepted`
- `Superseded`
- `Deprecated`

## Governança

Mudanças arquiteturais devem atualizar ADRs no mesmo conjunto de alterações.
