# Página de Documentação Inicial

## Summary

Criar a primeira versão de documentação do Craftsman como uma página central autenticada, focada no fluxo operacional ponta a ponta. A
documentação não será por tela isolada nesta fase; ela explicará como os módulos se conectam: pedidos, catálogo, estoque, produção, envios e
financeiro.

## Key Changes

- Adicionar uma rota GET /Docs via DocsController, protegida por ApplicationPolicies.Read.
- Criar a view Views/Docs/Index.cshtml com:
    - visão “Comece por aqui”;
    - checklist inicial de operação;
    - fluxo principal: importar/criar pedido -> mapear produto -> configurar ficha técnica -> garantir estoque -> enviar para produção ->
    acompanhar envio -> revisar financeiro;
- Não adicionar ícone de ajuda em cada página nesta primeira versão; isso fica como evolução futura apontando para âncoras específicas da página

## Acceptance Criteria

- A sidebar deve exibir “Documentação” para usuários autenticados com permissão de leitura.
- A página /Docs deve abrir no layout autenticado padrão.
- A documentação deve explicar como um fluxo se liga ao outro, não apenas descrever páginas.
- A página deve conter links para: pedidos/importação, pedido manual, produtos, mapeamentos, matérias-primas/estoque, produção, envios e
financeiro.