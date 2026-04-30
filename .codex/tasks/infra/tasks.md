# Infra Tasks

## INFRA-001 - Contexto EF Core em design-time e recriacao das migrations
- [ ] Criar uma classe de design-time para instanciar `AppDbContext` no projeto `Infra`.
- [ ] Recriar as migrations do `AppDbContext` a partir do modelo atual.
- [ ] Validar build e testes automatizados da solucao.

Criterios de aceitacao:
- O comando `dotnet ef migrations add` deve conseguir criar migrations usando o projeto `src/Infra`.
- A migration inicial recriada deve refletir o modelo EF atual, incluindo Identity, auditoria, eventos de dominio, Shopee e demais entidades persistidas.
- A solucao deve compilar.
