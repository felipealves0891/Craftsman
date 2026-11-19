<!-- spectro-managed: true -->
# Revisão: Reverter movimentação de estoque

## Resumo

- Decisão: `approved`
- A implementação atende aos requisitos funcionais e os quatro gates obrigatórios passaram.
- A rastreabilidade está completa após a remoção dos intervalos de IDs nas tarefas.
- A concorrência real contra PostgreSQL foi explicitamente retirada do escopo; permanece como risco residual aceito nesta entrega.
- Não foram identificados achados críticos ou altos.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`
- `gates.json` e evidências da execução `gate-20261119T021309Z`
- Packet independente `reviews/review-20261119T021600Z/`
- `src/App/Services/InventoryAppService.cs`
- `src/App/Controllers/InventoryController.cs`
- `src/App/Views/Inventory/Movements.cshtml`
- `src/App/Models/ManualInventoryModels.cs`
- `src/Domains/Inventory/Repositories/IStockMovementRepository.cs`
- `src/Infra/Repositories/StockMovementRepository.cs`
- Testes de inventário de aplicação, controller e persistência
- `.spectro/constitution.md` e `.spectro/config.yaml`

## Evidências verificadas

- `gates.json` válido e associado à revisão de implementação `6022ec7cb0aeb53765abce6cc58ecc0fbfffec81`; o commit posterior `3b2532e` apenas registra os artefatos dos gates.
- Build com `--warnaserror`: aprovado.
- Testes: 382 aprovados.
- Segurança de pacotes: nenhum pacote vulnerável reportado.
- Cobertura: todos os limiares configurados aprovados.
- `validate_traceability.ps1`: resultado `complete`, sem requisitos ou critérios descobertos.
- A operação POST usa `ApplicationPolicies.Write` e `ValidateAntiForgeryToken`.
- A elegibilidade, o saldo e a criação/exclusão são decididos no servidor.
- O caminho compensatório preserva a movimentação original e usa referência determinística.
- O caminho excepcional exclui somente saída sem referência com motivo exatamente `Production consumption`.

## Requisitos e critérios verificados

- `FR-001` a `FR-009`: implementados nos serviços, controller, persistência e testes.
- `NFR-001`: unidade serializável utilizada quando disponível e efeitos persistidos atomicamente.
- `NFR-002`: idempotência sequencial e referência determinística cobertas; a validação concorrente real PostgreSQL está fora do escopo aprovado.
- `NFR-003` e `NFR-004`: autorização, mensagens seguras e rastreabilidade verificadas.
- `CON-001` a `CON-005`: fronteiras atuais preservadas, histórico compensado e saldo negativo rejeitado.
- `AC-001` a `AC-010`: vinculados a tarefas e validações; todos os gates passaram.

## Achados

### `FINDING-001` — risco residual aceito

- Severidade: Medium
- Categoria: test
- Localização: `NFR-002` / `AC-008`; estratégia de testes em `plan.md`
- Evidência: não há teste concorrente real contra PostgreSQL. Os testes existentes cobrem repetição sequencial, idempotência, unidade serializável e ausência de compensação duplicada observável no provider atual.
- Impacto: diferenças específicas do provider PostgreSQL em conflitos de serialização ou constraints podem permanecer sem validação direta.
- Decisão: risco aceito como limite explícito desta entrega, conforme o plano atualizado; não bloqueia aprovação nem cria tarefa pendente.
- Recomendação futura: se a política operacional exigir evidência de concorrência em produção, adicionar teste PostgreSQL em uma feature ou plano posterior.
- Etapa de retorno: `human_review`

## Riscos residuais

- A concorrência real contra PostgreSQL não foi medida; a implementação depende da unidade serializável do ambiente de produção.
- A referência de reversão depende da convenção de `BusinessReference`; uma constraint única futura exigiria migration pelo fluxo EF aprovado.
- A exclusão de `Production consumption` sem referência reduz a trilha histórica nesse caso excepcional, conforme regra de negócio aprovada.

## Decisão final

`approved`: não há achados críticos/altos, a rastreabilidade está completa, os gates obrigatórios passaram e o único risco médio foi explicitamente delimitado e aceito no escopo. O risco residual deve permanecer documentado para futuras mudanças de infraestrutura ou política operacional.

