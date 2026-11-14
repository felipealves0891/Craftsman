<!-- spectro-managed: true -->
# Especificação: Corrigir a inclusão de custo de mão de obra

## Problema e objetivo

- Problema: alterações da feature `add-hourly-labor-cost` foram revertidas parcialmente, mas dados já persistidos com custo de mão de obra permanecem nos acertos financeiros e a rotina de reconciliação histórica continua registrada como serviço em background. Isso pode manter ou reaplicar custos que não fazem mais parte da regra financeira vigente.
- Objetivo: desabilitar a reconciliação automática de mão de obra e corrigir, por migration rastreável do Entity Framework, os acertos financeiros afetados para que custo de produção, custo total e margem sejam recalculados sem custo de mão de obra.

## Escopo

### Incluído

- Impedir o registro e a execução automática do serviço de reconciliação histórica de custo de mão de obra durante a inicialização da aplicação.
- Recalcular todos os acertos financeiros persistidos, incluindo os que não tenham custo de mão de obra, para que os valores derivados não dependam de cálculos anteriores.
- Remover o valor de mão de obra dos campos financeiros derivados dos acertos afetados, preservando o valor persistido como fonte para reversão da migration.
- Recalcular o custo de produção com os custos materiais rastreados, sem incluir horas × valor-hora no `Up`.
- Recalcular custo total e margem a partir dos valores financeiros corrigidos, preservando receita, desconto e custo de envio.
- Criar e aplicar a correção como migration do Entity Framework gerada e rastreável pelo fluxo `dotnet ef` adotado pelo projeto.
- Manter a correção repetível com segurança e registrar evidência da migration e dos dados corrigidos.

### Excluído

- Alterar horas registradas, tarefas de produção, produtos ou `HourlyRate`.
- Reativar ou substituir o cálculo de custo de mão de obra em novos acertos.
- Excluir acertos financeiros, pedidos, tarefas, movimentações de estoque ou seu histórico de auditoria.
- Reconstituir valores de mão de obra durante o fluxo normal da aplicação.
- Alterar receita, desconto, custo de envio ou a fórmula vigente de margem além do efeito da remoção do custo de mão de obra.

## Requisitos

### Requisitos funcionais

- `FR-001`: A aplicação não deve registrar nem iniciar o serviço de background responsável pela reconciliação histórica de custo de mão de obra.
- `FR-002`: A migration deve recalcular todos os acertos financeiros persistidos usando os dados-base disponíveis, sem depender da execução do serviço em background.
- `FR-003`: No `Up`, cada acerto deve ter o custo de produção recalculado exclusivamente a partir dos custos materiais rastreados, sem incluir o valor de mão de obra; o valor persistido de mão de obra deve ser mantido na coluna própria.
- `FR-004`: A correção deve recalcular custo total e margem a partir da receita, desconto, custo de envio e custo de produção recalculado, preservando pedido de origem, identidade e data do acerto.
- `FR-005`: A migration deve ser criada, identificada e aplicada pelo fluxo `dotnet ef` do projeto, incluindo o artefato de migration necessário para rastreabilidade e execução nos ambientes suportados.
- `FR-006`: Após a correção, uma nova inicialização não deve reaplicar custo de mão de obra nem modificar novamente os acertos corrigidos por causa do serviço desabilitado.
- `FR-007`: Todos os acertos devem ter suas colunas derivadas de custo e margem recalculadas a partir dos demais dados persistidos, inclusive os que não tenham custo de mão de obra.
- `FR-008`: O `Up` da migration deve recalcular os custos sem mão de obra e o `Down` deve recalculá-los com a mão de obra persistida, usando sempre os dados-base e não os valores derivados anteriores.

### Requisitos não funcionais

- `NFR-001`: A correção deve ser transacional por unidade de alteração definida pelo fluxo de persistência, evitando acertos parcialmente recalculados.
- `NFR-002`: A migration deve ser compatível com PostgreSQL e Entity Framework Core e deixar evidência reproduzível do comando `dotnet ef` usado para gerá-la e validá-la.
- `NFR-003`: A operação deve preservar precisão monetária, arredondamento e escala já adotados pelo domínio financeiro.
- `NFR-004`: A correção deve preservar rastreabilidade e não apagar movimentações de estoque nem registrar segredos ou dados sensíveis em logs.
- `NFR-005`: A implementação deve manter a separação entre domínio, aplicação e infraestrutura e atender aos gates de build, testes, segurança e cobertura do projeto.

### Restrições

- `CON-001`: A fonte do custo material deve permanecer nas movimentações de estoque existentes; não deve ser inferida a partir das horas de produção.
- `CON-002`: O valor persistido de mão de obra deve ser preservado durante o `Up` para que o `Down` possa somá-lo novamente; esse valor não deve ser recalculado a partir de horas, produtos ou configurações.
- `CON-003`: A correção de dados não deve depender de requisição de usuário, execução do worker ou alteração manual na tela financeira.
- `CON-004`: O campo `labor_cost_amount` deve permanecer no esquema como valor histórico; o campo `labor_cost_reconciled` deve ser removido por migration, com atualização correspondente do modelo persistido.

## Critérios de aceite

- `AC-001`: Dado o serviço de reconciliação histórica registrado na composição da aplicação, quando a correção for implementada, então a aplicação não o registrará nem o iniciará durante o startup.
- `AC-002`: Dado um acerto com custo material de 300 e custo de mão de obra persistido de 250, quando o `Up` da migration for aplicado, então o custo de produção será recalculado para 300, sem depender do custo de produção anterior, e o valor persistido de mão de obra permanecerá disponível para reversão.
- `AC-003`: Dado um acerto com receita de 1.000, custo de produção anterior de 550, custo de mão de obra de 250, custo de envio de 0 e desconto de 0, quando o `Up` for aplicado, então o custo total será 300 e a margem será 700; quando o `Down` for aplicado, então custo de produção, custo total e margem retornarão respectivamente a 550, 550 e 450.
- `AC-004`: Dado qualquer acerto financeiro, quando a migration for aplicada, então sua identidade, pedido, receita, desconto, custo de envio, data e movimentações de estoque permanecerão inalterados.
- `AC-005`: Dado um acerto sem custo de mão de obra, quando a migration for aplicada, então suas colunas de custo e margem também serão recalculadas a partir da receita, desconto, envio e custos materiais persistidos.
- `AC-006`: Dado um banco com acertos com e sem mão de obra, quando a migration for aplicada por `dotnet ef`, então todos os acertos serão recalculados e a migration ficará registrada no histórico de migrations.
- `AC-007`: Dado o banco já corrigido e a aplicação reiniciada, quando o startup ocorrer, então nenhum worker de mão de obra executará e os valores sem mão de obra permanecerão estáveis.
- `AC-008`: Dado o comando de geração/validação definido para a solução, quando ele for executado, então a migration e o snapshot/metadata correspondentes serão produzidos sem divergência de modelo nem erro de build.
- `AC-009`: Dado um acerto elegível, quando o `Down` for aplicado após o `Up`, então o valor persistido de mão de obra será somado novamente ao custo de produção e ao custo total e subtraído da margem, sem alterar receita, desconto ou custo de envio.
- `AC-010`: Dado um conjunto com acertos com e sem mão de obra, quando o `Up` ou o `Down` for aplicado, então todos os acertos serão recalculados e nenhum resultado dependerá do valor anterior de custo total, custo de produção ou margem.

## Riscos, dependências e perguntas abertas

- Dependência: Estado persistido das colunas de custo de mão de obra e marcador de reconciliação, além das movimentações materiais usadas no cálculo vigente.
- Dependência: Registro do serviço em `AddCraftsmanApplication` e o fluxo de migrations do contexto PostgreSQL.
- Risco: Se os dados materiais não puderem ser associados de forma determinística aos acertos, uma correção parcial pode gerar números incorretos; esses casos devem ser identificados e não corrigidos silenciosamente.

