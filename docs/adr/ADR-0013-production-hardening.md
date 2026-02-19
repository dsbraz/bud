# ADR-0013: Hardening de Produção — Estrutura e Segurança

## Status
Accepted

## Contexto

A aplicação funcional possuía gaps de segurança para produção:
- JWT key com fallback silencioso para chave de dev em qualquer ambiente
- Sem security headers (X-Frame-Options, CSP, HSTS, etc.)
- Sem rate limiting no login (vulnerável a brute-force)
- Sem forwarded headers (Cloud Run faz TLS termination)
- `AllowedHosts: "*"` sem restrição documentada

## Decisão

### 1. Centralização JWT em `JwtSettings`

Criado POCO `JwtSettings` com `IOptions<T>` pattern. AuthService e BudSecurityCompositionExtensions leem da mesma fonte tipada. Fail-fast em não-Development se a chave JWT estiver vazia.

### 2. Security Headers via Middleware

`SecurityHeadersMiddleware` aplica headers em todas as respostas:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy` com `wasm-unsafe-eval` (obrigatório para Blazor WASM) e `unsafe-inline` em styles (inline styles do Blazor)
- `frame-ancestors 'none'` no CSP (equivalente moderno do X-Frame-Options)

**Trade-off CSP:** `wasm-unsafe-eval` é necessário para Blazor WASM executar WebAssembly. Sem ele, a aplicação não funciona. `unsafe-inline` em styles é necessário para estilos dinâmicos do Blazor — substituir por nonces exigiria mudanças significativas no pipeline de renderização.

### 3. Rate Limiting no Login

Fixed window por IP usando `System.Threading.RateLimiting` (built-in). Configurável via `RateLimitSettings` (padrão: 10 req/60s). Resposta 429 com ProblemDetails em pt-BR.

**Trade-off:** Fixed window é simples e eficaz para o cenário atual. Sliding window ou token bucket seriam mais sofisticados, mas adicionam complexidade desnecessária para proteção contra brute-force básico.

### 4. Forwarded Headers

Habilitado `XForwardedFor` + `XForwardedProto` para Cloud Run (TLS termination no proxy). `KnownIPNetworks` e `KnownProxies` limpos para aceitar qualquer proxy — necessário em Cloud Run onde o IP do proxy não é fixo.

**Trust model:** Cloud Run gerencia a infraestrutura de proxy, então confiar nos headers forwarded é aceitável. Em ambientes com proxies não confiáveis, seria necessário restringir `KnownIPNetworks`.

### 5. HSTS

Habilitado em não-Development. Usa os defaults do ASP.NET Core (max-age: 30 dias, sem includeSubDomains por padrão).

## Consequências

- Ambiente dev via Docker Compose continua funcional sem configuração adicional (fallback de JWT key, sem forwarded headers/HSTS)
- Produção sem `Jwt:Key` não inicia (fail-fast intencional)
- Fitness functions previnem regressão (IConfiguration banido em Services, rate limiting obrigatório no Login)
- Rate limiting é por instância, não distribuído — aceitável para Cloud Run com poucas instâncias

## Alternativas consideradas

- **CSP via meta tag:** descartado — headers são mais seguros e não podem ser sobrescritos por XSS
- **Rate limiting distribuído (Redis):** descartado — complexidade desnecessária para o volume atual
- **IP-based blocking em vez de rate limiting:** descartado — rate limiting é mais granular e menos propenso a falsos positivos
