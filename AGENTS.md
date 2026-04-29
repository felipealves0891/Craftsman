# AGENTS.md
O arquivo .codex/PRODUCT.md contém informações sobre o produto, leia e, em caso de dúvidas, pode me perguntar

## Regras Gerais
- Sempre leia os arquivos em /specs antes de implementar
- Nunca implemente sem critérios de aceitação
- O código deve ser simples e legível
- Evite overengineering

## Fluxo de Trabalho Necessário
1. Leia as especificações no diretório /specs
2. Gere tasks.md se ele não existir
3. Implemente com base nas tarefas
4. Crie testes automatizados, unitarios e de integração
5. Garanta que todos os critérios de aceitação sejam atendidos
6. Sempre após de finalizar uma tarefa, marque ela como concluida

## Testes
- Priorize a cobertura dos critérios de aceitação
- Os testes devem ser claros e diretos

## Restrições
- Não invente requisitos que não estejam descritos
- Não altere o comportamento sem atualizar a especificação

## Observações
- Quando for fazer o build, use a variavel de ambiente DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet

# Repository Guidelines

## Project Structure & Module Organization

Craftsman is a .NET 8 Razor application organized as a layered solution. `Craftsman.slnx` is the solution entry point. Source code lives under `src/`: `src/App` contains MVC controllers, Razor views, view models, application services, and `wwwroot` assets; `src/Domains` contains business entities, services, events, value objects, and contracts; `src/Infra` contains EF Core persistence, migrations, repositories, caching, seed data, and integration services. Tests live in `tests/Craftsman.Tests`, grouped by feature area such as `Sales`, `Inventory`, `Production`, `Shipping`, `Finance`, `Application`, and `Integration`.

## Build, Test, and Development Commands

- `dotnet restore Craftsman.slnx`: restore NuGet packages.
- `dotnet build Craftsman.slnx`: compile the solution with nullable reference checks enabled.
- `dotnet test Craftsman.slnx`: run the xUnit test suite.
- `dotnet run --project src/App/Craftsman.csproj`: run locally. PostgreSQL must match `src/App/appsettings*.json`.
- `docker compose up --build`: build and run the app, PostgreSQL, and pgAdmin. The app is exposed at `http://localhost:8080`.

In `Development`, the app applies EF Core migrations and runs `DevelopmentDataSeeder` automatically on startup.

## Coding Style & Naming Conventions

Use C# with `net8.0`, nullable reference types, and implicit usings. Follow standard .NET naming: PascalCase for public types and members, camelCase for locals and parameters, interfaces prefixed with `I`, and async methods suffixed with `Async`. Keep domain rules in `src/Domains`, persistence in `src/Infra`, and UI/application orchestration in `src/App`.

## Testing Guidelines

Tests use xUnit with `Microsoft.NET.Test.Sdk` and `coverlet.collector`. Name test classes after the behavior under test, for example `OrderTests` or `ShipmentRepositoryTests`, and place them in the matching feature folder. Add integration tests when EF mappings, repositories, or import pipelines change. Run `dotnet test Craftsman.slnx` before opening a PR.

## Commit & Pull Request Guidelines

Recent commits use short Portuguese imperative or summary subjects, for example `ajustado agenda` and `Concluido fase 4`. Keep commits focused and use concise subject lines that describe the completed change. Pull requests should include a clear description, linked issue or task when available, test results, and screenshots for Razor view changes. Call out database migrations, seed data changes, and Docker or configuration updates explicitly.

## Security & Configuration Tips

Do not commit real credentials. Development credentials in `docker-compose.yml` are local-only defaults. Keep connection string changes in `appsettings.Development.json` or environment variables, and avoid changing the persisted Docker volume paths unless the setup documentation is updated.
