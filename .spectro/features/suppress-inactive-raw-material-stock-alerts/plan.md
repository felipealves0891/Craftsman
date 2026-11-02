<!-- spectro-managed: true -->
# Plano: Suprimir alertas de estoque de matérias-primas inativas

## Contexto e objetivo técnico

- `StockAlertAppService` centraliza os três fluxos de alerta em escopo: `ListAlertsAsync` alimenta o sino global, `ListCriticalAlertsAsync` deriva os alertas críticos exibidos na tela de movimentos e `ListAlertsForOrderAsync` produz os avisos relacionados a pedidos.
- O método compartilhado `BuildAlertsAsync` classifica saldo e limites, mas hoje não considera `RawMaterialStatus`. Assim, uma matéria-prima inativa continua elegível para aviso ou estado crítico.
- Os alertas globais usam a chave de cache `inventory:alerts:all`. `RawMaterialRepository` já invalida o prefixo `inventory:` ao adicionar ou atualizar um material, mas o faz antes de `InventoryAppService` confirmar a unidade de trabalho. O objetivo técnico inclui uma invalidação posterior ao `SaveChangesAsync` para que uma consulta subsequente não reutilize o alerta anterior à inativação.
- A clarificação `Q-001` resolveu a única ambiguidade major: a supressão vale para todos os pontos de alerta atuais. Não há bloqueios pendentes para o planejamento.

## Solução proposta

- Aplicar a regra de elegibilidade por status dentro do caminho compartilhado de construção de alertas: somente matérias-primas com `RawMaterialStatus.Active` podem ser classificadas e convertidas em `StockAlertViewModel`.
- Manter `ListCriticalAlertsAsync` como derivação de `ListAlertsAsync` e `ListAlertsForOrderAsync` usando o mesmo construtor compartilhado. Dessa forma, sino global, tela de movimentos e avisos de pedidos recebem a mesma regra sem filtros duplicados em controllers ou views.
- Injetar opcionalmente `IApplicationCache` em `InventoryAppService`, seguindo o padrão já usado por outros serviços da aplicação. Depois que `SaveRawMaterialAsync` concluir `SaveChangesAsync` com sucesso, invalidar o prefixo `inventory:`. A invalidação abrange tanto os alertas quanto a lista cacheada de matérias-primas e vale para criação, inativação, reativação e mudança de limites.
- Preservar a invalidação existente nos repositórios. A invalidação adicional no serviço tem finalidade de consistência após o commit e não altera o contrato de persistência.
- Não alterar `RawMaterial.GetStockAlertLevel`, pois a classificação por saldo continua válida para telas de consulta fora das notificações e o status determina elegibilidade para alertar, não o cálculo do nível.

## Arquitetura e contratos

- `StockAlertAppService.BuildAlertsAsync`: permanece como ponto comum de transformação. Antes de consultar saldo ou produzir view model, ignora materiais cujo status não seja `Active`.
- `StockAlertAppService.ListAlertsAsync`: conserva chave e expiração atuais. Após invalidação, recompõe a coleção somente com materiais ativos.
- `StockAlertAppService.ListCriticalAlertsAsync`: não muda de contrato; continua filtrando os alertas globais já elegíveis pelo nível crítico.
- `StockAlertAppService.ListAlertsForOrderAsync`: não muda de contrato; materiais inativos relacionados à ficha técnica do pedido chegam ao caminho compartilhado e são suprimidos.
- `InventoryAppService`: recebe `IApplicationCache?` como dependência opcional para preservar construções existentes em testes e composições manuais. `SaveRawMaterialAsync` invalida `inventory:` somente depois de a persistência terminar com sucesso.
- Fluxo de dados: edição da matéria-prima → atualização do repositório → confirmação da unidade de trabalho → invalidação pós-commit → próxima consulta de alertas → carregamento do status persistido → exclusão do material inativo.
- Erros: se a persistência falhar, a invalidação pós-commit não ocorre e a exceção segue o tratamento atual. Não será criado novo erro de negócio nem mensagem para a pessoa usuária.
- Persistência: nenhuma alteração de entidade, tabela, migration, saldo, limite ou movimentação. A inativação continua sendo representada exclusivamente por `RawMaterialStatus.Inactive`.
- Concorrência: a invalidação pós-commit reduz a janela em que uma consulta concorrente pode preencher o cache com estado anterior ao commit. Consultas iniciadas depois do salvamento bem-sucedido devem recompor o resultado com o status persistido.
- Observabilidade: não é necessário novo log ou evento; o comportamento é observável pela ausência do material nas coleções de alerta e pela manutenção de saldos e movimentações.
- Segurança: controllers, autorização e dados expostos não mudam.

## Alternativas consideradas

- Filtrar separadamente no view component, na tela de movimentos e no controller de pedidos: rejeitada porque duplicaria a regra e permitiria divergência entre os três canais.
- Alterar `RawMaterial.GetStockAlertLevel` para sempre retornar `Normal` quando o material estiver inativo: rejeitada porque mistura elegibilidade de notificação com classificação de saldo e poderia alterar telas de consulta explicitamente fora do escopo.
- Remover limites mínimo/crítico ao inativar: rejeitada porque viola `CON-001`, perde configuração histórica e prejudica eventual reativação.
- Confiar apenas na expiração de dois minutos do cache: rejeitada porque permitiria alerta residual após uma inativação concluída, contrariando `FR-002`.
- Remover completamente o cache de alertas: não é necessário; o filtro central e a invalidação pós-commit preservam o mecanismo existente com mudança menor.

## Riscos e compatibilidade

- Um filtro aplicado somente ao carregamento global não cobriria alertas de pedidos. A mitigação é aplicá-lo em `BuildAlertsAsync` e testar todos os métodos públicos.
- Um resultado global previamente aquecido pode permanecer no cache se a invalidação não ocorrer depois do commit. A mitigação é um teste com cache preenchido antes da inativação e nova consulta depois do salvamento.
- A adição da dependência de cache ao construtor pode afetar instanciações manuais. Mantê-la opcional e ao final da assinatura preserva compatibilidade; a injeção de dependência resolve a instância registrada em produção.
- Invalidar `inventory:` é mais amplo que remover somente `inventory:alerts:all`, mas corresponde ao prefixo já adotado pelo repositório e impede que a lista cacheada de materiais retenha o status anterior.
- Materiais inativos continuarão aparecendo em cadastro, saldos e histórico. Testes de regressão devem impedir que a supressão de alertas seja confundida com exclusão de dados.
- Não há mudança de API externa, rota, view model, esquema ou dependência de pacote.

## Estratégia de testes

- Testes focados de `StockAlertAppService` com materiais ativo e inativo em faixas de aviso e crítica: confirmar que `ListAlertsAsync` e `ListCriticalAlertsAsync` retornam apenas ativos e preservam classificação e ordenação existentes (`AC-001`, `AC-003`, `AC-005`).
- Teste de alerta por pedido com produto cuja ficha técnica referencia uma matéria-prima inativa abaixo do limite: `ListAlertsForOrderAsync` não deve retorná-la (`AC-001`, `AC-005`).
- Teste integrado do ciclo de inativação com cache em memória ou fake comportamental: aquecer `inventory:alerts:all`, salvar o mesmo material como inativo por `InventoryAppService`, consultar novamente e confirmar coleção/contador sem o material e invalidação posterior ao salvamento (`AC-002`).
- Teste de preservação: depois da inativação, confirmar que `ListBalancesAsync` e `ListMovementsAsync` ainda expõem saldo e movimentações inalterados (`AC-004`, `CON-001`).
- Teste de regressão para material ativo: os limites existentes continuam produzindo aviso/crítico; eventual reativação volta a seguir a regra normal de materiais ativos (`FR-003`).
- Executar os gates obrigatórios de `.spectro/config.yaml`: build com avisos como erros, testes, verificação de vulnerabilidades e cobertura.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001`, `AC-004` | `TASK-001`, `TASK-003` |
| `FR-002` | `AC-002`, `AC-005` | `TASK-001`, `TASK-002` |
| `FR-003` | `AC-003` | `TASK-001` |
| `CON-001` | `AC-004` | `TASK-003` |
| `CON-002` | Todos os critérios aplicáveis | `TASK-001`, `TASK-002` |
