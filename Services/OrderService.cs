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
    public interface IOrderService
    {
        CustOrderTable CreateOrder(OrderCreateViewModel model, string customerId, string dataAreaId);
        List<CustOrderTable> GetAllOrders(string customerId);
        List<CustOrderTable> GetOpenOrders(string customerId);
        List<CustOrderTable> GetInvoicedOrders(string customerId);
        List<CustOrderTable> GetPendingOrders(string customerId);
        CustOrderTable GetOrderDetail(string orderPrefixId);
        bool CancelOrder(string orderPrefixId, string userId);
        bool ConfirmReceivedOrder(string orderPrefixId, string userId);
        bool ProcessAdminRelease(OrderApproveViewModel model, string adminId);
        bool UpdateHoldsFreeQty(string orderPrefixId, decimal hfQty, string adminId);
        bool MergeOrders(OrderMergeViewModel model, string adminId);
        bool RevertOrder(string orderPrefixId, decimal releaseQty, string adminId);
        bool UpdateTankLorry(string orderPrefixId, string carrierCode, string adminId);
        bool UploadProforma(ProformaUploadViewModel model, string adminId, HttpServerUtilityBase server);
    }

    public class OrderService : IOrderService
    {
        private readonly PortalDbContext _db;

        public OrderService()
        {
            _db = new PortalDbContext();
        }

        public OrderService(PortalDbContext db)
        {
            _db = db;
        }

        public CustOrderTable CreateOrder(OrderCreateViewModel model, string customerId, string dataAreaId)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(customerId)) throw new ArgumentNullException(nameof(customerId));

            string cid = customerId.Trim();
            string pCode = model.ProductCode ?? "01";
            string pName = model.ProductName ?? (pCode == "01" ? "High Speed Diesel" : (pCode == "02" ? "Premier Motor Gasoline" : "HOBC"));
            string site = model.Site ?? "Main Depot";
            decimal qty = model.NewOrderQuantity;

            // Try executing legacy stored procedure
            try
            {
                _db.Database.ExecuteSqlCommand(
                    "EXEC InsertCUSTORDERTABLE @OrderCreatedUser = @p0, @ProductCode = @p1, @ProductName = @p2, @RequiredQuantity = @p3, @SiteName = @p4",
                    cid, pCode, pName, qty, site);

                var latestOrder = _db.CustOrders.Where(o => o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid).OrderByDescending(o => o.OrderId).FirstOrDefault();
                if (latestOrder != null)
                {
                    try
                    {
                        _db.Database.ExecuteSqlCommand("EXEC CP_ONORDER @customerId = @p0, @orderid = @p1", cid, latestOrder.OrderId);
                    }
                    catch { /* Suppress error */ }

                    return latestOrder;
                }
            }
            catch (Exception)
            {
                // Fallback to manual EF insertion if stored procedure is unavailable
            }

            int nextOrderId = 1;
            long nextRecId = 1;
            if (_db.CustOrders.Any())
            {
                nextOrderId = _db.CustOrders.Max(o => o.OrderId) + 1;
                nextRecId = _db.CustOrders.Max(o => o.RecId) + 1;
            }

            string orderPrefixId = site + "-" + nextOrderId.ToString();

            var order = new CustOrderTable
            {
                OrderId = nextOrderId,
                RecId = nextRecId,
                OrderPrefixId = orderPrefixId,
                OrderCreatedUser = cid,
                ProductCode = pCode,
                ProductName = pName,
                RequiredQuantity = (int)qty,
                HoldFreeQty = 0,
                HoldsFreeBalance = 0,
                PendingQty = (int)qty,
                ReleaseQty = 0,
                HoldsFreeFlag = 0,
                PartialHfFlag = 0,
                PendingFlag = 1,
                ReleasedFlag = 0,
                ShippedFlag = 0,
                CancelFlag = 0,
                ReceivedFlag = 0,
                ProfUploadFlag = 0,
                Site = site,
                Unit = "L",
                OrderCreatedOn = DateTime.Now.Date,
                OrderCreatedOnDateTime = DateTime.Now,
                DataAreaId = string.IsNullOrEmpty(dataAreaId) ? "tgpl" : dataAreaId
            };

            _db.CustOrders.Add(order);
            try
            {
                _db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException valEx)
            {
                var errorMessages = new List<string>();
                foreach (var validationErrors in valEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
                throw new InvalidOperationException("Entity Validation Error: " + string.Join("; ", errorMessages), valEx);
            }

            try
            {
                _db.Database.ExecuteSqlCommand("EXEC CP_ONORDER @customerId = @p0, @orderid = @p1", cid, nextOrderId);
            }
            catch { /* Suppress */ }

            return order;
        }

        public List<CustOrderTable> GetAllOrders(string customerId)
        {
            string cid = customerId?.Trim();
            return _db.CustOrders
                .Where(o => o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid)
                .OrderByDescending(o => o.OrderId)
                .ToList();
        }

        public List<CustOrderTable> GetOpenOrders(string customerId)
        {
            string cid = customerId?.Trim();
            return _db.CustOrders
                .Where(o => (o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid)
                            && (o.CancelFlag == 0 || o.CancelFlag == null)
                            && (o.ReceivedFlag == 0 || o.ReceivedFlag == null)
                            && (o.ShippedFlag == 0 || o.ShippedFlag == null))
                .OrderByDescending(o => o.OrderId)
                .ToList();
        }

        public List<CustOrderTable> GetInvoicedOrders(string customerId)
        {
            string cid = customerId?.Trim();
            return _db.CustOrders
                .Where(o => (o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid)
                            && o.ShippedFlag == 1
                            && o.ProfUploadFlag == 1)
                .OrderByDescending(o => o.OrderId)
                .ToList();
        }

        public List<CustOrderTable> GetPendingOrders(string customerId)
        {
            string cid = customerId?.Trim();
            return _db.CustOrders
                .Where(o => (o.OrderCreatedUser.Trim() == cid || o.OrderCreatedUser == cid)
                            && (o.PendingFlag == 1 || o.PendingQty > 0)
                            && (o.CancelFlag == 0 || o.CancelFlag == null))
                .OrderByDescending(o => o.OrderId)
                .ToList();
        }

        public CustOrderTable GetOrderDetail(string orderPrefixId)
        {
            if (string.IsNullOrWhiteSpace(orderPrefixId)) return null;
            string op = orderPrefixId.Trim();
            return _db.CustOrders.FirstOrDefault(o => o.OrderPrefixId == op || o.OrderPrefixId.Trim() == op);
        }

        public bool CancelOrder(string orderPrefixId, string userId)
        {
            var order = GetOrderDetail(orderPrefixId);
            if (order == null || order.ReleasedFlag == 1) return false;

            order.CancelFlag = 1;
            order.CancelDateTime = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        public bool ConfirmReceivedOrder(string orderPrefixId, string userId)
        {
            var order = GetOrderDetail(orderPrefixId);
            if (order == null) return false;

            order.ReceivedFlag = 1;
            order.ReceivedDateTime = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        public bool ProcessAdminRelease(OrderApproveViewModel model, string adminId)
        {
            var order = GetOrderDetail(model.OrderPrefixId);
            if (order == null) return false;

            int releaseQty = (int)model.ReleaseQty;
            int reqQty = order.RequiredQuantity;

            if (releaseQty >= reqQty)
            {
                // Full Release
                order.ReleaseQty = releaseQty;
                order.ReleasedFlag = 1;
                order.PendingFlag = 0;
                order.PendingQty = 0;
                order.HoldsFreeFlag = 0;
                order.HoldsFreeBalance = 0;
                order.PartialHfFlag = 0;
                order.ApprovedDate = DateTime.Now;
            }
            else if (releaseQty > 0)
            {
                // Partial Release: Split order
                int remaining = reqQty - releaseQty;
                order.ReleaseQty = releaseQty;
                order.HoldsFreeBalance = remaining;
                order.HoldsFreeFlag = 1;
                order.PartialHfFlag = 1;
                order.ApprovedDate = DateTime.Now;

                // Create child record for remaining quantity
                int nextOrderId = _db.CustOrders.Max(o => o.OrderId) + 1;
                var childOrder = new CustOrderTable
                {
                    OrderId = nextOrderId,
                    OrderPrefixId = order.OrderPrefixId + "-P",
                    OrderCreatedUser = order.OrderCreatedUser,
                    ProductCode = order.ProductCode,
                    ProductName = order.ProductName,
                    RequiredQuantity = remaining,
                    HoldFreeQty = remaining,
                    HoldsFreeBalance = remaining,
                    PendingQty = remaining,
                    ReleaseQty = 0,
                    HoldsFreeFlag = 1,
                    PartialHfFlag = 1,
                    PendingFlag = 1,
                    ReleasedFlag = 0,
                    ShippedFlag = 0,
                    CancelFlag = 0,
                    ReceivedFlag = 0,
                    ProfUploadFlag = 0,
                    Site = order.Site,
                    OrderCreatedOn = DateTime.Now.Date,
                    OrderCreatedOnDateTime = DateTime.Now,
                    DataAreaId = order.DataAreaId
                };
                _db.CustOrders.Add(childOrder);
            }

            _db.SaveChanges();
            return true;
        }

        public bool UpdateHoldsFreeQty(string orderPrefixId, decimal hfQty, string adminId)
        {
            var order = GetOrderDetail(orderPrefixId);
            if (order == null) return false;

            order.HoldFreeQty = order.HoldFreeQty + (int)hfQty;
            order.HoldsFreeDateTime = DateTime.Now;
            order.HoldsFreeFlag = 1;
            _db.SaveChanges();
            return true;
        }

        public bool MergeOrders(OrderMergeViewModel model, string adminId)
        {
            var order1 = GetOrderDetail(model.Order1PrefixId);
            var order2 = GetOrderDetail(model.Order2PrefixId);

            if (order1 == null || order2 == null) return false;

            int combinedQty = order1.RequiredQuantity + order2.RequiredQuantity;
            int nextOrderId = _db.CustOrders.Max(o => o.OrderId) + 1;
            string mergedPrefix = "TG-MGR-" + DateTime.Now.ToString("yyyyMMdd") + "-" + nextOrderId.ToString("D4");

            var mergedOrder = new CustOrderTable
            {
                OrderId = nextOrderId,
                OrderPrefixId = mergedPrefix,
                OrderCreatedUser = order1.OrderCreatedUser,
                ProductCode = order1.ProductCode,
                ProductName = order1.ProductName,
                RequiredQuantity = combinedQty,
                HoldFreeQty = 0,
                HoldsFreeBalance = combinedQty,
                PendingQty = combinedQty,
                ReleaseQty = 0,
                HoldsFreeFlag = 0,
                PartialHfFlag = 0,
                PendingFlag = 1,
                ReleasedFlag = 0,
                CancelFlag = 0,
                Site = order1.Site,
                OrderCreatedOn = DateTime.Now.Date,
                OrderCreatedOnDateTime = DateTime.Now,
                DataAreaId = order1.DataAreaId
            };

            // Mark original orders as merged/cancelled
            order1.CancelFlag = 1;
            order1.CancelDateTime = DateTime.Now;
            order2.CancelFlag = 1;
            order2.CancelDateTime = DateTime.Now;

            _db.CustOrders.Add(mergedOrder);
            _db.SaveChanges();
            return true;
        }

        public bool RevertOrder(string orderPrefixId, decimal releaseQty, string adminId)
        {
            var order = GetOrderDetail(orderPrefixId);
            if (order == null) return false;

            order.HoldsFreeFlag = 1;
            order.HoldFreeQty = (int)releaseQty;
            order.ReleasedFlag = 0;
            order.ReleaseQty = 0;
            _db.SaveChanges();
            return true;
        }

        public bool UpdateTankLorry(string orderPrefixId, string carrierCode, string adminId)
        {
            var order = GetOrderDetail(orderPrefixId);
            if (order == null) return false;

            order.CarrierCode = carrierCode;
            _db.SaveChanges();
            return true;
        }

        public bool UploadProforma(ProformaUploadViewModel model, string adminId, HttpServerUtilityBase server)
        {
            var order = GetOrderDetail(model.OrderPrefixId);
            if (order == null || model.ProformaFile == null || model.ProformaFile.ContentLength == 0) return false;

            string fileName = Path.GetFileName(model.ProformaFile.FileName);
            string extension = Path.GetExtension(fileName).ToLower();

            if (extension != ".pdf" && extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                return false;
            }

            if (model.ProformaFile.ContentLength > 409600) // 400KB limit
            {
                return false;
            }

            string uniqueFileName = Guid.NewGuid().ToString("N") + "_" + fileName;
            string targetFolder = server.MapPath("~/images/proforma/");

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string savePath = Path.Combine(targetFolder, uniqueFileName);
            model.ProformaFile.SaveAs(savePath);

            order.ProfUpload = uniqueFileName;
            order.ProfUploadFlag = 1;
            _db.SaveChanges();
            return true;
        }
    }
}
