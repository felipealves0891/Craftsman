<!-- spectro-managed: true -->
# Especificação: Exibir mão de obra na tabela sem incorporá-la aos custos

## Problema e objetivo

- Problema: A alteração anterior passou a usar o valor-hora da produção para aumentar os custos financeiros e refletir esse valor nos cards. Esse comportamento não deve permanecer.
- Objetivo: Reverter a inclusão do valor-hora nos cálculos e nos cards, mantendo somente uma coluna de mão de obra na tabela financeira/listagem aplicável.

## Escopo

### Incluído

- Remover o valor de mão de obra baseado em valor-hora da composição dos custos financeiros.
- Preservar os custos, o custo total e a margem conforme a regra vigente anterior à inclusão do valor-hora.
- Remover a mão de obra dos cards e demais indicadores resumidos que exibem custos ou margem.
- Adicionar uma coluna de mão de obra na tabela aplicável, sem alterar as demais colunas existentes.
- Exibir na coluna somente o valor monetário de mão de obra correspondente ao registro da linha.

### Excluído

- Alterar ou remover o cadastro de horas de produção ou de valor-hora dos produtos.
- Criar novo cálculo de valor-hora, apontamento manual, controle de ponto ou distribuição de horas.
- Alterar receita, descontos, custo de materiais, custo de envio ou outras regras financeiras não relacionadas à reversão do custo-hora.
- Exibir a mão de obra nos cards, totais resumidos, margem ou custo total.
- Alterar telas ou relatórios que não sejam a tabela explicitamente incluída nesta feature.

## Requisitos

### Requisitos funcionais

- `FR-001`: A aplicação não deve somar o custo de mão de obra baseado em valor-hora ao custo de produção, ao custo total ou à margem dos registros financeiros.
- `FR-002`: Os cálculos financeiros devem preservar os valores que seriam obtidos sem a inclusão do custo de mão de obra baseado em valor-hora, mantendo os demais componentes e regras vigentes.
- `FR-003`: Os cards e indicadores resumidos da tela financeira não devem contabilizar nem exibir a mão de obra como componente de custo ou margem.
- `FR-004`: A tabela financeira/listagem aplicável deve apresentar uma coluna identificada como mão de obra, contendo somente o valor monetário correspondente à linha, sem alterar a composição dos cards ou dos cálculos financeiros.

### Requisitos não funcionais

- `NFR-001`: A coluna de mão de obra deve seguir o formato monetário e o padrão visual das demais colunas financeiras da tabela.
- `NFR-002`: A mudança deve preservar a separação entre apresentação e regras financeiras existentes, sem introduzir nova regra de cálculo de custos.

### Restrições

- `CON-001`: O valor-hora do produto não pode ser usado para aumentar custos, custo total ou margem no escopo desta feature.
- `CON-002`: A coluna de mão de obra é exclusivamente informativa na tabela; sua presença não altera cards, totais, margem ou demais cálculos.
- `CON-003`: Custos de materiais, receita, descontos, custo de envio e demais componentes financeiros permanecem conforme a regra vigente anterior à inclusão do valor-hora.
- `CON-004`: A coluna deve exibir somente o valor monetário de mão de obra; não deve exibir horas ou valor-hora como informação separada.

## Critérios de aceite

- `AC-001`: Dado um registro com horas de produção e valor-hora cadastrado, quando os custos financeiros forem calculados ou consultados, então o valor-hora não será somado ao custo de produção, ao custo total nem subtraído da margem.
- `AC-002`: Dado um registro com custo de materiais, receita, desconto e custo de envio, quando os cards forem exibidos, então eles apresentarão os valores calculados sem contabilizar a mão de obra baseada em valor-hora.
- `AC-003`: Dado um registro elegível para a listagem, quando a tabela for exibida, então haverá uma coluna de mão de obra identificada e posicionada conforme o padrão da tabela.
- `AC-004`: Dado um registro com valor de mão de obra disponível para exibição, quando a linha for renderizada, então a coluna exibirá esse valor no formato monetário adotado pela tabela.
- `AC-005`: Dado um registro sem valor de mão de obra disponível, quando a linha for renderizada, então a coluna exibirá o estado vazio adotado pela tabela, sem alterar os demais valores da linha.
- `AC-006`: Dado o mesmo conjunto de dados antes e depois da mudança, quando os cards, o custo total e a margem forem comparados, então a única informação adicional será a coluna de mão de obra na tabela, sem alteração nos cálculos financeiros vigentes.

## Riscos, dependências e perguntas abertas

- Dependência: A tabela financeira/listagem e os cards atualmente exibidos pela aplicação.
- Dependência: A fonte de dados atualmente disponível para o valor monetário de mão de obra que será apresentado na nova coluna.
- Risco: Remover o custo-hora dos valores derivados pode exigir preservar ou reverter dados persistidos anteriormente para evitar que o histórico continue inflado.
- Decisão registrada: A coluna exibirá somente o valor monetário de mão de obra. Horas registradas e valor-hora não serão exibidos como conteúdo separado.
