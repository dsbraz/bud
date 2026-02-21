# ADR-0008: Casos de Uso da Aplicação

## Status
Accepted

## Contexto
A camada de aplicação precisa expressar capacidades de negócio de forma explícita.

## Decisão
Modelar a aplicação por casos de uso com:
- Uma classe por caso de uso.
- Método único `ExecuteAsync`.
- Orquestração de regras de aplicação sem conter regra de domínio central.

Os casos de uso cobrem autenticação, estrutura organizacional, missões, objetivos, métricas, check-ins, templates, dimensões, notificações e painel.

## Consequências
- Fluxos de negócio ficam rastreáveis por intenção.
- Redução de classes “genéricas” sem foco de capacidade.
- Aumento de número de classes com maior coesão.

## Alternativas consideradas
- Agrupar múltiplos fluxos em classes de comando/consulta amplas.
- Expor lógica diretamente em controllers.
