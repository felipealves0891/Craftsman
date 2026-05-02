# Melhorias na Usabilidade da aplicação

Estamos com alguns problemas de usabilidades que precisamos corrigir

## Problemas

### Valores X Placeholders

Estamos usando inputs type="number" e em alguns casos isso é um problema, porque não identificam corretamente as casas dessimais.

### Valores Monetarios

Nos campos de valor monetario, adicionar uma formação de moeda em Real (R$), usando ponto (.) para milhar e virgula (,) para decimal

### Navegação em Mobile

Sempre quando recaremos a pagina ou navegamos para outra pagina na visão Mobile, o sub-menu fica aberto, em tem que fechar após a navegação.
## Criterios de Aceitacao

- Campos decimais editaveis nao devem renderizar como `type="number"`.
- Campos monetarios editaveis devem exibir mascara em Real no formato `R$ 1.234,56`.
- Campos monetarios devem ser enviados para o servidor em formato aceito pelo model binding da aplicacao.
- Campos decimais nao monetarios devem aceitar virgula como separador decimal.
- Em telas mobile, os submenus da sidebar devem iniciar fechados apos reload ou navegacao.
- Em desktop, a sidebar deve continuar destacando a secao ativa como antes.
