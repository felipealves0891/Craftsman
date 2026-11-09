<!-- spectro-managed: true -->
# Tarefas: Adicionar valor das horas ao custo financeiro

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Componentes afetados | Conclusão verificável, testes esperados e risco | Status |
|---|---|---|---|---|---|---|---|
| `TASK-001` | Estender `FinancialSettlement` para receber e preservar `LaborCostAmount`, validar o valor, manter `ProductionCostAmount` como materiais + horas e derivar custo total/margem sem alterar receita, envio ou desconto. | `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `CON-003` | `AC-003`, `AC-004`, `AC-009` | Nenhuma | `src/Domains/Finance/Entities/FinancialSettlement.cs`, testes de `FinancialSettlement` | O objeto de domínio expõe o custo de horas, rejeita valores negativos, inclui o total correto e continua emitindo o evento financeiro com a margem correta. Atualizar todos os construtores/usos de teste. Risco: alterar a assinatura pode deixar chamadas legadas inconsistentes; localizar e atualizar todos os usos sem mudar regras não relacionadas. | completed |
| `TASK-002` | Alterar `SettlementCalculator` para resolver cada `ProductionTask.ProductId` pelo `IProductRepository`, calcular `ProductionDurationHours * Product.HourlyRate` sem multiplicar novamente por `Quantity`, somar ao custo de materiais e preservar elegibilidade/seleção atuais. | `FR-001`, `FR-002`, `FR-003`, `FR-005`, `NFR-003`, `CON-001`, `CON-002`, `CON-003` | `AC-001`, `AC-002`, `AC-005`, `AC-007`, `AC-008`, `AC-009` | `TASK-001` | `src/Domains/Finance/Services/SettlementCalculator.cs`, registro DI, repositório de produtos, testes de cálculo e de aplicação | Cobrir um produto, múltiplos produtos/tarefas, duração já escalada, ausência de tarefas, produto ausente, pedido não entregue e cálculo duplicado pelo serviço/handler. Produto ausente deve falhar antes de criar acerto. Risco: chamadas concorrentes ao mesmo `DbContext`; usar o padrão de acesso sequencial/cacheado já suportado pelos repositórios. | completed |
| `TASK-003` | Persistir `LaborCostAmount` na entidade financeira, conversões, configuração EF Core, snapshot e nova migration com `numeric(18,2)` e default zero para acertos existentes. | `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `CON-004` | `AC-003`, `AC-004`, `AC-006` | `TASK-001` | `FinancialSettlementEntity`, `FinancialSettlementConfiguration`, `FinancialSettlementRepository`, `src/Infra/Persistence/Migrations`, testes de persistência | Migration `Up`/`Down` é reversível, o round-trip preserva o valor, registros anteriores carregam zero e o índice único de pedido permanece intacto. Risco: snapshot/migration divergentes; gerar/verificar ambos pelo fluxo EF Core existente e revisar o diff antes dos gates. | completed |
| `TASK-004` | Expor o custo de horas nos modelos, view de detalhes e componente financeiro, mantendo custo total, margem, índice e resumos derivados do acerto persistido. | `FR-004`, `NFR-002` | `AC-003`, `AC-004`, `AC-006` | `TASK-001`, `TASK-003` | `src/App/Models/FinancialSettlementDetailViewModel.cs`, `FinanceAppService`, `src/App/Views/Finance/Details.cshtml`, `src/UI/Components/Finance/FinancialSettlementDetailsPage.razor`, testes de aplicação/UI | O detalhe exibe custo de produção total e custo de horas separadamente; o índice e KPIs exibem totais/margem atualizados; testes de renderização e mapeamento passam sem alterar rotas/autorização. Risco: records e parâmetros Razor posicionais; atualizar view, componente e testes em conjunto. | completed |
| `TASK-005` | Executar validação integrada da feature e registrar evidências dos critérios e gates obrigatórios. | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-005`, `NFR-001`, `NFR-002`, `NFR-003`, `CON-001`, `CON-002`, `CON-003`, `CON-004` | `AC-001` a `AC-009` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004` | Solução, `tests/Craftsman.Tests`, `.spectro/config.yaml`, evidências do Spectro | Build com warnings-as-errors, suíte de testes, segurança e cobertura passaram. Contra `origin/develop`, o código novo atingiu 100% de linhas e 100% de branches; cobertura total ficou em 82,18% de linhas e 56,57% de branches. | completed |

## Regras de execução

- Não adicionar configuração global de valor-hora em `appsettings`; usar somente `Product.HourlyRate`.
- Não multiplicar `ProductionTask.ProductionDurationHours` por `Quantity`; a duração já representa a quantidade planejada.
- Não alterar planejamento, apontamento, status ou seleção de tarefas do fluxo financeiro existente.
- Persistir o custo de horas calculado e manter acertos já existentes imutáveis quando o produto for editado posteriormente.
- Preservar a separação entre domínio, aplicação e infraestrutura, usando os repositórios e padrões de EF Core já presentes.
- Não adicionar dependências ou mudar a versão do framework. Mudanças de escopo exigem aprovação humana conforme `.spectro/config.yaml`.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`.
- Bloqueadas: nenhuma.

## Evidências da execução

- Testes direcionados de domínio/cálculo: 7 aprovados.
- Testes direcionados de finanças/UI: 71 aprovados.
- Suíte completa após a correção da expectativa de não dupla contagem e do cenário de produto ausente: 347 aprovados, 0 falhas.
- Build com `--warnaserror`: aprovado, 0 avisos e 0 erros.
- Segurança NuGet: aprovada, nenhum pacote vulnerável encontrado.
- Cobertura contra `develop`: linhas 82,18% (mínimo 81%) aprovada; branches 56,57% (mínimo 55%) aprovado.
- Cobertura contra `origin/develop`: linhas novas 100% (mínimo 90%) aprovada; branches novos 100% (mínimo 85%) aprovado.

