namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a room-specific electrical item for a job
    /// </summary>
    public class RoomSpecification
    {
        /// <summary>
        /// Database ID of the room specification
        /// </summary>
        public int SpecId { get; set; }

        /// <summary>
        /// Associated job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Name of the room (e.g., Kitchen, Living Room, Main Bedroom)
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// Description of the electrical item
        /// </summary>
        public string ItemDescription { get; set; }

        /// <summary>
        /// Quantity of the item
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Optional reference to price list item code
        /// </summary>
        public string ItemCode { get; set; }

        /// <summary>
        /// Unit price of the item
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total price (quantity × unit price)
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Navigation property to Job
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Navigation property to PriceList item
        /// </summary>
        public PriceList PriceListItem { get; set; }

        /// <summary>
        /// Recalculates the total price based on quantity and unit price
        /// </summary>
        public void RecalculateTotalPrice()
        {
            TotalPrice = Quantity * UnitPrice;
        }

        /// <summary>
        /// Updates the unit price from the price list item if available
        /// </summary>
        public void UpdatePriceFromPriceList()
        {
            if (PriceListItem != null)
            {
                UnitPrice = PriceListItem.TotalCost;
                RecalculateTotalPrice();
            }
        }

        /// <summary>
        /// Returns a formatted description including quantity
        /// </summary>
        public string FormattedDescription
        {
            get
            {
                return $"{Quantity} × {ItemDescription}";
            }
        }

        /// <summary>
        /// Returns a formatted specification with room, description, and price
        /// </summary>
        public string FormattedSpecification
        {
            get
            {
                return $"{RoomName}: {Quantity} × {ItemDescription} - ${TotalPrice:F2}";
            }
        }

        /// <summary>
        /// Creates a copy of the specification for another job
        /// </summary>
        public RoomSpecification CreateCopy(int newJobId)
        {
            return new RoomSpecification
            {
                JobId = newJobId,
                RoomName = this.RoomName,
                ItemDescription = this.ItemDescription,
                Quantity = this.Quantity,
                ItemCode = this.ItemCode,
                UnitPrice = this.UnitPrice,
                TotalPrice = this.TotalPrice
            };
        }
    }
}
