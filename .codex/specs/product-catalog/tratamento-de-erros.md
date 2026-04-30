# Plano PC-008 - Tratamento de Erros no Leitor de Código de Barras

## Summary

Completar o único item pendente de PC-008: informar o usuário com detalhes úteis quando a leitura por câmera falhar. A implementação deve manter
o salvamento manual do código de barras funcionando e não alterar o fluxo já criado para persistir Product.Barcode.

## Key Changes

- Melhorar product-barcode-scanner.js para classificar falhas comuns:
    - biblioteca ZXing indisponível/CDN não carregou;
    - navegador sem mediaDevices/getUserMedia;
    - página sem contexto seguro para câmera;
    - permissão negada;
    - câmera não encontrada;
    - câmera ocupada/indisponível;
    - erro desconhecido.
- Exibir a mensagem no elemento data-barcode-status, com texto operacional e detalhe técnico curto, por exemplo: Permissao da camera negada.
Detalhe: NotAllowedError.
    - sucesso após leitura;
- Garantir que qualquer erro pare a câmera, esconda o preview e preserve o campo Barcode editável manualmente.
- Não mexer no domínio, repositório ou migration para esta tarefa, exceto se o implementer precisar corrigir conflitos locais já existentes no
workspace.
- Ao final, marcar em .codex/tasks/product-catalog/tasks.md:
    - [x] Adicionar tratamento de erro, e informe o usuario, com detalhes do erro

## Test Plan

- Atualizar ProductBarcodeScannerViewTests para validar que o script contém mensagens específicas para:
    - permissão negada;
    - câmera indisponível/não encontrada;
    - biblioteca indisponível;
    - fallback manual.
- Validar que o script continua chamando controls.stop() em erro, cancelamento, submit e pagehide.
- Rodar:
    - DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet dotnet build-server shutdown
    - DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet dotnet build Craftsman.slnx --no-restore -v:minimal /
    p:UseSharedCompilation=false
    - DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet dotnet test Craftsman.slnx --no-build -v:minimal

## Assumptions

- O scanner continuará usando ZXing via CDN fixo em Products/Edit.cshtml.
- O fallback manual é obrigatório e deve permanecer disponível mesmo quando a câmera falhar.
- “Detalhes do erro” significa expor o error.name e, quando útil, uma mensagem curta, sem stack trace.