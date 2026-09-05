# GestorPDV.Relatorios

Geração de relatórios em PDF a partir de dados lidos diretamente do
PostgreSQL (via `IRelatorioRepository`, implementado em
`GestorPDV.Data.Postgres`).

Usa **FastReport.OpenSource** (MIT) + **FastReport.OpenSource.Export.PdfSimple**
como motor de relatório — não a edição comercial licenciada do FastReport.
`RelatorioTabularBuilder` monta o relatório programaticamente via API do
FastReport (`Report`, `ReportPage`, bandas, `TextObject`), em vez de um
arquivo `.frx` feito no FastReport Designer, porque este projeto não tem
acesso a uma máquina Windows/Visual Studio para usar o designer visual — ver
`docs/ROADMAP.md` (Fase 8, suposição 18).

`IRelatorioService` (implementado por `RelatorioService`) devolve os bytes
do PDF gerado, para ficar testável sem tocar em disco; quem chama decide
onde salvar/abrir o arquivo (a tela "Relatórios" do `GestorPDV.Wpf` salva
num arquivo temporário e abre no visualizador padrão do sistema).

Relatórios implementados na **Fase 8**: vendas por período, estoque atual e
contas a receber em aberto (com juros/multa recalculados via
`IFinanceiroService.CalcularEncargos`). O envio do PDF direto para a
impressora (Fase 9) e o cupom não fiscal da venda vivem em
`GestorPDV.Wpf/Impressao` — ver `docs/ROADMAP.md`.
