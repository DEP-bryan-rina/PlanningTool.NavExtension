using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Constants;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;

namespace PlanningTool.NavExtension.Pages.Procurement
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;   // ✅ NEW
        private readonly IUserPermissionService _userPermissionService;

        private readonly string conStr;
        public string searchString { get; set; }

        public IndexModel(AppDbContext context, IConfiguration config, EmailService emailService, IUserPermissionService userPermissionService)
        {

            _context = context;
            _emailService = emailService;
            _userPermissionService = userPermissionService;
            this.conStr = config.GetConnectionString("DevConnection");
        }
        public List<ImPexCenterModel> ImpexList { get; set; } = new();

        public List<ProcurementCenterDto> ProcurementList { get; set; } = new();
        public List<ProcurementCenterDto> ProcurementListTagged { get; set; } = new();


        [BindProperty] public string ParamRemarks { get; set; }

        
        [BindProperty(SupportsGet = true)] 

        public string? HBLNo { get; set; }
        public List<ProcurementCenterBalance> ProcurementTagged { get; set; } = new();        

        [BindProperty] public ImPexCenterModel Impex { get; set; }
        [BindProperty] public DateTime? ParamCETA { get; set; }
        [BindProperty] public string SelectedRowsJson { get; set; }

        private List<string> GetRecipients()
        {
            var recipients = _context.EmailNotifications
             .Where(x => x._ptProcurement)
             .Select(x => x._ptEmailAddress)
             .Where(x => !string.IsNullOrWhiteSpace(x))
             .Distinct()
             .ToList(); 

            return recipients;
        }
        public JsonResult OnGetTaggedItems(string hblNo)
        {
            var data = _context.GetTaggedItems
                .Where(x => x._ptHBLNo == hblNo && x._ptStatus != 0)
                .Select(x => new
                {
                    x.PK_ptProcID,
                    x._ptDocNo,
                    x._ptVendorName,
                    x._ptItemCode,
                    x._ptItemDescription,
                    x._ptOutstandingQty,
                    x._ptPartialDelivered,
                    x._ptBalanceQty
                })
                .ToList();

            return new JsonResult(data);
        }
        
        public async Task<IActionResult> OnGetAsync()
        {

            // Load Impex Information
            this.LoadImpex();

            // Load PO Oustanding Qty.
            this.LoadProcurementData("_spGetProcurementCenter", ProcurementList);

            // Load Tagged POs
            ProcurementTagged = _context.ProcurementCenterBalanceView
            .Where(x => x._ptStatus == 1)
            .ToList();          
            

            return Page();
        }

        private string GetUser()
        {

            return (User?.Identity != null && User.Identity.IsAuthenticated)
                ? User.Identity.Name
                : Environment.UserName;

        }

        public async Task<IActionResult> OnPostTagAsync()
        {
            try
            {
                string hblNo = Request.Form["HBLNo"];
                string containerNo = Request.Form["ContainerNo"];

                DateTime? eta = DateTime.TryParse(
                    Request.Form["ParamCETA"],
                    out var tempEta)
                    ? tempEta
                    : null;

                string? etaDisplay = eta?.ToString("MMM dd, yyyy");

                int updatedCount = 0;

                var taggedItems = new List<(
                    string DocNo,
                    string ItemCode,
                    string ItemDescription,
                    decimal Quantity
                )>();

                var selectedItems =
                    JsonSerializer.Deserialize<List<ProcurementCenterDto>>
                    (
                        SelectedRowsJson
                    ) ?? new();


                using var con = new SqlConnection(conStr);
                await con.OpenAsync();

                foreach (var item in selectedItems)
                {

                    taggedItems.Add((
                        item.IDDocNo,
                        item.ItemCode,
                        item.ItemDescription,
                        item.ptPartialDelivered.GetValueOrDefault()
                    ));

                    using var cmd = new SqlCommand("_spTagProcurementCenter", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add("@HBLNo", SqlDbType.NVarChar, 50).Value = item.ptHBLNo;
                    cmd.Parameters.Add("@ContainerNo", SqlDbType.NVarChar, 50).Value = item.ptContainerNo;
                    cmd.Parameters.Add("@DocType", SqlDbType.Int).Value = item.IDDocType;
                    cmd.Parameters.Add("@DocNo", SqlDbType.NVarChar, 50).Value = item.IDDocNo;
                    cmd.Parameters.Add("@LineNo", SqlDbType.Int).Value = item.IDLineNo;
                    cmd.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 50).Value = item.ItemCode;

                    var pd = cmd.Parameters.Add("@ParamPD", SqlDbType.Decimal);
                    pd.Precision = 18;
                    pd.Scale = 2;
                    pd.Value = item.ptPartialDelivered ?? 0m;

                    cmd.Parameters.Add("@ParamCETA", SqlDbType.DateTime2).Value =
                        item.ptContainerETA ?? (object)DBNull.Value;

                    cmd.Parameters.Add("@ParamCBY", SqlDbType.NVarChar, 100).Value = GetUser();

                    cmd.Parameters.Add("@ParamRemarks", SqlDbType.NVarChar, 255).Value =
                        string.IsNullOrWhiteSpace(item.ptRemarks)
                            ? DBNull.Value
                            : item.ptRemarks;

                    await cmd.ExecuteNonQueryAsync();

                    updatedCount++;
                }
                TempData["SuccessMessage"] = $"{updatedCount} record(s) tagged successfully.";

                if (updatedCount > 0)
                {
                    var recipients = GetRecipients();

                    string itemRows = string.Join("",
                        taggedItems.Select(x => $@"
                        <tr>
                            <td style='padding:4px 4px;'>{x.DocNo}</td>
                            <td style='padding:4px 4px;'>{x.ItemCode}</td>
                            <td style='padding:4px 4px;'>{x.ItemDescription ?? "-"}</td>
                            <td style='padding:4px 4px; text-align:right;'>
                                {x.Quantity:N2}
                            </td>
                        </tr>"));

                    string emailBody = $@"
                   <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                        <p>Dear Team,</p>

                        <p>
                            This is to inform you that the following shipment has been
                            <strong>successfully tagged</strong> in the system.
                        </p>

                        <table>
                            <tr>
                                <th style='text-align:left;'>HBL No</th>
                                <td>: {hblNo}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Container No</th>
                                <td>: {containerNo}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Dedon ETA</th>
                                <td>: {etaDisplay}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Status</th>
                                <td>: Tagged</td>
                            </tr>
                        </table>

                        <p><strong>Tagged Purchase Order Details:</strong></p>

                        <table border='1' cellpadding='4' cellspacing='0' style='border-collapse:collapse; width:70%;'>
                            <thead>
                                <tr>
                                    <th style='text-align:left;'>PO No</th>
                                    <th style='text-align:left;'>Item Code</th>
                                    <th style='text-align:left;'>Item Description</th>
                                    <th style='text-align:right;'>Expected Qty</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemRows}
                            </tbody>
                        </table>

                        <p>
                            Should you encounter any issues or require further assistance,
                            please do not hesitate to contact the IT Team.
                        </p>

                        <hr />

                        <small>
                            This is a system-generated email. Please do not reply.
                        </small>

                    </div>";

                    _ = Task.Run(() =>
                    _emailService.SendEmailAsync(
                        recipients,
                        "Purchase Order Successfully Tagged",
                        emailBody,
                        EmailModules.Procurement)
                    );
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            TempData["ActiveTab"] = "#withoutstandingqty";
            return RedirectToPage();
        }

        private void LoadProcurementData(string spName, List<ProcurementCenterDto> targetList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand(spName, con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        targetList.Clear();

                        while (reader.Read())
                        {
                            targetList.Add(MapProcurement(reader));
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Database error: " + sqlEx.Message;
            }
        }
        private ProcurementCenterDto MapProcurement(SqlDataReader reader)
        {
            return new ProcurementCenterDto
            {
                IDDocType = Convert.ToInt32(reader["IDDocType"]),
                IDDocNo = reader["IDDocNo"].ToString(),
                IDLineNo = Convert.ToInt32(reader["IDLineNo"]),
                VendorName = reader["VendorName"].ToString(),
                ItemCode = reader["ItemCode"].ToString(),
                ItemDescription = reader["ItemDescription"].ToString(),
                OutstandingQty = reader["OutstandingQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OutstandingQty"]),
                ptPartialDelivered = reader["ptPartialDelivered"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ptPartialDelivered"]),
                ptBalanceQty = reader["ptBalanceQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ptBalanceQty"]),
                ptHBLNo = reader["ptHBLNo"].ToString(),
                ptContainerNo = reader["ptContainerNo"].ToString(),
                ptContainerETA = reader["ptContainerETA"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ptContainerETA"]),
                ptCountryOfOrigin = reader["ptCountryOfOrigin"]?.ToString() ?? "",
                ptRemarks = reader["ptRemarks"]?.ToString() ?? ""
            };
        }

        private void LoadImpex()
        {
            ImpexList = _context.tblImPexCenter
                .Where(x => x._ptStatus != 2)
                .Select(x => new ImPexCenterModel
                {
                    PK_ptImpexID = x.PK_ptImpexID,
                    _ptHBLNo = x._ptHBLNo,
                    _ptContainerNo = x._ptContainerNo,
                    _ptCountryOfOrigin = x._ptCountryOfOrigin,
                    _ptContainerETA = x._ptContainerETA
                })
                .ToList();
        }
        private void CheckImPexStatus()
        {

        }

        [BindProperty]
        public string UntagIDs { get; set; }
        public IActionResult OnPostUntag(string UntagIDs)
        {
            // ==============================
            // Validate selected records
            // ==============================

            if (string.IsNullOrEmpty(UntagIDs))
            {
                TempData["ErrorMessage"] = "No records selected.";
                return RedirectToPage();
            }


            // ==============================
            // Convert selected IDs to List
            // ==============================

            var ids = UntagIDs
                .Split(',')
                .Select(int.Parse)
                .ToList();


            // ==============================
            // Get selected tagged PO records
            // ==============================

            var selectedRecords = _context.GetTaggedItems
                .Where(x => ids.Contains(x.PK_ptProcID))
                .ToList();


            if (!selectedRecords.Any())
            {
                TempData["ErrorMessage"] = "Records not found.";
                return RedirectToPage();
            }


            var user = GetUser();
            var now = DateTime.Now;


            // ==============================
            // Remove tagging from selected PO
            // Reset status and delivered quantity
            // ==============================

            foreach (var item in selectedRecords)
            {
                item._ptPartialDelivered = 0;
                item._ptStatus = 0;
                item._ptModifiedBy = user;
                item._ptModifiedDate = now;
            }


            _context.SaveChanges();


            // ==============================
            // Get affected HBL numbers
            // Used to validate ImPex status
            // ==============================

            var hblNumbers = selectedRecords
                .Where(x => !string.IsNullOrEmpty(x._ptHBLNo))
                .Select(x => x._ptHBLNo)
                .Distinct()
                .ToList();


            if (hblNumbers.Any())
            {

                // ==============================
                // Check remaining tagged PO
                // If HBL still has active tagged items,
                // keep ImPex status active
                // ==============================

                var activeTaggedHBL = _context.GetTaggedItems
                    .Where(x =>
                        hblNumbers.Contains(x._ptHBLNo) &&
                        x._ptStatus == 1)
                    .Select(x => x._ptHBLNo)
                    .Distinct()
                    .ToList();



                // ==============================
                // Get related ImPex records
                // ==============================

                var impexRecords = _context.tblImPexCenter
                    .Where(x => hblNumbers.Contains(x._ptHBLNo))
                    .ToList();



                // ==============================
                // Update ImPex status
                // If no tagged PO remains for HBL,
                // make it available again
                // ==============================

                foreach (var impex in impexRecords)
                {
                    if (!activeTaggedHBL.Contains(impex._ptHBLNo))
                    {
                        impex._ptStatus = 0;
                        impex._ptModifiedBy = user;
                        impex._ptModifiedDate = now;
                    }
                }


                _context.SaveChanges();

            }



            // ==============================
            // Build Email Table
            // ==============================

            var emailRows = new StringBuilder();

            foreach (var item in selectedRecords)
            {
            emailRows.Append($@"
                <tr>
                    <td style='padding:4px 4px;'>{item._ptDocNo}</td>
                    <td style='padding:4px 4px;'>{item._ptItemCode}</td>
                    <td style='padding:4px 4px;'>{item._ptItemDescription ?? "-"}</td>
                    <td style='padding:4px 4px;'>{item._ptHBLNo}</td>
                    <td style='padding:4px 4px;'>{item._ptContainerNo}</td>
                    <td>{item._ptContainerETA?.ToString("yyyy-MM-dd")}</td>              
                </tr>");
            }


            var recipients = GetRecipients();

            string emailBody = $@"
                   <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                        <p>Dear Team,</p>

                        <p> 
                            This is to inform you that the following shipment has been
                            <strong>successfully untagged</strong> in the system.
                        </p>

                        <table border='1' cellpadding='4' cellspacing='0' style='border-collapse:collapse; width:70%;'>
                            <thead>
                                <tr>
                                    <th style='text-align:left;'>PO No</th>
                                    <th style='text-align:left;'>Item Code</th>
                                    <th style='text-align:left;'>Item Description</th>
                                    <th style='text-align:left;'>HBL No</th>
                                    <th style='text-align:left;'>Container No</th>
                                    <th style='text-align:left;'>Dedon ETA</th>                                    
                                </tr>
                            </thead>
                            <tbody>
                                {emailRows}
                            </tbody>
                        </table>

                        <p>
                            Should you encounter any issues or require further assistance,
                            please do not hesitate to contact the IT Team.
                        </p>

                        <hr />

                        <small>
                            This is a system-generated email. Please do not reply.
                        </small>

                    </div>";

            _ = Task.Run(() =>
            _emailService.SendEmailAsync(
                recipients,
                "Purchase Order Successfully Untagged",
                emailBody,
                EmailModules.Procurement)
            );


            TempData["SuccessMessage"] =
                "Selected records untagged and balance quantities updated successfully.";

            TempData["ActiveTab"] = "#tagged";

            return RedirectToPage();

        }
        
        public async Task<IActionResult> OnPostTaggedUpdate(string TaggedUpdateJson)
        {
            if (string.IsNullOrWhiteSpace(TaggedUpdateJson))
            {
                TempData["ErrorMessage"] = "No record selected.";
                return RedirectToPage();
            }

            var updates = JsonSerializer.Deserialize<List<TaggedUpdateModel>>(TaggedUpdateJson);

            if (updates != null)
            {
                foreach (var item in updates)
                {
                    var record = _context.GetTaggedItems
                        .FirstOrDefault(x => x.PK_ptProcID == item.ID);

                    if (record != null)
                    {
                        record._ptPartialDelivered = item.PartialDelivered;

                        record._ptBalanceQty =
                            record._ptOutstandingQty - item.PartialDelivered;

                        record._ptModifiedBy = GetUser();
                        record._ptModifiedDate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Tagged records updated successfully.";
            TempData["ActiveTab"] = "#tagged";

            return RedirectToPage();
        }
    }
}
