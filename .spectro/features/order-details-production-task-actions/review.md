<!-- spectro-managed: true -->
# Revisão: Ações de tarefas na produção do detalhe do pedido

## Resumo

- Feature revisada: `order-details-production-task-actions`
- Revisão: `review-20261122T190418Z`
- Revisão de código: `07159ab7d6761048e51e65962fdb040995994e2e` (HEAD; o working tree contém somente atualizações geradas de `gates.json` e `state.json`)
- Decisão final: `approved`

A implementação atende à especificação esclarecida: a aba de produção do detalhe possui ações de iniciar, concluir e cancelar; ações incompatíveis ficam desabilitadas; cancelar exige confirmação; e o fluxo retorna ao detalhe do pedido associado, sem aceitar URL arbitrária.

## Artefatos examinados

- `.spectro/features/order-details-production-task-actions/spec.md`
- `.spectro/features/order-details-production-task-actions/clarifications.md`
- `.spectro/features/order-details-production-task-actions/plan.md`
- `.spectro/features/order-details-production-task-actions/tasks.md`
- `.spectro/features/order-details-production-task-actions/gates.json`
- `.spectro/constitution.md` e `.spectro/config.yaml`
- Código alterado em `ProductionController`, `ProductionScheduleService`, `Details.cshtml` e `OrderDetailsPage.razor`
- Testes de componentes, controller e integração HTTP
- Pacote independente em `.spectro/features/order-details-production-task-actions/reviews/review-20261122T190418Z/`

## Evidências verificadas

- `validate_traceability.ps1`: resultado `complete`; nenhum requisito ou critério de aceite sem tarefa/indicação de validação.
- `validate_gates.mjs`: relatório válido.
- `gates.json`: todos os gates obrigatórios passaram na execução `gate-20261122T190145Z`.
- Build com warnings-as-errors: aprovado.
- Testes: 433 aprovados, 0 falhas.
- Segurança de pacotes: aprovado pelo gate `dotnet list package --vulnerable`.
- Cobertura: linhas 88,68%, branches 73,99%, linhas novas 100,00% e branches novos 92,86%.
- Os testes verificam renderização dos controles, estados desabilitados, confirmação de cancelamento, transições, antiforgery, autorização, retorno ao pedido e rejeição de contexto de retorno incompatível.

## Requisitos e comportamento

- `FR-001`/`FR-002`: confirmados na tabela de produção com coluna `Acoes` e três controles por tarefa.
- `FR-003`: capacidades são propagadas do modelo já usado pela agenda; o domínio continua validando a transição no servidor.
- `FR-004`/`FR-005`: `/Production/Advance` e `ProductionScheduleService` continuam processando as ações e iniciando o pedido quando aplicável.
- `FR-006`: retorno contextual confirmado para sucesso e falha de transição; chamadas sem contexto preservam o redirecionamento da agenda.
- `FR-007`: cada formulário envia o identificador da própria tarefa.
- `FR-008`: cancelamento habilitado exige confirmação; botão incompatível permanece desabilitado.
- `NFR-001`/`CON-003`: autorização de escrita e antiforgery foram preservados.
- `NFR-002`: controles mantêm título, `aria-label`, ícones e estados equivalentes ao calendário.
- `NFR-003`/`CON-005`: nenhuma dependência, migration ou fluxo paralelo foi introduzido.
- `NFR-004`: falhas não produzem sucesso falso e retornam por caminho seguro.

## Achados

### Nenhum achado bloqueante

Não foram identificados achados `Critical` ou `High`. Não há achados `Medium` ou `Low` que exijam remediação antes da aprovação.

## Riscos residuais e limitações

- A confirmação de cancelamento é uma salvaguarda de interface; a segurança real continua dependendo de autorização, antiforgery e invariantes do domínio, que foram preservados.
- Não foi executado teste automatizado de navegador; a cobertura disponível valida o HTML renderizado e o pipeline HTTP. A troca para o detalhe ocorre por recarga HTTP, e a seleção da aba pode voltar ao estado inicial da página.
- O scanner de pacotes não substitui threat modeling, pentest ou revisão humana de autorização por recurso; esta feature valida a associação tarefa-pedido usada para o retorno, mas não introduz um modelo novo de permissões.

## Revisão de segurança

Revisão de segurança: concluída para o escopo da feature.

Escopo revisado: formulários da tabela de tarefas, token antiforgery, política de escrita, associação tarefa-pedido, redirecionamento contextual, validação de estado, auditoria, testes negativos e dependências NuGet.

Verificações executadas: revisão estática, testes unitários/de componente, testes de integração HTTP, gate de segurança de pacotes e gates de build/cobertura.

Achados: nenhum identificado no escopo revisado.

Riscos residuais / limitações: não há garantia de segurança absoluta; permanecem as limitações descritas acima sobre autorização por recurso já existente e ausência de navegador automatizado.

## Decisão

`approved` — a implementação está alinhada à especificação, ao plano, às clarificações e à constituição do projeto; os gates obrigatórios passaram e não há achados críticos ou altos pendentes.

