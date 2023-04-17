using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using Vepara_ASPNetCore.Requests;
using Vepara_ASPNetCore.Models;
using Vepara_ASPNetCore.Responses;
using Vepara_ASPNetCore.Helpers;
using Newtonsoft.Json;

namespace Vepara_ASPNetCore.Services
{
    public class VeparaPaymentService
    {

        private static readonly HttpClient _httpClient;
        static VeparaPaymentService()
        {
#if !NETSTANDARD
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            _httpClient = new HttpClient();
        }

        //TAKSİT BİLGİSİ ALMA
        //taksit listesini tarım kartları için vade aralığını ve ödeme sıklığını sağlamaktan sorumludur.
        public static VeparaGetPosResponse GetPos(VeparaGetPosRequest request, Settings settings, string token)
        {

            request.MerchantKey = settings.MerchantKey;

            var header = new Dictionary<string, string>();
            header.Add("Authorization", "Bearer " + token);

            VeparaGetPosResponse response = PostDataAsync<VeparaGetPosResponse, VeparaGetPosRequest>(settings.BaseUrl + "/api/getpos", request, header);

            return response;
        }

        public static Response PostDataAsync<Response, Request>(string endPoint, Request dto, Dictionary<string, string> headers = null)
        {

            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(endPoint),
                Content = JsonBuilder.ToJsonString<Request>(dto)
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            var httpResponse = _httpClient.SendAsync(requestMessage).GetAwaiter().GetResult();

            var t = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!httpResponse.IsSuccessStatusCode)
            {
                return default;
            }


            return JsonConvert.DeserializeObject<Response>(httpResponse.Content.ReadAsStringAsync().Result);
        }

        //TOKEN ALMA: Api iş yerini doğrulamak için diğer apilerle kullanılacak bir token oluşturulur
        //Üye iş yeri için ayarlanan ödeme entegrasyon seçeneği de döndürülür
        //yanıt anahtarı is_3D dir: 0 whiteLabel 2D,2: 1 whitelabel 2D veya 3D,3: whitelabel 3D,4: Markalı ödeme çözümü
        public static VeparaTokenResponse CreateToken(Settings settings)
        {
            VeparaTokenRequest tokenRequest = new VeparaTokenRequest();
            tokenRequest.AppKey = settings.AppKey;
            tokenRequest.AppSecret = settings.AppSecret;

            VeparaTokenResponse response = PostDataAsync<VeparaTokenResponse, VeparaTokenRequest>(settings.BaseUrl + "/api/token", tokenRequest);
            return response;
        }
    }
}
