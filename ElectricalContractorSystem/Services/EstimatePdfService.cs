using System;
using System.IO;
using System.Linq;
using ElectricalContractorSystem.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System.Diagnostics;

namespace ElectricalContractorSystem.Services
{
    public class EstimatePdfService
    {
        private readonly EstimateService _estimateService;
        private readonly DatabaseService _databaseService;
        
        // Company information - customize these
        private const string CompanyName = "Erik Rusek Electric";
        private const string CompanyAddress = "PO Box 851";
        private const string CompanyCity = "Manasquan, NJ 08736";
        private const string CompanyPhone = "(732) 555-0100";
        private const string CompanyEmail = "erik@erikrusekelectric.com";
        private const string CompanyLicense = "License #13VH07574900";

        // PDF styling
        private readonly BaseColor HeaderColor = new BaseColor(0, 64, 128); // Dark blue
        private readonly BaseColor AccentColor = new BaseColor(0, 102, 204); // Light blue
        private readonly Font TitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24, BaseColor.BLACK);
        private readonly Font HeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
        private readonly Font SubheaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
        private readonly Font NormalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
        private readonly Font SmallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
        private readonly Font BoldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);

        public EstimatePdfService(EstimateService estimateService, DatabaseService databaseService)
        {
            _estimateService = estimateService;
            _databaseService = databaseService;
        }

        public void GenerateEstimatePdf(int estimateId, string outputPath, bool showPrices = true)
        {
            var estimate = _estimateService.GetEstimateById(estimateId);
            if (estimate == null)
            {
                throw new InvalidOperationException($"Estimate {estimateId} not found");
            }

            // Get stage summaries using the database connection
            System.Collections.Generic.List<EstimateStageSummary> stageSummaries;
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                stageSummaries = _estimateService.GetEstimateStageSummaries(connection, estimateId);
            }

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var document = new Document(PageSize.LETTER);
                var writer = PdfWriter.GetInstance(document, fs);
                
                // Add page events for header/footer
                writer.PageEvent = new EstimatePdfPageEventHelper(CompanyName, estimate.EstimateNumber);
                
                document.Open();

                // Add company header
                AddCompanyHeader(document);
                
                // Add estimate header
                AddEstimateHeader(document, estimate);
                
                // Add customer information
                AddCustomerSection(document, estimate);
                
                // Add job details
                AddJobDetailsSection(document, estimate);
                
                // Add room-by-room breakdown
                AddRoomBreakdown(document, estimate, showPrices);
                
                // Add stage summary
                if (showPrices)
                {
                    AddStageSummary(document, stageSummaries);
                }
                
                // Add totals
                if (showPrices)
                {
                    AddTotalsSection(document, estimate);
                }
                
                // Add terms and conditions
                AddTermsAndConditions(document, estimate);
                
                // Add signature block
                AddSignatureBlock(document);

                document.Close();
            }
        }

        private void AddCompanyHeader(Document document)
        {
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1, 1 });
            table.SpacingAfter = 20f;

            // Company info cell
            var companyCell = new PdfPCell();
            companyCell.Border = Rectangle.NO_BORDER;
            companyCell.AddElement(new Paragraph(CompanyName, TitleFont));
            companyCell.AddElement(new Paragraph(CompanyAddress, NormalFont));
            companyCell.AddElement(new Paragraph(CompanyCity, NormalFont));
            companyCell.AddElement(new Paragraph(CompanyPhone, NormalFont));
            companyCell.AddElement(new Paragraph(CompanyEmail, NormalFont));
            companyCell.AddElement(new Paragraph(CompanyLicense, SmallFont));
            table.AddCell(companyCell);

            // Logo placeholder (you can add actual logo here)
            var logoCell = new PdfPCell(new Phrase(""));
            logoCell.Border = Rectangle.NO_BORDER;
            logoCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.AddCell(logoCell);

            document.Add(table);
            
            // Add a line separator using iTextSharp.text.pdf.draw.LineSeparator
            var line = new iTextSharp.text.pdf.draw.LineSeparator(2f, 100f, HeaderColor, Element.ALIGN_CENTER, -1);
            document.Add(new Chunk(line));
            document.Add(new Paragraph("\n"));
        }

        private void AddEstimateHeader(Document document, Estimate estimate)
        {
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SpacingAfter = 20f;
            table.SetWidths(new float[] { 1, 1 });

            // Estimate title
            var titleCell = new PdfPCell(new Phrase("ELECTRICAL ESTIMATE", HeaderFont));
            titleCell.Border = Rectangle.NO_BORDER;
            titleCell.Colspan = 2;
            titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
            titleCell.PaddingBottom = 10f;
            table.AddCell(titleCell);

            // Estimate number and date
            var leftCell = new PdfPCell();
            leftCell.Border = Rectangle.NO_BORDER;
            leftCell.AddElement(new Paragraph($"Estimate #: {estimate.EstimateNumber}", BoldFont));
            leftCell.AddElement(new Paragraph($"Date: {estimate.CreatedDate:MM/dd/yyyy}", NormalFont));
            if (estimate.ExpirationDate.HasValue)
            {
                leftCell.AddElement(new Paragraph($"Valid Until: {estimate.ExpirationDate.Value:MM/dd/yyyy}", NormalFont));
            }
            table.AddCell(leftCell);

            // Status
            var rightCell = new PdfPCell();
            rightCell.Border = Rectangle.NO_BORDER;
            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            rightCell.AddElement(new Paragraph($"Status: {estimate.Status}", BoldFont));
            table.AddCell(rightCell);

            document.Add(table);
        }

        private void AddCustomerSection(Document document, Estimate estimate)
        {
            document.Add(new Paragraph("CUSTOMER INFORMATION", SubheaderFont));
            
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SpacingAfter = 20f;
            table.SpacingBefore = 10f;
            table.SetWidths(new float[] { 1, 1 });

            // Customer details
            var customerCell = new PdfPCell();
            customerCell.Border = Rectangle.BOX;
            customerCell.Padding = 10f;
            customerCell.BackgroundColor = new BaseColor(245, 245, 245);
            customerCell.AddElement(new Paragraph(estimate.Customer.Name, BoldFont));
            if (!string.IsNullOrEmpty(estimate.Customer.Address))
                customerCell.AddElement(new Paragraph(estimate.Customer.Address, NormalFont));
            if (!string.IsNullOrEmpty(estimate.Customer.City))
                customerCell.AddElement(new Paragraph($"{estimate.Customer.City}, {estimate.Customer.State} {estimate.Customer.Zip}", NormalFont));
            if (!string.IsNullOrEmpty(estimate.Customer.Phone))
                customerCell.AddElement(new Paragraph($"Phone: {estimate.Customer.Phone}", NormalFont));
            if (!string.IsNullOrEmpty(estimate.Customer.Email))
                customerCell.AddElement(new Paragraph($"Email: {estimate.Customer.Email}", NormalFont));
            table.AddCell(customerCell);

            // Placeholder for additional info
            var placeholderCell = new PdfPCell(new Phrase(""));
            placeholderCell.Border = Rectangle.NO_BORDER;
            table.AddCell(placeholderCell);

            document.Add(table);
        }

        private void AddJobDetailsSection(Document document, Estimate estimate)
        {
            document.Add(new Paragraph("JOB DETAILS", SubheaderFont));
            
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SpacingAfter = 20f;
            table.SpacingBefore = 10f;
            table.SetWidths(new float[] { 1, 2 });

            // Job name
            AddDetailRow(table, "Job Name:", estimate.JobName);
            
            // Job address
            if (!string.IsNullOrEmpty(estimate.JobAddress))
            {
                AddDetailRow(table, "Job Location:", estimate.JobAddress);
                if (!string.IsNullOrEmpty(estimate.JobCity))
                {
                    AddDetailRow(table, "", $"{estimate.JobCity}, {estimate.JobState} {estimate.JobZip}");
                }
            }
            
            // Square footage and floors
            if (estimate.SquareFootage.HasValue)
            {
                AddDetailRow(table, "Square Footage:", estimate.SquareFootage.Value.ToString("N0"));
            }
            if (estimate.NumFloors.HasValue)
            {
                AddDetailRow(table, "Number of Floors:", estimate.NumFloors.Value.ToString());
            }

            document.Add(table);
        }

        private void AddDetailRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label, BoldFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.PaddingBottom = 5f;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value, NormalFont));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.PaddingBottom = 5f;
            table.AddCell(valueCell);
        }

        private void AddRoomBreakdown(Document document, Estimate estimate, bool showPrices)
        {
            document.Add(new Paragraph("SCOPE OF WORK", SubheaderFont));
            document.Add(new Paragraph("\n"));

            foreach (var room in estimate.Rooms.OrderBy(r => r.RoomOrder))
            {
                // Room header
                var roomTable = new PdfPTable(1);
                roomTable.WidthPercentage = 100;
                roomTable.SpacingBefore = 10f;
                
                var roomHeaderCell = new PdfPCell(new Phrase(room.RoomName.ToUpper(), BoldFont));
                roomHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                roomHeaderCell.Padding = 5f;
                roomTable.AddCell(roomHeaderCell);
                document.Add(roomTable);

                // Items table
                var itemsTable = showPrices ? new PdfPTable(4) : new PdfPTable(2);
                itemsTable.WidthPercentage = 100;
                itemsTable.SpacingAfter = 10f;
                
                if (showPrices)
                {
                    itemsTable.SetWidths(new float[] { 1, 4, 1, 1 });
                    // Headers
                    AddItemHeader(itemsTable, "Qty");
                    AddItemHeader(itemsTable, "Description");
                    AddItemHeader(itemsTable, "Unit Price");
                    AddItemHeader(itemsTable, "Total");
                }
                else
                {
                    itemsTable.SetWidths(new float[] { 1, 6 });
                    // Headers
                    AddItemHeader(itemsTable, "Qty");
                    AddItemHeader(itemsTable, "Description");
                }

                // Items
                foreach (var item in room.Items.OrderBy(i => i.LineOrder))
                {
                    AddItemCell(itemsTable, item.Quantity.ToString(), Element.ALIGN_CENTER);
                    AddItemCell(itemsTable, item.ItemDescription, Element.ALIGN_LEFT);
                    
                    if (showPrices)
                    {
                        AddItemCell(itemsTable, item.UnitPrice.ToString("C"), Element.ALIGN_RIGHT);
                        AddItemCell(itemsTable, item.TotalPrice.ToString("C"), Element.ALIGN_RIGHT);
                    }
                }

                if (showPrices)
                {
                    // Room subtotal
                    var roomTotal = room.Items.Sum(i => i.TotalPrice);
                    var subtotalCell = new PdfPCell(new Phrase($"Room Subtotal: {roomTotal:C}", BoldFont));
                    subtotalCell.Colspan = 4;
                    subtotalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    subtotalCell.Border = Rectangle.TOP_BORDER;
                    subtotalCell.PaddingTop = 5f;
                    itemsTable.AddCell(subtotalCell);
                }

                document.Add(itemsTable);
            }
        }

        private void AddItemHeader(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, BoldFont));
            cell.BackgroundColor = new BaseColor(240, 240, 240);
            cell.Padding = 5f;
            table.AddCell(cell);
        }

        private void AddItemCell(PdfPTable table, string text, int alignment)
        {
            var cell = new PdfPCell(new Phrase(text, NormalFont));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 3f;
            table.AddCell(cell);
        }

        private void AddStageSummary(Document document, System.Collections.Generic.List<EstimateStageSummary> stageSummaries)
        {
            if (stageSummaries.Count == 0) return;

            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("LABOR SUMMARY BY STAGE", SubheaderFont));
            
            var table = new PdfPTable(3);
            table.WidthPercentage = 100;
            table.SpacingBefore = 10f;
            table.SpacingAfter = 20f;
            table.SetWidths(new float[] { 3, 2, 2 });

            // Headers
            AddItemHeader(table, "Stage");
            AddItemHeader(table, "Labor Hours");
            AddItemHeader(table, "Material Cost");

            // Stage rows
            foreach (var stage in stageSummaries.OrderBy(s => s.StageOrder))
            {
                AddItemCell(table, stage.Stage, Element.ALIGN_LEFT);
                AddItemCell(table, stage.LaborHours.ToString("N1"), Element.ALIGN_CENTER);
                AddItemCell(table, stage.MaterialCost.ToString("C"), Element.ALIGN_RIGHT);
            }

            document.Add(table);
        }

        private void AddTotalsSection(Document document, Estimate estimate)
        {
            var table = new PdfPTable(2);
            table.WidthPercentage = 50;
            table.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.SpacingBefore = 20f;
            table.SetWidths(new float[] { 2, 1 });

            // Material subtotal
            decimal materialSubtotal = estimate.TotalMaterialCost;
            AddTotalRow(table, "Material Cost:", materialSubtotal.ToString("C"));

            // Material markup
            decimal materialMarkup = materialSubtotal * (estimate.MaterialMarkup / 100);
            AddTotalRow(table, $"Material Markup ({estimate.MaterialMarkup}%):", materialMarkup.ToString("C"));

            // Labor
            decimal laborHours = estimate.TotalLaborMinutes / 60m;
            decimal laborCost = laborHours * estimate.LaborRate;
            AddTotalRow(table, $"Labor ({laborHours:N1} hrs @ {estimate.LaborRate:C}):", laborCost.ToString("C"));

            // Subtotal
            decimal subtotal = materialSubtotal + materialMarkup + laborCost;
            AddTotalRow(table, "Subtotal:", subtotal.ToString("C"), true);

            // Tax
            decimal tax = subtotal * estimate.TaxRate;
            AddTotalRow(table, $"Tax ({estimate.TaxRate:P}):", tax.ToString("C"));

            // Total
            var totalCell1 = new PdfPCell(new Phrase("TOTAL:", HeaderFont));
            totalCell1.Border = Rectangle.TOP_BORDER;
            totalCell1.PaddingTop = 5f;
            totalCell1.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.AddCell(totalCell1);

            var totalCell2 = new PdfPCell(new Phrase(estimate.TotalPrice.ToString("C"), HeaderFont));
            totalCell2.Border = Rectangle.TOP_BORDER;
            totalCell2.PaddingTop = 5f;
            totalCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.AddCell(totalCell2);

            document.Add(table);
        }

        private void AddTotalRow(PdfPTable table, string label, string value, bool bold = false)
        {
            var font = bold ? BoldFont : NormalFont;
            
            var labelCell = new PdfPCell(new Phrase(label, font));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            labelCell.PaddingBottom = 3f;
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(value, font));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            valueCell.PaddingBottom = 3f;
            table.AddCell(valueCell);
        }

        private void AddTermsAndConditions(Document document, Estimate estimate)
        {
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("TERMS AND CONDITIONS", SubheaderFont));
            document.Add(new Paragraph("\n"));

            var terms = new System.Collections.Generic.List<string>
            {
                "1. This estimate is valid for 30 days from the date above unless otherwise specified.",
                "2. All work will be performed in accordance with local electrical codes and regulations.",
                "3. Any changes or additions to the scope of work may result in additional charges.",
                "4. Materials are subject to availability and price changes.",
                "5. Payment terms: 50% deposit upon acceptance, balance due upon completion.",
                "6. Warranty: One year on all labor, manufacturer's warranty on materials.",
                "7. Customer is responsible for obtaining necessary permits unless otherwise agreed.",
                "8. Access to work areas must be provided during normal business hours."
            };

            foreach (var term in terms)
            {
                document.Add(new Paragraph(term, SmallFont));
            }

            if (!string.IsNullOrEmpty(estimate.Notes))
            {
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("ADDITIONAL NOTES", SubheaderFont));
                document.Add(new Paragraph(estimate.Notes, NormalFont));
            }
        }

        private void AddSignatureBlock(Document document)
        {
            document.Add(new Paragraph("\n\n"));
            
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1, 1 });

            // Acceptance section
            var acceptanceCell = new PdfPCell();
            acceptanceCell.Border = Rectangle.NO_BORDER;
            acceptanceCell.AddElement(new Paragraph("ACCEPTANCE OF ESTIMATE", BoldFont));
            acceptanceCell.AddElement(new Paragraph("\n"));
            acceptanceCell.AddElement(new Paragraph("I accept the terms of this estimate and authorize the work to proceed.\n\n", NormalFont));
            acceptanceCell.AddElement(new Paragraph("_______________________________", NormalFont));
            acceptanceCell.AddElement(new Paragraph("Customer Signature\n", SmallFont));
            acceptanceCell.AddElement(new Paragraph("_______________________________", NormalFont));
            acceptanceCell.AddElement(new Paragraph("Date", SmallFont));
            table.AddCell(acceptanceCell);

            // Company authorization
            var authCell = new PdfPCell();
            authCell.Border = Rectangle.NO_BORDER;
            authCell.AddElement(new Paragraph("AUTHORIZED BY", BoldFont));
            authCell.AddElement(new Paragraph("\n"));
            authCell.AddElement(new Paragraph($"{CompanyName}\n\n", NormalFont));
            authCell.AddElement(new Paragraph("_______________________________", NormalFont));
            authCell.AddElement(new Paragraph("Authorized Signature\n", SmallFont));
            authCell.AddElement(new Paragraph("_______________________________", NormalFont));
            authCell.AddElement(new Paragraph("Date", SmallFont));
            table.AddCell(authCell);

            document.Add(table);

            // Thank you message
            document.Add(new Paragraph("\n"));
            var thankYou = new Paragraph("Thank you for considering us for your electrical needs!", HeaderFont);
            thankYou.Alignment = Element.ALIGN_CENTER;
            document.Add(thankYou);
        }
    }

    // Helper class for page headers/footers - renamed to avoid conflict
    public class EstimatePdfPageEventHelper : PdfPageEventHelper
    {
        private readonly string _companyName;
        private readonly string _estimateNumber;
        private readonly Font _footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);

        public EstimatePdfPageEventHelper(string companyName, string estimateNumber)
        {
            _companyName = companyName;
            _estimateNumber = estimateNumber;
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            var cb = writer.DirectContent;
            
            // Footer
            var footerTable = new PdfPTable(3);
            footerTable.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            footerTable.SetWidths(new float[] { 1, 1, 1 });

            var leftCell = new PdfPCell(new Phrase(_companyName, _footerFont));
            leftCell.Border = Rectangle.NO_BORDER;
            leftCell.HorizontalAlignment = Element.ALIGN_LEFT;
            footerTable.AddCell(leftCell);

            var centerCell = new PdfPCell(new Phrase($"Estimate {_estimateNumber}", _footerFont));
            centerCell.Border = Rectangle.NO_BORDER;
            centerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            footerTable.AddCell(centerCell);

            var rightCell = new PdfPCell(new Phrase($"Page {writer.PageNumber}", _footerFont));
            rightCell.Border = Rectangle.NO_BORDER;
            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            footerTable.AddCell(rightCell);

            footerTable.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin - 10, cb);
        }
    }
}
