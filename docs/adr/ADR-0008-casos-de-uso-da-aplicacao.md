# ADR-0008: Casos de Uso da Aplicação

## Status
Accepted

## Contexto
A camada de aplicação precisa expressar capacidades de negócio de forma explícita e co-localizar todos os artefatos de uma feature.

## Decisão
Modelar a aplicação por feature, co-localizando em `src/Server/Bud.Application/<Feature>/`:
- Uma classe por caso de uso com método único `ExecuteAsync`.
- Interface de repositório do feature (ex: `IGoalRepository`).
- Ports da feature quando o adapter tem dono funcional claro (ex: `IMyDashboardReadStore` em `Me/`).
- Mappers de contrato (ex: `CollaboratorsContractMapper`).
- Read models internos (ex: `GoalProgressSnapshot`).
- Policies de negócio do feature (ex: `GoalDateRangePolicy`).

Manter em `src/Server/Bud.Application/Ports/` apenas abstrações transversais, sem dono funcional específico, como tenant/auth.

Casos de uso orquestram regras de aplicação sem conter regra de domínio central.

## Consequências
- Fluxos de negócio ficam rastreáveis por intenção.
- Todos os artefatos de uma feature estão co-localizados.
- Redução de classes "genéricas" sem foco de capacidade.
- Aumento de número de classes com maior coesão.

## Alternativas consideradas
- Organização por tipo técnico (UseCases/, Mapping/, ReadModels/, Policies/ separados).
- Agrupar múltiplos fluxos em classes de comando/consulta amplas.
- Expor lógica diretamente em controllers.
