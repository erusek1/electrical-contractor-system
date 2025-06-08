using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class RoomTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public List<RoomTemplateItem> Items { get; set; }

        public RoomTemplate()
        {
            Items = new List<RoomTemplateItem>();
            IsActive = true;
        }
    }
}
