# ADR-0014: Co-localização de interfaces (ports) com implementações

## Status
Accepted

## Contexto

Até esta decisão, as interfaces de repositórios (`I*Repository`) e serviços de infraestrutura (`IAuthService`, `IMissionScopeResolver`, etc.) ficavam centralizadas em `Application/Ports/`, separadas fisicamente de suas implementações em `Infrastructure/Repositories/` e `Infrastructure/Services/`.

Essa separação seguia o princípio clássico de Clean Architecture (inversão de dependência com interfaces na camada de aplicação). Na prática, porém, criava fricção:

- Cada nova interface exigia criação de arquivo em diretório separado da implementação.
- Navegação entre interface e implementação era menos direta.
- A pasta `Application/Ports/` acumulava interfaces de domínios distintos sem organização interna clara.
- Os testes de arquitetura precisavam validar que `Application` não dependia de `Infrastructure`, mas a regra real desejada era que UseCases não dependessem de *implementações concretas* — não que fossem proibidos de importar namespaces de infraestrutura.

## Decisão

Mover as interfaces (ports) para co-localizar com suas implementações:

- **Interfaces de repositórios** (`I*Repository`) movidas para `Infrastructure/Repositories/`
- **Interfaces de serviços** (`IAuthService`, `IMissionScopeResolver`, `IMissionProgressService`, `INotificationRecipientResolver`) movidas para `Infrastructure/Services/`
- A pasta `Application/Ports/` foi removida.

O princípio de depender de abstrações permanece: UseCases continuam dependendo apenas de interfaces, nunca de classes concretas. A mudança é apenas na localização física dos arquivos.

Testes de arquitetura foram refinados para validar que UseCases dependem apenas de tipos interface de `Infrastructure/`, não de implementações concretas.

## Consequências

- **Positivas:**
  - Navegação mais direta entre interface e implementação (mesmo diretório).
  - Menor fricção ao criar novos repositórios/serviços.
  - Organização por domínio técnico (Repositories, Services) em vez de diretório genérico de ports.
- **Negativas:**
  - UseCases agora importam namespaces de `Infrastructure/` (apenas para tipos interface).
  - Testes de arquitetura precisaram ser refinados para distinguir interfaces de implementações concretas.

## Alternativas consideradas

- **Manter `Application/Ports/`**: funcional, mas com fricção de navegação e organização.
- **Criar pastas `Abstractions/` dentro de cada subpasta de Infrastructure**: adicionaria nível desnecessário de indireção.
