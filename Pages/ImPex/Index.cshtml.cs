using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Constants;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;
using System.Security;

namespace PlanningTool.NavExtension.Pages.ImPex
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IUserPermissionService _userPermissionService;
        private readonly string conStr;

        public IndexModel(AppDbContext context, EmailService emailService, IUserPermissionService userPermissionService)
        {
            _context = context;
            _emailService = emailService;
            _userPermissionService = userPermissionService;
            this.conStr = _context.Database.GetConnectionString();
        }
        [BindProperty] public ImPexCenterModel Impex { get; set; }

        public List<ImPexCenterModel> ImpexList { get; set; } = new List<ImPexCenterModel>();

        public List<ProcurementCenterModel> ProcurementList { get; set; } = new();
        public List<SelectListItem> CountryList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var access = HttpContext.Session.GetString("ImPex");

            if (string.IsNullOrEmpty(access))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!bool.Parse(access))
            {
                return RedirectToPage("/AccessDenied");
            }

            this.ImpexList = _context.tblImPexCenter.ToList();
            ProcurementList = await _context.GetTaggedItems.ToListAsync();

            this.LoadCountries();

            if (id.HasValue)
            {
                Impex = await _context.tblImPexCenter
                    .FirstOrDefaultAsync(x => x.PK_ptImpexID == id.Value);

                if (Impex == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    Impex = new ImPexCenterModel();
                }
            }
            else
            {
                Impex = new ImPexCenterModel();
            }


            return Page();
        }

        private void LoadCountries()
        {
            using SqlConnection con = new(conStr);

            string sql = @"
                SELECT [Code], [Name]
                FROM [DEDON2019].[dbo].[DEDONPH$Country_Region]
                WHERE NULLIF(LTRIM(RTRIM([Name])), '') IS NOT NULL
                ORDER BY [Name]";

            using SqlCommand cmd = new(sql, con);

            con.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                this.CountryList.Add(new SelectListItem
                {
                    Value = dr["Name"].ToString(), // or dr["Code"].ToString()
                    Text = $"{dr["Code"]} - {dr["Name"]}"
                });
            }
        }


        private List<string> GetRecipients()
        {

            var recipients = _context.EmailNotifications
            .Where(x => x._ptImPex)
            .Select(x => x._ptEmailAddress)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

            return recipients;

        }

        private string GetUser()
        {
            return (User?.Identity != null && User.Identity.IsAuthenticated)
                ? User.Identity.Name
                : Environment.UserName;
        }
        public IActionResult OnPostSave(int id)
        {
            try
            {
                // ADD Function
                if (Impex.PK_ptImpexID == 0)
                {
                    bool impexExists = _context.tblImPexCenter.AsNoTracking()
                       .Any(x => x._ptHBLNo == Impex._ptHBLNo);

                    if (impexExists)
                    {
                        TempData["ErrorMessage"] = "HBL No already exists.";
                        ImpexList = _context.tblImPexCenter.ToList();
                        return Page();
                    }

                    string user = GetUser();

                    Impex._ptCreatedBy = user;
                    Impex._ptCreatedDate = DateTime.Now;
                    Impex._ptStatus = 0;

                    _context.tblImPexCenter.Add(Impex);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "BHL No :  " + Impex._ptHBLNo + " Successfully created!";

                    var recipients = GetRecipients();

                    // Email functionality can be added here if needed.                    
                    string emailBody = $@"
                    <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                        <p>Dear Team,</p>

                        <p>
                            This is to inform you that a new
                            <strong>HBL entry</strong> has been successfully created and is now available in the system.
                        </p>

                        <table>
                            <tr>
                                <th style='text-align:left;'>HBL No</th>
                                <td>: {Impex._ptHBLNo}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Container No</th>
                                <td>: {Impex._ptContainerNo}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Dedon ETA</th>
                                <td>: {Impex._ptContainerETA:MMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <th style='text-align:left;'>Status</th>
                                <td>: New</td>
                            </tr>
                             <tr>
                                <th style='text-align:left;'>Action Performed By</th>
                                <td>: {GetUser()}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Action Date</th>
                                <td>: {DateTime.Now:MMM dd, yyyy hh:mm tt}</td>
                            </tr>
                        </table>

                        <p><strong>Shipment Details:</strong></p>

                        <table border='1'
                                cellpadding='4'
                                cellspacing='0'
                                style='border-collapse:collapse; width:50%;'>
                            <thead>
                                <tr>
                                    <th style='text-align:left;'>HBL No</th>
                                    <th style='text-align:left;'>Container No</th>
                                    <th style='text-align:left;'>Dedon ETA</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>{Impex._ptHBLNo}</td>
                                    <td>{Impex._ptContainerNo}</td>
                                    <td>{Impex._ptContainerETA:MMM dd, yyyy}</td>
                                </tr>
                            </tbody>
                        </table>

                        <p>
                            The shipment record is now ready for use and reference within the
                            ImPEx Monitoring System.
                        </p>

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
                        "New HBL Entry Created Notification",
                        emailBody,
                        EmailModules.ImPex)
                    );

                }
                // UPDATE Function
                else
                {
                    var existing = _context.tblImPexCenter
                        .FirstOrDefault(x => x.PK_ptImpexID ==
                                             Impex.PK_ptImpexID);


                    if (existing != null)
                    {
                        bool emailExists = _context.tblImPexCenter.Any(x =>
                           x._ptHBLNo == Impex._ptHBLNo &&
                           x.PK_ptImpexID != Impex.PK_ptImpexID);

                        if (emailExists)
                        {
                            TempData["ErrorMessage"] = "HBL No " + Impex._ptHBLNo + " already exists.";
                            ImpexList = _context.tblImPexCenter.ToList();
                            return Page();
                        }
                        // ✅ STORE OLD VALUES
                        var oldHBL = existing._ptHBLNo;
                        var oldContainer = existing._ptContainerNo;
                        var oldETA = existing._ptContainerETA;
                        var oldRemarks = existing._ptRemarks;
                        var oldCOO = existing._ptCountryOfOrigin;

                        // ✅ UPDATE EXISTING RECORD
                        existing._ptHBLNo = Impex._ptHBLNo;
                        existing._ptContainerNo = Impex._ptContainerNo;
                        existing._ptContainerETA = Impex._ptContainerETA;
                        existing._ptCountryOfOrigin = Impex._ptCountryOfOrigin;
                        existing._ptRemarks = Impex._ptRemarks;
                        existing._ptModifiedBy = GetUser();
                        existing._ptModifiedDate = DateTime.Now;


                        // ✅ Update tagged procurement records
                        var procurementRecords = _context.GetTaggedItems
                            .Where(x => x._ptHBLNo == Impex._ptHBLNo
                                     && x._ptStatus == 1)
                            .ToList();

                        foreach (var record in procurementRecords)
                        {
                            record._ptHBLNo = Impex._ptHBLNo;
                            record._ptContainerNo = Impex._ptContainerNo;
                            record._ptContainerETA = Impex._ptContainerETA;
                            record._ptCountryOfOrigin = Impex._ptCountryOfOrigin;

                            record._ptModifiedBy = GetUser();
                            record._ptModifiedDate = DateTime.Now;

                            _context.Entry(record).State = EntityState.Modified;
                        }                   
                        _context.SaveChanges();
                        TempData["SuccessMessage"] = "BHL No: " + Impex._ptHBLNo + " updated successfully!";


                        // ✅ DETECT CHANGES
                        var changeRows = new List<string>();

                        if (oldHBL != existing._ptHBLNo)
                        {
                            changeRows.Add($@"
                            <tr>
                                <td>HBL No</td>
                                <td>{oldHBL}</td>
                                <td>{existing._ptHBLNo}</td>
                            </tr>");
                        }

                        if (oldContainer != existing._ptContainerNo)
                        {
                            changeRows.Add($@"
                            <tr>
                                <td>Container No</td>
                                <td>{oldContainer}</td>
                                <td>{existing._ptContainerNo}</td>
                            </tr>");
                        }

                        if (oldETA != existing._ptContainerETA)
                        {
                            changeRows.Add($@"
                            <tr>
                                <td>Dedon ETA</td>
                                <td>{oldETA:MMM dd, yyyy}</td>
                                <td>{existing._ptContainerETA:MMM dd, yyyy}</td>
                            </tr>");
                        }

                        if (oldRemarks != existing._ptRemarks)
                        {
                            changeRows.Add($@"
                            <tr>
                                <td>Remarks</td>
                                <td>{oldRemarks}</td>
                                <td>{existing._ptRemarks}</td>
                            </tr>");
                        }

                        string changesHtml = changeRows.Any()
                            ? $@"
                            <table border='1'
                                    cellpadding='4'
                                    cellspacing='0'
                                    style='border-collapse:collapse; width:80%;'>

                                <thead>
                                    <tr>
                                        <th style='text-align:left;'>Field</th>
                                        <th style='text-align:left;'>Previous Value</th>
                                        <th style='text-align:left;'>Updated Value</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {string.Join("", changeRows)}
                                </tbody>

                            </table>"
                        : "<p>No changes detected.</p>";

                        try
                        {                           

                            // ✅ EMAIL
                            string emailBody = $@"
                            <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                                <p>Dear Team,</p>

                                <p>
                                    This is to inform you that an existing
                                    <strong>HBL record</strong> has been successfully updated in the system.
                                </p>

                                <table>
                                    <tr>
                                        <th style='text-align:left;'>HBL No</th>
                                        <td>: {existing._ptHBLNo}</td>
                                    </tr>

                                    <tr>
                                        <th style='text-align:left;'>Container No</th>
                                        <td>: {existing._ptContainerNo}</td>
                                    </tr>

                                    <tr>
                                        <th style='text-align:left;'>Dedon ETA</th>
                                        <td>: {existing._ptContainerETA:MMM dd, yyyy}</td>
                                    </tr>

                                    <tr>
                                        <th style='text-align:left;'>Status</th>
                                        <td>: Updated</td>
                                    </tr>
                                </table>

                                <p>
                                    <strong>Changes Made:</strong>
                                </p>

                                {changesHtml}

                                <p>
                                    Please take note of the updated information and make any necessary
                                    adjustments to your processes.
                                </p>

                                <p>
                                    Should you encounter any issues or require further assistance,
                                    please do not hesitate to contact the IT Team.
                                </p>

                                <hr />

                                <small>
                                    This is a system-generated email. Please do not reply.
                                </small>

                            </div>";

                            var recipients = GetRecipients();

                            _ = Task.Run(() =>
                            _emailService.SendEmailAsync(
                                recipients,
                                "HBL Information Updated",
                                emailBody,
                                EmailModules.ImPex)
                            );

                        }                     
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = "Email not sent: " + ex.Message;
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Record not found for update.";
                        ImpexList = _context.tblImPexCenter.ToList();
                        return Page();
                    }

                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                ImpexList = _context.tblImPexCenter.ToList();
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetCheckHblAsync(string hblNo)
        {

            bool exists = await _context.tblImPexCenter
                    .AnyAsync(x => x._ptHBLNo == hblNo);

            return new JsonResult(exists);

        }
    }
}
