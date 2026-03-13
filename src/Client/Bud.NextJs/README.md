# Bud.NextJs

Frontend web do Bud 2.0, construído com Next.js 15 (App Router). Consome a API REST do `Bud.Api` exclusivamente via API Routes próprias (padrão BFF), mantendo tokens Auth0 fora do browser.

## Requisitos

| Ferramenta | Versão |
|---|---|
| Node.js | 22.14.0 |
| Yarn | 1.22.22 |

## Variáveis de ambiente

Copie `.env` para `.env.local` e preencha:

```env
AUTH0_SECRET=
AUTH0_BASE_URL=
AUTH0_ISSUER_BASE_URL=
AUTH0_CLIENT_ID=
AUTH0_CLIENT_SECRET=
AUTH0_AUDIENCE=
BUD_API_URL=
```

## Instalação e execução

```bash
yarn install
yarn dev        # desenvolvimento com Turbopack
yarn build      # build de produção
yarn start      # iniciar build de produção
```

## Docker

A imagem expõe a porta `8080` e usa output `standalone` do Next.js.

```bash
# build
docker build -t bud-web .

# run
docker run -d -p 8080:8080 --env-file .env bud-web
```

O Dockerfile usa três estágios (`deps` → `builder` → `runner`) com usuário não-root `nextjs:nodejs`.

---

## Pacotes

### Runtime

| Pacote | Finalidade |
|---|---|
| `next` 15.x | Framework — App Router, API Routes, middleware, SSR |
| `react` / `react-dom` 19.x | Biblioteca de UI |
| `@auth0/nextjs-auth0` | Autenticação via Auth0; expõe `Auth0Client`, `Auth0Provider` e helpers de sessão |
| `next-intl` | Internacionalização (i18n) baseada em cookies; suporte a `pt` e `en` |
| `@tanstack/react-query` | Gerenciamento de estado assíncrono (fetch, mutações, cache) |
| `formik` | Gerenciamento de estado de formulários |
| `yup` | Validação de esquemas para os formulários Formik |
| `js-cookie` | Leitura e escrita de cookies no cliente (ex.: `selectedWorkspace`) |
| `jose` | Verificação/decodificação de JWTs nas API Routes |
| `base64url` | Codificação/decodificação Base64URL usada junto ao `jose` |
| `@radix-ui/react-avatar` | Componente primitivo de avatar acessível |
| `@radix-ui/react-dialog` | Componente primitivo de modal/dialog acessível |
| `@radix-ui/react-popover` | Componente primitivo de popover acessível |
| `@radix-ui/react-slot` | Utilidade de composição para o componente `Button` |
| `cmdk` | Base para o componente `Command` (busca/combobox) |
| `lucide-react` | Biblioteca de ícones SVG |
| `clsx` | Concatenação condicional de classes CSS |
| `tailwind-merge` | Resolução de conflitos entre classes Tailwind |
| `class-variance-authority` | Geração de variantes tipadas de componentes (usado no `Button`) |

### Dev

| Pacote | Finalidade |
|---|---|
| `typescript` | Tipagem estática |
| `tailwindcss` v4 | Framework de estilos utilitários |
| `@tailwindcss/postcss` | Integração PostCSS do Tailwind v4 |
| `tw-animate-css` | Animações CSS via Tailwind |
| `eslint` + `eslint-config-next` | Linting com regras Next.js |
| `eslint-config-prettier` + `eslint-plugin-prettier` | Integração ESLint/Prettier |
| `eslint-plugin-jsx-a11y` | Regras de acessibilidade para JSX |
| `prettier` | Formatação de código |

---

## Entrypoints

### Middleware — `src/middleware.ts`

Executado em toda requisição (exceto assets estáticos). Responsável por:

1. Delegar rotas `/auth/*` ao handler do Auth0.
2. Verificar sessão ativa; redirecionar para `/auth/login` caso não exista.

```
matcher: /((?!_next/static|_next/image|favicon.ico|.*\\..*).*)
```

### Layout raiz — `src/app/layout.tsx`

Monta a árvore de providers na seguinte ordem (de fora para dentro):

```
Auth0Provider
  NextIntlClientProvider
    QueryProvider
      WorkspaceProvider   ← lê cookie "selectedWorkspace" no servidor
        {children}
```

Aplica a fonte `Plus Jakarta Sans` e estilos base via `globals.css`.

### Página principal — `src/app/page.tsx`

Rota `/`. Requer workspace selecionado (redireciona para `/workspace` caso contrário). Ponto de entrada da aplicação pós-login.

### Rotas de workspace — `src/app/workspace/`

| Rota | Arquivo | Descrição |
|---|---|---|
| `/workspace` | `page.tsx` | Listagem de workspaces disponíveis |
| `/workspace/creation` | `creation/page.tsx` | Formulário de criação de workspace |

### Rota de convite — `src/app/invite/page.tsx`

Rota `/invite`. Tela de aceitação de convite de usuário.

### API Routes (BFF) — `src/app/api/user/`

Todas as rotas injetam o token Auth0 via `auth0.getAccessToken()` antes de repassar ao `Bud.Api`.

| Rota | Método | Descrição |
|---|---|---|
| `/api/user/get-user` | `GET` | Busca perfil do usuário autenticado em `BUD_API_URL/api/user/get-user` |
| `/api/user/create-invite` | `POST` | Envia convite de usuário para `BUD_API_URL/api/user/user-invite` |
| `/api/user/check-token` | `GET` | Valida token de convite em `BUD_API_URL/api/user/check-token` (não requer autenticação Auth0) |

---

## Estrutura de arquivos

```
src/
├── app/                        # App Router: páginas e API Routes
│   ├── layout.tsx              # Layout raiz com providers
│   ├── page.tsx                # Página principal (/)
│   ├── globals.css             # Estilos globais e variáveis CSS
│   ├── api/
│   │   └── user/               # API Routes BFF — um diretório por operação
│   │       ├── check-token/route.ts
│   │       ├── create-invite/route.ts
│   │       └── get-user/route.ts
│   ├── workspace/
│   │   ├── layout.tsx          # Layout compartilhado das rotas de workspace
│   │   ├── page.tsx            # Seleção de workspace
│   │   └── creation/
│   │       └── page.tsx        # Criação de workspace
│   └── invite/
│       └── page.tsx            # Aceitação de convite
│
├── presentation/               # Módulos de feature auto-contidos
│   └── <feature>/
│       ├── index.tsx           # Componente exportado pela feature (entry point)
│       ├── components/         # Componentes internos da feature
│       └── schemas/            # Esquemas Yup da feature
│
├── components/
│   ├── ui/                     # Primitivos de UI (shadcn/ui) — stateless e sem lógica de domínio
│   │   ├── avatar.tsx
│   │   ├── button.tsx
│   │   ├── command.tsx
│   │   ├── dialog.tsx
│   │   └── popover.tsx
│   ├── form-values/            # Campos de formulário Formik reutilizáveis
│   │   ├── TextFieldComponent.tsx
│   │   ├── SelectFieldComponent.tsx
│   │   └── FileFieldComponent.tsx
│   └── ComboboxComponent.tsx
│
├── providers/                  # Providers React de contexto global
│   ├── workspace-provider.tsx  # Estado do workspace selecionado + cookie
│   └── query-provider.tsx      # QueryClient do React Query
│
├── lib/                        # Utilitários e configurações de bibliotecas
│   ├── auth0.js                # Instância singleton do Auth0Client
│   ├── i18n.ts                 # Configuração do next-intl (locale via cookie)
│   ├── utils.ts                # Função cn() para composição de classes Tailwind
│   └── generate-acronym-and-color.js  # Gera sigla e cor HSL a partir de nome
│
├── types/                      # Tipos TypeScript alinhados ao domínio
│   └── workspace/
│       ├── WorkspaceSummaryType.tsx    # type WorkspaceSummary
│       └── WorkspaceVisibilityEnum.tsx # enum WorkspaceVisibility
│
└── middleware.ts               # Middleware global de autenticação

messages/                       # Arquivos de tradução (next-intl)
├── pt.json                     # Português (padrão)
└── en.json                     # Inglês
```

### Convenções

**Nomenclatura de arquivos**

- Páginas e layouts Next.js: `page.tsx`, `layout.tsx` (minúsculo, convenção do framework).
- Componentes React exportáveis: `PascalCase.tsx` (ex.: `CreateWorkspaceForm.tsx`).
- Utilitários e configurações: `kebab-case.ts` (ex.: `generate-acronym-and-color.js`).
- Enums e types de domínio: `PascalCaseNomeDoTipo.tsx` dentro de `src/types/<domínio>/`.

**Módulos de presentation**

Cada feature em `src/presentation/` é auto-contida e exporta um único componente raiz via `index.tsx`. Componentes internos ficam em `components/` e esquemas de validação em `schemas/`. Nenhum módulo de presentation importa outro diretamente.

**API Routes (BFF)**

Cada operação ocupa seu próprio diretório com um único arquivo `route.ts`. As rotas não contêm lógica de negócio — apenas recebem a requisição do browser, injetam o token Auth0 e repassam ao `Bud.Api`.

**Componentes de UI**

Os componentes em `src/components/ui/` são gerados/mantidos com o padrão shadcn/ui (ver `components.json`). Não devem conter lógica de domínio nem estado de aplicação. Novos primitivos seguem o mesmo padrão: wrapper fino sobre Radix UI com `cn()` para classes.

**Internacionalização**

Chaves de tradução são agrupadas por componente/feature nos arquivos `messages/*.json`. O locale é resolvido a partir do cookie `locale` no servidor (`src/lib/i18n.ts`); o padrão é `pt`.
