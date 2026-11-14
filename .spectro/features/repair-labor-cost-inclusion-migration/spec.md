<!-- spectro-managed: true -->
# Especificação: Corrigir a migration do marcador de inclusão do custo de mão de obra

## Problema e objetivo

- Problema: A aplicação consulta a coluna `financial_settlements.labor_cost_included_in_production`, mas o banco que atende a requisição não possui essa coluna, causando `Npgsql.PostgresException` 42703 durante a execução. A migration foi criada manualmente e o snapshot do Entity Framework Core foi alterado durante a implantação; como o snapshot é a referência usada para identificar diferenças do modelo, o EF Core entendeu que não havia alteração pendente e não gerou/aplicou a migration necessária.
- Objetivo: Restabelecer a compatibilidade entre o modelo persistido e o schema PostgreSQL, corrigindo exclusivamente as migrations da feature `add-hourly-labor-cost` para que sejam representadas e aplicadas por migrations geradas pelo `dotnet ef` a partir do contexto de design-time do projeto `Craftsman.Infra`.
- Referência dos artefatos: a migration `20261114120000_RevertLaborCostFromFinancialTotals` foi introduzida no commit `d82095eb4e9e9427c4c5086019f62c0e7b93e936`; o commit `e23430cd0b6ad9a3b5e1a3a6bdaf32e21a7173a2` a removeu e introduziu `20261114150000_AddLaborCostInclusionMarker`.

## Escopo

### Incluído

- Desfazer somente as migrations criadas manualmente para a feature `add-hourly-labor-cost` e restaurar um snapshot coerente com o modelo, preservando a ordem das alterações necessárias dessa feature.
- Gerar a migration do Entity Framework Core usando o `AppDbContext` e a factory de design-time disponíveis em `Craftsman.Infra`.
- Garantir que a migration gerada crie a coluna `labor_cost_included_in_production` na tabela `financial_settlements` com o mapeamento esperado pelo modelo.
- Preservar a compatibilidade do fluxo de inicialização que aplica migrations pendentes por `Database.MigrateAsync()`.
- Validar o schema resultante e o cenário que atualmente falha por coluna inexistente.

### Excluído

- Alterar a regra de negócio de inclusão ou exclusão do custo de mão de obra além do que já está definido no modelo e na migration corrigida.
- Reescrever ou apagar dados financeiros históricos sem uma decisão explícita sobre o tratamento dos registros existentes.
- Alterar o provedor PostgreSQL, a conexão da aplicação ou o mecanismo de execução automática de migrations.
- Corrigir outros erros de schema que não estejam relacionados à coluna `labor_cost_included_in_production`.
- Executar rollback destrutivo em banco compartilhado ou de produção sem confirmação do estado e do ambiente-alvo.

## Requisitos

### Requisitos funcionais

- `FR-001`: O conjunto de migrations da feature `add-hourly-labor-cost` no `Craftsman.Infra` deve representar uma sequência válida e rastreável para o modelo atual do `AppDbContext`, com snapshot e migrations produzidos pelo fluxo do Entity Framework Core e sem depender de migrations criadas manualmente.
- `FR-002`: Deve existir uma migration gerada pelo `dotnet ef` no projeto `Craftsman.Infra` que altere `financial_settlements` para disponibilizar a coluna `labor_cost_included_in_production` conforme o mapeamento persistente vigente.
- `FR-003`: A aplicação de todas as migrations pendentes em um banco compatível deve concluir sem erro e deixar o schema compatível com as consultas do repositório de acertos financeiros.
- `FR-004`: A consulta de acertos financeiros que atualmente falha por ausência da coluna deve executar sem `PostgresException` 42703 após a aplicação correta das migrations.
- `FR-005`: A aplicação das migrations recriadas deve ocorrer pelo fluxo do Entity Framework Core e registrar normalmente as migrations aplicadas em `__EFMigrationsHistory`.

### Requisitos não funcionais

- `NFR-001`: A geração da migration deve ser reproduzível usando o contexto de design-time existente em `src/Infra/Persistence/AppDbContextDesignTimeFactory.cs` e o projeto `src/Infra/Craftsman.Infra.csproj`.
- `NFR-002`: A correção deve manter a separação arquitetural existente, com o contexto, configurações e migrations na infraestrutura e sem dependência do domínio em Entity Framework Core.
- `NFR-003`: A alteração deve preservar rastreabilidade, permitir diagnóstico do estado de aplicação das migrations e não expor credenciais ou dados sensíveis em logs.

### Restrições

- `CON-001`: O projeto usado como origem da migration deve ser `Craftsman.Infra`, que contém o `AppDbContext` e o suporte de design-time.
- `CON-002`: A solução deve usar o fluxo oficial do Entity Framework Core (`dotnet ef`) para gerar as migrations da feature `add-hourly-labor-cost`; não deve manter migrations manuais dessa feature como substitutas dos artefatos gerados.
- `CON-005`: Migrations de outras features, mesmo que estejam no mesmo diretório de infraestrutura, estão fora do escopo e não devem ser removidas, regeneradas ou reordenadas.
- `CON-003`: Como as migrations manuais não estão registradas em `__EFMigrationsHistory`, a correção deve tratar o banco como não tendo essas migrations aplicadas e não deve criar uma estratégia de reparo de histórico inexistente.
- `CON-004`: Nenhum dado financeiro deve ser descartado ou recalculado durante a correção sem requisito ou decisão explícita sobre os registros históricos.

## Critérios de aceite

- `AC-001`: Dado o projeto `Craftsman.Infra` e o `AppDbContext` atual, quando as migrations da feature `add-hourly-labor-cost` forem geradas pelo fluxo `dotnet ef`, então os artefatos produzidos incluirão arquivos, designers e snapshot necessários para serem reconhecidos pelo Entity Framework Core e refletirão as alterações da feature no modelo.
- `AC-002`: Dado um banco cujo histórico esteja no último estado anterior ao marcador de inclusão, quando as migrations pendentes forem aplicadas, então `financial_settlements.labor_cost_included_in_production` existirá com tipo, nulabilidade e valor padrão compatíveis com o mapeamento vigente.
- `AC-003`: Dado um banco sem registro das migrations manuais em `__EFMigrationsHistory`, quando as migrations recriadas forem aplicadas pelo `dotnet ef`, então cada migration executada será registrada normalmente no histórico do EF Core.
- `AC-004`: Dado o banco após a aplicação bem-sucedida das migrations, quando a aplicação consultar ou carregar um acerto financeiro, então a requisição não produzirá `Npgsql.PostgresException` 42703 para `labor_cost_included_in_production`.
- `AC-005`: Dado o cenário confirmado de ausência das migrations manuais em `__EFMigrationsHistory`, quando a correção for validada, então não será executado rollback de histórico inexistente nem será criada uma migration manual de reparo para mascarar a ausência de rastreamento.
- `AC-006`: Dado um banco com registros financeiros existentes, quando a migration corrigida for aplicada, então os registros permanecerão disponíveis e nenhum valor histórico será alterado fora do comportamento explicitamente definido para a migration.

## Riscos, dependências e perguntas abertas

- Dependência: `src/Infra/Persistence/AppDbContextDesignTimeFactory.cs`, `src/Infra/Craftsman.Infra.csproj`, `AppDbContextModelSnapshot.cs` e o histórico `__EFMigrationsHistory` do ambiente afetado.
- Dependência: Acesso ao banco PostgreSQL do ambiente que apresentou o erro para verificar migrations aplicadas e a existência real da coluna.
- Risco: O snapshot e os artefatos manuais podem não refletir exatamente a sequência de alterações esperada; a sequência recriada deve ser conferida antes da aplicação.
- Risco: A migration manual atual contém atualização de valores financeiros legados, mas não há requisito para executar essa lógica manualmente no escopo confirmado; ela não deve ser preservada como operação separada sem que o `dotnet ef` a gere ou haja requisito explícito.
- Limite de escopo: A correção se aplica somente às migrations da feature `add-hourly-labor-cost`; migrations de outras features permanecem intactas.
- Limite de escopo: As alterações funcionais dos commits de referência permanecem preservadas; a correção incide nos artefatos de migration/snapshot e na validação do fluxo de persistência.
