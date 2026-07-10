using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;
using System.Data;

namespace PlanningTool.NavExtension.Pages.Procurement
{
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
            this.conStr = config.GetConnectionString("DefaultConnection");
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

            //var userId = User?.Identity?.Name;

            //if (string.IsNullOrWhiteSpace(userId))
            //{
            //    return Unauthorized();
            //}

            //userId = userId.ToUpperInvariant();

            //var userPermission = await _userPermissionService.GetUserPermissionAsync(userId);

            //if (userPermission == null || !userPermission.Procurement)
            //{
            //    return RedirectToPage("/AccessDenied", new
            //    {
            //        returnUrl = "/Procurement/Index"
            //    });
            //}

            LoadImpex();
            LoadProcurementData(searchString);

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
            try
            {
                if (SelectedRows == null || !SelectedRows.Any())
                {
                    TempData["ErrorMessage"] = "No PO rows selected.";
                    LoadImpex();
                    LoadProcurementData(searchString);
                    return RedirectToPage();
                }

                string hblNo = Request.Form["HBLNo"];
                string containerNo = Request.Form["ContainerNo"];

                string? paramCETA = DateTime.TryParse(Request.Form["ParamCETA"], out var eta)
                    ? eta.ToString("MMM dd, yyyy")
                    : null;

                int updatedCount = 0;

                // Hold checked PO / Item details for email
                var taggedItems = new List<(string DocNo, string ItemCode)>();

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    await con.OpenAsync();

                    foreach (string row in SelectedRows)
                    {
                        var parts = row.Split('|');

                        int docType = int.Parse(parts[0]);
                        string docNo = parts[1];
                        int lineNo = int.Parse(parts[2]);
                        string itemCode = parts[3].Trim();

                        taggedItems.Add((docNo, itemCode));

                        string key = $"{docType}|{docNo}|{lineNo}|{itemCode}";
                        string pdValue = Request.Form[$"PartialDeliveredValues[{key}]"];

                        using SqlCommand cmd = new("_spTagProcurementCenter", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@HBLNo", hblNo);
                        cmd.Parameters.AddWithValue("@ContainerNo", containerNo);
                        cmd.Parameters.AddWithValue("@DocType", docType);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        cmd.Parameters.AddWithValue("@LineNo", lineNo);
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@ParamPD", pdValue);
                        cmd.Parameters.AddWithValue("@ParamCETA", (object?)paramCETA ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ParamCBY", GetUser());
                        cmd.Parameters.AddWithValue(
                            "@ParamRemarks",
                            string.IsNullOrWhiteSpace(ParamRemarks)
                                ? DBNull.Value
                                : ParamRemarks);

                        await cmd.ExecuteNonQueryAsync();
                        updatedCount++;
                    }
                }

                TempData["SuccessMessage"] = $"{updatedCount} record(s) updated successfully.";

                if (updatedCount > 0)
                {
                    var recipients = GetRecipients();

                    string itemRows = string.Join("",
                        taggedItems.Select(x =>
                            $"<tr>" +
                            $"<td style='padding:2px 5px; text-align:left;'>{x.DocNo}</td>" +
                            $"<td style='padding:2px 5px; text-align:left;'>{x.ItemCode}</td>" +
                            $"</tr>"));

                    _ = Task.Run(() =>
                        _emailService.SendEmailAsync(
                            recipients,
                            "Purchase Order Successfully Tagged",
                            $@"
                            <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff; text-align:left;'>
                                <p>Dear Team,</p>

                                <p>
                                    This is to inform you that the following shipment has been successfully
                                    <strong>tagged</strong> in the system.
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
                                        <td>: {paramCETA}</td>
                                    </tr>

                                    <tr>
                                        <th style='text-align:left;'>Status</th>
                                        <td>: Tagged </td>
                                    </tr>

                                </table>

                                <p><strong>Tagged Purchase Order Details:</strong></p>

                                <table class='table table-bordered'>

                                    <thead>
                                        <tr>
                                            <th style='text-align:left;'>PO No</th>
                                            <th style='text-align:left;'>Item Code</th>
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

                                <p>
                                    Regards,<br />
                                    Procurement Monitoring Team
                                </p>

                                <hr />

                                <small>
                                    This is a system-generated email. Please do not reply.
                                </small>

                            </div>"
                        ));
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
                if (!string.IsNullOrEmpty(action) && action.StartsWith("untag|"))
                {
                    var data = action.Replace("untag|", "");
                    var parts = data.Split('|');

                    int docType = int.Parse(parts[0]);
                    string docNo = parts[1];
                    int lineNo = int.Parse(parts[2]);
                    string itemcode = parts[3];
                    string hblno = parts[4];
                    DateTime conETA = DateTime.Parse(parts[5]);
                    string dateOnlyConETA = conETA.ToString("MMM dd, yyyy");


                    using (SqlConnection con = new SqlConnection(conStr))
                    {
                        await con.OpenAsync();

                        using (SqlCommand cmd = new SqlCommand("_spUntagProcurementCenter", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@DocType", docType);
                            cmd.Parameters.AddWithValue("@DocNo", docNo);
                            cmd.Parameters.AddWithValue("@LineNo", lineNo);
                            cmd.Parameters.AddWithValue("@ItemCode", itemcode);
                            cmd.Parameters.AddWithValue("@HBLNo", hblno);
                            cmd.Parameters.AddWithValue("@ModifiedBy", GetUser());

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Record untagged successfully!";
                    // ✅ EMAIL                    

                    var recipients = GetRecipients();

                    _ = Task.Run(() => _emailService.SendEmailAsync(
                        recipients,
                        "PO Untagged from Shipment",
                        $@"
                        <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>
                            <p>Dear Team,</p>

                            <p>
                                This is to inform you that the following shipment details have already been
                                <strong>Untagged</strong> from the corresponding Purchase Order(s) in the system.
                            </p>

                            <table>

                                <tr>
                                    <th style='text-align:left;'>HBL No</th>
                                    <td style='text-align:left;'>: {hblno}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>PO No</th>
                                    <td style='text-align:left;'>: {docNo}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Item Code</th>
                                    <td style='text-align:left;'>: {itemcode}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Dedon ETA</th>
                                    <td style='text-align:left;'>: {dateOnlyConETA}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Status</th>
                                    <td style='text-align:left;'>: Untagged </td>
                                </tr>

                            </table>

                            <p>
                                Should you encounter any issues or require further assistance,
                                please do not hesitate to contact the IT Team.
                            </p>

                            <p>
                                Regards,<br />
                                Procurement Monitoring Team
                            </p>

                            <hr />

                            <small>
                                This is a system-generated email. Please do not reply.
                            </small>

                        </div>"
                      ));
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
