using Orders.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Orders.API.Services
{
    public interface IOrderPdfGenerator
    {
        byte[] Generate(Order order);
    }

    // Comprobante de compra en PDF. QuestPDF corre bajo la licencia Community (gratuita para
    // proyectos/empresas pequeñas) — se declara en Program.cs con QuestPDF.Settings.License.
    public class OrderPdfGenerator : IOrderPdfGenerator
    {
        public byte[] Generate(Order order)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    // Unit está calificado explícitamente: choca con MediatR.Unit (global using en GlobalUsing.cs).
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("eShop — Comprobante de compra").FontSize(18).Bold();
                        column.Item().PaddingTop(2).Text($"Orden #{order.Id}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(15).Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Cliente: ").SemiBold();
                                t.Span(order.CustomerId);
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Fecha: ").SemiBold();
                                t.Span(order.CreatedAt.ToString("dd/MM/yyyy HH:mm") + " UTC");
                            });
                        });

                        column.Item().Text(t =>
                        {
                            t.Span("Estado: ").SemiBold();
                            t.Span(order.Status.ToString());
                        });

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Producto");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Precio unit.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");

                                static IContainer HeaderCell(IContainer c) =>
                                    c.DefaultTextStyle(x => x.SemiBold())
                                     .PaddingBottom(4)
                                     .BorderBottom(1)
                                     .BorderColor(Colors.Grey.Lighten1);
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().Element(BodyCell).Text(item.ProductName);
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text($"${item.UnitPrice:0.00}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"${item.LineTotal:0.00}");

                                static IContainer BodyCell(IContainer c) =>
                                    c.PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        column.Item().PaddingTop(10).AlignRight().Column(totals =>
                        {
                            totals.Item().Text($"Subtotal: ${order.Subtotal:0.00}");
                            totals.Item().Text($"Impuestos: ${order.Tax:0.00}");
                            totals.Item().PaddingTop(4).Text($"Total: ${order.Total:0.00}").FontSize(13).Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generado automáticamente por Orders.API — eShop")
                         .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
