using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vepara_ASPNetCore.Extensions;
using Vepara_ASPNetCore.Models;
using Vepara_ASPNetCore.Requests;
using Vepara_ASPNetCore.Responses;
using Vepara_ASPNetCore.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Vepara_ASPNetCore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ILogger<CheckoutController> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutController(ILogger<CheckoutController> logger, IConfiguration config,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormCollection form)
        {
            var paymentForm = GetPaymentInfo(form);

            var recurring = GetRecurringPaymentInfo(form);

            Settings settings = new(_config["Vepara:AppKey"],
                _config["Vepara:AppSecret"],
                _config["Vepara:MerchantKey"],
                _config["Vepara:BaseUrl"]);

            Item product = new Item();
            product.Description = "Test";
            product.Name = "Test Ürünü";
            product.Quantity = 1;
            product.Price = 1;

            if (paymentForm.Is3D == PaymentType.WhiteLabel3D || paymentForm.Is3D == PaymentType.WhiteLabel2DOr3D)
            {
                //// 3D

                Vepara3DPaymentRequest paymentRequest = new Vepara3DPaymentRequest(settings, paymentForm.SelectedPosData);


                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                paymentRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                paymentRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                paymentRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                paymentRequest.CCHolderName = paymentForm.CreditCardName;
                paymentRequest.CCV = paymentForm.CreditCardCvv2;
                paymentRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                paymentRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                paymentRequest.InvoiceDescription = "";
                Random rnd = new Random();
                int num = rnd.Next();
                paymentRequest.InvoiceId = num.ToString();
                paymentRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    paymentRequest.IsRecurringPayment = true;
                    paymentRequest.RecurringPaymentNumber = recurring.Item2;
                    paymentRequest.RecurringPaymentCycle = recurring.Item3;
                    paymentRequest.RecurringPaymentInterval = recurring.Item4;
                    paymentRequest.RecurringWebhookKey = "webhook";
                }

                string requestForm = paymentRequest.GenerateFormHtmlToRedirect(_config["Vepara:BaseUrl"] + "/api/pay3d");

                var bytes = Encoding.UTF8.GetBytes(requestForm);


                await HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);

                // return View("Request3DVepara", requestForm);
            }

            return View();
        }

        [NonAction]
        public (bool, int, string, int) GetRecurringPaymentInfo(IFormCollection form)
        {
            bool is_recurring_payment = false;
            int recurring_payment_number = 0;
            string recurring_payment_cycle = "";
            int recurring_payment_interval = 0;

            if (!string.IsNullOrEmpty(form["is_recurring_payment"]))
            {
                is_recurring_payment = form["is_recurring_payment"] == "on";
                //Boolean.TryParse(form["is_recurring_payment"], out is_recurring_payment);
            }

            if (!string.IsNullOrEmpty(form["recurring_payment_number"]))
            {
                int.TryParse(form["recurring_payment_number"], out recurring_payment_number);
            }

            if (!string.IsNullOrEmpty(form["recurring_payment_cycle"]))
            {
                recurring_payment_cycle = form["recurring_payment_cycle"];
            }

            if (!string.IsNullOrEmpty(form["recurring_payment_interval"]))
            {
                int.TryParse(form["recurring_payment_interval"], out recurring_payment_interval);
            }

            return (is_recurring_payment, recurring_payment_number, recurring_payment_cycle, recurring_payment_interval);
        }

        [NonAction]
        public PaymentModel GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new PaymentModel();

            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];

            if (!string.IsNullOrEmpty(form["card-number"]))
            {
                paymentInfo.CreditCardNumber = form["card-number"];
            }

            if (!string.IsNullOrEmpty(form["ExpireMonth"]))
            {
                paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            }

            if (!string.IsNullOrEmpty(form["ExpireYear"]))
            {
                paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            }

            if (!string.IsNullOrEmpty(form["Amount"]))
            {
                paymentInfo.Amount = decimal.Parse(form["Amount"]);
            }

            if (!string.IsNullOrEmpty(form["OrderId"]))
            {
                paymentInfo.OrderId = form["OrderId"];
            }

            paymentInfo.CreditCardCvv2 = form["CardCode"];

            if (!string.IsNullOrEmpty(form["SelectedPosData"]))
            {
                var posData = form["SelectedPosData"];

                paymentInfo.SelectedPosData = JsonConvert.DeserializeObject<PosData>(form["SelectedPosData"]);
            }

            if (!string.IsNullOrEmpty(form["Is3D"]))
            {
                paymentInfo.Is3D = (PaymentType)(Int32.TryParse(form["Is3D"], out int is3D) ? is3D : 0);
            }

            return paymentInfo;
        }

        public ActionResult CheckBinCode(string binCode)
        {
            if (binCode.Length >= 6)
            {
                Settings settings = new(_config["Vepara:AppKey"],
                    _config["Vepara:AppSecret"],
                    _config["Vepara:MerchantKey"],
                    _config["Vepara:BaseUrl"]);


                VeparaGetPosRequest posRequest = new();

                posRequest.CreditCardNo = binCode;
                posRequest.Amount = 1;
                posRequest.CurrencyCode = "TRY";
                posRequest.IsRecurring = false;

                VeparaGetPosResponse posResponse = VeparaPaymentService.GetPos(posRequest, settings, GetAuthorizationToken(settings).Data.token);


                var data = posResponse.Data[0];
                posResponse.Data.Clear();
                posResponse.Data.Add(data);

                return Ok(new { posResponse = posResponse, is_3d = GetAuthorizationToken(settings).Data.is_3d });
            }

            return Ok();
        }

        [NonAction]
        public VeparaTokenResponse GetAuthorizationToken(Settings settings)
        {
            if (HttpContext.Session.Get<VeparaTokenResponse>("token") == default)
            {
                VeparaTokenResponse tokenResponse = VeparaPaymentService.CreateToken(settings);

                HttpContext.Session.Set<VeparaTokenResponse>("token", tokenResponse);
            }

            return HttpContext.Session.Get<VeparaTokenResponse>("token");
        }

        public IActionResult SuccessUrl()
        {
            string vepara__status = HttpContext.Request.Query["vepara__status"];
            string order_no = HttpContext.Request.Query["order_no"];
            string invoice_id = HttpContext.Request.Query["invoice_id"];
            string status_description = HttpContext.Request.Query["status_description"];
            string vepara__payment_method = HttpContext.Request.Query["vepara__payment_method"];

            string fullQuery = " invoice_id : " + invoice_id
                                                + "vepara__status :" + vepara__status + "order_no :" + order_no + "status_description :" + status_description
                                                + "vepara__payment_method :" + vepara__payment_method;

            ViewBag.SuccessMessage = fullQuery;

            return View();
        }

        public IActionResult CancelUrl()
        {
            string error_code = HttpContext.Request.Query["error-code"];
            string error = HttpContext.Request.Query["error"];
            string invoice_id = HttpContext.Request.Query["invoice_id"];

            string vepara__status = HttpContext.Request.Query["vepara__status"];
            string order_no = HttpContext.Request.Query["order_no"];
            string status_description = HttpContext.Request.Query["status_description"];
            string vepara__payment_method = HttpContext.Request.Query["vepara__payment_method"];

            string fullQuery = "error_code : " + error_code + " invoice_id : " + invoice_id + " error : " + error
                               + "vepara__status :" + vepara__status + "order_no :" + order_no + "status_description :" + status_description
                               + "vepara__payment_method :" + vepara__payment_method;

            ViewBag.Error = fullQuery;

            return View();
        }
    }
}
