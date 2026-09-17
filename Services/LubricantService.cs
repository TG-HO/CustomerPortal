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
        // Existing methods for backward compatibility
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

        // Exact /lubricants portal feature methods
        object GetHomeStats(string customerId);
        bool CreateNewOrderHead(string customerCode, string custSite, out int newOrderId, out string error);
        bool DeleteOrderHead(int orderId, out bool hasLines, out string error);
        (string BrandsHtml, string CategoriesHtml) GetBrandsAndCategoriesHtml();
        string GetFilteredProductsHtml(int brand, int category);
        (bool Success, string Price, string Unit, string ProductName) GetProductPriceAndUnit(string productId);
        bool AddOrderLine(string siteName, string custSite, string customerCode, int prodBrand, int prodCategory,
                          string productCode, string productName, double rates, string unit, int qty, string orderRef, out string error);
        object GetActiveOrderHeadsData(string customerId);
        (string TableHtml, List<object> Data) GetOrderDetailsData(string orderNumber);
        bool FinalizeOrder(string orderHead, string customerId, out string error);
        bool CancelTempOrderLine(long orderLineId, string customerId, out string error);
        object GetSubmittedOrdersData(string customerId);
        bool CancelSubmittedOrderLine(long lineNumber, string customerId, out string error);
    }

    public class BrandQueryResult
    {
        public int BRAND { get; set; }
        public string BRANDNAMES { get; set; }
    }

    public class CategoryQueryResult
    {
        public int CATEGORY { get; set; }
        public string CATEGORYNAMES { get; set; }
    }

    public class ProductQueryResult
    {
        public string ITEMID { get; set; }
        public string NAME { get; set; }
    }

    public class PriceQueryResult
    {
        public string itemid { get; set; }
        public string NAME { get; set; }
        public decimal? PRICE { get; set; }
        public string UNITID { get; set; }
    }

    public class OrderLineQueryResult
    {
        public string ORDERNUMBER { get; set; }
        public long ORDERID { get; set; }
        public string PRODUCTNAME { get; set; }
        public double? PRODUCTRATES { get; set; }
        public string UNIT { get; set; }
        public int? REQUIREDQTY { get; set; }
        public double? TOTALAMOUNT { get; set; }
        public DateTime? CREATEDONDATETIME { get; set; }
        public string ORDERSTATUS { get; set; }
    }

    public class SubmittedOrderQueryResult
    {
        public string ordernumber { get; set; }
        public string custsite { get; set; }
        public DateTime? createdondatetime { get; set; }
        public long linenumber { get; set; }
        public string customercode { get; set; }
        public string sitename { get; set; }
        public string productcode { get; set; }
        public string productname { get; set; }
        public int? requiredqty { get; set; }
        public double? productrates { get; set; }
        public string unit { get; set; }
        public double? totalamount { get; set; }
        public string orderstatus { get; set; }
    }

    public class OrderHeadQueryResult
    {
        public int ORDERID { get; set; }
        public string ORDER_STATUS { get; set; }
        public string CUSTOMERCODE { get; set; }
        public string CUSTSITE { get; set; }
        public DateTime? CREATEDDATETIME { get; set; }
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

        #region Exact /lubricants Portal Methods

        public object GetHomeStats(string customerId)
        {
            string custInfo = customerId ?? "";
            int newOrders = 0;
            int completeOrders = 0;
            string balance = "Advance Payment";

            try
            {
                newOrders = _db.Database.SqlQuery<int>(
                    @"select count(*) from LUBRICANT_ORDERS 
                      where status_flag = 0 and (cancel_flag is null or cancel_flag = 0) and customercode = @p0",
                    customerId ?? "").FirstOrDefault();
            }
            catch { }

            try
            {
                completeOrders = _db.Database.SqlQuery<int>(
                    @"select count(distinct ORDERHEAD_REF) from LUBRICANT_ORDERS_FINAL 
                      where customercode = @p0 and STATUS_FLAG = 1",
                    customerId ?? "").FirstOrDefault();
            }
            catch { }

            return new
            {
                customerInfo = custInfo,
                newOrders = newOrders.ToString(),
                completeOrders = completeOrders.ToString(),
                balance = balance
            };
        }

        public bool CreateNewOrderHead(string customerCode, string custSite, out int newOrderId, out string error)
        {
            newOrderId = 0;
            error = null;
            try
            {
                var head = new LubricantOrderHead
                {
                    OrderPrefix = "TLB-",
                    OrderStatus = "SUBMITTED",
                    CustomerCode = customerCode ?? "",
                    CustSite = custSite ?? customerCode ?? "",
                    CreatedDate = DateTime.Now.Date,
                    CreatedDateTime = DateTime.Now,
                    StatusFlag = 0,
                    FinalStatus = 0,
                    DataAreaId = "aml",
                    IsNew = 1
                };

                _db.LubricantOrderHeads.Add(head);
                _db.SaveChanges();

                head.OrderNumber = "TLB-" + head.OrderId;
                _db.SaveChanges();

                newOrderId = head.OrderId;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeleteOrderHead(int orderId, out bool hasLines, out string error)
        {
            hasLines = false;
            error = null;
            try
            {
                string orderNum = "TLB-" + orderId;
                int lineCount = _db.Database.SqlQuery<int>(
                    @"select count(*) from lubricant_orders_head as loh
                      inner join lubricant_orders as lo on loh.ordernumber = lo.orderhead_ref
                      where lo.status_flag = 0 and (lo.cancel_flag is null or lo.cancel_flag = 0)
                      and loh.ordernumber = @p0", orderNum).FirstOrDefault();

                if (lineCount > 0)
                {
                    hasLines = true;
                    return false;
                }

                var head = _db.LubricantOrderHeads.FirstOrDefault(h => h.OrderId == orderId);
                if (head != null)
                {
                    _db.LubricantOrderHeads.Remove(head);
                    _db.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public (string BrandsHtml, string CategoriesHtml) GetBrandsAndCategoriesHtml()
        {
            string brandsHtml = "<option value=\"-1\" selected disabled> -- SELECT -- </option>";
            string categoriesHtml = "<option value=\"-1\" selected disabled> -- SELECT -- </option>";

            try
            {
                string brandSql = @"SELECT DISTINCT i.BRAND, CASE WHEN i.BRAND = 0 THEN 'NONE'
                                    WHEN i.BRAND = 1 THEN 'PSO'
                                    WHEN i.BRAND = 2 THEN 'SHELL'
                                    WHEN i.BRAND = 3 THEN 'CHEVRON'
                                    WHEN i.BRAND = 4 THEN 'INDUS MOTOR' END AS BRANDNAMES
                                    FROM INVENTTABLE AS i
                                    WHERE i.DATAAREAID = 'aml'
                                    UNION SELECT 0, 'NONE'
                                    UNION SELECT 1, 'PSO'
                                    UNION SELECT 2, 'SHELL'
                                    UNION SELECT 3, 'CHEVRON'
                                    UNION SELECT 4, 'INDUS MOTOR'
                                    ORDER BY i.BRAND";

                var brands = _db.Database.SqlQuery<BrandQueryResult>(brandSql).ToList();
                foreach (var b in brands)
                {
                    brandsHtml += $"<option value=\"{b.BRAND}\">{b.BRANDNAMES}</option>";
                }
            }
            catch
            {
                brandsHtml += "<option value=\"0\">NONE</option><option value=\"1\">PSO</option><option value=\"2\">SHELL</option><option value=\"3\">CHEVRON</option><option value=\"4\">INDUS MOTOR</option>";
            }

            try
            {
                string catSql = @"SELECT DISTINCT i.CATEGORY, CASE WHEN i.CATEGORY = 0 THEN 'NONE'
                                  WHEN i.CATEGORY = 1 THEN 'Motor Cycle Oil'
                                  WHEN i.CATEGORY = 2 THEN 'Car Motor Oil'
                                  WHEN i.CATEGORY = 3 THEN 'Diesel Engine Oil'
                                  WHEN i.CATEGORY = 4 THEN 'Loose Oil Drum' END AS CATEGORYNAMES
                                  FROM INVENTTABLE AS i
                                  WHERE i.DATAAREAID = 'T-PB'
                                  UNION SELECT 0, 'NONE'
                                  UNION SELECT 1, 'Motor Cycle Oil'
                                  UNION SELECT 2, 'Car Motor Oil'
                                  UNION SELECT 3, 'Diesel Engine Oil'
                                  UNION SELECT 4, 'Loose Oil Drum'
                                  ORDER BY i.CATEGORY";

                var cats = _db.Database.SqlQuery<CategoryQueryResult>(catSql).ToList();
                foreach (var c in cats)
                {
                    categoriesHtml += $"<option value=\"{c.CATEGORY}\">{c.CATEGORYNAMES}</option>";
                }
            }
            catch
            {
                categoriesHtml += "<option value=\"0\">NONE</option><option value=\"1\">Motor Cycle Oil</option><option value=\"2\">Car Motor Oil</option><option value=\"3\">Diesel Engine Oil</option><option value=\"4\">Loose Oil Drum</option>";
            }

            return (brandsHtml, categoriesHtml);
        }

        public string GetFilteredProductsHtml(int brand, int category)
        {
            string result = "<option value=\"-1\" selected disabled> -- SELECT -- </option>";
            try
            {
                string sql = @"select i.ITEMID, ec.NAME from INVENTTABLE as i
                               inner join INVENTITEMGROUPITEM as ig on ig.ITEMID = i.ITEMID and i.DATAAREAID = ig.ITEMDATAAREAID
                               inner join ECORESPRODUCTTRANSLATION as ec on ec.PRODUCT = i.PRODUCT
                               where i.DATAAREAID = 'aml' 
                               and ig.ITEMGROUPID = 'Lubricant' 
                               and i.brand = @p0
                               and i.CATEGORY = @p1";

                var products = _db.Database.SqlQuery<ProductQueryResult>(sql, brand, category).ToList();
                if (products.Any())
                {
                    foreach (var p in products)
                    {
                        result += $"<option data-name=\"{p.NAME}\" name=\"{p.NAME}\" value=\"{p.ITEMID}\">{p.ITEMID} - {p.NAME}</option>";
                    }
                }
                else
                {
                    result += "<option value=\"-2\">No Records Found</option>";
                }
            }
            catch
            {
                result += "<option value=\"-2\">No Records Found</option>";
            }
            return result;
        }

        public (bool Success, string Price, string Unit, string ProductName) GetProductPriceAndUnit(string productId)
        {
            try
            {
                string sql = @"select i.itemid, ECORESPRODUCTTRANSLATION.NAME, cast(itm.PRICE as decimal(25, 2)) as PRICE, itm.UNITID
                               from INVENTTABLEMODULE itm
                               inner join INVENTTABLE as i on i.ITEMID = itm.ITEMID
                               inner join INVENTITEMGROUPITEM as igm on igm.ITEMID = itm.ITEMID
                               inner join ECORESPRODUCTTRANSLATION on ECORESPRODUCTTRANSLATION.product = i.product
                               where itm.DATAAREAID = 'aml' and i.DATAAREAID = 'aml'
                               and itm.MODULETYPE = 2 and igm.ITEMGROUPID = 'Lubricant'
                               and itm.ITEMID = @p0";

                var item = _db.Database.SqlQuery<PriceQueryResult>(sql, productId ?? "").FirstOrDefault();
                if (item != null)
                {
                    return (true, (item.PRICE ?? 0).ToString("F2"), item.UNITID ?? "", item.itemid ?? productId);
                }
                return (true, "0", "0", "img-not-available");
            }
            catch
            {
                return (true, "0", "0", "img-not-available");
            }
        }

        public bool AddOrderLine(string siteName, string custSite, string customerCode, int prodBrand, int prodCategory,
                                  string productCode, string productName, double rates, string unit, int qty, string orderRef, out string error)
        {
            error = null;
            try
            {
                double totalAmount = rates * qty;
                var line = new LubricantOrderLine
                {
                    CustSite = custSite ?? siteName ?? "",
                    OrderHeadRef = orderRef ?? "",
                    SiteName = siteName ?? "",
                    CustomerCode = customerCode ?? "",
                    ProductBrand = prodBrand.ToString(),
                    ProductCategory = prodCategory.ToString(),
                    ProductCode = productCode ?? "",
                    ProductName = productName ?? productCode ?? "",
                    ProductRates = rates,
                    Unit = unit ?? "",
                    RequiredQty = qty,
                    TotalAmount = totalAmount,
                    DataAreaId = "aml",
                    CreatedBy = customerCode ?? "",
                    CreatedOn = DateTime.Now.Date,
                    CreatedOnDateTime = DateTime.Now,
                    OrderStatus = "SUBMITTED",
                    StatusFlag = 0
                };

                _db.LubricantOrderLines.Add(line);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public object GetActiveOrderHeadsData(string customerId)
        {
            string cid = customerId?.Trim() ?? "";
            try
            {
                string sql = @"select ORDERID, ORDER_STATUS, CUSTOMERCODE, CUSTSITE, CREATEDDATETIME 
                               from LUBRICANT_ORDERS_HEAD 
                               where (cancel_flag = 0 or cancel_flag is null) 
                                 and (status_flag = 0 or status_flag is null) 
                                 and (FINAL_STATUS IS NULL OR FINAL_STATUS = 0) 
                                 and (CUSTOMERCODE = @p0 or RTRIM(LTRIM(CUSTOMERCODE)) = @p0)
                               order by ORDERID desc";

                var list = _db.Database.SqlQuery<OrderHeadQueryResult>(sql, cid).ToList();

                var tableData = list.Select(row => new
                {
                    Empty = $@"<td>
      <button data-ID=""{row.ORDERID}"" 
      data-ORDER_STATUS=""{row.ORDER_STATUS}"" 
      data-CUSTOMERCODE=""{row.CUSTOMERCODE}"" 
      data-CUSTSITE=""{row.CUSTSITE}"" 
      data-CREATEDDATETIME=""{(row.CREATEDDATETIME.HasValue ? row.CREATEDDATETIME.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}""
      data-orderhead=""{row.ORDERID}""
      class=""mr-2 btn CreateHeadButton btnUseOrder"" style=""background-color: #4a53c4; color: white;""> Select</button>
       
      <button data-OrderID=""{row.ORDERID}""  
      data-ORDER_STATUS=""{row.ORDER_STATUS}"" 
      data-CUSTOMERCODE=""{row.CUSTOMERCODE}"" 
      data-CUSTSITE=""{row.CUSTSITE}"" 
      data-CREATEDDATETIME=""{(row.CREATEDDATETIME.HasValue ? row.CREATEDDATETIME.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}""
      class=""mr-2 btn DeleteHeadButton"" id=""btnDeleteOrder{row.ORDERID}"" style=""background-color: #dd3030; color: white;"">Delete</button>
      <br/></td>",
                    ORDERID = "TLB-" + row.ORDERID,
                    ORDER_STATUS = row.ORDER_STATUS ?? "SUBMITTED",
                    CUSTOMERCODE = row.CUSTOMERCODE ?? "",
                    CUSTSITE = row.CUSTSITE ?? "",
                    CREATEDDATETIME = row.CREATEDDATETIME.HasValue ? row.CREATEDDATETIME.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
                }).ToList();

                return new
                {
                    result = "success",
                    message = "query success",
                    data = tableData
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    result = "error",
                    message = ex.Message,
                    data = new List<object>()
                };
            }
        }

        public (string TableHtml, List<object> Data) GetOrderDetailsData(string orderNumber)
        {
            string tableHtml = "";
            var rawList = new List<object>();

            try
            {
                string sql = @"select LOH.ORDERNUMBER, LO.ORDERID, LO.PRODUCTNAME, LO.PRODUCTRATES, LO.UNIT, LO.REQUIREDQTY, LO.TOTALAMOUNT, LO.CREATEDONDATETIME, LO.ORDERSTATUS
                               FROM LUBRICANT_ORDERS as LO
                               INNER JOIN LUBRICANT_ORDERS_HEAD AS LOH ON LOH.ORDERNUMBER = LO.ORDERHEAD_REF
                               WHERE (LO.CANCEL_FLAG IS NULL OR LO.CANCEL_FLAG = 0) AND LO.STATUS_FLAG = 0 and LOH.ORDERNUMBER = @p0";

                var lines = _db.Database.SqlQuery<OrderLineQueryResult>(sql, orderNumber ?? "").ToList();
                foreach (var res in lines)
                {
                    string dt = res.CREATEDONDATETIME.HasValue ? res.CREATEDONDATETIME.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                    tableHtml += $@"<tr>
            <td>{res.ORDERNUMBER}</td>
            <td>{res.ORDERID}</td>
            <td>{res.PRODUCTNAME}</td>
            <td>{(res.PRODUCTRATES ?? 0):F2}</td>
            <td>{res.UNIT}</td>
            <td>{(res.REQUIREDQTY ?? 0):N0}</td>
            <td>{(res.TOTALAMOUNT ?? 0):N0}</td>
            <td>{dt}</td>
            <td><button class=""btn btn-sm btn-danger btnDel"" id=""{res.ORDERID}"" data-id=""{res.ORDERID}"">Remove Item</button></td>
        </tr>";

                    rawList.Add(new
                    {
                        ORDERNUMBER = res.ORDERNUMBER,
                        ORDERID = res.ORDERID,
                        PRODUCTNAME = res.PRODUCTNAME,
                        PRODUCTRATES = res.PRODUCTRATES,
                        UNIT = res.UNIT,
                        REQUIREDQTY = (res.REQUIREDQTY ?? 0).ToString("N0"),
                        TOTALAMOUNT = (res.TOTALAMOUNT ?? 0).ToString("N0"),
                        CREATEDONDATETIME = dt,
                        ORDERSTATUS = res.ORDERSTATUS
                    });
                }
            }
            catch { }

            return (tableHtml, rawList);
        }

        public bool FinalizeOrder(string orderHead, string customerId, out string error)
        {
            error = null;
            try
            {
                string q1 = @"INSERT INTO LUBRICANT_ORDERS_FINAL ( [LINENUMBER]
                  ,[ORDERHEAD_REF],[CUSTSITE],[SITENAME],[CUSTOMERCODE],[PRODUCTBRAND],[PRODUCTCATEGORY],[PRODUCTCODE]
                  ,[PRODUCTNAME],[PRODUCTRATES],[UNIT],[REQUIREDQTY],[TOTALAMOUNT],[DATAAREAID],[CREATEDBY]
                  ,[CREATEDON],[CREATEDONDATETIME],[ORDERSTATUS],[STATUS_FLAG],[CANCEL_FLAG],[CANCELDATETIME]
                  ,[CANCELLED_BY],[UPLOADED_DATETIME],[UPLOADED_BY] )
                SELECT [ORDERID],[ORDERHEAD_REF],[CUSTSITE],[SITENAME],[CUSTOMERCODE],[PRODUCTBRAND],[PRODUCTCATEGORY],[PRODUCTCODE]
                  ,[PRODUCTNAME],[PRODUCTRATES],[UNIT],[REQUIREDQTY],[TOTALAMOUNT],[DATAAREAID],[CREATEDBY]
                  ,[CREATEDON],[CREATEDONDATETIME],[ORDERSTATUS],[STATUS_FLAG],[CANCEL_FLAG],[CANCELDATETIME],[CANCELLED_BY]
                  ,[UPLOADED_DATETIME],[UPLOADED_BY] FROM LUBRICANT_ORDERS WHERE ORDERHEAD_REF = @p0";

                string q2 = @"UPDATE LUBRICANT_ORDERS_HEAD SET FINAL_STATUS = 1 WHERE ORDERNUMBER = @p0";
                string q3 = @"DELETE from LUBRICANT_ORDERS where ORDERHEAD_REF = @p0";

                _db.Database.ExecuteSqlCommand(q1, orderHead ?? "");
                _db.Database.ExecuteSqlCommand(q2, orderHead ?? "");
                _db.Database.ExecuteSqlCommand(q3, orderHead ?? "");

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool CancelTempOrderLine(long orderLineId, string customerId, out string error)
        {
            error = null;
            try
            {
                string sql = @"update LUBRICANT_ORDERS set CANCEL_FLAG = 1, CANCELDATETIME = GETDATE(), CANCELLED_BY = @p0, ORDERSTATUS = 'CANCELLED', STATUS_FLAG = 2
                               where ORDERID = @p1 and (STATUS_FLAG = 0 or STATUS_FLAG is null)";
                _db.Database.ExecuteSqlCommand(sql, customerId ?? "", orderLineId);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public object GetSubmittedOrdersData(string customerId)
        {
            string cid = customerId?.Trim() ?? "";
            try
            {
                string sql = @"select lho.ordernumber, lc.custsite, lc.createdondatetime, lc.linenumber, lc.customercode, lc.sitename, lc.productcode, 
                               lc.productname, lc.requiredqty, lc.productrates, lc.unit, lc.totalamount, lc.orderstatus
                               from lubricant_orders_final as lc
                               inner join lubricant_orders_head as lho on lho.ordernumber = lc.orderhead_ref
                               where lc.status_flag = 0 and lc.dataareaid = 'aml' 
                               and lc.customercode = @p0 and (lc.cancel_flag is null or lc.cancel_flag = 0)
                               order by lc.linenumber desc";

                var list = _db.Database.SqlQuery<SubmittedOrderQueryResult>(sql, cid).ToList();
                var data = list.Select(res => new
                {
                    createdondatetime = res.createdondatetime.HasValue ? res.createdondatetime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    ordernumber = res.ordernumber ?? "",
                    linenumber = res.linenumber.ToString(),
                    customercode = res.customercode ?? "",
                    sitename = res.sitename ?? "",
                    productcode = res.productcode ?? "",
                    productname = res.productname ?? "",
                    unit = res.unit ?? "",
                    requiredqty = res.requiredqty?.ToString() ?? "0",
                    productrates = (res.productrates ?? 0).ToString("N2"),
                    totalamount = (res.totalamount ?? 0).ToString("N2"),
                    orderstatus = res.orderstatus ?? "SUBMITTED",
                    Empty = $@"<td><button data-ID=""{res.linenumber}"" class=""mr-2 btn CreateHeadButton btnDelete"" style=""background-color: #dd3030; color: white;"">Delete</button><br/></td>"
                }).ToList();

                return new
                {
                    result = "success",
                    message = "query success",
                    data = data
                };
            }
            catch
            {
                return new
                {
                    result = "success",
                    message = "query success",
                    data = new List<object>()
                };
            }
        }

        public bool CancelSubmittedOrderLine(long lineNumber, string customerId, out string error)
        {
            error = null;
            try
            {
                string sql = @"update LUBRICANT_ORDERS_FINAL set CANCEL_FLAG = 1, CANCELDATETIME = GETDATE(), CANCELLED_BY = @p0, ORDERSTATUS = 'CANCELLED', STATUS_FLAG = 2
                               where LINENUMBER = @p1 and (STATUS_FLAG = 0 or STATUS_FLAG is null)";
                _db.Database.ExecuteSqlCommand(sql, customerId ?? "", lineNumber);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        #endregion

        #region Existing Methods for Backward Compatibility

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

        #endregion
    }
}
