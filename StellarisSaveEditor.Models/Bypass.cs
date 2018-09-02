namespace StellarisSaveEditor.Models
{
    public class Bypass
    {
        public int Id { get; set; }

        public string BypassType { get; set; }

        public bool IsActive { get; set; }

        public Owner Owner { get; set; }

        public int? LinkedToBypassId { get; set; }
    }
}
