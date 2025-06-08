using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class EstimateRoom
    {
        public int RoomId { get; set; }
        public int EstimateId { get; set; }
        public string RoomName { get; set; }
        public int RoomOrder { get; set; }
        public List<EstimateItem> Items { get; set; }

        public EstimateRoom()
        {
            Items = new List<EstimateItem>();
        }
    }
}
