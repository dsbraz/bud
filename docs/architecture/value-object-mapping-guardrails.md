# Value Object Mapping Guardrails

Matriz declarativa para garantir que serviços façam mapeamento explícito de campos críticos de request para Value Objects.

Formato por linha:
`<arquivo>|<required-1>||<required-2>|<forbidden-1>||<forbidden-2>`

Linhas iniciadas com `#` são comentários.

src/Server/Bud.Application/UseCases/Collaborators/PatchCollaborator.cs|PersonName.TryCreate(requestedFullName||UpdateProfile(|
src/Server/Bud.Application/UseCases/Checkins/CreateCheckin.cs|indicator.CreateCheckin(|
src/Server/Bud.Application/UseCases/Checkins/PatchCheckin.cs|indicator.UpdateCheckin(|
src/Server/Bud.Application/UseCases/Goals/CreateGoal.cs|goal.CollaboratorId|
src/Server/Bud.Application/UseCases/Goals/PatchGoal.cs|goal.CollaboratorId|
