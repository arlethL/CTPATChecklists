namespace CTPATChecklists.Models
{
    public class GlobalSetting
    {
        public int Id { get; set; }

        // ---> Campos SMTP

        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string Password { get; set; }
        // ---> Dirección “From”

        public string FromEmail { get; set; }


    }
}
