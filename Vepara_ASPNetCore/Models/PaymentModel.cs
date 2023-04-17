using Vepara_ASPNetCore.Responses;
using System;

namespace Vepara_ASPNetCore.Models
{
    [Serializable]
    public class PaymentModel
    {
        public int CustomerId { get; set; }

        public string OrderId { get; set; }

        public decimal OrderTotal { get; set; }

        public string PaymentMethodSystemName { get; set; }

        public string CreditCardType { get; set; }

        public string CreditCardName { get; set; }

        public string CreditCardNumber { get; set; }

        public int CreditCardExpireYear { get; set; }

        public int CreditCardExpireMonth { get; set; }

        public string CreditCardCvv2 { get; set; }

        public string PurchaseOrderNumber { get; set; } //satınalma sipariş numarası

        public int InstallmentNumber { get; set; }

        public PosData SelectedPosData { get; set; }
        public PaymentType Is3D { get; set; }
        public decimal Amount { get; set; }

    }
}
