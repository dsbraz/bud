# ADR-0001: Linguagem Ubíqua e Bounded Contexts do Server

## Status
Accepted

## Contexto
O domínio do produto exige consistência de termos entre regras de negócio, código e contratos de API.
Sem um vocabulário comum, o modelo perde precisão e os casos de uso ficam ambíguos.

## Decisão
Adotar linguagem ubíqua explícita no Server com os contextos:
- Identidade e Acesso
- Estrutura Organizacional
- Missões e Execução
- Notificações
- Painel Operacional

Termos do domínio devem ser usados de forma uniforme em agregados, casos de uso, eventos e documentação.

## Consequências
- Redução de ambiguidade semântica.
- Melhora de rastreabilidade entre regra de negócio e implementação.
- Revisões de código passam a validar também aderência à linguagem ubíqua.

## Alternativas consideradas
- Manter nomes técnicos por camada.
- Permitir termos diferentes entre API e domínio.
