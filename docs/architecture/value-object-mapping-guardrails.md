# Value Object Mapping Guardrails

Matriz declarativa para garantir que serviços façam mapeamento explícito de campos críticos de request para Value Objects.

Formato por linha:
`<arquivo>|<required-1>||<required-2>|<forbidden-1>||<forbidden-2>`

Linhas iniciadas com `#` são comentários.

src/Bud.Server/Application/UseCases/Collaborators/PatchCollaborator.cs|PersonName.TryCreate(requestedFullName||UpdateProfile(|
src/Bud.Server/Application/UseCases/Metrics/CreateMetricCheckin.cs|metric.CreateCheckin(|
src/Bud.Server/Application/UseCases/Metrics/PatchMetricCheckin.cs|metric.UpdateCheckin(|
src/Bud.Server/Application/UseCases/Missions/CreateMission.cs|request.ScopeType.ToDomain()||MissionScope.Create(scopeType, request.ScopeId)||mission.SetScope(missionScope)|mission.SetScope(request.ScopeType, request.ScopeId)
src/Bud.Server/Application/UseCases/Missions/PatchMission.cs|request.ScopeType.Value.ToDomain()||MissionScope.Create(scopeType, scopeId)||mission.SetScope(missionScope)|mission.SetScope(request.ScopeType, request.ScopeId)
