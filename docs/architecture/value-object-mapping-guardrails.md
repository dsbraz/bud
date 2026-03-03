# Value Object Mapping Guardrails

Matriz declarativa para garantir que serviços façam mapeamento explícito de campos críticos de request para Value Objects.

Formato por linha:
`<arquivo>|<required-1>||<required-2>|<forbidden-1>||<forbidden-2>`

Linhas iniciadas com `#` são comentários.

src/Bud.Server/Application/UseCases/Collaborators/PatchCollaborator.cs|PersonName.TryCreate(requestedFullName||UpdateProfile(|
src/Bud.Server/Application/UseCases/Checkins/CreateCheckin.cs|indicator.CreateCheckin(|
src/Bud.Server/Application/UseCases/Checkins/PatchCheckin.cs|indicator.UpdateCheckin(|
src/Bud.Server/Application/UseCases/Goals/CreateGoal.cs|goal.CollaboratorId|
src/Bud.Server/Application/UseCases/Goals/PatchGoal.cs|goal.CollaboratorId|
