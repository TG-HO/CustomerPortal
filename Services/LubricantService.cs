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
            return _db.LubricantBrands.OrderBy(b => b.BrandName).ToList();
        }

        public List<LubricantProduct> GetProductsByBrand(int brandId)
        {
            return _db.LubricantProducts
                .Where(p => p.BrandId == brandId)
                .OrderBy(p => p.ProductName)
                .ToList();
        }

        public decimal GetProductPrice(string productCode, string customerId)
        {
            var product = _db.LubricantProducts.FirstOrDefault(p => p.ProductCode == productCode);
            return product?.Price ?? 0m;
        }

        public LubricantOrderHead CreateLubeOrder(LubeOrderCreateViewModel model, string customerId)
        {
            var product = _db.LubricantProducts.FirstOrDefault(p => p.ProductCode == model.ProductCode);
            decimal unitPrice = model.UnitPrice > 0 ? model.UnitPrice : (product?.Price ?? 0m);
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
                IsNew = 1
            };

            _db.LubricantOrderHeads.Add(head);
            _db.SaveChanges();

            var line = new LubricantOrderLine
            {
                OrderHeadRef = orderNumber,
                CustomerCode = customerId,
                ProductCode = model.ProductCode,
                ProductName = model.ProductName ?? product?.ProductName,
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
            return _db.LubricantOrderHeads
                .Where(h => (h.CustomerCode.Trim() == cid || h.CustomerCode == cid) && (h.CancelFlag == 0 || h.CancelFlag == null))
                .OrderByDescending(h => h.OrderId)
                .ToList();
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
