# Value Object Mapping Guardrails

Matriz declarativa para garantir que serviços façam mapeamento explícito de campos críticos de request para Value Objects.

Formato por linha:
`<arquivo>|<required-1>||<required-2>|<forbidden-1>||<forbidden-2>`

Linhas iniciadas com `#` são comentários.

src/Bud.Server/Application/Collaborators/CollaboratorCommand.cs|PersonName.TryCreate(request.FullName||UpdateProfile(|
src/Bud.Server/Application/MetricCheckins/MetricCheckinCommand.cs|metric.CreateCheckin(||metric.UpdateCheckin(|
src/Bud.Server/Application/Missions/MissionCommand.cs|MissionScope.Create(request.ScopeType, request.ScopeId)||mission.SetScope(missionScope)|mission.SetScope(request.ScopeType, request.ScopeId)
