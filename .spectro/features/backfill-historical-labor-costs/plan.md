<!-- spectro-managed: true -->
# Plano: Recalcular custo de horas em acertos históricos

## Contexto e objetivo técnico

- A migração `AddLaborCost` adicionou `LaborCostAmount` com valor padrão zero nos acertos existentes. O cálculo de novos acertos já soma horas ao custo de produção, mas o histórico precisa ser reconciliado.
- O objetivo técnico é executar uma reconciliação determinística em background, sem bloquear a aplicação, preservando a identidade do acerto e atualizando apenas o custo de horas e os valores derivados.
- A rotina usará o `HourlyRate` atual, processará cada acerto antigo ainda não reconciliado e continuará mesmo que um registro individual falhe.

## Solução proposta

- Introduzir um marcador persistido de reconciliação (`LaborCostReconciled`) com default `false` para dados legados. Novos acertos calculados pelo fluxo normal serão persistidos como reconciliados.
- Adicionar ao domínio uma operação explícita de reconciliação que atualize `LaborCostAmount`, `ProductionCostAmount` e o marcador, preservando `Id`, `OrderId`, receita, desconto, envio, status e `CalculatedAt`.
- Adicionar ao repositório financeiro consultas para localizar acertos não reconciliados e atualização por identidade. A atualização não deve excluir e recriar o registro.
- Extrair ou compartilhar a regra de composição de custo de materiais e horas entre o `SettlementCalculator` e o processador histórico, evitando que os dois fluxos tenham fórmulas divergentes.
- Criar um serviço de aplicação para processar um acerto por vez, em transação isolada. O custo de materiais será recalculado a partir das movimentações existentes; o custo de horas será `ProductionDurationHours * Product.HourlyRate`; o custo de produção será material + horas.
- Criar um `BackgroundService` registrado no container da aplicação. Ele criará escopos para os serviços scoped, processará os acertos sequencialmente e capturará/logará falhas por acerto. A falha do worker não será propagada para interromper o host.
- Manter a UI existente: como listagem, resumo e detalhe já leem `LaborCostAmount`, `ProductionCostAmount`, `TotalCostAmount` e `MarginAmount` persistidos, a reconciliação refletirá automaticamente no histórico após a atualização.

## Arquitetura e contratos

- **Domínio financeiro:** `FinancialSettlement` ganhará o marcador de reconciliação e um método de alteração controlada para aplicar o custo histórico. As propriedades derivadas continuarão calculando custo total e margem a partir do estado do objeto.
- **Contrato de persistência:** `IFinancialSettlementRepository` ganhará uma consulta de acertos pendentes e uma operação de atualização por `Id`; a operação deve preservar a chave e os campos fora do escopo.
- **Serviço de aplicação:** um contrato/processador de reconciliação receberá o `FinancialSettlement`, o pedido, as tarefas, os produtos e as movimentações necessárias, validará associações e aplicará a atualização dentro de unidade transacional.
- **Cálculo compartilhado:** a fórmula vigente deve ficar em uma unidade reutilizável ou em método comum ao cálculo normal e ao histórico. A duração persistida da tarefa será usada diretamente, sem multiplicar novamente por `Quantity`.
- **Background:** o worker usará `IServiceScopeFactory`/escopos equivalentes para não compartilhar `DbContext` entre operações concorrentes. A execução será sequencial por acerto para isolar falhas e evitar concorrência indevida no contexto EF; “em paralelo” significa paralelo ao restante da aplicação.
- **Persistência:** adicionar coluna booleana não nula com default `false`, configuração EF Core, conversões de entidade/modelo, snapshot e migration reversível. O índice único por `OrderId` será preservado.
- **Transação e cache:** cada acerto será atualizado e salvo atomicamente; após sucesso, os caches financeiros serão invalidados pelo repositório existente. Falhas não devem deixar alteração parcial.
- **Observabilidade:** logs estruturados devem indicar início/fim, quantidade encontrada, acerto/pedido processado e exceções por registro, sem dados sensíveis. O worker deve respeitar cancelamento no encerramento.

## Alternativas consideradas

- Usar apenas `LaborCostAmount == 0` para descobrir pendências: rejeitado, pois custo de horas zero pode ser válido e não distingue acerto legado de acerto já reconciliado.
- Usar somente uma data fixa de implantação para selecionar registros: rejeitado como único marcador, pois não representa de forma confiável reexecução nem registros reconciliados com custo zero.
- Recalcular chamando `SettlementCalculator` e substituir o acerto: rejeitado, pois criaria nova identidade/data e poderia alterar campos fora do escopo; a reconciliação deve preservar o registro histórico.
- Excluir e recriar o acerto via `ReplaceForOrderAsync`: rejeitado pela exigência de preservar identidade e rastreabilidade.
- Executar todos os acertos em paralelo: rejeitado para o processamento interno, pois os serviços/repositórios usam `DbContext` scoped e a execução sequencial por item oferece isolamento mais simples; o worker continua paralelo ao host.
- Bloquear a inicialização até concluir: rejeitado pela decisão `DEC-004`; a correção histórica não pode impedir o atendimento da aplicação.

## Riscos e compatibilidade

- **Elegibilidade de dados legados:** o marcador novo precisa ter default `false`; novos acertos devem gravá-lo como `true`, caso contrário o worker poderá reprocessar acertos recentes.
- **Valor-hora mutável:** o histórico será calculado com o `HourlyRate` atual conforme `DEC-002`; isso pode divergir do valor vigente na criação do acerto e deve ficar registrado como comportamento aprovado.
- **Dados inconsistentes:** produto ausente, tarefa órfã ou pedido inexistente deve falhar somente aquele item, sem salvar parcialmente e sem interromper o host.
- **Concorrência com geração manual:** o índice único existente e a atualização por identidade devem ser preservados; o processador deve revalidar o estado antes de gravar para não sobrescrever um acerto já reconciliado.
- **Migração:** a nova coluna deve ser compatível com PostgreSQL e com o snapshot do EF Core; registros existentes devem permanecer legíveis antes e depois da migration.
- **Framework e dependências:** preservar os projetos e versões efetivamente usadas; não adicionar pacote para hosted services, transação ou cálculo.

## Estratégia de testes

- **Domínio:** testar aplicação do custo, derivação de custo total/margem, preservação de campos e rejeição de valores inválidos; testar que um acerto marcado como reconciliado não seja reaplicado.
- **Serviço de reconciliação:** cobrir cálculo com uma/múltiplas tarefas e produtos, custo de materiais + horas, ausência de tarefas, `HourlyRate` zero, produto/pedido ausente, reexecução e isolamento por item.
- **Persistência:** testar migration/configuração, valor default para dados legados, round-trip do marcador e atualização que preserva `Id`, pedido e data; validar invalidação do cache.
- **Hosted service:** testar criação de escopo, execução em background, continuação após exceção individual, logs/contagem e cancelamento sem propagar erro ao host.
- **Regressão:** manter testes do `SettlementCalculator`, `FinanceAppService`, handler de entrega, listagem/resumo/detalhe e deduplicação de acertos.
- **Gates:** executar build com warnings-as-errors, suíte de testes, vulnerabilidades e cobertura conforme `.spectro/config.yaml`; validar todos os `AC-001` a `AC-009` por testes ou evidência de integração.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001`, `AC-006` | `TASK-002`, `TASK-004`, `TASK-006` |
| `FR-002` | `AC-002`, `AC-005` | `TASK-003`, `TASK-006` |
| `FR-003` | `AC-003`, `AC-004`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-005`, `TASK-006` |
| `FR-004` | `AC-003`, `AC-004`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-005` |
| `FR-005` | `AC-006` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-006` |
| `FR-006` | `AC-002`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-005` |
| `FR-007` | `AC-007`, `AC-008` | `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006` |
| `NFR-001` | `AC-001`, `AC-007` | `TASK-004`, `TASK-006` |
| `NFR-002` | `AC-003`, `AC-007`, `AC-008` | `TASK-002`, `TASK-003`, `TASK-005` |
| `NFR-003` | `AC-002`, `AC-005` | `TASK-001`, `TASK-003` |
| `NFR-004` | `AC-007`, `AC-008` | `TASK-004`, `TASK-006` |
| `NFR-005` | `AC-001` a `AC-009` | `TASK-006` |
| `CON-001` | `AC-002` | `TASK-003` |
| `CON-002` | `AC-002`, `AC-005` | `TASK-003` |
| `CON-003` | `AC-003`, `AC-004`, `AC-006` | `TASK-001`, `TASK-002`, `TASK-005` |
| `CON-004` | `AC-003`, `AC-006` | `TASK-002`, `TASK-005` |
| `CON-005` | `AC-006`, `AC-009` | `TASK-003`, `TASK-006` |

