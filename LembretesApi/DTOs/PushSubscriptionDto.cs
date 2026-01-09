namespace LembretesApi.DTOs
{
    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public KeysDto Keys { get; set; } = new KeysDto();
    }

    public class KeysDto
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
