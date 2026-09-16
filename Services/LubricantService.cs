using System;
using System.Collections.Generic;
using System.Linq;
using CustomerPortal_MVC_.DAL;
using CustomerPortal_MVC_.Models;
using CustomerPortal_MVC_.Models.ViewModels;

namespace CustomerPortal_MVC_.Services
{
    public interface ILubricantService
    {
        List<LubricantBrand> GetBrands();
        List<LubricantProduct> GetProductsByBrand(int brandId);
        decimal GetProductPrice(string productCode, string customerId);
        LubricantOrderHead CreateLubeOrder(LubeOrderCreateViewModel model, string customerId);
        LubricantOrderFinal FinalizeLubeOrder(long headId, string customerId);
        List<LubricantOrderHead> GetSubmittedLubeOrders(string customerId);
        List<LubricantOrderFinal> AdminGetSubmittedLubeOrders();
        bool AdminUpdateLubeStatus(long finalOrderId, string status, string adminId);
        bool AdminCancelLubeOrder(long finalOrderId, string adminId);
        bool AdminEditLubeOrder(long finalOrderId, decimal qty, decimal rate, string adminId);
    }

    public class LubricantService : ILubricantService
    {
        private readonly PortalDbContext _db;

        public LubricantService()
        {
            _db = new PortalDbContext();
        }

        public LubricantService(PortalDbContext db)
        {
            _db = db;
        }

        public List<LubricantBrand> GetBrands()
        {
            return new List<LubricantBrand>
            {
                new LubricantBrand { BrandId = 1, BrandName = "PSO" },
                new LubricantBrand { BrandId = 2, BrandName = "SHELL" },
                new LubricantBrand { BrandId = 3, BrandName = "CHEVRON" },
                new LubricantBrand { BrandId = 4, BrandName = "INDUS MOTOR" }
            };
        }

        public List<LubricantProduct> GetProductsByBrand(int brandId)
        {
            try
            {
                string sql = @"select i.ITEMID as ProductCode, 
                                      ec.NAME as ProductName, 
                                      isnull(cast(itm.PRICE as decimal(18,2)), 0) as Price,
                                      @p0 as BrandId
                               from INVENTTABLE as i
                               inner join INVENTITEMGROUPITEM as ig on ig.ITEMID = i.ITEMID and i.DATAAREAID = ig.ITEMDATAAREAID
                               inner join ECORESPRODUCTTRANSLATION as ec on ec.PRODUCT = i.PRODUCT
                               left join INVENTTABLEMODULE itm on itm.ITEMID = i.ITEMID and itm.DATAAREAID = 'aml' and itm.MODULETYPE = 2
                               where i.DATAAREAID = 'aml' 
                               and ig.ITEMGROUPID = 'Lubricant' 
                               and i.brand = @p0
                               order by ec.NAME";
                var list = _db.Database.SqlQuery<LubricantProduct>(sql, brandId).ToList();
                return list;
            }
            catch
            {
                return new List<LubricantProduct>();
            }
        }

        public decimal GetProductPrice(string productCode, string customerId)
        {
            try
            {
                string sql = @"select top 1 cast(itm.PRICE as decimal(18,2))
                               from INVENTTABLEMODULE itm
                               where itm.DATAAREAID = 'aml' and itm.MODULETYPE = 2 and itm.ITEMID = @p0";
                var price = _db.Database.SqlQuery<decimal?>(sql, productCode).FirstOrDefault();
                return price ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public LubricantOrderHead CreateLubeOrder(LubeOrderCreateViewModel model, string customerId)
        {
            decimal unitPrice = model.UnitPrice > 0 ? model.UnitPrice : GetProductPrice(model.ProductCode, customerId);
            string prodName = model.ProductName;
            if (string.IsNullOrEmpty(prodName))
            {
                try
                {
                    string nameSql = @"select top 1 ec.NAME from INVENTTABLE as i
                                       inner join ECORESPRODUCTTRANSLATION as ec on ec.PRODUCT = i.PRODUCT
                                       where i.ITEMID = @p0 and i.DATAAREAID = 'aml'";
                    prodName = _db.Database.SqlQuery<string>(nameSql, model.ProductCode).FirstOrDefault() ?? model.ProductCode;
                }
                catch
                {
                    prodName = model.ProductCode;
                }
            }

            decimal totalPrice = model.Quantity * unitPrice;

            int nextOrderId = 1;
            if (_db.LubricantOrderHeads.Any())
            {
                nextOrderId = _db.LubricantOrderHeads.Max(h => h.OrderId) + 1;
            }
            string orderNumber = "TLB-" + nextOrderId;

            var head = new LubricantOrderHead
            {
                OrderId = nextOrderId,
                OrderPrefix = "TLB-",
                OrderNumber = orderNumber,
                CustomerCode = customerId,
                CreatedDate = DateTime.Now.Date,
                CreatedDateTime = DateTime.Now,
                OrderStatus = "SUBMITTED",
                StatusFlag = 0,
                FinalStatus = 0,
                DataAreaId = "aml",
                IsNew = 1,
                TotalQty = (int)model.Quantity,
                GrandTotal = totalPrice
            };

            _db.LubricantOrderHeads.Add(head);
            _db.SaveChanges();

            var line = new LubricantOrderLine
            {
                OrderHeadRef = orderNumber,
                CustomerCode = customerId,
                ProductCode = model.ProductCode,
                ProductName = prodName,
                RequiredQty = (int)model.Quantity,
                ProductRates = (double)unitPrice,
                TotalAmount = (double)totalPrice,
                DataAreaId = "aml",
                CreatedBy = customerId,
                CreatedOn = DateTime.Now.Date,
                CreatedOnDateTime = DateTime.Now,
                OrderStatus = "SUBMITTED",
                StatusFlag = 0
            };

            _db.LubricantOrderLines.Add(line);
            _db.SaveChanges();

            return head;
        }

        public LubricantOrderFinal FinalizeLubeOrder(long headId, string customerId)
        {
            int hId = (int)headId;
            var head = _db.LubricantOrderHeads.FirstOrDefault(h => h.OrderId == hId);
            if (head == null) return null;

            var firstLine = _db.LubricantOrderLines.FirstOrDefault(l => l.OrderHeadRef == head.OrderNumber);
            if (firstLine == null) return null;

            var finalOrder = new LubricantOrderFinal
            {
                OrderHeadRef = head.OrderNumber,
                CustomerCode = customerId,
                SiteName = firstLine.SiteName ?? head.CustSite ?? "Main",
                CustSite = head.CustSite ?? "1",
                ProductCode = firstLine.ProductCode,
                ProductName = firstLine.ProductName,
                RequiredQty = firstLine.RequiredQty,
                ProductRates = firstLine.ProductRates,
                TotalAmount = firstLine.TotalAmount,
                StatusFlag = 0,
                CancelFlag = 0,
                DataAreaId = "aml",
                CreatedBy = customerId,
                CreatedOn = DateTime.Now.Date,
                CreatedOnDateTime = DateTime.Now,
                OrderStatus = "PENDING_APPROVAL",
                IsNew = 1
            };

            _db.LubricantOrderFinals.Add(finalOrder);
            head.FinalStatus = 1;
            _db.SaveChanges();

            return finalOrder;
        }

        public List<LubricantOrderHead> GetSubmittedLubeOrders(string customerId)
        {
            string cid = customerId?.Trim();
            var heads = _db.LubricantOrderHeads
                .Where(h => (h.CustomerCode.Trim() == cid || h.CustomerCode == cid) && (h.CancelFlag == 0 || h.CancelFlag == null))
                .OrderByDescending(h => h.OrderId)
                .ToList();

            if (!heads.Any()) return heads;

            var orderNums = heads.Select(h => h.OrderNumber).ToList();
            var finals = _db.LubricantOrderFinals.Where(f => orderNums.Contains(f.OrderHeadRef)).ToList();
            var lines = _db.LubricantOrderLines.Where(l => orderNums.Contains(l.OrderHeadRef)).ToList();

            foreach (var h in heads)
            {
                var matchingFinals = finals.Where(f => f.OrderHeadRef == h.OrderNumber).ToList();
                if (matchingFinals.Any())
                {
                    h.TotalQty = matchingFinals.Sum(f => f.RequiredQty ?? 0);
                    h.GrandTotal = (decimal)matchingFinals.Sum(f => f.TotalAmount ?? 0);
                }
                else
                {
                    var matchingLines = lines.Where(l => l.OrderHeadRef == h.OrderNumber).ToList();
                    h.TotalQty = matchingLines.Sum(l => l.RequiredQty ?? 0);
                    h.GrandTotal = (decimal)matchingLines.Sum(l => l.TotalAmount ?? 0);
                }
            }

            return heads;
        }

        public List<LubricantOrderFinal> AdminGetSubmittedLubeOrders()
        {
            return _db.LubricantOrderFinals
                .Where(f => f.CancelFlag == 0 || f.CancelFlag == null)
                .OrderByDescending(f => f.LineNumber)
                .ToList();
        }

        public bool AdminUpdateLubeStatus(long finalOrderId, string status, string adminId)
        {
            var finalOrder = _db.LubricantOrderFinals.FirstOrDefault(f => f.LineNumber == finalOrderId);
            if (finalOrder == null) return false;

            finalOrder.OrderStatus = string.IsNullOrEmpty(status) ? "UPLOADED" : status;
            finalOrder.StatusFlag = 1;
            finalOrder.UploadedBy = adminId;
            finalOrder.UploadedDateTime = DateTime.Now;

            var head = _db.LubricantOrderHeads.FirstOrDefault(h => h.OrderNumber == finalOrder.OrderHeadRef);
            if (head != null)
            {
                head.OrderStatus = finalOrder.OrderStatus;
                head.StatusFlag = 1;
            }

            _db.SaveChanges();
            return true;
        }

        public bool AdminCancelLubeOrder(long finalOrderId, string adminId)
        {
            var finalOrder = _db.LubricantOrderFinals.FirstOrDefault(f => f.LineNumber == finalOrderId);
            if (finalOrder == null) return false;

            finalOrder.CancelFlag = 1;
            finalOrder.OrderStatus = "CANCELLED";
            finalOrder.StatusFlag = 2;
            finalOrder.CancelledBy = adminId;
            finalOrder.CancelDateTime = DateTime.Now;

            _db.SaveChanges();
            return true;
        }

        public bool AdminEditLubeOrder(long finalOrderId, decimal qty, decimal rate, string adminId)
        {
            var finalOrder = _db.LubricantOrderFinals.FirstOrDefault(f => f.LineNumber == finalOrderId);
            if (finalOrder == null) return false;

            finalOrder.RequiredQty = (int)qty;
            finalOrder.ProductRates = (double)rate;
            finalOrder.TotalAmount = (double)(qty * rate);

            _db.SaveChanges();
            return true;
        }
    }
}
