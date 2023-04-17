namespace Vepara_ASPNetCore.Requests
{
    public class VeparaTokenRequest
    {
        public string AppKey { private get; set; }
        public string AppSecret { private get; set; }
        internal string MerchantKey { private get; set; }

        public string app_id { get { return this.AppKey; } }
        public string app_secret { get { return this.AppSecret; } }

    }

}
