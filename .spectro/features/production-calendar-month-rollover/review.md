<!-- spectro-managed: true -->
# Revisão: Atualização automática do mês na agenda de produção

- Review ID: `review-20261102T202152Z`
- Revisão examinada: `a56ed942cc07d1ff21648629e1d66739da6e6b18`
- Base: `main`
- Decisão: `needs_human_review`

## Resumo

A implementação da agenda está alinhada aos requisitos funcionais, possui testes de controller, renderização e comportamento do script, e passou pelos quatro gates configurados. A validação de rastreabilidade não encontrou requisitos ou critérios de aceitação sem vínculo.

As duas divergências identificadas foram revistas e aceitas pelo usuário: os limiares menores de cobertura são intencionais e serão elevados gradualmente; as remoções documentais são mantidas como parte intencional desta linha de trabalho. Não restam achados bloqueantes ou lacunas de rastreabilidade.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`, `gates.json` e `state.json`.
- `.spectro/constitution.md` e `.spectro/config.yaml`.
- `ProductionController`, `ProductionScheduleService`, a view e o partial da agenda, registro de `TimeProvider` e `production-calendar.js`.
- Testes de controller, renderização, serviço e `tests/production-calendar.test.mjs`.
- Evidências de build, testes, segurança e cobertura em `outputs/gate-20261102T201927Z/evidence/`.
- Diff da revisão de `main` até a revisão examinada.
- Pacote independente: `reviews/review-20261102T202152Z/review-packet.md` e `manifest.json`.

## Integridade das evidências

O manifesto da revisão identifica `a56ed94` como a revisão atual. O relatório de gates identifica `839b74f`; a comparação entre `839b74f` e `a56ed94` contém apenas alterações a `gates.json` e `state.json`, sem mudanças no código ou nos artefatos de especificação. O hash de `tasks.md` no relatório de gates coincide com o hash do manifesto da revisão. Assim, a evidência de gates é aplicável ao código revisado.

O relatório dos gates foi validado pelo `validate_gates.mjs`. Build, testes, segurança e cobertura passaram de acordo com os limiares atualmente configurados.

## Rastreabilidade e requisitos verificados

`validate_traceability.ps1` retornou `complete`: todos os requisitos (`FR-001` a `FR-003`, `NFR-001`, `CON-001` e `CON-002`) estão vinculados a tarefas e ao plano; os critérios `AC-001` a `AC-004` têm referências de validação.

- `FR-001`, `FR-002`, `AC-002` e `AC-003`: o controller usa `TimeProvider` local para escolher o mês padrão; o endpoint de mês e o endpoint do partial usam a mesma referência temporal; a view inicial e o endpoint compartilham `_Calendar`.
- `FR-003` e `AC-004`: a data selecionada é preservada na view e desativa a atualização automática. Os testes de serviço e controller cobrem a seleção explícita.
- `NFR-001`, `AC-001`: o script consulta o mês do servidor a cada 30 segundos, pausa o timer quando a aba fica oculta e consulta ao retornar. Evita requisições concorrentes, preserva o fragmento em falha e impede substituir o DOM se data, status ou fragmento mudar durante a requisição.
- Segurança: o controller mantém a política de leitura no nível da classe; as respostas GET não são armazenadas em cache; o fragmento é renderizado no servidor e mantém o token antifalsificação usado pelas ações existentes.
- A suíte registrada passou com 305 testes; os 7 testes de comportamento JavaScript também passaram em execução direcionada.

## Achados

### FINDING-001 — Exceção temporária de cobertura aprovada pelo usuário

- **Severidade:** Medium
- **Estado:** aceito pelo usuário em 2026-11-02; aumento gradual dos limiares permanece como direção futura.
- **Categoria:** consistency
- **Localização:** `.spectro/config.yaml:25`; `.spectro/constitution.md:35,55`; `gates.json` / evidência `04-coverage.log`
- **Evidência:** a Constituição registra 90% de linhas e 75% de branches totais; o gate usa 70% e 50%, e passou com 72,94% e 50,00%. O usuário confirmou que diminuiu os limiares intencionalmente porque a cobertura anterior era muito baixa e que pretende elevá-los aos poucos. As métricas de código novo passaram.
- **Impacto:** a política escrita ainda apresenta a meta futura como se fosse o mínimo atual. A decisão esclarece que os valores atuais são uma etapa transitória, sem exigir que esta feature alcance imediatamente 90%/75%.
- **Recomendação:** manter os limiares atuais como baseline aprovado e documentar uma progressão quando os incrementos forem definidos, para que a Constituição diferencie a meta final do gate vigente.
- **Etapa de retorno:** `human_review`

### FINDING-002 — Remoções extensas de documentação não estão rastreadas pela feature

- **Severidade:** Medium
- **Estado:** aceito pelo usuário em 2026-11-02; as remoções serão mantidas.
- **Categoria:** scope
- **Localização:** commit `2b5e733`; exclusões de `.agents/`, `AGENTS.md`, `prompts/` e `specs/`
- **Evidência:** o diff de `main` inclui 31 arquivos removidos, totalizando cerca de 10 mil linhas de guias, prompts e especificações anteriores. `spec.md`, `plan.md` e `tasks.md` desta feature não descrevem essa migração nem ligam essas exclusões a critérios de aceitação.
- **Impacto:** a feature inclui uma mudança de escopo ampla e sem rastreabilidade no plano aprovado; documentos de requisitos e instruções deixam de estar disponíveis na árvore atual.
- **Recomendação:** aceite registrado nesta revisão. Se a migração voltar a ser alterada, documentar o destino/substituição dos artefatos em um plano próprio.
- **Etapa de retorno:** `plan`

## Riscos residuais

- A aceitação manual de `AC-001` e `AC-004` foi dispensada por autorização explícita do usuário; não houve observação em navegador. Os testes de JavaScript usam DOM e `fetch` simulados, portanto não verificam a integração real entre browser, autenticação, endpoints e renderização Razor durante uma virada de mês.
- A cadência de 30 segundos é razoável para polling, mas o atraso real percebido não foi medido em navegador.
- Os documentos removidos permanecem disponíveis no histórico Git; esta revisão não avaliou uma migração individual de cada documento para o novo fluxo Spectro.

## Decisão final

`approved`. O desvio de cobertura e as remoções de documentação foram aceitos pelo usuário. A validação em navegador permanece dispensada, conforme registrado em `tasks.md`.
