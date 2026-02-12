# ADR-0015: Maturidade de CQRS, Value Objects, Aggregates e Event Versioning

## Status
Accepted

## Contexto

Para elevar maturidade arquitetural, o projeto precisava formalizar decisões de domínio além da separação atual de UseCases, com foco em:

1. explicitar fronteiras de agregados,
2. introduzir Value Objects em invariantes críticas,
3. registrar versão de eventos no outbox sem quebra de compatibilidade.

## Decisão

1. **CQRS formalizado por direção arquitetural**:
   - manter commands e queries separados por UseCases;
   - evoluir leitura para read models dedicados de forma incremental, sem ruptura.
2. **Value Object de e-mail**:
   - introduzido `EmailAddress` para normalização e validação semântica centralizada.
3. **Event versioning no outbox**:
   - metadado de versão embutido no `EventType` (`<assembly-qualified-type>|v<version>`);
   - fallback compatível para eventos antigos sem sufixo;
   - suporte a versão por atributo (`DomainEventVersionAttribute`) ou interface (`IVersionedDomainEvent`).
4. **Fronteiras de aggregate**:
   - manter invariantes principais no agregado raiz e evitar validações transversais em controllers.

## Consequências

- Redução de duplicação de normalização de e-mail em serviços.
- Base preparada para evolução de eventos com compatibilidade retroativa.
- Direcionamento explícito para expansão de read models por contexto sem big-bang.

## Alternativas consideradas

1. Versionar em coluna dedicada do outbox: descartado neste momento por exigir migração de schema imediata.
2. Manter validação de e-mail espalhada em services/validators: descartado por baixa coesão.
