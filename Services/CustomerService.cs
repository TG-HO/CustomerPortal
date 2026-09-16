using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CustomerPortal_MVC_.DAL;
using CustomerPortal_MVC_.Models;
using CustomerPortal_MVC_.Models.ViewModels;

namespace CustomerPortal_MVC_.Services
{
    public interface ICustomerService
    {
        decimal GetCustomerBalance(string customerId);
        decimal GetCreditLimit(string customerId);
        decimal GetDealerRate(string customerId, string productCode);
        decimal GetTankCapacity(string customerId, string productCode);
        List<CpDeposit> GetCustomerPayments(string customerId);
        bool SubmitDepositSlip(DepositUploadViewModel model, string customerId, HttpServerUtilityBase server);
        DashboardSummaryViewModel GetDashboardSummary(string customerId);
        List<CustomerTransactionViewModel> GetCustomerTransactions(string customerId);
    }

    public class CustomerService : ICustomerService
    {
        private readonly PortalDbContext _db;

        public CustomerService()
        {
            _db = new PortalDbContext();
        }

        public CustomerService(PortalDbContext db)
        {
            _db = db;
        }

        public decimal GetCustomerBalance(string customerId)
        {
            string cid = customerId?.Trim();
            try
            {
                var result = _db.Database.SqlQuery<decimal?>(
                    @"select sum(ct.amountmst) as Balance from custtrans as ct
                      inner join CUSTTABLE as c on c.ACCOUNTNUM = ct.ACCOUNTNUM and c.dataareaid = ct.dataareaid and (c.dataareaid='tgpl' or c.dataareaid='TGPL')
                      where ct.accountnum = @p0 and (c.dataareaid='tgpl' or c.dataareaid='TGPL')", cid).FirstOrDefault();
                return result ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public decimal GetCreditLimit(string customerId)
        {
            string cid = customerId?.Trim();
            try
            {
                var result = _db.Database.SqlQuery<decimal?>(
                    @"select top 1 c.CREDITMAX as CreditLimit from CUSTTABLE as c
                      where c.accountnum = @p0 and (c.dataareaid='tgpl' or c.dataareaid='TGPL')", cid).FirstOrDefault();
                if (result == null || result == 0m)
                {
                    result = _db.Database.SqlQuery<decimal?>(
                        @"select top 1 c.CREDITMAX as CreditLimit from CUSTTABLE as c
                          where c.accountnum = @p0 order by c.CREDITMAX desc", cid).FirstOrDefault();
                }
                return result ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public decimal GetDealerRate(string customerId, string productCode)
        {
            string cid = customerId?.Trim();
            try
            {
                string col = "unit_price_hsd";
                if (productCode == "02" || string.Equals(productCode, "PMG", StringComparison.OrdinalIgnoreCase))
                    col = "unit_price_pmg";
                else if (productCode == "007" || string.Equals(productCode, "HOBC", StringComparison.OrdinalIgnoreCase))
                    col = "unit_price_HOBC";

                string sql = $"select top 1 {col} from dealerCustomPrices where RTRIM(LTRIM(customer_id)) = @p0 order by status desc, id desc";
                var result = _db.Database.SqlQuery<double?>(sql, cid).FirstOrDefault();
                if (result.HasValue && result.Value > 0)
                {
                    return (decimal)result.Value;
                }

                // Fallback to latest configured rate for any customer
                string fallbackSql = $"select top 1 {col} from dealerCustomPrices where {col} > 0 order by status desc, id desc";
                var fallbackResult = _db.Database.SqlQuery<double?>(fallbackSql).FirstOrDefault();
                if (fallbackResult.HasValue && fallbackResult.Value > 0)
                {
                    return (decimal)fallbackResult.Value;
                }

                return (productCode == "01" || productCode == "HSD" ? 347.11m : productCode == "02" || productCode == "PMG" ? 308.91m : 375.00m);
            }
            catch
            {
                return (productCode == "01" || productCode == "HSD" ? 347.11m : productCode == "02" || productCode == "PMG" ? 308.91m : 375.00m);
            }
        }

        public decimal GetTankCapacity(string customerId, string productCode)
        {
            string cid = customerId?.Trim();
            try
            {
                string col = (productCode == "02" || string.Equals(productCode, "PMG", StringComparison.OrdinalIgnoreCase)) ? "PMGTANK" : "HSDTANK";
                string sql = $@"select isnull(SUM({col}),0) as TankCap from tankCapacity 
                                where CUSTGROUP = (select top 1 custgroup from tankCapacity where CUSTACCOUNT = @p0)";
                var result = _db.Database.SqlQuery<decimal?>(sql, cid).FirstOrDefault();
                return result ?? 50000m;
            }
            catch
            {
                return 50000m;
            }
        }

        public List<CpDeposit> GetCustomerPayments(string customerId)
        {
            string cid = customerId?.Trim();
            return _db.Deposits
                .Where(d => d.AccountCode.Trim() == cid || d.AccountCode == cid)
                .OrderByDescending(d => d.DsDate)
                .ToList();
        }

        public bool SubmitDepositSlip(DepositUploadViewModel model, string customerId, HttpServerUtilityBase server)
        {
            if (model == null || model.DsUpload == null || model.DsUpload.ContentLength == 0)
            {
                return false;
            }

            string fileName = Path.GetFileName(model.DsUpload.FileName);
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension != ".pdf" && extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                return false;
            }

            if (model.DsUpload.ContentLength > 409600) // 400KB limit
            {
                return false;
            }

            string uniqueFileName = Guid.NewGuid().ToString("N") + "_" + fileName;
            string targetFolder = server.MapPath("~/images/ds/");

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string savePath = Path.Combine(targetFolder, uniqueFileName);
            model.DsUpload.SaveAs(savePath);

            var deposit = new CpDeposit
            {
                Amount = model.Amount,
                Bank = model.Bank,
                DsDate = model.DsDate,
                Description = model.Description,
                DsUpload = uniqueFileName,
                AccountCode = customerId?.Trim()
            };

            _db.Deposits.Add(deposit);
            _db.SaveChanges();
            return true;
        }

        public List<CustomerTransactionViewModel> GetCustomerTransactions(string customerId)
        {
            string cid = customerId?.Trim();
            try
            {
                string sql = @"
                    select CT.VOUCHER as Voucher,
                    CASE WHEN CT.TRANSTYPE = 2 THEN 'SALES ORDER' WHEN CT.TRANSTYPE IN (15,36) THEN 'PAYMENT' ELSE '' END AS TransType,
                    CAST(CT.TRANSDATE AS DATE) AS TransDate,
                    case when ct.invoice = '' then 'N/A' else ct.invoice end as Invoice,
                    (SELECT ABS(CCT.AMOUNTCUR) FROM CUSTTRANS CCT WHERE CCT.TRANSTYPE IN (15,36) AND CCT.RECID = CT.RECID) AS Credit,
                    (SELECT ABS(CCT.AMOUNTCUR) FROM CUSTTRANS CCT WHERE CCT.TRANSTYPE = 2 AND CCT.RECID = CT.RECID) AS Debit,
                    (ABS(CT.AMOUNTCUR)-ABS(CT.SETTLEAMOUNTCUR)) AS Balance 
                    FROM CUSTTRANS CT 
                    WHERE CT.ACCOUNTNUM = @p0 AND TRANSTYPE IN (15,2,36) and (ct.DATAAREAID='tgpl' or ct.DATAAREAID='TGPL')
                    ORDER BY CT.CREATEDDATETIME DESC";

                var list = _db.Database.SqlQuery<CustomerTransactionViewModel>(sql, cid).ToList();
                return list;
            }
            catch
            {
                return new List<CustomerTransactionViewModel>();
            }
        }

        public DashboardSummaryViewModel GetDashboardSummary(string customerId)
        {
            string cid = customerId?.Trim();
            var cust = _db.Customers.FirstOrDefault(c => (c.AccountNum.Trim() == cid || c.AccountNum == cid) && (c.DataAreaId == "tgpl" || c.DataAreaId == "TGPL"))
                    ?? _db.Customers.FirstOrDefault(c => c.AccountNum.Trim() == cid || c.AccountNum == cid);
            var orders = _db.CustOrders.Where(o => (o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid) && (o.CancelFlag == 0 || o.CancelFlag == null)).ToList();

            decimal creditLimit = GetCreditLimit(cid);
            decimal balance = GetCustomerBalance(cid);
            decimal rateHsd = GetDealerRate(cid, "01");
            decimal ratePmg = GetDealerRate(cid, "02");
            decimal rateHobc = GetDealerRate(cid, "007");

            decimal totalHfBalance = orders.Sum(o => (decimal)o.HoldsFreeBalance);

            // Amount of Product to be Delivered formula matching PHP:
            // Sum of open order quantities * dealer rates + Salesline open quantities * dealer rates
            decimal openOrderHsd = orders.Where(o => o.ProductCode == "01" && (o.ReceivedFlag == 0 || o.ReceivedFlag == null)).Sum(o => (decimal)(o.PendingQty ?? o.RequiredQuantity));
            decimal openOrderPmg = orders.Where(o => o.ProductCode == "02" && (o.ReceivedFlag == 0 || o.ReceivedFlag == null)).Sum(o => (decimal)(o.PendingQty ?? o.RequiredQuantity));
            decimal openOrderHobc = orders.Where(o => o.ProductCode == "007" && (o.ReceivedFlag == 0 || o.ReceivedFlag == null)).Sum(o => (decimal)(o.PendingQty ?? o.RequiredQuantity));

            decimal dynHsd = 0m, dynPmg = 0m, dynHobc = 0m;
            try
            {
                dynHsd = _db.Database.SqlQuery<decimal?>("select isnull(sum(sl.SALESQTY),0) from SALESLINE sl where sl.SALESSTATUS in (1,2) and itemid = '01' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", cid).FirstOrDefault() ?? 0m;
                dynPmg = _db.Database.SqlQuery<decimal?>("select isnull(sum(sl.SALESQTY),0) from SALESLINE sl where sl.SALESSTATUS in (1,2) and itemid = '02' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", cid).FirstOrDefault() ?? 0m;
                dynHobc = _db.Database.SqlQuery<decimal?>("select isnull(sum(sl.SALESQTY),0) from SALESLINE sl where sl.SALESSTATUS in (1,2) and itemid = '007' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", cid).FirstOrDefault() ?? 0m;
            }
            catch { }

            decimal amountToBeDelivered = ((openOrderHsd + dynHsd) * rateHsd) + ((openOrderPmg + dynPmg) * ratePmg) + ((openOrderHobc + dynHobc) * rateHobc);
            decimal totalBalance = balance + amountToBeDelivered;
            decimal availableCreditLimit = creditLimit - totalBalance;

            var pendingOrders = orders.Where(o => o.PendingFlag == 1 || (o.PendingQty.HasValue && o.PendingQty.Value > 0)).OrderByDescending(o => o.OrderId).ToList();
            var holdsFreeOrders = orders.Where(o => (o.HoldsFreeFlag == 1 || o.PartialHfFlag == 1 || o.HoldsFreeBalance > 0)).OrderByDescending(o => o.OrderId).ToList();
            var scheduledOrders = orders.Where(o => (o.ApprovedStatus == 1 || o.ReleasedFlag == 1) && (o.ShippedFlag == 0 || o.ShippedFlag == null)).OrderByDescending(o => o.OrderId).ToList();
            var shippedOrders = orders.Where(o => o.ShippedFlag == 1 && (o.ReceivedFlag == 0 || o.ReceivedFlag == null)).OrderByDescending(o => o.OrderId).ToList();
            var deliveredOrders = orders.Where(o => o.ReceivedFlag == 1 || (o.ShippedFlag == 1 && o.ProfUploadFlag == 1)).OrderByDescending(o => o.OrderId).Take(20).ToList();

            var summary = new DashboardSummaryViewModel
            {
                CustomerId = cid,
                CustomerName = cust?.PartyEntity?.Name ?? cust?.CustSite ?? cid,
                CustomerSite = cust?.CustSite ?? "Main Depot",
                TotalOrdersCount = orders.Count,
                OpenOrdersCount = orders.Count(o => (o.ReceivedFlag == 0 || o.ReceivedFlag == null) && (o.ShippedFlag == 0 || o.ShippedFlag == null)),
                PendingOrdersCount = pendingOrders.Count,
                InvoicedOrdersCount = orders.Count(o => o.ShippedFlag == 1 && o.ProfUploadFlag == 1),
                TotalHoldsFreeBalance = totalHfBalance,
                CreditLimit = creditLimit,
                CurrentBalance = balance,
                AmountToBeDelivered = amountToBeDelivered,
                AvailableCreditLimit = availableCreditLimit,
                DealerRatesHsd = rateHsd,
                DealerRatesPmg = ratePmg,
                DealerRatesHobc = rateHobc,
                PendingOrdersList = pendingOrders,
                HoldsFreeOrdersList = holdsFreeOrders,
                ScheduledOrdersList = scheduledOrders,
                ShippedOrdersList = shippedOrders,
                DeliveredOrdersList = deliveredOrders
            };

            return summary;
        }
    }
}
