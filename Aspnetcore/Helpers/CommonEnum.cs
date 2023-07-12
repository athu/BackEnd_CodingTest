namespace WebApi.Helpers
{
    public class CommonEnum
    {
        public enum MailFolder : int
        {
            Sent = 0,
            Received = 1
        }

        public enum MailLabels : int
        {
            ASSET_LOAN = 0,
            ASSET_VERIFICATION = 1,
            ASSET_SERVICING = 2,
            ASSET_LOST_DAMAGED = 3,
            ASSET_DONATED = 4,
            ASSET_TRANSFER = 5,
            OTHERS = 6
        };
    }
}
