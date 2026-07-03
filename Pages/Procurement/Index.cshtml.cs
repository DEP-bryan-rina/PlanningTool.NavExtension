using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using System.Data;

namespace PlanningTool.NavExtension.Pages.Procurement
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;   // ✅ NEW

        private readonly string conStr;
        public string searchString { get; set; }

        public IndexModel(AppDbContext context, IConfiguration config, EmailService emailService)
        {

            _context = context;
            _emailService = emailService;
            conStr = config.GetConnectionString("DefaultConnection");
        }
        public List<ImPexCenterModel> ImpexList { get; set; } = new();


        public List<ProcurementCenterDto> ProcurementList { get; set; } = new();

        [BindProperty] public string ParamRemarks { get; set; }
        [BindProperty] public string HBLNo { get; set; }
        [BindProperty] public ImPexCenterModel Impex { get; set; }
        [BindProperty] public DateTime? ParamCETA { get; set; }

        private string GetUser()
        {
            var userName = (User?.Identity != null && User.Identity.IsAuthenticated)
            ? User.Identity.Name
            : Environment.UserName;

            return $"{userName} ({Environment.MachineName})";
        }

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
        public void OnGet()
        {
            // Get Impex Data Dropdown view
            this.LoadImpex();

            // Get Procurement Data Stored Procedure
            this.LoadProcurementData(searchString);

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
        public async Task OnPostTagAsync(string HBLNo, List<string> SelectedRows)
        {
            try
            {
                if(SelectedRows == null || SelectedRows.Count == 0)
                {
                    TempData["ErrorMessage"] = "No rows selected.";
                    this.LoadImpex();
                    this.LoadProcurementData(searchString);
                    return;
                }

                // Call Stored Procedure function
                using (SqlConnection con = new SqlConnection(conStr))
                {
                    await con.OpenAsync();

                    foreach(var row in SelectedRows)
                    {
                        var parts = row.Split('|');

                        int docType = int.Parse(parts[0]);
                        string docNo = parts[1];
                        int lineNo = int.Parse(parts[2]);
                        string itemcode = parts[3].Trim();


                        string key = $"{docType}|{docNo}|{lineNo}|{itemcode}";

                        string pdValue = Request.Form[$"PartialDeliveredValues[{key}]"];


                        // ✅ Get values from Request
                        string hblno = Request.Form["HBLNo"];
                        string containerNo = Request.Form["ContainerNo"];

                         string? ParamCETA = DateTime.TryParse(Request.Form["ParamCETA"], out var eta)
                            ? eta.ToString("MMM dd, yyyy")
                            : null;


                        using (SqlCommand cmd = new SqlCommand("_spTagProcurementCenter", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@HBLNo", hblno);
                            cmd.Parameters.AddWithValue("@ContainerNo", containerNo);
                            cmd.Parameters.AddWithValue("@DocType", docType);
                            cmd.Parameters.AddWithValue("@DocNo", docNo);
                            cmd.Parameters.AddWithValue("@LineNo", lineNo);
                            cmd.Parameters.AddWithValue("@ItemCode", itemcode); // ✅ IMPORTANT
                            cmd.Parameters.AddWithValue("@ParamPD", pdValue);
                            cmd.Parameters.AddWithValue("@ParamCETA", (object)ParamCETA ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ParamCBY", GetUser());
                            cmd.Parameters.AddWithValue("@ParamRemarks",
                                string.IsNullOrEmpty(ParamRemarks) ? (object)DBNull.Value : ParamRemarks);

                            await cmd.ExecuteNonQueryAsync();

                        }
                    }
                }
                TempData["SuccessMessage"] = $"{SelectedRows.Count} record(s) updated successfully!";

                var recipients = GetRecipients();

                // ✅ EMAIL
                _ = Task.Run(() => _emailService.SendEmailAsync(
                recipients,
                    "Procurement Control Center | HBL and Container ETA tagged from Purchase Orders",
                    $@"
                    <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>

                        <p>Dear Team,</p>
                        <p>
                            Please be informed that the HBL Number <b></b>{HBLNo} </b> 
                            and Container ETA <b>{ParamCETA}</b>
                            have already been tagged to the corresponding Purchase Orders in the System.
                        </p>
                        <p>
                            Should you encounter any issues or require assistance, please do not hesitate to contact the IT team.
                        </p>
                        <br/>
                        <p>This is a system-generated email. Please do not reply.</p>
                    </div>"
                    ));
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;               
            }
            this.LoadImpex();
            this.LoadProcurementData(searchString);
        }
       public async Task OnPostUntagAsync(string action)
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
                        "Procurement Control Center | HBL and Container ETA Untagged from Purchase Orders",
                        $@"
                        <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>
                            <p>Dear Team,</p>

                            <p>
                                Please be informed that the HBL Number <b>{hblno}</b> 
                                and Container ETA <b>{dateOnlyConETA}</b>
                                have already been <b>Untagged</b> to the corresponding Purchase Orders in the System.
                            </p>

                            <p>
                                Should you encounter any issues or require assistance, please do not hesitate to contact the IT team.
                            </p>

                            <br/>
                            <p>This is a system-generated email. Please do not reply.</p>

                        </div>"
                      ));
                }
            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = "Database error: " + sqlEx.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unexpected error: " + ex.Message;
            }

            // ✅ Always reload data
            this.LoadProcurementData(searchString);
            this.LoadImpex();
        }

        private void LoadProcurementData(string _search)
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
                            ptRemarks = reader["ptRemarks"] == DBNull.Value ? "" : reader["ptRemarks"].ToString()
                        });
                    }
                }
            }
        }
    }
}
