using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using PlanningTool.NavExtension.Data;
using System.Configuration;
using System.Data;
using static PlanningTool.NavExtension.Pages.QA.VendorsModel;

namespace PlanningTool.NavExtension.Pages.QA
{
    public class VendorsModel : PageModel
    {
        private readonly AppDbContext   _context;
        private readonly IConfiguration _config;

        public List<VendorQA> VendorReport { get; set; } = new();
        public List<VendorOverview> VendorView { get; set; } = new();

        public VendorsModel(AppDbContext context, IConfiguration config)
        {
            _context    = context;
            _config     = config;
        }
        public void OnGet()
        {
            this.GetVendorsCalculation();


            using SqlConnection con = new SqlConnection(_config.GetConnectionString("DevConnection"));
            using SqlCommand cmd    = new SqlCommand("_spGetVendorOverview", con);

            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                VendorView.Add(new VendorOverview
                {
                    // Vendor Information
                    VendorCode      = dr["VendorCode"]?.ToString(),
                    VendorName      = dr["VendorName"]?.ToString(),
                    ContactPerson   = dr["ContactPerson"]?.ToString(),
                    PhoneNo         = dr["PhoneNo"]?.ToString(),
                    Email           = dr["Email"]?.ToString(),
                    Address         = dr["Address"]?.ToString(),
                    City            = dr["City"]?.ToString(),
                    Country         = dr["Country"]?.ToString(),
                    Buyer           = dr["Buyer"]?.ToString(),
                    VendorGroup     = dr["VendorGroup"]?.ToString(),
                    PaymentTerms    = dr["PaymentTerms"]?.ToString(),
                    Currency        = dr["Currency"]?.ToString(),
                    LeadTime        = dr["LeadTime"]?.ToString(),
                    Status          = dr["Status"]?.ToString(),

                });
            }
        }
        public IActionResult OnGetVendorQA()
        {
            List<VendorQA> result = new();

            using (SqlConnection con =
            new SqlConnection(
            _config.GetConnectionString("DevConnection")))
            {

                SqlCommand cmd  = new SqlCommand("_spGetVendorQADashboard",con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    result.Add(new VendorQA
                    {
                        VendorName  = dr["VendorName"].ToString(),
                        QtyReceived = dr["QtyReceived"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReceived"]),
                        QtyGood     = dr["QtyGood"]     == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyGood"]),
                        QtyRepair   = dr["QtyRepair"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyRepair"]),
                        QtyReject   = dr["QtyReject"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReject"]),
                        DefectRate  = dr["DefectRate"]  == DBNull.Value ? 0 : Convert.ToDecimal(dr["DefectRate"])
                    });
                }
            }
            return new JsonResult(result);

        }
        public void GetVendorsCalculation()
        {    
            using SqlConnection con = new SqlConnection(_config.GetConnectionString("DevConnection"));
            using SqlCommand cmd    = new SqlCommand("_spGetVendorQADashboard", con);
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                VendorReport.Add(new VendorQA
                {
                    // Vendor Information
                    VendorName  = dr["VendorName"].ToString(),
                    QtyReceived = dr["QtyReceived"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReceived"]),
                    QtyGood     = dr["QtyGood"]     == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyGood"]),
                    QtyRepair   = dr["QtyRepair"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyRepair"]),
                    QtyReject   = dr["QtyReject"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReject"]),
                    DefectRate  = dr["DefectRate"]  == DBNull.Value ? 0 : Convert.ToDecimal(dr["DefectRate"])
                });
            }            
        }

        public class VendorQA
        {

            public string VendorName { get; set; }
            public decimal QtyReceived { get; set; }
            public decimal QtyGood { get; set; }
            public decimal QtyRepair { get; set; }
            public decimal QtyReject { get; set; }
            public decimal DefectRate { get; set; }

        }
        public class VendorOverview
        {
            public string VendorCode { get; set; }
            public string VendorName { get; set; }
            public string ContactPerson { get; set; }
            public string PhoneNo { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public string Buyer { get; set; }
            public string VendorGroup { get; set; }
            public string PaymentTerms { get; set; }
            public string Currency { get; set; }
            public string LeadTime { get; set; }
            public DateTime? LastModified { get; set; }
            public string Status { get; set; }
        }
    }
}
