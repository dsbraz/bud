# ADR-0014: Unificação de Mission+Objective em Goal e renomeação de Metric→Indicator

**Status:** Accepted
**Data:** 2026-02-27

---

## Contexto

O modelo de domínio original possuía quatro entidades hierárquicas separadas:

- **Mission** (missão): entidade raiz de planejamento com escopo (Organization/Workspace/Team/Collaborator).
- **Objective** (objetivo): filho de Mission, com campo `dimension` opcional.
- **Metric** (métrica): indicador vinculado a Mission ou Objective.
- **MetricCheckin** (checkin de métrica): registro de evolução de uma Metric.

Esse modelo impunha rigidez estrutural: uma Objective sempre dependia de uma Mission, e Metrics eram mapeadas por pares `MissionId`/`ObjectiveId`. A hierarquia de dois níveis fixos limitava casos de uso como OKR com múltiplos níveis de Key Results ou metas de equipe aninhadas.

---

## Decisão

Unificar Mission e Objective em uma única entidade **Goal** (meta), recursiva via `ParentId?`, e renomear Metric → **Indicator** e MetricCheckin → **Checkin**.

### Mapeamento de entidades

| Antes           | Depois (código) | UI (pt-BR) |
|-----------------|-----------------|------------|
| `Mission`       | `Goal`          | Meta       |
| `Objective`     | `Goal` (filho)  | Meta       |
| `Metric`        | `Indicator`     | Indicador  |
| `MetricCheckin` | `Checkin`       | Checkin    |

### Mapeamento de enums

| Antes                     | Depois                      |
|---------------------------|-----------------------------|
| `MissionScopeType`        | `GoalScopeType`             |
| `MissionStatus`           | `GoalStatus`                |
| `MetricType`              | `IndicatorType`             |
| `MetricUnit`              | `IndicatorUnit`             |
| `QuantitativeMetricType`  | `QuantitativeIndicatorType` |

### Estrutura de Goal

```
Goal
├── Id
├── OrganizationId (tenant)
├── ParentId?          ← hierarquia recursiva
├── Name
├── Description?
├── Dimension?         ← campo herdado de Objective
├── StartDate
├── EndDate
├── Status (GoalStatus)
├── ScopeType (GoalScopeType)
├── ScopeId
└── Indicators (ICollection<Indicator>)
```

### Rotas da API

| Antes                                    | Depois                                       |
|------------------------------------------|----------------------------------------------|
| `GET/POST /api/missions`                 | `GET/POST /api/goals`                        |
| `GET/PATCH/DELETE /api/missions/{id}`    | `GET/PATCH/DELETE /api/goals/{id}`           |
| `GET /api/missions/{id}/metrics`         | `GET /api/goals/{id}/indicators`             |
| `GET /api/missions/{id}/objectives`      | `GET /api/goals/{id}/children`               |
| `GET/POST /api/objectives`               | Absorvido em `/api/goals` (via `parentId`)   |
| `GET/PATCH/DELETE /api/objectives/{id}`  | `GET/PATCH/DELETE /api/goals/{id}`           |
| `GET/POST /api/metrics`                  | `GET/POST /api/indicators`                   |
| `GET/PATCH/DELETE /api/metrics/{id}`     | `GET/PATCH/DELETE /api/indicators/{id}`      |
| `POST /api/metrics/{id}/checkins`        | `POST /api/indicators/{id}/checkins`         |
| `GET/PATCH/DELETE .../checkins/{id}`     | `GET/PATCH/DELETE .../checkins/{id}`         |

### MCP tools

| Antes                       | Depois                          |
|-----------------------------|---------------------------------|
| `mission_create` … `delete` | `goal_create` … `goal_delete`   |
| `mission_metric_*`          | `goal_indicator_*`              |
| `metric_checkin_*`          | `indicator_checkin_*`           |

---

## Consequências

### Positivas

- **Flexibilidade**: Goals podem ter qualquer profundidade de sub-metas, suportando OKR, BSC e outros frameworks sem alterações no modelo.
- **Simplificação**: de 4 entidades para 3 (Goal, Indicator, Checkin), reduzindo a superfície do domínio.
- **Nomenclatura mais clara**: "Indicador" é mais preciso do que "Métrica" no contexto de OKR/gestão de performance.

### Negativas / Trade-offs

- **Migração de dados**: necessária migração das tabelas `Missions` e `Objectives` → `Goals`, e `Metrics` → `Indicators`.
- **Breaking change na API**: clientes que consomem `/api/missions` ou `/api/metrics` precisam atualizar as chamadas.
- **Scope resolver**: a lógica de resolução de escopo (Workspace/Team/Collaborator) é mantida mas agora aplicada à entidade Goal.

---

## Alternativas consideradas

1. **Manter Mission e Objective separadas com herança/interface**: rejeitado por adicionar complexidade sem ganho real de modelo.
2. **Renomear apenas Metric → Indicator** sem unificar Mission/Objective: parcial, não resolve a rigidez hierárquica.
3. **Introduzir entidade genérica Node**: considerado em iteração anterior; rejeitado por ser pouco expressivo no domínio de gestão de metas.

---

## Referências

- `ADR-0001-linguagem-ubiqua-e-bounded-contexts-do-server.md` — definição do bounded context de planejamento.
- `ADR-0003-agregados-entidades-value-objects-e-invariantes.md` — padrão de agregados no domínio.
