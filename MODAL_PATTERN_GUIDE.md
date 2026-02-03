# Guia de Implementação: Padrão Modal para CRUD

## 📋 Resumo

Foi implementado um novo padrão de UI para páginas de gerenciamento, migrando do layout **two-column grid** (formulário + lista lado a lado) para o padrão moderno **list-first + modal**.

### ✅ Implementação Completa em:
- [Teams.razor](src/Bud.Client/Pages/Teams.razor) ✓

### 🔨 Pendente de migração:
- Organizations.razor
- Workspaces.razor
- Collaborators.razor
- Missions.razor
- MissionMetrics.razor

---

## 🎯 Benefícios do Novo Padrão

1. **Melhor uso do espaço** - Lista ocupa 100% da largura
2. **Foco na tarefa** - Modal isola a ação de criação
3. **Responsivo** - Funciona em mobile/tablet (modal vira bottom sheet)
4. **Escalável** - Facilita adicionar filtros, actions, export
5. **Padrão moderno** - Usado por Linear, Notion, GitHub, Asana
6. **UX consistente** - Mesmo modal serve para editar

---

## 🚀 Como Migrar uma Página

### Passo 1: Estrutura da Página

**ANTES:**
```razor
<div class="grid-2">
    <!-- Formulário de criação -->
    <div class="card">
        <h2>Criar [Entity]</h2>
        <EditForm>...</EditForm>
    </div>

    <!-- Lista -->
    <div class="card">
        <h2>Lista</h2>
        <table>...</table>
    </div>
</div>
```

**DEPOIS:**
```razor
<div class="page-header">
    <div>
        <div class="page-kicker">Gestão</div>
        <h1>[Entity]</h1>
        <p class="page-subtitle">Descrição...</p>
    </div>
    <button class="button primary" @onclick="OpenCreateModal">
        <span class="button-icon-text">+</span>
        Nova [entity]
    </button>
</div>

<ManagementMenu />

<!-- Lista full-width -->
<div class="card">
    <div class="card-filters">
        <!-- Filtros e busca aqui -->
    </div>
    <table class="table">...</table>
</div>

<!-- Modal -->
@if (isModalOpen)
{
    <Modal Title="Criar [entity]" OnClose="CloseModal">
        <EditForm Model="@newEntity" OnValidSubmit="CreateEntity">
            <!-- Campos do formulário -->
            <div class="form-actions">
                <button class="button tertiary" type="button" @onclick="CloseModal">Cancelar</button>
                <button class="button primary" type="submit">Salvar</button>
            </div>
        </EditForm>
    </Modal>
}
```

### Passo 2: Código C#

Adicionar ao `@code` block:

```csharp
private bool isModalOpen = false;

private void OpenCreateModal()
{
    newEntity = new CreateEntityRequest();
    formMessage = null;
    isModalOpen = true;
}

private void CloseModal()
{
    isModalOpen = false;
    formMessage = null;
}

private async Task CreateEntity()
{
    formMessage = null;

    // ... validação e lógica de criação ...

    await Api.CreateEntityAsync(newEntity);
    await LoadEntities();
    formMessage = "Entidade criada com sucesso.";

    // Auto-close modal após sucesso
    await Task.Delay(1500);
    CloseModal();
}
```

### Passo 3: Filtros da Lista

Use a classe `.card-filters` para organizar filtros horizontalmente:

```razor
<div class="card-filters">
    <div class="form-row">
        <label>Busca</label>
        <InputText class="input" @bind-Value="search" />
    </div>
    <div class="form-row">
        <label>Status</label>
        <select class="input" @bind="status">
            <option value="">Todos</option>
        </select>
    </div>
    <div class="form-row" style="align-self: flex-end;">
        <button class="button" @onclick="LoadData">Atualizar</button>
    </div>
</div>
```

---

## 🎨 Componente Modal

### Localização
`src/Bud.Client/Shared/Modal.razor`

### Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `Title` | string | "" | Título do modal |
| `Size` | string | "md" | Tamanho: "sm", "md", "lg", "xl" |
| `ChildContent` | RenderFragment | - | Conteúdo do modal |
| `Footer` | RenderFragment? | null | Footer customizado (opcional) |
| `OnClose` | EventCallback | - | Callback ao fechar |
| `CloseOnOverlayClick` | bool | true | Fechar ao clicar no overlay |

### Tamanhos Disponíveis

- **sm:** 400px - Para formulários muito simples (2 campos)
- **md:** 540px - Padrão, para maioria dos casos (3-5 campos)
- **lg:** 720px - Para formulários com muitos campos
- **xl:** 960px - Para formulários complexos ou com preview

### Exemplo de Uso

```razor
<Modal Title="Criar organização" Size="md" OnClose="CloseModal">
    <EditForm Model="@model" OnValidSubmit="OnSubmit">
        <!-- Seu formulário aqui -->
    </EditForm>
</Modal>
```

---

## 🎯 Classes CSS Adicionadas

### Layout de Filtros
```css
.card-filters - Container flex para filtros horizontais
```

### Modal
```css
.modal-overlay - Backdrop com fade-in
.modal-dialog - Container do modal com slide-up
.modal-header - Header com título e botão fechar
.modal-close - Botão de fechar circular
.modal-body - Corpo scrollable
.modal-footer - Footer opcional para actions
```

### Utilidades
```css
.button-icon-text - Para ícone + texto em botões
```

---

## 📱 Responsividade

### Desktop (> 640px)
- Modal centralizado
- Tamanhos respeitados (sm, md, lg, xl)
- Overlay escurece tela

### Mobile (≤ 640px)
- Modal vira **bottom sheet**
- Ocupa 95vh da altura
- Slide-up animation de baixo para cima
- Border radius apenas no topo

---

## ♿ Acessibilidade

### Implementado
- ✅ Botão de fechar com `aria-label="Fechar"`
- ✅ Animações suaves (respeitam `prefers-reduced-motion`)
- ✅ Overlay clicável para fechar (pode ser desabilitado)
- ✅ Botão "Cancelar" visível no formulário

### Pendente (futuro)
- ⏳ ESC key para fechar (requer JavaScript interop)
- ⏳ Focus trap (foco permanece dentro do modal)
- ⏳ Restaurar foco ao elemento que abriu o modal

---

## 🎨 Design Tokens Utilizados

O modal usa os design tokens definidos em `tokens.css`:

```css
--z-index-modal: 1050
--shadow-modal: (shadow definido)
--radius-xl: 16px
--radius-circle: 9999px
--color-surface: (cor de fundo)
--color-border-light: (bordas)
--spacing-*: (espaçamentos)
--transition-fast: 100ms
--transition-base: 200ms
```

---

## 📝 Checklist de Migração

Para migrar cada página:

- [ ] Remover `.grid-2` wrapper
- [ ] Mover formulário para dentro de `<Modal>`
- [ ] Adicionar botão "Nova [entity]" no page-header
- [ ] Reorganizar filtros em `.card-filters`
- [ ] Adicionar `isModalOpen` state
- [ ] Criar métodos `OpenCreateModal()` e `CloseModal()`
- [ ] Adicionar botão "Cancelar" no form-actions
- [ ] Testar em desktop e mobile
- [ ] Verificar animações e feedback de sucesso

---

## 🔮 Evolução Futura

### 1. Funcionalidade de Edição
O modal pode ser reutilizado para edição:

```csharp
private Guid? editingEntityId = null;

private void OpenEditModal(Entity entity)
{
    editingEntityId = entity.Id;
    newEntity = MapToRequest(entity);
    isModalOpen = true;
}

private async Task SaveEntity()
{
    if (editingEntityId.HasValue)
    {
        await Api.UpdateEntityAsync(editingEntityId.Value, newEntity);
    }
    else
    {
        await Api.CreateEntityAsync(newEntity);
    }
    // ...
}
```

### 2. Actions na Tabela
Adicionar coluna de ações:

```razor
<th></th>
<!-- ... -->
<td>
    <button class="button-icon button-sm ghost" @onclick="() => OpenEditModal(item)">
        <svg><!-- edit icon --></svg>
    </button>
    <button class="button-icon button-sm ghost" @onclick="() => DeleteEntity(item.Id)">
        <svg><!-- delete icon --></svg>
    </button>
</td>
```

### 3. Bulk Actions
Adicionar seleção múltipla e ações em lote:

```razor
<div class="card-actions">
    <button class="button secondary">Exportar selecionados</button>
    <button class="button tertiary">Deletar selecionados</button>
</div>
```

---

## 📚 Referências

- **Padrões de mercado:** Linear, Notion, GitHub, Asana
- **Pesquisa UX:** [Modal UX Best Practices](https://www.eleken.co/blog-posts/modal-ux)
- **Design:** Baseado no Figma Style Guide do projeto

---

**Última atualização:** 2026-02-02
**Implementado por:** Claude Code
**Status:** ✅ Pronto para replicação
