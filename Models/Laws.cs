using System.ComponentModel.DataAnnotations;
using System.Text;

namespace last.Models
{
    public class Laws
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Zone { get; set; }
        public long Views { get; set; }

        public Laws() { }

        public Laws(string id, string name, string description, string zone)
        {
            Id = id;
            Name = name;
            Description = description;
            Zone = zone;
            Views = Views;
        }
        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Id).Append(" ").Append(this.Name).Append(" ").Append(this.Description).Append(" ").Append(this.Zone).Append(" ");
            return sb.ToString();

        }


    }
}
