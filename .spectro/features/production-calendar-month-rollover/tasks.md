<!-- spectro-managed: true -->
# Tarefas: Atualização automática do mês na agenda de produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Componentes afetados | Conclusão verificável, testes esperados e risco | Status |
|---|---|---|---|---|---|---|---|
| `TASK-001` | Centralizar a referência de data da agenda em `TimeProvider` local do servidor e garantir que, sem `plannedDate`, o mês padrão é o atual, não o mês deduzido das tarefas. Preservar a referência quando `plannedDate` estiver preenchido. | `FR-001`, `FR-003`, `CON-002` | `AC-003`, `AC-004` | Nenhuma | `ProductionController`, `ProductionScheduleService`, composição de serviços, view model/view da agenda, testes de produção | Registrar `TimeProvider.System` e permitir relógio controlado nos testes. Cobrir mês atual sem tarefas, mês atual com tarefas e data explícita. Risco: diferença entre `DateTime.Today`, `DateTime.Now` e `ToLocalTime`; usar um único relógio local para a referência de calendário. | completed |
| `TASK-002` | Extrair a marcação/calculação do calendário para um partial compartilhado e acrescentar ações GET protegidas para informar o mês do servidor e renderizar o fragmento do mês atual, preservando o filtro de status. | `FR-001`, `FR-002`, `FR-003`, `CON-001`, `CON-002` | `AC-002`, `AC-003`, `AC-004` | `TASK-001` | `ProductionController`, `Views/Production/Index.cshtml`, novo partial da agenda, testes de controller/renderização | A tela inicial e o endpoint usam a mesma renderização; o fragmento contém contêiner substituível; a atualização não aceita nem substitui data explícita; status e ações antifalsificação continuam presentes. Cobrir mês, status, autorização de leitura e estado vazio nos testes. Risco: divergência visual/funcional entre renderização inicial e parcial; eliminar duplicação usando o mesmo partial. | completed |
| `TASK-003` | Adicionar script restrito à agenda de produção para detectar mudança do mês reportado pelo servidor e substituir o fragmento sem recarregar a página; não executar atualização automática com data selecionada. | `FR-001`, `FR-002`, `FR-003`, `NFR-001`, `CON-001` | `AC-001`, `AC-002`, `AC-004` | `TASK-002` | `Views/Production/Index.cshtml`, script da página em `wwwroot/js`, testes de renderização e aceitação manual | O polling só ocorre sem `plannedDate`, mantém status, evita chamadas sobrepostas, atualiza ao retornar à aba e preserva o DOM atual quando a rede falha. Verificar manualmente a virada sem recarga e a preservação da data explícita. Risco: timer de navegador pode ser suspenso em aba oculta; consultar novamente ao recuperar visibilidade. | running |
| `TASK-004` | Executar e registrar os gates locais obrigatórios e fechar a matriz de aceitação após as alterações. | `FR-001`, `FR-002`, `FR-003`, `NFR-001`, `CON-001`, `CON-002` | `AC-001`, `AC-002`, `AC-003`, `AC-004` | `TASK-001`, `TASK-002`, `TASK-003` | Solução e suíte `Craftsman.Tests`; evidências do Spectro | Passar build com warnings-as-errors, testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml`; registrar evidência de navegador para AC-001 e AC-004. Risco: a solução não tem harness de navegador identificado; se continuar sem harness, incluir evidência manual reproduzível sem adicionar dependência não aprovada. | blocked |

## Regras de execução

- Não alterar regras de planejamento, status, duração ou persistência das tarefas de produção.
- Manter a rota da agenda e os filtros existentes compatíveis; a atualização periódica não envia `plannedDate` quando a pessoa está no modo automático.
- A resposta do fragmento deve ser renderizada pelo servidor e protegida pela mesma política de leitura da tela.
- Não adicionar dependências de navegador ou persistência para implementar o comportamento.
- Resolver qualquer necessidade de mudança de escopo antes de implementá-la, conforme `.spectro/config.yaml`.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`
- Em andamento: `TASK-003` (aguardando aceitação manual em navegador)
- Bloqueadas: `TASK-004` (gate de cobertura abaixo dos limiares configurados)

## Evidências desta execução

- Testes direcionados: 43 aprovados no conjunto inicial; 2 testes adicionais de renderização do componente e 2 testes do partial Razor também passaram.
- Suíte completa: 237 aprovados, 0 falhas.
- Build com `--warnaserror`: aprovado, 0 avisos e 0 erros.
- Verificação de vulnerabilidades NuGet: aprovada; nenhum pacote vulnerável encontrado. Foi necessária execução fora do sandbox para acessar o feed NuGet.
- Sintaxe do script: `node --check src/App/wwwroot/js/production-calendar.js` aprovado.
- Snapshot e classes das migrations EF, DTOs, opções/configurações, registros de linha da UI, registros de composição de serviços e `Program` estão marcados com `[ExcludeFromCodeCoverage]`, conforme decisão humana.
- Gate de cobertura, com os limiares atuais do `.spectro/config.yaml`: falhou com cobertura total de linhas 59,55% (mínimo 70%) e branches 39,34% (mínimo 50%). O script só compara linhas novas já commitadas; com a árvore suja mostrou 0 linhas C# novas e métricas de cobertura nova como N/A.
- Aceitação de virada ao vivo no navegador (`AC-001`, `AC-004`): pendente; a suíte não contém harness de navegador.
