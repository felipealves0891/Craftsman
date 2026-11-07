<!-- spectro-managed: true -->
# Especificação: Tratar concorrência ao editar pedido manual

## Problema e objetivo

- Problema: A edição de um pedido manual pode falhar com `DbUpdateConcurrencyException` quando o registro esperado não é mais compatível com o estado persistido. Atualmente essa falha pode escapar do tratamento específico do POST de edição e resultar em erro não amigável, além de dificultar a investigação posterior.
- Objetivo: Tratar a falha de concorrência durante a edição do pedido com uma resposta segura para o usuário e coletar evidências suficientes, vinculadas por um identificador de correlação, para investigar o cenário sem expor detalhes internos.

## Escopo

### Incluído

- Tratar especificamente a `DbUpdateConcurrencyException` no fluxo de atualização de pedido manual.
- Apresentar ao usuário uma mensagem segura e orientada à ação, mantendo o formulário com os dados enviados e orientando-o a recarregar os dados e tentar novamente, sem stack trace, mensagem técnica do banco ou detalhes internos da aplicação.
- Registrar a ocorrência, o pedido afetado, a etapa/operação de persistência, o resultado da tentativa e os dados de diagnóstico permitidos, todos associados ao mesmo identificador de correlação da requisição.
- Ampliar a rastreabilidade das manipulações de persistência do fluxo de edição manual, incluindo pedido, itens, cliente, produção e acerto financeiro relacionados à tentativa, para permitir reconstruir a sequência percorrida pela requisição.
- Preservar os dados e os efeitos já persistidos quando a gravação da edição falhar por concorrência.

### Excluído

- Definir ou implementar uma estratégia automática de retry, merge, sobrescrita ou resolução da versão concorrente.
- Alterar as regras de negócio que determinam quando um pedido pode ser editado.
- Alterar o modelo de concorrência, adicionar colunas de versão ou mudar o esquema do banco sem decisão específica posterior.
- Expor ao usuário ou registrar credenciais, tokens, segredos ou dados sensíveis não necessários para a investigação.
- Alterar o tratamento de outros tipos de exceção ou de outros fluxos de edição que não sejam necessários para atender este cenário.

## Requisitos

### Requisitos funcionais

- `FR-001`: Quando a atualização de um pedido manual resultar em `DbUpdateConcurrencyException`, o fluxo deve capturar essa condição especificamente e retornar o usuário ao fluxo de edição com uma mensagem segura, sem propagar a exceção técnica como erro não tratado.
- `FR-002`: A mensagem apresentada para uma falha de concorrência deve informar que o pedido foi alterado ou removido durante a edição e orientar o usuário sobre a próxima ação, sem incluir stack trace, SQL, nomes de tipos internos ou detalhes do banco.
- `FR-003`: Cada tentativa de atualização de pedido manual deve manter um identificador de correlação único e consistente entre a requisição, os registros de diagnóstico da aplicação e as manipulações de persistência observadas durante a tentativa.
- `FR-004`: Em uma falha de concorrência, os registros de diagnóstico devem identificar pelo menos a operação de edição, o identificador do pedido, a etapa em que a falha ocorreu, o tipo da exceção, o resultado da operação e o identificador de correlação.
- `FR-005`: O fluxo deve registrar metadados mínimos sobre as etapas percorridas, operações, estados, resultado e duração das leituras, alterações e tentativas de gravação do pedido e de seus efeitos relacionados, respeitando as restrições de segurança e privacidade existentes.
- `FR-006`: Quando a atualização falhar por concorrência, o fluxo não deve indicar sucesso nem executar efeitos posteriores que dependam de uma gravação do pedido que não foi confirmada.

### Requisitos não funcionais

- `NFR-001`: Os diagnósticos devem ser pesquisáveis pelo identificador de correlação e pelo identificador do pedido, com a mesma correlação em todos os registros da tentativa.
- `NFR-002`: O tratamento deve preservar a mensagem segura mesmo quando a exceção contiver detalhes técnicos, SQL, stack trace ou informações do provedor PostgreSQL.
- `NFR-003`: Os dados registrados devem observar a política do projeto: não incluir credenciais, tokens, segredos ou dados sensíveis desnecessários.

### Restrições

- `CON-001`: A mudança deve respeitar a separação existente entre App, Domains e Infra e os padrões de persistência com Entity Framework Core e PostgreSQL.
- `CON-002`: O identificador de correlação existente na requisição/auditoria deve ser reutilizado quando disponível; a especificação não exige um novo mecanismo de correlação.
- `CON-003`: A especificação define o comportamento observável e a evidência necessária, mas não determina em qual camada o `try/catch`, o logger ou a coleta de diagnóstico devem ser implementados.
- `CON-004`: A falha de concorrência não deve ser convertida em sucesso, nem deve autorizar sobrescrever silenciosamente a alteração concorrente.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-006`): Dado um pedido aberto para edição, quando a gravação resultar em `DbUpdateConcurrencyException`, então a exceção é tratada pelo fluxo de edição, a resposta não é um erro técnico não tratado, não há redirecionamento de sucesso e não são executados efeitos posteriores condicionados à gravação confirmada.
- `AC-002` (`FR-002`, `NFR-002`): Dada uma falha de concorrência contendo stack trace e mensagem do PostgreSQL, quando a resposta for apresentada, então o formulário permanece com os dados enviados e o usuário vê somente uma mensagem segura orientando-o a recarregar os dados e tentar novamente, sem stack trace, SQL, nomes de exceção ou detalhes internos.
- `AC-003` (`FR-003`, `FR-004`, `NFR-001`): Dada uma tentativa de editar o pedido `orderId`, quando ocorrer a falha de concorrência, então os diagnósticos da tentativa podem ser localizados pelo mesmo identificador de correlação e registram a operação de edição, `orderId`, etapa da falha, tipo da exceção e resultado.
- `AC-004` (`FR-005`, `NFR-003`): Dada uma tentativa de edição com leituras, alterações e gravações relacionadas ao pedido, quando os diagnósticos forem consultados, então as etapas e operações relevantes estão associadas à correlação da tentativa, com estados, resultado e duração, sem corpo completo, contatos do cliente, valores antes/depois, credenciais, tokens, segredos ou dados sensíveis desnecessários.
- `AC-005` (`FR-006`, `CON-004`): Dada uma falha de concorrência, quando o estado do pedido e seus efeitos relacionados forem consultados após a resposta, então nenhuma alteração parcial posterior à falha é apresentada como confirmada e a alteração concorrente não foi sobrescrita silenciosamente.
- `AC-006` (`FR-001`, `FR-002`): Dada uma edição que falha por validação ou regra de negócio sem `DbUpdateConcurrencyException`, quando o fluxo retornar ao formulário, então o tratamento existente para essa causa permanece aplicável e não é apresentado indevidamente como conflito de concorrência.

## Riscos, dependências e perguntas abertas

- Dependência: O fluxo atual de edição de pedido manual passa por `ManualOrdersController.Edit`, `ManualOrderService.UpdateAsync`, `IUnitOfWork.SaveChangesAsync` e `AppDbContext.SaveChangesAsync`.
- Dependência: O contexto já possui auditoria de entidades com `CorrelationId`; a implementação deve usar essa correlação e complementar os registros apenas com metadados do fluxo necessários para cobrir o POST de edição.
- Risco: Instrumentação insuficiente pode não revelar em qual etapa da requisição ocorreu a divergência; os registros devem manter a sequência das etapas e operações sem ampliar desnecessariamente os dados coletados.
- Risco: O fluxo de atualização possui mais de uma gravação e operações posteriores de produção/acerto; um tratamento incompleto pode registrar sucesso ou deixar evidência ambígua após uma falha intermediária.
- Decisão registrada: Após o conflito, o formulário permanece com os dados enviados e orienta o usuário a recarregar os dados e tentar novamente.
- Decisão registrada: A instrumentação fica limitada ao fluxo de edição manual do pedido e às entidades/efeitos relacionados à tentativa.
- Decisão registrada: A coleta prioriza o fluxo percorrido pela requisição, usando metadados mínimos e seguros; não inclui corpo completo, contatos do cliente ou valores antes/depois.
- Decisão registrada: Não foi especificada resolução automática do conflito; a especificação limita o comportamento à resposta segura e à coleta de evidências.

