# AGENTS.md
O arquivo PRODUCT.md contém informações sobre o produto, leia e, em caso de dúvidas, pode me perguntar

## Regras Gerais
- Sempre leia os arquivos em /specs antes de implementar
- Nunca implemente sem critérios de aceitação
- O código deve ser simples e legível
- Evite overengineering

## Fluxo de Trabalho Necessário
1. Leia as especificações no diretório /specs
2. Gere tasks.md se ele não existir
3. Implemente com base nas tarefas
4. Crie testes automatizados, unitarios e de integração
5. Garanta que todos os critérios de aceitação sejam atendidos
6. Sempre após de finalizar uma tarefa, marque ela como concluida

## Testes
- Priorize a cobertura dos critérios de aceitação
- Os testes devem ser claros e diretos

## Restrições
- Não invente requisitos que não estejam descritos
- Não altere o comportamento sem atualizar a especificação

## Observações
- Quando for fazer o build, use a variavel de ambiente DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet