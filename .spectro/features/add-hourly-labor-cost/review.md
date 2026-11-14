<!-- spectro-managed: true -->
# Revisão: Exibir mão de obra na tabela sem incorporá-la aos custos

## Resumo

- Decisão: `approved`
- Código revisado: `e23430cd0b6ad9a3b5e1a3a6bdaf32e21a7173a2`
- Base da revisão: `develop`
- Pacote independente: `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T195426Z/`
- Rastreabilidade: completa, sem requisitos ou critérios de aceite descobertos.
- Gates obrigatórios: build, testes, segurança e cobertura aprovados no `gate-20261114T195240Z`.

As remediações da revisão anterior foram confirmadas. O valor monetário de mão de obra permanece disponível apenas como informação da tabela; não compõe custo de produção, custo total, margem ou cards.

## Artefatos examinados

- `.spectro/features/add-hourly-labor-cost/spec.md`
- `.spectro/features/add-hourly-labor-cost/clarifications.md`
- `.spectro/features/add-hourly-labor-cost/plan.md`
- `.spectro/features/add-hourly-labor-cost/tasks.md`
- `.spectro/features/add-hourly-labor-cost/gates.json`
- `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T195426Z/manifest.json`
- `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T195426Z/traceability.json`
- Diff `develop..HEAD` e arquivos de domínio, aplicação, infraestrutura, UI, migrations e testes relacionados à feature.

## Requisitos verificados

- `FR-001` e `FR-002`: novos acertos mantêm `LaborCostAmount`, mas usam somente materiais em `ProductionCostAmount`; custo total e margem permanecem sem mão de obra.
- `FR-003`: cards e resumo continuam baseados apenas nos valores financeiros derivados sem mão de obra.
- `FR-004`: a listagem projeta o valor monetário e a tabela renderiza a coluna correspondente.
- `NFR-001` e `NFR-002`: formatação monetária e separação App/Domains/Infra foram preservadas.
- `CON-001` a `CON-004`: cadastro, horas, demais componentes financeiros e escopo da tabela foram preservados.
- `AC-001` a `AC-006`: cálculo, histórico, cards, coluna, zero válido e ausência foram cobertos por testes e evidências de gates.

## Remediações verificadas

### `FINDING-001` — Migration histórica

- Resolvido por `TASK-REV-001`.
- A migration `20261114150000_AddLaborCostInclusionMarker` adiciona `labor_cost_included_in_production`, classifica registros legados explicitamente e normaliza os valores uma única vez.
- Novos acertos persistem o marcador como não incorporado; reconciliações também preservam essa semântica.
- O teste de domínio confirma que a reconciliação mantém a mão de obra informativa e o custo material separado.

### `FINDING-002` — Zero versus ausência

- Resolvido por `TASK-REV-002`.
- `FinancialSettlementListItemViewModel.LaborCostAmount` agora é anulável.
- A aplicação retorna `null` para acertos ainda não reconciliados, preserva zero como valor válido e a UI exibe `Sem valor` quando não houver informação.
- Testes de aplicação e componente cobrem os dois estados.

## Testes e gates

- Testes direcionados das remediações: 35 aprovados.
- Suíte completa: 364 aprovados, 0 falhas.
- Build com `--warnaserror`: aprovado.
- Segurança de pacotes: aprovado.
- Cobertura: aprovada pelos limiares configurados.
- Evidências: `.spectro/features/add-hourly-labor-cost/outputs/gate-20261114T195240Z/evidence/`.
- Relatório: `.spectro/features/add-hourly-labor-cost/gates.json`.

## Achados

- Nenhum achado `Critical` ou `High` permanece.
- Nenhum achado `Medium` ou `Low` permanece sem remediação aceita.

## Riscos residuais

- A validação operacional de `dotnet ef migrations list` continua indisponível porque o startup project não referencia `Microsoft.EntityFrameworkCore.Design`; a migration foi compilada e o modelo/snapshot foram atualizados manualmente.
- Não foram observados riscos residuais que impeçam a aprovação funcional, arquitetural, de segurança ou de qualidade.

## Decisão final

`approved` — a implementação atende à especificação, ao plano, às clarificações, aos critérios de aceite e aos gates obrigatórios.
