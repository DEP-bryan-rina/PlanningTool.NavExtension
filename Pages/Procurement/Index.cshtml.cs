using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;
using System.Data;
using PlanningTool.NavExtension.Constants;

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

        [BindProperty] public string ParamRemarks { get; set; }
        [BindProperty] public string HBLNo { get; set; }
        [BindProperty] public ImPexCenterModel Impex { get; set; }
        [BindProperty] public DateTime? ParamCETA { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var access = HttpContext.Session.GetString("Procurement");

            if (string.IsNullOrEmpty(access))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!bool.Parse(access))
            {
                return RedirectToPage("/AccessDenied");
            }

            this.LoadImpex();
            this.LoadProcurementData(searchString);

            return Page();
        }

        private string GetUser()
        {

            return (User?.Identity != null && User.Identity.IsAuthenticated)
                ? User.Identity.Name
                : Environment.UserName;

        }
        public async Task<IActionResult> OnPostTagAsync(string HBLNo, List<string> SelectedRows)
        {
            this.LoadProcurementData(searchString);
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

                using var con = new SqlConnection(conStr);
                await con.OpenAsync();

                foreach (var row in SelectedRows)
                {
                    var parts = row.Split('|');

                    int docType = int.Parse(parts[0]);
                    string docNo = parts[1];
                    int lineNo = int.Parse(parts[2]);
                    string itemCode = parts[3].Trim();

                    string key = $"{docType}|{docNo}|{lineNo}|{itemCode}";

                    decimal partialDelivered =
                        decimal.TryParse(
                            Request.Form[$"PartialDeliveredValues[{key}]"],
                            out var pd)
                        ? pd
                        : 0;

                    var itemInfo = ProcurementList.FirstOrDefault(x =>
                        x.IDDocType == docType &&
                        x.IDDocNo == docNo &&
                        x.IDLineNo == lineNo &&
                        x.ItemCode == itemCode);

                    taggedItems.Add((
                        docNo,
                        itemCode,
                        itemInfo?.ItemDescription ?? string.Empty,
                        partialDelivered
                    ));

                    using var cmd = new SqlCommand("_spTagProcurementCenter", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@HBLNo", hblNo);
                    cmd.Parameters.AddWithValue("@ContainerNo", containerNo);
                    cmd.Parameters.AddWithValue("@DocType", docType);
                    cmd.Parameters.AddWithValue("@DocNo", docNo);
                    cmd.Parameters.AddWithValue("@LineNo", lineNo);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                    cmd.Parameters.AddWithValue("@ParamPD", partialDelivered);
                    cmd.Parameters.AddWithValue("@ParamCETA", (object?)eta ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ParamCBY", GetUser());
                    cmd.Parameters.AddWithValue(
                        "@ParamRemarks",
                        string.IsNullOrWhiteSpace(ParamRemarks)
                            ? DBNull.Value
                            : ParamRemarks);

                    await cmd.ExecuteNonQueryAsync();
                    updatedCount++;
                }

                TempData["SuccessMessage"] =
                    $"{updatedCount} record(s) updated successfully.";

                if (updatedCount > 0)
                {
                    var recipients = GetRecipients();

                    string itemRows = string.Join("",
                        taggedItems.Select(x => $@"
                        <tr>
                            <td style='padding:4px 4px;'>{x.DocNo}</td>
                            <td style='padding:4px 4px;'>{x.ItemCode}</td>
                            <td style='padding:4px 4px;'>{x.ItemDescription}</td>
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

                        <table border='1' cellpadding='4' cellspacing='0' style='border-collapse:collapse; width:50%;'>
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

            this.LoadImpex();
            this.LoadProcurementData(searchString);

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostUntagAsync(string action)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(action) &&
                    action.StartsWith("untag|"))
                {
                    var parts = action.Replace("untag|", "").Split('|');

                    int docType = int.Parse(parts[0]);
                    string docNo = parts[1];
                    int lineNo = int.Parse(parts[2]);
                    string itemCode = parts[3];
                    string hblNo = parts[4];

                    DateTime conETA = DateTime.Parse(parts[5]);
                    string etaDisplay = conETA.ToString("MMM dd, yyyy");

                    var itemInfo = ProcurementList.FirstOrDefault(x =>
                        x.IDDocType == docType &&
                        x.IDDocNo == docNo &&
                        x.IDLineNo == lineNo &&
                        x.ItemCode == itemCode);

                    string itemDescription = itemInfo?.ItemDescription ?? string.Empty;
                    decimal quantity = itemInfo?.ptPartialDelivered ?? 0;

                    using var con = new SqlConnection(conStr);
                    await con.OpenAsync();

                    using (var cmd = new SqlCommand("_spUntagProcurementCenter", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@DocType", docType);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        cmd.Parameters.AddWithValue("@LineNo", lineNo);
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@HBLNo", hblNo);
                        cmd.Parameters.AddWithValue("@ModifiedBy", GetUser());

                        await cmd.ExecuteNonQueryAsync();
                    }

                    TempData["SuccessMessage"] = "Record untagged successfully!";

                    var recipients = GetRecipients();

                    string body = $@"
                        <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                            <p>Dear Team,</p>

                            <p>
                                This is to inform you that the following shipment details
                                have been <strong>untagged</strong> from the corresponding
                                Purchase Order in the system.
                            </p>

                            <table>

                                <tr>
                                    <th style='text-align:left;'>HBL No</th>
                                    <td>: {hblNo}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>PO No</th>
                                    <td>: {docNo}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Item Code</th>
                                    <td>: {itemCode}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Dedon ETA</th>
                                    <td>: {etaDisplay}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Status</th>
                                    <td>: Untagged</td>
                                </tr>

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
                            "PO Untagged from Shipment",
                            body,
                            EmailModules.Procurement));
                }
            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Database error: " + sqlEx.Message;
            }

            // ✅ Always reload data
            this.LoadProcurementData(searchString);
            this.LoadImpex();

            return RedirectToPage();
        }
        private void LoadProcurementData(string _search)
        {
            try
            {
                _search = string.IsNullOrWhiteSpace(_search) ? null : _search.Trim();

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    SqlCommand cmd = new SqlCommand("_spGetProcurementCenter", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    // ✅ Pass parameter properly
                    cmd.Parameters.AddWithValue("@Search",
                        string.IsNullOrEmpty(_search) ? (object)DBNull.Value : _search);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        ProcurementList.Clear();

                        while (reader.Read())
                        {
                            ProcurementList.Add(new ProcurementCenterDto
                            {
                                IDDocType = Convert.ToInt32(reader["IDDocType"]),
                                IDDocNo = reader["IDDocNo"].ToString(),
                                IDLineNo = Convert.ToInt32(reader["IDLineNo"]),
                                VendorName = reader["VendorName"].ToString(),
                                ItemCode = reader["ItemCode"].ToString(),
                                ItemDescription = reader["ItemDescription"].ToString(),
                                OutstandingQty = reader["OutstandingQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OutstandingQty"]),

                                ptBalanceQty = reader["ptBalanceQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ptBalanceQty"]),
                                ptPartialDelivered = reader["ptPartialDelivered"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ptPartialDelivered"]),
                                ptHBLNo = reader["ptHBLNo"].ToString(),
                                ptContainerNo = reader["ptContainerNo"].ToString(),
                                ptContainerETA = reader["ptContainerETA"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ptContainerETA"]),
                                ptCountryOfOrigin = reader["ptCountryOfOrigin"] == DBNull.Value ? "" : reader["ptCountryOfOrigin"].ToString(),
                                ptRemarks = reader["ptRemarks"] == DBNull.Value ? "" : reader["ptRemarks"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Database error: " + sqlEx.Message;
            }
        }
        private void LoadImpex()
        {
            ImpexList = _context.tblImPexCenter
                .Where(x => x._ptStatus == 0)
                .Select(x => new ImPexCenterModel
                {
                    PK_ptImpexID = x.PK_ptImpexID,
                    _ptHBLNo = x._ptHBLNo,
                    _ptContainerNo = x._ptContainerNo,
                    _ptContainerETA = x._ptContainerETA
                })
                .ToList();
        }

    }
}
