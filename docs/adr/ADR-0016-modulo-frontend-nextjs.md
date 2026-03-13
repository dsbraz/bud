# ADR-0016: Módulo Frontend Next.js

## Status
Accepted

## Contexto

O Bud possuía um único cliente frontend baseado em Blazor WebAssembly (`Bud.BlazorWasm`), separado do `Bud.Api` conforme decidido na ADR-0015. A evolução do produto demanda uma interface web com maior flexibilidade de composição de componentes, ecossistema npm maduro, suporte nativo a SSR/SSG, melhor experiência de desenvolvimento e adoção mais ampla de profissionais front-end no mercado.

O modelo de autenticação via Auth0 já estava estabelecido no domínio, e a API REST do `Bud.Api` já expunha os contratos necessários. Faltava um cliente web moderno capaz de consumir esses contratos com rotas tipadas, internacionalização, gerenciamento de estado assíncrono e uma biblioteca de componentes consistente.

## Decisão

Adicionar o módulo `src/Client/Bud.NextJs` como novo cliente frontend da plataforma Bud, construído com Next.js 15 (App Router) e implantado como serviço independente.

**Responsabilidades do módulo:**

- Autenticação de usuários via Auth0 (`@auth0/nextjs-auth0`).
- Seleção e criação de workspaces como fluxo inicial pós-login.
- Envio e gerenciamento de convites de usuários.
- Internacionalização (pt-BR / en-US) via `next-intl`.
- Consumo da API REST do `Bud.Api` por meio de API Routes próprias do Next.js (BFF — Backend for Frontend), que encapsulam tokens e evitam expor credenciais ao browser.

**Organização interna (App Router):**

- `src/app/` — páginas e API Routes agrupadas por rota (`workspace/`, `invite/`, `api/user/`).
- `src/presentation/` — módulos de feature auto-contidos (language-selection, workspace-creation, workspace-selection).
- `src/providers/` — providers de contexto React (Auth0, React Query, Workspace).
- `src/components/ui/` — biblioteca de componentes primitivos baseada em Radix UI + Tailwind CSS.
- `src/types/` — tipos TypeScript alinhados ao domínio de Workspace.

**Stack técnica:**

| Camada | Tecnologia |
|---|---|
| Framework | Next.js 15.x com Turbopack |
| Linguagem | TypeScript |
| Autenticação | Auth0 (`@auth0/nextjs-auth0`) |
| Estado assíncrono | TanStack React Query |
| Formulários | Formik + Yup |
| Estilização | Tailwind CSS |
| Componentes | Radix UI |
| Internacionalização | next-intl |
| Container | Dockerfile multi-stage |

**Regras de dependência:**

- `Bud.NextJs` não referencia nenhum projeto .NET; consome apenas a API HTTP pública do `Bud.Api`.
- API Routes do Next.js atuam como BFF: recebem requisições do browser, adicionam o token Auth0 e repassam ao `Bud.Api`, mantendo o padrão same-origin definido na ADR-0015.
- O deploy do `Bud.NextJs` é independente do `Bud.Api`, seguindo o modelo de serviços separados da ADR-0015.

## Consequências

- O ecossistema npm passa a ser parte do ciclo de build e CI/CD do projeto.
- Desenvolvedores front-end podem trabalhar no `Bud.NextJs` sem dependência direta da toolchain .NET.
- O padrão BFF via API Routes evita exposição de tokens Auth0 no browser e centraliza a lógica de integração com o `Bud.Api`.
- O `Bud.BlazorWasm` pode ser descontinuado gradualmente à medida que as features forem migradas para o `Bud.NextJs`.
- Testes do `Bud.NextJs` seguem a estratégia definida na ADR-0012 no que se aplica ao frontend: testes de componente e testes de integração de API Routes.

## Alternativas consideradas

- **Manter exclusivamente o Blazor WebAssembly:** descartado pela menor adoção de mercado, maior fricção no ecossistema de componentes e ausência de SSR nativo.
- **Migrar para SPA React sem Next.js (Vite + React Router):** descartado pela ausência de SSR, ausência de API Routes nativas para BFF e menor suporte a internacionalização.
- **Adotar Remix ou outro meta-framework React:** descartado pela menor maturidade de ecossistema e curva de adoção mais alta para a equipe no momento da decisão.
