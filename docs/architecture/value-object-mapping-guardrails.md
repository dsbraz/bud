# Value Object Mapping Guardrails

Matriz declarativa para garantir que serviços façam mapeamento explícito de campos críticos de request para Value Objects.

Formato por linha:
`<arquivo>|<required-1>||<required-2>|<forbidden-1>||<forbidden-2>`

Linhas iniciadas com `#` são comentários.

src/Bud.Server/Services/CollaboratorService.cs|PersonName.TryCreate(request.FullName||UpdateProfile(|
src/Bud.Server/Services/MetricCheckinService.cs|metric.CreateCheckin(||metric.UpdateCheckin(|
src/Bud.Server/Services/MissionService.cs|MissionScope.Create(request.ScopeType, request.ScopeId)||mission.SetScope(missionScope)|mission.SetScope(request.ScopeType, request.ScopeId)
