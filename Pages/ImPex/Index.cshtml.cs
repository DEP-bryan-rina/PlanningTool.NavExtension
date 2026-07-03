using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Pages.ImPex
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public IndexModel(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty] public ImPexCenterModel Impex { get; set; }


        public List<ImPexCenterModel> ImpexList { get; set; } = new List<ImPexCenterModel>();


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

        public void OnGet(int? id)
        {
            ImpexList.Clear();
            ImpexList = _context.tblImPexCenter.ToList();

            Impex = new ImPexCenterModel();

            if (id.HasValue)
            {
                Impex = _context.tblImPexCenter
                    .FirstOrDefault(x => x.PK_ptImpexID == id.Value)
                    ?? new ImPexCenterModel();
            }
        }
        private string GetUser()
        {
            var userName = (User?.Identity != null && User.Identity.IsAuthenticated)
            ? User.Identity.Name
            : Environment.UserName;

            return $"{userName} ({Environment.MachineName})";
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
                    _ = Task.Run(() => _emailService.SendEmailAsync(
                        recipients,
                        "Procurement Control Center | New HBL Entry Created",
                        $@"
                         <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>
                          <p>Dear Team,</p>
                          <p>
                              Please be informed that the New HBL Entry <b>{Impex._ptHBLNo}</b> has been successfully created and is now ready 
                              for use.
                          </p>
                          <p>
                            In the event of any updates or chages to the shipment details, a separate notification will be sent accordingly
                            to kepp everyone informed.
                          </p>
                          <p>
                            Should you encounter any issues or require assistance, please do not hesitate to contact the IT team.
                          </p>        
                          <br/>
                          <p>This is a system-generated email. Please do not reply.</p>
                        </div>"
                    ));

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


                        existing._ptHBLNo = Impex._ptHBLNo;
                        existing._ptContainerNo = Impex._ptContainerNo;
                        existing._ptContainerETA = Impex._ptContainerETA;
                        existing._ptRemarks = Impex._ptRemarks;
                        existing._ptModifiedBy = GetUser();
                        existing._ptModifiedDate = DateTime.Now;

                        _context.SaveChanges();
                        TempData["SuccessMessage"] = "BHL No: " + Impex._ptHBLNo + " updated successfully!";

                        // ✅ DETECT CHANGES
                        var changes = new List<string>();

                        if (oldHBL != existing._ptHBLNo)
                            changes.Add($"HBL No: {oldHBL} → {existing._ptHBLNo}");

                        if (oldContainer != existing._ptContainerNo)
                            changes.Add($"<span style='color:#00ff99;'>HBL No: {oldHBL} → {existing._ptHBLNo}</span>");


                        if (oldETA != existing._ptContainerETA)
                            changes.Add($"Container ETA: {oldETA:yyyy-MM-dd} → {existing._ptContainerETA:yyyy-MM-dd}");

                        if (oldRemarks != existing._ptRemarks)
                            changes.Add($"Remarks: {oldRemarks} → {existing._ptRemarks}");

                        string changesHtml = changes.Any()
                            ? string.Join("<br/>", changes)
                            : "No changes detected";

                        try
                        {
                            var recipients = GetRecipients();

                            // ✅ EMAIL
                            _ = Task.Run(() => _emailService.SendEmailAsync(
                                recipients,
                                "Procurement Control Center | Updated",
                                $@"
                                <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>
                                    <p>
                                        Dear Team,
                                    </p>
                                    <p>
                                        Please be informed that the following HBL: {existing._ptHBLNo} has been updated.
                                    </p>
                                        <p><b>Current Details:</b></p>
                                        <table style='border-collapse:collapse; font-family: 'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>
                                          <tr>
                                                <td>BHL No: </td>
                                                <td>{existing._ptHBLNo}</td>
                                          </tr>
                                          <tr>            
                                                <td>Container No: </td>
                                                <td>{existing._ptContainerNo}</td>
                                           </tr>
                                           <tr>
                                                <td>Container ETA: </td>
                                                <td>{existing._ptContainerETA:yyyy-MM-dd}</td>
                                           </tr>
                                            <tr>
                                                <td>Remarks:</td>
                                                <td>{existing._ptRemarks}</td>
                                            </tr>
                                            </table>
                                        <br/>
                                    <p><b>Changes Made:</b></p>
                                    <p>{changesHtml}</p>
                                    <br/>
                                <p>This is a system-generated email. Please do not reply.</p>

                            </div>
                                "
                            ));

                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = "Email not sent " + ex.Message;
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
    }
}
