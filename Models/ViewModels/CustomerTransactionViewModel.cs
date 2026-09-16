using System;
using System.Collections.Generic;

namespace CustomerPortal_MVC_.Models.ViewModels
{
    public class CustomerTransactionViewModel
    {
        public string Voucher { get; set; }
        public string TransType { get; set; }
        public DateTime? TransDate { get; set; }
        public string Invoice { get; set; }
        public decimal? Credit { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Balance { get; set; }
    }

    public class CustomerTransactionPageViewModel
    {
        public decimal CustomerBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AvailableCreditLimit { get; set; }
        public List<CustomerTransactionViewModel> Transactions { get; set; }

        public CustomerTransactionPageViewModel()
        {
            Transactions = new List<CustomerTransactionViewModel>();
        }
    }
}
