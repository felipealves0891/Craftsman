<!-- spectro-managed: true -->
# Plano: Exibir mão de obra na tabela sem incorporá-la aos custos

## Contexto e objetivo técnico

- A implementação atual calcula `ProductionDurationHours * Product.HourlyRate`, soma esse valor ao custo de produção e persiste o resultado em `FinancialSettlement`.
- O mesmo modelo já persiste `LaborCostAmount`, e a página financeira possui cards derivados de `TotalCostAmount` e `MarginAmount`.
- O objetivo técnico é manter o valor monetário de mão de obra disponível como dado informativo da linha, mas fazer `ProductionCostAmount`, `TotalCostAmount` e `MarginAmount` refletirem somente os componentes financeiros vigentes sem o custo-hora.
- A tabela principal de `/Finance` deve ganhar uma coluna monetária de mão de obra. Os cards continuam consumindo apenas o resumo financeiro sem mão de obra.

## Solução proposta

- Manter o cálculo monetário de mão de obra para preencher `LaborCostAmount`, usando a regra já existente de duração da tarefa multiplicada pelo `Product.HourlyRate`.
- Alterar o cálculo do acerto para usar somente o custo de materiais em `ProductionCostAmount`; `LaborCostAmount` não será somado ao custo de produção, custo total ou margem.
- Ajustar a reconciliação histórica para preservar/calcular o valor monetário de mão de obra na coluna e atualizar o custo de produção com materiais somente.
- Normalizar os acertos já persistidos pela funcionalidade anterior, subtraindo de `ProductionCostAmount` o `LaborCostAmount` que foi incorporado anteriormente, sem remover o valor informativo da mão de obra.
- Expor `LaborCostAmount` no modelo de listagem e projetá-lo para `FinanceIndexPage.SettlementRow` com o formato monetário já usado pela tabela.
- Adicionar somente a coluna de mão de obra na tabela, atualizando o `colspan` do estado vazio. Não adicionar mão de obra aos cards nem alterar a tabela de detalhes fora do escopo.

## Arquitetura e contratos

- Domínio: `SettlementCostCalculator` continua sendo a fonte do cálculo monetário de mão de obra; `SettlementCalculator` passa a separar `materialCost` e `laborCost`, usando apenas `materialCost` como custo de produção.
- Domínio/aplicação: `FinancialSettlement` continua armazenando `LaborCostAmount`; a operação de reconciliação deve receber o valor monetário de mão de obra e o custo de produção material, sem somá-los.
- Aplicação: `FinancialSettlementListItemViewModel` recebe `LaborCostAmount`; `FinanceAppService.ToListItem` copia o valor sem incluí-lo em `FinancialSettlementSummaryViewModel`.
- Apresentação: `src/App/Views/Finance/Index.cshtml` passa o valor formatado para `FinanceIndexPage.SettlementRow`; `src/UI/Components/Finance/FinanceIndexPage.razor` renderiza a coluna monetária.
- Persistência: manter as colunas `labor_cost_amount` e `labor_cost_reconciled`. Criar uma migration de normalização para corrigir registros existentes que tiveram o custo-hora somado ao custo de produção.
- Fluxo de dados: tarefa/produto e movimentações -> cálculo de mão de obra e materiais -> acerto com custos separados -> modelo da listagem -> coluna monetária; cards usam somente o resumo de custo/margem.
- Erros: produto ausente continua sendo erro do cálculo do valor monetário da mão de obra; falhas individuais da reconciliação histórica continuam isoladas e diagnosticáveis; nenhuma falha deve salvar somente parte da correção.
- Concorrência e cache: preservar a transação por acerto do worker e invalidar o cache financeiro por meio do repositório após correções, para que cards e tabela não exibam valores antigos.
- Observabilidade: manter diagnósticos identificando acerto/pedido quando a reconciliação falhar, sem registrar dados sensíveis.

## Alternativas consideradas

- Remover `LaborCostAmount` e o cálculo de valor monetário: rejeitada porque a nova coluna precisa exibir o valor monetário de mão de obra.
- Manter o custo-hora em `ProductionCostAmount` e apenas ocultá-lo nos cards: rejeitada porque altera custo total/margem e viola a reversão solicitada.
- Calcular o valor monetário somente na UI: rejeitada porque duplicaria regra de domínio, não atenderia o histórico persistido e poderia divergir do valor usado na apuração.
- Excluir a coluna persistida e criar uma nova fonte paralela: rejeitada porque o modelo atual já possui o valor monetário e sua persistência, reduzindo a necessidade de nova estrutura.

## Riscos e compatibilidade

- Risco alto: acertos existentes podem ter `ProductionCostAmount` inflado; a migration deve ser idempotente e preservar `LaborCostAmount`, receita, desconto, envio, identidade e datas.
- Risco médio: chamadas e testes atuais assumem que produção é materiais + mão de obra; atualizar as expectativas sem alterar regras de receita, desconto ou envio.
- Risco médio: o worker histórico e o fluxo de novos acertos podem divergir; ambos devem usar a mesma separação entre custo material e valor informativo de mão de obra.
- Risco baixo: a alteração do record de listagem exige atualizar todos os construtores e testes de renderização.
- Compatibilidade: não remover `HourlyRate`, horas de produção, colunas financeiras ou o marcador de reconciliação; a mudança é semântica para o cálculo e aditiva na tabela.
- Fora do plano: alterações em telas/relatórios que não sejam a tabela financeira principal, novos apontamentos de horas e mudanças no cadastro de produtos.

## Estratégia de testes

- Domínio: verificar que mão de obra permanece em `LaborCostAmount`, enquanto `ProductionCostAmount`, `TotalCostAmount` e `MarginAmount` usam somente materiais, envio e desconto conforme as regras vigentes.
- Aplicação: verificar listagem com valor monetário de mão de obra, resumo sem soma de mão de obra, cache invalidado após atualização e detalhe preservando o valor persistido.
- Histórico/persistência: verificar migration e reconciliação com valor positivo, valor zero, ausência de tarefas, reexecução e preservação de campos não relacionados.
- UI: verificar cabeçalho/valor da coluna, formatação monetária, estado vazio e `colspan`; verificar que os cards não exibem nem somam mão de obra.
- Regressão: executar os gates obrigatórios configurados: build com avisos como erro, testes, vulnerabilidades de pacotes e cobertura.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002`, `CON-001`, `CON-003` | `AC-001`, `AC-006` | `TASK-001`, `TASK-002` |
| `FR-003` | `AC-002`, `AC-006` | `TASK-003`, `TASK-004` |
| `FR-004`, `CON-002` | `AC-003`, `AC-004`, `AC-005` | `TASK-003`, `TASK-004` |
| `NFR-001` | `AC-004`, `AC-005` | `TASK-003`, `TASK-004` |
| `NFR-002` | `AC-001`, `AC-006` | `TASK-001`, `TASK-003`, `TASK-004` |
| `CON-004` | `AC-003`, `AC-004` | `TASK-003`, `TASK-004` |
