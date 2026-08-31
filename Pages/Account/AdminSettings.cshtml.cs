using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Pages.Account
{
    [Authorize]
    public class AdminSettingsModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminSettingsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty] public EmailNotication EmailNotification { get; set; } = new();

        public List<EmailNotication> NotificationList { get; set; } = new();

        [BindProperty] public List<vw_ProcurementETAEmailNotification> ContainerETAView { get; set; } = new();

        public IList<ReminderNotification> ReminderNotifications { get; set; } = new List<ReminderNotification>();

        public async Task OnGet(int? id)
        {
            // Load Notifications
            NotificationList = _context.EmailNotifications.ToList();


            // Load Reminders
            ContainerETAView = _context.vw_ProcurementETAEmailNotification.ToList();

            //ReminderNotifications = await _context.tblReminderNotification
            //            .OrderByDescending(x => x.CreatedDate)
            //            .ToListAsync();

            
            if (id.HasValue)
            {
                EmailNotification = _context.EmailNotifications
                    .FirstOrDefault(x => x._PK_ptEmailNotification_ID == id.Value)
                    ?? new EmailNotication();
            }
        }
        public List<SelectListItem> UserTypes { get; set; } = new()
        {
            new SelectListItem { Value = "Admin", Text = "Admin" },
            new SelectListItem { Value = "User", Text = "User" },
            new SelectListItem { Value = "Notification", Text = "Notification" }
        };
        public IActionResult OnPostSave()
        {
            try
            {
                // Add New Record
                if (EmailNotification._PK_ptEmailNotification_ID == 0)
                {
                    bool emailExists = _context.EmailNotifications
                        .Any(x => x._ptEmailAddress == EmailNotification._ptEmailAddress);

                    if (emailExists)
                    {
                        TempData["ErrorMessage"] = "Email address already exists.";
                        NotificationList = _context.EmailNotifications.ToList();
                        return Page();
                    }

                    EmailNotification._ptStatus         = "Active";
                    EmailNotification._ptCreatedDate    = DateTime.Now;          

                    _context.EmailNotifications.Add(EmailNotification);
                    _context.SaveChanges(); // Save changes to the database

                    TempData["SuccessMessage"] = EmailNotification._ptEmailAddress + "added successfully";
                }
                // Update Records
                else
                {
                    var existing = _context.EmailNotifications
                        .FirstOrDefault(x => x._PK_ptEmailNotification_ID ==
                                             EmailNotification._PK_ptEmailNotification_ID);

                    if (existing != null)
                    {
                        bool emailExists = _context.EmailNotifications.Any(x =>
                            x._ptEmailAddress == EmailNotification._ptEmailAddress &&
                            x._PK_ptEmailNotification_ID != EmailNotification._PK_ptEmailNotification_ID);

                        if (emailExists)
                        {
                            TempData["ErrorMessage"] = "Email address already exists.";
                            NotificationList = _context.EmailNotifications.ToList();
                            return Page();
                        }

                        existing._ptEmailAddress    = EmailNotification._ptEmailAddress;
                        existing._ptImPex           = EmailNotification._ptImPex;
                        existing._ptProcurement     = EmailNotification._ptProcurement;
                        existing._pt2WeeksReminder  = EmailNotification._pt2WeeksReminder;
                        existing._pt2WeeksTrigger   = EmailNotification._pt2WeeksTrigger;
                        existing._ptDelaysReminder  = EmailNotification._ptDelaysReminder;
                        existing._ptDelaysTrigger   = EmailNotification._ptDelaysTrigger;
                        existing._ptUserType        = EmailNotification._ptUserType;
                        existing._ptUpdatedDate     = DateTime.Now;

                        _context.SaveChanges(); // Save changes to the database

                        TempData["SuccessMessage"]  = existing._ptEmailAddress + " updated successfully";
                    }
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                NotificationList = _context.EmailNotifications.ToList();
                return Page();
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var data = _context.EmailNotifications
                .FirstOrDefault(x => x._PK_ptEmailNotification_ID == id);

            if (data != null)
            {
                data._ptStatus = "Inactive";
                _context.SaveChanges(); // Save changes to the database
            }

            TempData["Success"] = "Marked as inactive successfully";
            return RedirectToPage();
        }

        public IActionResult OnPostActivate(int id)
        {
            var data = _context.EmailNotifications
                .FirstOrDefault(x => x._PK_ptEmailNotification_ID == id);

            if (data != null)
            {
                data._ptStatus = "Active";
                _context.SaveChanges(); // Save changes to the database
            }

            TempData["Success"] = "Activated successfully";
            return RedirectToPage();
        }
        private string GenerateEmailBody(ReminderNotification reminder)
        {
            if (reminder.ReminderType == "2WeeksReminder")
            {
                return $@"
                <p>Hello Team,</p>

                <p>
                    This is an automated reminder that the following shipment
                    is expected to arrive within the next 14 days.
                </p>

                <table border='1' cellpadding='5'>
                    <tr>
                        <td><b>PO No</b></td>
                        <td>{reminder.PONo}</td>
                    </tr>
                    <tr>
                        <td><b>HBL No</b></td>
                        <td>{reminder.HBLNo}</td>
                    </tr>
                    <tr>
                        <td><b>Container No</b></td>
                        <td>{reminder.ContainerNo}</td>
                    </tr>
                    <tr>
                        <td><b>ETA</b></td>
                        <td>{reminder.ContainerETA:MMMM dd, yyyy}</td>
                    </tr>
                </table>";
            }

            return $@"
            <p>Hello Team,</p>

            <p>
                The following shipment has exceeded its expected arrival date.
            </p>

            <table border='1' cellpadding='5'>
                <tr>
                    <td><b>PO No</b></td>
                    <td>{reminder.PONo}</td>
                </tr>
                <tr>
                    <td><b>HBL No</b></td>
                    <td>{reminder.HBLNo}</td>
                </tr>
                <tr>
                    <td><b>Container No</b></td>
                    <td>{reminder.ContainerNo}</td>
                </tr>
                <tr>
                    <td><b>ETA</b></td>
                    <td>{reminder.ContainerETA:MMMM dd, yyyy}</td>
                </tr>
            </table>";
        }
    }
}
