using Orders.API.Models;
using Orders.API.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Orders.API.Services
{
    public interface IOrderPdfGenerator
    {
        byte[] Generate(Order order);
    }

    // Comprobante de compra en PDF, con estructura de ticket. QuestPDF corre bajo la licencia
    // Community (gratuita para proyectos/empresas pequeñas) — se declara en Program.cs con
    // QuestPDF.Settings.License.
    public class OrderPdfGenerator : IOrderPdfGenerator
    {
        private static readonly string BrandColor = Colors.Green.Darken2;

        public byte[] Generate(Order order)
        {
            var orderNumber = order.Id.ToOrderNumber();
            var (statusLabel, statusColor) = StatusStyle(order.Status);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Ancho de ticket angosto en vez de una carta A4 completa, con alto continuo
                    // (se ajusta al contenido en vez de dejar un espacio en blanco fijo abajo,
                    // como una cinta de recibo real). Unit calificado explícito: choca con
                    // MediatR.Unit (global using en GlobalUsing.cs).
                    page.ContinuousSize(9, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.MarginVertical(0.9f, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.MarginHorizontal(0.7f, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(column =>
                    {
                        column.Spacing(4);

                        // ── Encabezado de marca ──
                        column.Item().AlignCenter().Text("eShop").FontSize(20).Bold().FontColor(BrandColor);
                        column.Item().AlignCenter().Text("COMPROBANTE DE COMPRA")
                            .FontSize(8).LetterSpacing(0.08f).FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // ── Datos de la orden ──
                        column.Item().AlignCenter().Text(orderNumber).FontSize(15).Bold();
                        column.Item().AlignCenter().Text($"ID completo: {order.Id}")
                            .FontSize(6.5f).FontColor(Colors.Grey.Medium);

                        column.Item().PaddingTop(6).Column(info =>
                        {
                            info.Spacing(2);
                            InfoRow(info, "Cliente", order.CustomerId);
                            InfoRow(info, "Fecha", order.CreatedAt.ToString("dd/MM/yyyy HH:mm") + " UTC");
                            info.Item().Row(row =>
                            {
                                row.ConstantItem(55).Text("Estado").FontColor(Colors.Grey.Darken1);
                                row.RelativeItem().AlignRight().Text(statusLabel).Bold().FontColor(statusColor);
                            });
                        });

                        column.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // ── Detalle de productos ──
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Producto");
                                header.Cell().Element(HeaderCell).Text("Ref");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Importe");

                                static IContainer HeaderCell(IContainer c) =>
                                    c.DefaultTextStyle(x => x.SemiBold().FontSize(7.5f))
                                     .PaddingBottom(3)
                                     .BorderBottom(1)
                                     .BorderColor(Colors.Grey.Darken1);
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().Element(BodyCell).Text(item.ProductName).FontSize(8);
                                table.Cell().Element(BodyCell).Text(item.ProductId.ToShortCode())
                                    .FontSize(7).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString()).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text($"${item.LineTotal:0.00}").FontSize(8);

                                // Segunda línea con el precio unitario, para no perderlo sin agregar otra columna angosta.
                                table.Cell().ColumnSpan(4).Element(c => c.PaddingBottom(4))
                                    .Text($"  {item.Quantity} x ${item.UnitPrice:0.00} c/u")
                                    .FontSize(6.5f).FontColor(Colors.Grey.Medium);

                                static IContainer BodyCell(IContainer c) =>
                                    c.PaddingTop(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        column.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // ── Totales ──
                        column.Item().Column(totals =>
                        {
                            totals.Spacing(2);
                            TotalRow(totals, "Subtotal", order.Subtotal, bold: false);
                            TotalRow(totals, "Impuestos", order.Tax, bold: false);
                        });

                        column.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Black)
                            .PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text($"${order.Total:0.00}").Bold().FontSize(13).FontColor(BrandColor);
                        });

                        column.Item().PaddingTop(14).AlignCenter().Text("¡Gracias por tu compra!")
                            .FontSize(9).Italic();
                        column.Item().AlignCenter().Text("Generado automáticamente por Orders.API — eShop")
                            .FontSize(6.5f).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void InfoRow(ColumnDescriptor column, string label, string value)
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(55).Text(label).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(value);
            });
        }

        private static void TotalRow(ColumnDescriptor column, string label, decimal amount, bool bold)
        {
            column.Item().Row(row =>
            {
                var style = TextStyle.Default.FontSize(9);
                if (bold) style = style.Bold();
                row.RelativeItem().Text(label).Style(style);
                row.RelativeItem().AlignRight().Text($"${amount:0.00}").Style(style);
            });
        }

        private static (string Label, string Color) StatusStyle(OrderStatus status) => status switch
        {
            OrderStatus.Pending => ("PENDIENTE", Colors.Orange.Darken2),
            OrderStatus.Confirmed => ("CONFIRMADA", Colors.Green.Darken2),
            OrderStatus.Cancelled => ("CANCELADA", Colors.Red.Darken2),
            _ => (status.ToString().ToUpperInvariant(), Colors.Grey.Darken2)
        };
    }
}
