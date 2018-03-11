namespace WalletApp.Models
{
    public class HomeViewModel
    {
        public string Amount { get; set; }

        public string DestinationAddress { get; set; }

        public decimal Balance { get; set; }

        public string WalletAddress{ get; set; }

        public string SenderId { get; set; }
    }

}
